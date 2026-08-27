using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CodeX.Games.MCLA.Files
{
    //.sco - a compiled RAGE script object.
    //
    //The file on disc is a 28 byte header followed by a payload that is AES-256-ECB decrypted
    //sixteen times with the RPF3 key and then zlib inflated; Rpf3File.ExtractFileResource already
    //does that and hands us the header followed by the inflated image, so Load() only has to split
    //that image into its code and statics sections.
    //
    //The instruction set below was read out of the interpreter, scrThread::Run (sub_82554E20 in the
    //retail XEX): one byte of opcode, immediates little endian even though the console is big
    //endian, opcodes 0x50 and up being the "push a small constant" fast path (value = opcode - 0x60)
    //and 0x00 plus 0x4C-0x4F being invalid. Native commands are called by hash (sub_82557F78, the
    //RAGE filename hash of the lowercased name, bumped to 2 when it lands below that) which is
    //patched to a function pointer at load time, so a file on disc always carries the hash.
    public class ScoFile : FilePack
    {
        public const int HeaderSize = 28;

        public byte Version { get; set; }
        public uint CodeSize { get; set; }
        public uint StaticsCount { get; set; }
        public uint ParentStaticsCount { get; set; } //+16, equals StaticsCount except in derived scripts
        public uint CompressedSize { get; set; }
        public byte[] Code { get; set; }
        public uint[] Statics { get; set; }
        public List<ScoInstruction> Instructions { get; set; }

        public ScoFile()
        {
        }

        public ScoFile(GameArchiveFileInfo info) : base(info)
        {
        }

        public override void Load(byte[] data)
        {
            if (data == null || data.Length < HeaderSize)
            {
                LoadException = new Exception($"Not a script object: {data?.Length ?? 0} bytes");
                return;
            }
            if (data[0] != 'S' || data[1] != 'c' || data[2] != 'r')
            {
                LoadException = new Exception("Not a script object: missing 'Scr' magic");
                return;
            }

            Version = data[3];
            CodeSize = BitConverter.ToUInt32(data, 4);
            StaticsCount = BitConverter.ToUInt32(data, 8);
            ParentStaticsCount = BitConverter.ToUInt32(data, 16);
            CompressedSize = BitConverter.ToUInt32(data, 24);

            var payload = data.Length - HeaderSize;
            if (CodeSize > payload)
            {
                //Still packed: Rpf3File.ExtractFileResource skips scripts of 50 bytes or less, and a
                //.sco read straight off disk never went through it at all. Unpack it here instead.
                data = Unpack(data);
                if (data == null)
                {
                    LoadException = new Exception("Could not unpack the script payload");
                    return;
                }
                payload = data.Length - HeaderSize;
            }

            Code = new byte[CodeSize];
            Buffer.BlockCopy(data, HeaderSize, Code, 0, (int)CodeSize);

            var staticsBytes = payload - (int)CodeSize;
            var count = Math.Min(StaticsCount, (uint)(staticsBytes / 4));
            Statics = new uint[count];
            for (int i = 0; i < count; i++)
            {
                Statics[i] = BitConverter.ToUInt32(data, HeaderSize + (int)CodeSize + i * 4);
            }

            Instructions = ScoDisassembler.Decode(Code);
        }

        //AES-256-ECB sixteen times with the RPF3 key, then zlib. Returns the header followed by the
        //inflated image, which is the shape Load() expects.
        private static byte[] Unpack(byte[] data)
        {
            try
            {
                var packed = new byte[data.Length - HeaderSize];
                Buffer.BlockCopy(data, HeaderSize, packed, 0, packed.Length);
                var plain = Rpf3Crypto.DecryptAES(packed);
                var inflated = Rpf3Crypto.DecompressZlib(plain);
                if (inflated == null || inflated.Length == 0) return null;

                var buffer = new byte[HeaderSize + inflated.Length];
                Buffer.BlockCopy(data, 0, buffer, 0, HeaderSize);
                Buffer.BlockCopy(inflated, 0, buffer, HeaderSize, inflated.Length);
                return buffer;
            }
            catch
            {
                return null;
            }
        }

        public override byte[] Save()
        {
            throw new NotImplementedException();
        }

        public override void Read(MetaNodeReader reader)
        {
            throw new NotImplementedException();
        }

        public override void Write(MetaNodeWriter writer)
        {
            writer.WriteByte("Version", Version);
            writer.WriteUInt32("CodeSize", CodeSize);
            writer.WriteUInt32("StaticsCount", StaticsCount);
            writer.WriteUInt32("ParentStaticsCount", ParentStaticsCount);
        }

        public string ToText()
        {
            var sb = new StringBuilder();
            var name = FileInfo?.Name ?? FilePath ?? "(unnamed)";

            if (LoadException != null)
            {
                sb.AppendLine($"; {name}");
                sb.AppendLine($"; {LoadException.Message}");
                return sb.ToString();
            }

            int natives = 0, named = 0, strings = 0, funcs = 0;
            var used = new Dictionary<uint, int>();
            foreach (var ins in Instructions)
            {
                switch (ins.Op)
                {
                    case ScoOpcode.CALLNATIVE:
                        natives++;
                        used.TryGetValue(ins.NativeHash, out var n);
                        used[ins.NativeHash] = n + 1;
                        if (ScoNatives.Lookup(ins.NativeHash) != null) named++;
                        break;
                    case ScoOpcode.PUSH_STRING: strings++; break;
                    case ScoOpcode.ENTER: funcs++; break;
                }
            }

            sb.AppendLine($"; {name}");
            sb.AppendLine($"; RAGE script object, version {Version}");
            sb.AppendLine($"; code {CodeSize} bytes, statics {StaticsCount}, parent statics {ParentStaticsCount}, packed {CompressedSize} bytes");
            sb.AppendLine($"; {Instructions.Count} instructions, {funcs} functions, {strings} strings");
            sb.AppendLine($"; {natives} native calls, {used.Count} distinct, {named} named");
            sb.AppendLine();

            //Distinct native commands first - on a cutscene or mission script this list alone says
            //what the script does.
            if (used.Count > 0)
            {
                sb.AppendLine("; native commands used");
                var keys = new List<uint>(used.Keys);
                keys.Sort((a, b) =>
                {
                    var c = used[b].CompareTo(used[a]);
                    return c != 0 ? c : a.CompareTo(b);
                });
                foreach (var h in keys)
                {
                    var nm = ScoNatives.Lookup(h);
                    sb.AppendLine($";   {h:X8}  x{used[h],-5} {nm ?? "(unknown)"}");
                }
                sb.AppendLine();
            }

            sb.AppendLine(ScoDisassembler.ToText(Instructions));

            if (Statics != null && Statics.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine(".statics");
                for (int i = 0; i < Statics.Length; i++)
                {
                    if ((i % 8) == 0)
                    {
                        if (i > 0) sb.AppendLine();
                        sb.Append($"{i,6}:");
                    }
                    sb.Append($" {Statics[i]:X8}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    public enum ScoOpcode : byte
    {
        IADD = 0x01, ISUB = 0x02, IMUL = 0x03, IDIV = 0x04, IMOD = 0x05,
        NOT = 0x06, INEG = 0x07, ICMPEQ = 0x08, ICMPNE = 0x09, ICMPGT = 0x0A,
        ICMPGE = 0x0B, ICMPLT = 0x0C, ICMPLE = 0x0D, FADD = 0x0E, FSUB = 0x0F,
        FMUL = 0x10, FDIV = 0x11, FMOD = 0x12, FNEG = 0x13, FCMPEQ = 0x14,
        FCMPNE = 0x15, FCMPGT = 0x16, FCMPGE = 0x17, FCMPLT = 0x18, FCMPLE = 0x19,
        VADD = 0x1A, VSUB = 0x1B, VMUL = 0x1C, VDIV = 0x1D, VNEG = 0x1E,
        AND = 0x1F, OR = 0x20, XOR = 0x21,
        JUMP = 0x22, JUMPFALSE = 0x23, JUMPTRUE = 0x24,
        ITOF = 0x25, FTOI = 0x26, FTOV = 0x27,
        PUSH_S16 = 0x28, PUSH_S32 = 0x29, PUSH_F32 = 0x2A,
        DUP = 0x2B, DROP = 0x2C,
        CALLNATIVE = 0x2D, CALL = 0x2E, ENTER = 0x2F, RET = 0x30,
        LOAD = 0x31,      //pop pointer, push what it points at
        STORE = 0x32,     //pop pointer, pop value, write it
        STORE_P = 0x33,   //pop value, write it through the pointer left underneath
        LOAD_N = 0x34,    //pop pointer, pop count, push that many dwords
        STORE_N = 0x35,
        //Address of one of the first eight frame slots - the fast path for a local.
        LOCAL_0 = 0x36, LOCAL_1 = 0x37, LOCAL_2 = 0x38, LOCAL_3 = 0x39,
        LOCAL_4 = 0x3A, LOCAL_5 = 0x3B, LOCAL_6 = 0x3C, LOCAL_7 = 0x3D,
        LOCAL = 0x3E,     //pop index, push the address of that frame slot
        STATIC = 0x3F,    //pop index, push the address of that script static
        GLOBAL = 0x40,    //pop index, push the address of that global
        ARRAY = 0x41,     //pop array pointer, element size and index, push element address
        SWITCH = 0x42, PUSH_STRING = 0x43, PUSH_NULLSTR = 0x44,
        STRCPY = 0x45, ITOS = 0x46, STRCAT = 0x47, STRCATI = 0x48,
        CATCH = 0x49, THROW = 0x4A,
        STACK_TO_STR = 0x4B, //pop buffer, size and count, copy that many stack dwords in as a string
        PUSH_CONST = 0xFF, //synthetic: every opcode >= 0x50 pushes (opcode - 0x60)
        INVALID = 0xFE,
    }

    public class ScoInstruction
    {
        public int Offset { get; set; }
        public int Length { get; set; }
        public ScoOpcode Op { get; set; }
        public byte RawOp { get; set; }
        public int Immediate { get; set; }       //jump/call target, pushed constant, byte operand
        public float ImmediateFloat { get; set; }
        public byte ArgCount { get; set; }       //CALLNATIVE, ENTER, RET
        public byte ReturnCount { get; set; }    //CALLNATIVE, RET
        public ushort LocalCount { get; set; }   //ENTER
        public uint NativeHash { get; set; }
        public string Text { get; set; }         //PUSH_STRING
        public List<KeyValuePair<uint, int>> Cases { get; set; } //SWITCH: value -> target

        public override string ToString()
        {
            return $"{Offset:X6} {Op}";
        }
    }

    public static class ScoDisassembler
    {
        public static List<ScoInstruction> Decode(byte[] code)
        {
            var list = new List<ScoInstruction>();
            if (code == null) return list;

            int i = 0;
            while (i < code.Length)
            {
                var ins = new ScoInstruction { Offset = i, RawOp = code[i] };
                var op = code[i];

                if (op >= 0x50)
                {
                    ins.Op = ScoOpcode.PUSH_CONST;
                    ins.Immediate = op - 0x60;
                    ins.Length = 1;
                    list.Add(ins);
                    i += 1;
                    continue;
                }

                ins.Op = (ScoOpcode)op;
                switch (op)
                {
                    case 0x22: //JUMP
                    case 0x23: //JUMPFALSE
                    case 0x24: //JUMPTRUE
                    case 0x2E: //CALL
                        if (i + 5 > code.Length) goto invalid;
                        ins.Immediate = BitConverter.ToInt32(code, i + 1);
                        ins.Length = 5;
                        break;
                    case 0x28: //PUSH_S16
                        if (i + 3 > code.Length) goto invalid;
                        ins.Immediate = BitConverter.ToInt16(code, i + 1);
                        ins.Length = 3;
                        break;
                    case 0x29: //PUSH_S32
                        if (i + 5 > code.Length) goto invalid;
                        ins.Immediate = BitConverter.ToInt32(code, i + 1);
                        ins.Length = 5;
                        break;
                    case 0x2A: //PUSH_F32
                        if (i + 5 > code.Length) goto invalid;
                        ins.ImmediateFloat = BitConverter.ToSingle(code, i + 1);
                        ins.Immediate = BitConverter.ToInt32(code, i + 1);
                        ins.Length = 5;
                        break;
                    case 0x2D: //CALLNATIVE
                        if (i + 7 > code.Length) goto invalid;
                        ins.ArgCount = code[i + 1];
                        ins.ReturnCount = code[i + 2];
                        ins.NativeHash = BitConverter.ToUInt32(code, i + 3);
                        ins.Length = 7;
                        break;
                    case 0x2F: //ENTER
                        if (i + 4 > code.Length) goto invalid;
                        ins.ArgCount = code[i + 1];
                        ins.LocalCount = BitConverter.ToUInt16(code, i + 2);
                        ins.Length = 4;
                        break;
                    case 0x30: //RET
                        if (i + 3 > code.Length) goto invalid;
                        ins.ArgCount = code[i + 1];
                        ins.ReturnCount = code[i + 2];
                        ins.Length = 3;
                        break;
                    case 0x42: //SWITCH
                    {
                        if (i + 2 > code.Length) goto invalid;
                        int n = code[i + 1];
                        if (i + 2 + n * 8 > code.Length) goto invalid;
                        ins.Cases = new List<KeyValuePair<uint, int>>(n);
                        for (int c = 0; c < n; c++)
                        {
                            var val = BitConverter.ToUInt32(code, i + 2 + c * 8);
                            var target = BitConverter.ToInt32(code, i + 6 + c * 8);
                            ins.Cases.Add(new KeyValuePair<uint, int>(val, target));
                        }
                        ins.Length = 2 + n * 8;
                        break;
                    }
                    case 0x43: //PUSH_STRING
                    {
                        if (i + 2 > code.Length) goto invalid;
                        int len = code[i + 1];
                        if (i + 2 + len > code.Length) goto invalid;
                        var end = len;
                        while (end > 0 && code[i + 2 + end - 1] == 0) end--;
                        ins.Text = Encoding.ASCII.GetString(code, i + 2, end);
                        ins.Length = 2 + len;
                        break;
                    }
                    case 0x45: //STRCPY
                    case 0x46: //ITOS
                    case 0x47: //STRCAT
                    case 0x48: //STRCATI
                        if (i + 2 > code.Length) goto invalid;
                        ins.Immediate = code[i + 1];
                        ins.Length = 2;
                        break;
                    default:
                        if (!Enum.IsDefined(typeof(ScoOpcode), (ScoOpcode)op) || op == 0x00) goto invalid;
                        ins.Length = 1;
                        break;
                }

                list.Add(ins);
                i += ins.Length;
                continue;

            invalid:
                ins.Op = ScoOpcode.INVALID;
                ins.Length = 1;
                list.Add(ins);
                i += 1;
            }
            return list;
        }

        public static string ToText(List<ScoInstruction> instructions)
        {
            var labels = new HashSet<int>();
            var funcs = new HashSet<int>();
            foreach (var ins in instructions)
            {
                switch (ins.Op)
                {
                    case ScoOpcode.JUMP:
                    case ScoOpcode.JUMPFALSE:
                    case ScoOpcode.JUMPTRUE:
                        labels.Add(ins.Immediate);
                        break;
                    case ScoOpcode.CALL:
                        funcs.Add(ins.Immediate);
                        break;
                    case ScoOpcode.ENTER:
                        funcs.Add(ins.Offset);
                        break;
                    case ScoOpcode.SWITCH:
                        foreach (var c in ins.Cases) labels.Add(c.Value);
                        break;
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine(".code");
            foreach (var ins in instructions)
            {
                if (funcs.Contains(ins.Offset))
                {
                    sb.AppendLine();
                    sb.AppendLine($"func_{ins.Offset:X6}:" + (ins.Op == ScoOpcode.ENTER
                        ? $"                    ; {ins.ArgCount} args, {ins.LocalCount} locals"
                        : string.Empty));
                }
                else if (labels.Contains(ins.Offset))
                {
                    sb.AppendLine($"loc_{ins.Offset:X6}:");
                }
                sb.Append($"  {ins.Offset:X6}  ");
                sb.AppendLine(Format(ins));
            }
            return sb.ToString();
        }

        private static string Format(ScoInstruction ins)
        {
            switch (ins.Op)
            {
                case ScoOpcode.PUSH_CONST:
                    return $"{"PUSH",-14}{ins.Immediate}";
                case ScoOpcode.PUSH_S16:
                case ScoOpcode.PUSH_S32:
                    return $"{"PUSH",-14}{ins.Immediate}    ; 0x{ins.Immediate:X8}";
                case ScoOpcode.PUSH_F32:
                    return $"{"PUSHF",-14}{ins.ImmediateFloat.ToString("R", CultureInfo.InvariantCulture)}";
                case ScoOpcode.PUSH_STRING:
                    return $"{"PUSH_STRING",-14}\"{Escape(ins.Text)}\"";
                case ScoOpcode.JUMP:
                case ScoOpcode.JUMPFALSE:
                case ScoOpcode.JUMPTRUE:
                    return $"{ins.Op,-14}loc_{ins.Immediate:X6}";
                case ScoOpcode.CALL:
                    return $"{ins.Op,-14}func_{ins.Immediate:X6}";
                case ScoOpcode.ENTER:
                    return $"{ins.Op,-14}args={ins.ArgCount} locals={ins.LocalCount}";
                case ScoOpcode.RET:
                    return $"{ins.Op,-14}args={ins.ArgCount} returns={ins.ReturnCount}";
                case ScoOpcode.CALLNATIVE:
                {
                    var name = ScoNatives.Lookup(ins.NativeHash);
                    var label = name ?? $"native_{ins.NativeHash:X8}";
                    return $"{ins.Op,-14}{label}  args={ins.ArgCount} returns={ins.ReturnCount}  ; {ins.NativeHash:X8}";
                }
                case ScoOpcode.SWITCH:
                {
                    var sb = new StringBuilder();
                    sb.Append($"{ins.Op,-14}{ins.Cases.Count} cases");
                    foreach (var c in ins.Cases)
                    {
                        sb.AppendLine();
                        sb.Append($"          case {(int)c.Key,-10} -> loc_{c.Value:X6}");
                    }
                    return sb.ToString();
                }
                case ScoOpcode.STRCPY:
                case ScoOpcode.ITOS:
                case ScoOpcode.STRCAT:
                case ScoOpcode.STRCATI:
                    return $"{ins.Op,-14}max={ins.Immediate}";
                case ScoOpcode.INVALID:
                    return $"{"???",-14}{ins.RawOp:X2}";
                default:
                    return ins.Op.ToString();
            }
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                if (c == '"') sb.Append("\\\"");
                else if (c == '\\') sb.Append("\\\\");
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else if (c < 0x20 || c > 0x7E) sb.Append($"\\x{(int)c:X2}");
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
