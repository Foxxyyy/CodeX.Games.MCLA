using CodeX.Core.Engine;
using CodeX.Core.Numerics;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;
using System.Numerics;
using System.Xml.Linq;

namespace CodeX.Games.MCLA.Files
{
    public class XmdFile : PiecePack
    {
        public Rpf3FileEntry FileEntry { get; private set; }
        public Rsc5MapDistrictLod MapDrawable { get; private set; }

        public XmdFile()
        {
        }

        public XmdFile(Rpf3FileEntry entry) : base(entry)
        {
            FileEntry = entry;
        }

        public override void Load(byte[] data)
        {
            var e = FileInfo as Rpf3ResourceFileEntry;
            var r = new Rsc5DataReader(e, data, DataEndianess.BigEndian);

            Pieces = [];
            MapDrawable = r.ReadBlock<Rsc5MapDistrictLod>();

            var lod = MapDrawable.Lod;
            var models = lod?.ModelsData.Items;

            if (models != null)
            {
                Pieces[e.ShortNameLower] = MapDrawable;
                for (int i = 0; i < models.Length; i++)
                {
                    var mdl = models[i];
                    if (mdl == null) continue;

                    var name = $"{e.ShortNameLower}_{i}";
                    var clone = new Rsc5MapDistrictLod(mdl, MapDrawable.ShaderGroup, name);
                    Pieces[name] = clone;
                }
            }
        }

        public override byte[] Save()
        {
            return null;
        }
    }
}