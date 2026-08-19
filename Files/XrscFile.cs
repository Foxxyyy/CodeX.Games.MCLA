using CodeX.Core.Utilities;
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

            //A "_set" package holds a whole wardrobe of drawables, everything else holds one
            var drawables = Fragment?.Drawables;
            if (drawables != null && drawables.Length > 0)
            {
                foreach (var drawable in drawables)
                {
                    drawable.FilePack = this;
                    drawable.Name ??= e.Name;
                    Pieces[JenkHash.GenHash(drawable.Name)] = drawable;
                }
                Piece = drawables[0];
            }
            else if (Fragment?.Drawable != null)
            {
                var drawable = Fragment.Drawable;
                drawable.FilePack = this;
                drawable.Name ??= e.Name;

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