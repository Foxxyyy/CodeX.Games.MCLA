using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using System.Numerics;
using System.Text;

namespace CodeX.Games.MCLA.Files
{
    //.xcc - CarConfig. Not an RSC5 resource: CarConfig::Load reads the file straight over the
    //object (a flat 8176 byte struct) and only patches the vtables back in, so the layout here
    //is the in-memory class layout. Names come from the game's own "CarConfig" strings.
    public class XccFile : FilePack
    {
        public const int FileSize = 8176;

        //Vinyl layers: 288 slots of 20 bytes, split into five surfaces. The split comes from the
        //game's own tables - offsets at 0x827E9770 and capacities at 0x820510B0.
        public const int LayersOffset = 0x0904;
        public const int LayerSize = 20;
        public const int LayerSlots = 288;
        public static readonly int[] SurfaceOffsets = [0, 64, 128, 192, 208];
        public static readonly int[] SurfaceCapacities = [64, 64, 64, 16, 16];

        public string ConfigName { get; set; }
        public string VehicleName { get; set; }
        public uint ConfigId { get; set; } //Differs between configs of the same car
        public string LicensePlate { get; set; }
        public byte LicensePlateStyle { get; set; }
        public int[] PerformanceLevels { get; set; } //Six upgrade categories, 0-5
        public XccVinylSurface[] Surfaces { get; set; }

        public XccFile()
        {
        }

        public XccFile(GameArchiveFileInfo info) : base(info)
        {
        }

        public override void Load(byte[] data)
        {
            if (data == null || data.Length < FileSize)
            {
                LoadException = new Exception($"Not a CarConfig: expected {FileSize} bytes, got {data?.Length ?? 0}");
                return;
            }

            VehicleName = ReadString(data, 0x1FAD, 32);
            ConfigName = ReadString(data, 0x1FCD, 34);
            ConfigId = ReadUInt32(data, 0x1FA8);
            LicensePlate = ReadString(data, 0x0847, 7);
            LicensePlateStyle = data[0x084F];

            PerformanceLevels = new int[6];
            for (int i = 0; i < PerformanceLevels.Length; i++)
            {
                PerformanceLevels[i] = (int)ReadUInt32(data, 0x0680 + i * 0x30);
            }

            Surfaces = new XccVinylSurface[SurfaceOffsets.Length];
            for (int s = 0; s < Surfaces.Length; s++)
            {
                Surfaces[s] = new XccVinylSurface(s, SurfaceOffsets[s], SurfaceCapacities[s], data);
            }
        }

        public override byte[] Save()
        {
            return null;
        }

        public override void Read(MetaNodeReader reader)
        {
            throw new NotImplementedException();
        }

        public override void Write(MetaNodeWriter writer)
        {
            writer.WriteString("ConfigName", ConfigName);
            writer.WriteString("VehicleName", VehicleName);
            writer.WriteUInt32("ConfigId", ConfigId);
            writer.WriteString("LicensePlate", LicensePlate);
            writer.WriteByte("LicensePlateStyle", LicensePlateStyle);
            writer.WriteInt32Array("PerformanceLevels", PerformanceLevels);
            writer.WriteNodeArray("Surfaces", Surfaces);
        }

        internal static string ReadString(byte[] data, int offset, int length)
        {
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                var b = data[offset + i];
                if (b == 0 || b == 0xCD) break;
                sb.Append((char)b);
            }
            return sb.ToString();
        }

        internal static uint ReadUInt32(byte[] data, int offset)
        {
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
        }

        internal static ushort ReadUInt16(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        //Scale, position and rotation are stored as halfs
        internal static float ReadHalf(byte[] data, int offset)
        {
            return (float)BitConverter.UInt16BitsToHalf(ReadUInt16(data, offset));
        }
    }

    public class XccVinylSurface : MetaNode
    {
        public int Index { get; set; }
        public int FirstSlot { get; set; }
        public int Capacity { get; set; }
        public XccVinylLayer[] Layers { get; set; }

        public XccVinylSurface()
        {
        }

        public XccVinylSurface(int index, int firstSlot, int capacity, byte[] data)
        {
            Index = index;
            FirstSlot = firstSlot;
            Capacity = capacity;

            //The game compacts layers towards the start of the surface, so the first empty slot
            //ends the list (see the shuffle in sub_82393220).
            var layers = new List<XccVinylLayer>();
            for (int i = 0; i < capacity; i++)
            {
                var offset = XccFile.LayersOffset + (firstSlot + i) * XccFile.LayerSize;
                var layer = new XccVinylLayer(i, data, offset);
                if (layer.ShapeId == 0xFFFF) break;
                layers.Add(layer);
            }
            Layers = [.. layers];
        }

        public void Read(MetaNodeReader reader)
        {
            throw new NotImplementedException();
        }

        public void Write(MetaNodeWriter writer)
        {
            writer.WriteInt32("Index", Index);
            writer.WriteInt32("FirstSlot", FirstSlot);
            writer.WriteInt32("Capacity", Capacity);
            writer.WriteNodeArray("Layers", Layers);
        }

        public override string ToString()
        {
            return $"Surface {Index}: {Layers?.Length ?? 0}/{Capacity} layers";
        }
    }

    public class XccVinylLayer : MetaNode
    {
        public int Index { get; set; }
        public ushort ShapeId { get; set; } //0xFFFF = empty slot. Decimal coded as group * 1000 + shape
        public uint ColourArgb { get; set; }
        public Vector2 Scale { get; set; }
        public Vector2 Position { get; set; }
        public float Rotation { get; set; }
        public ushort Unknown_Ch { get; set; }
        public byte Unknown_12h { get; set; }
        public byte Unknown_13h { get; set; }

        public XccVinylLayer()
        {
        }

        public XccVinylLayer(int index, byte[] data, int offset)
        {
            Index = index;
            Scale = new Vector2(XccFile.ReadHalf(data, offset), XccFile.ReadHalf(data, offset + 2));
            Position = new Vector2(XccFile.ReadHalf(data, offset + 4), XccFile.ReadHalf(data, offset + 6));
            ColourArgb = XccFile.ReadUInt32(data, offset + 8);
            Unknown_Ch = XccFile.ReadUInt16(data, offset + 12);
            Rotation = XccFile.ReadHalf(data, offset + 14);
            ShapeId = XccFile.ReadUInt16(data, offset + 16);
            Unknown_12h = data[offset + 18];
            Unknown_13h = data[offset + 19];
        }

        public void Read(MetaNodeReader reader)
        {
            throw new NotImplementedException();
        }

        public void Write(MetaNodeWriter writer)
        {
            writer.WriteInt32("Index", Index);
            writer.WriteUInt16("ShapeId", ShapeId);
            writer.WriteString("Colour", $"0x{ColourArgb:X8}"); //ARGB
            writer.WriteVector2("Scale", Scale);
            writer.WriteVector2("Position", Position);
            writer.WriteSingle("Rotation", Rotation);
            writer.WriteUInt16("Unknown_Ch", Unknown_Ch);
            writer.WriteByte("Unknown_12h", Unknown_12h);
            writer.WriteByte("Unknown_13h", Unknown_13h);
        }

        public override string ToString()
        {
            return $"Layer {Index}: shape {ShapeId}";
        }
    }
}
