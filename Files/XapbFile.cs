using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    internal class XapbFile : PiecePack
    {
        public Rsc5DrawableDictionary<Rsc5Drawable> DrawableDictionary;
        public string Name;

        public XapbFile(Rpf3FileEntry file) : base(file)
        {
            DrawableDictionary = null;
            Name = file.NameLower;
        }

        public override void Load(byte[] data)
        {
            var e = FileInfo as Rpf3ResourceFileEntry;
            var r = new Rsc5DataReader(e, data);

            DrawableDictionary = r.ReadBlock<Rsc5DrawableDictionary<Rsc5Drawable>>();
            Pieces = new Dictionary<JenkHash, Piece>();

            if ((DrawableDictionary?.Drawables.Item != null) && (DrawableDictionary?.Hashes.Items != null))
            {
                var drawable = DrawableDictionary.Drawables.Item;
                var hashes = DrawableDictionary.Hashes.Items;

                var hash = hashes[0];
                Pieces[hash] = drawable;
                Piece = drawable;
            }
        }

        public override byte[] Save()
        {
            return null;
        }
    }
}
