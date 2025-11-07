using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    public class XwtFile(Rpf3FileEntry file) : TexturePack(file)
    {
        public Rpf3FileEntry Entry = file;
        public Rsc5Bitmap Bitmap { get; private set; }

        public override void Load(byte[] data)
        {
            var e = FileInfo as Rpf3ResourceFileEntry;
            var r = new Rsc5DataReader(e, data, DataEndianess.BigEndian);

            Bitmap = r.ReadBlock<Rsc5Bitmap>();
            Textures = [];

            if (Bitmap != null)
            {
                var tex1 = Bitmap?.Texture1.Item;
                var tex2 = Bitmap?.Texture2.Item;

                if (tex1 != null)
                    Textures[tex1.Name] = tex1;
                if (tex2 != null)
                    Textures[tex2.Name] = tex2;
            }
        }

        public override byte[] Save()
        {
            return null;
        }

        public override void BuildFromTextureList(List<Texture> textures)
        {
            base.BuildFromTextureList(textures);
        }
    }
}