using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using System.Text;
using CodeX.Core.Numerics;
using System.Numerics;

namespace CodeX.Games.MCLA.RSC5
{
    public class Rsc5DataReader : BlockReader
    {
        public Rpf3ResourceFileEntry FileEntry;
        public DataEndianess Endianess;
        public int VirtualSize;
        public int PhysicalSize;

        private const ulong VIRTUAL_BASE = 0x50000000;
        private const ulong PHYSICAL_BASE = 0x60000000;
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

        public new byte[] ReadBytes(int count)
        {
            int dataOffset = GetDataOffset();
            byte[] dst = new byte[count];
            Buffer.BlockCopy(Data, dataOffset, dst, 0, count);
            Position += (ulong)count;
            return dst;
        }

        public new byte[] ReadBytesReversed(int count)
        {
            byte[] numArray = ReadBytes(count);
            Array.Reverse((Array)numArray);
            return numArray;
        }

        public new byte[] ReadBytes(ulong ptr, int count)
        {
            ulong position = Position;
            Position = ptr;
            byte[] numArray = ReadBytes(count);
            Position = position;
            return numArray;
        }

        public new byte ReadByte()
        {
            byte num = Data[GetDataOffset()];
            ++Position;
            return num;
        }

        public new short ReadInt16()
        {
            short num = BufferUtil.ReadShort(this.Data, this.GetDataOffset());
            this.Position += 2UL;
            byte[] bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToInt16(bytes, 0);
        }

        public new ushort ReadUInt16()
        {
            ushort num = BufferUtil.ReadUshort(Data, GetDataOffset());
            Position += 2UL;
            byte[] bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public new int ReadInt32()
        {
            int num = BufferUtil.ReadInt(Data, GetDataOffset());
            Position += 4UL;
            byte[] bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public new uint ReadUInt32()
        {
            uint num = BufferUtil.ReadUint(Data, GetDataOffset());
            Position += 4UL;
            byte[] bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public new long ReadInt64()
        {
            long num = BufferUtil.ReadLong(Data, GetDataOffset());
            Position += 8UL;
            byte[] bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public new ulong ReadUInt64()
        {
            ulong num = BufferUtil.ReadUlong(Data, GetDataOffset());
            Position += 8UL;
            byte[] bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public new float ReadSingle()
        {
            float num = BufferUtil.ReadSingle(Data, GetDataOffset());
            Position += 4UL;
            byte[] bytes = BitConverter.GetBytes(num);
            Array.Reverse((Array)bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        public new double ReadDouble()
        {
            double num = BufferUtil.ReadDouble(Data, GetDataOffset());
            Position += 4UL;
            return num;
        }

        public string ReadString()
        {
            List<byte> byteList = new List<byte>();
            int dataOffset = GetDataOffset();
            byte num1 = Data[dataOffset];
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
            Vector2 vector = BufferUtil.ReadVector2(Data, GetDataOffset());
            Position += 8UL;
            return Rpf3Crypto.Swap(vector);
        }

        public Vector2[] ReadVector2Arr(int count)
        {
            Vector2[] vector2Array = new Vector2[count];
            for (int index = 0; index < count; ++index)
            {
                vector2Array[index] = Rpf3Crypto.Swap(BufferUtil.ReadVector2(Data, GetDataOffset()));
                Position += 8UL;
            }
            return vector2Array;
        }

        public new Vector3 ReadVector3()
        {
            var v = BufferUtil.ReadVector3(Data, GetDataOffset());
            Position += 12;
            return new Vector3(v.Z, v.X, v.Y);
        }

        public Vector3[] ReadVector3Arr(int count)
        {
            var vectors = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                vectors[i] = ReadVector3();
            }
            return vectors;
        }

        public Vector4 ReadVector4(bool toZXYW = true)
        {
            var v = BufferUtil.ReadVector4(Data, GetDataOffset());
            Position += 16;

            if (float.IsNaN(v.W))
            {
                v = new Vector4(v.XYZ(), 0.0f);
            }
            return toZXYW ? new Vector4(v.Z, v.X, v.Y, v.W) : v;
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
                Min = BufferUtil.ReadVector4(Data, GetDataOffset()),
                Max = BufferUtil.ReadVector4(Data, GetDataOffset())
            };
            Position += 32;
            return Rpf3Crypto.ToZXY(bb);
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

        public T ReadBlock<T>(Func<Rsc5DataReader, T> createFunc = null) where T : Rsc5Block, new()
        {
            if (Position == 0) return default(T);
            if (BlockPool.TryGetValue(Position, out var exitem))
            {
                if (exitem is T exblock)
                {
                    return exblock;
                }
            }
            var block = (createFunc != null) ? createFunc(this) : new T();
            BlockPool[Position] = block;
            block.FilePosition = Position;
            block.Read(this);
            return block;
        }

        public T ReadBlock<T>(ulong position, Func<Rsc5DataReader, T> createFunc = null) where T : Rsc5Block, new()
        {
            if (position == 0 || position == 0xCDCDCDCD)
            {
                return default;
            }
            var p = Position;
            Position = position;
            var b = ReadBlock<T>(createFunc);
            Position = p;
            return b;
        }

        public T ReadCustomBlock<T>(Func<Rsc5DataReader, T> createFunc = null) where T : Rsc5Block, new()
        {
            return ReadBlock(createFunc);
        }

        public Rsc5Ptr<T> ReadPtr<T>(Func<Rsc5DataReader, T> createFunc = null) where T : Rsc5Block, new()
        {
            var ptr = new Rsc5Ptr<T>();
            ptr.Read(this, createFunc);
            return ptr;
        }

        public Rsc5PtrArr<T> ReadPtrArr<T>(Func<Rsc5DataReader, T> createFunc = null) where T : Rsc5Block, new()
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

        public Rsc5CustomArr<T> ReadArr<T>(Func<Rsc5DataReader, T> createFunc = null) where T : Rsc5Block, new()
        {
            var arr = new Rsc5CustomArr<T>();
            arr.Read(this);
            return arr;
        }

        public Rsc5RawArr<T> ReadRawArrItems<T>(Rsc5RawArr<T> arr, uint count, bool uShort = false) where T : unmanaged
        {
            arr.ReadItems(this, count, uShort);
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

        public Rsc5Ptr<T> ReadPtrPtr<T>() where T : Rsc5Block, new()
        {
            var ptr = new Rsc5Ptr<T>();
            ptr.ReadPtr(this);
            return ptr;
        }

        public Rsc5Ptr<T> ReadPtrItem<T>(Rsc5Ptr<T> ptr, Func<Rsc5DataReader, T> createFunc = null) where T : Rsc5Block, new()
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
    }

    public class Rsc5DataWriter : BlockWriter
    {
        public HashSet<object> PhysicalBlocks = new HashSet<object>();

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

        public void WriteBlock<T>(T block) where T : Rsc5Block, new()
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

        public void WriteStr(Rsc5Str str)
        {
            str.Write(this);
        }
        public void WriteArr<T>(Rsc5Arr<T> arr) where T : unmanaged
        {
            arr.Write(this);
        }

        public void WritePtr<T>(Rsc5Ptr<T> ptr) where T : Rsc5Block, new()
        {
            ptr.Write(this);
        }

        public void WritePtrArr<T>(Rsc5PtrArr<T> ptr) where T : Rsc5Block, new()
        {
            ptr.Write(this);
        }

        public void WriteRawArrPtr<T>(Rsc5RawArr<T> ptr) where T : unmanaged
        {
            ptr.Write(this);
        }
    }

    public interface Rsc5Block
    {
        ulong FilePosition { get; set; }
        ulong BlockLength { get; }
        bool IsPhysical { get; }
        void Read(Rsc5DataReader reader);
        void Write(Rsc5DataWriter writer);
    }

    public abstract class Rsc5BlockBase : Rsc5Block
    {
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public abstract ulong BlockLength { get; }
        public abstract void Read(Rsc5DataReader reader);
        public abstract void Write(Rsc5DataWriter writer);
    }

    public class Rsc5BlockMap : Rsc5BlockBase
    {
        public override ulong BlockLength => 4;

        public uint Unknown1 { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            Unknown1 = reader.ReadUInt32();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public struct Rsc5CustomArr<T> where T : Rsc5Block, new()
    {
        public uint Position;
        public ushort Count;
        public ushort Capacity;
        public T[] Items;

        public void Read(Rsc5DataReader reader, Func<Rsc5DataReader, T> createFunc = null) //Big-endian
        {
            Position = reader.ReadUInt32();
            Count = reader.ReadUInt16();
            Capacity = reader.ReadUInt16();

            var p = reader.Position;
            reader.Position = Position;
            Items = new T[Count];

            for (int i = 0; i < Count; i++)
            {
                Items[i] = reader.ReadCustomBlock(createFunc);
            }
            reader.Position = p;
        }

        public void Write(Rsc5DataWriter writer)
        {
        }

        public T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public override string ToString()
        {
            return "Count: " + Count.ToString();
        }
    }

    public struct Rsc5Arr<T> where T : unmanaged
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
            reader.Position = p;
        }

        public void Read(Rsc5DataReader reader, uint count)
        {
            Position = reader.ReadUInt32();
            Count = count;
            Capacity = count;
            var p = reader.Position;
            reader.Position = Position;
            Items = reader.ReadArray<T>(count);
            reader.Position = p;
        }

        public void Write(Rsc5DataWriter writer)
        {
        }

        public T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public override string ToString()
        {
            return "Count: " + Count.ToString();
        }
    }

    public struct Rsc5PtrArr<T> where T : Rsc5Block, new()
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
                Items[i] = reader.ReadBlock<T>(BitConverter.ToUInt32(buffer, 0), createFunc);
            }
            reader.Position = p;
        }

        public void Write(Rsc5DataWriter writer)
        {
        }

        public T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public override string ToString()
        {
            return "Count: " + Count.ToString();
        }
    }

    public struct Rsc5Ptr<T> where T : Rsc5Block, new()
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

        public void Write(Rsc5DataWriter writer)
        {
            writer.AddPointerRef(Item);
            writer.WriteUInt32((uint)Position);
            writer.WriteBlock(Item);
        }

        public override string ToString()
        {
            return Item?.ToString() ?? Position.ToString();
        }
    }

    public struct Rsc5Str
    {
        public ulong Position;
        public string Value;

        public void Read(Rsc5DataReader reader)
        {
            Position = reader.ReadUInt32();
            if (Position != 0)
            {
                var p = reader.Position;
                reader.Position = Position;
                Value = reader.ReadString();
                reader.Position = p;
            }
        }

        public void Write(Rsc5DataWriter writer)
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

        public override string ToString()
        {
            return Value;
        }
    }

    public class Rsc5String : Rsc5BlockBase
    {
        public override ulong BlockLength => 8;
        public uint Position;
        public string Value;
        public uint FixedLength;

        public Rsc5String() { }
        public Rsc5String(uint fixedLength) { FixedLength = fixedLength; }

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

    public abstract class Rsc5FileBase : Rsc5BlockBase
    {
        public ulong VFT { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public struct Rsc5RawArr<T> where T : unmanaged
    {
        public ulong Position;
        public T[] Items;

        public void ReadPtr(Rsc5DataReader reader)
        {
            Position = reader.ReadUInt32();
        }

        public void ReadItems(Rsc5DataReader reader, uint count, bool uShort = false) //Shouldn't be done like this for BE values - Foxxyyy
        {
            var p = reader.Position;
            reader.Position = Position;

            if (uShort)
                Items = reader.ReadUInt16Arr((int)count) as T[];
            else
                Items = reader.ReadArray<T>(count);

            Items ??= Array.Empty<T>();
            reader.Position = p;
        }

        public void Write(Rsc5DataWriter writer)
        {
        }

        public T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public override string ToString()
        {
            return "Count: " + (Items?.Length.ToString() ?? "0");
        }
    }
}
