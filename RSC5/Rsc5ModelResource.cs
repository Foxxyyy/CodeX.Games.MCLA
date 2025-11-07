namespace CodeX.Games.MCLA.RSC5
{
    public class Rsc5ModelResource : Rsc5BlockBaseMap //Fragments seem to be constructed dynamically from model resources
    {
        public override ulong BlockLength => 60;
        public override uint VFT { get; set; } = 0x0057C350;
        public Rsc5Ptr<Rsc5Drawable> Drawable { get; set; }
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

        public override void Read(Rsc5DataReader reader)
        {
            base.Read(reader);
            Drawable = reader.ReadPtr<Rsc5Drawable>();
            Unknown_Ch = reader.ReadUInt32();
            Unknown_10h = reader.ReadUInt32();
            Unknown_14h = reader.ReadPtrArr<Rsc5StringA>();
            Unknown_1Ch = reader.ReadPtrArr<Rsc5BlockMap>();
            SkeletonRef = reader.ReadPtr<Rsc5SkeletonData>();
            Unknown_28h = reader.ReadPtr<Rsc5BlockMap>();
            Unknown_2Ch = reader.ReadPtr<Rsc5BlockMap>();
            Unknown_30h = reader.ReadPtr<Rsc5BlockMap>();
            Unknown_34h = reader.ReadInt32();
            Unknown_38h = reader.ReadUInt32();
            Drawable.Item?.SetSkeleton(SkeletonRef.Item);
        }

        public override void Write(Rsc5DataWriter writer)
        {
            base.Write(writer);
            writer.WritePtr(BlockMap);
            writer.WritePtr(Drawable);
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