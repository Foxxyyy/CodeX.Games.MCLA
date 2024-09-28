using CodeX.Core.Engine;
using CodeX.Core.Numerics;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.Files;
using CodeX.Games.MCLA.RPF3;
using System.Collections.Generic;
using System.Numerics;

namespace CodeX.Games.MCLA
{
    public class MCLAMap : StreamingLevel<MCLAGame, MCLAMapFileCache, Rpf3FileManager>
    {
        public List<Rpf3Entry> CityFiles;
        public Dictionary<JenkHash, MCLAMapNode> MapNodeDict;
        public StreamingSet<MCLAMapNode> StreamNodes;

        public static readonly Setting EnabledSetting = Settings.Register("MCLAMap.Enabled", SettingType.Bool, true, true);
        public static readonly Setting StartPositionSetting = Settings.Register("MCLAMap.StartPosition", SettingType.Vector3, new Vector3(150.0f, -350.0f, 30.0f));

        public Statistic NodeCountStat = Statistics.Register("MCLAMap.NodeCount", StatisticType.Counter);
        public Statistic EntityCountStat = Statistics.Register("MCLAMap.EntityCount", StatisticType.Counter);

        public MCLAMap(MCLAGame game) : base(game, "MCLA Map Level")
        {
            this.Game = game;
            this.DefaultSpawnPoint = StartPositionSetting.GetVector3();
            this.BoundingBox = new BoundingBox(new Vector3(-100000.0f), new Vector3(100000.0f));
            this.InitPhysicsSim();
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
            this.Cache = new MCLAMapFileCache(this.FileManager);
            this.StreamNodes = new StreamingSet<MCLAMapNode>();
            this.StreamPosition = this.DefaultSpawnPoint;
            this.MapNodeDict = new Dictionary<JenkHash, MCLAMapNode>();

            dfm.LoadCityFiles(this.Cache);

            foreach (var kv in dfm.XshpFiles)
            {
                var node = new MCLAMapNode(kv.Value);
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
            if (EnabledSetting.GetBool() == false) return false;

            this.StreamNodes.BeginIteration();

            var nodes = this.StreamNodes.CurrentSet;
            var ents = this.StreamEntities.CurrentSet;
            var spos = this.StreamPosition;

            this.StreamBVH.BeginIteration();
            this.StreamBVH.AddStreamPosition(spos);

            foreach (var node in this.StreamBVH.StreamItems.Cast<MCLAMapNode>())
            {
                var n = node;
                if (n.NameHash == 0) continue;
                if (n.Enabled == false) continue;

                while (!nodes.Contains(n))
                {
                    nodes.Add(n);
                }
            }

            foreach (var node in nodes) //Find current entities for max lod level
            {
                var mapdata = node.MapData;
                if (mapdata?.RootEntities != null)
                {
                    foreach (var e in mapdata.RootEntities)
                    {
                        RecurseAddStreamEntity(e, ref spos, ents);
                    }
                }
            }

            var needsAnotherUpdate = AddExtraStreamEntities();
            foreach (var ent in ents.ToList()) //Make sure all current entities assets are loaded
            {
                var upd = ent.Piece == null;
                var pp = Cache.GetPiecePack(new JenkHash(ent.Level.Name), Rpf3ResourceType.BitMap);
                ent.Piece = pp?.Piece;

                if (upd && (ent.Piece != null))
                {
                    ent.EnsurePieceLightInstances();
                    ent.UpdateBounds();
                }
            }

            this.NodeCountStat.SetCounter(nodes.Count);
            this.EntityCountStat.SetCounter(ents.Count);

            this.StreamNodes.EndIteration();
            if (needsAnotherUpdate) this.StreamUpdateRequest = true;

            return true;
        }

        private static void RecurseAddStreamEntity(Entity e, ref Vector3 spos, HashSet<Entity> ents)
        {
            e.StreamingDistance = (e.Position - spos).Length();
            if (e.BoundingBox.Contains(ref spos) == ContainmentType.Contains)
            {
                e.StreamingDistance = 0.0f;
            }

            if ((e.StreamingDistance < e.LodDistMax) && ((e.LodChildren == null) || (e.StreamingDistance >= e.LodDistMin)))
            {
                ents.Add(e);
            }
        }

        public override void GetEntities(SceneViewProjection proj)
        {
            base.GetEntities(proj);
            var isScreen = proj.View.Type == SceneViewType.Screen;
            var ents = proj.View.ViewEntities ?? StreamEntities.ActiveSet;

            if (ents != null)
            {
                foreach (var ent in ents)
                {
                    if (ent == null) continue;
                    if (ent.Piece == null)
                    {
                        ents.Remove(ent);
                        continue;
                    }
                    if (ent.BoundingSphere.Radius < proj.MinEntitySize) continue;
                    if (ent.BoundingBox.Minimum == ent.BoundingBox.Maximum) continue;

                    ent.CurrentDistance = ent.StreamingDistance;
                    if (proj.Frustum.ContainsAABB(ref ent.BoundingBox))
                    {
                        if (ent.Batch?.Lods != null)
                        {
                            //Add separate entities for the lod batches
                            for (int i = 0; i < ent.Batch.Lods.Length; i++)
                            {
                                var l = ent.Batch.Lods[i];
                                if (l?.RenderEntity != null)
                                {
                                    l.RenderEntity.CurrentDistance = ent.CurrentDistance;
                                    proj.Entities.Add(l.RenderEntity);
                                }
                            }
                        }
                        else proj.Entities.Add(ent);
                    }
                }
            }

            proj.SortEntities();
            if (isScreen)
            {
                UpdateStreamingPosition(proj.Params.Position);
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

        public MCLAMapNode(XshpFile xshp)
        {
            MapData = new MCLAMapData(xshp);
            NameHash = xshp.Hash;
            BoundingBox = MapData.BoundingBox;
            StreamingBox = MapData.StreamingBox;
            Enabled = true;
        }

        public void LoadMapData(XshpFile mapChild)
        {
            if (MapData != null)
            {
                UnloadMapData();
            }
            MapData ??= new MCLAMapData(mapChild);
        }

        public void UnloadMapData()
        {
            if (MapData != null)
            {
                foreach (var e in MapData.RootEntities)
                {
                    e.LodParent?.RemoveLodChild(e);
                }
                MapData.LodParent = null;
            }
            ParentNode = null;
        }

        public void SetParentNode(MCLAMapNode node)
        {
            if (ParentNode != node)
            {
                ParentNode = node;
                MapData?.SetParent(node?.MapData);
            }
        }

        public override string ToString()
        {
            return NameHash.ToString();
        }
    }

    public class MCLAMapData : Level
    {
        public MCLAMapData(XshpFile xshp)
        {
            FilePack = xshp;
            FilePack.EditorObject = this;
            Name = xshp.Name;

            var dist = new Vector3(800.0f);
            BoundingBox = xshp.Piece.BoundingBox;
            StreamingBox = new BoundingBox(BoundingBox.Center - dist, BoundingBox.Center + dist);

            var e = new Entity()
            {
                Position = BoundingBox.Center,
                LodLevel = 0,
                LodDistMax = 800.0f,
                Index = Entities.Count,
                Level = this
            };
            this.Add(e);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class MCLAMapFileCache : StreamingCache
    {
        public Rpf3FileManager FileManager;
        private readonly Dictionary<Rpf3ResourceType, StreamingCacheDict<JenkHash>> Cache = new();

        public MCLAMapFileCache(Rpf3FileManager fman)
        {
            FileManager = fman;
        }

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

        public PiecePack GetPiecePack(JenkHash hash, Rpf3ResourceType ext)
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
                        var piecePack = FileManager.LoadPiecePack(entry, null, true);
                        cacheItem.Object = piecePack;
                    }
                    catch { }
                }
            }
            cacheItem.LastUseFrame = CurrentFrame;
            cache[hash] = cacheItem;
            return cacheItem.Object as PiecePack;
        }

        /*public Piece GetPiece(BaseArchetype archetype)
        {
            if (archetype == null) return null;

            Piece piece = null;
            PiecePack pack = null;
            JenkHash assetName = JenkHash.GenHash(archetype.Name?.ToLowerInvariant());
            JenkHash textureDictionary = JenkHash.GenHash(archetype.TxdName?.ToLowerInvariant());
            JenkHash drawableDictionary = JenkHash.GenHash(archetype.LODModel?.ToLowerInvariant());
            var loaddeps = false;
            if ((drawableDictionary != 0) && (drawableDictionary != 0x3ADB3357)) //"null"
            {
                pack = GetPiecePack(drawableDictionary, Img3ResourceType.Model, out loaddeps);
                pack?.Pieces?.TryGetValue(assetName, out piece);
                loaddeps = true;
            }
            else
            {
                pack = GetPiecePack(assetName, Img3ResourceType.Model, out loaddeps);
                if (pack == null)
                {
                    pack = GetPiecePack(assetName, Img3ResourceType.ModelFrag, out loaddeps);
                }
                piece = pack?.Piece;
            }

            if (piece?.AllModels != null)
            {
                var txds = new List<TexturePack>();
                void addTxd(JenkHash h)
                {
                    var txd = GetTexturePack(h);
                    if (txd != null)
                    {
                        txds.Add(txd);
                    }
                }
                FileManager.GetTxds(textureDictionary, addTxd);

                if (loaddeps)
                {
                    FileManager.ApplyTextures(piece, txds);
                }

            }

            if (piece != null)
            {
                JenkHash ideName = archetype.Ide?.FileEntry?.ShortNameHash ?? 0;
                if (ideName != 0)//try load item collisions from wbd
                {
                    var wbd = GetPiecePack(ideName, Img3ResourceType.Bounds, out loaddeps);
                    if (wbd?.Pieces != null)
                    {
                        if (wbd.Pieces.TryGetValue(assetName, out var cpiece))
                        {
                            piece.Collider = cpiece.Collider;
                        }
                    }
                    if (piece.Collider == null)
                    {
                        //try to load colliders from other wbd files in the same archive as the ide name... (yuk!)
                        var entry = FileManager.DataFileMgr.TryGetStreamEntry(ideName, Img3ResourceType.Bounds);
                        if (entry?.Archive?.AllEntries != null)
                        {
                            foreach (Img3FileEntry sube in entry.Archive.AllEntries)
                            {
                                if (sube.ResourceType != Img3ResourceType.Bounds) continue;
                                if (sube.NameLower.EndsWith(".wbd") == false) continue;
                                if (sube.ShortNameHash == ideName) continue;

                                wbd = GetPiecePack(sube.ShortNameHash, Img3ResourceType.Bounds, out loaddeps);
                                if (wbd?.Pieces != null)
                                {
                                    if (wbd.Pieces.TryGetValue(assetName, out var cpiece))
                                    {
                                        piece.Collider = cpiece.Collider;
                                        break;
                                    }
                                    else
                                    { }
                                }
                                else
                                { }
                            }
                        }
                    }
                }
            }
            return piece;
        }*/
    }
}