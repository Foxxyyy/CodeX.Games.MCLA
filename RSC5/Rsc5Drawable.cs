using CodeX.Core.Engine;
using CodeX.Core.Numerics;
using CodeX.Core.Shaders;
using CodeX.Core.Utilities;
using System.Numerics;
using System.Text;
using CodeX.Games.MCLA.RPF3;
using TC = System.ComponentModel.TypeConverterAttribute;
using EXP = System.ComponentModel.ExpandableObjectConverter;

namespace CodeX.Games.MCLA.RSC5
{
    [TC(typeof(EXP))] public class Rsc5DrawableDictionary<T> : Rsc5FileBase where T: Rsc5DrawableBase, new()
    {
        public override ulong BlockLength => 64;
        public uint BlockMapPointer { get; set; }
        public Rsc5Arr<JenkHash> Hashes { get; set; }
        public Rsc5Ptr<T> Drawables { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            BlockMapPointer = reader.ReadUInt32();
            Drawables = reader.ReadPtr<T>();
            Hashes = reader.ReadArr<JenkHash>();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [TC(typeof(EXP))] public class Rsc5MapDictionary<T> : Rsc5FileBase where T : Rsc5DrawableBase, new()
    {
        public override ulong BlockLength => 64;
        public uint BlockMapPointer { get; set; }
        public Rsc5Ptr<Rsc5TextureDictionary> TextureDictionary { get; set; }
        public Rsc5Ptr<T> Drawables { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            BlockMapPointer = reader.ReadUInt32();
            TextureDictionary = reader.ReadPtr<Rsc5TextureDictionary>();
            Drawables = reader.ReadPtr<T>();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [TC(typeof(EXP))] public class Rsc5Drawable : Rsc5DrawableBase
    {
        public Rsc5Str NameRef { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
        }

        public override void Write(Rsc5DataWriter writer)
        {
            base.Write(writer);
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return NameRef.Value;
        }
    }

    [TC(typeof(EXP))] public class Rsc5DrawableBase : Piece, Rsc5Block
    {
        public virtual ulong BlockLength => 160;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public ulong VFT { get; set; }

        public Rsc5Ptr<Rsc5ShaderGroup> ShaderGroup { get; set; }
        public Rsc5DrawableLod Lod { get; set; }

        public virtual void Read(Rsc5DataReader reader)
        {
            Lod = reader.ReadBlock<Rsc5DrawableLod>();
            Lods = new[] { Lod };

            if (Lod != null)
            {
                Lod.LodDist = 9999f;
            }

            UpdateAllModels();
            AssignGeometryShaders(ShaderGroup.Item);
        }

        public virtual void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public void AssignGeometryShaders(Rsc5ShaderGroup shaderGrp)
        {
            if (AllModels == null)
                return;

            foreach (var model in AllModels.Cast<Rsc5DrawableModel>())
            {
                var geoms = model?.Geometries.Items;
                if (geoms == null) continue;

                int geomcount = geoms.Length;
                for (int i = 0; i < geomcount; i++)
                {
                    var geom = geoms[i];
                    geom.SetDefaultShader();
                    geom.ShaderInputs = geom.Shader.CreateShaderInputs();
                }
            }
        }
    }

    [TC(typeof(EXP))]
    public class Rsc5DrawableLod : PieceLod, Rsc5Block
    {
        public ulong BlockLength => 16;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public Rsc5PtrArr<Rsc5DrawableModel> ModelsData { get; set; }

        public void Read(Rsc5DataReader reader)
        {
            ModelsData = reader.ReadPtrArr<Rsc5DrawableModel>();
            Models = ModelsData.Items;
        }

        public void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(StringBuilder sb, int indent, Vector3 center)
        {
            foreach (var m in ModelsData.Items)
            {
                Xml.OpenTag(sb, indent, "Item");
                m.WriteXml(sb, indent + 1, center);
                Xml.CloseTag(sb, indent, "Item");
            }
        }
    }

    [TC(typeof(EXP))]
    public class Rsc5DrawableModel : Model, Rsc5Block
    {
        public ulong BlockLength
        {
            get
            {
                //this crazyness exists to force a particular block layout when saving, same as vanilla files
                var off = (ulong)48;
                var geocount = Geometries.Count;
                var geoms = Geometries.Items;
                off += (geocount * 2u); //ShaderMapping
                if (geocount == 1) off += 6;
                else off += ((16 - (off % 16)) % 16);
                off += (geocount * 8u); //Geometries pointers
                off += ((16 - (off % 16)) % 16);
                off += (geocount + ((geocount > 1) ? 1u : 0)) * 32; //BoundsData
                for (int i = 0; i < geocount; i++)
                {
                    var geom = (geoms != null) ? geoms[i] : null;
                    if (geom != null)
                    {
                        off += ((16 - (off % 16)) % 16);
                        off += geom.BlockLength; //Geometries
                    }
                }
                return off;
            }
        }
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public ulong VFT { get; set; } = 0x82E7C4;
        public Rsc5PtrArr<Rsc5DrawableGeometry> Geometries { get; set; }
        public Rsc5RawArr<BoundingBox4> BoundsData { get; set; }
        public Rsc5RawArr<ushort> ShaderMapping { get; set; }
        public uint SkeletonBinding { get; set; }//4th byte is bone index, 2nd byte for skin meshes
        public ushort RenderMaskFlags { get; set; } //First byte is called "Mask" in GIMS EVO
        public ushort GeometriesCount3 { get; set; } //always equal to Geometries.Count, is it ShaderMappingCount?

        public byte BoneIndex
        {
            get { return (byte)((SkeletonBinding >> 24) & 0xFF); }
            set { SkeletonBinding = (SkeletonBinding & 0x00FFFFFF) + ((value & 0xFFu) << 24); }
        }
        public byte SkeletonBindUnk2 //always 0
        {
            get { return (byte)((SkeletonBinding >> 16) & 0xFF); }
            set { SkeletonBinding = (SkeletonBinding & 0xFF00FFFF) + ((value & 0xFFu) << 16); }
        }
        public byte HasSkin //only 0 or 1
        {
            get { return (byte)((SkeletonBinding >> 8) & 0xFF); }
            set { SkeletonBinding = (SkeletonBinding & 0xFFFF00FF) + ((value & 0xFFu) << 8); }
        }
        public byte SkeletonBindUnk1 //only 0 or 43 (in rare cases, see below)
        {
            get { return (byte)((SkeletonBinding >> 0) & 0xFF); }
            set { SkeletonBinding = (SkeletonBinding & 0xFFFFFF00) + ((value & 0xFFu) << 0); }
        }
        public byte RenderMask
        {
            get { return (byte)((RenderMaskFlags >> 0) & 0xFF); }
            set { RenderMaskFlags = (ushort)((RenderMaskFlags & 0xFF00u) + ((value & 0xFFu) << 0)); }
        }
        public byte Flags
        {
            get { return (byte)((RenderMaskFlags >> 8) & 0xFF); }
            set { RenderMaskFlags = (ushort)((RenderMaskFlags & 0xFFu) + ((value & 0xFFu) << 8)); }
        }

        public void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            Geometries = reader.ReadPtrArr<Rsc5DrawableGeometry>();
            BoundsData = reader.ReadRawArrPtr<BoundingBox4>();
            ShaderMapping = reader.ReadRawArrPtr<ushort>();
            SkeletonBinding = reader.ReadUInt32();
            RenderMaskFlags = reader.ReadUInt16();
            GeometriesCount3 = reader.ReadUInt16();

            var geocount = Geometries.Count;
            ShaderMapping = reader.ReadRawArrItems(ShaderMapping, geocount, true);
            BoundsData = reader.ReadRawArrItems(BoundsData, geocount > 1 ? geocount + 1u : geocount);

            var geoms = Geometries.Items;
            if (geoms != null)
            {
                var shaderMapping = ShaderMapping.Items;
                var boundsData = (BoundsData.Items != null && BoundsData.Items.Length > 0) ? Rpf3Crypto.Swap(BoundsData.Items) : null;

                for (int i = 0; i < geoms.Length; i++)
                {
                    var geom = geoms[i];
                    if (geom != null)
                    {
                        geom.ShaderID = ((shaderMapping != null) && (i < shaderMapping.Length)) ? shaderMapping[i] : (ushort)0;
                        geom.AABB = (boundsData != null) ? ((boundsData.Length > 1) && ((i + 1) < boundsData.Length)) ? boundsData[i + 1] : boundsData[0] : new BoundingBox4();

                        geom.BoundingBox = new BoundingBox(geom.AABB.Min.XYZ(), geom.AABB.Max.XYZ());
                    }
                }
            }
            Meshes = Geometries.Items;

            // FIXME
            RenderInMainView = true; //((RenderMaskFlags & 0x1) > 0);
            RenderInShadowView = true; //((RenderMaskFlags & 0x2) > 0);
            RenderInEnvmapView = true; //((RenderMaskFlags & 0x4) > 0);
        }

        public void Write(Rsc5DataWriter writer)
        {
            GeometriesCount3 = Geometries.Count; //is this correct?
            throw new NotImplementedException();
        }

        public void WriteXml(StringBuilder sb, int indent, Vector3 center)
        {
            Xml.ValueTag(sb, indent, "RenderMask", "255");
            Xml.ValueTag(sb, indent, "Flags", "0");
            Xml.ValueTag(sb, indent, "HasSkin", "0");
            Xml.ValueTag(sb, indent, "BoneIndex", BoneIndex.ToString());
            Xml.ValueTag(sb, indent, "Unknown1", SkeletonBindUnk1.ToString());

            if (Geometries.Items != null)
            {
                Xml.OpenTag(sb, indent, "Geometries");
                foreach (var m in Geometries.Items)
                {
                    Xml.OpenTag(sb, indent + 1, "Item");
                    m.WriteXml(sb, indent + 2, center);
                    Xml.CloseTag(sb, indent + 1, "Item");
                }
                Xml.CloseTag(sb, indent, "Geometries");
            }
        }

        public override string ToString()
        {
            var geocount = Geometries.Items?.Length ?? 0;
            return "(" + geocount.ToString() + " geometr" + (geocount != 1 ? "ies)" : "y)");
        }
    }

    [TC(typeof(EXP))]
    public class Rsc5DrawableGeometry : Mesh, Rsc5Block
    {
        public ulong BlockLength
        {
            get
            {
                ulong l = 152;
                var boneIds = BoneIds.Items;
                if (boneIds != null)
                {
                    if (boneIds.Length > 4) l += 8;
                    l += (uint)(boneIds.Length) * 2;
                }
                return l;
            }
        }
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;

        public ulong VFT { get; set; } = 0x140618798;
        public ulong Unknown_8h; // 0x0000000000000000
        public ulong Unknown_10h; // 0x0000000000000000
        public Rsc5Ptr<Rsc5VertexBuffer> VertexBuffer { get; set; }
        public ulong Unknown_20h; // 0x0000000000000000
        public ulong Unknown_28h; // 0x0000000000000000
        public ulong Unknown_30h; // 0x0000000000000000
        public Rsc5Ptr<Rsc5IndexBuffer> IndexBuffer { get; set; }
        public ulong Unknown_40h; // 0x0000000000000000
        public ulong Unknown_48h; // 0x0000000000000000
        public ulong Unknown_50h; // 0x0000000000000000
        public uint IndicesCount { get; set; }
        public uint TrianglesCount { get; set; }
        public ushort Unknown_62h = 3; // 0x0003 // indices per primitive (triangle)
        public uint Unknown_64h; // 0x00000000
        public Rsc5RawArr<ushort> BoneIds { get; set; }//data is embedded at the end of this struct
        public ushort BoneIdsCount { get; set; }
        public uint Unknown_74h; // 0x00000000
        public Rsc5RawArr<byte> VertexDataRef { get; set; }
        public ulong Unknown_80h; // 0x0000000000000000
        public ulong Unknown_88h; // 0x0000000000000000
        public ulong Unknown_90h; // 0x0000000000000000

        public Rsc5Shader ShaderRef { get; set; }
        public ushort ShaderID { get; set; }//read/written by parent model
        public BoundingBox4 AABB { get; set; }//read/written by parent model

        public Rsc5DrawableGeometry()
        {
        }

        public void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            Unknown_8h = reader.ReadUInt32();
            Unknown_10h = reader.ReadUInt32();
            VertexBuffer = reader.ReadPtr<Rsc5VertexBuffer>();
            Unknown_20h = reader.ReadUInt32();
            Unknown_28h = reader.ReadUInt32();
            Unknown_30h = reader.ReadUInt32();
            IndexBuffer = reader.ReadPtr<Rsc5IndexBuffer>();
            Unknown_40h = reader.ReadUInt32();
            Unknown_48h = reader.ReadUInt32();
            Unknown_50h = reader.ReadUInt32();
            IndicesCount = reader.ReadUInt32();
            TrianglesCount = reader.ReadUInt32();
            VertexCount = reader.ReadUInt16();
            Unknown_62h = reader.ReadUInt16();
            BoneIds = reader.ReadRawArrPtr<ushort>();
            VertexStride = reader.ReadUInt16();
            BoneIdsCount = reader.ReadUInt16();
            BoneIds = reader.ReadRawArrItems(BoneIds, BoneIdsCount, true);

            if (VertexBuffer.Item != null) //hack to fix stupid "locked" things
            {
                VertexLayout = VertexBuffer.Item?.Layout.Item?.VertexLayout;
                VertexData = Rpf3Crypto.Swap(VertexBuffer.Item.Data1.Items ?? VertexBuffer.Item.Data2.Items);

                if (VertexCount == 0)
                {
                    VertexCount = VertexBuffer.Item.VertexCount;
                }
            }

            //Swap CMLA axis + endianess
            byte[] numArray = VertexData;
            var elems = VertexLayout.Elements;
            var elemcount = elems.Length;

            for (int index = 0; index < numArray.Length; index += VertexStride)
            {
                for (int i = 0; i < elemcount; i++)
                {
                    var elem = elems[i];
                    int elemoffset = elem.Offset;

                    switch (elem.Format)
                    {
                        case VertexElementFormat.Float3:
                            var v3 = BufferUtil.ReadVector3(numArray, index + elemoffset);
                            Rpf3Crypto.WriteVector3AtIndex(v3, numArray, index + elemoffset);
                            break;
                        case VertexElementFormat.Float4:
                            var v4 = BufferUtil.ReadVector4(numArray, index + elemoffset);
                            Rpf3Crypto.WriteVector4AtIndex(v4, numArray, index + elemoffset);
                            break;
                        case VertexElementFormat.Dec3N: //10101012
                            var packed = BufferUtil.ReadUint(numArray, index + elemoffset);
                            var pv = FloatUtil.Dec3NToVector4(packed); //Convert Dec3N to Vector4
                            var np1 = FloatUtil.Vector4ToDec3N(new Vector4(pv.Z, pv.X, pv.Y, pv.W)); //Convert Vector4 back to Dec3N from CMLA axis
                            BufferUtil.WriteUint(numArray, index + elemoffset, np1);
                            break;
                        case VertexElementFormat.UShort2N: //Scale terrain UVs
                            Rpf3Crypto.ReadRescaleUShort2N(numArray, index + elemoffset);
                            break;
                        default:
                            break;
                    }
                }
            }

            Indices = IndexBuffer.Item?.Indices.Items;
            VertexData = numArray;
        }

        public void Write(Rsc5DataWriter writer)
        {
            VertexCount = VertexBuffer.Item != null ? VertexBuffer.Item.VertexCount : 0; //TODO: fix?
            VertexStride = (int)(VertexBuffer.Item != null ? VertexBuffer.Item.VertexStride : 0); //TODO: fix?
            IndicesCount = IndexBuffer.Item != null ? IndexBuffer.Item.IndicesCount : 0; //TODO: fix?
            TrianglesCount = IndicesCount / 3;
            throw new NotImplementedException();
        }

        public void WriteXml(StringBuilder sb, int indent, Vector3 center)
        {
            var aabbMin = new Vector4(Vector3.Subtract(AABB.Min.XYZ(), center), AABB.Min.W);
            var aabbMax = new Vector4(Vector3.Subtract(AABB.Max.XYZ(), center), AABB.Max.W);

            Xml.ValueTag(sb, indent, "ShaderIndex", ShaderID.ToString());
            Xml.SelfClosingTag(sb, indent, "BoundingBoxMin " + FloatUtil.GetVector4XmlString(aabbMin));
            Xml.SelfClosingTag(sb, indent, "BoundingBoxMax " + FloatUtil.GetVector4XmlString(aabbMax));

            if (VertexLayout != null)
            {
                Xml.OpenTag(sb, indent, "VertexBuffer");
                Xml.ValueTag(sb, indent, "Flags", "0");
                Xml.OpenTag(sb, indent, string.Format("Layout type=\"{0}\"", VertexBuffer.Item.Layout.Item.Types.ToString()).Replace("RDR1_1", "GTAV1").Replace("RDR1_2", "GTAV1"));
                var elems = VertexLayout.Elements;
                var elemcount = elems.Length;

                for (int i = 0; i < elemcount; i++)
                {
                    var elem = elems[i];
                    var name = TranslateToSollumz(elem.SemanticName);

                    if (string.IsNullOrEmpty(name))
                        continue;
                    if (elem.SemanticIndex > 0 || name == "Tangent")
                        continue;

                    if (name == "Colour" || name == "TexCoord")
                        Xml.SelfClosingTag(sb, indent + 1, name + elem.SemanticIndex);
                    else if ((name == "Tangent" || name == "BlendWeights" || name == "BlendIndices") && elem.SemanticIndex > 0)
                        Xml.SelfClosingTag(sb, indent + 1, name + elem.SemanticIndex);
                    else
                        Xml.SelfClosingTag(sb, indent + 1, name);
                }

                Xml.CloseTag(sb, indent, "Layout");
                Xml.OpenTag(sb, indent, "Data");

                var elemoffset = 0;
                for (int v = 0; v < VertexCount; v++)
                {
                    Xml.Indent(sb, indent + 1);
                    var formatedOutput = GenerateVertexData(v, center, ref elemoffset);
                    sb.Append(formatedOutput);
                    sb.AppendLine();
                }
                Xml.CloseTag(sb, indent, "Data");
                Xml.CloseTag(sb, indent, "VertexBuffer");
            }

            if (IndexBuffer.Item != null)
            {
                Xml.OpenTag(sb, indent, "IndexBuffer");
                Xml.WriteRawArray(sb, indent, "Data", Indices);
                Xml.CloseTag(sb, indent, "IndexBuffer");
            }
        }

        public string TranslateToSollumz(string name)
        {
            switch (name)
            {
                case "POSITION": return "Position";
                case "BLENDWEIGHTS": return "BlendWeights";
                case "BLENDINDICES": return "BlendIndices";
                case "NORMAL": return "Normal";
                case "COLOR": return "Colour";
                case "TEXCOORD": return "TexCoord";
                case "TANGENT": return "Tangent";
                case "BINORMAL": return "Binormal";
                default: return "";
            }
        }

        public string GenerateVertexData(int v, Vector3 center, ref int elemoffset)
        {
            if (VertexLayout == null)
                return "";

            var elems = VertexLayout.Elements;
            var elemcount = elems.Length;

            List<Vector3> vertexPositions = new List<Vector3>();
            List<Colour> vertexColors = new List<Colour>();
            List<Vector3> vertexNormals = new List<Vector3>();
            List<Vector4> vertexTangents = new List<Vector4>();
            List<Vector2> texCoords = new List<Vector2>();
            List<Colour> blendWeights = new List<Colour>();
            List<Colour> blendIndices = new List<Colour>();

            var sb = new StringBuilder();
            for (int i = 0; i < elemcount; i++)
            {
                var elem = elems[i];
                var index = elem.SemanticIndex;
                var elemsize = VertexElementFormats.GetSizeInBytes(elem.Format);

                if (elem.SemanticIndex > 0)
                {
                    elemoffset += elemsize;
                    continue;
                }

                switch (elem.SemanticName)
                {
                    case "TEXCOORD":
                        switch (elem.Format)
                        {
                            case VertexElementFormat.Half2:
                                var x = BitConverter.ToUInt16(VertexData, elemoffset);
                                var y = BitConverter.ToUInt16(VertexData, elemoffset + 2);
                                var half2 = new Half2(x, y);
                                texCoords.Add(new Vector2((float)half2.X, (float)half2.Y));
                                break;
                            case VertexElementFormat.Float2:
                                var xy = BufferUtil.ReadVector2(VertexData, elemoffset);
                                texCoords.Add(xy);
                                break;
                        }
                        break;

                    case "NORMAL":
                    case "POSITION":
                        var pos = BufferUtil.ReadVector3(VertexData, elemoffset);
                        pos = Vector3.Subtract(pos, center);
                        switch (elem.SemanticName)
                        {
                            case "NORMAL":
                                vertexNormals.Add(pos);
                                break;
                            case "POSITION":
                                vertexPositions.Add(pos);
                                break;
                        }
                        break;
                    case "TANGENT":
                        var pos4 = BufferUtil.ReadVector4(VertexData, elemoffset);
                        var center4 = new Vector4(center, pos4.W);
                        var tangents = Vector4.Subtract(pos4, center4);
                        vertexTangents.Add(tangents);
                        break;

                    case "BLENDWEIGHTS":
                    case "COLOR":
                        var color = BufferUtil.ReadColour(VertexData, elemoffset);
                        switch (elem.SemanticName)
                        {
                            case "BLENDWEIGHTS":
                                blendWeights.Add(color);
                                break;
                            case "COLOR":
                                vertexColors.Add(color);
                                break;
                        }
                        break;
                    default:
                        break;
                }
                elemoffset += elemsize;

                switch (elem.SemanticName)
                {
                    case "POSITION": sb.Append(string.Format("{0} {1} {2}   ", vertexPositions[index].X, vertexPositions[index].Y, vertexPositions[index].Z).Replace(",", ".")); break;
                    case "BLENDWEIGHTS": var bw = blendWeights[index].ToArray(); sb.Append(string.Format("{0} {1} {2} {3}   ", bw[0], bw[1], bw[2], bw[3])); break;
                    case "BLENDINDICES": var bi = blendIndices[index].ToArray(); sb.Append(string.Format("{0} {1} {2} {3}   ", bi[0], bi[1], bi[2], bi[3])); break;
                    case "NORMAL": sb.Append(string.Format("{0} {1} {2}   ", vertexNormals[index].X, vertexNormals[index].Y, vertexNormals[index].Z).Replace(",", ".")); break;
                    case "COLOR": var color = vertexColors[index].ToArray(); sb.Append(string.Format("{0} {1} {2} {3}   ", color[0], color[1], color[2], color[3])); break;
                    case "TEXCOORD": sb.Append(string.Format("{0} {1}   ", texCoords[index].X, texCoords[index].Y).Replace(",", ".")); break;
                    case "TANGENT": sb.Append(string.Format("{0} {1} {2} {3}   ", vertexTangents[index].X, vertexTangents[index].Y, vertexTangents[index].Z, vertexTangents[index].W).Replace(",", ".")); break;
                    default: break;
                }
            }
            return sb.ToString();
        }

        public void SetShaderRef(Rsc5Shader shader)
        {
            ShaderRef = shader;

            if (shader != null)
            {

                switch (new JenkHash(shader.ShaderName.Value))
                {
                    #region default
                    case 0xe4df46d5://"default"
                    case 0x2f4b79d0://"default_spec"
                    case 0xd4156c86://"default_detail"
                    case 0x15406984://"default_tnt"
                    case 0xc9aac531://"default_um"
                    case 0xeaabbc5a://"default_terrain_wet"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region normal
                    case 0x4f485502://"normal"
                    case 0x38dd00df://"normal_spec"
                    case 0xa6dd8d99://"normal_spec_detail_tnt"
                    case 0x5fb135d1://"normal_spec_pxm"
                    case 0x464a2606://"normal_spec_reflect_emissivenight"
                    case 0xd7c59e09://"normal_spec_detail"
                    case 0xf829494a://"normal_reflect"
                    case 0x1510ebe7://"normal_decal"
                    case 0x6473cd91://"normal_spec_decal_pxm"
                    case 0x6cd4735b://"normal_spec_tnt"
                    case 0x1c1c2570://"normal_spec_decal"
                    case 0x19074229://"normal_detail"
                    case 0x1b147031://"normal_tnt"
                    case 0x2143cd55://"normal_spec_reflect"
                    case 0x27a1bdb0://"normal_cubemap_reflect"
                    case 0xbfb3a6d2://"normal_spec_cubemap_reflect"
                    case 0x938e49f1://"normal_decal_tnt"
                    case 0xac8c0806://"normal_um"
                    case 0x6fbf8acf://"normal_um_tnt"
                    case 0x4697ea6f://"normal_spec_wrinkle"
                    case 0x71bacc0d://"normal_spec_decal_tnt"
                    case 0x5a242d91://"normal_diffspec_detail_dpm_tnt"
                    case 0x698b18e5://"normal_diffspec"
                    case 0x7a5b66f3://"normal_diffspec_detail"
                    case 0xb50b181d://"normal_diffspec_detail_dpm"
                    case 0x5f270f50://"normal_spec_detail_dpm_vertdecal_tnt"
                    case 0x848f3c54://"normal_reflect_decal"
                    case 0x12a99bc9://"normal_spec_detail_dpm"
                    case 0x9843e682://"normal_diffspec_tnt"
                    case 0x126d79a2://"normal_spec_um"
                    case 0xbaac9b33://"normal_spec_dpm"
                    case 0x002195ab://"normal_decal_pxm"
                    case 0xd503eed2://"normal_pxm"
                    case 0xdd131a87://"normal_detail_dpm"
                    case 0xe3417217://"normal_spec_decal_detail"
                    case 0xaa650daf://"normal_spec_emissive"
                    case 0xc63d63f8://"normal_spec_pxm_tnt"
                    case 0x2f8bc40d://"normal_spec_batch"
                    case 0x4bb15898://"normal_spec_detail_dpm_tnt"
                    case 0x2248a6bf://"normal_diffspec_detail_tnt"
                    case 0x7c11bcba://"normal_terrain_wet"
                    case 0xfc64670a://"normal_pxm_tnt"
                    case 0x955e9c3c://"normal_spec_decal_nopuddle"
                    case 0x0099a5d5://"normal_decal_pxm_tnt"
                    case 0xf5d7a727://"normal_spec_reflect_decal"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region spec
                    case 0x7204a391://"spec"
                    case 0xed86862a://"spec_reflect"
                    case 0xa4f28a79://"spec_decal"
                    case 0xbf4b354a://"spec_tnt"
                    case 0x48eb3653://"spec_reflect_decal"
                    case 0x75b0706e://"spec_twiddle_tnt"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region decal
                    case 0xf08729d4://"decal"
                    case 0xc8d38fcc://"decal_normal_only"
                    case 0x14e49f72://"decal_tnt"
                    case 0x37c6e600://"decal_glue"
                    case 0xe8f01193://"decal_spec_only"
                    case 0x1f98bd87://"decal_emissive_only"
                    case 0xd94d6305://"decal_dirt"
                    case 0x1ab91784://"decal_diff_only_um"
                    case 0xd5738c1b://"decal_emissivenight_only"
                    case 0xae556d0e://"decal_amb_only"
                    case 0x56a60f25://"decal_shadow_only"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region cutout
                    case 0x4fa432a7://"cutout_fence"
                    case 0x063762cf://"cutout_fence_normal"
                    case 0x56a2ae25://"cutout_hard"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region emissive
                    case 0x8f245469://"emissive"
                    case 0xb08c6435://"emissivenight"
                    case 0xe3a69ef4://"emissive_speclum"
                    case 0xb44fd60e://"emissive_additive_alpha"
                    case 0x0c3a3e76://"emissivestrong"
                    case 0x15c92c35://"emissive_tnt"
                    case 0x92c006a9://"emissive_clip"
                    case 0x21abe462://"emissivenight_geomnightonly"
                    case 0x97b4896e://"emissive_additive_uv_alpha"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region parallax
                    case 0x20c330f5://"parallax"
                    case 0xa9c79955://"parallax_specmap"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region reflect
                    case 0x98bc393e://"reflect"
                    case 0x45f9ed91://"reflect_decal"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region mirror
                    case 0x0b316929://"mirror_default"
                    case 0xfe999a18://"mirror_decal"
                    case 0x6020a5f6://"mirror_crack"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region ped
                    case 0x34d90761://"ped"
                    case 0x297b46e9://"ped_default"
                    case 0x66d4abf7://"ped_fur"
                    case 0x603f1257://"ped_wrinkle"
                    case 0x4221fa89://"ped_hair_spiked"
                    case 0x934fa33f://"ped_hair_cutout_alpha"
                    case 0xcfdc0d22://"ped_decal"
                    case 0x44789aa6://"ped_alpha"
                    case 0x1e2ec776://"ped_cloth"
                    case 0x6d77de1b://"ped_decal_exp"
                    case 0xdb41829e://"ped_enveff"
                    case 0x117e38ad://"ped_default_enveff"
                    case 0x512410f2://"ped_wrinkle_cs"
                    case 0xccdf1b33://"ped_decal_nodiff"
                    case 0x936879cc://"ped_default_cloth"
                    case 0x846c045e://"ped_wrinkle_cloth"
                    case 0xa91dbbce://"ped_palette"
                    case 0x1c92a859://"ped_default_palette"
                    case 0x4c10b625://"ped_decal_decoration"
                    case 0x9baf37fa://"ped_nopeddamagedecals"
                    case 0x243d02ae://"ped_default_mp"
                    case 0x83fb2c58://"ped_wrinkle_enveff"
                    case 0x9bbed9d0://"ped_wrinkle_cloth_enveff"
                    case 0xaa0cc812://"ped_cloth_enveff"
                    case 0x6f83f1e2://"ped_emissive"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region cloth
                    case 0x5b8159ec://"cloth_default"
                    case 0xbdc29ccc://"cloth_normal_spec"
                    case 0x95536566://"cloth_spec_alpha"
                    case 0xc92cbed1://"cloth_normal_spec_tnt"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region weapon
                    case 0x8676a645://"weapon_normal_spec_tnt"
                    case 0x5ff02c23://"weapon_normal_spec_detail_palette"
                    case 0x2098ad32://"weapon_normal_spec_cutout_palette"
                    case 0x3dc5551f://"weapon_normal_spec_alpha"
                    case 0x59b24d3d://"weapon_emissivestrong_alpha"
                    case 0x9905a1ed://"weapon_normal_spec_palette"
                    case 0x71a93f43://"weapon_normal_spec_detail_tnt"
                    case 0x642cfd79://"weapon_emissive_tnt"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region vehicle
                    case 0x1d5f09ce://"vehicle_tire"
                    case 0xf9fb7331://"vehicle_paint1"
                    case 0x2327939b://"vehicle_paint1_enveff"
                    case 0xede85b0b://"vehicle_paint2"
                    case 0x0fe76347://"vehicle_paint2_enveff"
                    case 0xe029bf8e://"vehicle_paint3"
                    case 0xec8e929d://"vehicle_paint3_enveff"
                    case 0x982de28e://"vehicle_paint3_lvr"
                    case 0xc0cb80d2://"vehicle_paint4"
                    case 0xaba4513d://"vehicle_paint4_enveff"
                    case 0x72d0ac88://"vehicle_paint4_emissive"
                    case 0xe498076b://"vehicle_paint5_enveff"
                    case 0xa17c4234://"vehicle_paint6"
                    case 0xc30069b8://"vehicle_paint6_enveff"
                    case 0x953229a0://"vehicle_paint7"
                    case 0x6e94ccc6://"vehicle_paint7_enveff"
                    case 0x86598bef://"vehicle_paint8"
                    case 0x77f6ef2a://"vehicle_paint9"
                    case 0xd963b58b://"vehicle_mesh"
                    case 0xed5d39c0://"vehicle_mesh_enveff"
                    case 0xa62a444e://"vehicle_mesh2_enveff"
                    case 0xc9866cc2://"vehicle_vehglass_inner"
                    case 0xf5579485://"vehicle_interior"
                    case 0x2a92aee4://"vehicle_interior2"
                    case 0x0a853c1a://"vehicle_shuts"
                    case 0xe515a6e7://"vehicle_lightsemissive"
                    case 0xffe6fbea://"vehicle_badges"
                    case 0x8a7a2bef://"vehicle_licenseplate"
                    case 0x0f8bd089://"vehicle_dash_emissive"
                    case 0x7c98d207://"vehicle_vehglass"
                    case 0x79218a98://"vehicle_dash_emissive_opaque"
                    case 0x60d16e80://"vehicle_detail"
                    case 0x1fa3ecee://"vehicle_detail2"
                    case 0x57fd87b6://"vehicle_decal"
                    case 0x7a016de7://"vehicle_decal2"
                    case 0x2a996272://"vehicle_blurredrotor"
                    case 0xd387e65d://"vehicle_blurredrotor_emissive"
                    case 0xedbc397a://"vehicle_basic"
                    case 0x833b47f2://"vehicle_emissive_opaque"
                    case 0x5e1a9cb5://"vehicle_emissive_alpha"
                    case 0x39960ea5://"vehicle_track"
                    case 0x2932ddcc://"vehicle_track_ammo"
                    case 0x84fb9684://"vehicle_track_emissive"
                    case 0xf36cfb59://"vehicle_track2"
                    case 0x0fa9ece2://"vehicle_track2_emissive"
                    case 0x9341b630://"vehicle_cloth"
                    case 0x5c4bce0d://"vehicle_cloth2"
                    case 0x2ce663ab://"vehicle_generic"
                    case 0x2239ab26://"vehicle_cutout"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region sky
                    case 0xbd37f63e://"sky_system"
                    case 0xe013c993://"clouds_animsoft"
                    case 0x727a5985://"clouds_altitude"
                    case 0x68f5078b://"clouds_fast"
                    case 0x1f9fb43b://"clouds_anim"
                    case 0xf45250cd://"clouds_soft"
                    case 0x3672ee6d://"clouds_fog"
                        SetupSkyShader(shader);
                        break;
                    #endregion
                    #region water
                    case 0x07569ca0://"water_fountain"
                    case 0xe585594f://"water_shallow"
                    case 0x38b8a4bb://"water_river"
                    case 0x81cc67b3://"water_riverocean"
                    case 0xdb72e78f://"water_riverlod"
                    case 0xc01539a0://"water_terrainfoam"
                    case 0x489b68b7://"water_poolenv"
                    case 0x0b273b78://"water_riverfoam"
                    case 0x938e5d80://"water_rivershallow"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region glass
                    case 0x85aba20b://"glass_env"
                    case 0xc5098ee2://"glass_pv_env"
                    case 0x629c08dc://"glass_displacement"
                    case 0xa587608a://"glass_normal_spec_reflect"
                    case 0xd9f8c9bf://"glass"
                    case 0x1a910b87://"glass_emissive"
                    case 0x706a3722://"glass_pv"
                    case 0xa9a6eb84://"glass_spec"
                    case 0xb680ab5c://"glass_reflect"
                    case 0xb8c7819b://"glass_breakable"
                    case 0x451700e5://"glass_emissivenight"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    #region grass
                    case 0x72623394://"grass"
                    case 0x3627511f://"grass_fur"
                    case 0xdaa2be7f://"grass_fur_mask"
                    case 0x10d73576://"grass_batch"
                        SetupGrassShader(shader);
                        break;
                    #endregion
                    #region trees
                    case 0xa157712e://"trees_lod"
                    case 0x646a7334://"trees_lod_tnt"
                    case 0x5791780f://"trees"
                    case 0x78b69914://"trees_tnt"
                    case 0xe31afeb8://"trees_normal"
                    case 0x970ad6ac://"trees_normal_spec"
                    case 0x0aa15ef3://"trees_normal_diffspec"
                    case 0x2e42b9e2://"trees_normal_spec_tnt"
                    case 0x4fa7bbcd://"trees_normal_diffspec_tnt"
                    case 0xb60ae4d9://"trees_shadow_proxy"
                    case 0x6fc19de0://"trees_lod2" //this probably needs its own billboard shader
                        SetupTreesShader(shader);
                        break;
                    #endregion
                    #region terrain
                    case 0x08327e14://"terrain_cb_w_4lyr_lod"
                    case 0x26f44b20://"terrain_cb_w_4lyr_2tex_blend_pxm_spm"
                    case 0x9727947c://"terrain_cb_w_4lyr_2tex_blend_lod"
                    case 0xf4b9c22c://"terrain_cb_w_4lyr_pxm"
                    case 0xb5dc8364://"terrain_cb_w_4lyr"
                    case 0x9b081dc2://"terrain_cb_w_4lyr_spec_pxm"
                    case 0x943081a5://"terrain_cb_w_4lyr_2tex_blend_pxm"
                    case 0x8a0b759d://"terrain_cb_w_4lyr_2tex_blend"
                    case 0xcab475d5://"terrain_cb_w_4lyr_pxm_spm"
                    case 0x26894ef4://"terrain_cb_w_4lyr_spec"
                    case 0xb989de51://"terrain_cb_w_4lyr_2tex"
                    case 0x8f3df5bd://"terrain_cb_4lyr_2tex"
                    case 0xec585e67://"terrain_cb_w_4lyr_cm_pxm_tnt"
                    case 0x18e4a4a5://"terrain_cb_w_4lyr_cm_tnt"
                    case 0x119d5b03://"terrain_cb_w_4lyr_cm"
                    case 0xfd51e359://"terrain_cb_w_4lyr_spec_int"
                    case 0x8355bdd9://"terrain_cb_4lyr"
                    case 0x708f32fa://"terrain_cb_w_4lyr_2tex_pxm"
                    case 0xf98200c6://"terrain_cb_w_4lyr_cm_pxm"
                    case 0xe8aa4c60://"terrain_cb_w_4lyr_spec_int_pxm"
                    case 0x2caafe59://"blend_2lyr"
                        SetupTerrainShader(shader);
                        break;
                    #endregion
                    #region misc
                    case 0x01605013://"cable"
                    case 0xb8f9baf0://"radar"
                    case 0x4ba72c6f://"ptfx_model"
                    case 0xe5d7af85://"cpv_only"
                    case 0x3d88f3f5://"minimap"
                    case 0xe05f1a42://"distance_map"
                    case 0xb0f74d23://"albedo_alpha"
                        SetupDefaultShader(shader);
                        break;
                    #endregion
                    default:
                        SetupDefaultShader(shader);
                        break;
                }


                var bucket = shader.DrawBucket;
                switch (bucket)
                {
                    case 0: ShaderBucket = ShaderBucket.Solid; break;//solid
                    case 1: ShaderBucket = ShaderBucket.Alpha1; break;//alpha 
                    case 2: ShaderBucket = ShaderBucket.Decal1; break;//decal
                    case 3: ShaderBucket = ShaderBucket.Alpha1; break;//cutout
                    case 6: ShaderBucket = ShaderBucket.Alpha1; break;//water
                    case 7: ShaderBucket = ShaderBucket.Alpha1; break;//glass
                    default:
                        break;
                }
            }
        }

        private void SetupDefaultShader(Rsc5Shader s)
        {
            SetDefaultShader();
            ShaderInputs = Shader.CreateShaderInputs();
            ShaderInputs.SetFloat(0xDF918855, 1.0f);//"BumpScale"
            ShaderInputs.SetUInt32(0x249983FD, 5); //"ParamsMapConfig"
            ShaderInputs.SetFloat(0x4D52C5FF, 1.0f); //"AlphaScale"
            ShaderInputs.SetFloat4(0x1C2824ED, Vector4.One);//"NoiseSettings"

            if (s == null || s.Params == null)
                return;

            Textures = new Texture[5];
            var sfresnel = 0.96f;
            var sintensitymult = 0.3f;
            var sfalloffmult = 50.0f;
            var sintmask = 1.0f;

            for (int p = 0; p < s.Params.Length; p++)
            {
                var parm = s.Params[p];
                if (parm.Type == 0)
                {
                    var tex = parm.Texture;
                    if (tex != null)
                    {
                        switch (parm.Hash)
                        {
                            #region used textures
                            case 0xF1FE2B71://diffusesampler
                            case 0x50022388://platebgsampler
                            case 0x1cf5b657://texturesamp
                            case 0x605fcc60://distancemapsampler
                                Textures[0] = tex;
                                break;
                            case 0x46B7C64F://bumpsampler
                            case 0x65DF0BCE://platebgbumpsampler
                            case 0x8ac11cb0://normalsampler
                                Textures[1] = tex;
                                break;
                            case 0x608799C6://specsampler
                                Textures[2] = tex;
                                break;
                            case 0x485F22B0://flowsampler
                            case 0x991ECEBE://fogsampler
                            case 0xC2B08918://foamsampler
                                if (Textures[0] == null) Textures[0] = tex;
                                break;
                            case 0xca4299e4://detailsampler
                                Textures[3] = tex;
                                break;
                            case 0xf648a067://tintpalettesampler
                                tex.Sampler = TextureSampler.Create(TextureSamplerFilter.Point, SharpDX.Direct3D11.TextureAddressMode.Wrap);
                                Textures[4] = tex;
                                ShaderInputs.SetUInt32(0x2905BED7, 1);//"ColourTintMode"
                                break;
                            #endregion
                            #region unused textures
                            case 0x88cc3d90://lookupsampler
                            case 0xf165e62b://heightsampler
                            case 0xc5bbae28://environmentsampler
                            case 0x037eb699://diffuseextrasampler
                            case 0x86cb389d://wrinklemasksampler_0
                            case 0xd4a3d449://wrinklemasksampler_1
                            case 0xb0660bce://wrinklemasksampler_2
                            case 0xbda82652://wrinklemasksampler_3
                            case 0x883768d9://wrinklesampler_a
                            case 0x09bbebe0://wrinklesampler_b
                            case 0x0ad3a268://diffusesampler2
                            case 0x2f0b625e://volumesampler
                            case 0x92bc3625://noisesampler
                            case 0x7f354429://anisonoisespecsampler
                            case 0xbbf891f0://snowsampler
                            case 0x0be242c5://wrinklemasksampler_4
                            case 0x1dbbe678://wrinklemasksampler_5
                            case 0xab98831e://texturesamplerdiffpal
                            case 0xb1dd9ffc://gmirrorcracksampler
                            case 0x7e9a27fe://dirtsampler
                            case 0x7a141f5c://fontsampler
                            case 0xbb302c18://fontnormalsampler
                            case 0x0294945c://snowsampler0
                            case 0x11a73281://snowsampler1
                            case 0x70e23e69://"bumpsampler2"
                            case 0xbc38845d://??? blank_normal
                            case 0x55393736://???  lowrider_lvr_1  (vehicle_paint3_lvr)
                                break;
                            #endregion
                            default:
                                break;
                        }
                    }
                }
                else
                {

                    switch (parm.Hash)
                    {
                        #region default
                        case 0xf6712b81://"bumpiness"
                            ShaderInputs.SetFloat(0xDF918855, parm.Vector.X);//"BumpScale"
                            break;
                        case 0x27b9b2fa://"specularfresnel"         //~0.3-1, low for metals, ~0.96 for nonmetals
                            sfresnel = parm.Vector.X;
                            break;
                        case 0xf418334f://"specularintensitymult"   //0-1, final multiplier?
                            sintensitymult = parm.Vector.X;
                            break;
                        case 0x87744680://"specularfalloffmult"     //10-150+?, higher is shinier
                            sfalloffmult = parm.Vector.X;
                            break;
                        case 0xff11711d://"specmapintmask"          //always 1?
                            sintmask = parm.Vector.X;
                            break;
                        case 0x5eebed48://"emissivemultiplier"
                            ShaderInputs.SetFloat(0x83DDF493, FloatUtil.Saturate(parm.Vector.X * 0.025f));//"EmissiveMult"
                            break;
                        case 0x32dd9ff5://"wetnessmultiplier"       //indication of porosity?
                        case 0xb51e2e8f://"detailsettings"
                        case 0xe9437406://"hardalphablend"
                        case 0x4620a35d://"usetessellation"
                        case 0xba54c190://"globalanimuv1"
                        case 0xd79bfc1e://"globalanimuv0"
                        case 0x948a54f2://"matmaterialcolorscale"
                        case 0xfdd796cf://"tintpaletteselector"
                        case 0x0e710045://"parallaxselfshadowamount"
                        case 0x13ba4503://"heightbias"
                        case 0x38757622://"heightscale"
                        case 0x3bc8669f://"reflectivepower"
                        case 0xc72ea263://"umglobaloverrideparams"
                        case 0x21ffda1a://"umglobalparams"
                        case 0xb3f978f0://"specdesaturateexponent"
                        case 0x2ab72b48://"specdesaturateintensity"
                        case 0x73529a7c://"tessellationmultiplier"
                        case 0x81db4c55://"parallaxscalebias"
                        case 0xdbb6bf5b://"ambientdecalmask"
                            break;
                        #endregion
                        #region radar
                        case 0x614ed177://"clippingplane"
                        case 0x5a7625df://"diffusecol"
                            break;
                        #endregion;
                        #region cable
                        case 0xc5574322://"alphatestvalue"
                        case 0xd4108f2c://"gcableparams"
                        case 0xf5d26f89://"gviewproj"
                        case 0x07de6684://"shader_cableemissive"
                        case 0x974c6f03://"shader_cableambient"
                        case 0x06e11655://"shader_cablediffuse2"
                        case 0x6ae0586a://"shader_cablediffuse"
                        case 0x01fb89df://"shader_windamount"
                        case 0x8ff24649://"shader_fadeexponent"
                        case 0x84a43fbb://"shader_radiusscale"
                            break;
                        #endregion
                        #region glass
                        case 0xb84d48a2://"decaltint"
                        case 0xc7503fa9://"crackdecalbumpalphathreshold"
                        case 0x80773567://"crackdecalbumpamount"
                        case 0xcbdbf631://"crackedgebumpamount"
                        case 0x2b8ea03b://"crackdecalbumptilescale"
                        case 0x1d5b899b://"crackedgebumptilescale"
                        case 0x5106c3fb://"brokenspecularcolor"
                        case 0x345a4189://"brokendiffusecolor"
                        case 0x4552bd35://"displparams"
                            break;
                        #endregion
                        #region mirror
                        case 0x1d9dfdb6://"gmirrordebugparams"
                        case 0x707c72c5://"gmirrorbounds"
                        case 0x3ad6afa7://"gmirrordistortionamount"
                        case 0x5d1b34f4://"gmirrorcrackamount"
                            break;
                        #endregion
                        #region dirt
                        case 0x3e95fa90://"dirtdecalmask"
                            break;
                        #endregion
                        #region weapon
                        case 0x28e1926b://"specular2color"
                        case 0xea3526e6://"specular2colorintensity"
                        case 0x257df714://"specular2factor"
                        case 0x9d226f7c://"paletteselector"
                            break;
                        #endregion
                        #region wrinkle
                        case 0x49187ebc://"wrinklemaskstrengths3"
                        case 0x008f6da7://"wrinklemaskstrengths2"
                        case 0xee05c894://"wrinklemaskstrengths1"
                        case 0xdc5ba540://"wrinklemaskstrengths0"
                            break;
                        #endregion
                        #region ped
                        case 0x1a06bf4b://"envefffatthickness"
                        case 0xee2fc969://"stubblecontrol"
                        case 0xcff5eadf://"furbendparams"
                        case 0x1a6b71d3://"furglobalparams"
                        case 0x947c2a14://"furattencoef"
                        case 0xf0017cb5://"furaoblend"
                        case 0x7f644b1b://"furstiffness"
                        case 0xb85f80b0://"furselfshadowmin"
                        case 0x3c20327a://"furnoiseuvscale"
                        case 0x553e1d35://"furlength"
                        case 0x0f21cc6b://"furmaxlayers"
                        case 0xb3485fe4://"furminlayers"
                        case 0x6063ce32://"ordernumber"
                        case 0x15a413a7://"anisotropicalphabias"
                        case 0xcefe3244://"specularnoisemapuvscalefactor"
                        case 0x63c2fc31://"anisotropicspecularcolour"
                        case 0x1dccadc4://"anisotropicspecularexponent"
                        case 0x17f0c457://"anisotropicspecularintensity"
                            break;
                        #endregion
                        #region water
                        case 0xe9c969fb://"fogcolor"
                        case 0xebdba3f6://"specularfalloff"
                        case 0xa95fc535://"specularintensity"
                        case 0xd3cd3e65://"ripplescale"
                        case 0xb9470b30://"ripplebumpiness"
                        case 0x45e94323://"ripplespeed"
                        case 0xaef7b610://"heightopacity"
                        case 0x28c2377e://"wavemovement"
                        case 0x1074f838://"waterheight"
                        case 0x88e19e2f://"waveoffset"
                            break;
                        #endregion
                        #region distance_map (signs)
                        case 0xb3a79c9a://"fillcolor"
                            break;
                        #endregion
                        #region batch
                        case 0x407f8395://"glodfadetilescale"
                        case 0x9eb9f4b4://"glodfadepower"
                        case 0xc28f4fc7://"glodfaderange"
                        case 0x742c34cb://"glodfadestartdist"
                        case 0x5d82554d://"vecbatchaabbmin"
                                        //case 0xfd3d498c://"gscalerange"
                                        //case 0xa186f243://"vecbatchaabbdelta"
                            break;
                        #endregion
                        #region vehicle
                        case 0xdac8f5e7://"envefftextileuv"
                        case 0x90f4fd1b://"enveffscale"
                        case 0xa114f369://"enveffthickness"
                        case 0xa2e041b8://"specular2color_dirlerp"
                        case 0x44546346://"dirtcolor"
                        case 0xec247f19://"dirtlevelmod"
                        case 0x185f047c://"matdiffusecolor"
                        case 0x7be0df5b://"matwheelworldviewproj"
                        case 0xbd720b18://"matwheelworld"
                        case 0xf38696b0://"tyredeformparams2"
                        case 0x5dc44eb2://"tyredeformparams"
                        case 0x505f2850://"damagedwheeloffsets"
                        case 0x0041e541://"damagetextureoffset"
                        case 0x168b499d://"damagemultiplier"
                        case 0x84f7d5d9://"boundradius"
                        case 0xf948e930://"dimmersetpacked"
                        case 0x455bbba5://"distepsilonscalemin"
                        case 0x601e5d15://"distmapcenterval"
                        case 0x0572eb1b://"fontnormalscale"
                        case 0x6e12f49e://"licenseplatefonttint"
                        case 0xb3c19d6b://"licenseplatefontextents"
                        case 0x4dadde1a://"numletters"
                        case 0x59495f93://"lettersize"
                        case 0x339b7281://"letterindex2"
                        case 0x6aee612a://"letterindex1"
                        case 0x6a99768c://"diffuse2specmod"
                        case 0x5e0e088c://"matdiffusecolor2"
                        case 0x5549d131://"spectextileuv"
                        case 0xa7d8c84b://"diffusetextileuv"
                        case 0xe019fd7a://"trackanimuv"
                        case 0xd9000d9f://???
                        case 0xed3f35ee://???
                        case 0x3d19b44d://???
                            break;
                        #endregion

                        default:
                            break;
                    }
                }
            }

            ShaderInputs.SetFloat(0xDA9702A9, FloatUtil.Saturate(sintensitymult * (1.0f - ((sfresnel - 0.1f) / 0.896f))));//"MeshMetallicity"
            ShaderInputs.SetFloat(0x92176B1A, FloatUtil.Saturate(0.0f));// ((sfalloffmult)-10.0f) / 500.0f));//"MeshSmoothness"
            ShaderInputs.SetFloat(0x57C22E45, FloatUtil.Saturate(sintensitymult));//"MeshParamsMult"

            var db = s.DrawBucket;
            if (db == 2)
            {
                var decalMasks = Vector4.One; //albedo, normal, params, irradiance
                var decalMode = 1u;
                switch (new JenkHash(s.ShaderName.Value))
                {
                    case 0xf08729d4://"decal"
                    case 0x14e49f72://"decal_tnt"
                    case 0x37c6e600://"decal_glue"
                    case 0xd94d6305://"decal_dirt"
                    case 0x1510ebe7://"normal_decal"
                    case 0x938e49f1://"normal_decal_tnt"
                    case 0x002195ab://"normal_decal_pxm"
                    case 0x0099a5d5://"normal_decal_pxm_tnt"
                    case 0x1c1c2570://"normal_spec_decal"
                    case 0x6473cd91://"normal_spec_decal_pxm"
                    case 0x71bacc0d://"normal_spec_decal_tnt"
                    case 0xe3417217://"normal_spec_decal_detail"
                    case 0x955e9c3c://"normal_spec_decal_nopuddle"
                    case 0xf5d7a727://"normal_spec_reflect_decal"
                    case 0x848f3c54://"normal_reflect_decal"
                    case 0xa4f28a79://"spec_decal"
                    case 0x48eb3653://"spec_reflect_decal"
                    case 0x45f9ed91://"reflect_decal"
                    case 0xc01539a0://"water_terrainfoam"
                    case 0xcfdc0d22://"ped_decal"
                    case 0x6d77de1b://"ped_decal_exp"
                    case 0x4c10b625://"ped_decal_decoration"
                    case 0xe05f1a42://"distance_map"
                    case 0x4f485502://"normal"
                    case 0x7a016de7://"vehicle_decal2"
                        break;
                    case 0x1ab91784://"decal_diff_only_um"
                        decalMasks = new Vector4(1, 0, 0, 0);
                        break;
                    case 0xc8d38fcc://"decal_normal_only"
                        decalMasks = new Vector4(0, 1, 0, 0);
                        break;
                    case 0xe8f01193://"decal_spec_only"
                        decalMasks = new Vector4(0, 0, 1, 0);
                        break;
                    case 0xae556d0e://"decal_amb_only"
                        decalMasks = new Vector4(0, 0, 0, 1);
                        break;
                    case 0x1f98bd87://"decal_emissive_only"
                    case 0xd5738c1b://"decal_emissivenight_only" //is this right?
                        decalMasks = new Vector4(1, 0, 0, 1);
                        break;
                    case 0xccdf1b33://"ped_decal_nodiff"
                        decalMasks = new Vector4(0, 1, 1, 1);
                        break;
                    case 0x56a60f25://"decal_shadow_only"
                        decalMasks = new Vector4(0, 0, 0, 1); //what is this exactly?
                        break;
                    default:
                        break;
                }
                ShaderInputs.SetFloat4(0x5C3AB6E9, decalMasks); //"DecalMasks"
                ShaderInputs.SetUInt32(0x0188ECE8, decalMode);  //"DecalMode"
            }
            if (db == 3)
            {
                ShaderInputs.SetUInt32(0x26E8F6BB, 1); //"NormalDoubleSided"  //flip normals if they are facing away from the camera
            }

        }

        private void SetupTerrainShader(Rsc5Shader s)
        {
            SetCoreShader<BlendShader>(ShaderBucket.Solid);
            ShaderInputs = Shader.CreateShaderInputs();
            ShaderInputs.SetFloat4(0x7CB163F5, Vector4.Zero);//"BumpScales"
            ShaderInputs.SetFloat4(0xAD966CCC, new Vector4(1, 1, 0, 0));//"UVScaleOffset"
            ShaderInputs.SetFloat4(0x640811D6, Vector4.One);//"UVIndices"
            ShaderInputs.SetFloat4(0x401BDDBB, Vector4.One);//"UVLookupIndex"
            ShaderInputs.SetFloat4(0xA83AA336, new Vector4(0.0f));//"LODColourLevels" //actually tintPaletteSelector

            uint blendMode = 1;
            switch (new JenkHash(s.ShaderName.Value))
            {
                case 0x08327e14://"terrain_cb_w_4lyr_lod"
                    blendMode = 1;
                    break;
                case 0xb989de51://"terrain_cb_w_4lyr_2tex"
                case 0x8a0b759d://"terrain_cb_w_4lyr_2tex_blend"
                case 0x9727947c://"terrain_cb_w_4lyr_2tex_blend_lod"
                case 0x26f44b20://"terrain_cb_w_4lyr_2tex_blend_pxm_spm"
                case 0x943081a5://"terrain_cb_w_4lyr_2tex_blend_pxm"
                case 0x708f32fa://"terrain_cb_w_4lyr_2tex_pxm"
                case 0xb5dc8364://"terrain_cb_w_4lyr"
                case 0x26894ef4://"terrain_cb_w_4lyr_spec"
                case 0x9b081dc2://"terrain_cb_w_4lyr_spec_pxm"
                case 0xf4b9c22c://"terrain_cb_w_4lyr_pxm"
                case 0xcab475d5://"terrain_cb_w_4lyr_pxm_spm"
                case 0xfd51e359://"terrain_cb_w_4lyr_spec_int"
                case 0xe8aa4c60://"terrain_cb_w_4lyr_spec_int_pxm"
                    blendMode = 2;
                    break;
                case 0x119d5b03://"terrain_cb_w_4lyr_cm"
                case 0xf98200c6://"terrain_cb_w_4lyr_cm_pxm"
                    blendMode = 3;
                    break;
                case 0x18e4a4a5://"terrain_cb_w_4lyr_cm_tnt"
                case 0xec585e67://"terrain_cb_w_4lyr_cm_pxm_tnt"
                    blendMode = 4;
                    break;
                case 0x8355bdd9://"terrain_cb_4lyr"
                case 0x8f3df5bd://"terrain_cb_4lyr_2tex"
                case 0x2caafe59://"blend_2lyr"
                    blendMode = 1;
                    break;
                default:
                    break;
            }
            ShaderInputs.SetUInt32(0x9B920BD, blendMode);//"BlendMode"

            if (s == null || s.Params == null)
                return;

            Textures = new Texture[16]; //albedo(0-3); normal(0-3); params(0-3); masks; lodcolour; lodnorm; height;
            for (int p = 0; p < s.Params.Length; p++)
            {
                var parm = s.Params[p];
                if (parm.Type == 0)
                {
                    var tex = parm.Texture;
                    if (tex != null)
                    {
                        switch (parm.Hash)
                        {
                            case 0xd52b11df://texturesampler_layer0
                                Textures[0] = tex;
                                break;
                            case 0x2420afd1://texturesampler_layer1
                                Textures[1] = tex;
                                break;
                            case 0x31934ab6://texturesampler_layer2
                                Textures[2] = tex;
                                break;
                            case 0x78b758fd://texturesampler_layer3
                                Textures[3] = tex;
                                break;
                            case 0x3fff9563://bumpsampler_layer0
                                Textures[4] = tex;
                                break;
                            case 0x54cdbeff://bumpsampler_layer1
                                Textures[5] = tex;
                                break;
                            case 0xa3a2dca8://bumpsampler_layer2
                                Textures[6] = tex;
                                break;
                            case 0xb1597815://bumpsampler_layer3
                                Textures[7] = tex;
                                break;
                            case 0x2e8e5039://heightmapsamplerlayer0
                                Textures[8] = tex;
                                break;
                            case 0x9936a58c://heightmapsamplerlayer1
                                Textures[9] = tex;
                                break;
                            case 0x8be08ae0://heightmapsamplerlayer2
                                Textures[10] = tex;
                                break;
                            case 0x85b0fe81://heightmapsamplerlayer3
                                Textures[11] = tex;
                                break;

                            case 0x88cc3d90://lookupsampler
                                Textures[12] = tex;
                                ShaderInputs.SetUInt32(0xC1F6297F, 1);//"MasksMode"
                                break;
                            case 0xf648a067://tintpalettesampler
                                tex.Sampler = TextureSampler.Create(TextureSamplerFilter.Point, SharpDX.Direct3D11.TextureAddressMode.Wrap);
                                Textures[13] = tex;
                                break;

                            default:
                                break;
                        }
                    }
                }
                else
                {

                    switch (parm.Hash)
                    {
                        case 0xf6712b81://"bumpiness"
                            ShaderInputs.SetFloat4(0x7CB163F5, new Vector4(parm.Vector.X));//"BumpScales"
                            break;
                        case 0xbcf48c51://"materialwetnessmultiplier"
                        case 0xad4c52e0://"bumpselfshadowamount"
                        case 0xf418334f://"specularintensitymult"
                        case 0x87744680://"specularfalloffmult"
                        case 0x4620a35d://"usetessellation"
                        case 0xbc710e59://"heightbias3"
                        case 0x5b99b862://"heightscale3"
                        case 0xc95fa836://"heightbias2"
                        case 0x81de04ea://"heightscale2"
                        case 0x30e17734://"heightbias1"
                        case 0x7128637f://"heightscale1"
                        case 0x2010d597://"heightbias0"
                        case 0xa751cfd1://"heightscale0"
                        case 0x0e710045://"parallaxselfshadowamount"
                        case 0xf8a5b0da://"specintensityadjust"
                        case 0x3f1564c7://"specularintensitymultspecmap"
                        case 0x0d89b4ab://"specfalloffadjust"
                        case 0xa3d8627a://"specularfalloffmultspecmap"
                        case 0x32dd9ff5://"wetnessmultiplier"
                        case 0xfdd796cf://"tintpaletteselector"
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        private void SetupGrassShader(Rsc5Shader s)
        {
            SetDefaultShader();
            ShaderInputs = Shader.CreateShaderInputs();
            ShaderInputs.SetFloat(0xDF918855, 2.0f);//"BumpScale"
            ShaderInputs.SetUInt32(0x249983FD, 5); //"ParamsMapConfig"
            ShaderInputs.SetUInt32(0x2905BED7, 1); //"ColourTintMode"
            ShaderInputs.SetFloat(0x4D52C5FF, 1.0f); //"AlphaScale"
            ShaderInputs.SetFloat4(0x1C2824ED, Vector4.One);//"NoiseSettings"

            if (s == null || s.Params == null)
                return;

            Textures = new Texture[3];
            for (int p = 0; p < s.Params.Length; p++)
            {
                var parm = s.Params[p];
                if (parm.Type == 0)
                {
                    var tex = parm.Texture;
                    if (tex != null)
                    {
                        switch (parm.Hash)
                        {
                            case 0xF1FE2B71://diffusesampler
                                Textures[0] = tex;
                                break;


                            //fur:
                            case 0x46B7C64F://bumpsampler
                                ShaderInputs.SetUInt32(0x2905BED7, 0); //"ColourTintMode" //TOTAL HACK to turn off tinting for grass fur
                                Textures[1] = tex;
                                break;
                            case 0x608799C6://specsampler
                                Textures[2] = tex;
                                break;
                            case 0x65e15377://comboheightsamplerfur01
                            case 0xaea56292://comboheightsamplerfur23
                            case 0x958eb755://comboheightsamplerfur45
                            case 0x533d9fb8://comboheightsamplerfur67
                            case 0xbd772afa://stipplesampler
                            case 0xe23133b8://furmasksampler
                            case 0xaf9c8381://diffusehfsampler
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {

                    switch (parm.Hash)
                    {
                        case 0x38ee77ab://"galphatocoveragescale"
                            ShaderInputs.SetFloat(0x4D52C5FF, parm.Vector.X); //"AlphaScale"
                            break;
                        case 0x4d15c563://"shadowfalloff"
                        case 0x38a2f7f5://"alphatest"
                        case 0x377ec8ce://"alphascale"
                        case 0xe557c30e://"_fakedgrassnormal"
                        case 0xbc242376://"umovementparams"
                        case 0xfd3ddac7://"fadealphalod2distfar0"
                        case 0x4d2367d1://"fadealphalod2dist"
                        case 0xd4623cc4://"fadealphalod1dist"
                        case 0xd8d5c09f://"fadealphadistumtimer"
                        case 0xba8b599e://"_vecvehcoll3r"
                        case 0x776c535d://"_vecvehcoll3m"
                        case 0x948b8da3://"_vecvehcoll3b"
                        case 0xd0d40bc7://"_vecvehcoll2r"
                        case 0x11d10dc0://"_vecvehcoll2m"
                        case 0x5bf6a1f2://"_vecvehcoll2b"
                        case 0x6da7468f://"_vecvehcoll1r"
                        case 0xde702857://"_vecvehcoll1m"
                        case 0xc6f87968://"_vecvehcoll1b"
                        case 0xdb24a724://"_vecvehcoll0r"
                        case 0x7a7965cf://"_vecvehcoll0m"
                        case 0xcd3d0b51://"_vecvehcoll0b"
                        case 0x499e4a30://"_veccollparams"
                        case 0x83ecbf37://"_dimensionlod2"
                        case 0x7a349c20://"vecplayerpos"
                        case 0xe7e830c1://"veccamerapos"
                        case 0xbce6048d://"groundcolor"
                        case 0x51178072://"plantcolor"
                        case 0x045667b9://"matgrasstransform"
                            break;

                        //fur:
                        case 0xf6712b81://"bumpiness"
                            ShaderInputs.SetFloat(0xDF918855, parm.Vector.X);//"BumpScale"
                            break;
                        case 0x2a670f69://"furshadow47"
                        case 0x41d91335://"furshadow03"
                        case 0xff245382://"furalphaclip47"
                        case 0x114d742c://"furalphaclip03"
                        case 0xf6dfe913://"furalphadistance"
                        case 0x3582987d://"furuvscales"
                        case 0x42ebbdcf://"furlayerparams"
                        case 0x4620a35d://"usetessellation"
                        case 0x32dd9ff5://"wetnessmultiplier"
                        case 0xff11711d://"specmapintmask"
                        case 0xf418334f://"specularintensitymult"
                        case 0x87744680://"specularfalloffmult"
                        case 0x27b9b2fa://"specularfresnel"
                            break;

                        //batch:
                        case 0x407f8395://"glodfadetilescale"
                        case 0x9eb9f4b4://"glodfadepower"
                        case 0xc28f4fc7://"glodfaderange"
                        case 0x742c34cb://"glodfadestartdist"
                        case 0x92e7d306://?
                        case 0x8409a33a://"galphatest"
                        case 0x9827b3ed://"gwindbendscalevar"
                        case 0x2a4b9dc7://"gwindbendingglobals"
                        case 0xfd3d498c://"gscalerange"
                        case 0xa186f243://"vecbatchaabbdelta"
                        case 0x5d82554d://"vecbatchaabbmin"
                            break;

                        default:
                            break;
                    }
                }
            }
        }
        private void SetupTreesShader(Rsc5Shader s)
        {
            SetDefaultShader();
            ShaderInputs = Shader.CreateShaderInputs();
            ShaderInputs.SetFloat(0xDF918855, 2.0f);//"BumpScale"
            ShaderInputs.SetUInt32(0x249983FD, 5); //"ParamsMapConfig"
            ShaderInputs.SetFloat(0x4D52C5FF, 1.0f); //"AlphaScale"
            ShaderInputs.SetFloat4(0x1C2824ED, Vector4.One);//"NoiseSettings"
            //ShaderInputs.SetUInt32(0x26E8F6BB, 1); //"NormalDoubleSided"  //flip normals if they are facing away from the camera

            if (s == null || s.Params == null)
                return;

            Textures = new Texture[5];
            for (int p = 0; p < s.Params.Length; p++)
            {
                var parm = s.Params[p];
                if (parm.Type == 0)
                {
                    var tex = parm.Texture;
                    if (tex != null)
                    {
                        switch (parm.Hash)
                        {
                            case 0xF1FE2B71://diffusesampler
                                Textures[0] = tex;
                                break;
                            case 0x46B7C64F://bumpsampler
                                Textures[1] = tex;
                                break;
                            case 0x608799C6://specsampler
                                Textures[2] = tex;
                                break;
                            case 0xf648a067://"tintpalettesampler"
                                tex.Sampler = TextureSampler.Create(TextureSamplerFilter.Point, SharpDX.Direct3D11.TextureAddressMode.Wrap);
                                Textures[4] = tex;
                                ShaderInputs.SetUInt32(0x2905BED7, 1);//"ColourTintMode"
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {

                    switch (parm.Hash)
                    {
                        case 0xf6712b81://"bumpiness"
                            ShaderInputs.SetFloat(0xDF918855, parm.Vector.X);//"BumpScale"
                            break;
                        case 0x377ec8ce://"alphascale"
                            ShaderInputs.SetFloat(0x4D52C5FF, parm.Vector.X); //"AlphaScale"
                            break;
                        case 0x4d15c563://"shadowfalloff"
                        case 0x38a2f7f5://"alphatest"
                        case 0x6db8e46f://"selfshadowing"
                        case 0xe0d28d44://"usetreenormals"
                        case 0x0c6fa156://"windglobalparams"
                        case 0x21ffda1a://"umglobalparams"
                        case 0xff11711d://"specmapintmask"
                        case 0xf418334f://"specularintensitymult"
                        case 0x87744680://"specularfalloffmult"
                        case 0x27b9b2fa://"specularfresnel"
                        case 0xfdd796cf://"tintpaletteselector"
                            break;

                        case 0x41e69fbe://"treelod2normal"
                        case 0x744e7500://"treelod2params"
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private void SetupSkyShader(Rsc5Shader s)
        {
            SetDefaultShader();
            ShaderInputs = Shader.CreateShaderInputs();
            ShaderInputs.SetFloat(0xDF918855, 1.0f);//"BumpScale"
            ShaderInputs.SetUInt32(0x249983FD, 1); //"ParamsMapConfig"
            ShaderInputs.SetFloat(0x4D52C5FF, 1.0f); //"AlphaScale"
            ShaderInputs.SetFloat4(0x1C2824ED, Vector4.One);//"NoiseSettings"

            if (s == null || s.Params == null)
                return;

            Textures = new Texture[3];
            for (int p = 0; p < s.Params.Length; p++)
            {
                var parm = s.Params[p];
                if (parm.Type == 0)
                {
                    var tex = parm.Texture;
                    if (tex != null)
                    {
                        switch (parm.Hash)
                        {
                            //sky_system
                            case 0xcc5c77fc://perlinsampler
                            case 0x3344cee3://highdetailsampler
                            case 0xf745c599://starfieldsampler
                            case 0x5b709d2a://moonsampler
                                break;

                            //clouds
                            case 0x8ac11cb0://normalsampler
                                Textures[1] = tex;
                                break;
                            case 0xe43044d6://densitysampler
                            case 0x874fd28b://detaildensitysampler
                            case 0xad1518e5://detailnormalsampler
                            case 0x9a35e36c://detaildensity2sampler
                            case 0x77755b8f://detailnormal2sampler
                                break;

                            default:
                                break;
                        }
                    }
                }
                else
                {

                    switch (parm.Hash)
                    {
                        #region sky_system
                        case 0x229f9d0f://"debugcloudsparams"
                        case 0x723da98d://"noisephase"
                        case 0xe15c7aa5://"noisedensityoffset"
                        case 0x3aaba477://"noisesoftness"
                        case 0x0b37698b://"noisethreshold"
                        case 0xf98cbcea://"noisescale"
                        case 0x514fb02f://"noisefrequency"
                        case 0x83f07e59://"mooncolor"
                        case 0xbcc436dc://"lunarcycle"
                        case 0x8b9afb5d://"moonintensity"
                        case 0x077058ae://"moonposition"
                        case 0xcfb33aca://"moondirection"
                        case 0x66915a48://"starfieldintensity"
                        case 0xf04c79fb://"speedconstants"
                        case 0x1808cc54://"horizonlevel"
                        case 0x4647a8ee://"effectsconstants"
                        case 0x67cb6062://"smallcloudcolorhdr"
                        case 0x16748424://"smallcloudconstants"
                        case 0x6e30e7c0://"cloudconstants2"
                        case 0x5bf7434d://"cloudconstants1"
                        case 0x81d5c15e://"clouddetailconstants"
                        case 0xdb7313bf://"cloudshadowminusbasecolourtimesshadowstrength"
                        case 0xb9fa3b61://"cloudmidcolour"
                        case 0x0c5efad9://"cloudbaseminusmidcolour"
                        case 0xafd624bf://"sunposition"
                        case 0x9b6972ea://"sundirection"
                        case 0x5c32aa8b://"sunconstants"
                        case 0x405e1a56://"sundisccolorhdr"
                        case 0x3e8d0382://"suncolorhdr"
                        case 0x3522fd1f://"suncolor"
                        case 0xf68abdaa://"hdrintensity"
                        case 0x77886744://"skyplaneparams"
                        case 0xd524e91a://"skyplanecolor"
                        case 0x002ac014://"zenithconstants"
                        case 0xb4592d88://"zenithtransitioncolor"
                        case 0x523f5189://"zenithcolor"
                        case 0xa4cc5a5b://"azimuthtransitionposition"
                        case 0x9f26ba95://"azimuthtransitioncolor"
                        case 0x6fb9487f://"azimuthwestcolor"
                        case 0x3e478b00://"azimutheastcolor"
                            break;
                        #endregion
                        #region clouds
                        case 0x652f6472://"cloudlayeranimscale3"
                        case 0xad26745f://"cloudlayeranimscale2"
                        case 0xa0e05bd3://"cloudlayeranimscale1"
                        case 0xe885f554://"gsoftparticlerange"
                        case 0x86b1e21a://"grescaleuv3"
                        case 0x7a00c8b8://"grescaleuv2"
                        case 0x2b2aab0d://"grescaleuv1"
                        case 0xd13d7466://"guvoffset3"
                        case 0x8aa2672d://"guvoffset2"
                        case 0x98e703ba://"guvoffset1"
                        case 0x07c150ef://"gcamerapos"
                        case 0x52bd00bc://"gcloudviewproj"
                        case 0xc4b23e96://"guvoffset"
                        case 0xccf657aa://"ganimblendweights"
                        case 0x2badee99://"ganimsculpt"
                        case 0xf82166c9://"ganimcombine"
                        case 0x95d12386://"gnearfarqmult"
                        case 0x31ab9738://"gwraplighting_msaaref"
                        case 0x17b7ac6e://"gscalediffusefillambient"
                        case 0x89363bdc://"gpiercinglightpower_strength_normalstrength_thickness"
                        case 0x3ad81b66://"gscatterg_gsquared_phasemult_scale"
                        case 0xb56dd95a://"gdensityshiftscale"
                        case 0xc3d07779://"gbouncecolor"
                        case 0x952d996b://"gambientcolor"
                        case 0xc7a732a6://"gcloudcolor"
                        case 0xb33e5862://"gsuncolor"
                        case 0x77c59bbb://"gsundirection"
                        case 0xc01dbb19://"gwestcolor"
                        case 0x8b08cbb2://"geastminuswestcolor"
                        case 0xc66b7df1://"gskycolor"
                            break;
                        #endregion

                        default:
                            break;
                    }
                }
            }
        }

        public override string ToString()
        {
            return VertexCount.ToString() + " verts, " + (ShaderRef?.ToString() ?? "NULL SHADER)");
        }
    }

    [TC(typeof(EXP))]
    public class Rsc5IndexBuffer : Rsc5BlockBase
    {
        public override ulong BlockLength => 96;
        public ulong VFT { get; set; } = 0x14061D158;
        public Rsc5RawArr<ushort> Indices { get; set; }
        public uint IndicesCount { get; set; }
        public uint Unknown_Ch; // 0x00000000
        public ulong Unknown_18h; // 0x0000000000000000
        public ulong Unknown_20h; // 0x0000000000000000
        public ulong Unknown_28h; // 0x0000000000000000
        public ulong Unknown_30h; // 0x0000000000000000
        public ulong Unknown_38h; // 0x0000000000000000
        public ulong Unknown_40h; // 0x0000000000000000
        public ulong Unknown_48h; // 0x0000000000000000
        public ulong Unknown_50h; // 0x0000000000000000
        public ulong Unknown_58h; // 0x0000000000000000

        public override void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            IndicesCount = reader.ReadUInt32();
            Indices = reader.ReadRawArrPtr<ushort>();
            Unknown_18h = reader.ReadUInt32();
            Unknown_20h = reader.ReadUInt32();
            Unknown_28h = reader.ReadUInt32();
            Unknown_30h = reader.ReadUInt32();
            Unknown_38h = reader.ReadUInt32();
            Unknown_40h = reader.ReadUInt32();
            Unknown_48h = reader.ReadUInt32();
            Unknown_50h = reader.ReadUInt32();
            Unknown_58h = reader.ReadUInt32();
            Indices = reader.ReadRawArrItems(Indices, IndicesCount, true);
        }

        public override void Write(Rsc5DataWriter writer)
        {
            IndicesCount = (uint)(Indices.Items != null ? Indices.Items.Length : 0);
            throw new NotImplementedException();

            //writer.Write(VFT);
            //writer.Write(IndicesCount);
            //writer.Write(Unknown_Ch);
            //writer.Write(Indices);
            //writer.Write(Unknown_18h);
            //writer.Write(Unknown_20h);
            //writer.Write(Unknown_28h);
            //writer.Write(Unknown_30h);
            //writer.Write(Unknown_38h);
            //writer.Write(Unknown_40h);
            //writer.Write(Unknown_48h);
            //writer.Write(Unknown_50h);
            //writer.Write(Unknown_58h);
        }

    }
    [TC(typeof(EXP))]
    public class Rsc5VertexBuffer : Rsc5BlockBase
    {
        public override ulong BlockLength => 128;
        public ulong VFT { get; set; } = 0x14061D3F8;
        public Rsc5RawArr<byte> Data1 { get; set; }
        public ushort Flags { get; set; } //only 0 or 1024
        public Rsc5RawArr<byte> Data2 { get; set; }
        public ushort VertexCount { get; set; }
        public Rsc5Ptr<Rsc5VertexDeclaration> Layout { get; set; }
        public uint VertexStride { get; set; }
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            VertexCount = reader.ReadUInt16();
            Flags = reader.ReadUInt16();
            Data1 = reader.ReadRawArrPtr<byte>();
            VertexStride = reader.ReadUInt32();
            Layout = reader.ReadPtr<Rsc5VertexDeclaration>();
            Unknown1 = reader.ReadUInt32();
            Data2 = reader.ReadRawArrPtr<byte>();
            Unknown2 = reader.ReadUInt32();

            var datalen = VertexCount * VertexStride;
            Data1 = reader.ReadRawArrItems(Data1, datalen);
            Data2 = reader.ReadRawArrItems(Data2, datalen);
        }

        public override void Write(Rsc5DataWriter writer)
        {
            VertexCount = (ushort)(Data1.Items != null ? Data1.Items.Length : Data2.Items != null ? Data2.Items.Length : 0);
            throw new NotImplementedException();

            //writer.Write(VFT);
            //writer.Write(VertexStride);
            //writer.Write(Flags);
            //writer.Write(Unknown_Ch);
            //writer.Write(Data1);
            //writer.Write(VertexCount);
            //writer.Write(Unknown_1Ch);
            //writer.Write(Data2);
            //writer.Write(Unknown_28h);
            //writer.Write(Layout);
            //writer.Write(Unknown_38h);
            //writer.Write(Unknown_40h);
            //writer.Write(Unknown_48h);
            //writer.Write(Unknown_50h);
            //writer.Write(Unknown_58h);
            //writer.Write(Unknown_60h);
            //writer.Write(Unknown_68h);
            //writer.Write(Unknown_70h);
            //writer.Write(Unknown_78h);
        }

        public override string ToString()
        {
            var cstr = "Count: " + VertexCount.ToString();
            if (Layout.Item == null) return "!NULL LAYOUT! - " + cstr;
            return "Type: " + Layout.Item.FVF.ToString() + ", " + cstr;
        }
    }

    [TC(typeof(EXP))] public class Rsc5VertexDeclaration : Rsc5BlockBase //rage::grcFvf
    {
        /*
         * FVF - Flexible Vertex Format
         * This class uses the concepts of channels and data size/type.
         * A channel represents actual data sent, such as positions or normals.
         * A data size/type represents how that data is stored in a vertex buffer
         */

        public override ulong BlockLength => 16;
        public uint FVF { get; set; } //m_Fvf, fvf channels currently used, (16601, 16473, 16857, etc)
        public byte FVFSize { get; set; } //m_FvfSize, total size of the fvf
        public byte Flags { get; set; } //m_Flags, various flags to use (i.e. transformed positions, etc)
        public byte DynamicOrder { get; set; } //m_DynamicOrder, if fvf is in dynamic order or standard order
        public byte ChannelCount { get; set; } //m_ChannelCount, number of 1's in 'Flags'
        public Rsc5VertexDeclarationTypes Types { get; set; } //m_FvfChannelSizes, 16 fields 4 bits each
        public VertexLayout VertexLayout { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            FVF = reader.ReadUInt32();
            FVFSize = reader.ReadByte();
            Flags = reader.ReadByte();
            DynamicOrder = reader.ReadByte();
            ChannelCount = reader.ReadByte();
            Types = (Rsc5VertexDeclarationTypes)reader.ReadUInt64();

            ulong t = (ulong)Types;
            ulong types = 0;
            ulong semantics = 0;
            int n = 0;

            for (int i = 0; i < 16; i++)
            {
                if (((FVF >> i) & 1) != 0)
                {
                    var i4 = i * 4;
                    var n4 = n * 4;
                    var ef = GetEngineElementFormat((Rsc5VertexComponentType)((t >> i4) & 0xF));
                    var si = GetEngineSemanticIndex((Rsc5VertexElementSemantic)i);
                    types += (((ulong)ef) << n4);
                    semantics += (((ulong)si) << n4);
                    n++;
                }
            }
            VertexLayout = new VertexLayout(types, semantics);
        }

        public override void Write(Rsc5DataWriter writer)
        {
            writer.WriteUInt32(FVF);
            writer.WriteByte(FVFSize);
            writer.WriteByte(Flags);
            writer.WriteByte(DynamicOrder);
            writer.WriteByte(ChannelCount);
            writer.WriteUInt64((ulong)Types);
        }

        public Rsc5VertexComponentType GetComponentType(int index)
        {
            //index is the flags bit index
            return (Rsc5VertexComponentType)(((ulong)Types >> (index * 4)) & 0x0000000F);
        }

        public int GetComponentOffset(int index)
        {
            //index is the flags bit index
            var offset = 0;
            for (int k = 0; k < index; k++)
            {
                if (((FVF >> k) & 0x1) == 1)
                {
                    var componentType = GetComponentType(k);
                    offset += Rsc5VertexComponentTypes.GetSizeInBytes(componentType);
                }
            }
            return offset;
        }

        private static VertexElementFormat GetEngineElementFormat(Rsc5VertexComponentType t)
        {
            switch (t)
            {
                case Rsc5VertexComponentType.Half2: return VertexElementFormat.Half2;
                case Rsc5VertexComponentType.Float: return VertexElementFormat.Float;
                case Rsc5VertexComponentType.Half4: return VertexElementFormat.Half4;
                case Rsc5VertexComponentType.FloatUnk: return VertexElementFormat.Float;
                case Rsc5VertexComponentType.Float2: return VertexElementFormat.Float2;
                case Rsc5VertexComponentType.Float3: return VertexElementFormat.Float3;
                case Rsc5VertexComponentType.Float4: return VertexElementFormat.Float4;
                case Rsc5VertexComponentType.UByte4: return VertexElementFormat.UByte4;
                case Rsc5VertexComponentType.Colour: return VertexElementFormat.Colour;
                case Rsc5VertexComponentType.Dec3N: return VertexElementFormat.Dec3N;
                default:
                    return VertexElementFormat.None;
            }
        }

        public byte GetEngineSemanticIndex(Rsc5VertexElementSemantic s)
        {
            switch (s)
            {
                default:
                case Rsc5VertexElementSemantic.Position: return 0;
                case Rsc5VertexElementSemantic.BlendWeights: return 1;
                case Rsc5VertexElementSemantic.BlendIndices: return 2;
                case Rsc5VertexElementSemantic.Normal: return 3;
                case Rsc5VertexElementSemantic.Colour0: return 4;
                case Rsc5VertexElementSemantic.Colour1: return 4;
                case Rsc5VertexElementSemantic.TexCoord0: return 5;
                case Rsc5VertexElementSemantic.TexCoord1: return 5;
                case Rsc5VertexElementSemantic.TexCoord2: return 5;
                case Rsc5VertexElementSemantic.TexCoord3: return 5;
                case Rsc5VertexElementSemantic.TexCoord4: return 5;
                case Rsc5VertexElementSemantic.TexCoord5: return 5;
                case Rsc5VertexElementSemantic.TexCoord6: return 5;
                case Rsc5VertexElementSemantic.TexCoord7: return 5;
                case Rsc5VertexElementSemantic.Tangent0: return 6;
                case Rsc5VertexElementSemantic.Tangent1: return 6;
                case Rsc5VertexElementSemantic.Binormal0: return 7;
                case Rsc5VertexElementSemantic.Binormal1: return 7;
            }
        }


        public override string ToString()
        {
            return FVFSize.ToString() + ": " + ChannelCount.ToString() + ": " + FVF.ToString() + ": " + Types.ToString();
        }
    }

    public enum Rsc5VertexDeclarationTypes : ulong
    {
        MCLA1 = 0xAA1111111199A996
    }

    public enum Rsc5VertexComponentType : byte
    {
        Nothing = 0,
        Half2 = 1,
        Float = 2,
        Half4 = 3,
        FloatUnk = 4,
        Float2 = 5,
        Float3 = 6,
        Float4 = 7,
        UByte4 = 8,
        Colour = 9,
        Dec3N = 10,
        Unk1 = 11,
        Unk2 = 12,
        Unk3 = 13,
        UShort2N = 14,
        Unk5 = 15,
    }

    public enum Rsc5VertexElementSemantic : byte //grcFvfChannels, list of fvf channels available
    {
        Position = 0,
        BlendWeights = 1,
        BlendIndices = 2, //Binding
        Normal = 3,
        Colour0 = 4, //Normal
        Colour1 = 5, //Diffuse
        TexCoord0 = 6,
        TexCoord1 = 7,
        TexCoord2 = 8,
        TexCoord3 = 9,
        TexCoord4 = 10,
        TexCoord5 = 11,
        TexCoord6 = 12,
        TexCoord7 = 13,
        Tangent0 = 14,
        Tangent1 = 15,
        Binormal0 = 16,
        Binormal1 = 17,
    }

    [TC(typeof(EXP))] public static class Rsc5VertexComponentTypes
    {
        public static int GetSizeInBytes(Rsc5VertexComponentType type)
        {
            switch (type)
            {
                case Rsc5VertexComponentType.Nothing: return 2; //Half
                case Rsc5VertexComponentType.Half2: return 4; //Half2
                case Rsc5VertexComponentType.Float: return 6; //Half3
                case Rsc5VertexComponentType.Half4: return 8; //Half4
                case Rsc5VertexComponentType.FloatUnk: return 4; //Float
                case Rsc5VertexComponentType.Float2: return 8; //Float2
                case Rsc5VertexComponentType.Float3: return 12; //Float3
                case Rsc5VertexComponentType.Float4: return 16; //Float4
                case Rsc5VertexComponentType.UByte4: return 4; //UByte4
                case Rsc5VertexComponentType.Colour: return 4; //Color
                case Rsc5VertexComponentType.Dec3N: return 4; //PackedNormal
                case Rsc5VertexComponentType.Unk1: return 2; //Short_UNorm
                case Rsc5VertexComponentType.Unk2: return 4; //Short2_Unorm
                case Rsc5VertexComponentType.Unk3: return 2; //Byte2_UNorm
                case Rsc5VertexComponentType.UShort2N: return 4; //Short2
                case Rsc5VertexComponentType.Unk5: return 8; //Short4
                default: return 0;
            }
        }

        public static int GetComponentCount(Rsc5VertexComponentType type)
        {
            switch (type)
            {
                case Rsc5VertexComponentType.Nothing: return 0;
                case Rsc5VertexComponentType.Float: return 1;
                case Rsc5VertexComponentType.Float2: return 2;
                case Rsc5VertexComponentType.Float3: return 3;
                case Rsc5VertexComponentType.Float4: return 4;
                case Rsc5VertexComponentType.Colour: return 4;
                case Rsc5VertexComponentType.UByte4: return 4;
                case Rsc5VertexComponentType.Half2: return 2;
                case Rsc5VertexComponentType.Half4: return 4;
                case Rsc5VertexComponentType.Dec3N: return 3;
                case Rsc5VertexComponentType.UShort2N: return 2;
                default: return 0;
            }
        }
    }

    [TC(typeof(EXP))] public class Rsc5Skeleton : Skeleton, Rsc5Block
    {
        public ulong BlockLength => 4;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public Rsc5Ptr<Rsc5Bone> Bone { get; set; }

        public void Read(Rsc5DataReader reader)
        {
            Bone = reader.ReadPtr<Rsc5Bone>();
        }

        public void Write(Rsc5DataWriter writer)
        {
            writer.WritePtr(Bone);
        }
    }

    public class Rsc5Bone : Bone, Rsc5Block
    {
        public ulong BlockLength => 224;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public Rsc5Str NameStr { get; set; }
        public ushort Unknown1 { get; set; }
        public ushort Flags { get; set; }
        public Rsc5Ptr<Rsc5Bone> NextSibling { get; set; }
        public Rsc5Ptr<Rsc5Bone> FirstChild { get; set; }
        public Rsc5Ptr<Rsc5Bone> ParentRef { get; set; }
        public ushort ID { get; set; }
        public ushort Mirror { get; set; }
        public ushort Unknown3 { get; set; }
        public uint Unknown4 { get; set; }
        public Vector4 OrigPosition { get; set; }
        public Vector4 OrigRotationEuler { get; set; }
        public Vector4 OrigRotation { get; set; }
        public Vector4 OrigScale { get; set; }
        public Vector4 AbsolutePosition { get; set; }
        public Vector4 AbsoluteRotationEuler { get; set; }
        public Vector4 Sorient { get; set; }
        public Vector4 TranslationMin { get; set; }
        public Vector4 TranslationMax { get; set; }
        public Vector4 RotationMin { get; set; }
        public Vector4 RotationMax { get; set; }
        public Vector4 Unknown5 { get; set; }
        public uint UnknownInt { get; set; } //updated from Rsc5Skeleton
        public Matrix4x4 Transform1 { get; set; } //updated from Rsc5Skeleton
        public Matrix4x4 Transform2 { get; set; } //updated from Rsc5Skeleton
        public Matrix4x4 Transform3 { get; set; } //updated from Rsc5Skeleton

        public void Read(Rsc5DataReader reader)
        {
            NameStr = reader.ReadStr();
            Unknown1 = reader.ReadUInt16();
            Flags = reader.ReadUInt16();
            NextSibling = new Rsc5Ptr<Rsc5Bone>() { Position = reader.ReadUInt32() }; //reader.ReadPtr<Rsc5Bone>(); //###need to avoid circular reads here!
            FirstChild = new Rsc5Ptr<Rsc5Bone>() { Position = reader.ReadUInt32() }; //reader.ReadPtr<Rsc5Bone>();
            ParentRef = new Rsc5Ptr<Rsc5Bone>() { Position = reader.ReadUInt32() }; //reader.ReadPtr<Rsc5Bone>();
            Index = reader.ReadUInt16();
            ID = reader.ReadUInt16();
            Mirror = reader.ReadUInt16();
            Unknown3 = reader.ReadUInt16();
            Unknown4 = reader.ReadUInt32();
            OrigPosition = reader.ReadVector4();
            OrigRotationEuler = reader.ReadVector4();
            OrigRotation = reader.ReadVector4();
            OrigScale = reader.ReadVector4();
            AbsolutePosition = reader.ReadVector4();
            AbsoluteRotationEuler = reader.ReadVector4();
            Sorient = reader.ReadVector4();
            TranslationMin = reader.ReadVector4();
            TranslationMax = reader.ReadVector4();
            RotationMin = reader.ReadVector4(); // Minimum euler rotation maybe?
            RotationMax = reader.ReadVector4(); // Maximum euler rotation maybe?
            Unknown5 = reader.ReadVector4();

            Name = NameStr.Value;
            Position = OrigPosition.XYZ();
            Rotation = OrigRotation.ToQuaternion();
            Scale = Vector3.One; //OrigScale.XYZ();

            AnimRotation = Rotation;
            AnimTranslation = Position;
            AnimScale = Scale;
        }

        public void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [TC(typeof(EXP))]
    public class Rsc5ShaderGroup : Rsc5BlockBase
    {
        public override ulong BlockLength => 12;
        public ulong VFT { get; set; }
        public uint BlockMap { get; set; }
        public Rsc5Ptr<Rsc5TextureDictionary> TextureDictionary { get; set; }
        public Rsc5PtrArr<Rsc5Shader> Shaders { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            BlockMap = reader.ReadUInt32();
            Shaders = reader.ReadPtrArr<Rsc5Shader>();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            Xml.ValueTag(sb, indent, "Unknown30", "0");
            Xml.OpenTag(sb, indent, "Shaders");

            var shaders = Shaders.Items;
            if (shaders != null)
            {
                indent += 2;
                foreach (var v in shaders)
                {
                    Xml.OpenTag(sb, indent - 1, "Item");
                    Xml.StringTag(sb, indent, "Name", v.ShaderFileName.Value);
                    Xml.StringTag(sb, indent, "FileName", v.ShaderFileName.Value + ".sps");
                    Xml.ValueTag(sb, indent, "RenderBucket", v.DrawBucket.ToString());
                    Xml.OpenTag(sb, indent, "Parameters");

                    foreach (var p in v.Params)
                    {
                        p.WriteXml(sb, indent + 1);
                    }
                    Xml.CloseTag(sb, indent, "Parameters");
                    Xml.CloseTag(sb, indent - 1, "Item");
                }
            }
            Xml.CloseTag(sb, indent, "Shaders");
        }
    }

    [TC(typeof(EXP))]
    public class Rsc5Shader : Rsc5BlockBase
    {
        public override ulong BlockLength => 96;
        public uint VFT { get; set; }
        public uint BlockMapAdress { get; set; }
        public byte Version { get; set; } //2
        public byte DrawBucket { get; set; }
        public byte UsageCount { get; set; }
        public byte Unknown1 { get; set; }
        public ushort Unknown2 { get; set; }
        public ushort ShaderIndex { get; set; }
        public uint ParamsDataPtr { get; set; }
        public uint Unknown3 { get; set; }
        public ushort ParamsCount { get; set; }
        public ushort EffectSize { get; set; }
        public uint ParamsTypesPtr { get; set; }
        public uint Hash { get; private set; }
        public uint ParamsNamesPtr { get; set; }
        public uint Unknown4 { get; set; }
        public uint Unknown5 { get; set; }
        public Rsc5Str ShaderName { get; set; }
        public uint Unknown6 { get; set; }
        public Rsc5Str ShaderFileName { get; set; }
        public uint Unknown7 { get; set; }
        public Rsc5ShaderParameter[] Params { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            BlockMapAdress = reader.ReadUInt32();
            Version = reader.ReadByte();
            DrawBucket = reader.ReadByte();
            UsageCount = reader.ReadByte();
            Unknown1 = reader.ReadByte();
            Unknown2 = reader.ReadUInt16();
            ShaderIndex = reader.ReadUInt16();
            ParamsDataPtr = reader.ReadUInt32();
            Unknown3 = reader.ReadUInt32();
            ParamsCount = reader.ReadUInt16();
            EffectSize = reader.ReadUInt16();
            ParamsTypesPtr = reader.ReadUInt32();
            Hash = reader.ReadUInt32();
            ParamsNamesPtr = reader.ReadUInt32();
            Unknown4 = reader.ReadUInt32();
            Unknown5 = reader.ReadUInt32();
            ShaderName = reader.ReadStr();
            Unknown6 = reader.ReadUInt32();
            ShaderFileName = reader.ReadStr();
            Unknown7 = reader.ReadUInt32();

            var pc = ParamsCount;
            uint[] ptrs = Rpf3Crypto.Swap(reader.ReadArray<uint>(pc, ParamsDataPtr));
            byte[] types = reader.ReadArray<byte>(pc, ParamsTypesPtr);
            uint[] hashes = Rpf3Crypto.Swap(reader.ReadArray<uint>(pc, ParamsNamesPtr));

            Params = new Rsc5ShaderParameter[pc];
            for (uint i = 0; i < pc; i++)
            {
                var p = new Rsc5ShaderParameter
                {
                    Hash = hashes[i],
                    Type = types[i]
                };

                switch (p.Type)
                {
                    case 0: //texture
                        p.Texture = reader.ReadBlock<Rsc5TextureBase>(ptrs[i]);
                        break;
                    case 1: //vector4
                        p.Vector = reader.ReadVector4(ptrs[i]);
                        break;
                    default: //array
                        p.Array = reader.ReadArray<Vector4>(p.Type, ptrs[i]);
                        break;
                }
                Params[i] = p;
            }
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return ShaderName.Value;
        }
    }

    [TC(typeof(EXP))]
    public class Rsc5ShaderParameter
    {
        public JenkHash Hash { get; set; }
        public byte Type { get; set; } //0: texture, 1: vector4, 2+: vector4 array
        public Vector4 Vector { get; set; }
        public Vector4[] Array { get; set; }
        public Rsc5TextureBase Texture { get; set; }

        public override string ToString()
        {
            return Hash.ToString() + ": " + ((Type == 0) ? ("texture: " + Texture?.ToString() ?? "(none)") : ((Type > 1) ? ("array: count " + Type.ToString()) : ("vector4: " + Vector.ToString())));
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            if (Hash.Str == "colors")
                return;

            var otstr = "Item name=\"" + Hash.ToString() + "\" type=\"" + ((Rsc5ShaderParamType)Type).ToString() + "\"";
            switch (Type)
            {
                case (byte)Rsc5ShaderParamType.Texture:
                    var name = Texture?.Name ?? "";
                    if (name != "")
                    {
                        if (name.EndsWith(".dds"))
                            name = name.Replace(".dds", "");
                        if (name.StartsWith("memory:$"))
                        {
                            int index = name.LastIndexOf(":") + 1;
                            name = name.Remove(0, index);
                        }
                        Xml.OpenTag(sb, indent, otstr);
                        Xml.StringTag(sb, indent + 1, "Name", Xml.Escape(name.ToLower()));
                        Xml.CloseTag(sb, indent, "Item");
                    }
                    break;

                case (byte)Rsc5ShaderParamType.Vector:
                    Xml.SelfClosingTag(sb, indent, otstr + " " + FloatUtil.GetVector4XmlString(Vector));
                    break;

                default:
                    Xml.OpenTag(sb, indent, otstr);
                    foreach (var vec in Array)
                    {
                        Xml.SelfClosingTag(sb, indent + 1, "Value " + FloatUtil.GetVector4XmlString(vec));
                    }
                    Xml.CloseTag(sb, indent, "Item");
                    break;
            }
        }

        public enum Rsc5ShaderParamType : byte
        {
            Texture = 0,
            Vector = 1
        }
    }
}
