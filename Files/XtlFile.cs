using CodeX.Core.Engine;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    //.xtl and .xtp, both resource type 83. The damage/zone/scratch maps hang off the root, and
    //anything else the package carries (brake rotors, calipers, wheels) lives in the shader
    //group's own texture dictionary.
    public class XtlFile(Rpf3FileEntry file) : TexturePack(file)
    {
        public Rsc5XtlTextureDictionary Taillights = null;

        public override void Load(byte[] data)
        {
            var e = FileInfo as Rpf3ResourceFileEntry;
            var r = new Rsc5DataReader(e, data, Core.Utilities.DataEndianess.BigEndian);

            Taillights = r.ReadBlock<Rsc5XtlTextureDictionary>();
            Textures = [];

            if (Taillights == null)
            {
                return;
            }

            var texs = new[]
            {
                Taillights.ZoneTexture.Item,
                Taillights.MaxDamageTexture.Item,
                Taillights.ScratchTexture.Item
            };

            foreach (var tex in texs)
            {
                Add(tex);
            }

            var dict = Taillights.ShaderGroup.Item?.TextureDictionary.Item;
            if (dict?.Textures.Items != null)
            {
                foreach (var tex in dict.Textures.Items)
                {
                    Add(tex);
                }
            }
        }

        private void Add(Rsc5Texture tex)
        {
            if (tex?.Name == null || tex.Data == null) return;
            tex.FilePack = this;
            Textures[tex.Name] = tex;
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
