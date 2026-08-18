using System.Linq;
using CodeX.Core.Engine;

namespace CodeX.Games.MCLA.RSC5
{
    public class Rsc5ModelResource : Rsc5BlockBaseMap //Fragments seem to be constructed dynamically from model resources
    {
        //VFTs are stored little-endian in the file, so they read back byte reversed.
        //0x6DE5B900 = 0x00B9E56D, the plain model resource; 0xF8726100 = 0x006172F8, the "_set" package.
        public const uint SetPackageVFT = 0xF8726100;

        public override ulong BlockLength => 60;
        public override uint VFT { get; set; } = 0x0057C350;
        public Rsc5Ptr<Rsc5DrawableLod> DrawableLod { get; set; }
        public uint Unknown_Ch { get; set; } //Always 0?
        public uint Unknown_10h { get; set; } //Always 0?
        public Rsc5PtrArr<Rsc5StringA> Unknown_14h { get; set; }
        public Rsc5PtrArr<Rsc5BlockMap> Unknown_1Ch { get; set; } //Always 0?
        public Rsc5Ptr<Rsc5SkeletonData> SkeletonRef { get; set; }
        public Rsc5Ptr<Rsc5BlockMap> Unknown_28h { get; set; } //Some stuff related to occluders?
        public Rsc5Ptr<Rsc5BlockMap> Unknown_2Ch { get; set; }
        public Rsc5Ptr<Rsc5BlockMap> Unknown_30h { get; set; }
        public int Unknown_34h { get; set; } //-1, 2
        public uint Unknown_38h { get; set; } //Always 0?

        //Only set for the "_set" package, which carries complete drawables instead of a bare lod
        public Rsc5Str Name { get; set; }
        public Rsc5DrawableBase[] Drawables { get; private set; }

        public Piece Drawable { get; private set; }
        public string LoadError { get; private set; }

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);

            if (VFT == SetPackageVFT)
            {
                ReadSetPackage(reader);
                return;
            }

            // Normal Model Resource
            DrawableLod = reader.ReadPtr<Rsc5DrawableLod>(); // 0x08
            Unknown_Ch = reader.ReadUInt32(); // 0x0C
            var Unknown_10h = reader.ReadUInt32(); // 0x10
            var Unknown_14h = reader.ReadUInt32(); // 0x14
            var Unknown_18h = reader.ReadUInt32(); // 0x18
            var Unknown_1Ch = reader.ReadPtrArr<Rsc5BlockMap>(); // 0x1C - 0x23
            SkeletonRef = reader.ReadPtr<Rsc5SkeletonData>(); // 0x24 - 0x27
            Unknown_28h = reader.ReadPtr<Rsc5BlockMap>(); // 0x28 - 0x2B
            Unknown_2Ch = reader.ReadPtr<Rsc5BlockMap>(); // 0x2C - 0x2F
            var Unknown_30h = reader.ReadPtr<Rsc5BlockMap>(); // 0x30 - 0x33
            var Unknown_34h = reader.ReadInt32(); // 0x34 - 0x37
            var Unknown_38h = reader.ReadUInt32(); // 0x38 - 0x3B
            var Unknown_3Ch = reader.ReadPtr<Rsc5BlockMap>(); // 0x3C - 0x3F

            try
            {
                var lod = DrawableLod.Item;
                if (lod == null)
                {
                    LoadError = "no drawable lod";
                    return;
                }

                lod.LodDist = 9999f;
                var drawable = new Rsc5Drawable
                {
                    Lod = lod,
                    Lods = [lod]
                };
                drawable.Name = System.IO.Path.GetFileNameWithoutExtension(reader.FileEntry?.Name);
                drawable.SetSkeleton(SkeletonRef.Item);
                drawable.UpdateAllModels();
                drawable.AssignShaders();
                drawable.UpdateBounds();

                var center = (drawable.BoundingBox.Minimum + drawable.BoundingBox.Maximum) / 2;
                var radius = System.Numerics.Vector3.Distance(center, drawable.BoundingBox.Maximum);
                drawable.BoundingSphere = new CodeX.Core.Numerics.BoundingSphere(center, radius);
                Drawable = drawable;
            }
            catch (System.Exception ex)
            {
                LoadError = ex.GetType().Name + ": " + ex.Message;
            }
        }

        //Character and prop "_set" resources. The payload is one or more complete rmcDrawables
        //(shader group, skeleton, bounds and the four lods), not the lod-only layout above. Some
        //files hang a single drawable off the root, others reach a whole wardrobe of them through
        //a container we don't decode, so the drawables are located by signature instead.
        private void ReadSetPackage(Rsc5DataReader reader)
        {
            Name = reader.ReadStr(); //0x08
            Unknown_Ch = reader.ReadUInt32(); //0x0C, u16 count + u16 capacity

            var positions = FindDrawables(reader.Data, reader.VirtualSize);
            var drawables = new List<Rsc5DrawableBase>();
            var baseName = Name.Value ?? System.IO.Path.GetFileNameWithoutExtension(reader.FileEntry?.Name);

            foreach (var position in positions)
            {
                try
                {
                    var drawable = reader.ReadBlock<Rsc5DrawableBase>(position);
                    if (drawable?.AllModels == null || drawable.AllModels.Length == 0) continue;

                    drawable.Name ??= (positions.Count > 1)
                        ? baseName + "_" + drawables.Count.ToString("00")
                        : baseName;
                    drawables.Add(drawable);
                }
                catch
                {
                    //A broken drawable shouldn't cost us the rest of the package
                }
            }

            Drawables = [.. drawables];
            Drawable = drawables.FirstOrDefault();
            if (Drawable == null)
            {
                LoadError = "no drawable";
            }
        }

        //An rmcDrawable is recognised by its three bounding vectors: the packer leaves 0x7F800001
        //in the w component of each. Together with a null block map and a resolvable shader group
        //and lod that's enough to pick them out of the segment without a false positive.
        private static List<ulong> FindDrawables(byte[] data, int virtualSize)
        {
            var result = new List<ulong>();
            var limit = Math.Min(virtualSize, data.Length) - 0x80;

            for (int offset = 0; offset <= limit; offset += 16)
            {
                if (ReadUInt32(data, offset + 0x04) != 0) continue;
                if (ReadUInt32(data, offset + 0x1C) != 0x7F800001) continue;
                if (ReadUInt32(data, offset + 0x2C) != 0x7F800001) continue;
                if (ReadUInt32(data, offset + 0x3C) != 0x7F800001) continue;
                if (!IsVirtual(ReadUInt32(data, offset + 0x08), virtualSize, false)) continue;

                var lods = false;
                for (int i = 0; i < 4; i++)
                {
                    if (!IsVirtual(ReadUInt32(data, offset + 0x40 + i * 4), virtualSize, true)) { lods = false; break; }
                    lods |= ReadUInt32(data, offset + 0x40 + i * 4) != 0;
                }
                if (!lods) continue;

                result.Add(0x50000000ul | (uint)offset);
            }
            return result;
        }

        private static bool IsVirtual(uint pointer, int virtualSize, bool allowNull)
        {
            if (pointer == 0) return allowNull;
            if ((pointer & 0xF0000000) != 0x50000000) return false;
            return (pointer & 0x0FFFFFFF) < (uint)virtualSize;
        }

        private static uint ReadUInt32(byte[] data, int offset)
        {
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
        }

        public override void Write(Rsc5DataWriter writer)
        {
            base.Write(writer);
            writer.WritePtr(BlockMap);
            writer.WritePtr(DrawableLod);
            writer.WriteUInt32(Unknown_Ch);
            writer.WriteUInt32(Unknown_10h);
            writer.WritePtrArr(Unknown_14h);
            writer.WritePtrArr(Unknown_1Ch);
            writer.WritePtr(SkeletonRef);
            writer.WritePtr(Unknown_28h);
            writer.WritePtr(Unknown_2Ch);
            writer.WritePtr(Unknown_30h);
            writer.WriteInt32(Unknown_34h);
            writer.WriteUInt32(Unknown_38h);
        }
    }
}
