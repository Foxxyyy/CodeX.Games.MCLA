using CodeX.Core.Engine;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    public class XshpFile : TexturePack
    {
        public Rsc5Bitmap BitMap;

        public XshpFile(Rpf3FileEntry file) : base(file)
        {
            BitMap = null;
        }

        public override void Load(byte[] data)
        {
            if (FileInfo is not Rpf3ResourceFileEntry e)
            {
                return;
            }

            var r = new Rsc5DataReader(e, data);
            BitMap = r.ReadBlock<Rsc5Bitmap>();
            Textures = new Dictionary<string, Texture>();

            if (BitMap != null)
            {
                var tex = BitMap?.Texture1.Item;
                var tex2 = BitMap?.Texture2.Item;

                if (tex != null)
                    Textures[tex.Name] = tex;
                if (tex2 != null)
                    Textures[tex2.Name] = tex2;
            }
        }

        public override byte[] Save()
        {
            return null;
        }
    }
}