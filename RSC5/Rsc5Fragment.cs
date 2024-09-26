namespace CodeX.Games.MCLA.RSC5
{
    public class Rsc5Fragment<T> : Rsc5FileBase where T : Rsc5LodBase, new()
    {
        public override ulong BlockLength => 64;
        public Rsc5Ptr<Rsc5BlockMap> BlockMapPointer { get; set; }
        public Rsc5Ptr<T> Drawables { get; set; }
        public uint Unknown_Ch { get; set; }
        public uint Unknown_10h { get; set; }
        public Rsc5PtrArr<Rsc5BlockMap> Unknown_14h { get; set; }
        public uint Unknown_1Ch { get; set; }
        public uint Unknown_20h { get; set; }
        public Rsc5Ptr<Rsc5Skeleton> SkeletonRef { get; set; }

        public override void Read(Rsc5DataReader reader)
        {
            VFT = reader.ReadUInt32();
            BlockMapPointer = reader.ReadPtr<Rsc5BlockMap>();
            Drawables = reader.ReadPtr<T>();
            Unknown_Ch = reader.ReadUInt32();
            Unknown_10h = reader.ReadUInt32();
            Unknown_14h = reader.ReadPtrArr<Rsc5BlockMap>(); //Name?
            Unknown_1Ch = reader.ReadUInt32();
            Unknown_20h = reader.ReadUInt32();
            SkeletonRef = reader.ReadPtr<Rsc5Skeleton>();
        }

        public override void Write(Rsc5DataWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}