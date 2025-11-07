using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    public class XctFile : FilePack
    {
        public Rpf3FileEntry FileEntry;
        public Rsc5City CityData;

        public XctFile()
        {
        }

        public XctFile(Rpf3FileEntry file) : base(file)
        {
            FileEntry = file;
        }

        public override void Load(byte[] data)
        {
            var e = FileInfo as Rpf3ResourceFileEntry;
            var r = new Rsc5DataReader(e, data, DataEndianess.BigEndian);
            CityData = r.ReadBlock<Rsc5City>();
        }

        public override byte[] Save()
        {
            return null;
        }

        public override void Read(MetaNodeReader reader)
        {
        }

        public override void Write(MetaNodeWriter writer)
        {
            CityData?.Write(writer);
        }
    }
}