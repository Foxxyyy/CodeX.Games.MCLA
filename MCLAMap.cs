using CodeX.Core.Engine;
using CodeX.Core.Numerics;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.Files;
using CodeX.Games.MCLA.RPF3;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace CodeX.Games.MCLA
{
    public class MCLAMap : StreamingLevel<MCLAGame, MCLAMapFileCache, Rpf3FileManager>
    {
        public List<Rpf3Entry> CityFiles;
        public Dictionary<JenkHash, MCLAMapNode> MapNodeDict;
        public Dictionary<JenkHash, MCLAMapNode> StreamNodesPrev;
        public Dictionary<JenkHash, MCLAMapNode> StreamNodes;

        public static readonly Setting EnabledSetting = Settings.Register("MCLAMap.Enabled", SettingType.Bool, true, true);
        public static readonly Setting StartPositionSetting = Settings.Register("MCLAMap.StartPosition", SettingType.Vector3, new Vector3(1641.5f, 765.0f, -11.0f));

        public Statistic NodeCountStat = Statistics.Register("MCLAMap.NodeCount", StatisticType.Counter);
        public Statistic EntityCountStat = Statistics.Register("MCLAMap.EntityCount", StatisticType.Counter);

        public override string[] GetSelectionModes()
        {
            return
            [
                "Entity",
                "Occlusion"
            ];
        }

        public MCLAMap(MCLAGame game) : base(game, "MCLA Map Level")
        {
            this.Game = game;
            this.DefaultSpawnPoint = StartPositionSetting.GetVector3();
            this.BoundingBox = new BoundingBox(new Vector3(-100000.0f), new Vector3(100000.0f));
            this.InitRenderData();
        }

        protected override bool StreamingInit()
        {
            Core.Engine.Console.Write("MCLAMap", "Initialising " + this.Game.Name + "...");
            this.FileManager = Game.GetFileManager() as Rpf3FileManager;

            if (this.FileManager == null)
            {
                throw new Exception("Failed to initialize MCLA.");
            }

            if (EnabledSetting.GetBool() == false)
            {
                Cache = new MCLAMapFileCache(FileManager);
                return true;
            }

            var dfm = this.FileManager.DataFileMgr;
            var xct = dfm.GetSectorBounds();
            var xmd = dfm.LoadMapLod();

            this.Cache = new MCLAMapFileCache(this.FileManager);
            this.MapNodeDict = [];
            this.StreamNodes = [];
            this.StreamNodesPrev = [];
            this.StreamPosition = this.DefaultSpawnPoint;

            //Global map LOD (always visible)
            if (xmd?.MapDrawable != null)
            {
                var node = new MCLAMapNode(xmd);
                MapNodeDict[node.NameHash] = node;
            }

            //Occluders (always active)
            var occluders = new MCLAMapNode(this.FileManager);
            this.MapNodeDict[occluders.NameHash] = occluders;

            //Individual streaming sectors
            foreach (var kv in dfm.XcsFiles)
            {
                var node = new MCLAMapNode(kv.Value, xct);
                this.MapNodeDict[node.NameHash] = node;
            }

            this.StreamBVH = new StreamingBVH();
            foreach (var kvp in this.MapNodeDict)
            {
                var mapnode = kvp.Value;
                if (mapnode.StreamingBox.Minimum != mapnode.StreamingBox.Maximum)
                {
                    this.StreamBVH.Add(mapnode);
                }
            }

            Core.Engine.Console.Write("MCLAMap", FileManager.Game.Name + " map initialised.");
            return true;
        }

        protected override bool StreamingUpdate()
        {
            if (this.StreamNodes == null) return false;
            if (!EnabledSetting.GetBool()) return false;

            var helpers = this.StreamHelpers.CurrentSet;
            var nodes = this.StreamNodesPrev;
            var ents = this.StreamEntities.CurrentSet;
            var spos = this.StreamPosition;

            this.StreamNodesPrev = this.StreamNodes;
            nodes.Clear();

            foreach (var kvp in this.MapNodeDict)
            {
                var node = kvp.Value;
                var center = node.StreamingBox.Center;
                var dist = Vector3.Distance(center, spos);

                if (node.StreamingBox.Contains(ref spos) == ContainmentType.Contains)
                    node.EnsureLoaded(this.FileManager, this.Cache);
                else
                    node.Unload();

                if (!node.Enabled) continue;
                
                var mapdata = node.MapData;
                if (mapdata?.Entities != null)
                {
                    foreach (var e in mapdata.Entities)
                    {
                        RecurseAddStreamEntity(e, ref spos, ents);
                        if (e.Piece == null && mapdata.IsLodMap)
                        {
                            mapdata.SetLodPiece();
                        }
                    }
                }
                
                if (mapdata?.Helpers != null)
                {
                    foreach (var e in mapdata.Helpers)
                    {
                        if (e?.Piece == null) continue;
                        helpers.Add(e);
                    }
                }
            }

            foreach (var ent in ents.ToList()) //Make sure all current entities assets are loaded
            {
                if (ent.Name.StartsWith("lod")) continue;
                var hash = new JenkHash(ent.Level.Name);
                var pp = Cache.GetPiecePack(this.FileManager, hash, Rpf3ResourceType.Generic);
                var changed = pp?.Piece != ent.Piece;
                
                ent.Piece = pp?.Piece;
                if (pp?.Piece != null && changed && pp.Piece.Lods != null)
                {
                    var ld = 0.0f;
                    for (int i = 0; i < pp.Piece.Lods.Length; i++)
                    {
                        if (pp.Piece.Lods[i] != null)
                        {
                            ld = Math.Max(ld, pp.Piece.Lods[i].LodDist);
                        }
                    }
                    ent.LodDistMax = ld;
                }
            }

            this.NodeCountStat.SetCounter(nodes.Count);
            this.EntityCountStat.SetCounter(ents.Count);
            this.StreamNodes = nodes;

            return true;
        }

        private static void RecurseAddStreamEntity(Entity e, ref Vector3 spos, HashSet<Entity> ents)
        {
            e.StreamingDistance = (e.Position - spos).Length();
            if (e.BoundingBox.Contains(ref spos) == ContainmentType.Contains)
            {
                e.StreamingDistance = 0.0f;
            }

            if ((e.StreamingDistance <= e.LodDistMax) && (e.StreamingDistance >= e.LodDistMin))
            {
                ents.Add(e);
            }
        }
    }

    public class MCLAMapNode : StreamingBVHItem
    {
        public BoundingBox StreamingBox { get; set; }
        public BoundingBox BoundingBox { get; set; }

        public MCLAMapNode ParentNode;
        public MCLAMapData MapData;
        public JenkHash NameHash;
        public bool Enabled;

        public MCLAMapNode(XcsFile xcs, XctFile xct)
        {
            this.MapData = new MCLAMapData(xcs);
            this.NameHash = xcs.Hash;
            this.Enabled = false; //Start unloaded by default

            var sectors = xct?.CityData?.MapSectors.Items;
            if (sectors != null)
            {
                foreach (var sector in sectors)
                {
                    if (sector == null) continue;
                    var hash = new JenkHash(sector.SectorName.ToString().ToLower());

                    if (hash == new JenkHash(xcs.Name.Replace(".xcs", "")))
                    {
                        var bbMin = sector.AABBMin.XYZ();
                        var bbMax = sector.AABBMax.XYZ();
                        this.BoundingBox = new BoundingBox(bbMin, bbMax);

                        var dist = new Vector3(500.0f);
                        this.StreamingBox = new BoundingBox(bbMin - dist, bbMax + dist);
                        break;
                    }
                }
            }
        }

        public MCLAMapNode(XmdFile xmd)
        {
            this.MapData = new MCLAMapData(xmd);
            this.NameHash = xmd.FileEntry.NameHash;
            this.Enabled = true; //LOD always active

            var bb = xmd.MapDrawable.BoundingBox;
            var dist = new Vector3(9999.0f);

            this.BoundingBox = bb;
            this.StreamingBox = new BoundingBox(bb.Minimum - dist, bb.Maximum + dist);
        }

        public MCLAMapNode(Rpf3FileManager mgr)
        {
            this.MapData = new MCLAMapData(mgr);
            this.NameHash = MapData.Name;
            this.BoundingBox = MapData.BoundingBox;
            this.StreamingBox = MapData.StreamingBox;
            this.Enabled = true; //Occluders always active
        }

        public void EnsureLoaded(Rpf3FileManager fm, MCLAMapFileCache cache)
        {
            if (this.Enabled) return;
            if (this.MapData.FilePack is not XcsFile xcs) return;

            var hash = xcs.Hash;
            var piecePack = cache.GetPiecePack(fm, hash, Rpf3ResourceType.Generic);

            if (piecePack is not XcsFile loadedXcs) return;
            this.MapData.FilePack = loadedXcs;

            if (loadedXcs.Piece != null)
            {
                this.MapData.Entities.Clear();
                this.Enabled = true;

                var e = new Entity()
                {
                    Position = BoundingBox.Center,
                    BoundingBox = BoundingBox,
                    LodDistMax = 500.0f,
                    Index = MapData.Entities.Count,
                    Name = loadedXcs.Name,
                    Level = MapData
                };
                this.MapData.Add(e);
            }
        }

        public void Unload()
        {
            if (!this.Enabled) return;
            if (this.MapData.FilePack is not XcsFile xcs) return;

            xcs.Piece = null;
            xcs.DependenciesLoaded = false;

            this.MapData.Entities.Clear();
            this.Enabled = false;
        }

        public override string ToString()
        {
            return this.NameHash.ToString();
        }
    }

    public class MCLAMapData : LevelLod
    {
        public bool IsLodMap { get; set; }

        public MCLAMapData(XcsFile xcs)
        {
            this.FilePack = xcs;
            this.FilePack.EditorObject = this;
            this.Name = xcs.Name;
            this.IsLodMap = false;
        }

        public MCLAMapData(XmdFile xmd)
        {
            this.FilePack = xmd;
            this.FilePack.EditorObject = this;
            this.Name = "lod.xmd";
            this.IsLodMap = true;

            if (xmd.Pieces == null) return;
            foreach (var kv in xmd.Pieces)
            {
                var piece = kv.Value;
                if (piece == null) continue;
                if (piece == xmd.MapDrawable) continue; //Skip global LOD piece

                var bb = piece.BoundingBox;
                var center = bb.Center;

                var e = new Entity()
                {
                    Index = Entities.Count,
                    Position = center,
                    BoundingBox = bb,
                    LodDistMin = 600.0f,
                    LodDistMax = 9999.0f,
                    Name = piece.Name,
                    Piece = piece,
                    Level = this,
                };
                this.Add(e);
            }

            var aabb = xmd.MapDrawable.BoundingBox;
            var dist = new Vector3(1000.0f);
            this.BoundingBox = aabb;
            this.StreamingBox = new BoundingBox(aabb.Minimum - dist, aabb.Maximum + dist);
        }

        public MCLAMapData(Rpf3FileManager mgr)
        {
            var file = mgr.GetEntry("xarchive_cache.rpf\\city\\sc\\city.occluder");
            if (file == null) return;

            var fi = file as GameArchiveFileInfo;
            var data = file.Archive.ExtractFile(fi);
            var txt = mgr.ConvertToText(fi, data, out var _);

            var occluderFile = ParseOccluderFile(txt);
            var bbMin = new Vector3(float.MaxValue);
            var bbMax = new Vector3(float.MinValue);

            var parts = new List<EditablePart>();
            foreach (var oc in occluderFile.Occluders)
            {
                foreach (var v in oc.Vertices)
                {
                    bbMin = Vector3.Min(bbMin, v);
                    bbMax = Vector3.Max(bbMax, v);
                }
                parts.Add(new MCLABoxOccluder(oc, this));
            }

            this.BoundingBox = new BoundingBox(bbMin, bbMax);
            this.StreamingBox = new BoundingBox(bbMin - new Vector3(500.0f), bbMax + new Vector3(500.0f));
            this.Name = "city.occluder";
            this.IsLodMap = false;

            var container = new EditablePart("Occlusion", 0x44F766F9, [.. parts]); //"Occlusion"
            this.AddHelper(new Entity(container, this.ToString()));
        }

        public void SetLodPiece()
        {
            var xmd = FilePack as XmdFile;
            if (xmd?.Pieces == null || Entities == null) return;
            
            foreach (var ent in Entities)
            {
                if (ent.Piece != null) continue;
                if (xmd.Pieces.TryGetValue(ent.Name, out var piece) && piece != null)
                {
                    ent.Piece = piece;
                }
            }
        }

        private static OccluderFile ParseOccluderFile(string text)
        {
            static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

            var lines = text.Split('\n');
            var file = new OccluderFile();
            MCLAOccluder current = null;
            MCLAOccluderPolygon currentPoly = null;

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("//")) continue;

                //File version
                if (line.StartsWith("version:", StringComparison.OrdinalIgnoreCase))
                {
                    file.Version = int.Parse(line.Split(':', 2)[1].Trim());
                    continue;
                }

                //Occluder block start
                if (line.StartsWith("Occluder ", StringComparison.OrdinalIgnoreCase))
                {
                    current = new MCLAOccluder();
                    file.Occluders.Add(current);
                    continue;
                }

                //Occluder type
                if (line.StartsWith("Type ", StringComparison.OrdinalIgnoreCase))
                {
                    current.Type = line.Split(' ', 2)[1].Trim();
                    continue;
                }

                if (line.StartsWith("Name ", StringComparison.OrdinalIgnoreCase))
                {
                    current.Name = line[5..].Trim();
                    continue;
                }

                if (line.StartsWith("BoundingSphere", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    current.BoundingCenter = new Vector3(F(parts[3]), F(parts[1]), F(parts[2]));
                    current.BoundingRadius = F(parts[4]);
                    continue;
                }

                //Vertices block (3 floats)
                var parts3 = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts3.Length == 3 && float.TryParse(parts3[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    current.Vertices.Add(new Vector3(F(parts3[2]), F(parts3[0]), F(parts3[1])));
                    continue;
                }

                //Polygon section start
                if (line.StartsWith("Vertices ", StringComparison.OrdinalIgnoreCase) && line.Contains('4'))
                {
                    currentPoly = new MCLAOccluderPolygon();
                    current.Polygons.Add(currentPoly);
                    continue;
                }

                //Polygon vertex indices line
                if (currentPoly != null && Regex.IsMatch(line, @"^\d+(\s+\d+)+$"))
                {
                    var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                        currentPoly.Vertices.Add(int.Parse(p));
                    continue;
                }

                //Polygon edges block start/end
                if (line.StartsWith("Edges ", StringComparison.OrdinalIgnoreCase)) continue;

                if (line == "}")
                {
                    currentPoly = null;
                    continue;
                }
            }
            return file;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }

    public class MCLABoxOccluder : EditablePart
    {
        public MCLAOccluder Data { get; set; }

        public MCLABoxOccluder(MCLAOccluder occluder, LevelLod owner)
        {
            const uint colour = 0xFFA0A0A0;
            var verts = occluder.Vertices;
            var polys = occluder.Polygons;
            var vlayout = new VertexLayout("PNC");
            var stride = vlayout.Stride; //32 bytes

            var indices = new List<ushort>();
            foreach (var poly in polys)
            {
                if (poly.Vertices.Count < 3) continue;

                for (int i = 1; i < poly.Vertices.Count - 1; i++)
                {
                    indices.Add((ushort)poly.Vertices[0]);
                    indices.Add((ushort)poly.Vertices[i]);
                    indices.Add((ushort)poly.Vertices[i + 1]);
                }
            }

            var numVerts = verts.Count;
            var numTris = indices.Count / 3;
            var vdat = new byte[numVerts * stride];
            var normals = new Vector3[numVerts];
            var normalCount = new int[numVerts];

            for (int t = 0; t < numTris; t++)
            {
                var i0 = indices[t * 3 + 0];
                var i1 = indices[t * 3 + 1];
                var i2 = indices[t * 3 + 2];

                var v0 = verts[i0];
                var v1 = verts[i1];
                var v2 = verts[i2];

                var d1 = Vector3.Normalize(v1 - v0);
                var d2 = Vector3.Normalize(v2 - v0);
                var n = Vector3.Normalize(Vector3.Cross(d1, d2));

                normals[i0] += n;
                normals[i1] += n;
                normals[i2] += n;
                normalCount[i0]++;
                normalCount[i1]++;
                normalCount[i2]++;
            }

            for (int i = 0; i < numVerts; i++)
            {
                if (normalCount[i] > 0)
                    normals[i] = Vector3.Normalize(normals[i] / normalCount[i]);
                else
                    normals[i] = Vector3.UnitY;
            }

            for (int i = 0; i < numVerts; i++)
            {
                var pos = new Vector4(verts[i], 1.0f);
                var normal = normals[i];
                var offset = i * stride;

                BufferUtil.WriteVector4(vdat, offset, ref pos);
                BufferUtil.WriteVector3(vdat, offset + 16, ref normal);
                BufferUtil.WriteUint(vdat, offset + 28, colour);
            }

            var mesh = new Mesh
            {
                Name = occluder.Name,
                FilePack = owner.FilePack,
                VertexLayout = vlayout,
                VertexStride = stride,
                VertexCount = numVerts,
                VertexData = vdat,
                Indices = [.. indices],
                BoundingBox = BoundingBox.FromPoints([.. verts])
            };

            mesh.EnsureBVH();
            mesh.SetDefaultShader();

            Data = occluder;
            Name = occluder.Name;
            PartOwner = owner;
            FilePack = owner.FilePack;
            PartShape = EditablePartShape.Mesh;
            PartMesh = mesh;
            UpdateBounds();
        }
    }

    public class OccluderFile //city.occluder
    {
        public int Version { get; set; }
        public List<MCLAOccluder> Occluders { get; set; } = [];
    }

    public class MCLAOccluder
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public Vector3 BoundingCenter { get; set; }
        public float BoundingRadius { get; set; }

        public List<Vector3> Vertices { get; set; } = [];
        public List<Vector2Int> Edges { get; set; } = [];
        public List<MCLAOccluderPolygon> Polygons { get; set; } = [];

        public override string ToString() => $"Name: {Name} - Type: {Type}";
    }

    public struct Vector2Int(int x, int y)
    {
        public int X = x;
        public int Y = y;

        public override readonly string ToString() => $"({X}, {Y})";
    }

    public class MCLAOccluderPolygon
    {
        public List<int> Vertices { get; set; } = [];
        public List<int> Edges { get; set; } = [];
    }

    public class MCLAMapFileCache(Rpf3FileManager fman) : StreamingCache
    {
        public Rpf3FileManager FileManager = fman;
        private readonly Dictionary<Rpf3ResourceType, StreamingCacheDict<JenkHash>> Cache = new();

        public Dictionary<JenkHash, StreamingCacheEntry> GetCache(Rpf3ResourceType ext)
        {
            if (!Cache.TryGetValue(ext, out var cache))
            {
                cache = new StreamingCacheDict<JenkHash>(this);
                Cache[ext] = cache;
            }
            return cache;
        }

        public override void Invalidate(string gamepath)
        {
            if (string.IsNullOrEmpty(gamepath)) return;
            Rpf3FileManager.GetRpf3FileHashExt(gamepath, out var hash, out var ext);
            Cache.TryGetValue(ext, out var cache);
            cache?.Remove(hash);
        }

        public override void BeginFrame()
        {
            base.BeginFrame();
            foreach (var cache in Cache)
            {
                cache.Value.RemoveOldItems();
            }
        }

        public PiecePack GetPiecePack(Rpf3FileManager fm, JenkHash hash, Rpf3ResourceType ext)
        {
            var cache = GetCache(ext);
            if (!cache.TryGetValue(hash, out var cacheItem))
            {
                cacheItem = new StreamingCacheEntry();
                var entry = FileManager.DataFileMgr.TryGetStreamEntry(hash, ext);

                if (entry != null)
                {
                    Core.Engine.Console.Write("MCLAMap", entry.Name);
                    try
                    {
                        var piecePack = FileManager.LoadPiecePack(entry, null, false);
                        cacheItem.Object = piecePack;
                    }
                    catch { }
                }
            }
            else
            {
                if (cacheItem.Object is XcsFile xcs && !xcs.DependenciesLoaded)
                {
                    fm.LoadDependencies(xcs);
                    xcs.DependenciesLoaded = true;
                }
                else if (cacheItem.Object is XmdFile xmd)
                {
                    fm.LoadDependencies(xmd);
                }
            }

            cacheItem.LastUseFrame = CurrentFrame;
            cache[hash] = cacheItem;
            return cacheItem.Object as PiecePack;
        }
    }
}