using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Forms.Utilities;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    public class XshpFile : PiecePack
    {
        public Rsc5Bitmap Bitmap;
        public Rsc5City City;

        public XshpFile(Rpf3FileEntry file) : base(file)
        {
            City = null;
            Bitmap = null;
        }

        public override void Load(byte[] data)
        {
            if (FileInfo is not Rpf3ResourceFileEntry e)
            {
                return;
            }

            var r = new Rsc5DataReader(e, data);
            var ident = (Rsc5XshpType)r.ReadUInt32();
            r.Position = Rpf3Crypto.VIRTUAL_BASE;

            if (ident == Rsc5XshpType.CITY)
            {
                City = r.ReadBlock<Rsc5City>();
            }
            else if (ident == Rsc5XshpType.BITMAP_VINYL || ident == Rsc5XshpType.BITMAP_TIRE)
            {
                Bitmap = r.ReadBlock<Rsc5Bitmap>();
            }

            Pieces = new Dictionary<JenkHash, Piece>();
            if (Bitmap != null)
            {
                var tex1 = Bitmap?.Texture1.Item;
                var tex2 = Bitmap?.Texture2.Item;
                var txp = new TexturePack(e) { Textures = new Dictionary<string, Texture>() };

                if (tex1 != null)
                {
                    txp.Textures[tex1.Name] = tex1;
                    tex1.Pack = txp;
                }
                if (tex2 != null)
                {
                    txp.Textures[tex2.Name] = tex2;
                    tex2.Pack = txp;
                }

                var texCount = txp.Textures?.Count ?? 0;
                if (texCount > 0)
                {
                    Piece = new Piece { TexturePack = txp };
                    Pieces.Add(e.ShortNameLower, Piece);
                }
                MessageBoxEx.Show(null, $"Detected XSHP bitmap file ({texCount} texture{(texCount > 1 ? "s" : "")} & 0 model)");
            }
            else if (City != null)
            {
                var drawable = City.Drawable.Item;
                var dict = City.Dictionary.Item;
                var txp = new TexturePack(e) { Textures = new Dictionary<string, Texture>() };

                if (dict != null)
                {
                    for (int i = 0; i < dict.Textures.Items.Length; i++)
                    {
                        var tex = dict.Textures.Items[i];
                        var hash = dict.HashTable.Items[i];

                        tex.Name ??= hash.ToString();
                        txp.Textures[hash.ToString()] = tex;
                        tex.Pack = txp;
                    }
                }

                if (drawable != null)
                {
                    drawable.TexturePack = txp;
                    Piece = drawable;
                    Pieces.Add(e.ShortNameLower, drawable);
                }
            }
        }

        public override byte[] Save()
        {
            return null;
        }
    }

    public enum Rsc5XshpType : uint
    {
        BITMAP_VINYL = 0x40CC5600,
        BITMAP_TIRE = 0x9CC65600,
        CITY = 0x10B75C00,
        UNKNOWN = 0x3CAF5C00
    }
}