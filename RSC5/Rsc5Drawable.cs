using CodeX.Core.Engine;
using CodeX.Core.Numerics;
using CodeX.Core.Shaders;
using CodeX.Core.Utilities;
using System.Numerics;
using CodeX.Games.MCLA.RPF3;
using TC = System.ComponentModel.TypeConverterAttribute;
using EXP = System.ComponentModel.ExpandableObjectConverter;
using CodeX.Games.MCLA.Files;
using System.Diagnostics;

namespace CodeX.Games.MCLA.RSC5
{
    [TC(typeof(EXP))] public class Rsc5AmbientDrawablePed : Rsc5FileBase //.xapb
    {
        public override ulong BlockLength => 64;
        public Rsc5Ptr<Rsc5BlockMap> BlockMap { get; set; }
        public Rsc5Ptr<Rsc5DrawableBase> Drawable { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            BlockMap = reader.ReadPtr<Rsc5BlockMap>();
            Drawable = reader.ReadPtr<Rsc5DrawableBase>();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [TC(typeof(EXP))] public class Rsc5City : Rsc5FileBase //.xshp located in resources/city
    { 
        public override ulong BlockLength => 40;
        public Rsc5Ptr<Rsc5BlockMap> BlockMap { get; set; }
        public Rsc5Ptr<Rsc5TextureDictionary> Dictionary { get; set; }
        public uint Unknown_Ch { get; set; }
        public Rsc5Ptr<Rsc5SimpleDrawableBase> Drawable { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            BlockMap = reader.ReadPtr<Rsc5BlockMap>();
            Dictionary = reader.ReadPtr<Rsc5TextureDictionary>();
            Unknown_Ch = reader.ReadUInt32();
            Drawable = reader.ReadPtr<Rsc5SimpleDrawableBase>();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [TC(typeof(EXP))] public class Rsc5Drawable : Rsc5LodBase
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
        public virtual ulong BlockLength => 116;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;

        public uint VFT { get; set; } = 0x00516D84;
        public Rsc5Ptr<Rsc5BlockMap> BlockMap { get; set; }
        public Rsc5Ptr<Rsc5ShaderGroup> ShaderGroup { get; set; }
        public Rsc5Ptr<Rsc5Skeleton> SkeletonRef { get; set; }
        public Vector4 BoundingCenter { get; set; }
        public Vector4 BoundingBoxMin { get; set; }
        public Vector4 BoundingBoxMax { get; set; }
        public Rsc5Ptr<Rsc5DrawableLod> DrawableModelsHigh { get; set; }
        public Rsc5Ptr<Rsc5DrawableLod> DrawableModelsMed { get; set; }
        public Rsc5Ptr<Rsc5DrawableLod> DrawableModelsLow { get; set; }
        public Rsc5Ptr<Rsc5DrawableLod> DrawableModelsVlow { get; set; }
        public float LodDistHigh { get; set; }
        public float LodDistMed { get; set; }
        public float LodDistLow { get; set; }
        public float LodDistVlow { get; set; }
        public int DrawBucketMaskHigh { get; set; }
        public int DrawBucketMaskMed { get; set; }
        public int DrawBucketMaskLow { get; set; }
        public int DrawBucketMaskVlow { get; set; }
        public float BoundingSphereRadius { get; set; }

        public virtual void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            BlockMap = reader.ReadPtr<Rsc5BlockMap>();
            ShaderGroup = reader.ReadPtr<Rsc5ShaderGroup>();
            SkeletonRef = reader.ReadPtr<Rsc5Skeleton>();
            BoundingCenter = reader.ReadVector4();
            BoundingBoxMin = reader.ReadVector4();
            BoundingBoxMax = reader.ReadVector4();
            DrawableModelsHigh = reader.ReadPtr<Rsc5DrawableLod>();
            DrawableModelsMed = reader.ReadPtr<Rsc5DrawableLod>();
            DrawableModelsLow = reader.ReadPtr<Rsc5DrawableLod>();
            DrawableModelsVlow = reader.ReadPtr<Rsc5DrawableLod>();
            LodDistHigh = reader.ReadSingle();
            LodDistMed = reader.ReadSingle();
            LodDistLow = reader.ReadSingle();
            LodDistVlow = reader.ReadSingle();
            DrawBucketMaskHigh = reader.ReadInt32();
            DrawBucketMaskMed = reader.ReadInt32();
            DrawBucketMaskLow = reader.ReadInt32();
            DrawBucketMaskVlow = reader.ReadInt32();
            BoundingSphereRadius = reader.ReadSingle();

            Lods = new[]
            {
                DrawableModelsHigh.Item,
                DrawableModelsMed.Item,
                DrawableModelsLow.Item,
                DrawableModelsVlow.Item
            };

            if (DrawableModelsHigh.Item != null) DrawableModelsHigh.Item.LodDist = LodDistHigh;
            if (DrawableModelsMed.Item != null) DrawableModelsMed.Item.LodDist = LodDistMed;
            if (DrawableModelsLow.Item != null) DrawableModelsLow.Item.LodDist = LodDistLow;
            if (DrawableModelsVlow.Item != null) DrawableModelsVlow.Item.LodDist = LodDistVlow;

            UpdateAllModels();
            AssignShaders();
            SetSkeleton(SkeletonRef.Item);
            CreateTexturePack(reader.FileEntry);

            UpdateBounds();
            BoundingSphere = new BoundingSphere(BoundingBox.Center, BoundingSphereRadius);
        }

        public virtual void Write(Rsc5DataWriter writer)
        {
            writer.WriteUInt32(VFT);
            writer.WritePtr(BlockMap);
            writer.WritePtr(ShaderGroup);
            writer.WritePtr(SkeletonRef);
            writer.WriteVector4(BoundingCenter);
            writer.WriteVector4(BoundingBoxMin);
            writer.WriteVector4(BoundingBoxMax);
            writer.WritePtr(DrawableModelsHigh);
            writer.WritePtr(DrawableModelsMed);
            writer.WritePtr(DrawableModelsLow);
            writer.WritePtr(DrawableModelsVlow);
            writer.WriteSingle(LodDistHigh);
            writer.WriteSingle(LodDistMed);
            writer.WriteSingle(LodDistLow);
            writer.WriteSingle(LodDistVlow);
            writer.WriteInt32(DrawBucketMaskHigh);
            writer.WriteInt32(DrawBucketMaskMed);
            writer.WriteInt32(DrawBucketMaskLow);
            writer.WriteInt32(DrawBucketMaskVlow);
            writer.WriteSingle(BoundingSphereRadius);
        }

        public void AssignShaders()
        {
            //Assign embedded textures to mesh for rendering
            if ((ShaderGroup.Item?.Shaders.Items != null) && (AllModels != null))
            {
                var shaders = ShaderGroup.Item?.Shaders.Items;
                for (int i = 0; i < AllModels.Length; i++)
                {
                    var model = AllModels[i];
                    if (model.Meshes != null)
                    {
                        for (int j = 0; j < model.Meshes.Length; j++)
                        {
                            if (model.Meshes[j] is Rsc5DrawableGeometry mesh)
                            {
                                var shader = (mesh.ShaderID < shaders.Length) ? shaders[mesh.ShaderID] : null;
                                mesh.SetShaderRef(shader);
                            }
                        }
                    }
                }
            }
        }

        public void SetSkeleton(Rsc5Skeleton skel)
        {
            Skeleton = skel;
            if (AllModels != null)
            {
                var bones = skel?.Bones;
                foreach (var model in AllModels.Cast<Rsc5DrawableModel>())
                {
                    var boneidx = model.BoneIndex;
                    if ((model.HasSkin == 0) && (bones != null) && (boneidx < bones.Length))
                    {
                        if (model.Meshes != null)
                        {
                            foreach (var mesh in model.Meshes)
                            {
                                mesh.BoneIndex = boneidx;
                            }
                        }
                    }
                }
            }
        }

        private void CreateTexturePack(GameArchiveFileInfo e)
        {
            var texs = XapbFile.Textures;
            if (texs == null) return;

            var txp = new TexturePack(e)
            {
                Textures = new Dictionary<string, Texture>()
            };

            for (int i = 0; i < texs.Count; i++)
            {
                var tex = texs[i];
                if (tex == null) continue;
                txp.Textures[tex.Name] = tex;
                tex.Pack = txp;
            }
            TexturePack = txp;
        }
    }

    [TC(typeof(EXP))] public class Rsc5LodBase : Piece, Rsc5Block
    {
        public virtual ulong BlockLength => 160;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;

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
            AssignGeometryShaders();
        }

        public virtual void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public void AssignGeometryShaders()
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

    [TC(typeof(EXP))] public class Rsc5SimpleDrawableBase : Piece, Rsc5Block
    {
        public virtual ulong BlockLength => 160;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;

        public uint VFT { get; set; } = 0x00595E80;
        public Rsc5Ptr<Rsc5BlockMap> BlockMap { get; set; }
        public Rsc5Ptr<Rsc5ShaderGroup> ShaderGroup { get; set; }
        public Rsc5Ptr<Rsc5DrawableLodMap> Lod { get; set; }

        public virtual void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            BlockMap = reader.ReadPtr<Rsc5BlockMap>();
            ShaderGroup = reader.ReadPtr<Rsc5ShaderGroup>();
            Lod = reader.ReadPtr<Rsc5DrawableLodMap>();

            Lods = new[] { Lod.Item };
            if (Lod.Item != null)
            {
                Lod.Item.LodDist = 9999f;
            }

            UpdateAllModels();
            AssignGeometryShaders();
            UpdateBounds();

            var center = (BoundingBox.Minimum + BoundingBox.Maximum) / 2;
            var radius = Vector3.Distance(center, BoundingBox.Maximum);
            BoundingSphere = new BoundingSphere(center, radius);
        }

        public virtual void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public void AssignGeometryShaders()
        {
            //Assign embedded textures to mesh for rendering
            if ((ShaderGroup.Item?.Shaders.Items != null) && (AllModels != null))
            {
                var shaders = ShaderGroup.Item?.Shaders.Items;
                for (int i = 0; i < AllModels.Length; i++)
                {
                    var model = AllModels[i];
                    if (model.Meshes != null)
                    {
                        for (int j = 0; j < model.Meshes.Length; j++)
                        {
                            if (model.Meshes[j] is Rsc5DrawableGeometry mesh)
                            {
                                var shader = (mesh.ShaderID < shaders.Length) ? shaders[mesh.ShaderID] : null;
                                mesh.SetShaderRef(shader);
                            }
                        }
                    }
                }
            }
        }
    }

    [TC(typeof(EXP))] public class Rsc5DrawableLodMap : Rsc5DrawableLod, Rsc5Block
    {
        public new ulong BlockLength => 32;
        public new ulong FilePosition { get; set; }
        public new bool IsPhysical => false;

        public uint VFT { get; set; } = 0x005960EC;
        public Rsc5Ptr<Rsc5BlockMap> BlockMap { get; set; }
        public uint ParentDictionary { get; set; }
        public uint RefCount { get; set; } = 1;
        public Rsc5Arr<uint> Hashes { get; set; }

        public new void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            BlockMap = reader.ReadPtr<Rsc5BlockMap>();
            ParentDictionary = reader.ReadUInt32();
            RefCount = reader.ReadUInt32();
            Hashes = reader.ReadArr<uint>();
            base.Read(reader);
        }

        public new void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [TC(typeof(EXP))] public class Rsc5DrawableLod : PieceLod, Rsc5Block
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
    }

    [TC(typeof(EXP))] public class Rsc5DrawableModel : Model, Rsc5Block
    {
        public ulong BlockLength => 28;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public ulong VFT { get; set; }
        public Rsc5PtrArr<Rsc5DrawableGeometry> Geometries { get; set; } //m_Geometries
        public Rsc5RawArr<Vector4> BoundsData { get; set; } //m_AABBs, one for each geometry + one for the whole model (unless there's only one model)
        public Rsc5RawArr<ushort> ShaderMapping { get; set; } //m_ShaderIndex
        public uint SkeletonBinding { get; set; } //4th byte is bone index, 2nd byte for skin meshes
        public ushort RenderMaskFlags { get; set; } //m_SkinFlag, determine whether to render with the skinned draw path or not
        public ushort GeometriesCount3 { get; set; } //m_Count

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
            BoundsData = reader.ReadRawArrPtr<Vector4>();
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
                var boundsData = (BoundingBox4[])null;
                if (BoundsData.Items != null && BoundsData.Items.Length > 0)
                {
                    //MCLA prefers Vector4's with the W component as the size of the bounding box
                    var vecs = Rpf3Crypto.Swap(BoundsData.Items);
                    boundsData = new BoundingBox4[vecs.Length];

                    for (int i = 0; i < vecs.Length; i++)
                    {
                        var v = vecs[i];
                        var vMin = new Vector3(v.Z - v.W, v.X - v.W, v.Y - v.W);
                        var vMax = new Vector3(v.Z + v.W, v.X + v.W, v.Y + v.W);
                        boundsData[i] = new BoundingBox4(new Vector4(vMin, 0.0f), new Vector4(vMax, 0.0f));
                    }
                }

                for (int i = 0; i < geoms.Length; i++)
                {
                    var geom = geoms[i];
                    if (geom != null)
                    {
                        geom.ShaderID = ((shaderMapping != null) && (i < shaderMapping.Length)) ? shaderMapping[i] : (ushort)0;
                        geom.AABB = (boundsData != null) ? ((boundsData.Length > 1) && ((i + 1) < boundsData.Length)) ? boundsData[i + 1] : boundsData[0] : new BoundingBox4();
                        geom.BoundingBox = new BoundingBox(geom.AABB.Min.XYZ(), geom.AABB.Max.XYZ());
                        geom.BoundingSphere = new BoundingSphere(geom.BoundingBox.Center, geom.BoundingBox.Size.Length() * 0.5f);

                        //MCLA also has NULL AABBs sometimes, so we have to calculate the bounds manually
                        if (boundsData == null)
                        {
                            geom.UpdateBounds();
                        }
                    }
                }
            }

            Meshes = Geometries.Items;
            RenderInMainView = true;
            RenderInShadowView = true;
            RenderInEnvmapView = true;
        }

        public void Write(Rsc5DataWriter writer)
        {
            GeometriesCount3 = Geometries.Count; //is this correct?
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            var geocount = Geometries.Items?.Length ?? 0;
            return "(" + geocount.ToString() + " geometr" + (geocount != 1 ? "ies)" : "y)");
        }
    }

    [TC(typeof(EXP))] public class Rsc5DrawableGeometry : Mesh, Rsc5Block
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

        public uint VFT { get; set; }
        public uint Unknown_8h { get; set; }
        public uint Unknown_10h { get; set; }
        public Rsc5Ptr<Rsc5VertexBuffer> VertexBuffer { get; set; } //m_VB[4] - rage::grcVertexBuffer
        public Rsc5Ptr<Rsc5VertexBuffer> VertexBuffer2 { get; set; }
        public Rsc5Ptr<Rsc5VertexBuffer> VertexBuffer3 { get; set; }
        public Rsc5Ptr<Rsc5VertexBuffer> VertexBuffer4 { get; set; }
        public Rsc5Ptr<Rsc5IndexBuffer> IndexBuffer { get; set; } //m_IB[4] - rage::grcIndexBuffer
        public Rsc5Ptr<Rsc5IndexBuffer> IndexBuffer2 { get; set; }
        public Rsc5Ptr<Rsc5IndexBuffer> IndexBuffer3 { get; set; }
        public Rsc5Ptr<Rsc5IndexBuffer> IndexBuffer4 { get; set; }
        public uint IndicesCount { get; set; } //m_IndexCount
        public uint TrianglesCount { get; set; } //m_PrimCount
        public ushort Unknown_62h { get; set; } = 3; //m_PrimType, rendering primitive type
        public uint Unknown_64h { get; set; }
        public Rsc5RawArr<ushort> BoneIds { get; set; } //m_MtxPalette, matrix palette for this geometry
        public ushort BoneIdsCount { get; set; } //m_MtxCount, the number of matrices in the matrix paletter
        public uint Unknown_74h { get; set; }
        public Rsc5RawArr<byte> VertexDataRef { get; set; }
        public uint Unknown_80h { get; set; } //m_OffsetBuffer, PS3 only I think
        public uint Unknown_88h { get; set; } //m_IndexOffset, PS3 only I think
        public uint Unknown_90h { get; set; }

        public Rsc5Shader ShaderRef { get; set; }
        public ushort ShaderID { get; set; } //Read-written by parent model
        public BoundingBox4 AABB { get; set; } //Read-written by parent model

        public Rsc5DrawableGeometry()
        {
        }

        public void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            Unknown_8h = reader.ReadUInt32();
            Unknown_10h = reader.ReadUInt32();
            VertexBuffer = reader.ReadPtr<Rsc5VertexBuffer>();
            VertexBuffer2 = reader.ReadPtr<Rsc5VertexBuffer>();
            VertexBuffer3 = reader.ReadPtr<Rsc5VertexBuffer>();
            VertexBuffer4 = reader.ReadPtr<Rsc5VertexBuffer>();
            IndexBuffer = reader.ReadPtr<Rsc5IndexBuffer>();
            IndexBuffer2 = reader.ReadPtr<Rsc5IndexBuffer>();
            IndexBuffer3 = reader.ReadPtr<Rsc5IndexBuffer>();
            IndexBuffer4 = reader.ReadPtr<Rsc5IndexBuffer>();
            IndicesCount = reader.ReadUInt32();
            TrianglesCount = reader.ReadUInt32();
            VertexCount = reader.ReadUInt16();
            Unknown_62h = reader.ReadUInt16();
            BoneIds = reader.ReadRawArrPtr<ushort>();
            VertexStride = reader.ReadUInt16();
            BoneIdsCount = reader.ReadUInt16();
            BoneIds = reader.ReadRawArrItems(BoneIds, BoneIdsCount, true);

            if (VertexBuffer.Item != null) //Hack to fix stupid "locked" things
            {
                VertexLayout = VertexBuffer.Item?.Layout.Item?.VertexLayout;
                VertexData = Rpf3Crypto.Swap(VertexBuffer.Item.Data1.Items ?? VertexBuffer.Item.Data2.Items);

                if (VertexCount == 0)
                {
                    VertexCount = VertexBuffer.Item.VertexCount;
                }
            }

            //Swap MCLA axis + endianess
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
                            var np1 = FloatUtil.Vector4ToDec3N(new Vector4(pv.Z, pv.X, pv.Y, pv.W)); //Convert Vector4 back to Dec3N with MCLA axis
                            BufferUtil.WriteUint(numArray, index + elemoffset, np1);
                            break;
                        case VertexElementFormat.Half2:
                            var half2 = BufferUtil.ReadStruct<Half2>(numArray, index + elemoffset);
                            half2 = new Half2(half2.Y, half2.X);
                            BufferUtil.WriteStruct(numArray, index + elemoffset, ref half2);
                            break;
                        case VertexElementFormat.UShort2N:
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
            VertexCount = VertexBuffer.Item != null ? VertexBuffer.Item.VertexCount : 0;
            VertexStride = (int)(VertexBuffer.Item != null ? VertexBuffer.Item.VertexStride : 0);
            IndicesCount = IndexBuffer.Item != null ? IndexBuffer.Item.IndicesCount : 0;
            TrianglesCount = IndicesCount / 3;
            throw new NotImplementedException();
        }

        public void SetShaderRef(Rsc5Shader shader)
        {
            ShaderRef = shader;
            if (shader != null)
            {
                switch (new JenkHash(shader.ShaderName.ToString()))
                {
                    case 0xE4CB95DC: //CityTerrain
                        SetupTerrainShader(shader);
                        break;
                    case 0xDAFA8999: //CityRoad
                    default:
                        SetupDefaultShader(shader);
                        break;
                }

                var bucket = shader.DrawBucket;
                switch (bucket)
                {
                    case 0: ShaderBucket = ShaderBucket.Solid; break; //solid
                    case 1: ShaderBucket = ShaderBucket.Alpha1; break; //alpha 
                    case 2: ShaderBucket = ShaderBucket.Decal1; break; //decal
                    case 3: ShaderBucket = ShaderBucket.Alpha1; break; //cutout
                    case 6: ShaderBucket = ShaderBucket.Alpha1; break; //water
                    case 7: ShaderBucket = ShaderBucket.Alpha1; break; //glass
                    default:
                        break;
                }
            }
        }

        private void SetupDefaultShader(Rsc5Shader s)
        {
            SetDefaultShader();
            ShaderInputs = Shader.CreateShaderInputs();
            ShaderInputs.SetFloat(0xDF918855, 1.0f); //"BumpScale"
            ShaderInputs.SetFloat(0x4D52C5FF, 1.0f); //"AlphaScale"

            if (s == null || s.Params == null) return;
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
                        }
                    }
                }
            }

            var db = s.DrawBucket;
            if (db == 2)
            {
                var decalMasks = Vector4.One; //albedo, normal, params, irradiance
                var decalMode = 1u;
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

            ShaderInputs.SetUInt32(0x9B920BD, 1); //BlendMode
            if (s == null || s.Params == null) return;

            Textures = new Texture[6]; //albedo(0-3); normal(0-3);
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
                            case 0xd52b11df: //diffusesamplera
                                Textures[0] = tex;
                                break;
                            case 0x2420afd1: //diffusesamplerb
                                Textures[1] = tex;
                                break;
                            case 0x31934ab6: //diffusesamplerc
                                Textures[2] = tex;
                                break;
                            case 0x3fff9563: //normalsamplera
                                Textures[4] = tex;
                                break;
                            case 0x54cdbeff: //normalsamplerb
                                Textures[5] = tex;
                                break;
                            case 0xa3a2dca8: //normalsamplerc
                                Textures[6] = tex;
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
                            ShaderInputs.SetFloat4(0x7CB163F5, new Vector4(parm.Vector.X)); //BumpScales
                            break;
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

    [TC(typeof(EXP))] public class Rsc5IndexBuffer : Rsc5BlockBase
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

    [TC(typeof(EXP))] public class Rsc5VertexBuffer : Rsc5BlockBase
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

    [TC(typeof(EXP))] public class Rsc5Bone : Bone, Rsc5Block
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

    [TC(typeof(EXP))] public class Rsc5ShaderGroup : Rsc5BlockBase
    {
        public override ulong BlockLength => 16;
        public ulong VFT { get; set; }
        public uint BlockMap { get; set; }
        public Rsc5Ptr<Rsc5TextureDictionary> TextureDictionary { get; set; }
        public Rsc5PtrArr<Rsc5Shader> Shaders { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            BlockMap = reader.ReadUInt32();
            Shaders = reader.ReadPtrArr<Rsc5Shader>();

            if (Shaders.Items != null)
            {
                XapbFile.Textures ??= new List<Rsc5Texture>();
                foreach (var shader in Shaders.Items)
                {
                    if (shader.Params == null) continue;
                    foreach (var param in shader.Params)
                    {
                        if (param == null || param.Texture == null) continue;
                        if (XapbFile.Textures.Contains(param.Texture)) continue;
                        XapbFile.Textures.Add(param.Texture);
                    }
                }
            }
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
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
                        p.Texture = reader.ReadBlock<Rsc5Texture>(ptrs[i]);
                        break;
                    case 1: //vector4
                        p.Vector = Rpf3Crypto.Swap(reader.ReadVector4(ptrs[i]));
                        break;
                    default: //array
                        p.Array = Rpf3Crypto.Swap(reader.ReadArray<Vector4>(p.Type, ptrs[i]));
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

    [TC(typeof(EXP))] public class Rsc5ShaderParameter
    {
        public JenkHash Hash { get; set; }
        public byte Type { get; set; } //0: texture, 1: vector4, 2+: vector4 array
        public Vector4 Vector { get; set; }
        public Vector4[] Array { get; set; }
        public Rsc5Texture Texture { get; set; }

        public override string ToString()
        {
            return Hash.ToString() + ": " + ((Type == 0) ? ("texture: " + Texture?.ToString() ?? "(none)") : ((Type > 1) ? ("array: count " + Type.ToString()) : ("vector4: " + Vector.ToString())));
        }

        public enum Rsc5ShaderParamType : byte
        {
            Texture = 0,
            Vector = 1
        }
    }
}
