using CodeX.Games.MCLA.RPF3;
using EXP = System.ComponentModel.ExpandableObjectConverter;
using TC = System.ComponentModel.TypeConverterAttribute;

namespace CodeX.Games.MCLA.RSC5
{
    //Serialized Flash movie (.xsf, resource type 27). The root block holds the character
    //table; every character shares a 12 byte header and the type byte at +8 selects the layout.
    public enum Rsc5FlashObjectType : byte
    {
        Movie = 0,
        Shape = 1,
        Font = 2,
        Unknown3 = 3,
        Bitmap = 4,
        Sprite = 5,
        Button = 6,
        Text = 7
    }

    [TC(typeof(EXP))]
    public class Rsc5FlashMovie : Rsc5BlockBaseMap
    {
        public override ulong BlockLength => 64;
        public override uint VFT { get; set; }
        public byte ObjectType { get; set; } //0, followed by the packer's 'pad' filler
        public uint ScenesPosition { get; set; } //Scene descriptors, first dword is the ARGB background colour
        public ushort ScenesCount { get; set; }
        public uint CharactersPosition { get; set; }
        public ushort CharactersCount { get; set; } //Fixed 1793 slot table, most of it unused
        public Rsc5FlashObject[] Characters { get; set; }
        public float Width { get; set; } //Stage size in pixels, 1280x720 in most files
        public float Height { get; set; }
        public float FrameRate { get; set; } //8.8 fixed point
        public ushort FrameCount { get; set; }
        public uint ShapeCount { get; set; }
        public uint ImportsPosition { get; set; } //Resolved at runtime, always zeroed in the file
        public ushort ImportsCount { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            ObjectType = reader.ReadByte();
            reader.ReadBytes(3); //'pad'

            ScenesPosition = reader.ReadUInt32();
            ScenesCount = reader.ReadUInt16();
            reader.ReadUInt16(); //capacity
            reader.ReadUInt32(); //always 0

            CharactersPosition = reader.ReadUInt32();
            CharactersCount = reader.ReadUInt16();
            reader.ReadUInt16(); //capacity
            reader.ReadUInt32(); //always 0

            Width = reader.ReadSingle();
            reader.ReadUInt32(); //always 0
            Height = reader.ReadSingle();
            FrameRate = reader.ReadUInt16() / 256.0f;
            FrameCount = reader.ReadUInt16();
            ShapeCount = reader.ReadUInt32();

            ImportsPosition = reader.ReadUInt32();
            ImportsCount = reader.ReadUInt16();

            //The character table is a fixed 1793 slot array and only a fraction of it is filled in;
            //the rest holds packer filler or stale pointers, so every slot is validated before it is
            //followed. Rsc5PtrArr can't be used here - it would try to read the junk.
            Characters = new Rsc5FlashObject[CharactersCount];
            for (int i = 0; i < CharactersCount; i++)
            {
                reader.Position = CharactersPosition + (ulong)(i * 4);
                var ptr = reader.ReadUInt32();
                if (!Rsc5Flash.IsCharacter(reader, ptr)) continue;

                try
                {
                    Characters[i] = reader.ReadBlock<Rsc5FlashObject>(ptr);
                }
                catch (Exception ex)
                {
                    Characters[i] = null; //A single bad character shouldn't take the whole movie down
                    Core.Engine.Console.Write("Rsc5FlashMovie", $"character {i} failed: {ex.Message}");
                }
            }
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"Flash movie: {Width}x{Height}, {CharactersCount} characters, {FrameRate}fps";
        }
    }

    [TC(typeof(EXP))]
    public class Rsc5FlashObject : Rsc5BlockBaseMap
    {
        public override ulong BlockLength => 32;
        public override uint VFT { get; set; }
        public Rsc5FlashObjectType ObjectType { get; set; }

        //Bitmap (type 4)
        public Rsc5Texture Texture { get; set; }
        public string ExternalTextureName { get; set; } //Set instead of Texture when the art lives in another pack
        public ushort Width { get; set; }
        public ushort Height { get; set; }

        //Font (type 2)
        public uint GlyphPagesPosition { get; set; }
        public ushort GlyphPagesCount { get; set; }

        //Text (type 7)
        public uint TextColour { get; set; } //ARGB
        public ushort FontIndex { get; set; }
        public ushort TextHeight { get; set; } //Twips
        public ushort GlyphCount { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            ObjectType = (Rsc5FlashObjectType)reader.ReadByte();
            reader.ReadBytes(3); //'pad'

            switch (ObjectType)
            {
                case Rsc5FlashObjectType.Bitmap:
                    var texturePtr = reader.ReadUInt32();
                    var namePtr = reader.ReadUInt32();
                    Width = reader.ReadUInt16();
                    Height = reader.ReadUInt16();

                    //One of the two is set: the art is either packed into this movie or pulled
                    //from another pack (shared.xtd) by file name.
                    if (Rsc5Flash.IsInVirtualSegment(reader, texturePtr, 64))
                    {
                        Texture = reader.ReadBlock<Rsc5Texture>(texturePtr);
                    }
                    else if (Rsc5Flash.IsInVirtualSegment(reader, namePtr, 1))
                    {
                        var pos = reader.Position;
                        reader.Position = namePtr;
                        ExternalTextureName = reader.ReadString();
                        reader.Position = pos;
                    }
                    break;
                case Rsc5FlashObjectType.Font:
                    GlyphPagesPosition = reader.ReadUInt32();
                    GlyphPagesCount = reader.ReadUInt16();
                    break;
                case Rsc5FlashObjectType.Text:
                    reader.ReadUInt32(); //glyph record pointer
                    reader.ReadUInt32(); //placement pointer
                    reader.ReadUInt32();
                    TextColour = reader.ReadUInt32();
                    FontIndex = reader.ReadUInt16();
                    TextHeight = reader.ReadUInt16();
                    reader.ReadBytes(16); //bounds
                    GlyphCount = reader.ReadUInt16();
                    break;
            }
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return ObjectType.ToString();
        }
    }

    public static class Rsc5Flash
    {
        public const uint FlashMovieVFT = 0x00554944;
        public const uint VirtualBase = 0x50000000;

        private static readonly uint[] KnownFormats = [2, 82, 83, 84, 134];

        //Unset pointers hold the packer's 0xCDCDCDCD filler, and stale ones can point past the
        //end of the segment, so every pointer is bounds checked before it is followed.
        public static bool IsInVirtualSegment(Rsc5DataReader reader, uint pointer, int size)
        {
            if ((pointer & 0xF0000000) != VirtualBase) return false;
            var offset = (int)(pointer & 0x0FFFFFFF);
            return offset >= 0 && offset + size <= Math.Min(reader.VirtualSize, reader.Data.Length);
        }

        //Only slots that actually carry a character have a null block map at +4, a type byte in
        //range at +8 and the packer's 'pad' filler in the three bytes behind it.
        public static bool IsCharacter(Rsc5DataReader reader, uint pointer)
        {
            if ((pointer & 0xF0000000) != VirtualBase) return false;

            var offset = (int)(pointer & 0x0FFFFFFF);
            var data = reader.Data;
            if (offset < 0 || offset + 32 > Math.Min(reader.VirtualSize, data.Length)) return false;

            if (ReadUInt32(data, offset + 4) != 0) return false;
            if (data[offset + 8] > (byte)Rsc5FlashObjectType.Text) return false;
            return data[offset + 9] == 'p' && data[offset + 10] == 'a' && data[offset + 11] == 'd';
        }

        //Fonts reach their glyph atlas through structures we don't decode yet, so the textures
        //are collected by sweeping the virtual segment for grcTexture blocks instead. A block is
        //accepted only when its name pointer resolves to a .dds string and its D3D header carries
        //a format we know - that matches the character table exactly on every file tested.
        public static List<ulong> FindTextureBlocks(byte[] data, int virtualSize)
        {
            var result = new List<ulong>();
            var limit = Math.Min(virtualSize, data.Length) - 0x40;

            for (int offset = 0; offset <= limit; offset += 4)
            {
                if (!TryGetVirtualOffset(data, offset + 0x18, virtualSize, out var nameOffset)) continue;
                if (!TryGetVirtualOffset(data, offset + 0x1C, virtualSize - 0x40, out var d3dOffset)) continue;
                if (!EndsWithDds(data, nameOffset, virtualSize)) continue;

                var width = ReadUInt16(data, offset + 0x20);
                var height = ReadUInt16(data, offset + 0x22);
                if (width == 0 || height == 0 || width > 4096 || height > 4096) continue;

                var format = ReadUInt32(data, d3dOffset + 0x20) & 0xFF;
                if (Array.IndexOf(KnownFormats, format) < 0) continue;

                result.Add(VirtualBase | (uint)offset);
            }
            return result;
        }

        private static bool TryGetVirtualOffset(byte[] data, int at, int limit, out int offset)
        {
            offset = 0;
            var ptr = ReadUInt32(data, at);
            if ((ptr & 0xF0000000) != Rsc5Flash.VirtualBase) return false;
            offset = (int)(ptr & 0x0FFFFFFF);
            return offset >= 0 && offset < limit;
        }

        private static bool EndsWithDds(byte[] data, int offset, int limit)
        {
            for (int i = offset; i < limit && i < offset + 256; i++)
            {
                var b = data[i];
                if (b == 0)
                {
                    return i >= offset + 4
                        && data[i - 4] == '.'
                        && (data[i - 3] | 0x20) == 'd'
                        && (data[i - 2] | 0x20) == 'd'
                        && (data[i - 1] | 0x20) == 's';
                }
                if (b < 0x20 || b > 0x7E) return false;
            }
            return false;
        }

        private static uint ReadUInt32(byte[] data, int offset)
        {
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
        }

        private static ushort ReadUInt16(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }
    }
}
