using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using System.Text;
using CodeX.Core.Numerics;
using System.Numerics;
using EXP = System.ComponentModel.ExpandableObjectConverter;
using TC = System.ComponentModel.TypeConverterAttribute;

namespace CodeX.Games.MCLA.RSC5
{
    public class Rsc5DataReader : BlockReader
    {
        public const ulong VIRTUAL_BASE = 0x50000000;
        public const ulong PHYSICAL_BASE = 0x60000000;

        public Rpf3ResourceFileEntry FileEntry;
        public DataEndianess Endianess;
        public int VirtualSize;
        public int PhysicalSize;

        public int Offset => GetDataOffset();

        public Rsc5DataReader(Rpf3ResourceFileEntry entry, byte[] data, DataEndianess endianess = DataEndianess.LittleEndian)
        {
            FileEntry = entry;
            Endianess = endianess;
            Data = data;
            VirtualSize = entry.GetVirtualSize();
            PhysicalSize = entry.GetPhysicalSize();
            Position = 0x50000000;
        }

        public override int GetDataOffset()
        {
            if ((Position & VIRTUAL_BASE) == VIRTUAL_BASE)
            {
                return (int)(Position & 0x0FFFFFFF);
            }
            if ((Position & PHYSICAL_BASE) == PHYSICAL_BASE)
            {
                return (int)(Position & 0x1FFFFFFF) + VirtualSize;
            }
            throw new Exception("Invalid Position. Possibly the file is corrupted.");
        }

        public bool ReadBoolean()
        {
            return ReadByte() > 0;
        }

        public new byte[] ReadBytes(int count)
        {
            int dataOffset = GetDataOffset();
            byte[] dst = new byte[count];
            Buffer.BlockCopy(Data, dataOffset, dst, 0, count);
            Position += (ulong)count;
            return dst;
        }

        //The padded size of the last texture in a segment can run past the end of the data.
        //Reading it as far as the data goes and zero filling the rest keeps the caller's size
        //assumptions intact instead of throwing.
        public byte[] ReadBytesPadded(int count)
        {
            int dataOffset = GetDataOffset();
            var dst = new byte[count];
            var available = Math.Min(count, Data.Length - dataOffset);

            if (available > 0)
            {
                Buffer.BlockCopy(Data, dataOffset, dst, 0, available);
            }
            Position += (ulong)count;
            return dst;
        }

        public new byte[] ReadBytesReversed(int count)
        {
            var numArray = ReadBytes(count);
            Array.Reverse((Array)numArray);
            return numArray;
        }

        public new byte[] ReadBytes(ulong ptr, int count)
        {
            var position = Position;
            Position = ptr;

            var numArray = ReadBytes(count);
            Position = position;
            return numArray;
        }

        public new byte ReadByte()
        {
            var num = Data[GetDataOffset()];
            ++Position;
            return num;
        }

        public new short ReadInt16()
        {
            var num = BufferUtil.ReadShort(this.Data, this.GetDataOffset());
            Position += 2UL;

            var bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToInt16(bytes, 0);
        }

        public new ushort ReadUInt16()
        {
            var num = BufferUtil.ReadUshort(Data, GetDataOffset());
            Position += 2UL;

            var bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public new int ReadInt32()
        {
            var num = BufferUtil.ReadInt(Data, GetDataOffset());
            Position += 4UL;

            var bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public new uint ReadUInt32()
        {
            var num = BufferUtil.ReadUint(Data, GetDataOffset());
            Position += 4UL;

            var bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public new long ReadInt64()
        {
            var num = BufferUtil.ReadLong(Data, GetDataOffset());
            Position += 8UL;

            var bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public new ulong ReadUInt64()
        {
            var num = BufferUtil.ReadUlong(Data, GetDataOffset());
            Position += 8UL;

            var bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public new float ReadSingle()
        {
            var num = BufferUtil.ReadSingle(Data, GetDataOffset());
            Position += 4UL;

            var bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        public new double ReadDouble()
        {
            var num = BufferUtil.ReadDouble(Data, GetDataOffset());
            Position += 4UL;
            return num;
        }

        public string ReadString()
        {
            var byteList = new List<byte>();
            var dataOffset = GetDataOffset();
            var num1 = Data[dataOffset];
            uint num2 = 1;

            while (num1 > 0)
            {
                byteList.Add(num1);
                num1 = Data[dataOffset + num2];
                ++num2;
            }
            Position += num2;
            return Encoding.UTF8.GetString(byteList.ToArray());
        }

        public new Vector2 ReadVector2()
        {
            var vector = BufferUtil.ReadVector2(Data, GetDataOffset());
            Position += 8UL;
            return Rpf3Crypto.Swap(vector);
        }

        public Vector2[] ReadVector2Arr(int count)
        {
            var vector2Array = new Vector2[count];
            for (int index = 0; index < count; ++index)
            {
                vector2Array[index] = ReadVector2();
            }
            return vector2Array;
        }

        public Vector3 ReadVector3(bool toZXY = true)
        {
            var vector = BufferUtil.ReadVector3(Data, GetDataOffset());
            vector = Rpf3Crypto.Swap(vector);
            Position += 12;
            return toZXY ? new Vector3(vector.Z, vector.X, vector.Y) : vector;
        }

        public Vector3[] ReadVector3Arr(int count, bool toZXY = true)
        {
            var vectors = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                vectors[i] = ReadVector3(toZXY);
            }
            return vectors;
        }

        public Vector4 ReadVector4(bool toZXYW = true)
        {
            var vector = BufferUtil.ReadVector4(Data, GetDataOffset());
            vector = Rpf3Crypto.Swap(vector);
            Position += 16;

            if (float.IsNaN(vector.W))
            {
                vector = new Vector4(vector.XYZ(), 0.0f);
            }
            return toZXYW ? new Vector4(vector.Z, vector.X, vector.Y, vector.W) : vector;
        }

        public Vector4[] ReadVector4Arr(int count, bool toZXYW = true)
        {
            var vectors = new Vector4[count];
            for (int i = 0; i < count; i++)
            {
                vectors[i] = ReadVector4(toZXYW);
            }
            return vectors;
        }

        public new Matrix4x4 ReadMatrix4x4()
        {
            var matrix = BufferUtil.ReadMatrix4x4(Data, GetDataOffset());
            matrix = Rpf3Crypto.Swap(matrix);
            Position += 64;

            if (float.IsNaN(matrix.M14))
                matrix.M14 = 0.0f;
            if (float.IsNaN(matrix.M24))
                matrix.M24 = 0.0f;
            if (float.IsNaN(matrix.M34))
                matrix.M34 = 0.0f;
            if (float.IsNaN(matrix.M44))
                matrix.M44 = 0.0f;
            return Rpf3Crypto.ToZXY(matrix);
        }

        public BoundingBox4 ReadBoundingBox4()
        {
            var bb = new BoundingBox4
            {
                Min = ReadVector4(),
                Max = ReadVector4()
            };
            Position += 32;
            return bb;
        }

        public ushort[] ReadUInt16Arr(int count)
        {
            ushort[] numArray = new ushort[count];
            for (int index = 0; index < count; ++index)
            {
                numArray[index] = Rpf3Crypto.Swap(BufferUtil.ReadUshort(this.Data, this.GetDataOffset()));
                Position += 2UL;
            }
            return numArray;
        }

        public uint[] ReadUInt32Arr(int count)
        {
            uint[] numArray = new uint[count];
            for (int index = 0; index < count; ++index)
            {
                numArray[index] = Rpf3Crypto.Swap(BufferUtil.ReadUint(this.Data, this.GetDataOffset()));
                Position += 4UL;
            }
            return numArray;
        }

        public T ReadBlock<T>(Func<Rsc5DataReader, T> createFunc = null) where T : IRsc5Block
        {
            if (Position == 0) return default;
            if (BlockPool.TryGetValue(Position, out var exitem))
            {
                if (exitem is T exblock)
                {
                    return exblock;
                }
            }
            var block = (createFunc != null) ? createFunc(this) : (T)Activator.CreateInstance(typeof(T), nonPublic: true);
            BlockPool[Position] = block;
            block.FilePosition = Position;
            block.Read(this);
            return block;
        }

        public T ReadBlock<T>(ulong position, Func<Rsc5DataReader, T> createFunc = null) where T : IRsc5Block
        {
            if (position == 0 || position == 0xCDCDCDCD)
            {
                return default;
            }
            var p = Position;
            Position = position;
            var b = ReadBlock(createFunc);
            Position = p;
            return b;
        }

        public Rsc5Ptr<T> ReadPtr<T>(Func<Rsc5DataReader, T> createFunc = null) where T : IRsc5Block, new()
        {
            var ptr = new Rsc5Ptr<T>();
            ptr.Read(this, createFunc);
            return ptr;
        }

        public Rsc5PtrArr<T> ReadPtrArr<T>(Func<Rsc5DataReader, T> createFunc = null) where T : IRsc5Block, new()
        {
            var arr = new Rsc5PtrArr<T>();
            arr.Read(this, createFunc);
            return arr;
        }

        public Rsc5Arr<T> ReadArr<T>(bool size64 = false) where T : unmanaged
        {
            var arr = new Rsc5Arr<T>();
            arr.Read(this, size64);
            return arr;
        }

        public Rsc5ManagedArr<T> ReadArr<T>(Func<Rsc5DataReader, T> createFunc = null) where T : IRsc5Block, new()
        {
            var arr = new Rsc5ManagedArr<T>();
            arr.Read(this, createFunc);
            return arr;
        }

        public Rsc5RawArr<T> ReadRawArrPtr<T>(int virtualSize = -1) where T : unmanaged
        {
            var arr = new Rsc5RawArr<T>();
            arr.ReadPtr(this);

            if (virtualSize != -1)
                arr.Position += (ulong)virtualSize;

            return arr;
        }

        public Rsc5RawArr<T> ReadRawArrItems<T>(Rsc5RawArr<T> arr, uint count) where T : unmanaged
        {
            arr.ReadItems(this, count);
            return arr;
        }

        public Rsc5RawLst<T> ReadRawLstPtr<T>() where T : IRsc5Block, new()
        {
            var arr = new Rsc5RawLst<T>();
            arr.ReadPtr(this);
            return arr;
        }

        public Rsc5RawLst<T> ReadRawLstItems<T>(Rsc5RawLst<T> arr, uint count, Func<Rsc5DataReader, T> createFunc = null) where T : IRsc5Block, new()
        {
            arr.ReadItems(this, count, createFunc);
            return arr;
        }

        public Rsc5Ptr<T> ReadPtrPtr<T>() where T : IRsc5Block, new()
        {
            var ptr = new Rsc5Ptr<T>();
            ptr.ReadPtr(this);
            return ptr;
        }

        public Rsc5Ptr<T> ReadPtrItem<T>(Rsc5Ptr<T> ptr, Func<Rsc5DataReader, T> createFunc = null) where T : IRsc5Block, new()
        {
            ptr.ReadItem(this, createFunc);
            return ptr;
        }

        public Rsc5Str ReadStr()
        {
            var str = new Rsc5Str();
            str.Read(this);
            return str;
        }

        public Rsc5StrA ReadStrA()
        {
            var str = new Rsc5StrA();
            str.Read(this);
            return str;
        }

        public static BlockAnalyzer Analyze<T>(Rpf3ResourceFileEntry rfe, byte[] data, Func<Rsc5DataReader, T> createFunc = null) where T : IRsc5Block, new()
        {
            var r = new Rsc5DataReader(rfe, data);
            var block = r.ReadBlock(createFunc);
            var analyzer = new BlockAnalyzer(r, rfe);
            return analyzer;
        }
    }

    public class Rsc5DataWriter : BlockWriter //TODO: Make everything big-endian
    {
        public HashSet<object> PhysicalBlocks = [];

        protected override ulong GetPointer(BuilderBlock block)
        {
            if (block == null) return 0;
            if (block.Block == null) return 0;
            if (PhysicalBlocks.Contains(block.Block))
            {
                return 0x60000000 + block.Position;
            }
            return 0x50000000 + block.Position;
        }

        public void WriteBlock<T>(T block) where T : IRsc5Block
        {
            if (block == null)
                return;

            var exdata = Data;
            var expos = Position;
            var size = block.BlockLength;

            Data = new byte[size];
            Position = 0;

            AddBlock(block, Data);
            block.Write(this);

            if (block.IsPhysical)
            {
                PhysicalBlocks.Add(block);
            }
            Data = exdata;
            Position = expos;
        }

        public void WriteBlocks<T>(T[] blocks) where T : IRsc5Block, new()
        {
            if (blocks == null)
                return;
            if (blocks.Length == 0)
                return;

            var b0 = blocks[0];
            var bs = (int)(b0?.BlockLength ?? 0);

            if (bs == 0)
                return;

            var exdata = Data;
            var expos = Position;
            var size = blocks.Length * bs;

            this.Data = new byte[size];
            this.Position = 0;
            this.AddBlock(blocks, Data);

            if (b0.IsPhysical)
            {
                this.PhysicalBlocks.Add(blocks);
            }

            for (int i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];
                if (block == null) continue;
                this.Position = (ulong)(i * bs);
                block.Write(this);
            }
            this.Data = exdata;
            this.Position = expos;
        }

        public void WriteBoolean(bool value)
        {
            WriteByte(value ? (byte)1 : (byte)0);
        }

        public void WriteStr(Rsc5Str str)
        {
            str.Write(this);
        }

        public void WriteStrA(Rsc5StrA str)
        {
            str.Write(this);
        }

        public void WriteArr<T>(Rsc5Arr<T> arr) where T : unmanaged
        {
            arr.Write(this);
        }

        public void WritePtr<T>(Rsc5Ptr<T> ptr) where T : IRsc5Block, new()
        {
            ptr.Write(this);
        }

        public void WritePtrArr<T>(Rsc5PtrArr<T> ptr) where T : IRsc5Block, new()
        {
            ptr.Write(this);
        }

        public void WriteRawArr<T>(Rsc5RawArr<T> ptr) where T : unmanaged
        {
            ptr.Write(this);
        }

        public void WriteRawLst<T>(Rsc5RawLst<T> lst) where T : IRsc5Block, new()
        {
            lst.Write(this);
        }

        public void WritePtrEmbed(object target, object owner, ulong offset)
        {
            //Target is the object this is a pointer to - will be 0 pointer if target is null
            //Owner is the object that the final pointer will be offset from
            //Offset is added to the owner's pointer to get the final pointer.
            if (target != null)
            {
                AddPointerRef(owner, offset);
            }
            WriteUInt32(0); //This data will get updated later if the object isn't null
        }
    }

    [TC(typeof(EXP))] public struct Rsc5Ptr<T> where T : IRsc5Block
    {
        public ulong Position;
        public T Item;

        public void ReadPtr(Rsc5DataReader reader)
        {
            Position = reader.ReadUInt32();
        }

        public void Read(Rsc5DataReader reader, Func<Rsc5DataReader, T> createFunc = null)
        {
            Position = reader.ReadUInt32();
            Item = reader.ReadBlock(Position, createFunc);
        }

        public void ReadItem(Rsc5DataReader reader, Func<Rsc5DataReader, T> createFunc = null)
        {
            Item = reader.ReadBlock(Position, createFunc);
        }

        public readonly void Write(Rsc5DataWriter writer)
        {
            writer.AddPointerRef(Item);
            writer.WriteUInt32((uint)Position);
            writer.WriteBlock(Item);
        }

        public override readonly string ToString()
        {
            return Item?.ToString() ?? Position.ToString();
        }
    }

    [TC(typeof(EXP))] public struct Rsc5Arr<T> where T : unmanaged
    {
        public uint Position;
        public uint Count;
        public uint Capacity;
        public T[] Items;

        public void Read(Rsc5DataReader reader, bool size64 = false)
        {
            Position = reader.ReadUInt32();
            Count = size64 ? reader.ReadUInt32() : reader.ReadUInt16();
            Capacity = size64 ? reader.ReadUInt32() : reader.ReadUInt16();

            var p = reader.Position;
            reader.Position = Position;

            Items = reader.ReadArray<T>(Count);
            Rpf3Crypto.SwapEndianness(Items);
            Rpf3Crypto.TransformToZXY(Items);

            reader.Position = p;
        }

        public readonly void Write(Rsc5DataWriter writer)
        {
        }

        public readonly T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public override readonly string ToString()
        {
            return "Count: " + Count.ToString();
        }
    }

    [TC(typeof(EXP))] public struct Rsc5RawArr<T>(T[] items) where T : unmanaged
    {
        public ulong Position { get; set; } = 0;
        public T[] Items { get; set; } = items;

        public void ReadPtr(Rsc5DataReader reader)
        {
            Position = reader.ReadUInt32();
        }

        public void ReadItems(Rsc5DataReader reader, uint count)
        {
            var p = reader.Position;
            reader.Position = Position;

            Items = reader.ReadArray<T>(count);
            Rpf3Crypto.SwapEndianness(Items);
            Rpf3Crypto.TransformToZXY(Items);

            reader.Position = p;
        }

        public readonly void Write(Rsc5DataWriter writer)
        {
            writer.AddPointerRef(Items);
            writer.WriteUInt32((uint)Position);
            writer.WriteArray(Items);
        }

        public readonly T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public override readonly string ToString()
        {
            return "Count: " + (Items?.Length.ToString() ?? "0");
        }
    }

    [TC(typeof(EXP))] public struct Rsc5RawLst<T>(T[] items) where T : IRsc5Block, new()
    {
        public uint Position { get; set; } = 0;
        public T[] Items { get; set; } = items;

        public void ReadPtr(Rsc5DataReader reader)
        {
            Position = reader.ReadUInt32();
        }

        public void ReadItems(Rsc5DataReader reader, uint count, Func<Rsc5DataReader, T> createFunc = null)
        {
            if (Position != 0)
            {
                var p = reader.Position;
                reader.Position = Position;
                Items = new T[count];

                for (int i = 0; i < count; i++)
                {
                    Items[i] = reader.ReadBlock(createFunc);
                }
                reader.Position = p;
            }
        }

        public readonly void Write(Rsc5DataWriter writer)
        {
            writer.AddPointerRef(Items);
            writer.WriteUInt32(Position);
            writer.WriteBlocks(Items);
        }

        public readonly T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public override readonly string ToString()
        {
            return "Count: " + (Items?.Length.ToString() ?? "0");
        }
    }

    [TC(typeof(EXP))] public struct Rsc5ManagedArr<T> where T : IRsc5Block, new()
    {
        public uint Position { get; set; }
        public ushort Count { get; set; }
        public ushort Capacity { get; set; }
        public T[] Items { get; set; }

        public Rsc5ManagedArr(T[] items)
        {
            Position = 0;
            Items = items;
            Count = (ushort)(items?.Length ?? 0);
            Capacity = Count;
        }

        public void Read(Rsc5DataReader reader, Func<Rsc5DataReader, T> createFunc = null)
        {
            Position = reader.ReadUInt32();
            Count = reader.ReadUInt16();
            Capacity = reader.ReadUInt16();

            var p = reader.Position;
            reader.Position = Position;
            Items = new T[Count];

            for (int i = 0; i < Count; i++)
            {
                Items[i] = reader.ReadBlock(createFunc);
            }
            reader.Position = p;
        }

        public readonly void Write(Rsc5DataWriter writer)
        {
            writer.AddPointerRef(Items);
            writer.WriteUInt32(Position);
            writer.WriteUInt16(Count);
            writer.WriteUInt16(Capacity);
            writer.WriteBlocks(Items);
        }

        public readonly T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public override readonly string ToString()
        {
            return "Count: " + Count.ToString();
        }
    }

    [TC(typeof(EXP))] public struct Rsc5PtrArr<T> where T : IRsc5Block, new()
    {
        public uint Position;
        public ushort Count;
        public ushort Capacity;
        public uint[] Pointers;
        public T[] Items;

        public void Read(Rsc5DataReader reader, Func<Rsc5DataReader, T> createFunc = null)
        {
            Position = reader.ReadUInt32();
            Count = reader.ReadUInt16();
            Capacity = reader.ReadUInt16();

            var p = reader.Position;
            reader.Position = Position;
            Pointers = reader.ReadArray<uint>(Count);
            Items = new T[Count];

            for (int i = 0; i < Count; i++)
            {
                byte[] buffer = BitConverter.GetBytes(Pointers[i]);
                Array.Reverse((Array)buffer);
                Items[i] = reader.ReadBlock(BitConverter.ToUInt32(buffer, 0), createFunc);
            }
            reader.Position = p;
        }

        public readonly void Write(Rsc5DataWriter writer)
        {
        }

        public readonly T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public override readonly string ToString()
        {
            return "Count: " + Count.ToString();
        }
    }

    [TC(typeof(EXP))] public struct Rsc5Str
    {
        public ulong Position { get; set; }
        public string Value { get; set; }

        public Rsc5Str(string str)
        {
            Position = 0;
            Value = str;
        }

        public Rsc5Str(ulong pos)
        {
            Position = pos;
        }

        public void Read(Rsc5DataReader reader)
        {
            Position = reader.ReadUInt32();
            if (Position != 0)
            {
                var blockexists = reader.BlockPool.TryGetValue(Position, out var exblock);
                if (blockexists && (exblock is string str))
                {
                    Value = str;
                    return;
                }

                var p = reader.Position;
                reader.Position = Position;
                Value = reader.ReadString();
                reader.Position = p;

                if (blockexists == false)
                {
                    reader.BlockPool[Position] = Value;
                }
            }
        }

        public readonly void Write(Rsc5DataWriter writer)
        {
            writer.AddPointerRef(Value);
            writer.WriteUInt32((uint)Position);

            if (Value != null)
            {
                var encoding = Encoding.UTF8;
                var b = encoding.GetBytes(Value);
                var len = b.Length + 1;
                var data = new byte[len];

                if (b != null)
                {
                    Buffer.BlockCopy(b, 0, data, 0, b.Length);
                }
                writer.AddBlock(Value, data);
            }
        }

        public override readonly string ToString()
        {
            return Value ?? "NULL";
        }
    }

    [TC(typeof(EXP))] public struct Rsc5StrA(string str, ushort capacity)
    {
        public uint Position { get; set; } = 0;
        public ushort Length { get; set; } = (ushort)(str?.Length ?? 0);
        public ushort Capacity { get; set; } = capacity;
        public string Value { get; set; } = str;

        public void Read(Rsc5DataReader reader)
        {
            Position = reader.ReadUInt32();
            Length = reader.ReadUInt16();
            Capacity = reader.ReadUInt16();

            if (Position != 0)
            {
                var blockexists = reader.BlockPool.TryGetValue(Position, out var exblock);
                if (blockexists && (exblock is string str))
                {
                    Value = str;
                    return;
                }

                var p = reader.Position;
                reader.Position = Position;

                Value = reader.ReadStringFixedLength(Length);
                reader.Position = p;

                if (!blockexists)
                {
                    reader.BlockPool[Position] = Value;
                }
            }
        }

        public readonly void Write(Rsc5DataWriter writer)
        {
            writer.AddPointerRef(Value);
            writer.WriteUInt32(Position);
            writer.WriteUInt16(Length);
            writer.WriteUInt16(Capacity);

            if (Value != null)
            {
                var encoding = Encoding.UTF8;
                var b = encoding.GetBytes(Value);
                var len = b.Length + 1;
                var data = new byte[len];

                if (b != null)
                {
                    Buffer.BlockCopy(b, 0, data, 0, b.Length);
                }
                writer.AddBlock(Value, data);
            }
        }

        public override readonly string ToString()
        {
            return Value;
        }
    }

    [TC(typeof(EXP))] public class Rsc5String : Rsc5BlockBase
    {
        public override ulong BlockLength => 8;
        public uint Position;
        public string Value;
        public uint FixedLength;

        public Rsc5String()
        {
        }

        public Rsc5String(uint fixedLength)
        {
            FixedLength = fixedLength;
        }

        public override void Read(Rsc5DataReader reader)
        {
            Position = reader.ReadUInt32();
            if (Position != 0)
            {
                var p = reader.Position;
                reader.Position = Position;

                if (FixedLength != 0)
                    Value = Encoding.ASCII.GetString(reader.ReadArray<byte>(40, false)).Trim('\0');
                else
                    Value = reader.ReadString();
                reader.Position = p;
            }
        }

        public override void Write(Rsc5DataWriter writer)
        {
        }

        public override string ToString()
        {
            return Value;
        }
    }

    [TC(typeof(EXP))] public class Rsc5StringA : IRsc5Block
    {
        public ulong BlockLength => 8;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public Rsc5StrA String { get; set; }

        public void Read(Rsc5DataReader reader)
        {
            String = reader.ReadStrA();
        }

        public void Write(Rsc5DataWriter writer)
        {
            writer.WriteStrA(String);
        }

        public override string ToString()
        {
            return String.ToString();
        }
    }

    [TC(typeof(EXP))] public abstract class Rsc5BlockBase : IRsc5Block
    {
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public abstract ulong BlockLength { get; }
        public abstract void Read(Rsc5DataReader reader);
        public abstract void Write(Rsc5DataWriter writer);
    }

    [TC(typeof(EXP))] public class Rsc5BlockMap : Rsc5BlockBase
    {
        public override ulong BlockLength => 4;
        public uint Block { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            Block = reader.ReadUInt32();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            writer.WriteUInt32(Block);
        }
    }

    [TC(typeof(EXP))] public abstract class Rsc5FileBase : Rsc5BlockBase
    {
        public abstract uint VFT { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            writer.WriteUInt32(VFT);
        }
    }

    [TC(typeof(EXP))] public abstract class Rsc5BlockBaseMap : Rsc5FileBase //rage::pgBase
    {
        public Rsc5Ptr<Rsc5BlockMap> BlockMap { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            BlockMap = reader.ReadPtr<Rsc5BlockMap>();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            base.Write(writer);
            writer.WritePtr(BlockMap);
        }
    }

    [TC(typeof(EXP))] public abstract class Rsc5BlockBaseMapRef : Rsc5BlockBaseMap //rage::pgBaseRefCounted
    {
        public uint RefCount { get; set; } //m_RefCount

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            RefCount = reader.ReadUInt32();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            base.Write(writer);
            writer.WriteUInt32(RefCount);
        }

        public override string ToString()
        {
            return "References: " + RefCount.ToString();
        }
    }

    [TC(typeof(EXP))]  public interface IRsc5Block : BlockBase
    {
        bool IsPhysical { get; }
        void Read(Rsc5DataReader reader);
        void Write(Rsc5DataWriter writer);
    }
}
