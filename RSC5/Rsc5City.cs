using System.Numerics;
using CodeX.Core.Numerics;
using CodeX.Core.Utilities;

namespace CodeX.Games.MCLA.RSC5
{
    public class Rsc5City : Rsc5BlockBaseMap, MetaNode
    {
        public override ulong BlockLength => 28;
        public override uint VFT { get; set; } = 0x005CAF3C;
        public Rsc5Ptr<Rsc5CityBounds> MapBounds { get; set; }
        public uint Unknown_Ch { get; set; } //0
        public Rsc5Ptr<Rsc5CityUnknown1> Unknown_10h { get; set; }
        public Rsc5ManagedArr<Rsc5CityMapSector> MapSectors { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            MapBounds = reader.ReadPtr<Rsc5CityBounds>();
            Unknown_Ch = reader.ReadUInt32();
            Unknown_10h = reader.ReadPtr<Rsc5CityUnknown1>();
            MapSectors = reader.ReadArr<Rsc5CityMapSector>();
        }

        public void Read(MetaNodeReader reader)
        {
            throw new NotImplementedException();
        }

        public void Write(MetaNodeWriter writer)
        {
            writer.WriteNode("MapBounds", MapBounds.Item);
            writer.WriteNodeArray("Sectors", MapSectors.Items);
        }
    }

    public class Rsc5CityMapSector : Rsc5BlockBase, MetaNode
    {
        public override ulong BlockLength => 304;
        public byte[] Unknown_0h { get; set; } //Empty buffer of 208 bytes
        public Vector4 AABBMin { get; set; }
        public Vector4 AABBMax { get; set; }
        public Rsc5Str SectorName { get; set; }
        public byte[] Unknown_F4h { get; set; } //Empty buffer of 60 bytes

        public override void Read(Rsc5DataReader reader)
        {
            Unknown_0h = reader.ReadBytes(0xD0);
            AABBMin = reader.ReadVector4();
            AABBMax = reader.ReadVector4();
            SectorName = reader.ReadStr();
            Unknown_F4h = reader.ReadBytes(0x3C);
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public void Read(MetaNodeReader reader)
        {
            throw new NotImplementedException();
        }

        public void Write(MetaNodeWriter writer)
        {
            writer.WriteString("@name", this.ToString());
            writer.WriteVector3("BoundsMin", AABBMin.XYZ());
            writer.WriteVector3("BoundsMax", AABBMax.XYZ());
        }

        public override string ToString()
        {
            return SectorName.ToString();
        }
    }

    public class Rsc5CityBounds : Rsc5BlockBase, MetaNode
    {
        public override ulong BlockLength => 64;
        public Vector4 AABBMin { get; set; }
        public Vector4 AABBMax { get; set; }
        public uint Unknown_20h { get; set; }
        public uint Unknown_24h { get; set; }
        public uint Unknown_28h { get; set; }
        public uint Unknown_2Ch { get; set; }
        public uint Unknown_30h { get; set; }
        public uint Unknown_34h { get; set; }
        public uint Unknown_38h { get; set; }
        public uint Unknown_3Ch { get; set; }
        public uint Unknown_40h { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            AABBMin = reader.ReadVector4();
            AABBMax = reader.ReadVector4();
            Unknown_20h = reader.ReadUInt32();
            Unknown_24h = reader.ReadUInt32();
            Unknown_28h = reader.ReadUInt32();
            Unknown_30h = reader.ReadUInt32();
            Unknown_34h = reader.ReadUInt32();
            Unknown_38h = reader.ReadUInt32();
            Unknown_3Ch = reader.ReadUInt32();
            Unknown_40h = reader.ReadUInt32();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public void Read(MetaNodeReader reader)
        {
            throw new NotImplementedException();
        }

        public void Write(MetaNodeWriter writer)
        {
            writer.WriteVector3("BoundsMin", AABBMin.XYZ());
            writer.WriteVector3("BoundsMax", AABBMax.XYZ());
        }
    }

    public class Rsc5CityUnknown1 : Rsc5FileBase, MetaNode
    {
        public override ulong BlockLength => 28;
        public override uint VFT { get; set; } = 0x005CB23C;
        public ushort Unknown_4h { get; set; } //270
        public ushort Unknown_6h { get; set; } //32766
        public uint Unknown_8h { get; set; } //0
        public int Unknown_Ch { get; set; } //1
        public int Unknown_10h { get; set; } //0
        public int Unknown_14h { get; set; } //0
        public int Unknown_18h { get; set; } //0

        public override void Read(Rsc5DataReader reader)
        {
            Unknown_4h = reader.ReadUInt16();
            Unknown_6h = reader.ReadUInt16();
            Unknown_8h = reader.ReadUInt32();
            Unknown_Ch = reader.ReadInt32();
            Unknown_10h = reader.ReadInt32();
            Unknown_14h = reader.ReadInt32();
            Unknown_18h = reader.ReadInt32();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public void Read(MetaNodeReader reader)
        {
            throw new NotImplementedException();
        }

        public void Write(MetaNodeWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}