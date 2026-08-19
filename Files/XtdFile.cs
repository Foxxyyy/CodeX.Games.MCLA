using CodeX.Core.Engine;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    public class XtdFile(Rpf3FileEntry file) : TexturePack(file)
    {
        public Rsc5TextureDictionary TextureDictionary = null;

        public override void Load(byte[] data)
        {
            var e = FileInfo as Rpf3ResourceFileEntry;
            var r = new Rsc5DataReader(e, data, Core.Utilities.DataEndianess.BigEndian);

            TextureDictionary = r.ReadBlock<Rsc5TextureDictionary>();
            Textures = [];

            var textures = TextureDictionary?.Textures.Items;
            var hashes = TextureDictionary?.Hashes.Items;

            if (textures != null && hashes != null)
            {
                for (int i = 0; i < textures.Length; i++)
                {
                    var tex = textures[i];
                    var hash = hashes[i];

                    tex.Name ??= hash.ToString();
                    Textures[tex.Name] = tex;
                }
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