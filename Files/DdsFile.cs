using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;

namespace CodeX.Games.MCLA.Files
{
    //Plain DDS files sitting loose in the archive (sky, vehicle/shared_font, ...). They're
    //standard little endian DDS, not a RAGE resource, so they just need wrapping in a pack.
    public class DdsFile(Rpf3FileEntry file) : TexturePack(file)
    {
        public override void Load(byte[] data)
        {
            Textures = [];

            var tex = DDSIO.GetTexture(data);
            if (tex == null) return;

            tex.Name = System.IO.Path.GetFileNameWithoutExtension(FileInfo?.Name ?? "texture");
            tex.Sampler = TextureSampler.AnisotropicWrap;
            tex.FilePack = this;
            Textures[tex.Name] = tex;
        }

        public override byte[] Save()
        {
            return null;
        }
    }
}
