using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    //.xsf, the serialized Flash movie used for every piece of UI (hud, pause, garage...).
    //It is not a texture dictionary - the art is spread over the bitmap and font characters,
    //so the pack is built by walking the character table and sweeping for the leftover atlases.
    public class XsfFile(Rpf3FileEntry file) : TexturePack(file)
    {
        public Rpf3FileEntry Entry = file;
        public Rsc5FlashMovie Movie { get; private set; }
        public List<string> ExternalTextures { get; } = [];

        public override void Load(byte[] data)
        {
            var e = FileInfo as Rpf3ResourceFileEntry;
            var r = new Rsc5DataReader(e, data, DataEndianess.BigEndian);

            Movie = r.ReadBlock<Rsc5FlashMovie>();
            Textures = [];

            var characters = Movie?.Characters;
            if (characters != null)
            {
                foreach (var character in characters)
                {
                    if (character == null) continue;
                    if (character.ObjectType != Rsc5FlashObjectType.Bitmap) continue;

                    if (character.Texture != null)
                    {
                        AddTexture(character.Texture);
                    }
                    else if (!string.IsNullOrEmpty(character.ExternalTextureName))
                    {
                        ExternalTextures.Add(character.ExternalTextureName);
                    }
                }
            }

            //Fonts reach their glyph atlas through structures that aren't decoded yet, so anything
            //the character walk missed is picked up by sweeping the virtual segment.
            foreach (var position in Rsc5Flash.FindTextureBlocks(data, r.VirtualSize))
            {
                try
                {
                    var tex = r.ReadBlock<Rsc5Texture>(position);
                    if (tex != null)
                    {
                        AddTexture(tex);
                    }
                }
                catch
                {
                }
            }
        }

        private void AddTexture(Rsc5Texture tex)
        {
            if (tex.Data == null) return;

            tex.Name = GetShortName(tex.Name);
            tex.FilePack = this;

            var name = tex.Name;
            for (int i = 1; Textures.TryGetValue(name, out var existing) && existing != tex; i++)
            {
                name = tex.Name + "#" + i;
            }
            Textures[name] = tex;
        }

        //Names are authoring paths, eg "t:/mc4/assets/ui/hud/textures/hud_0327"
        private static string GetShortName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "texture";
            var slash = name.LastIndexOfAny(['/', '\\']);
            return (slash >= 0) ? name[(slash + 1)..] : name;
        }

        public override byte[] Save()
        {
            return null;
        }
    }
}
