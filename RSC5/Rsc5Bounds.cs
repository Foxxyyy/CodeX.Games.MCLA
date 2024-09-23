using CodeX.Core.Engine;
using CodeX.Core.Numerics;
using CodeX.Core.Physics;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using System.Numerics;
using TC = System.ComponentModel.TypeConverterAttribute;
using EXP = System.ComponentModel.ExpandableObjectConverter;

namespace CodeX.Games.MCLA.RSC5
{
    public class Rsc5BoundsFile : Rsc5BlockBase
    {
        public override ulong BlockLength => 12;
        public uint VFT { get; set; }
        public Rsc5Ptr<Rsc5BlockMap> BlockMap { get; set; }
        public Rsc5Ptr<Rsc5Bounds> Bounds { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            BlockMap = reader.ReadPtr<Rsc5BlockMap>();
            Bounds = reader.ReadPtr<Rsc5Bounds>(Rsc5Bounds.Create);
        }
        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public class Rsc5BoundsDictionary : Rsc5BlockBase
    {
        public override ulong BlockLength => 24;
        public uint VFT { get; set; }
        public Rsc5Ptr<Rsc5BlockMap> BlockMap { get; set; }
        public JenkHash ParentDictionary { get; set; }
        public uint UsageCount { get; set; }
        public Rsc5Arr<JenkHash> Hashes { get; set; }
        public Rsc5PtrArr<Rsc5Bounds> Bounds { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            BlockMap = reader.ReadPtr<Rsc5BlockMap>();
            ParentDictionary = reader.ReadUInt32();
            UsageCount = reader.ReadUInt32();
            Hashes = reader.ReadArr<JenkHash>();
            Bounds = reader.ReadPtrArr<Rsc5Bounds>(Rsc5Bounds.Create);
        }
        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public enum Rsc5BoundsType : byte
    {
        Sphere = 0,
        Capsule = 1,
        Box = 3,
        Geometry = 4,
        GeometryBVH = 10,
        Composite = 12
    }

    [TC(typeof(EXP))] public class Rsc5Bounds : Collider, Rsc5Block
    {
        public virtual ulong BlockLength => 128;
        public ulong FilePosition { get; set; }
        public bool IsPhysical => false;
        public uint VFT { get; set; }
        public Rsc5BoundsType Type { get; set; }
        public byte Unknown1 { get; set; }
        public ushort Unknown2 { get; set; }
        public float SphereRadius { get; set; }
        public float WorldRadius { get; set; }
        public Vector4 BoxMax { get; set; }
        public Vector4 BoxMin { get; set; }
        public Vector4 BoxCenter { get; set; }
        public Vector4 Unknown3 { get; set; }
        public Vector4 SphereCenter { get; set; }
        public Vector4 Unknown4 { get; set; }
        public Vector3 Margin { get; set; }
        public uint RefCount { get; set; }

        public Rsc5Bounds() { }
        public Rsc5Bounds(Rsc5BoundsType type)
        {
            Type = type;
            InitCollider(GetEngineType(type));
        }
        
        public virtual void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            Type = (Rsc5BoundsType)reader.ReadByte();
            Unknown1 = reader.ReadByte();
            Unknown2 = reader.ReadUInt16();
            SphereRadius = reader.ReadSingle();
            WorldRadius = reader.ReadSingle();
            BoxMax = reader.ReadVector4();
            BoxMin = reader.ReadVector4();
            BoxCenter = reader.ReadVector4();
            Unknown3 = reader.ReadVector4();
            SphereCenter = reader.ReadVector4();
            Unknown4 = reader.ReadVector4();
            Margin = reader.ReadVector3();
            RefCount = reader.ReadUInt32();
        }
        public virtual void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }

        public static Rsc5Bounds Create(Rsc5DataReader r)
        {
            r.Position += 4;
            var type = (Rsc5BoundsType)r.ReadByte();
            r.Position -= 5;
            switch (type)
            {
                case Rsc5BoundsType.Sphere: return new Rsc5BoundSphere();
                case Rsc5BoundsType.Capsule: return new Rsc5BoundCapsule();
                case Rsc5BoundsType.Box: return new Rsc5BoundBox();
                case Rsc5BoundsType.Geometry: return new Rsc5BoundGeometry();
                case Rsc5BoundsType.GeometryBVH: return new Rsc5BoundGeometryBVH();
                case Rsc5BoundsType.Composite: return new Rsc5BoundComposite();
                case (Rsc5BoundsType)5: break; //in fragments
                default: break;
            }
            return new Rsc5Bounds();
        }

        public static ColliderType GetEngineType(Rsc5BoundsType t)
        {
            switch (t)
            {
                case Rsc5BoundsType.Sphere: return ColliderType.Sphere;
                case Rsc5BoundsType.Capsule: return ColliderType.Capsule;
                case Rsc5BoundsType.Box: return ColliderType.Box;
                case Rsc5BoundsType.Geometry: return ColliderType.Mesh;
                case Rsc5BoundsType.GeometryBVH: return ColliderType.Mesh;
                case Rsc5BoundsType.Composite: return ColliderType.None;
                default: return ColliderType.None;
            }
        }

        public override string ToString()
        {
            return $"{Type} : {BoxMin} : {BoxMax}";
        }
    }

    public class Rsc5BoundSphere : Rsc5Bounds
    {
        public override ulong BlockLength => base.BlockLength + 24;
        public Vector4 Radius { get; set; }
        public Rsc5BoundMaterial Material { get; set; }
        public uint Padding1 { get; set; }

        public Rsc5BoundSphere() : base(Rsc5BoundsType.Sphere) { }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            Radius = reader.ReadVector4();
            Material = reader.ReadStruct<Rsc5BoundMaterial>();
            Padding1 = reader.ReadUInt32();
            if (Padding1 != 0)
            { }
            if (Material.Ref.MaterialData == null)
            { }
            PartColour = Material.Ref.Colour;
            PartSize = new Vector3(Radius.X, 0.0f, 0.0f);
            ComputeMass(ColliderType.Sphere, PartSize, 1.0f);
            ComputeBodyInertia();
        }
    }
    public class Rsc5BoundCapsule : Rsc5Bounds
    {
        public override ulong BlockLength => base.BlockLength + 96;//check this!
        public Vector4 Radius { get; set; }
        public Vector4 Height { get; set; }
        public Vector4 Unknown5 { get; set; }
        public Vector4 Unknown6 { get; set; }
        public Vector4 Unknown7 { get; set; }
        public Rsc5BoundMaterial Material { get; set; }
        public Vector3 Unknown8 { get; set; }

        public Rsc5BoundCapsule() : base(Rsc5BoundsType.Capsule) { }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            Radius = reader.ReadVector4();
            Height = reader.ReadVector4();
            Unknown5 = reader.ReadVector4();
            Unknown6 = reader.ReadVector4();
            Unknown7 = reader.ReadVector4();
            Material = reader.ReadStruct<Rsc5BoundMaterial>();
            Unknown8 = reader.ReadVector3();

            if (Material.Ref.MaterialData == null)
            { }

            PartColour = Material.Ref.Colour;
            PartSize = new Vector3(Radius.X, Height.X, 0.0f);
            ComputeMass(ColliderType.Capsule, PartSize, 1.0f);
            ComputeBodyInertia();
        }
    }
    public class Rsc5BoundBox : Rsc5Bounds
    {
        public override ulong BlockLength => base.BlockLength + 32;//is this actually variable length?
        public uint Unknown5 { get; set; }
        public uint VertexColoursPtr { get; set; }//is this ever nonzero?
        public uint Unknown6 { get; set; }
        public uint PolygonsPointer { get; set; }//polys pointer (but polys are embedded in this structure!)
        public Vector4 Quantum { get; set; }
        public Vector4 CenterGeom { get; set; }
        public uint VerticesPtr { get; set; }
        public uint Unknown7 { get; set; }//0
        public uint Unknown8 { get; set; }//0xcdcdcd00
        public uint Unknown9 { get; set; }//0xffffffff
        public uint Unknown10 { get; set; }//0
        public uint Unknown11 { get; set; }//0
        public uint VerticesCount { get; set; }//8 (vertex count)
        public uint PolygonsCount { get; set; }//6 (poly count)
        public Vector4 BoxSize { get; set; }//size (max-min)
        public Vector4[] Corners { get; set; }
        public Rsc5BoundGeometryPolygon[] Polygons { get; set; }
        public Rsc5BoundMaterial Material { get; set; }
        public uint Unknown12 { get; set; }//could be materials array
        public uint Unknown13 { get; set; }
        public uint Unknown14 { get; set; }
        public uint Unknown15 { get; set; }
        public uint Unknown16 { get; set; }
        public uint Unknown17 { get; set; }
        public uint Unknown18 { get; set; }

        public Vector3S[] Vertices { get; set; }

        public Rsc5BoundBox() : base(Rsc5BoundsType.Box) { }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            Unknown5 = reader.ReadUInt32();
            VertexColoursPtr = reader.ReadUInt32();
            Unknown6 = reader.ReadUInt32();
            PolygonsPointer = reader.ReadUInt32();
            Quantum = reader.ReadVector4();
            CenterGeom = reader.ReadVector4();
            VerticesPtr = reader.ReadUInt32();
            Unknown7 = reader.ReadUInt32();
            Unknown8 = reader.ReadUInt32();
            Unknown9 = reader.ReadUInt32();
            Unknown10 = reader.ReadUInt32();
            Unknown11 = reader.ReadUInt32();
            VerticesCount = reader.ReadUInt32();
            PolygonsCount = reader.ReadUInt32();
            BoxSize = reader.ReadVector4();
            Corners = reader.ReadStructArray<Vector4>(VerticesCount);
            Polygons = reader.ReadStructArray<Rsc5BoundGeometryPolygon>(PolygonsCount);
            Material = reader.ReadStruct<Rsc5BoundMaterial>();
            Unknown12 = reader.ReadUInt32();
            Unknown13 = reader.ReadUInt32();
            Unknown14 = reader.ReadUInt32();
            Unknown15 = reader.ReadUInt32();
            Unknown16 = reader.ReadUInt32();
            Unknown17 = reader.ReadUInt32();
            Unknown18 = reader.ReadUInt32();

            Vertices = reader.ReadArray<Vector3S>(VerticesCount, VerticesPtr);


            if (Material.Ref.MaterialData == null)
            { }


            PartColour = Material.Ref.Colour;
            PartSize = (BoxMax.XYZ() - BoxMin.XYZ());// * 0.5f;
            ComputeMass(ColliderType.Box, PartSize, 1.0f);
            ComputeBodyInertia();
        }

    }
    public class Rsc5BoundGeometry : Rsc5Bounds
    {
        public override ulong BlockLength => 224;

        public uint Unknown5 { get; set; }
        public uint VertexColoursPtr { get; set; }
        public uint Unknown6 { get; set; }
        public uint PolygonsPtr { get; set; }
        public Vector4 Quantum { get; set; }
        public Vector4 CenterGeom { get; set; }
        public uint VerticesPtr { get; set; }
        public uint Unknown7 { get; set; }
        public uint Unknown8 { get; set; }
        public uint Unknown9 { get; set; }
        public uint Unknown10 { get; set; }
        public uint Unknown11 { get; set; }
        public uint VerticesCount { get; set; }
        public uint PolygonsCount { get; set; }
        public uint MaterialsPtr { get; set; }
        public uint Unknown12 { get; set; }
        public byte MaterialsCount { get; set; }
        public byte Unknown13 { get; set; }
        public ushort Unknown14 { get; set; }
        public uint Unknown15 { get; set; }

        public Colour[] VertexColours { get; set; }
        public Vector3S[] Vertices { get; set; }
        public Rsc5BoundGeometryPolygon[] Polygons { get; set; }
        public Rsc5BoundMaterial[] Materials { get; set; }


        public Rsc5BoundGeometry(Rsc5BoundsType type = Rsc5BoundsType.Geometry) : base(type) { }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            Unknown5 = reader.ReadUInt32();
            VertexColoursPtr = reader.ReadUInt32();
            Unknown6 = reader.ReadUInt32();
            PolygonsPtr = reader.ReadUInt32();
            Quantum = reader.ReadVector4();
            CenterGeom = reader.ReadVector4();
            VerticesPtr = reader.ReadUInt32();
            Unknown7 = reader.ReadUInt32();
            Unknown8 = reader.ReadUInt32();
            Unknown9 = reader.ReadUInt32();
            Unknown10 = reader.ReadUInt32();
            Unknown11 = reader.ReadUInt32();
            VerticesCount = reader.ReadUInt32();
            PolygonsCount = reader.ReadUInt32();
            MaterialsPtr = reader.ReadUInt32();
            Unknown12 = reader.ReadUInt32();
            MaterialsCount = reader.ReadByte();
            Unknown13 = reader.ReadByte();
            Unknown14 = reader.ReadUInt16();
            Unknown15 = reader.ReadUInt32();

            VertexColours = reader.ReadArray<Colour>(VerticesCount, VertexColoursPtr);
            Vertices = reader.ReadArray<Vector3S>(VerticesCount, VerticesPtr);
            Polygons = reader.ReadArray<Rsc5BoundGeometryPolygon>(PolygonsCount, PolygonsPtr);
            Materials = reader.ReadArray<Rsc5BoundMaterial>(MaterialsCount, MaterialsPtr);

            CreateMesh();


            PartSize = (BoxMax.XYZ() - BoxMin.XYZ());// * 0.5f;
            ComputeMass(ColliderType.Box, PartSize, 1.0f);//just an approximation to work with
            ComputeBasicBodyInertia(ColliderType.Box, PartSize);//just an approximation to work with
        }


        private void CreateMesh()
        {
            var verts = new List<ShapeVertex>();
            var indsl = new List<ushort>();
            var q = Quantum.XYZ();
            var t = CenterGeom.XYZ();
            bool usevertexcolours = false;

            int addVertex(short index, Vector3 norm, uint matind)
            {
                var matcol = Colour.White;
                if ((Materials != null) && (matind < Materials.Length))
                {
                    matcol = Materials[matind].Ref.Colour;
                }

                var c = verts.Count;
                verts.Add(new ShapeVertex()
                {
                    Position = new Vector4(Vertices[index].ToVector3(q) + t, 1),
                    Normal = norm,
                    Colour = (usevertexcolours && (VertexColours != null)) ? VertexColours[index] : matcol,
                    Texcoord = Vector2.Zero,
                    Tangent = Vector3.Zero
                });
                return c;
            }

            for (int i = 0; i < PolygonsCount; i++)
            {
                var p = Polygons[i];
                var quad = (p.TriIndex4 != 0);
                var matind = p.MaterialIndex;
                var i1 = (ushort)addVertex(p.TriIndex1, p.Normal, matind);
                var i2 = (ushort)addVertex(p.TriIndex2, p.Normal, matind);
                var i3 = (ushort)addVertex(p.TriIndex3, p.Normal, matind);
                indsl.Add(i1);
                indsl.Add(i2);
                indsl.Add(i3);
                if (quad)
                {
                    var i4 = (ushort)addVertex(p.TriIndex4, p.Normal, matind);
                    indsl.Add(i1);
                    indsl.Add(i3);
                    indsl.Add(i4);
                }
            }

            PartMesh = Shape.Create("BoundGeometry", verts.ToArray(), indsl.ToArray());
            UpdateBounds();
        }

    }
    public class Rsc5BoundGeometryBVH : Rsc5BoundGeometry
    {
        public override ulong BlockLength => base.BlockLength;
        public Rsc5Ptr<Rsc5BoundGeometryBVHRoot> BVH { get; set; }

        public Rsc5BoundGeometryBVH() : base(Rsc5BoundsType.GeometryBVH) { }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            BVH = reader.ReadPtr<Rsc5BoundGeometryBVHRoot>();
            var t1 = reader.ReadUInt32();
            var t2 = reader.ReadUInt32();
            var t3 = reader.ReadUInt32();

            if (t1 != 0)
            { }
            if (t2 != 0)
            { }
            if (t3 != 0)
            { }

        }
    }
    public class Rsc5BoundComposite : Rsc5Bounds
    {
        public override ulong BlockLength => 144;
        public uint ChildrenPtr { get; set; }
        public uint ChildrenTransforms1Ptr { get; set; }
        public uint ChildrenTransforms2Ptr { get; set; }
        public uint ChildrenBoundingBoxesPtr { get; set; }
        public ushort ChildrenCount1 { get; set; }
        public ushort ChildrenCount2 { get; set; }
        public uint Unknown5 { get; set; }
        public uint Unknown6 { get; set; }
        public uint Unknown7 { get; set; }

        public Rsc5Bounds[] Children { get; set; }
        public Matrix4x4[] ChildrenTransforms1 { get; set; }
        public Matrix4x4[] ChildrenTransforms2 { get; set; }
        public BoundingBox4[] ChildrenBoundingBoxes { get; set; }


        public Rsc5BoundComposite() : base(Rsc5BoundsType.Composite) { }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);

            ChildrenPtr = reader.ReadUInt32();
            ChildrenTransforms1Ptr = reader.ReadUInt32();
            ChildrenTransforms2Ptr = reader.ReadUInt32();
            ChildrenBoundingBoxesPtr = reader.ReadUInt32();
            ChildrenCount1 = reader.ReadUInt16();
            ChildrenCount2 = reader.ReadUInt16();
            Unknown5 = reader.ReadUInt32();
            Unknown6 = reader.ReadUInt32();
            Unknown7 = reader.ReadUInt32();

            var childptrs = reader.ReadArray<uint>(ChildrenCount1, ChildrenPtr);
            ChildrenTransforms1 = reader.ReadArray<Matrix4x4>(ChildrenCount1, ChildrenTransforms1Ptr);
            ChildrenTransforms2 = reader.ReadArray<Matrix4x4>(ChildrenCount1, ChildrenTransforms2Ptr);
            ChildrenBoundingBoxes = reader.ReadArray<BoundingBox4>(ChildrenCount1, ChildrenBoundingBoxesPtr);

            if (childptrs != null)
            {
                var cc = Math.Min(ChildrenCount1, childptrs.Length);
                Children = new Rsc5Bounds[cc];
                for (int i = 0; i < cc; i++)
                {
                    Children[i] = reader.ReadBlock(childptrs[i], Create);
                    if (Children[i] != null)
                    {
                        Children[i].Name = "Child" + i.ToString();
                        //Children[i].BoneIndex = i;
                        if ((ChildrenTransforms1 != null) && (i < ChildrenTransforms1.Length))
                        {
                            Children[i].PartTransform = new Matrix3x4(ChildrenTransforms1[i]);
                        }
                    }
                }
            }

            PartChildren = Children;
            UpdateBounds();
        }
    }

    public struct Rsc5BoundGeometryPolygon
    {
        public Vector3 Normal { get; set; }
        public uint MatAndTriArea { get; set; }
        public short TriIndex1 { get; set; }
        public short TriIndex2 { get; set; }
        public short TriIndex3 { get; set; }
        public short TriIndex4 { get; set; }
        public short EdgeIndex1 { get; set; }
        public short EdgeIndex2 { get; set; }
        public short EdgeIndex3 { get; set; }
        public short EdgeIndex4 { get; set; }

        public byte MaterialIndex { get { return (byte)(MatAndTriArea & 0xFF); } }
        public float TriArea { get { return BufferUtil.GetUintFloat(MatAndTriArea & 0xFFFFFF00); } }

        public override string ToString()
        {
            return Normal.ToString() + "   Material: " + MaterialIndex.ToString();
        }
    }
    public struct Rsc5BoundGeometryBVHNode
    {
        public short MinX { get; set; }
        public short MinY { get; set; }
        public short MinZ { get; set; }
        public short MaxX { get; set; }
        public short MaxY { get; set; }
        public short MaxZ { get; set; }
        public short ItemId { get; set; }
        public byte ItemCount { get; set; }
        public byte Padding1 { get; set; }//is this just ItemCount also?

        public Vector3 Min
        {
            get { return new Vector3(MinX, MinY, MinZ); }
            set { MinX = (short)value.X; MinY = (short)value.Y; MinZ = (short)value.Z; }
        }
        public Vector3 Max
        {
            get { return new Vector3(MaxX, MaxY, MaxZ); }
            set { MaxX = (short)value.X; MaxY = (short)value.Y; MaxZ = (short)value.Z; }
        }
        
        public override string ToString()
        {
            return ItemId.ToString() + ": " + ItemCount.ToString();
        }
    }
    public struct Rsc5BoundGeometryBVHTree
    {
        public short MinX { get; set; }
        public short MinY { get; set; }
        public short MinZ { get; set; }
        public short MaxX { get; set; }
        public short MaxY { get; set; }
        public short MaxZ { get; set; }
        public short NodeIndex1 { get; set; } //fivem says they are ushorts
        public short NodeIndex2 { get; set; } //fivem says they are ushorts
        
        public Vector3 Min
        {
            get { return new Vector3(MinX, MinY, MinZ); }
            set { MinX = (short)value.X; MinY = (short)value.Y; MinZ = (short)value.Z; }
        }
        public Vector3 Max
        {
            get { return new Vector3(MaxX, MaxY, MaxZ); }
            set { MaxX = (short)value.X; MaxY = (short)value.Y; MaxZ = (short)value.Z; }
        }

        public override string ToString()
        {
            return NodeIndex1.ToString() + ", " + NodeIndex2.ToString() + "  (" + (NodeIndex2 - NodeIndex1).ToString() + " nodes)";
        }
    }
    public class Rsc5BoundGeometryBVHRoot : Rsc5BlockBase
    {
        public override ulong BlockLength => 88;
        public Rsc5Arr<Rsc5BoundGeometryBVHNode> Nodes { get; set; }
        public uint Depth { get; set; } //depth of the hierarchy? but value is 0xCDCDCDCD
        public Vector4 BoundingBoxMin { get; set; }
        public Vector4 BoundingBoxMax { get; set; }
        public Vector4 BVHQuantumInverse { get; set; } // 1 / BVHQuantum
        public Vector4 BVHQuantum { get; set; }
        public Rsc5Arr<Rsc5BoundGeometryBVHTree> Trees { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            Nodes = reader.ReadArr<Rsc5BoundGeometryBVHNode>();
            Depth = reader.ReadUInt32();
            BoundingBoxMin = reader.ReadVector4();
            BoundingBoxMax = reader.ReadVector4();
            BVHQuantumInverse = reader.ReadVector4();
            BVHQuantum = reader.ReadVector4();
            Trees = reader.ReadArr<Rsc5BoundGeometryBVHTree>();
        }
        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [Flags] public enum Rsc5BoundsMaterialFlags : ushort
    {
        Unknown_0 = 0x1,
        Unknown_1 = 0x2,
        Unknown_2 = 0x4,
        Unknown_3 = 0x8,
        Unknown_4 = 0x10,
        Unknown_5 = 0x20,
        Unknown_6 = 0x40,
        Unknown_7 = 0x80,
        Unknown_8 = 0x100,
        Unknown_9 = 0x200,
        Unknown_10 = 0x400,
        Unknown_11 = 0x800,
        Unknown_12 = 0x1000,
        Unknown_13 = 0x2000,
        Unknown_14 = 0x4000,
        Unknown_15 = 0x8000,
    }
    public struct Rsc5BoundMaterial
    {
        public Rsc5BoundsMaterialRef Ref { get; set; }
        public byte Unknown { get; set; }
        public Rsc5BoundsMaterialFlags Flags { get; set; }

        public override string ToString()
        {
            return Ref.ToString() + " : " + Unknown.ToString() + ": " + Flags.ToString();
        }
    }
    [TC(typeof(EXP))] public struct Rsc5BoundsMaterialRef
    {
        public byte Index { get; set; }

        public Rsc5BoundsMaterialData MaterialData
        {
            get
            {
                return Rsc5BoundsMaterialTypes.GetMaterial(this);
            }
        }
        public Colour Colour
        {
            get
            {
                var mat = MaterialData;
                if (mat != null)
                {
                    return mat.Colour;
                }
                return Colour.Red;
            }
        }

        public override string ToString()
        {
            return Rsc5BoundsMaterialTypes.GetMaterialName(this) + " (" + Index.ToString() + ")";
        }

        public static implicit operator byte(Rsc5BoundsMaterialRef r)
        {
            return r.Index;  //implicit conversion
        }

        public static implicit operator Rsc5BoundsMaterialRef(byte b)
        {
            return new Rsc5BoundsMaterialRef() { Index = b };
        }
    }
    [TC(typeof(EXP))] public class Rsc5BoundsMaterialData
    {
        public string Name { get; set; }
        public string FXGroup { get; set; }
        public string HeliFX { get; set; }
        public string Friction { get; set; }
        public string Elasticity { get; set; }
        public string Density { get; set; }
        public string TyreGrip { get; set; }
        public string WetGrip { get; set; }
        public string Roughness { get; set; }
        public string PedDensity { get; set; }
        public string Flammability { get; set; }
        public string BurnTime { get; set; }
        public string BurnStr { get; set; }
        public string SeeThru { get; set; }
        public string ShootThru { get; set; }
        public string IsWet { get; set; }
        public string Material { get; set; }

        public Colour Colour { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
    public static class Rsc5BoundsMaterialTypes
    {
        public static List<Rsc5BoundsMaterialData> Materials;

        public static void Init(Rpf3FileManager fman)
        {
            //Core.Engine.Console.Write("Rsc5BoundsMaterialTypes", "Initialising Bounds Material Types...");

            var list = new List<Rsc5BoundsMaterialData>();
            string filename = "common\\data\\materials\\materials.dat";

            var physpath = fman.Folder + "\\" + filename;
            var txt = System.IO.File.ReadAllText(physpath);
            ////######## TODO: correctly handle DLC

            AddMaterialsDat(txt, list);

            Materials = list;
        }

        private static void AddMaterialsDat(string txt, List<Rsc5BoundsMaterialData> list)
        {
            list.Clear();
            if (txt == null) return;
            string[] lines = txt.Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Length < 20) continue;
                if (line[0] == '#') continue;
                string[] parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 10) continue;
                int cp = 0;
                var d = new Rsc5BoundsMaterialData();
                for (int p = 0; p < parts.Length; p++)
                {
                    string part = parts[p].Trim();
                    if (string.IsNullOrWhiteSpace(part)) continue;
                    switch (cp)
                    {
                        case 0: d.Name = part; break;
                        case 1: d.FXGroup = part; break;
                        case 2: d.HeliFX = part; break;
                        case 3: d.Friction = part; break;
                        case 4: d.Elasticity = part; break;
                        case 5: d.Density = part; break;
                        case 6: d.TyreGrip = part; break;
                        case 7: d.WetGrip = part; break;
                        case 8: d.Roughness = part; break;
                        case 9: d.PedDensity = part; break;
                        case 10: d.Flammability = part; break;
                        case 11: d.BurnTime = part; break;
                        case 12: d.BurnStr = part; break;
                        case 13: d.SeeThru = part; break;
                        case 14: d.ShootThru = part; break;
                        case 15: d.IsWet = part; break;
                        case 16: d.Material = part; break;
                    }
                    cp++;
                }
                if (cp != 23)
                { }

                d.Colour = CreateMaterialColour(d.FXGroup);

                list.Add(d);
            }


            //StringBuilder sb = new StringBuilder();
            //foreach (var d in list)
            //{
            //    sb.AppendLine(d.Name);
            //}
            //string names = sb.ToString();

        }

        private static Colour CreateMaterialColour(string fxgroup)
        {
            switch (fxgroup)
            {
                case "VOID":            return new Colour(0, 0, 0);
                case "CONCRETE":        return new Colour(145, 145, 145);
                case "STONE":           return new Colour(185, 185, 185);
                case "PAVING_SLABS":    return new Colour(200, 165, 130);
                case "BRICK_COBBLE":    return new Colour(195, 95, 30);
                case "GRAVEL":          return new Colour(235, 235, 235);
                case "MUD_SOFT":        return new Colour(105, 95, 75);
                case "DIRT_DRY":        return new Colour(175, 160, 140);
                case "SAND":            return new Colour(235, 220, 190);
                case "SNOW":            return new Colour(255, 255, 255);
                case "WOOD":            return new Colour(155, 130, 95);
                case "METAL":           return new Colour(155, 180, 190);
                case "CERAMICS":        return new Colour(220, 210, 195);
                case "MARBLE":          return new Colour(195, 155, 145);
                case "LAMINATE":        return new Colour(240, 230, 185);
                case "CARPET_FABRIC":   return new Colour(250, 100, 100);
                case "LINOLEUM":        return new Colour(205, 150, 80);
                case "RUBBER":          return new Colour(70, 70, 70);
                case "PLASTIC":         return new Colour(255, 250, 210);
                case "CARDBOARD":       return new Colour(120, 115, 95);
                case "PAPER":           return new Colour(230, 225, 220);
                case "MATTRESS_FOAM":   return new Colour(230, 235, 240);
                case "PILLOW_FEATHERS": return new Colour(230, 230, 230);
                case "GRASS":           return new Colour(130, 205, 75);
                case "BUSHES":          return new Colour(85, 160, 30);
                case "TREE_BARK_DARK":  return new Colour(105, 90, 80);
                case "TREE_BARK_MEDIUM":return new Colour(115, 100, 70);
                case "TREE_BARK_LIGHT": return new Colour(125, 110, 80);
                case "FLOWERS":         return new Colour(200, 180, 190);
                case "LEAVES_PILE":     return new Colour(70, 100, 50);
                case "GLASS":           return new Colour(205, 240, 255);
                case "WINDSCREEN":      return new Colour(210, 245, 245);
                case "CAR_METAL":       return new Colour(255, 255, 255);
                case "CAR_PLASTIC":     return new Colour(255, 255, 255);
                case "WATER":           return new Colour(55, 145, 230);
                case "GENERIC":         return new Colour(255, 0, 255);
                case "PED_HEAD":        return new Colour(255, 55, 20);
                case "PED_TORSO":       return new Colour(255, 55, 20);
                case "PED_LIMB":        return new Colour(185, 100, 85);
                case "PED_FOOT":        return new Colour(185, 100, 85);
                case "TVSCREEN":        return new Colour(115, 125, 125);
                case "VIDEOWALL":       return new Colour(115, 125, 125);
                default: return new Colour(0xFFCCCCCC);
            }
        }


        public static Rsc5BoundsMaterialData GetMaterial(Rsc5BoundsMaterialRef r)
        {
            if (Materials == null) return null;
            if (r.Index >= Materials.Count) return null;
            return Materials[r.Index];
        }

        public static Rsc5BoundsMaterialData GetMaterial(byte index)
        {
            if (Materials == null) return null;
            if ((int)index >= Materials.Count) return null;
            return Materials[index];
        }

        public static string GetMaterialName(Rsc5BoundsMaterialRef r)
        {
            var m = GetMaterial(r);
            if (m == null) return string.Empty;
            return m.Name;
        }

        public static Colour GetMaterialColour(Rsc5BoundsMaterialRef r)
        {
            var m = GetMaterial(r);
            if (m == null) return new Colour(0xFFCCCCCC);
            return m.Colour;
        }
    }

}
