using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    public class MapFile : PiecePack
    {
        public Rsc5MapDictionary<Rsc5Drawable> Fragment;

        public MapFile(Rpf3FileEntry file) : base(file)
        {
            Fragment = null;
        }

        public override void Load(byte[] data)
        {
            if (FileInfo is not Rpf3ResourceFileEntry e)
                return;

            var r = new Rsc5DataReader(e, data);
            Fragment = r.ReadBlock<Rsc5MapDictionary<Rsc5Drawable>>();
            Pieces = new Dictionary<JenkHash, Piece>();

            if (Fragment != null)
            {
                var drawable = Fragment.Drawables.Item;
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