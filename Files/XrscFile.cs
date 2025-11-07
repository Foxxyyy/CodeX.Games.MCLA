using CodeX.Core.Engine;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    public class XrscFile(Rpf3FileEntry file) : PiecePack(file)
    {
        public Rsc5ModelResource Fragment = null;

        public override void Load(byte[] data)
        {
            if (FileInfo is not Rpf3ResourceFileEntry e) return;

            var r = new Rsc5DataReader(e, data);
            Fragment = r.ReadBlock<Rsc5ModelResource>();
            Pieces = [];

            if (Fragment != null)
            {
                var drawable = Fragment.Drawable.Item;
                drawable.FilePack = this;

                Piece = drawable;
                Pieces.Add(e.ShortNameHash, drawable);
            }
        }

        public override byte[] Save()
        {
            return null;
        }
    }
}