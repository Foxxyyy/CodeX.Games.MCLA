using CodeX.Core.Engine;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    class XtlFile(Rpf3FileEntry file) : TexturePack(file)
    {
        public Rsc5XtlTextureDictionary Taillights = null;

        public override void Load(byte[] data)
        {
            var e = FileInfo as Rpf3ResourceFileEntry;
            var r = new Rsc5DataReader(e, data, Core.Utilities.DataEndianess.BigEndian);

            Taillights = r.ReadBlock<Rsc5XtlTextureDictionary>();
            Textures = [];

            if (Taillights != null)
            {
                var texs = new[]
                {
                    Taillights.ZoneTexture.Item,
                    Taillights.MaxDamageTexture.Item,
                    Taillights.ScratchTexture.Item
                };

                foreach (var tex in texs)
                {
                    if (tex != null)
                    {
                        Textures[tex.Name] = tex;
                    }
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
