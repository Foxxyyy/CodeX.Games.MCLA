using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using SharpDX.Direct3D11;

namespace CodeX.Games.MCLA.RSC5
{
    public class Rsc5XtlTextureDictionary : Rsc5BlockBaseMap
    {
        public override ulong BlockLength => 80;
        public override uint VFT { get; set; } = 0x0057C228;
        public uint Unknown_8h { get; set; } //Always 0?
        public Rsc5Ptr<Rsc5ShaderGroup> ShaderGroup { get; set; }
        public Rsc5Ptr<Rsc5BlockMap> Unknown_10h { get; set; }
        public Rsc5Ptr<Rsc5Texture> ZoneTexture { get; set; }
        public Rsc5Ptr<Rsc5Texture> MaxDamageTexture { get; set; }
        public Rsc5Ptr<Rsc5Texture> ScratchTexture { get; set; }
        public float Unknown_20h { get; set; } = 0.0f; //Always 0.0f?
        public float Unknown_24h { get; set; } = 1.0f; //Always 1.0f?
        public float Unknown_28h { get; set; } = 0.0f; //Always 0.0f?
        public float Unknown_2Ch { get; set; } = 1.0f; //Always 1.0f?
        public float Unknown_30h { get; set; } = 0.0f; //Always 0.0f?
        public float Unknown_34h { get; set; } = 1.0f; //Always 1.0f?
        public float Unknown_38h { get; set; } = 0.0f; //Always 0.0f?
        public float Unknown_3Ch { get; set; } = 1.0f; //Always 1.0f?
        public float Unknown_40h { get; set; } = 0.0f; //Always 0.0f?
        public float Unknown_44h { get; set; } = 1.0f; //Always 1.0f?
        public Rsc5Ptr<Rsc5BlockMap> Unknown_48h { get; set; }
        public Rsc5Ptr<Rsc5BlockMap> Unknown_4Ch { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            Unknown_8h = reader.ReadUInt32();
            ShaderGroup = reader.ReadPtr<Rsc5ShaderGroup>();
            Unknown_10h = reader.ReadPtr<Rsc5BlockMap>();
            ZoneTexture = reader.ReadPtr<Rsc5Texture>();
            MaxDamageTexture = reader.ReadPtr<Rsc5Texture>();
            ScratchTexture = reader.ReadPtr<Rsc5Texture>();
            Unknown_20h = reader.ReadSingle();
            Unknown_24h = reader.ReadSingle();
            Unknown_28h = reader.ReadSingle();
            Unknown_2Ch = reader.ReadSingle();
            Unknown_30h = reader.ReadSingle();
            Unknown_34h = reader.ReadSingle();
            Unknown_38h = reader.ReadSingle();
            Unknown_3Ch = reader.ReadSingle();
            Unknown_40h = reader.ReadSingle();
            Unknown_44h = reader.ReadSingle();
            Unknown_48h = reader.ReadPtr<Rsc5BlockMap>();
            Unknown_4Ch = reader.ReadPtr<Rsc5BlockMap>();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            base.Write(writer);
            writer.WriteUInt32(Unknown_8h);
            writer.WritePtr(ShaderGroup);
            writer.WritePtr(Unknown_10h);
            writer.WritePtr(ZoneTexture);
            writer.WritePtr(MaxDamageTexture);
            writer.WritePtr(ScratchTexture);
            writer.WriteSingle(Unknown_20h);
            writer.WriteSingle(Unknown_24h);
            writer.WriteSingle(Unknown_28h);
            writer.WriteSingle(Unknown_2Ch);
            writer.WriteSingle(Unknown_30h);
            writer.WriteSingle(Unknown_34h);
            writer.WriteSingle(Unknown_38h);
            writer.WriteSingle(Unknown_3Ch);
            writer.WriteSingle(Unknown_40h);
            writer.WriteSingle(Unknown_44h);
            writer.WritePtr(Unknown_48h);
            writer.WritePtr(Unknown_4Ch);
        }
    }

    public class Rsc5TextureDictionary : Rsc5BlockBaseMap, MetaNode
    {
        public override ulong BlockLength => 32;
        public override uint VFT { get; set; } = 0;
        public uint ParentDictionary { get; set; } //Always 0 in file
        public uint UsageCount { get; set; } //Always 1 in file
        public Rsc5Arr<JenkHash> Hashes { get; set; }
        public Rsc5PtrArr<Rsc5Texture> Textures { get; set; }

        public Dictionary<JenkHash, Rsc5Texture> Dict = [];

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            ParentDictionary = reader.ReadUInt32();
            UsageCount = reader.ReadUInt32();
            Hashes = reader.ReadArr<JenkHash>();
            Textures = reader.ReadPtrArr<Rsc5Texture>();

            if (Textures.Items != null)
            {
                var hCount = Hashes.Items?.Length ?? 0;
                var tCount = Textures.Items.Length;
                var imax = Math.Min(hCount, tCount);
                for (int i = 0; i < tCount; i++)
                {
                    var t = Textures.Items[i];
                    if (t == null) continue;
                    var h = (i < imax && Hashes.Items != null) ? Hashes.Items[i] : 0;
                    
                    t.Name ??= h.ToString();
                    Dict[h] = t;
                }
            }
        }

        public override void Write(Rsc5DataWriter writer)
        {
            base.Write(writer);
            writer.WriteUInt32(ParentDictionary);
            writer.WriteUInt32(UsageCount > 0 ? UsageCount : 1);
            writer.WriteArr(Hashes);
            writer.WritePtrArr(Textures);
        }

        public void Read(MetaNodeReader reader)
        {
            var textures = reader.ReadNodeArray<Rsc5Texture>("Textures");
            if (textures != null)
            {
                Build(textures);
            }
        }

        public void Write(MetaNodeWriter writer)
        {
            writer.WriteUInt32("@version", 0);
            writer.WriteNodeArray("Textures", Textures.Items);
        }

        public void Build(IEnumerable<Rsc5Texture> textures)
        {
            var list = new List<Tuple<JenkHash, Rsc5Texture>>();
            Dict.Clear();
            foreach (var texture in textures)
            {
                var cleanName = Path.GetFileNameWithoutExtension(texture.Name.Replace(".dds", "").ToLowerInvariant());
                var hash = JenkHash.GenHash(cleanName + ".dds");
                list.Add(new Tuple<JenkHash, Rsc5Texture>(hash, texture));
                Dict[hash] = texture;
            }
            int count = list.Count;
            var texArr = new Rsc5Texture[count];
            var hashArr = new JenkHash[count];
            for (int i = 0; i < count; i++)
            {
                hashArr[i] = list[i].Item1;
                texArr[i] = list[i].Item2;
            }
            Hashes = new Rsc5Arr<JenkHash>(hashArr);
            Textures = new Rsc5PtrArr<Rsc5Texture>(texArr);
        }
    }

    public class Rsc5Bitmap : Rsc5BlockBaseMap
    {
        public override ulong BlockLength => 16;
        public override uint VFT { get; set; } = 0;
        public Rsc5Ptr<Rsc5Texture> Texture1 { get; set; }
        public Rsc5Ptr<Rsc5Texture> Texture2 { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            Texture1 = reader.ReadPtr<Rsc5Texture>();
            Texture2 = reader.ReadPtr<Rsc5Texture>();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            base.Write(writer);
            writer.WritePtr(Texture1);
            writer.WritePtr(Texture2);
        }
    }

    public class Rsc5D3DTexture : Rsc5BlockMap
    {
        public override ulong BlockLength => 64;
        public uint[] D3DHeader { get; set; } = new uint[16];
        public Rsc5TextureData TextureData { get; set; }
        public Rsc5TextureFormat Format { get; set; }

        public Rsc5D3DTexture() { }

        public Rsc5D3DTexture(uint[] header, Rsc5TextureData data, Rsc5TextureFormat format)
        {
            if (header != null && header.Length >= 16)
            {
                D3DHeader = (uint[])header.Clone();
            }
            else
            {
                D3DHeader = new uint[16];
            }
            TextureData = data;
            Format = format;
        }

        public override void Read(Rsc5DataReader reader)
        {
            D3DHeader = reader.ReadUInt32Arr(16);
        }

        public override void Write(Rsc5DataWriter writer)
        {
            if (TextureData != null)
            {
                writer.AddPointerRef(TextureData, writer.Data, 0x20, (ulong)Format);
            }
            for (int i = 0; i < 16; i++)
            {
                writer.WriteUInt32(D3DHeader[i]);
            }
        }
    }

    public class Rsc5Texture : Rsc5TextureBase
    {
        public override ulong BlockLength => base.BlockLength + 28; //36 + 28 = 64
        public Rsc5TextureType TextureType { get; set; } //0 = normal, 1 = cube, 3 = volume
        public float ColorExpR { get; set; } = 1.0f; //m_ColorExprR
        public float ColorExpG { get; set; } = 1.0f; //m_ColorExprG
        public float ColorExpB { get; set; } = 1.0f; //m_ColorExprB
        public float ColorOfsR { get; set; } = 0.0f; //m_ColorOfsR
        public float ColorOfsG { get; set; } = 0.0f; //m_ColorOfsG
        public float ColorOfsB { get; set; } = 0.0f; //m_ColorOfsB
        public int Size { get; set; }
        public uint D3DValue { get; set; } //grcTexture D3D header dword: format in 0..5, endianness in 6..7, base address above
        public uint[] D3DHeader { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            if (D3DBaseTexture.Item == null)
            {
                return;
            }

            Stride = reader.ReadUInt16();
            TextureType = (Rsc5TextureType)reader.ReadByte();
            MipLevels = reader.ReadByte();
            ColorExpR = reader.ReadSingle(); //1.0f
            ColorExpG = reader.ReadSingle(); //1.0f
            ColorExpB = reader.ReadSingle(); //1.0f
            ColorOfsR = reader.ReadSingle(); //0.0f
            ColorOfsG = reader.ReadSingle(); //0.0f
            ColorOfsB = reader.ReadSingle(); //0.0f

            D3DHeaderPosition = D3DBaseTexture.Item.FilePosition;
            reader.Position = D3DBaseTexture.Item.FilePosition + 0x20;
            var d3dValue = reader.ReadInt32();
            DataOffset = (ulong)Rpf3Crypto.GetBaseAdressFromDirect3D(d3dValue, reader);
            reader.Position = DataOffset;

            D3DValue = (uint)d3dValue;
            var hp = reader.Position;
            reader.Position = D3DHeaderPosition;
            D3DHeader = reader.ReadUInt32Arr(16);
            reader.Position = hp;
            Format = ConvertToEngineFormat((Rsc5TextureFormat)(d3dValue & byte.MaxValue));
            Size = Rpf3Crypto.GetTiledSurfaceSize(Width, Height, Format);
            Data = reader.ReadBytesPadded(Size);
            Sampler = TextureSampler.Create(TextureSamplerFilter.Anisotropic, TextureAddressMode.Wrap);
            Data = Rpf3Crypto.UnswizzleXbox360Data(Data, Width, Height, Format);
            Size = Data.Length; //In case of virtual dimensions, get the actual texture size
        }

        public override void Write(Rsc5DataWriter writer)
        {
            var swizzled = (Data != null) ? Rpf3Crypto.SwizzleXbox360Data(Data, Width, Height, Format) : [];
            var rsc5Format = ConvertToRsc5Format(Format);
            var texData = new Rsc5TextureData
            {
                BlockLength = (ulong)(swizzled?.Length ?? 0),
                Data = swizzled ?? []
            };

            var d3d = new uint[16];
            if (D3DHeader != null && D3DHeader.Length >= 16)
            {
                Array.Copy(D3DHeader, d3d, 16);
            }
            d3d[7] = (d3d[7] & ~(0x1FFu << 22)) | (((uint)(Width >> 5) & 0x1FFu) << 22);
            d3d[8] = (uint)rsc5Format;
            d3d[9] = (d3d[9] & ~0x03FFFFFFu) | ((uint)(Width - 1) & 0x1FFFu) | (((uint)(Height - 1) & 0x1FFFu) << 13);
            d3d[11] = (d3d[11] & ~(0xFu << 6)) | ((uint)(MipLevels & 0xFu) << 6);

            var d3dBlock = new Rsc5D3DTexture(d3d, texData, rsc5Format);
            D3DBaseTexture = new Rsc5Ptr<Rsc5BlockMap>
            {
                Item = d3dBlock
            };

            base.Write(writer);
            writer.WriteUInt16(Stride);
            writer.WriteByte((byte)TextureType);
            writer.WriteByte(MipLevels);
            writer.WriteSingle(ColorExpR);
            writer.WriteSingle(ColorExpG);
            writer.WriteSingle(ColorExpB);
            writer.WriteSingle(ColorOfsR);
            writer.WriteSingle(ColorOfsG);
            writer.WriteSingle(ColorOfsB);

            writer.WriteBlock(texData);
        }

        public enum Rsc5TextureType
        {
            Normal = 0,
            Cube = 1,
            Volume = 3
        };

        private int CalcDataSize(int width, int height)
        {
            var num = Format switch
            {
                TextureFormat.BC1 => width * height / 2,
                TextureFormat.BC2 or TextureFormat.BC3 or TextureFormat.A8R8G8B8 or TextureFormat.L8 => width * height,
                _ => throw new NotImplementedException()
            };
            return num;
        }

        public static TextureFormat ConvertToEngineFormat(Rsc5TextureFormat f)
        {
            return f switch
            {
                Rsc5TextureFormat.D3DFMT_DXT3 => TextureFormat.BC2,
                Rsc5TextureFormat.D3DFMT_DXT5 => TextureFormat.BC3,
                Rsc5TextureFormat.D3DFMT_A8R8G8B8 => TextureFormat.A8R8G8B8,
                Rsc5TextureFormat.D3DFMT_L8 => TextureFormat.L8,
                _ => TextureFormat.BC1,
            };
        }

        public static Rsc5TextureFormat ConvertToRsc5Format(TextureFormat f)
        {
            return f switch
            {
                TextureFormat.BC2 => Rsc5TextureFormat.D3DFMT_DXT3,
                TextureFormat.BC3 => Rsc5TextureFormat.D3DFMT_DXT5,
                TextureFormat.A8R8G8B8 => Rsc5TextureFormat.D3DFMT_A8R8G8B8,
                TextureFormat.L8 => Rsc5TextureFormat.D3DFMT_L8,
                _ => Rsc5TextureFormat.D3DFMT_DXT1,
            };
        }

        public static Rsc5Texture Create(Texture t)
        {
            var tex = new Rsc5Texture
            {
                Name = t.Name,
                Sampler = t.Sampler.Value != 0 ? t.Sampler : TextureSampler.AnisotropicWrap,
                Format = t.Format,
                Width = t.Width,
                Height = t.Height,
                Depth = t.Depth,
                Stride = (ushort)t.Stride,
                MipLevels = (byte)(t.MipLevels > 0 ? t.MipLevels : 1),
                Data = t.Data,
                TextureType = Rsc5TextureType.Normal,
                ColorExpR = 1.0f,
                ColorExpG = 1.0f,
                ColorExpB = 1.0f,
                ColorOfsR = 0.0f,
                ColorOfsG = 0.0f,
                ColorOfsB = 0.0f,
                Unknown_4h = 1,
                IsModified = true
            };
            if (tex.Stride == 0)
            {
                tex.Stride = (ushort)(tex.Width * (tex.Format == TextureFormat.BC1 ? 2 : 4));
            }
            return tex;
        }

        public override void Read(MetaNodeReader reader)
        {
            base.Read(reader);
            var cleanName = Name ?? "";
            if (cleanName.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
            {
                cleanName = cleanName[..^4];
            }
            cleanName = Path.GetFileName(cleanName);
            var dds = reader.ReadExternal(cleanName + ".dds");
            if (dds != null)
            {
                var tex = DDSIO.GetTexture(dds);
                if (tex != null)
                {
                    Width = tex.Width;
                    Height = tex.Height;
                    Format = tex.Format;
                    MipLevels = (byte)(tex.MipLevels > 0 ? tex.MipLevels : 1);
                    Data = tex.Data;
                    Stride = (ushort)(tex.Stride > 0 ? tex.Stride : tex.Width * (tex.Format == TextureFormat.BC1 ? 2 : 4));
                    Sampler = tex.Sampler.Value != 0 ? tex.Sampler : TextureSampler.AnisotropicWrap;
                    IsModified = true;
                }
            }
        }

        public override void Write(MetaNodeWriter writer)
        {
            base.Write(writer);
        }

        public override string ToString()
        {
            return "Texture: " + Width.ToString() + "x" + Height.ToString() + ": " + Name;
        }
    }

    public class Rsc5TextureBase : Texture, IRsc5Block
    {
        public virtual ulong BlockLength => 36;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public uint VFT { get; set; }
        public uint Unknown_4h { get; set; } = 1;
        public uint Unknown_8h { get; set; }
        public uint Unknown_Ch { get; set; }
        public uint Unknown_10h { get; set; }
        public uint Unknown_14h { get; set; }
        public Rsc5Str TextureName { get; set; }
        public Rsc5Ptr<Rsc5BlockMap> D3DBaseTexture { get; set; }

        public ulong DataOffset { get; set; }
        public ulong D3DHeaderPosition { get; set; }
        public ulong TexturePosition { get; set; }
        public bool IsModified { get; set; }

        public virtual void Read(Rsc5DataReader reader)
        {
            TexturePosition = reader.Position;
            VFT = reader.ReadUInt32();
            Unknown_4h = reader.ReadUInt32();
            Unknown_8h = reader.ReadUInt32();
            Unknown_Ch = reader.ReadUInt32();
            Unknown_10h = reader.ReadUInt32();
            Unknown_14h = reader.ReadUInt32();
            TextureName = reader.ReadStr();
            D3DBaseTexture = reader.ReadPtr<Rsc5BlockMap>();

            Name = TextureName.Value?.Replace(".dds", "").Replace("pack:/", "") ?? null;
            if (D3DBaseTexture.Item == null)
            {
                return;
            }

            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
        }

        public virtual void Write(Rsc5DataWriter writer)
        {
            if (string.IsNullOrEmpty(Name))
            {
                Name = "texture";
            }
            var nameToSave = Name.EndsWith(".dds") ? Name : Name + ".dds";
            TextureName = new Rsc5Str(nameToSave);

            writer.WriteUInt32(VFT);
            writer.WriteUInt32(Unknown_4h);
            writer.WriteUInt32(Unknown_8h);
            writer.WriteUInt32(Unknown_Ch);
            writer.WriteUInt32(Unknown_10h);
            writer.WriteUInt32(Unknown_14h);
            writer.WriteStr(TextureName);
            writer.WritePtr(D3DBaseTexture);
            writer.WriteUInt16(Width);
            writer.WriteUInt16(Height);
        }

        public override void Read(MetaNodeReader reader)
        {
            Name = reader.ReadString("@name");
            if (string.IsNullOrEmpty(Name))
            {
                Name = reader.ReadString("Name");
            }
        }

        public override void Write(MetaNodeWriter writer)
        {
            var cleanName = Name ?? "";
            if (cleanName.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
            {
                cleanName = cleanName[..^4];
            }
            cleanName = Path.GetFileName(cleanName);
            writer.WriteString("@name", cleanName);
            var dds = (Data != null) ? DDSIO.GetDDSFile(this) : null;
            if (dds != null)
            {
                writer.WriteExternal(cleanName + ".dds", dds);
            }
        }

        public override string ToString()
        {
            return "TextureBase: " + Name;
        }
    }

    public class Rsc5TextureData : IRsc5Block
    {
        public ulong BlockLength { get; set; }
        public ulong FilePosition { get; set; }
        public bool IsPhysical => true;
        public byte[] Data { get; set; }

        public void Read(Rsc5DataReader reader)
        {
            Data = reader.ReadBytes((int)BlockLength);
        }

        public void Write(Rsc5DataWriter writer)
        {
            writer.WriteBytes(Data);
        }
    }

    public enum Rsc5TextureFormat : uint
    {
        D3DFMT_L8 = 2,
        D3DFMT_DXT1 = 82,
        D3DFMT_DXT3 = 83,
        D3DFMT_DXT5 = 84,
        D3DFMT_A8R8G8B8 = 134
    }
}
