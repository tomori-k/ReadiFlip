using ReadiFlip.Edax;
using ReadiFlip.Reversi;

namespace ReadiFlip.Generator;

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
    public (Board, Color) Generate(int d1 = 4, int d2 = 6, int minPly = 15, int maxPly = 50, int trial = 10000)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(d1);
        ArgumentOutOfRangeException.ThrowIfNegative(d2);
        if (!(0 <= minPly && minPly <= 60)) throw new ArgumentOutOfRangeException();

        for (int i = 0; i < trial; ++i)
        {
            var reversi = new Reversi.Reversi();

            while (!reversi.IsOver)
            {
                var ply = 60 - reversi.Board.NumEmpties;

                if (minPly <= ply && ply <= maxPly && IsGoodForPractice(reversi.Board, d1, d2))
                {
                    return (reversi.Board, reversi.Color);
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
    public bool IsGoodForPractice(Board board, int d1, int d2)
    {
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
        if (!candidatesDepth1.Any(x => x.Sq == bestDepth3.Sq)) return false;

        // 3. 3手読みの他の手が最善手よりも明らかに悪い
        if (!(scoresDepth3
            .Where(x => bestDepth3.Score - x.Score <= d2)
            .Count() <= 1)
        ) return false;

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

        reversi.MakeMove(move.Sq);
    }

    record MoveWithScore(Square Sq, int Score);

    List<MoveWithScore> GenerateMovesWithScore(Reversi.Reversi reversi, int depth)
    {
        if (depth <= 0) throw new ArgumentOutOfRangeException();

        var moves = reversi.GenerateMoves();
        var movesWithScore = new List<MoveWithScore>();

        foreach (var move in moves)
        {
            reversi.MakeMove(move);
            movesWithScore.Add(new(move, -Search(reversi, depth - 1)));
            reversi.UndoMove();
        }

        return movesWithScore;
    }

    int Search(Reversi.Reversi reversi, int depth, int alpha = EdaxEval.SCORE_MIN, int beta = EdaxEval.SCORE_MAX)
    {
        if (depth == 0) return eval.Evaluate(reversi.Board);

        var moves = reversi.GenerateMoves();
        var bestScore = EdaxEval.SCORE_MIN;

        foreach (var move in moves)
        {
            reversi.MakeMove(move);
            try
            {
                var score = -Search(reversi, depth - 1, -beta, -Math.Max(alpha, bestScore));

                if (score >= beta) return score;
                if (score > bestScore) bestScore = score;
            }
            finally
            {
                reversi.UndoMove();
            }
        }

        return bestScore;
    }
}
