using System.Text;
using System.Text.RegularExpressions;

namespace SarcLibrary
{
    public static partial class SarcHelper
    {
        internal static readonly byte[] SARC = { 0x53, 0x41, 0x52, 0x43 };
        internal static readonly byte[] SFAT = { 0x53, 0x46, 0x41, 0x54 };
        internal static readonly byte[] SFNT = { 0x53, 0x46, 0x4E, 0x54 };
        internal static readonly byte[] FLIM = { 0x46, 0x4C, 0x49, 0x4D };
        internal static readonly byte[] YAZ0 = { 0x59, 0x41, 0x5A, 0x30 };

        internal const string Null = "\x00";
        internal const string Empty = "";
        internal const string BFFNT = "bffnt";

        internal const uint HashKey = 0x65;
        internal const int MinAlignment = 4;

        private static readonly Regex RegexAZ = CompiledRegexAZ();

        private static readonly Dictionary<string, string> FileExtensions = new() {
            { "AAHS", ".sharc" }, { "AAMP", ".aamp" }, { "BAHS", ".sharcb" },
            { "BNSH", ".bnsh" }, { "BNTX", ".bntx" }, { "BY", ".byaml" },
            { "CFNT", ".bcfnt" }, { "CGFX", ".bcres" }, { "CLAN", ".bclan" },
            { "CLYT", ".bclyt" }, { "CSTM", ".bcstm" }, { "CTPK", ".ctpk" },
            { "CWAV", ".bcwav" }, { "FFNT", ".bffnt" }, { "FLAN", ".bflan" },
            { "FLIM", ".bclim" }, { "FLYT", ".bflyt" }, { "FRES", ".bfres" },
            { "FSEQ", ".bfseq" }, { "FSHA", ".bfsha" }, { "FSTM", ".bfstm" },
            { "FWAV", ".bfwav" }, { "Gfx2", ".gtx" }, { "MsgPrjBn", ".msbp" },
            { "MsgStdBn", ".msbt" }, { "SARC", ".sarc" }, { "STM", ".bfsha" },
            { "VFXB", ".pctl" }, { "Yaz", ".szs" }, { "YB", ".byaml" },
        };

        private static readonly Dictionary<string, int> FileAlignments = new() {
            { "aglatex", 8 }, { "baglatex", 8 }, { "aglblm", 8 }, { "baglblm", 8 },
            { "aglccr", 8 }, { "baglccr", 8 }, { "aglclwd", 8 }, { "baglclwd", 8 },
            { "aglcube", 8 }, { "baglcube", 8 }, { "agldof", 8 }, { "bagldof", 8 },
            { "aglenv", 8 }, { "baglenv", 8 }, { "aglenvset", 8 }, { "baglenvset", 8 },
            { "aglfila", 8 }, { "baglfila", 8 }, { "agllmap", 8 }, { "bagllmap", 8 },
            { "agllref", 8 }, { "bagllref", 8 }, { "aglshpp", 8 }, { "baglshpp", 8 },
            { "glght", 8 }, { "bglght", 8 }, { "glpbd", 8 }, { "bglpbd", 8 },
            { "glpbm", 8 }, { "bglpbm", 8 }, { "gsdw", 8 }, { "bgsdw", 8 },
            { "ksky", 8 }, { "bksky", 8 }, { "ofx", -8192 }, { "bofx", -8192 },
            { "pref", 8 }, { "bpref", 8 }, { "sharc", 0x1000 }, { "sharcb", 0x1000 },
            { "baglmf", 0x80 }, { "fmd", -8192 }, { "ftx", -8192 }, { "genvres", -8192 },
            { "gtx", 0x2000 }
        };

        private static readonly string[] BotwFactoryNames = {
            "sarc", "bfres", "bcamanim", "batpl, bnfprl", "bplacement",
            "hks, lua", "bactcapt", "bitemico", "jpg", "bmaptex",
            "bstftex", "bgdata", "bgsvdata", "hknm2", "bmscdef", "bars",
            "bxml", "bgparamlist", "bmodellist", "baslist", "baiprog", "bphysics",
            "bchemical", "bas", "batcllist", "batcl", "baischedule", "bdmgparam",
            "brgconfiglist", "brgconfig", "brgbw", "bawareness", "bdrop", "bshop",
            "brecipe", "blod", "bbonectrl", "blifecondition", "bumii", "baniminfo",
            "byaml", "bassetting", "hkrb", "hkrg", "bphyssb", "hkcl", "hksc",
            "hktmrb", "brgcon", "esetlist", "bdemo", "bfevfl", "bfevtm"
        };

        public static string GuessFileExtension(ReadOnlySpan<byte> data)
        {
            string magic = data[0..8].SequenceEqual(YAZ0) ? Encoding.UTF8.GetString(data[0x11..0x15]) : Encoding.UTF8.GetString(data[0..8]);
            return FileExtensions.TryGetValue(RegexAZ.Replace(magic, Empty), out string? value) ? value : "bin";
        }

        public static int GetBinaryFileAlignment(ReadOnlySpan<byte> data)
        {
            if (data.Length <= 0x20) {
                return 1;
            }

            Endian endian = (Endian)BitConverter.ToInt16(data[0x0C..0x0E]);

            int fileSize = BitConverter.ToInt32(data[0x1C..0x20]).AsInt32(endian);
            if (fileSize != data.Length) {
                return 1;
            }

            return 1 << data[0x0E];
        }

        public static int GetCafeBflimAlignment(ReadOnlySpan<byte> data)
        {
            if (data.Length <= 0x28 || !data[^0x28..^0x24].SequenceEqual(FLIM)) {
                return 1;
            }

            Endian endian = (Endian)BitConverter.ToInt16(data[^0x24..^0x22]);
            return BitConverter.ToInt16(data[^0x8..^0x06]).AsInt32(endian);
        }

        public static int GetFileAlignment(string name, ReadOnlySpan<byte> data, SarcFile sarc)
        {
            int alignment = MinAlignment;
            string ext = Path.GetExtension(name).Remove(0, 1);

            if (ext == BFFNT) {
                alignment = LCM(alignment, sarc.Endian == Endian.Big ? 0x2000 : 0x1000);
            }
            else if (FileAlignments.TryGetValue(ext, out int fetchedAlignment)) {
                alignment = LCM(alignment, fetchedAlignment);
            }

            if (sarc.Legacy && (data[0x00..0x04].SequenceEqual(SARC) || data[0x00..0x04].SequenceEqual(YAZ0) && data[0x11..0x15].SequenceEqual(SARC))) {
                alignment = LCM(alignment, 0x2000);
            }

            if (sarc.Legacy || Array.IndexOf(BotwFactoryNames, ext) == -1) {
                alignment = LCM(alignment, GetBinaryFileAlignment(data));
                if (sarc.Endian == Endian.Big) {
                    alignment = LCM(alignment, GetCafeBflimAlignment(data));
                }
            }

            return alignment;
        }

        public static int GCD(int a, int b)
        {
            while (a != 0 && b != 0) {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a | b;
        }

        public static int LCM(int a, int b)
        {
            return a / GCD(a, b) * b;
        }

        public static uint GetHash(ReadOnlySpan<char> name)
        {
            long hash = 0;
            for (int i = 0; i < name.Length; i++) {
                hash = hash * HashKey + (sbyte)name[i];
            }

            return (uint)hash;
        }

        public static void SwapEndian(ref this int value)
        {
            value = (int)(((value & 0x000000FF) << 24) + ((value & 0x0000FF00) << 8) + ((value & 0x00FF0000) >> 8) + ((value & 0xFF000000) >> 24));
        }

        public static void SwapEndian(ref this uint value)
        {
            value = ((value & 0x000000FF) << 24) + ((value & 0x0000FF00) << 8) + ((value & 0x00FF0000) >> 8) + ((value & 0xFF000000) >> 24);
        }

        public static void SwapEndian(ref this ushort value)
        {
            value = (ushort)(((value & 0x00FF) << 8) + ((value & 0xFF00) >> 8));
        }

        public static int AsInt32(this short value, Endian endian)
        {
            return endian == Endian.Big ? ((value & 0x00FF) << 8) + ((value & 0xFF00) >> 8) : value;
        }

        public static int AsInt32(this int value, Endian endian)
        {
            return endian == Endian.Big ? (int)(((value & 0x000000FF) << 24) + ((value & 0x0000FF00) << 8) + ((value & 0x00FF0000) >> 8) + ((value & 0xFF000000) >> 24)) : value;
        }

        public static uint AsUInt32(this uint value, Endian endian)
        {
            return endian == Endian.Big ? ((value & 0x000000FF) << 24) + ((value & 0x0000FF00) << 8) + ((value & 0x00FF0000) >> 8) + ((value & 0xFF000000) >> 24) : value;
        }

        public static ushort AsInt16(this int value, Endian endian)
        {
            return (ushort)(endian == Endian.Big ? ((value & 0x00FF) << 8) + ((value & 0xFF00) >> 8) : value);
        }

        public static void Align(this Stream stream, int alignment)
        {
            byte[] buffer = new byte[(alignment - (int)stream.Position % alignment) % alignment];
            stream.Write(buffer);
        }

        public static int Align(this int value, int alignment)
        {
            return value + ((alignment - value % alignment) % alignment);
        }

        [GeneratedRegex("[^a-zA-Z0-9 -]")]
        private static partial Regex CompiledRegexAZ();
    }
}
