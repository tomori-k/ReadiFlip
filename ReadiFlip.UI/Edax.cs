using System.Buffers.Binary;

namespace ReadiFlip.Edax;

// https://github.com/abulmo/edax-reversi/blob/master/src/eval.c

public class Eval
{
    public const int NUM_FEATURE = 47;
    public const int NUM_PLY = 54;

    readonly ushort[] feature = new ushort[48];
    readonly int numEmpties;
    readonly uint parity;
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
public class EvalWeight
{
    public short S0;
    public short[] C9 = new short[19683];
    public short[] C10 = new short[59049];
    public short[] S100 = new short[59049];
    public short[] S101 = new short[59049];
    public short[] S8x4 = new short[6561 * 4];
    public short[] S7654 = new short[2187 + 729 + 243 + 81];
}

public static class Edax
{
    const uint EDAX = 0x45444158;
    const uint EVAL = 0x4556414c;
    const uint LAVE = 0x4c415645;
    const uint XADE = 0x58414445;
    const int NUM_WEIGHTS = 114364;

    static readonly int[] EVAL_PACKED_OFS = [0, 10206, 40095, 69741, 99387, 102708, 106029, 109350, 112671, 113805, 114183, 114318, 114363];
    static readonly SymmetryPacking[] P;

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

    static Edax()
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

    public static void ReadEval(BinaryReader reader)
    {
        var header = ReadHeader(reader);
        var weights = ReadWeights(reader, header.Edax);

        Console.WriteLine("Read eval!!");
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
        var weights = new EvalWeight[Eval.NUM_PLY - 2];

        for (var ply = 0; ply < Eval.NUM_PLY; ++ply)
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
}
