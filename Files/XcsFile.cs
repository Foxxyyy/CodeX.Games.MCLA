using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    public class XcsFile(Rpf3FileEntry file) : PiecePack(file)
    {
        public Rpf3FileEntry Entry = file;
        public Rsc5CitySector CitySector { get; private set; }
        public Rsc5TextureDictionary CityTextures { get; private set; }
        public JenkHash Hash { get; } = JenkHash.GenHash(file?.NameLower ?? "");
        public string Name { get; } = file?.NameLower;
        public bool DependenciesLoaded { get; set; }

        public override void Load(byte[] data)
        {
            var e = FileInfo as Rpf3ResourceFileEntry;
            var r = new Rsc5DataReader(e, data)
            {
                Position = Rpf3Crypto.VIRTUAL_BASE
            };

            CitySector = r.ReadBlock<Rsc5CitySector>();
            Pieces = [];

            if (CitySector != null)
            {
                var piece = CitySector.SectorPiece.Item;
                if (piece != null)
                {
                    Piece = piece;
                    Pieces.Add(e.ShortNameLower, piece);
                }
            }
        }

        public override byte[] Save()
        {
            return null;
        }
    }
}