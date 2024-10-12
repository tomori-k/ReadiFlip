using ReadiFlip.Edax;
using ReadiFlip.Reversi;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ReadiFlip.Generator;

public record Puzzle(
    Board Board,
    Color Color,
    SearchResult BestMove,
    List<SearchResult> OtherMoves
);

public record SearchResult(
    Square Move,
    int Score,
    Square[] Pv
);

[RequiresUnreferencedCode("Necessary because of RangeAttribute usage")]
public record Parameter
{
    [Range(0, 64, ErrorMessage = "D1 invalid (0-64)")]
    public int D1 { get; set; } = 4;

    [Range(0, 64, ErrorMessage = "D2 invalid (0-64)")]
    public int D2 { get; set; } = 6;

    [Range(0, 60, ErrorMessage = "Min ply invalid (0-60)")]
    public int MinPly { get; set; } = 15;

    [Range(0, 60, ErrorMessage = "Max ply invalid (0-60)")]
    public int MaxPly { get; set; } = 50;

    public int Trial { get; set; } = 10000;
}

public class Generator
{
    readonly EdaxEval eval;
    readonly Random random;

    public Generator(EdaxEval eval)
    {
        this.eval = eval;
        this.random = new Random();
    }

    public Generator(EdaxEval eval, int seed)
    {
        this.eval = eval;
        this.random = new Random(seed);
    }

    /// <summary>
    /// 読み練習用の局面を 1 つ生成する。条件は以下の通り。<br/>
    ///
    /// - 1 手読みで最善のスコアを x とし、スコアが x - d1 以上の手が他に 1 手以上存在する。 <br/>
    /// - 3 手読みでの最善手が上の1手読みの条件に引っかかる手の中に存在する。<br/>
    /// - 3 手読みで最善のスコアを x として、その他の手はすべて x - d2 未満のスコアになる。 <br/>
    /// </summary>
    /// <param name="d1"></param>
    /// <param name="d2"></param>
    /// <param name="minPly"></param>
    /// <param name="maxPly"></param>
    /// <param name="trial"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Puzzle Generate(Parameter param)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(param.D1);
        ArgumentOutOfRangeException.ThrowIfNegative(param.D2);
        if (!(0 <= param.MinPly && param.MinPly <= 60)) throw new ArgumentOutOfRangeException();

        for (int i = 0; i < param.Trial; ++i)
        {
            var reversi = new Reversi.Reversi();

            while (!reversi.IsOver)
            {
                var ply = 60 - reversi.Board.NumEmpties;

                if (param.MinPly <= ply && ply <= param.MaxPly && IsGoodForPractice(reversi.Board, param.D1, param.D2, out var answer))
                {
                    return new(reversi.Board, reversi.Color, answer.Value.Best, answer.Value.Others);
                }

                MakeMove(reversi, random);
            }
        }

        throw new Exception("Max trial reached.");
    }

    /// <summary>
    /// 読み練習に使える局面か判定する。
    /// </summary>
    /// <param name="board"></param>
    /// <returns></returns>
    public bool IsGoodForPractice(Board board, int d1, int d2, [NotNullWhen(true)] out (SearchResult Best, List<SearchResult> Others)? answer)
    {
        // default
        answer = null;

        var reversi = new Reversi.Reversi(board);

        if (reversi.IsOver) return false;

        var scoresDepth1 = GenerateMovesWithScore(reversi, 1);
        var bestScoreDepth1 = scoresDepth1.MaxBy(x => x.Score)!.Score;
        var candidatesDepth1 = scoresDepth1.Where(x => x.Score >= bestScoreDepth1 - d1);

        // 1. 1手読みで手の候補が複数ある
        if (!(candidatesDepth1.Count() > 1)) return false;

        var scoresDepth3 = GenerateMovesWithScore(reversi, 3);
        var bestDepth3 = scoresDepth3.MaxBy(x => x.Score)!;

        // 2. 3手読みでの最善手が 1 手読みの候補の中にある
        if (!candidatesDepth1.Any(x => x.Move == bestDepth3.Move)) return false;

        // 3. 3手読みの他の手が最善手よりも明らかに悪い
        if (!(scoresDepth3
            .Where(x => bestDepth3.Score - x.Score <= d2)
            .Count() <= 1)
        ) return false;

        answer = (
            bestDepth3,
            scoresDepth3
                .Where(x => candidatesDepth1.Select(y => y.Move).Contains(x.Move))
                .ToList()
        );

        return true;
    }

    /// <summary>
    /// 3手読みである程度よさそうな手で 1 手進める。
    /// 最善手を打つとは限らない。
    /// </summary>
    /// <param name="reversi"></param>
    /// <param name="random"></param>
    public void MakeMove(Reversi.Reversi reversi, Random random)
    {
        const int D = 4; // -4 の手まで許容

        var movesWithScore = GenerateMovesWithScore(reversi, 3)
            .OrderByDescending(x => x.Score);
        var bestScore = movesWithScore.First().Score;
        var candidates = movesWithScore
            .Where(x => x.Score >= bestScore - D)
            .ToList();

        var i = random.Next(candidates.Count);
        var move = candidates[i];

        reversi.MakeMove(move.Move);
    }


    List<SearchResult> GenerateMovesWithScore(Reversi.Reversi reversi, int depth)
    {
        if (depth <= 0) throw new ArgumentOutOfRangeException();

        var moves = reversi.GenerateMoves();
        var results = new List<SearchResult>();
        Span<Square> pvBuffer = stackalloc Square[depth - 1];

        foreach (var move in moves)
        {
            reversi.MakeMove(move);
            var score = -Search(reversi, depth - 1, pvBuffer);
            results.Add(new(move, score, pvBuffer.ToArray()));
            reversi.UndoMove();
        }

        return results;
    }

    int Search(Reversi.Reversi reversi, int depth, Span<Square> pv, int alpha = EdaxEval.SCORE_MIN, int beta = EdaxEval.SCORE_MAX)
    {
        if (depth == 0) return eval.Evaluate(reversi.Board);

        var moves = reversi.GenerateMoves();
        var bestScore = EdaxEval.SCORE_MIN;
        Span<Square> pvBuffer = stackalloc Square[pv.Length - 1];

        foreach (var move in moves)
        {
            reversi.MakeMove(move);
            try
            {
                var score = -Search(reversi, depth - 1, pvBuffer, -beta, -Math.Max(alpha, bestScore));

                if (score >= beta) return score;
                if (score > bestScore)
                {
                    bestScore = score;
                    pv[0] = move;
                    pvBuffer.CopyTo(pv[1..]);
                }
            }
            finally
            {
                reversi.UndoMove();
            }
        }

        return bestScore;
    }
}
