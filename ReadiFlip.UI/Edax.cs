using ReadiFlip.Reversi;
using System.Buffers.Binary;

namespace ReadiFlip.Edax;

// https://qiita.com/tanaka-a/items/6d6725d5866ebe85fb0b
// https://github.com/abulmo/edax-reversi/blob/master/src/eval.c
// Console.WriteLine($"{eval.Evaluate(ReadiFlip.Reversi.Board.Init)}"); // -4
// Console.WriteLine($"{eval.Evaluate(new ReadiFlip.Reversi.Board(0x0000003c1c040000, 0x0000080020080000))}"); // -1
// Console.WriteLine($"{eval.Evaluate(new ReadiFlip.Reversi.Board(0x0000080c1cec3830, 0x1c3c3670e0100004))}"); // 2 (228)

public class EdaxFeature
{
    public const int NUM_FEATURES = 47;

    /** array to convert features into coordinates */
    static readonly Square[][] EVAL_F2X = [
        [Square.A1, Square.B1, Square.A2, Square.B2, Square.C1, Square.A3, Square.C2, Square.B3, Square.C3],
        [Square.H1, Square.G1, Square.H2, Square.G2, Square.F1, Square.H3, Square.F2, Square.G3, Square.F3],
        [Square.A8, Square.A7, Square.B8, Square.B7, Square.A6, Square.C8, Square.B6, Square.C7, Square.C6],
        [Square.H8, Square.H7, Square.G8, Square.G7, Square.H6, Square.F8, Square.G6, Square.F7, Square.F6],

        [Square.A5, Square.A4, Square.A3, Square.A2, Square.A1, Square.B2, Square.B1, Square.C1, Square.D1, Square.E1],
        [Square.H5, Square.H4, Square.H3, Square.H2, Square.H1, Square.G2, Square.G1, Square.F1, Square.E1, Square.D1],
        [Square.A4, Square.A5, Square.A6, Square.A7, Square.A8, Square.B7, Square.B8, Square.C8, Square.D8, Square.E8],
        [Square.H4, Square.H5, Square.H6, Square.H7, Square.H8, Square.G7, Square.G8, Square.F8, Square.E8, Square.D8],

        [Square.B2, Square.A1, Square.B1, Square.C1, Square.D1, Square.E1, Square.F1, Square.G1, Square.H1, Square.G2],
        [Square.B7, Square.A8, Square.B8, Square.C8, Square.D8, Square.E8, Square.F8, Square.G8, Square.H8, Square.G7],
        [Square.B2, Square.A1, Square.A2, Square.A3, Square.A4, Square.A5, Square.A6, Square.A7, Square.A8, Square.B7],
        [Square.G2, Square.H1, Square.H2, Square.H3, Square.H4, Square.H5, Square.H6, Square.H7, Square.H8, Square.G7],

        [Square.A1, Square.C1, Square.D1, Square.C2, Square.D2, Square.E2, Square.F2, Square.E1, Square.F1, Square.H1],
        [Square.A8, Square.C8, Square.D8, Square.C7, Square.D7, Square.E7, Square.F7, Square.E8, Square.F8, Square.H8],
        [Square.A1, Square.A3, Square.A4, Square.B3, Square.B4, Square.B5, Square.B6, Square.A5, Square.A6, Square.A8],
        [Square.H1, Square.H3, Square.H4, Square.G3, Square.G4, Square.G5, Square.G6, Square.H5, Square.H6, Square.H8],

        [Square.A2, Square.B2, Square.C2, Square.D2, Square.E2, Square.F2, Square.G2, Square.H2],
        [Square.A7, Square.B7, Square.C7, Square.D7, Square.E7, Square.F7, Square.G7, Square.H7],
        [Square.B1, Square.B2, Square.B3, Square.B4, Square.B5, Square.B6, Square.B7, Square.B8],
        [Square.G1, Square.G2, Square.G3, Square.G4, Square.G5, Square.G6, Square.G7, Square.G8],

        [Square.A3, Square.B3, Square.C3, Square.D3, Square.E3, Square.F3, Square.G3, Square.H3],
        [Square.A6, Square.B6, Square.C6, Square.D6, Square.E6, Square.F6, Square.G6, Square.H6],
        [Square.C1, Square.C2, Square.C3, Square.C4, Square.C5, Square.C6, Square.C7, Square.C8],
        [Square.F1, Square.F2, Square.F3, Square.F4, Square.F5, Square.F6, Square.F7, Square.F8],

        [Square.A4, Square.B4, Square.C4, Square.D4, Square.E4, Square.F4, Square.G4, Square.H4],
        [Square.A5, Square.B5, Square.C5, Square.D5, Square.E5, Square.F5, Square.G5, Square.H5],
        [Square.D1, Square.D2, Square.D3, Square.D4, Square.D5, Square.D6, Square.D7, Square.D8],
        [Square.E1, Square.E2, Square.E3, Square.E4, Square.E5, Square.E6, Square.E7, Square.E8],

        [Square.A1, Square.B2, Square.C3, Square.D4, Square.E5, Square.F6, Square.G7, Square.H8],
        [Square.A8, Square.B7, Square.C6, Square.D5, Square.E4, Square.F3, Square.G2, Square.H1],

        [Square.B1, Square.C2, Square.D3, Square.E4, Square.F5, Square.G6, Square.H7],
        [Square.H2, Square.G3, Square.F4, Square.E5, Square.D6, Square.C7, Square.B8],
        [Square.A2, Square.B3, Square.C4, Square.D5, Square.E6, Square.F7, Square.G8],
        [Square.G1, Square.F2, Square.E3, Square.D4, Square.C5, Square.B6, Square.A7],

        [Square.C1, Square.D2, Square.E3, Square.F4, Square.G5, Square.H6],
        [Square.A3, Square.B4, Square.C5, Square.D6, Square.E7, Square.F8],
        [Square.F1, Square.E2, Square.D3, Square.C4, Square.B5, Square.A6],
        [Square.H3, Square.G4, Square.F5, Square.E6, Square.D7, Square.C8],

        [Square.D1, Square.E2, Square.F3, Square.G4, Square.H5],
        [Square.A4, Square.B5, Square.C6, Square.D7, Square.E8],
        [Square.E1, Square.D2, Square.C3, Square.B4, Square.A5],
        [Square.H4, Square.G5, Square.F6, Square.E7, Square.D8],

        [Square.D1, Square.C2, Square.B3, Square.A4],
        [Square.A5, Square.B6, Square.C7, Square.D8],
        [Square.E1, Square.F2, Square.G3, Square.H4],
        [Square.H5, Square.G6, Square.F7, Square.E8],

        [Square.NOMOVE]
    ];

    static readonly ushort[] EVAL_OFFSET = [
        0,     0,     0,     0,     0,     0,     0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
        0,     0,     0,     0,  6561,  6561,  6561,  6561, 13122, 13122, 13122, 13122, 19683, 19683,     0,     0,
        0,     0,  2187,  2187,  2187,  2187,  2916,  2916,  2916,  2916,  3159,  3159,  3159,  3159,     0,     0
    ];

    public ushort[] Feature { get; }

    EdaxFeature(ushort[] feature)
    {
        this.Feature = feature;
    }

    public static EdaxFeature From(Board board)
    {
        var feature = new ushort[48];
        var b = (board.NumEmpties & 1) != 0 ? board.Inv : board;

        for (var i = 0; i < NUM_FEATURES; ++i)
        {
            ushort x = 0;
            for (var j = 0; j < EVAL_F2X[i].Length; j++)
            {
                x = (ushort)(x * 3 + (int)b[EVAL_F2X[i][j]]);
            }
            feature[i] = (ushort)(x + EVAL_OFFSET[i]);
        }

        return new(feature);
    }
}

public class EdaxEval
{
    const uint EDAX = 0x45444158;
    const uint EVAL = 0x4556414c;
    const uint LAVE = 0x4c415645;
    const uint XADE = 0x58414445;
    const int NUM_WEIGHTS = 114364;

    public const int NUM_PLY = 54;
    public const int SCORE_MIN = -64;
    public const int SCORE_MAX = 64;

    static readonly int[] EVAL_PACKED_OFS = [0, 10206, 40095, 69741, 99387, 102708, 106029, 109350, 112671, 113805, 114183, 114318, 114363];
    static readonly SymmetryPacking[] P;

    readonly EvalWeight[] weights;

    public EvalHeader Header { get; }

    EdaxEval(EvalHeader header, EvalWeight[] weights)
    {
        this.Header = header;
        this.weights = weights;
    }

    public int Evaluate(Board board)
    {
        var feature = EdaxFeature.From(board);
        var score = AccumulateEval(feature, 60 - board.NumEmpties);

        if (score > 0) score += 64; else score -= 64;
        score /= 128;

        if (score < SCORE_MIN + 1) score = SCORE_MIN + 1;
        if (score > SCORE_MAX - 1) score = SCORE_MAX - 1;

        return score;
    }

    int AccumulateEval(EdaxFeature feature, int ply)
    {
        if (ply >= NUM_PLY)
            ply = NUM_PLY - 2 + (ply & 1);
        ply -= 2;
        if (ply < 0)
            ply &= 1;

        var w = weights[ply];
        var f = feature.Feature;

        var sum = w.C9[f[0]] + w.C9[f[1]] + w.C9[f[2]] + w.C9[f[3]]
          + w.C10[f[4]] + w.C10[f[5]] + w.C10[f[6]] + w.C10[f[7]]
          + w.S100[f[8]] + w.S100[f[9]] + w.S100[f[10]] + w.S100[f[11]]
          + w.S101[f[12]] + w.S101[f[13]] + w.S101[f[14]] + w.S101[f[15]]
          + w.S8x4[f[16]] + w.S8x4[f[17]] + w.S8x4[f[18]] + w.S8x4[f[19]]
          + w.S8x4[f[20]] + w.S8x4[f[21]] + w.S8x4[f[22]] + w.S8x4[f[23]]
          + w.S8x4[f[24]] + w.S8x4[f[25]] + w.S8x4[f[26]] + w.S8x4[f[27]]
          + w.S7654[f[30]] + w.S7654[f[31]] + w.S7654[f[32]] + w.S7654[f[33]]
          + w.S7654[f[34]] + w.S7654[f[35]] + w.S7654[f[36]] + w.S7654[f[37]]
          + w.S7654[f[38]] + w.S7654[f[39]] + w.S7654[f[40]] + w.S7654[f[41]]
          + w.S7654[f[42]] + w.S7654[f[43]] + w.S7654[f[44]] + w.S7654[f[45]];

        return sum + w.S8x4[f[28]] + w.S8x4[f[29]] + w.S0;
    }

    public record EvalHeader(
        uint Edax,
        uint Eval,
        uint Version,
        uint Release,
        uint Build,
        double Date
    );

    /* unpacked weight */
    class EvalWeight
    {
        public short S0;
        public short[] C9 = new short[19683];
        public short[] C10 = new short[59049];
        public short[] S100 = new short[59049];
        public short[] S101 = new short[59049];
        public short[] S8x4 = new short[6561 * 4];
        public short[] S7654 = new short[2187 + 729 + 243 + 81];
    }

    public static EdaxEval ReadEval(BinaryReader reader)
    {
        var header = ReadHeader(reader);
        var weights = ReadWeights(reader, header.Edax);

        return new(header, weights);
    }

    static EvalHeader ReadHeader(BinaryReader reader)
    {
        var edaxHeader = reader.ReadUInt32();
        var evalHeader = reader.ReadUInt32();

        if (!IsValidHeader(edaxHeader, evalHeader))
        {
            throw new Exception("Invalid eval file");
        }

        var version = reader.ReadUInt32();
        var release = reader.ReadUInt32();
        var build = reader.ReadUInt32();
        var date = reader.ReadDouble();

        if (edaxHeader == XADE)
        {
            version = BinaryPrimitives.ReverseEndianness(version);
            release = BinaryPrimitives.ReverseEndianness(release);
            build = BinaryPrimitives.ReverseEndianness(build);
        }

        return new EvalHeader(edaxHeader, evalHeader, version, release, build, date);
    }

    static EvalWeight[] ReadWeights(BinaryReader reader, uint edaxHeader)
    {
        var w = new short[NUM_WEIGHTS];
        var weights = new EvalWeight[NUM_PLY - 2];

        for (var ply = 0; ply < NUM_PLY; ++ply)
        {
            for (var i = 0; i < NUM_WEIGHTS; ++i)
            {
                w[i] = reader.ReadInt16();
            }

            if (ply < 2) continue; // skip ply 1 & 2

            if (edaxHeader == XADE)
            {
                for (var i = 0; i < NUM_WEIGHTS; ++i)
                {
                    w[i] = BinaryPrimitives.ReverseEndianness(w[i]);
                }
            }

            var weight = weights[ply - 2] = new();
            var pp = P[ply & 1];

            for (var k = 0; k < 19683; k++)
            {
                weight.C9[k] = w[pp.EVAL_C9[k] + EVAL_PACKED_OFS[0]];
            }
            for (var k = 0; k < 59049; k++)
            {
                weight.C10[k] = w[pp.EVAL_C10[k] + EVAL_PACKED_OFS[1]];
                var i = pp.EVAL_S10[k];
                weight.S100[k] = w[i + EVAL_PACKED_OFS[2]];
                weight.S101[k] = w[i + EVAL_PACKED_OFS[3]];
            }
            for (var k = 0; k < 6561; k++)
            {
                var i = pp.EVAL_S8[k];
                weight.S8x4[k] = w[i + EVAL_PACKED_OFS[4]];
                weight.S8x4[k + 6561] = w[i + EVAL_PACKED_OFS[5]];
                weight.S8x4[k + 13122] = w[i + EVAL_PACKED_OFS[6]];
                weight.S8x4[k + 19683] = w[i + EVAL_PACKED_OFS[7]];
            }
            for (var k = 0; k < 2187; k++)
            {
                weight.S7654[k] = w[pp.EVAL_S7[k] + EVAL_PACKED_OFS[8]];
            }
            for (var k = 0; k < 729; k++)
            {
                weight.S7654[k + 2187] = w[pp.EVAL_S6[k] + EVAL_PACKED_OFS[9]];
            }
            for (var k = 0; k < 243; k++)
            {
                weight.S7654[k + 2916] = w[pp.EVAL_S5[k] + EVAL_PACKED_OFS[10]];
            }
            for (var k = 0; k < 81; k++)
            {
                weight.S7654[k + 3159] = w[pp.EVAL_S4[k] + EVAL_PACKED_OFS[11]];
            }
            weight.S0 = w[EVAL_PACKED_OFS[12]];
        }

        return weights;
    }

    static bool IsValidHeader(uint edaxHeader, uint evalHeader)
    {
        // 違和感しかないが本家がこうなってる
        return edaxHeader == EDAX || evalHeader == EVAL || edaxHeader == XADE || evalHeader == LAVE;
    }

    static EdaxEval()
    {
        P = [new SymmetryPacking(), new SymmetryPacking()];

        ReadOnlySpan<int> kd_S10 = [19683, 6561, 2187, 729, 243, 81, 27, 9, 3, 1];
        ReadOnlySpan<int> kd_C10 = [19683, 6561, 2187, 729, 81, 243, 27, 9, 3, 1];
        ReadOnlySpan<int> kd_C9 = [1, 9, 3, 81, 27, 243, 2187, 729, 6561];
        var T = new int[2 * 59049];
        var OPPONENT_FEATURE = new ushort[59049];

        SetOpponentFeature(OPPONENT_FEATURE, 0, 10);

        SetEvalPacking(P[0].EVAL_S8, T, kd_S10[2..], 0, 0, 0, 8);   /* 8 squares : 6561 -> 3321 */
        for (var j = 0; j < 6561; ++j)
            P[1].EVAL_S8[j] = P[0].EVAL_S8[OPPONENT_FEATURE[j + 26244]];  // 1100000000(3)

        SetEvalPacking(P[0].EVAL_S7, T, kd_S10[3..], 0, 0, 0, 7);   /* 7 squares : 2187 -> 1134 */
        for (var j = 0; j < 2187; ++j)
            P[1].EVAL_S7[j] = P[0].EVAL_S7[OPPONENT_FEATURE[j + 28431]];  // 1110000000(3)

        SetEvalPacking(P[0].EVAL_S6, T, kd_S10[4..], 0, 0, 0, 6);   /* 6 squares : 729 -> 378 */
        for (var j = 0; j < 729; ++j)
            P[1].EVAL_S6[j] = P[0].EVAL_S6[OPPONENT_FEATURE[j + 29160]];  // 1111000000(3)

        SetEvalPacking(P[0].EVAL_S5, T, kd_S10[5..], 0, 0, 0, 5);   /* 5 squares : 243 -> 135 */
        for (var j = 0; j < 243; ++j)
            P[1].EVAL_S5[j] = P[0].EVAL_S5[OPPONENT_FEATURE[j + 29403]];  // 1111100000(3)

        SetEvalPacking(P[0].EVAL_S4, T, kd_S10[6..], 0, 0, 0, 4);   /* 4 squares : 81 -> 45 */
        for (var j = 0; j < 81; ++j)
            P[1].EVAL_S4[j] = P[0].EVAL_S4[OPPONENT_FEATURE[j + 29484]];  // 1111110000(3)

        SetEvalPacking(P[0].EVAL_C9, T, kd_C9, 0, 0, 0, 9);    /* 9 corner squares : 19683 -> 10206 */
        for (var j = 0; j < 19683; ++j)
            P[1].EVAL_C9[j] = P[0].EVAL_C9[OPPONENT_FEATURE[j + 19683]];  // 1000000000(3)

        SetEvalPacking(P[0].EVAL_S10, T, kd_S10, 0, 0, 0, 10); /* 10 squares (edge + X) : 59049 -> 29646 */
        SetEvalPacking(P[0].EVAL_C10, T, kd_C10, 0, 0, 0, 10); /* 10 squares (angle + X) : 59049 -> 29889 */
        for (var j = 0; j < 59049; ++j)
        {
            P[1].EVAL_S10[j] = P[0].EVAL_S10[OPPONENT_FEATURE[j]];
            P[1].EVAL_C10[j] = P[0].EVAL_C10[OPPONENT_FEATURE[j]];
        }
    }

    class SymmetryPacking
    {
        public short[] EVAL_C10 = new short[59049];
        public short[] EVAL_S10 = new short[59049];
        public short[] EVAL_C9 = new short[19683];
        public short[] EVAL_S8 = new short[6561];
        public short[] EVAL_S7 = new short[2187];
        public short[] EVAL_S6 = new short[729];
        public short[] EVAL_S5 = new short[243];
        public short[] EVAL_S4 = new short[81];
    }

    static Span<ushort> SetOpponentFeature(Span<ushort> p, int o, int d)
    {
        if (--d > 0)
        {
            p = SetOpponentFeature(p, (o + 1) * 3, d);
            p = SetOpponentFeature(p, o * 3, d);
            p = SetOpponentFeature(p, (o + 2) * 3, d);
            return p;
        }
        else
        {
            p[0] = (ushort)(o + 1);
            p[1] = (ushort)o;
            p[2] = (ushort)(o + 2);
            return p[3..];
        }
    }

    static int SetEvalPacking(Span<short> pe, int[] T, ReadOnlySpan<int> kd, int l, int k, int n, int d)
    {
        int i, q0, q1, q2, q3;

        if (--d > 3)
        {
            l *= 3;
            n = SetEvalPacking(pe, T, kd, l, k, n, d);
            k += kd[d];
            n = SetEvalPacking(pe, T, kd, l + 3, k, n, d);
            k += kd[d];
            n = SetEvalPacking(pe, T, kd, l + 6, k, n, d);
        }
        else
        {
            l *= 27;
            for (q3 = 0; q3 < 3; ++q3)
            {
                for (q2 = 0; q2 < 3; ++q2)
                {
                    for (q1 = 0; q1 < 3; ++q1)
                    {
                        for (q0 = 0; q0 < 3; ++q0)
                        {
                            if (k < l) i = T[k];
                            else T[l] = i = n++;
                            pe[l++] = (short)i;
                            k += kd[0];
                        }
                        k += (kd[1] - kd[0] * 3);
                    }
                    k += (kd[2] - kd[1] * 3);
                }
                k += (kd[3] - kd[2] * 3);
            }
        }
        return n;
    }
}
