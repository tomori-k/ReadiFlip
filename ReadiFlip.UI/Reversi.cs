using System.Diagnostics;
using System.Numerics;

namespace ReadiFlip.Reversi;

public enum Square
{
    A1, B1, C1, D1, E1, F1, G1, H1,
    A2, B2, C2, D2, E2, F2, G2, H2,
    A3, B3, C3, D3, E3, F3, G3, H3,
    A4, B4, C4, D4, E4, F4, G4, H4,
    A5, B5, C5, D5, E5, F5, G5, H5,
    A6, B6, C6, D6, E6, F6, G6, H6,
    A7, B7, C7, D7, E7, F7, G7, H7,
    A8, B8, C8, D8, E8, F8, G8, H8,
    PASS, NOMOVE
};

public enum Color
{
    BLACK = 0,
    WHITE,
    EMPTY,
    OFF_SIDE
};

public record Board(ulong Player, ulong Opponent)
{
    public Board Inv => new Board(Opponent, Player);

    public Color this[Square x]
    {
        get
        {
            ulong b = 1UL << (int)x;
            return (Color)(Convert.ToInt32((Player & b) == 0) * 2 - Convert.ToInt32((Opponent & b) != 0));
        }
    }

    public int NumEmpties => 64 - BitOperations.PopCount(Player ^ Opponent);

    public static Board Init { get; } = new(0x0000000810000000, 0x0000001008000000);

    public override string ToString()
    {
        // Player=X
        // Opponent=O

        return string.Join(
            string.Empty,
            Enumerable.Range(0, 64)
            .Select(x => this[(Square)x] == Color.BLACK ? 'X' : this[(Square)x] == Color.WHITE ? 'O' : '-')
        );
    }
}

public class Reversi
{
    readonly Stack<Board> boards = new();

    public Board Board { get; private set; }
    public Color Color { get; private set; }

    public bool IsOver => GenerateMoves().Count == 0;

    public Reversi() : this(Board.Init, Color.BLACK)
    {
    }

    public Reversi(Board board) : this(board, Color.BLACK)
    {
    }

    public Reversi(Board board, Color color)
    {
        this.Board = board;
        this.Color = color;
    }

    public List<Square> GenerateMoves()
    {
        var moves = new List<Square>();

        for (var i = 0; i < 64; ++i)
        {
            var sq = (Square)i;

            if (Board[sq] != Color.EMPTY) continue;

            var flip = ComputeFlip(Board, sq);

            if (flip != 0)
            {
                moves.Add(sq);
            }
        }

        return moves;
    }

    public static ulong ComputeFlip(Board board, Square sq)
    {
        var flip = 0UL;

        for (var dy = -1; dy <= 1; ++dy)
        {
            for (var dx = -1; dx <= 1; ++dx)
            {
                if (dy == 0 && dx == 0) continue;
                flip ^= ComputeFlip(board, sq, dy, dx);
            }
        }

        return flip;
    }

    public static ulong ComputeFlip(Board board, Square sq, int dy, int dx)
    {
        var flip = 0UL;
        var y = (int)sq / 8;
        var x = (int)sq % 8;

        for (var k = 1; k <= 8; ++k)
        {
            var ny = y + k * dy;
            var nx = x + k * dx;

            if (!(0 <= ny && ny < 8 && 0 <= nx && nx < 8))
            {
                return 0UL;
            }

            var s = ny * 8 + nx;

            if (((board.Opponent >> s) & 1) != 0)
            {
                flip ^= 1UL << s;
            }
            else if (((board.Player >> s) & 1) != 0)
            {
                return flip;
            }
            else
            {
                return 0UL;
            }
        }

        throw new UnreachableException();
    }

    public void MakeMove(Square sq)
    {
        var flip = ComputeFlip(Board, sq);

        if (flip == 0UL) throw new Exception("Invalid move");

        this.boards.Push(this.Board);
        this.Board = new(this.Board.Opponent ^ flip, this.Board.Player ^ flip ^ (1UL << (int)sq));
        this.Color = this.Color == Color.BLACK ? Color.WHITE : Color.BLACK;
    }

    public void UndoMove()
    {
        this.Board = this.boards.Pop();
        this.Color = this.Color == Color.BLACK ? Color.WHITE : Color.BLACK;
    }

    public override string ToString()
    {
        return $"{(Color == Color.BLACK ? Board : Board.Inv)} {(Color == Color.BLACK ? 'X' : 'O')}";
    }
}