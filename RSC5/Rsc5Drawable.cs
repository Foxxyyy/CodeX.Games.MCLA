using CodeX.Core.Engine;
using CodeX.Core.Numerics;
using CodeX.Core.Shaders;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.Files;
using CodeX.Games.MCLA.RPF3;
using System.Numerics;
using EXP = System.ComponentModel.ExpandableObjectConverter;
using TC = System.ComponentModel.TypeConverterAttribute;

namespace CodeX.Games.MCLA.RSC5
{
    [TC(typeof(EXP))] public class Rsc5AmbientDrawablePed : Rsc5BlockBaseMap //.xapb
    {
        public override ulong BlockLength => 64;
        public override uint VFT { get; set; } = 0;
        public Rsc5Ptr<Rsc5DrawableBase> Drawable { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            Drawable = reader.ReadPtr<Rsc5DrawableBase>();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [TC(typeof(EXP))] public class Rsc5CitySector : Rsc5BlockBaseMap //.xcs root
    { 
        public override ulong BlockLength => 40;
        public override uint VFT { get; set; } = 0;
        public Rsc5Ptr<Rsc5TextureDictionary> TextureDictionary { get; set; }
        public uint Unknown_Ch { get; set; }
        public Rsc5Ptr<Rsc5CitySectorPiece> SectorPiece { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            TextureDictionary = reader.ReadPtr<Rsc5TextureDictionary>();
            Unknown_Ch = reader.ReadUInt32();
            SectorPiece = reader.ReadPtr<Rsc5CitySectorPiece>();    
            
            TexturePack texpack = null;
            var txdict = TextureDictionary.Item.Dict;
            
            if (txdict != null)
            {
                texpack = new TexturePack(reader.FileEntry)
                {
                    Textures = []
                };

                foreach (var kvp in txdict)
                {
                    texpack.Textures[kvp.Key.ToString()] = kvp.Value;
                }
            }

            if (SectorPiece.Item != null)
            {
                var piece = SectorPiece.Item;
                piece.TexturePack = texpack;
                piece.ApplyTextures(TextureDictionary.Item);
            }
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [TC(typeof(EXP))] public class Rsc5MapDistrictLod : Piece, IRsc5Block, IRsc5DrawableRoot //.xmd root
    {
        public ulong BlockLength => 28;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public uint VFT { get; set; } = 0x00596410;
        public Rsc5Ptr<Rsc5BlockMap> BlockMap { get; set; }
        public Rsc5Ptr<Rsc5ShaderGroup> ShaderGroupPtr { get; set; }
        public Rsc5Arr<Vector4> CullingPlanes { get; set; } //Normalized plane normals. W component is the distance from origin.

        public Rsc5DrawableLod Lod { get; set; }
        public Rsc5ShaderGroup ShaderGroup => ShaderGroupPtr.Item;

        public Rsc5MapDistrictLod()
        {
        }

        public Rsc5MapDistrictLod(Rsc5DrawableModel model, Rsc5ShaderGroup shaderGroup, string name)
        {
            ShaderGroupPtr = new Rsc5Ptr<Rsc5ShaderGroup>()
            {
                Item = shaderGroup
            };

            var lod = new Rsc5DrawableLod
            {
                ModelsData = new Rsc5PtrArr<Rsc5DrawableModel>()
                {
                    Items = [model]
                }
            };

            lod.Models = lod.ModelsData.Items;
            lod.LodDist = 9999.0f;
            Lod = lod;

            Lods = [lod];
            Name = name;

            UpdateAllModels();
            AssignShaders();
            UpdateBounds();
        }

        public void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            BlockMap = reader.ReadPtr<Rsc5BlockMap>();
            ShaderGroupPtr = reader.ReadPtr<Rsc5ShaderGroup>();
            Lod = reader.ReadBlock<Rsc5DrawableLod>();
            CullingPlanes = reader.ReadArr<Vector4>();

            if (Lod != null) Lod.LodDist = 9999f;
            Lods = [Lod];
            Name = Path.GetFileNameWithoutExtension(reader.FileEntry.Name);

            UpdateAllModels();
            AssignShaders();
            UpdateBounds();
        }

        public void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public void AssignShaders()
        {
            if (ShaderGroup?.Shaders.Items == null) return;
            if (Lod == null) return;

            var shaders = ShaderGroup.Shaders.Items;
            var models = Lod.ModelsData.Items;

            foreach (var model in models)
            {
                if (model == null || model.Geometries.Items == null) continue;

                var geomList = model.Geometries.Items;
                var shaderMapping = model.ShaderMapping.Items;

                for (int i = 0; i < geomList.Length; i++)
                {
                    var geom = geomList[i];
                    if (geom == null) continue;

                    ushort shaderId = 0;
                    if (shaderMapping != null && i < shaderMapping.Length)
                    {
                        shaderId = shaderMapping[i];
                    }

                    if (shaderId < shaders.Length)
                    {
                        var shader = shaders[shaderId];
                        geom.SetShaderRef(shader, model);
                    }
                }
            }
        }
    }

    [TC(typeof(EXP))] public class Rsc5DrawableBase : Piece, IRsc5Block
    {
        public ulong BlockLength => 116;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public uint VFT { get; set; } = 0x00516D84;
        public Rsc5Ptr<Rsc5BlockMap> BlockMap { get; set; }
        public Rsc5Ptr<Rsc5ShaderGroup> ShaderGroup { get; set; }
        public Rsc5Ptr<Rsc5SkeletonData> SkeletonRef { get; set; }
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
            SkeletonRef = reader.ReadPtr<Rsc5SkeletonData>();
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

            Lods =
            [
                DrawableModelsHigh.Item,
                DrawableModelsMed.Item,
                DrawableModelsLow.Item,
                DrawableModelsVlow.Item
            ];

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
                                mesh.SetShaderRef(shader, model);
                            }
                        }
                    }
                }
            }
        }

        public void SetSkeleton(Rsc5SkeletonData skel)
        {
            Skeleton = skel;
            if (AllModels == null) return;

            var bones = skel?.Bones;
            if (bones == null) return;

            var origbones = (skel != SkeletonRef.Item) ? SkeletonRef.Item.BoneData.Items : null;
            foreach (var model in AllModels.Cast<Rsc5DrawableModel>())
            {
                if (model == null) continue;
                if (model.Meshes == null) continue;

                var boneidx = model.MatrixIndex;
                if ((model.SkinFlag == 0) && (boneidx < bones.Length))
                {
                    if (model.Meshes != null)
                    {
                        foreach (var mesh in model.Meshes)
                        {
                            mesh.BoneIndex = boneidx;
                            if ((boneidx < 0) && (bones.Length > 1))
                            {
                                mesh.Enabled = false;
                            }
                        }
                    }
                }
                else if (model.SkinFlag == 1)
                {
                    foreach (var mesh in model.Meshes)
                    {
                        if (mesh is not Rsc5DrawableGeometry geom) continue;
                        var boneids = geom.BoneIds.Items;
                        if (boneids != null)
                        {
                            var boneinds = new int[boneids.Length];
                            for (int i = 0; i < boneinds.Length; i++)
                            {
                                if (origbones != null) //Make sure to preseve original bone ordering!
                                {
                                    var origbone = origbones[boneids[i]];
                                    if ((origbone != null) && skel.BonesMap.TryGetValue(origbone.ID, out var newbone))
                                    {
                                        boneinds[i] = newbone.Index;
                                    }
                                    else
                                    {
                                        boneinds[i] = boneids[i];
                                    }
                                }
                                else
                                {
                                    boneinds[i] = boneids[i];
                                }
                            }
                            geom.Rig = new SkeletonRig(skel, true, boneinds);
                        }
                        else
                        {
                            geom.Rig = new SkeletonRig(skel, true);
                        }
                        geom.RigMode = MeshRigMode.MeshRig;
                        geom.IsSkin = true;
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
                Textures = []
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

    [TC(typeof(EXP))] public class Rsc5Drawable : Piece, IRsc5Block
    {
        public ulong BlockLength => 160;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public Rsc5DrawableLod Lod { get; set; }

        public void Read(Rsc5DataReader reader)
        {
            Lod = reader.ReadBlock<Rsc5DrawableLod>();
            if (Lod != null)
            {
                Lod.LodDist = 9999f;
            }

            Lods = [Lod];
            Name = Path.GetFileNameWithoutExtension(reader.FileEntry.Name);

            UpdateAllModels();
            AssignShaders();
            UpdateBounds();
        }

        public void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public void AssignShaders()
        {
            if (AllModels == null) return;
            foreach (var model in AllModels.Cast<Rsc5DrawableModel>())
            {
                var geoms = model?.Geometries.Items;
                if (geoms == null) continue;

                var geomcount = geoms.Length;
                for (int i = 0; i < geomcount; i++)
                {
                    var geom = geoms[i];
                    geom.SetDefaultShader();
                    geom.SetDefaultTextures(Texture.CreateOnePx(Colour.White), null, null);
                    geom.ShaderInputs = geom.Shader.CreateShaderInputs();
                }
            }
        }

        public void SetSkeleton(Rsc5SkeletonData skel)
        {
            Skeleton = skel;
            if (AllModels == null) return;

            var bones = skel?.Bones;
            if (bones == null) return;

            foreach (var model in AllModels.Cast<Rsc5DrawableModel>())
            {
                if (model == null) continue;
                if (model.Meshes == null) continue;

                var boneidx = model.MatrixIndex;
                if ((model.SkinFlag == 0) && (boneidx < bones.Length))
                {
                    if (model.Meshes != null)
                    {
                        foreach (var mesh in model.Meshes)
                        {
                            mesh.BoneIndex = boneidx;
                            if ((boneidx < 0) && (bones.Length > 1))
                            {
                                mesh.Enabled = false;
                            }
                        }
                    }
                }
                else if (model.SkinFlag == 1)
                {
                    foreach (var mesh in model.Meshes)
                    {
                        if (mesh is not Rsc5DrawableGeometry geom) continue;
                        var boneids = geom.BoneIds.Items;
                        if (boneids != null)
                        {
                            var boneinds = new int[boneids.Length];
                            for (int i = 0; i < boneinds.Length; i++)
                            {
                                boneinds[i] = boneids[i];
                            }
                            geom.Rig = new SkeletonRig(skel, true, boneinds);
                        }
                        else
                        {
                            geom.Rig = new SkeletonRig(skel, true);
                        }
                        geom.RigMode = MeshRigMode.MeshRig;
                        geom.IsSkin = true;
                    }
                }
            }
        }
    }

    [TC(typeof(EXP))] public class Rsc5CitySectorPiece : Piece, IRsc5Block, IRsc5DrawableRoot
    {
        public virtual ulong BlockLength => 160;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;

        public uint VFT { get; set; } = 0x00595E80;
        public Rsc5Ptr<Rsc5BlockMap> BlockMap { get; set; }
        public Rsc5Ptr<Rsc5ShaderGroup> ShaderGroupPtr { get; set; }
        public Rsc5Ptr<Rsc5DrawableLodMap> LodPtr { get; set; }

        public Rsc5DrawableLod Lod => LodPtr.Item?.Drawables;
        public Rsc5ShaderGroup ShaderGroup => ShaderGroupPtr.Item;

        public virtual void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            BlockMap = reader.ReadPtr<Rsc5BlockMap>();
            ShaderGroupPtr = reader.ReadPtr<Rsc5ShaderGroup>();
            LodPtr = reader.ReadPtr<Rsc5DrawableLodMap>();

            Lods = [Lod];
            if (Lod != null) Lod.LodDist = 9999f;

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

        public void ApplyTextures(Dictionary<JenkHash, Rsc5Texture> dict)
        {
            if (AllModels == null || dict == null) return;
            foreach (var model in AllModels)
            {
                if (model?.Meshes == null) continue;
                foreach (var mesh in model.Meshes)
                {
                    if (mesh?.Textures == null) continue;
                    for (int i = 0; i < mesh.Textures.Length; i++)
                    {
                        var texture = mesh.Textures[i];
                        if (texture?.Name == null) continue;

                        var normalized = Rpf3Crypto.NormalizeTexName(texture.Name);
                        var hash = JenkHash.GenHash(normalized + ".dds");

                        if (dict.TryGetValue(hash, out var newTexture))
                        {
                            mesh.Textures[i] = newTexture;
                            mesh.Textures[i].Name = normalized;
                        }
                    }
                }
            }
        }

        public void ApplyTextures(Rsc5TextureDictionary txd)
        {
            if (txd?.Dict == null) return;
            ApplyTextures(txd.Dict);
        }

        public void AssignGeometryShaders() //Assign embedded textures to mesh for rendering
        {
            if ((ShaderGroup?.Shaders.Items != null) && (AllModels != null))
            {
                var shaders = ShaderGroup.Shaders.Items;
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
                                mesh.SetShaderRef(shader, model);
                            }
                        }
                    }
                }
            }
        }
    }

    [TC(typeof(EXP))] public class Rsc5DrawableLodMap : Rsc5BlockBaseMap
    {
        public override ulong BlockLength => 32;
        public override uint VFT { get; set; } = 0x005960EC;
        public uint ParentDictionary { get; set; }
        public uint RefCount { get; set; } = 1;
        public Rsc5Arr<JenkHash> Hashes { get; set; }
        public Rsc5DrawableLod Drawables { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            ParentDictionary = reader.ReadUInt32();
            RefCount = reader.ReadUInt32();
            Hashes = reader.ReadArr<JenkHash>();
            Drawables = reader.ReadBlock<Rsc5DrawableLod>();

            var hashes = Hashes.Items;
            var drawables = Drawables.Models;

            for (int i = 0; i < drawables.Length; i++)
            {
                var drawable = drawables[i];
                var hash = (i < hashes.Length) ? hashes[i] : 0;
                drawable.Name = hash.ToString();
            }
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [TC(typeof(EXP))] public class Rsc5DrawableLod : PieceLod, IRsc5Block
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

    [TC(typeof(EXP))] public class Rsc5DrawableModel : Model, IRsc5Block //rage::grmModel
    {
        public ulong BlockLength => 28;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public ulong VFT { get; set; } = 0x005A4B0C;
        public Rsc5PtrArr<Rsc5DrawableGeometry> Geometries { get; set; } //m_Geometries
        public Rsc5RawArr<Vector4> BoundsData { get; set; } //m_AABBs, one for each geometry + one for the whole model (unless there's only one model)
        public Rsc5RawArr<ushort> ShaderMapping { get; set; } //m_ShaderIndex
        public byte MatrixCount { get; set; } //m_MatrixCount, bone count
        public byte Flags { get; set; } //m_Flags
        public byte Type { get; set; } = 0xCD; //m_Type, always 0xCD?
        public byte MatrixIndex { get; set; } //m_MatrixIndex
        public byte Stride { get; set; } //m_Stride, always 0?
        public byte SkinFlag { get; set; } //m_SkinFlag, determine whether to render with the skinned draw path or not
        public ushort GeometriesCount { get; set; } //m_Count

        public BoundingBox BoundingBox { get; set; } //Created from first GeometryBounds item

        public void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            Geometries = reader.ReadPtrArr<Rsc5DrawableGeometry>();
            BoundsData = reader.ReadRawArrPtr<Vector4>();
            ShaderMapping = reader.ReadRawArrPtr<ushort>();
            MatrixCount = reader.ReadByte();
            Flags = reader.ReadByte();
            Type = reader.ReadByte();
            MatrixIndex = reader.ReadByte();
            Stride = reader.ReadByte();
            SkinFlag = reader.ReadByte();
            GeometriesCount = reader.ReadUInt16();

            var geocount = Geometries.Count;
            ShaderMapping = reader.ReadRawArrItems(ShaderMapping, geocount);
            BoundsData = reader.ReadRawArrItems(BoundsData, geocount > 1 ? geocount + 1u : geocount);

            var geoms = Geometries.Items;
            if (geoms != null)
            {
                var shaderMapping = ShaderMapping.Items;
                BoundingBox4[] boundsData = null;

                if (BoundsData.Items != null && BoundsData.Items.Length > 0)
                {
                    var vecs = BoundsData.Items;
                    boundsData = new BoundingBox4[vecs.Length];

                    for (int i = 0; i < vecs.Length; i++)
                    {
                        var v = vecs[i];
                        var vMin = new Vector3(v.X - v.W, v.Y - v.W, v.Z - v.W);
                        var vMax = new Vector3(v.X + v.W, v.Y + v.W, v.Z + v.W);
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

                        //MCLA has NULL AABBs sometimes, so we have to calculate them manually
                        if (boundsData == null)
                        {
                            geom.UpdateBounds();
                        }
                    }
                }

                if ((boundsData != null) && (boundsData.Length > 0))
                {
                    ref var bb = ref boundsData[0];
                    BoundingBox = new BoundingBox(bb.Min.XYZ(), bb.Max.XYZ());
                }
            }

            Meshes = geoms;
            RenderInMainView = true;
            RenderInShadowView = true;
            RenderInEnvmapView = true;
        }

        public void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            if (Geometries.Count > 1)
            {
                var verts = 0;
                foreach (var geo in Geometries.Items)
                {
                    verts += geo.VertexCount;
                }
                return string.Format("{0} verts, {1} geometries", verts, Geometries.Count);
            }
            return Geometries.Items[0].ToString();
        }
    }

    [TC(typeof(EXP))] public class Rsc5DrawableGeometry : Mesh, IRsc5Block
    {
        public ulong BlockLength => 80;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;

        public uint VFT { get; set; } = 0x00557754;
        public uint Unknown_4h { get; set; }
        public uint Unknown_8h { get; set; }
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
        public ushort PrimitiveType { get; set; } = 3; //m_PrimType, rendering primitive type
        public Rsc5RawArr<ushort> BoneIds { get; set; } //m_MtxPalette, matrix palette for this geometry
        public ushort BoneIdsCount { get; set; } //m_MtxCount, the number of matrices in the matrix paletter
        public Rsc5RawArr<byte> VertexDataRef { get; set; }
        public uint OffsetBuffer { get; set; } //m_OffsetBuffer, PS3 only I think
        public uint IndexOffset { get; set; } //m_IndexOffset, PS3 only I think
        public uint Unknown_3Ch { get; set; }

        public Rsc5Shader ShaderRef { get; set; }
        public bool RoadMaterial { get; set; } //Albedo is composited from the control and decal maps
        public ushort ShaderID { get; set; } //Read-written by parent model
        public BoundingBox4 AABB { get; set; } //Read-written by parent model

        public void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            Unknown_4h = reader.ReadUInt32();
            Unknown_8h = reader.ReadUInt32();
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
            PrimitiveType = reader.ReadUInt16();
            BoneIds = reader.ReadRawArrPtr<ushort>();
            VertexStride = reader.ReadUInt16();
            BoneIdsCount = reader.ReadUInt16();
            VertexDataRef = reader.ReadRawArrPtr<byte>();
            OffsetBuffer = reader.ReadUInt32();
            IndexOffset = reader.ReadUInt32();
            Unknown_3Ch = reader.ReadUInt32();
            BoneIds = reader.ReadRawArrItems(BoneIds, BoneIdsCount);

            if (VertexBuffer.Item != null) //Hack to fix stupid "locked" things
            {
                VertexLayout = VertexBuffer.Item.Layout.Item.VertexLayout;
                VertexData = Rpf3Crypto.Swap(VertexBuffer.Item.LockedData.Items ?? VertexBuffer.Item.VertexData.Items);

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
                    var elemoffset = elem.Offset;

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
                        case VertexElementFormat.Colour:
                            var color = BufferUtil.ReadColour(VertexData, index + elemoffset);
                            color = new Colour(color.B, color.G, color.R, color.A);
                            BufferUtil.WriteColour(VertexData, index + elemoffset, ref color);
                            break;
                        case VertexElementFormat.Dec3N:
                            var packed = BufferUtil.ReadUint(numArray, index + elemoffset);
                            var pv = Rpf3Crypto.Dec3NToVector4(packed); //Convert Dec3N to Vector4
                            var np1 = Rpf3Crypto.Vector4ToDec3N(new Vector4(pv.Z, pv.X, pv.Y, pv.W)); //Convert Vector4 back to Dec3N with MCLA axis
                            BufferUtil.WriteUint(numArray, index + elemoffset, np1);
                            break;
                        case VertexElementFormat.Half2:
                            var half2 = BufferUtil.ReadStruct<Half2>(numArray, index + elemoffset);
                            half2 = new Half2(half2.Y, half2.X);
                            BufferUtil.WriteStruct(numArray, index + elemoffset, ref half2);
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
            writer.WriteUInt32(VFT);
            writer.WriteUInt32(Unknown_4h);
            writer.WriteUInt32(Unknown_8h);
            writer.WritePtr(VertexBuffer);
            writer.WritePtr(VertexBuffer2);
            writer.WritePtr(VertexBuffer3);
            writer.WritePtr(VertexBuffer4);
            writer.WritePtr(IndexBuffer);
            writer.WritePtr(IndexBuffer2);
            writer.WritePtr(IndexBuffer3);
            writer.WritePtr(IndexBuffer4);
            writer.WriteUInt32(IndicesCount);
            writer.WriteUInt32(TrianglesCount);
            writer.WriteUInt16((ushort)VertexCount);
            writer.WriteUInt16(PrimitiveType);
            writer.WriteRawArr(BoneIds);
            writer.WriteUInt16((ushort)VertexStride);
            writer.WriteUInt16(BoneIdsCount);
            writer.WriteUInt32(0xCDCDCDCD); //VertexDataRef
            writer.WriteUInt32(OffsetBuffer);
            writer.WriteUInt32(IndexOffset);
            writer.WriteUInt32(Unknown_3Ch);
        }

        public void SetShaderRef(Rsc5Shader shader, Model model)
        {
            ShaderRef = shader;
            if (shader != null)
            {
                var shaderName = shader.ShaderName.ToString().ToLower();
                var hash = new JenkHash(shaderName);
                Name = Path.GetFileNameWithoutExtension(shader.ShaderFileName.ToString());

                switch (hash)
                {
                    case 0xD2492FB1: //CityGrass
                        SetupGrassTerrainShader(shader);
                        break;
                    case 0xE4CB95DC: //CityTerrain
                        SetupTerrainShader(shader);
                        break;
                    case 0xDAFA8999: //CityRoad
                        SetupDecalGrimeShader(shader);
                        break;
                    case 0x603A18C1: //CityDecal
                        SetupDecalShader(shader);
                        break;
                    //case 0xACB3CD30: //CityPondWater
                    //case 0xC196451F: //CityOceanShore
                    //case 0x4D36038E: //CityOceanWater
                        //SetupWaterShader(shader, model);
                        //break;
                    case 0x4F8CB366: //CityGroundGrimeDecal
                    case 0x27DAEE8D: //CityGrime
                    case 0x8723C5BE: //CityGrimeDecal
                    case 0x300437A8: //CityGroundGrime
                    case 0x478552AF: //CityGroundDecalGrime
                        SetupDecal2Shader(shader);
                        break;
                    default:
                        SetupDefaultShader(shader);
                        break;
                }

                //The alpha channel of an MCLA diffuse map is a specular/gloss mask, not opacity -
                //only shaders that declare a non-solid draw bucket really blend. Handing the mask
                //to the core shader punches holes through skin, beards and car bodies.
                var opaque = shader.DrawBucket == 0;

                switch (hash)
                {
                    case 0x61C0C8F9: //CityNormalMap
                        ShaderInputs.SetFloat(0xDF918855, 0.5f); //BumpScale
                        break;
                    case 0xA6DD4FC1: //CityWindowLOD
                    case 0x816FF892: //CityWindowUpper
                        opaque = true;
                        break;
                }
                ShaderInputs.SetFloat(0x4D52C5FF, opaque ? 0.0f : 1.0f); //AlphaScale

                var bucket = shader.DrawBucket;
                switch (bucket)
                {
                    case 0: ShaderBucket = ShaderBucket.Solid; break; //Solid
                    case 1: ShaderBucket = ShaderBucket.Alpha1; break; //Alpha 
                    case 2: ShaderBucket = ShaderBucket.Decal1; break; //Decal
                    case 3: ShaderBucket = ShaderBucket.Alpha1; break; //Cutout
                    case 6: ShaderBucket = ShaderBucket.Alpha1; break; //Water
                    case 7: ShaderBucket = ShaderBucket.Alpha1; break; //Glass
                    default: break;
                }
            }
        }

        private void SetupDefaultShader(Rsc5Shader s)
        {
            SetDefaultShader();
            ShaderInputs = Shader.CreateShaderInputs();

            if (s == null || s.Params == null) return;
            Textures = new Texture[2];

            var sfresnel = 0.96f;
            var sintensitymult = 0.2f;
            var sfalloffmult = 35.0f;

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
                            case 0x3C870418: //neonsampler
                            case 0xF1FE2B71: //diffusesampler
                            case 0x50022388: //platebgsampler
                            case 0x1cf5b657: //texturesamp
                            case 0x605fcc60: //distancemapsampler
                                Textures[0] = tex;
                                break;
                            case 0x46B7C64F: //bumpsampler
                            case 0x65DF0BCE: //platebgbumpsampler
                            case 0x8AC11CB0: //normalsampler
                            case 0x332EBE1C: //heightspecularsampler
                                Textures[1] = tex;
                                break;
                        }
                    }
                }
                else
                {
                    switch (parm.Hash)
                    {
                        case 0xF6712B81: //bumpiness
                            ShaderInputs.SetFloat(0xDF918855, parm.Vector.X); //BumpScale
                            break;
                        case 0x484A5EBD: //specularcolorfactor //0-1, final multiplier?
                            sintensitymult = parm.Vector.X;
                            break;
                        case 0x166E0FD1: //specularfactor //10-150+?, higher is shinier
                        case 0x93D520E8: //specularexponent //10-150+?, higher is shinier
                            sfalloffmult = parm.Vector.X;
                            break;
                    }
                }
            }

            ShaderInputs.SetFloat(0x57C22E45, FloatUtil.Saturate(sfalloffmult / 100.0f)); //MeshParamsMult
            ShaderInputs.SetFloat(0xDA9702A9, FloatUtil.Saturate(sintensitymult * (1.0f - ((sfresnel - 0.1f) / 0.896f)))); //MeshMetallicity

            var db = s.DrawBucket;
            if (db == 3)
            {
                ShaderInputs.SetUInt32(0x26E8F6BB, 1); //NormalDoubleSided, flip normals if they are facing away from the camera
            }
        }

        private void SetupDecalShader(Rsc5Shader s)
        {
            SetCoreShader<BlendShader>(ShaderBucket.Solid);
            ShaderInputs = Shader.CreateShaderInputs();
            ShaderInputs.SetUInt32(0x9B920BD, 27); //BlendMode

            if (s == null || s.Params == null) return;
            Textures = new Texture[2];

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
                            case 0xF1FE2B71: //diffusesampler
                            case 0x2b5170fd: //texturesampler
                            case 0x3e19076b: //detailmapsampler
                            case 0x605fcc60: //distancemapsampler
                                Textures[0] = tex;
                                break;
                            case 0xE3381C99: //grimesampler
                            case 0xA79AEEC0: //decalsampler
                                Textures[1] = tex;
                                break;
                        }
                    }
                }
            }
        }

        private void SetupDecal2Shader(Rsc5Shader s)
        {
            SetCoreShader<BlendShader>(ShaderBucket.Solid);
            ShaderInputs = Shader.CreateShaderInputs();
            ShaderInputs.SetUInt32(0x9B920BD, 28); //BlendMode

            if (s == null || s.Params == null) return;
            Textures = new Texture[2];

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
                            case 0xF1FE2B71: //diffusesampler
                            case 0x2b5170fd: //texturesampler
                            case 0x3e19076b: //detailmapsampler
                            case 0x605fcc60: //distancemapsampler
                                Textures[0] = tex;
                                break;
                            case 0xE3381C99: //grimesampler
                            case 0xA79AEEC0: //decalsampler
                                Textures[1] = tex;
                                break;
                        }
                    }
                }
            }
        }

        //Road materials. The diffuse map here is NOT albedo: the game's own pixel shader samples
        //it as "DiffuseSampler.yx" and feeds those two channels into the specular tint and a
        //luminance term only, while the decal map is sampled ".xyzw" and carries the markings.
        //Blue is never read so it sits at zero and the green holds the detail, which is what made
        //every road and sidewalk render green.
        private void SetupDecalGrimeShader(Rsc5Shader s)
        {
            SetCoreShader<BlendShader>(ShaderBucket.Solid);
            ShaderInputs = Shader.CreateShaderInputs();
            ShaderInputs.SetUInt32(0x9B920BD, 29); //BlendMode

            if (s == null || s.Params == null) return;
            Textures = new Texture[4];

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
                            case 0xF1FE2B71: //diffusesampler, the control map
                            case 0x2b5170fd: //texturesampler
                            case 0x3e19076b: //detailmapsampler
                            case 0x605fcc60: //distancemapsampler
                                Textures[1] = tex;
                                break;
                            case 0xA79AEEC0: //decalsampler, the markings overlay
                                Textures[2] = tex;
                                break;
                            case 0xE3381C99: //grimesampler
                                Textures[3] = tex;
                                break;
                        }
                    }
                }
            }

            //Neither map is an albedo on its own, so the pair gets composited into one - see
            //Rsc5RoadMaterial. That can only happen once the textures have been resolved by the
            //file manager, so it just gets flagged here.
            RoadMaterial = true;
            Textures[0] = Rsc5RoadMaterial.GetAlbedo(Textures[1], Textures[2]);
        }

        private void SetupGrassTerrainShader(Rsc5Shader s)
        {
            SetCoreShader<BlendShader>(ShaderBucket.Solid);
            ShaderInputs = Shader.CreateShaderInputs();
            ShaderInputs.SetUInt32(0x9B920BD, 30); //BlendMode

            if (s == null || s.Params == null) return;
            Textures = new Texture[4];

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
                            case 0xC9A79FED: //diffusesamplera
                                Textures[0] = tex;
                                break;
                            case 0xF7357B08: //diffusesamplerb
                                Textures[1] = tex;
                                break;
                            case 0xA4CFD63A: //diffusesamplerc
                                Textures[2] = tex;
                                break;
                            case 0x8BFCEF8D: //channelmapsampler
                                Textures[3] = tex;
                                break;
                        }
                    }
                }
            }
        }

        private void SetupTerrainShader(Rsc5Shader s)
        {
            SetCoreShader<BlendShader>(ShaderBucket.Solid);
            ShaderInputs = Shader.CreateShaderInputs();
            ShaderInputs.SetUInt32(0x9B920BD, 31); //BlendMode
            ShaderInputs.SetFloat4(0x7CB163F5, new Vector4(0.5f)); //BumpScales

            if (s == null || s.Params == null) return;
            Textures = new Texture[7];

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
                            case 0xC9A79FED: //diffusesamplera
                                Textures[0] = tex;
                                break;
                            case 0xF7357B08: //diffusesamplerb
                                Textures[1] = tex;
                                break;
                            case 0xA4CFD63A: //diffusesamplerc
                                Textures[2] = tex;
                                break;
                            case 0x8BFCEF8D: //channelmapsampler
                                Textures[3] = tex;
                                break;
                            case 0xBE97CA14: //normalsamplera
                                Textures[4] = tex;
                                break;
                            case 0xCCDCE69E: //normalsamplerb
                                Textures[5] = tex;
                                break;
                            case 0xA8419D68: //normalsamplerc
                                Textures[6] = tex;
                                break;
                        }
                    }
                }
            }
        }

        private void SetupWaterShader(Rsc5Shader s, Model model) //be_smb_beach_03, hw_ws_macauthupark
        {
            SetCoreShader<RefractShader>(ShaderBucket.Translucency);
            ShaderInputs = Shader.CreateShaderInputs();
            ShaderInputs.SetUInt32(0x047FEB72, 1); //NormalsMode - animated ocean normals
            ShaderInputs.SetUInt32(0x5598A4F3, 0); //RefractMode - SSR water
            ShaderInputs.SetUInt32(0xC7AFBF36, 1); //WaterFlowUVSource - texcoord1
            ShaderInputs.SetUInt32(0xD7CA292B, 1); //NormalsSource - always up (0,0,1)
            ShaderInputs.SetFloat3(0x22963AF5, new(0.025f, 0.025f, 0.35f)); //WaterRippleSize (Z = wave height)
            ShaderInputs.SetFloat(0xA52368EA, 0.8f); //WaterRippleSpeed
            ShaderInputs.SetFloat(0x16557DF5, 0.02f); //WaterRippleHeight
            ShaderInputs.SetFloat(0xCC9749CA, 5.0f); //RefractEdgeBlend
            ShaderInputs.SetFloat4(0x9A7F494F, new Vector4(0.1f, 0.05f, 0.02f, 0)); //RefractAbsorption
            ShaderInputs.SetFloat4(0xF6B32D0C, new Vector4(1, 0, 0, 0)); //WaterFlowTextureMaskX
            ShaderInputs.SetFloat4(0xE06E0082, new Vector4(0, 1, 0, 0)); //WaterFlowTextureMaskY

            model.RenderInShadowView = false;
            if (s == null || s.Params == null) return;
            Textures = new Texture[4];

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
                            case 0xF1FE2B71: //diffusesampler
                                Textures[0] = parm.Texture;
                                break;
                            case 0x63F7C0E8: //wavefoamsampler
                                Textures[1] = parm.Texture;
                                break;
                            case 0xC2B08918: //foamsampler
                                Textures[2] = parm.Texture;
                                break;
                            case 0x00A67ACD: //ripple normal map
                                Textures[3] = parm.Texture;
                                break;
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return Name + ", " + (ShaderRef?.ToString() ?? "NULL SHADER") + ", " + VertexCount.ToString() + " verts";
        }
    }

    [TC(typeof(EXP))] public class Rsc5XenonD3DResource : Rsc5BlockBase //rage::grcXenonD3DResource (XeDK headers)
    {
        public override ulong BlockLength => 32;
        public Rsc5IndexBufferD3DFlags Common { get; set; } //Flags common to all resources
        public uint ReferenceCount { get; set; } = 1; //External reference count
        public uint Fence { get; set; } //This is the fence number of the last ring buffer reference to this resource
        public uint ReadFence { get; set; } //This is used to determine when it's safe for the CPU to read a resource that was written to by the GPU
        public uint Identifier { get; set; } //Game-supplied data that identifies the resource
        public uint BaseFlush { get; set; } = 0xFFFF0000; //Encodes the memory range to be flushed by D3D via 'dcbf' at 'Unlock' time
        public uint DWORD0 { get; set; } //GPU Address for index buffers - vertex buffers are GPU constant type: [0..1] and BaseAddress: [2..31]
        public uint DWORD1 { get; set; } //Buffer size for index buffers - vertex buffers are Endian: [0..1], Size: (in DWORDS) [2..25], AddressClamp: [26], RequestSize: [28..29] and ClampDisable: [30..31]

        public override void Read(Rsc5DataReader reader)
        {
            Common = (Rsc5IndexBufferD3DFlags)reader.ReadUInt32();
            ReferenceCount = reader.ReadUInt32();
            Fence = reader.ReadUInt32();
            ReadFence = reader.ReadUInt32();
            Identifier = reader.ReadUInt32();
            BaseFlush = reader.ReadUInt32();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            writer.WriteUInt32((uint)Common);
            writer.WriteUInt32(ReferenceCount);
            writer.WriteUInt32(Fence);
            writer.WriteUInt32(ReadFence);
            writer.WriteUInt32(Identifier);
            writer.WriteUInt32(BaseFlush);
        }
    }

    [TC(typeof(EXP))] public class Rsc5IndexBuffer : Rsc5FileBase //grcIndexBufferD3D
    {
        public override ulong BlockLength => 32;
        public override uint VFT { get; set; } = 0x00566198;
        public uint IndicesCount { get; set; }  //m_IndexCount
        public Rsc5RawArr<ushort> Indices { get; set; } //m_IndexData
        public Rsc5Ptr<Rsc5XenonD3DResource> D3DIndexBuffer { get; set; } //m_D3DBuffer

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            IndicesCount = reader.ReadUInt32();
            Indices = reader.ReadRawArrPtr<ushort>();
            D3DIndexBuffer = reader.ReadPtr<Rsc5XenonD3DResource>();
            Indices = reader.ReadRawArrItems(Indices, IndicesCount);
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [TC(typeof(EXP))] public class Rsc5VertexBuffer : Rsc5FileBase //grcVertexBufferD3D
    {
        public override ulong BlockLength => 32;
        public override uint VFT { get; set; } = 0x005665A0;
        public ushort VertexCount { get; set; } //m_VertCount
        public byte Locked { get; set; } //m_Locked
        public byte Flags { get; set; } //m_Flags
        public Rsc5RawArr<byte> LockedData { get; set; } //m_pLockedData, same as m_pVertexData
        public uint VertexStride { get; set; } //m_Stride
        public Rsc5Ptr<Rsc5VertexDeclaration> Layout { get; set; } //m_Fvf
        public uint LockThreadID { get; set; } //m_dwLockThreadId
        public Rsc5RawArr<byte> VertexData { get; set; } //m_pVertexData
        public Rsc5Ptr<Rsc5XenonD3DResource> D3DVertexBuffer { get; set; } //m_D3DBuffer

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            VertexCount = reader.ReadUInt16();
            Locked = reader.ReadByte();
            Flags = reader.ReadByte();
            LockedData = reader.ReadRawArrPtr<byte>();
            VertexStride = reader.ReadUInt32();
            Layout = reader.ReadPtr<Rsc5VertexDeclaration>();
            LockThreadID = reader.ReadUInt32();
            VertexData = reader.ReadRawArrPtr<byte>();
            D3DVertexBuffer = reader.ReadPtr<Rsc5XenonD3DResource>();

            LockedData = reader.ReadRawArrItems(LockedData, (uint)(VertexCount * Layout.Item.FVFSize));
            VertexData = reader.ReadRawArrItems(VertexData, (uint)(VertexCount * Layout.Item.FVFSize));
        }

        public override void Write(Rsc5DataWriter writer)
        {
            base.Write(writer);
            writer.WriteUInt16(VertexCount);
            writer.WriteByte(Locked);
            writer.WriteByte(Flags);
            writer.WriteRawArr(LockedData); //Should be NULL
            writer.WriteUInt32(VertexStride);
            writer.WritePtr(Layout);
            writer.WriteUInt32(LockThreadID);
            writer.WriteRawArr(VertexData);
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
            return t switch
            {
                Rsc5VertexComponentType.Half2 => VertexElementFormat.Half2,
                Rsc5VertexComponentType.Float => VertexElementFormat.Float,
                Rsc5VertexComponentType.Half4 => VertexElementFormat.Half4,
                Rsc5VertexComponentType.FloatUnk => VertexElementFormat.Float,
                Rsc5VertexComponentType.Float2 => VertexElementFormat.Float2,
                Rsc5VertexComponentType.Float3 => VertexElementFormat.Float3,
                Rsc5VertexComponentType.Float4 => VertexElementFormat.Float4,
                Rsc5VertexComponentType.UByte4 => VertexElementFormat.UByte4,
                Rsc5VertexComponentType.Colour => VertexElementFormat.Colour,
                Rsc5VertexComponentType.Dec3N => VertexElementFormat.Dec3N,
                Rsc5VertexComponentType.UShort2N => VertexElementFormat.Short2N,
                _ => VertexElementFormat.None,
            };
        }

        public byte GetEngineSemanticIndex(Rsc5VertexElementSemantic s)
        {
            return s switch
            {
                Rsc5VertexElementSemantic.BlendWeights => 1,
                Rsc5VertexElementSemantic.BlendIndices => 2,
                Rsc5VertexElementSemantic.Normal => 3,
                Rsc5VertexElementSemantic.Colour0 => 4,
                Rsc5VertexElementSemantic.Colour1 => 4,
                Rsc5VertexElementSemantic.TexCoord0 => 5,
                Rsc5VertexElementSemantic.TexCoord1 => 5,
                Rsc5VertexElementSemantic.TexCoord2 => 5,
                Rsc5VertexElementSemantic.TexCoord3 => 5,
                Rsc5VertexElementSemantic.TexCoord4 => 5,
                Rsc5VertexElementSemantic.TexCoord5 => 5,
                Rsc5VertexElementSemantic.TexCoord6 => 5,
                Rsc5VertexElementSemantic.TexCoord7 => 5,
                Rsc5VertexElementSemantic.Tangent0 => 6,
                Rsc5VertexElementSemantic.Tangent1 => 6,
                Rsc5VertexElementSemantic.Binormal0 => 7,
                Rsc5VertexElementSemantic.Binormal1 => 7,
                _ => 0,
            };
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
            return type switch
            {
                Rsc5VertexComponentType.Nothing => 2, //Half
                Rsc5VertexComponentType.Half2 => 4, //Half2
                Rsc5VertexComponentType.Float => 6, //Half3
                Rsc5VertexComponentType.Half4 => 8, //Half4
                Rsc5VertexComponentType.FloatUnk => 4, //Float
                Rsc5VertexComponentType.Float2 => 8, //Float2
                Rsc5VertexComponentType.Float3 => 12, //Float3
                Rsc5VertexComponentType.Float4 => 16, //Float4
                Rsc5VertexComponentType.UByte4 => 4, //UByte4
                Rsc5VertexComponentType.Colour => 4, //Color
                Rsc5VertexComponentType.Dec3N => 4, //PackedNormal
                Rsc5VertexComponentType.Unk1 => 2, //Short_UNorm
                Rsc5VertexComponentType.Unk2 => 4, //Short2_Unorm
                Rsc5VertexComponentType.Unk3 => 2, //Byte2_UNorm
                Rsc5VertexComponentType.UShort2N => 4, //Short2
                Rsc5VertexComponentType.Unk5 => 8, //Short4
                _ => 0,
            };
        }

        public static int GetComponentCount(Rsc5VertexComponentType type)
        {
            return type switch
            {
                Rsc5VertexComponentType.Nothing => 0,
                Rsc5VertexComponentType.Float => 1,
                Rsc5VertexComponentType.Float2 => 2,
                Rsc5VertexComponentType.Float3 => 3,
                Rsc5VertexComponentType.Float4 => 4,
                Rsc5VertexComponentType.Colour => 4,
                Rsc5VertexComponentType.UByte4 => 4,
                Rsc5VertexComponentType.Half2 => 2,
                Rsc5VertexComponentType.Half4 => 4,
                Rsc5VertexComponentType.Dec3N => 3,
                Rsc5VertexComponentType.UShort2N => 2,
                _ => 0,
            };
        }
    }

    [TC(typeof(EXP))] public class Rsc5SkeletonData : Skeleton, IRsc5Block //rage::crSkeletonData
    {
        public ulong BlockLength => 64;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;

        public Rsc5RawLst<Rsc5BoneData> BoneData { get; set; }
        public Rsc5RawArr<int> ParentIndices { get; set; } //m_ParentIndices, pointer to parent indices table, NULL if none calculated
        public Rsc5RawArr<Matrix4x4> JointScaleOrients { get; set; } //m_CumulativeJointScaleOrients, mostly NULL
        public Rsc5RawArr<Matrix4x4> InverseJointScaleOrients { get; set; } //m_CumulativeInverseJointScaleOrients, inverse cumulative joint scale orient matrices
        public Rsc5RawArr<Matrix4x4> DefaultTransforms { get; set; } //m_DefaultTransforms, default bone transform
        public ushort BoneCount { get; set; } // m_NumBones, number of bones in skeleton
        public ushort NumTranslationDofs { get; set; } //m_NumTranslationDofs
        public ushort NumRotationDofs { get; set; } //m_NumRotationDofs
        public ushort NumScaleDofs { get; set; } //m_NumScaleDofs
        public uint Flags { get; set; } //m_Flags
        public Rsc5ManagedArr<Rsc5SkeletonBoneTag> BoneIDs { get; set; } //m_BoneIdTable, rage::crSkeletonData
        public uint RefCount { get; set; } = 1; //m_RefCount
        public uint Signature { get; set; } //m_Signature, skeleton signature (a hash value that identifies the skeleton's structure, the order of the branches of child bones matter)
        public Rsc5Str JointDataFileName { get; set; } //m_JointDataFileName, always NULL?
        public uint JointData { get; set; } //m_JointData
        public uint Unknown6 { get; set; } //Padding
        public uint Unknown7 { get; set; } //Padding

        public Dictionary<Rsc5BoneIdEnum, Rsc5BoneData> BonesMap { get; set; } //For convienience finding bones by tag

        public void Read(Rsc5DataReader reader)
        {
            BoneData = reader.ReadRawLstPtr<Rsc5BoneData>();
            ParentIndices = reader.ReadRawArrPtr<int>();
            JointScaleOrients = reader.ReadRawArrPtr<Matrix4x4>();
            InverseJointScaleOrients = reader.ReadRawArrPtr<Matrix4x4>();
            DefaultTransforms = reader.ReadRawArrPtr<Matrix4x4>();
            BoneCount = reader.ReadUInt16();
            NumTranslationDofs = reader.ReadUInt16();
            NumRotationDofs = reader.ReadUInt16();
            NumScaleDofs = reader.ReadUInt16();
            Flags = reader.ReadUInt32();
            BoneIDs = reader.ReadArr<Rsc5SkeletonBoneTag>();
            RefCount = reader.ReadUInt32();
            Signature = reader.ReadUInt32();
            JointDataFileName = reader.ReadStr();
            JointData = reader.ReadUInt32();
            Unknown6 = reader.ReadUInt32();
            Unknown7 = reader.ReadUInt32();

            BoneData = reader.ReadRawLstItems(BoneData, BoneCount);
            ParentIndices = reader.ReadRawArrItems(ParentIndices, BoneCount);
            JointScaleOrients = reader.ReadRawArrItems(JointScaleOrients, BoneCount);
            InverseJointScaleOrients = reader.ReadRawArrItems(InverseJointScaleOrients, BoneCount);
            DefaultTransforms = reader.ReadRawArrItems(DefaultTransforms, BoneCount);
            Bones = BoneData.Items;

            for (uint i = 0; i < BoneCount; i++)
            {
                var b = (Rsc5BoneData)Bones[i];
                b.ParentIndex = (ParentIndices.Items != null) ? ParentIndices.Items[i] : 0;
                b.JointScaleOrients = (JointScaleOrients.Items != null) ? JointScaleOrients.Items[i] : Matrix4x4.Identity;
                b.InverseJointScaleOrients = (InverseJointScaleOrients.Items != null) ? InverseJointScaleOrients.Items[i] : Matrix4x4.Identity;
                b.DefaultTransforms = (DefaultTransforms.Items != null) ? DefaultTransforms.Items[i] : Matrix4x4.Identity;
                Bones[i] = b;
            }

            for (int i = 0; i < Bones.Length; i++)
            {
                var bone = (Rsc5BoneData)Bones[i];
                var ns = bone.NextSibling;
                var fc = bone.FirstChild;
                var pr = bone.ParentRef;

                if (reader.BlockPool.TryGetValue(ns.Position, out var nsi)) ns.Item = nsi as Rsc5BoneData;
                if (reader.BlockPool.TryGetValue(fc.Position, out var fci)) fc.Item = fci as Rsc5BoneData;
                if (reader.BlockPool.TryGetValue(pr.Position, out var pri)) pr.Item = pri as Rsc5BoneData;

                bone.NextSibling = ns;
                bone.FirstChild = fc;
                bone.ParentRef = pr;
                bone.Parent = pr.Item;
            }

            var bonesSorted = Bones.ToList();
            bonesSorted.Sort((a, b) => a.Index.CompareTo(b.Index));

            for (int i = 0; i < bonesSorted.Count; i++)
            {
                var bone = bonesSorted[i];
                bone.UpdateAnimTransform();
                bone.AbsTransform = bone.AnimTransform;
                bone.BindTransformInv = Matrix4x4Ext.Invert(bone.AnimTransform);
                bone.BindTransformInv.M44 = 1.0f;
                bone.UpdateSkinTransform();
            }

            BuildBoneTags(false);
            UpdateBoneTransforms();
            BuildBonesDictionary();
        }

        public void Write(Rsc5DataWriter writer)
        {
            
        }

        public void BuildBoneTags(bool fromXml)
        {
            BonesMap = [];
            var tags = new List<Rsc5SkeletonBoneTag>();
            var bones = BoneData.Items;

            if (bones != null)
            {
                for (int i = 0; i < bones.Length; i++)
                {
                    var bone = bones[i];
                    var tag = new Rsc5SkeletonBoneTag
                    {
                        BoneTag = bone.ID,
                        BoneIndex = (ushort)i
                    };
                    BonesMap[bone.ID] = bone;
                    tags.Add(tag);
                }
            }

            if (!fromXml) return;
            var skip = tags.Count < 1;
            if (tags.Count == 1)
            {
                var t0 = tags[0];
                skip = t0.BoneTag == 0;
            }

            if (skip)
            {
                BoneIDs = new();
                return;
            }

            if (BoneIDs.Items == null)
            {
                tags = tags.OrderBy(tag => tag.BoneIndex).ToList();
                BoneIDs = new([.. tags]);
            }
        }
    }

    [TC(typeof(EXP))] public class Rsc5BoneData : Bone, IRsc5Block //rage::crBoneData
    {
        public ulong BlockLength => 224;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;

        public Rsc5Str NameStr { get; set; }  //m_Name, bone name
        public uint Dofs { get; set; } //m_Dofs, bone data degree-of-freedom flags
        public Rsc5Ptr<Rsc5BoneData> NextSibling { get; set; } //m_Next, pointer to the bone's next sibling, or NULL if no more siblings
        public Rsc5Ptr<Rsc5BoneData> FirstChild { get; set; } //m_Child, pointer to the bone's first child, or NULL if no children
        public Rsc5Ptr<Rsc5BoneData> ParentRef { get; set; } //m_Parent, pointer to the bone's parent, or NULL if no parent
        public Rsc5BoneIdEnum ID { get; set; } //m_BoneId, the bone id of this bone (or if bone ids not used, returns the bone's index)
        public ushort Mirror { get; set; } //m_MirrorIndex, index of the bone that is a mirror to this bone (used to mirror animations/frames)
        public byte NumTransChannels { get; set; } //m_NumTransChannels, related to TranslationMin and TranslationMax
        public byte NumRotChannels { get; set; } //m_NumRotChannels, related to RotationMin and RotationMax
        public byte NumScaleChannels { get; set; } //m_NumScaleChannels, related to OrigScale
        public ushort Unknown_1Dh { get; set; } //Pad
        public byte Unknown_1Fh { get; set; } //Pad
        public Vector4 OrigPosition { get; set; } //m_DefaultTranslation, default translation vector of bone, this is the offset between this bone and it's parent
        public Vector4 OrigRotationEuler { get; set; } //m_DefaultRotation, default rotation on the bone
        public Quaternion OrigRotation { get; set; } //m_DefaultRotationQuat, default rotation on the bone as a quaternion
        public Vector4 OrigScale { get; set; } //m_DefaultScale, default scale vector (not properly implemented in RAGE)
        public Vector4 AbsolutePosition { get; set; } //m_GlobalOffset, depending on Dofs, Parent->DefaultTranslation or DefaultTranslation transformed to the model space
        public Vector4 AbsoluteRotationEuler { get; set; } //m_JointOrient
        public Vector4 Sorient { get; set; } //m_ScaleOrient
        public Vector4 TranslationMin { get; set; } //m_TransMin
        public Vector4 TranslationMax { get; set; } //m_TransMax
        public Vector4 RotationMin { get; set; } //m_RotMin
        public Vector4 RotationMax { get; set; } //m_RotMax
        public uint JointData { get; set; } //m_JointData, always 0
        public JenkHash NameHash { get; set; } //m_NameHash, bone name hashed
        public uint Unknown_D8h { get; set; } //Always 0
        public uint Unknown_DCh { get; set; } //Always 0

        public int SiblingIndex { get; set; }
        public int ChildIndex { get; set; }
        public int ParentIndex { get; set; }
        public Matrix4x4 JointScaleOrients { get; set; }
        public Matrix4x4 InverseJointScaleOrients { get; set; }
        public Matrix4x4 DefaultTransforms { get; set; }

        public void Read(Rsc5DataReader reader)
        {
            NameStr = reader.ReadStr();
            Dofs = reader.ReadUInt32();
            NextSibling = new Rsc5Ptr<Rsc5BoneData>() { Position = reader.ReadUInt32() };
            FirstChild = new Rsc5Ptr<Rsc5BoneData>() { Position = reader.ReadUInt32() };
            ParentRef = new Rsc5Ptr<Rsc5BoneData>() { Position = reader.ReadUInt32() };
            Index = reader.ReadUInt16();
            ID = (Rsc5BoneIdEnum)reader.ReadUInt16();
            Mirror = reader.ReadUInt16();
            NumTransChannels = reader.ReadByte();
            NumRotChannels = reader.ReadByte();
            NumScaleChannels = reader.ReadByte();
            Unknown_1Dh = reader.ReadUInt16();
            Unknown_1Fh = reader.ReadByte();
            OrigPosition = reader.ReadVector4();
            OrigRotationEuler = reader.ReadVector4();
            OrigRotation = reader.ReadVector4().ToQuaternion();
            OrigScale = reader.ReadVector4();
            AbsolutePosition = reader.ReadVector4();
            AbsoluteRotationEuler = reader.ReadVector4();
            Sorient = reader.ReadVector4();
            TranslationMin = reader.ReadVector4();
            TranslationMax = reader.ReadVector4();
            RotationMin = reader.ReadVector4();
            RotationMax = reader.ReadVector4();
            JointData = reader.ReadUInt32();
            NameHash = reader.ReadUInt32();
            Unknown_D8h = reader.ReadUInt32();
            Unknown_DCh = reader.ReadUInt32();

            Name = NameStr.Value;
            Position = OrigPosition.XYZ();
            Rotation = OrigRotation;
            Scale = Vector3.One;

            AnimRotation = Rotation;
            AnimTranslation = Position;
            AnimScale = Scale;
        }

        public void Write(Rsc5DataWriter writer)
        {
            Rsc5BoneData parent = null, child = null, sibling = null;
            var bdata = writer.BlockList.OfType<Rsc5SkeletonBoneData>().FirstOrDefault();

            if (bdata != null)
            {
                if (NextSibling.Item != null)
                    sibling = bdata.Bones.FirstOrDefault(b => string.Equals(b.Name, NextSibling.Item.Name));
                if (FirstChild.Item != null)
                    child = bdata.Bones.FirstOrDefault(b => string.Equals(b.Name, FirstChild.Item.Name));
                if (ParentRef.Item != null)
                    parent = bdata.Bones.FirstOrDefault(b => string.Equals(b.Name, ParentRef.Item.Name));
            }

            writer.WriteStr(NameStr);
            writer.WriteUInt32(Dofs);
            writer.WritePtrEmbed(sibling, sibling, (ulong)(224 * sibling?.Index ?? 0));
            writer.WritePtrEmbed(child, child, (ulong)(224 * child?.Index ?? 0));
            writer.WritePtrEmbed(parent, parent, (ulong)(224 * parent?.Index ?? 0));
            writer.WriteUInt16((ushort)Index);
            writer.WriteUInt16((ushort)ID);
            writer.WriteUInt16(Mirror);
            writer.WriteByte(NumTransChannels);
            writer.WriteByte(NumRotChannels);
            writer.WriteByte(NumScaleChannels);
            writer.WriteUInt16(Unknown_1Dh);
            writer.WriteByte(Unknown_1Fh);
            writer.WriteVector4(OrigPosition);
            writer.WriteVector4(OrigRotationEuler);
            writer.WriteVector4(OrigRotation.ToVector4());
            writer.WriteVector4(OrigScale);
            writer.WriteVector4(AbsolutePosition);
            writer.WriteVector4(AbsoluteRotationEuler);
            writer.WriteVector4(Sorient);
            writer.WriteVector4(TranslationMin);
            writer.WriteVector4(TranslationMax);
            writer.WriteVector4(RotationMin);
            writer.WriteVector4(RotationMax);
            writer.WriteUInt32(JointData);
            writer.WriteUInt32(NameHash);
            writer.WriteUInt32(Unknown_D8h);
            writer.WriteUInt32(Unknown_DCh);
        }
    }

    [TC(typeof(EXP))] public class Rsc5SkeletonBoneData : Rsc5BlockBase
    {
        public override ulong BlockLength => BonesCount * 224;
        public uint BonesCount { get; set; }
        public Rsc5BoneData[] Bones { get; set; }

        public Rsc5SkeletonBoneData()
        {
        }

        public Rsc5SkeletonBoneData(Rsc5BoneData[] bones)
        {
            Bones = bones;
            BonesCount = (uint)(bones?.Length ?? 0);
        }

        public override void Read(Rsc5DataReader reader)
        {
            //Only use this for writing BoneData
        }

        public override void Write(Rsc5DataWriter writer)
        {
            if (Bones != null)
            {
                foreach (var bone in Bones)
                {
                    bone.Write(writer);
                }
            }
        }
    }

    [TC(typeof(EXP))] public class Rsc5SkeletonBoneTag : Rsc5BlockBase, MetaNode //rage::crSkeletonData::BoneIdData
    {
        public override ulong BlockLength => 4;
        public Rsc5BoneIdEnum BoneTag { get; set; } //m_Id
        public ushort BoneIndex { get; set; } //m_Index

        public override void Read(Rsc5DataReader reader)
        {
            BoneTag = (Rsc5BoneIdEnum)reader.ReadUInt16();
            BoneIndex = reader.ReadUInt16();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            writer.WriteUInt16((ushort)BoneTag);
            writer.WriteUInt16(BoneIndex);
        }

        public override string ToString()
        {
            return $"{BoneTag} : {BoneIndex}";
        }

        public void Read(MetaNodeReader reader)
        {
            BoneTag = reader.ReadEnum<Rsc5BoneIdEnum>("BoneTag");
            BoneIndex = reader.ReadUInt16("BoneIndex");
        }

        public void Write(MetaNodeWriter writer)
        {
            writer.WriteEnum("BoneTag", BoneTag);
            writer.WriteUInt16("BoneIndex", BoneIndex);
        }
    }

    [TC(typeof(EXP))] public class Rsc5ShaderGroup : Rsc5FileBase
    {
        public override ulong BlockLength => 16;
        public override uint VFT { get; set; } = 0x005AA6FC;
        public Rsc5Ptr<Rsc5TextureDictionary> TextureDictionary { get; set; } //Sits where pgBase keeps its block map; only packages that embed their own textures fill it in
        public Rsc5PtrArr<Rsc5Shader> Shaders { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);

            var position = reader.Position;
            var pointer = reader.ReadUInt32();
            if (IsTextureDictionary(reader, pointer))
            {
                reader.Position = position;
                TextureDictionary = reader.ReadPtr<Rsc5TextureDictionary>();
                reader.Position = position + 4;
            }

            Shaders = reader.ReadPtrArr<Rsc5Shader>();

            if (Shaders.Items != null)
            {
                XapbFile.Textures ??= [];
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

        //The slot holds a plain block map in most files, so it's only followed when the target
        //really looks like a grcTextureDictionary: null block map, and matching hash and texture
        //arrays with the same non-zero count.
        private static bool IsTextureDictionary(Rsc5DataReader reader, uint pointer)
        {
            if ((pointer & 0xF0000000) != Rpf3Crypto.VIRTUAL_BASE) return false;

            var data = reader.Data;
            var offset = (int)(pointer & 0x0FFFFFFF);
            if (offset < 0 || offset + 32 > Math.Min(reader.VirtualSize, data.Length)) return false;

            static uint U32(byte[] d, int o) => (uint)((d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3]);
            static ushort U16(byte[] d, int o) => (ushort)((d[o] << 8) | d[o + 1]);

            if (U32(data, offset + 4) != 0) return false; //block map
            var hashes = U32(data, offset + 0x10);
            var textures = U32(data, offset + 0x18);
            if ((hashes & 0xF0000000) != Rpf3Crypto.VIRTUAL_BASE) return false;
            if ((textures & 0xF0000000) != Rpf3Crypto.VIRTUAL_BASE) return false;

            var count = U16(data, offset + 0x14);
            return count != 0 && count == U16(data, offset + 0x1C);
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [TC(typeof(EXP))] public class Rsc5Shader : Rsc5FileBase
    {
        public override ulong BlockLength => 96; //Likely wrong
        public override uint VFT { get; set; }
        public uint BlockMapAdress { get; set; }
        public byte Version { get; set; } //2
        public byte DrawBucket { get; set; }
        public byte UsageCount { get; set; }
        public byte Unknown1 { get; set; }
        public ushort Unknown2 { get; set; }
        public ushort ShaderIndex { get; set; }
        public Rsc5RawArr<uint> ParamsData { get; set; }
        public uint Unknown3 { get; set; }
        public ushort ParamsCount { get; set; }
        public ushort EffectSize { get; set; }
        public Rsc5RawArr<byte> ParamsTypes { get; set; }
        public uint Hash { get; private set; }
        public Rsc5RawArr<uint> ParamsNames { get; set; }
        public uint Unknown4 { get; set; }
        public uint Unknown5 { get; set; }
        public Rsc5Str ShaderName { get; set; }
        public uint Unknown6 { get; set; }
        public Rsc5Str ShaderFileName { get; set; }
        public uint Unknown7 { get; set; }
        public Rsc5ShaderParameter[] Params { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            BlockMapAdress = reader.ReadUInt32();
            Version = reader.ReadByte();
            DrawBucket = reader.ReadByte();
            UsageCount = reader.ReadByte();
            Unknown1 = reader.ReadByte();
            Unknown2 = reader.ReadUInt16();
            ShaderIndex = reader.ReadUInt16();
            ParamsData = reader.ReadRawArrPtr<uint>();
            Unknown3 = reader.ReadUInt32();
            ParamsCount = reader.ReadUInt16();
            EffectSize = reader.ReadUInt16();
            ParamsTypes = reader.ReadRawArrPtr<byte>();
            Hash = reader.ReadUInt32();
            ParamsNames = reader.ReadRawArrPtr<uint>();
            Unknown4 = reader.ReadUInt32();
            Unknown5 = reader.ReadUInt32();
            ShaderName = reader.ReadStr();
            Unknown6 = reader.ReadUInt32();
            ShaderFileName = reader.ReadStr();
            Unknown7 = reader.ReadUInt32();

            var pc = ParamsCount;
            ParamsData = reader.ReadRawArrItems(ParamsData, pc);
            ParamsTypes = reader.ReadRawArrItems(ParamsTypes, pc);
            ParamsNames = reader.ReadRawArrItems(ParamsNames, pc);
            Params = new Rsc5ShaderParameter[pc];

            for (uint i = 0; i < pc; i++)
            {
                var ptr = ParamsData.Items[i];
                var hash = ParamsNames.Items[i];
                var type = ParamsTypes.Items[i];

                var p = new Rsc5ShaderParameter
                {
                    Hash = hash,
                    Type = type
                };

                switch (p.Type)
                {
                    case 0: //texture
                        p.Texture = reader.ReadBlock<Rsc5Texture>(ptr);
                        break;
                    case 1: //vector4
                        p.Vector = Rpf3Crypto.Swap(reader.ReadVector4(ptr));
                        break;
                    default: //array
                        p.Array = Rpf3Crypto.Swap(reader.ReadArray<Vector4>(p.Type, ptr));
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
            return $"{ShaderName}";
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
    }

    [Flags] public enum Rsc5IndexBufferD3DFlags : uint
    {
        D3DCOMMON_TYPE_VERTEXBUFFER = 0x1,
        D3DCOMMON_TYPE_INDEXBUFFER = 0x2,
        D3DCOMMON_TYPE_TEXTURE = 0x3,
        D3DCOMMON_TYPE_SURFACE = 0x4,
        D3DCOMMON_TYPE_VERTEXDECLARATION = 0x5,
        D3DCOMMON_TYPE_VERTEXSHADER = 0x6,
        D3DCOMMON_TYPE_PIXELSHADER = 0x7,
        D3DCOMMON_TYPE_CONSTANTBUFFER = 0x8,
        D3DCOMMON_TYPE_COMMANDBUFFER = 0x9,
        D3DCOMMON_CPU_CACHED_MEMORY = 0x200000,
        D3DINDEXBUFFER_INDEX32 = 0x80000000, //Indices are 32-bit instead of 16-bit
        D3DINDEXBUFFER_ENDIAN_8IN16 = 0x20000000,
        D3DINDEXBUFFER_ENDIAN_8IN32 = 0x40000000,
    }

    public enum Rsc5BoneIdEnum : ushort //TODO: Add remaining bone IDs
    {
        ROOT = 0,
        BOX_OCCLUDER = 59432
    }

    public interface IRsc5DrawableRoot
    {
        Rsc5ShaderGroup ShaderGroup { get; }
        Rsc5DrawableLod Lod { get; }
        void UpdateAllModels();
    }
}
