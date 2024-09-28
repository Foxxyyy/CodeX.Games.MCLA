using CodeX.Core.Engine;
using CodeX.Core.Shaders;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.Files;
using CodeX.Games.MCLA.RSC5;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace CodeX.Games.MCLA.RPF3
{
    public class Rpf3FileManager : FileManager
    {
        public override string ArchiveTypeName => "RPF4";
        public override string ArchiveExtension => ".rpf";
        public Rpf3DataFileMgr DataFileMgr { get; set; }
        public Rpf3Store Store { get; set; }

        public Rpf3FileManager(MCLAGame game) : base(game)
        {
            Store = new Rpf3Store(this);
        }

        public override void InitFileTypes()
        {
            InitGenericFileTypes();
            InitFileType(".rpf", "Rage Package File", FileTypeIcon.Archive);
            InitFileType(".gxt2", "Global Text Table", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".sps", "Shader Preset", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".ugc", "User-Generated Content", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".pso", "Metadata (PSO)", FileTypeIcon.XmlFile, FileTypeAction.ViewXml);
            InitFileType(".xnd", "Path Nodes", FileTypeIcon.LinkFile, FileTypeAction.ViewModels);
            InitFileType(".xnv", "Nav Mesh", FileTypeIcon.SystemFile, FileTypeAction.ViewModels);
            InitFileType(".xvr", "Vehicle Record", FileTypeIcon.SystemFile, FileTypeAction.ViewModels);
            InitFileType(".fxc", "Compiled Shaders", FileTypeIcon.SystemFile, FileTypeAction.ViewHex);
            InitFileType(".xapb", "Ambient Ped", FileTypeIcon.Piece, FileTypeAction.ViewModels, true, true);
            InitFileType(".xft", "Fragment", FileTypeIcon.Piece, FileTypeAction.ViewModels, true, true);
            InitFileType(".cut", "Cutscene", FileTypeIcon.Level, FileTypeAction.ViewXml, true);
            InitFileType(".xtd", "Texture Dictionary", FileTypeIcon.Image, FileTypeAction.ViewTextures, true, true);
            InitFileType(".xcd", "Clip Dictionary", FileTypeIcon.Animation, FileTypeAction.ViewXml, true);
            InitFileType(".xpt", "Particle Effect", FileTypeIcon.Animation, FileTypeAction.ViewModels, true, true);
            InitFileType(".xbn", "Static Collisions", FileTypeIcon.Collisions, FileTypeAction.ViewModels, true);
            InitFileType(".xbd", "Collision Dictionary", FileTypeIcon.Collisions, FileTypeAction.ViewModels, true);
            InitFileType(".ide", "Item Definitions", FileTypeIcon.Library, FileTypeAction.ViewText);
            InitFileType(".ipl", "Item Placements", FileTypeIcon.Process, FileTypeAction.ViewText);
            InitFileType(".awc", "Audio Wave Container", FileTypeIcon.Audio, FileTypeAction.ViewAudio);
            InitFileType(".rel", "Audio Data (REL)", FileTypeIcon.AudioPlayback, FileTypeAction.ViewAudio, true);
            InitFileType(".nametable", "Name Table", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".xpdb", "Pose Matcher Database", FileTypeIcon.SystemFile, FileTypeAction.ViewXml, true);
            InitFileType(".sco", "Script", FileTypeIcon.Script, FileTypeAction.ViewHex, false);
            InitFileType(".xat", "Action Tree", FileTypeIcon.Animation, FileTypeAction.ViewHex, false);
            InitFileType(".xpfl", "Particle Effects Library", FileTypeIcon.Animation, FileTypeAction.ViewHex, false);
            InitFileType(".xsd", "XSD File", FileTypeIcon.Library, FileTypeAction.ViewXml, false);
            InitFileType(".xshp", "BitMap Texture", FileTypeIcon.Piece, FileTypeAction.ViewModels);

            InitFileType(".drawable", "Drawable", FileTypeIcon.Piece, FileTypeAction.ViewModels); //Custom extension until we get the actual filenames
            InitFileType(".ppp", "Post-Processing Pipeline", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".mccp", "Midnight Club Checkpoint", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".tune", "TUNE File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".ped", "PED File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".list", "LIST File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".lst", "LIST File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".type", "TYPE File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".mcform", "MCFORM File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".mcuiclass", "MCUICLASS File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".uilogic", "UILOGIC File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".hudmap", "HUDMAP File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".mesh", "MESH File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".map", "MAP File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".maps", "MAPS File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".grid", "GRID File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".aogrid", "AOGRID File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
            InitFileType(".career", "CAREER File", FileTypeIcon.TextFile, FileTypeAction.ViewText, false);
        }

        public override void InitCreateInfos()
        {

        }

        public override bool Init()
        {
            JenkIndex.LoadStringsFile("MCLA");
            LoadStartupCache();
            return true;
        }

        public override void InitArchives(string[] files)
        {
            foreach (var path in files)
            {
                var relpath = path.Replace(Folder + "\\", "");
                var filepathl = path.ToLowerInvariant();
                var isFile = File.Exists(path);
                Core.Engine.Console.Write("Rfp3FileManager", Game.GamePathPrefix + relpath + "...");

                if (isFile)
                {
                    if (IsArchive(filepathl))
                    {
                        var archive = GetArchive(path, relpath);
                        if (archive?.AllEntries == null)
                            continue;

                        RootArchives.Add(archive);
                        var queue = new Queue<GameArchive>();
                        queue.Enqueue(archive);

                        while (queue.Count > 0)
                        {
                            var a = queue.Dequeue();
                            if (a.Children != null)
                            {
                                foreach (var ca in a.Children)
                                {
                                    queue.Enqueue(ca);
                                }
                            }
                            AllArchives.Add(a);
                        }
                    }
                }
            }
        }

        public override void InitArchivesComplete()
        {
            foreach (var archive in AllArchives)
            {
                if (archive.AllEntries != null)
                {
                    ArchiveDict[archive.Path] = archive;
                    foreach (var entry in archive.AllEntries)
                    {
                        if (entry is Rpf3FileEntry fe)
                        {
                            EntryDict[fe.Path] = fe;
                            JenkIndex.Ensure(fe.ShortNameLower, "MCLA");
                        }
                    }
                }
            }

            InitGameFiles();
            if (StartupCacheDirty)
            {
                SaveStartupCache();
            }
        }

        private void InitGameFiles()
        {
            Core.Engine.Console.Write("MCLA.InitGameFiles", "Initialising MCLA...");
            DataFileMgr ??= new Rpf3DataFileMgr(this);
            DataFileMgr.Init();
            Core.Engine.Console.Write("MCLA.InitGameFiles", "MCLA Initialised.");
        }

        public override void SaveStartupCache()
        {
            var file = StartupUtil.GetFilePath("CodeX.Games.MCLA.startup.dat");
            var strfile = StartupUtil.GetFilePath("CodeX.Games.MCLA.strings.txt");
            var strtime = 0L;

            if (File.Exists(strfile))
            {
                strtime = File.GetLastWriteTime(strfile).ToBinary();
            }

            Core.Engine.Console.Write("Rpf3FileManager", "Building MCLA startup cache");

            using var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            bw.Write(strtime);

            Store.SaveStartupCache(bw);

            var buf = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(buf, 0, buf.Length);

            File.WriteAllBytes(file, buf);
        }

        public override void LoadStartupCache()
        {
            var file = StartupUtil.GetFilePath("CodeX.Games.MCLA.startup.dat");
            if (File.Exists(file) == false)
            {
                StartupCacheDirty = true;
                return;
            }

            var strfile = StartupUtil.GetFilePath("CodeX.Games.MCLA.strings.txt");
            var strtime = 0L;
            if (File.Exists(strfile))
            {
                strtime = File.GetLastWriteTime(strfile).ToBinary();
            }

            Core.Engine.Console.Write("Rpf3FileManager", "Loading MCLA startup cache...");

            var cmpbuf = File.ReadAllBytes(file);
            using var ms = new MemoryStream(cmpbuf);
            var br = new BinaryReader(ms);
            var strtimet = br.ReadInt64();

            if (strtimet != strtime)
            {
                StartupCacheDirty = true; //strings file mismatch, rebuild the startup cache.
                return;
            }
            Store.LoadStartupCache(br);
        }

        public override bool IsArchive(string filename)
        {
            return filename.EndsWith(".rpf");
        }

        public override GameArchive GetArchive(string path, string relpath)
        {
            if ((StartupCache != null) && (StartupCache.TryGetValue(path, out GameArchive archive)))
            {
                return archive;
            }
            var rpf = new Rpf3File(path, relpath);
            rpf.ReadStructure();
            return rpf;
        }

        public override GameArchive CreateArchive(string gamefolder, string relpath)
        {
            return Rpf3File.CreateNew(gamefolder, relpath);
        }

        public override GameArchive CreateArchive(GameArchiveDirectory dir, string name)
        {
            throw new Exception("Cannot create archive in another");
        }

        public override GameArchiveFileInfo CreateFile(GameArchiveDirectory dir, string name, byte[] data, bool overwrite = true)
        {
            return Rpf3File.CreateFile(dir as Rpf3DirectoryEntry, name, data, overwrite);
        }

        public override GameArchiveDirectory CreateDirectory(GameArchiveDirectory dir, string name)
        {
            return Rpf3File.CreateDirectory(dir as Rpf3DirectoryEntry, name);
        }

        public override GameArchiveFileInfo CreateFileEntry(string name, string path, ref byte[] data)
        {
            return null;
        }

        public override void RenameArchive(GameArchive file, string newname)
        {
            Rpf3File.RenameArchive(file as Rpf3File, newname);
        }

        public override void RenameEntry(GameArchiveEntry entry, string newname)
        {
            Rpf3File.RenameEntry(entry as Rpf3Entry, newname);
        }

        public override void DeleteEntry(GameArchiveEntry entry)
        {
            Rpf3File.DeleteEntry(entry as Rpf3Entry);
        }

        public override void Defragment(GameArchive file, Action<string, float> progress = null)
        {
            
        }

        public override string ConvertToXml(GameArchiveFileInfo file, byte[] data, out string newfilename, out object infoObject, string folder = "")
        {
            infoObject = null;
            var fileext = Path.GetExtension(file.Name).ToLowerInvariant();

            switch (fileext)
            {
                case ".xsd":
                case ".xml":
                case ".meta":
                    newfilename = file.Name;
                    return TextUtil.GetUTF8Text(data);
            }

            var fmtext = "";
            if (file is Rpf3FileEntry fe)
            {
                //TODO: determine actual metadata file format!
                fmtext = ".pso";
            }
            newfilename = file.Name + fmtext + ".xml";
            return string.Empty;
        }

        public override byte[] ConvertFromXml(string xml, string filename, string folder = "")
        {
            return null;
        }

        public override string GetXmlFormatName(string filename, out int trimlength)
        {
            trimlength = 4;
            var str1 = filename.Substring(0, filename.Length - trimlength);
            var idx = str1.LastIndexOf('.');
            if (idx < 0)
            {
                return "RSC XML";
            }
            trimlength += str1.Length - idx;
            return str1[(idx + 1)..].ToUpperInvariant() + " XML";
        }

        public override string ConvertToText(GameArchiveFileInfo file, byte[] data, out string newfilename)
        {
            newfilename = file.Name;
            return TextUtil.GetUTF8Text(data);
        }

        public override byte[] ConvertFromText(string text, string filename)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        public override TexturePack LoadTexturePack(GameArchiveFileInfo file, byte[] data = null)
        {
            data = EnsureFileData(file, data);

            if (data == null)
                return null;
            if (file is not Rpf3FileEntry entry)
                return null;

            if (file.NameLower.EndsWith(".xtd"))
            {
                var xtd = new XtdFile(entry);
                xtd.Load(data);
                return xtd;
            }
            return null;
        }

        public override PiecePack LoadPiecePack(GameArchiveFileInfo file, byte[] data = null, bool loadDependencies = false)
        {
            data = EnsureFileData(file, data);
            if (data == null)
                return null;
            if (file is not Rpf3FileEntry entry)
                return null;

            XapbFile.Textures?.Clear();
            if (entry.NameLower.EndsWith(".xapb"))
            {
                var xapb = new XapbFile(entry);
                xapb.Load(data);
                return xapb;
            }
            else if (entry.NameLower.EndsWith(".xft"))
            {
                var xft = new XftFile(entry);
                xft.Load(data);
                return xft;
            }
            else if (entry.NameLower.EndsWith(".xshp"))
            {
                var xshp = new XshpFile(entry);
                xshp.Load(data);
                if (loadDependencies) LoadDependencies(xshp);
                return xshp;
            }
            else if (entry.NameLower.EndsWith(".drawable"))
            {
                var drawable = new DrawableFile(entry);
                drawable.Load(data);
                return drawable;
            }
            return null;
        }

        private void LoadDependencies(PiecePack pack)
        {
            if (pack?.Pieces == null) return;

            var fm = Game.GetFileManager() as Rpf3FileManager;
            var dfm = fm?.DataFileMgr;
            if (dfm == null) return;

            if (pack is XshpFile xshp && xshp.City != null)
            {
                //Cache texture data for faster lookups
                var seperatedTextures = new Dictionary<string, Texture>();
                var cutItems = dfm.StreamEntries[Rpf3ResourceType.Drawable];
                var xshpCity = dfm.StreamEntries[Rpf3ResourceType.BitMap];

                //Gather separated textures
                foreach (var c in cutItems)
                {
                    var pp = LoadPiecePack(c.Value);
                    if (pp?.FileInfo?.Parent?.NameLower != "cutsceneitems") continue;

                    foreach (var piece in pp.Pieces.Values)
                    {
                        if (piece?.TexturePack?.Textures == null) continue;
                        foreach (var tex in piece.TexturePack.Textures.Values)
                        {
                            if (tex != null)
                            {
                                seperatedTextures[tex.Name.ToLowerInvariant()] = tex;
                            }
                        }
                    }
                }

                if (seperatedTextures.Count == 0) return;

                //Process each piece (ensure piece texture updates are sequential to prevent issues)
                foreach (var kvp in pack.Pieces)
                {
                    var piece = kvp.Value;
                    if (piece?.AllModels == null) continue;

                    //Process models and meshes
                    foreach (var mesh in piece.AllModels.SelectMany(p => p.Meshes))
                    {
                        if (mesh?.Textures == null) continue;

                        for (int i = 0; i < mesh.Textures.Length; i++)
                        {
                            var texture = mesh.Textures[i];
                            if (texture == null || texture?.Data != null) continue; //Skip if null or already loaded

                            var texName = texture.Name.ToLowerInvariant();
                            var foundTexture = false;

                            //First, try to find in 'cutsceneitems' textures
                            if (seperatedTextures.TryGetValue(texName, out var tex))
                            {
                                //Lock for thread safety when updating textures
                                lock (mesh.Textures)
                                {
                                    mesh.Textures[i] = tex;
                                }
                                UpdateShaderTextures(piece, tex);
                                foundTexture = true;
                            }
                            else
                            {
                                //If not found, search in TexturesCity
                                foreach (var item in fm.Store.TexturesCity)
                                {
                                    if (foundTexture) break;
                                    if (item.Texture != (texName + ".dds")) continue;

                                    foreach (var xshpEntry in xshpCity.Values)
                                    {
                                        if (foundTexture) break;
                                        if (xshpEntry.PathLower != item.FileLocation) continue;

                                        var pp = LoadPiecePack(xshpEntry);
                                        if (pp == null) continue;

                                        foreach (var xshpPiece in pp.Pieces)
                                        {
                                            if (xshpPiece.Value.TexturePack.Textures.TryGetValue(texName + ".dds", out var xshpTex))
                                            {
                                                lock (mesh.Textures)
                                                {
                                                    mesh.Textures[i] = xshpTex;
                                                }
                                                UpdateShaderTextures(piece, xshpTex);
                                                foundTexture = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            //Break early if texture is updated
                            if (foundTexture) break;
                        }
                    }
                }
            }
        }

        private void UpdateShaderTextures(Piece piece, Texture tex) //Helper method to update shader textures
        {
            if (piece is Rsc5SimpleDrawableBase drawable)
            {
                foreach (var shader in drawable.ShaderGroup.Item?.Shaders.Items ?? Enumerable.Empty<Rsc5Shader>())
                {
                    foreach (var param in shader?.Params ?? Enumerable.Empty<Rsc5ShaderParameter>())
                    {
                        if (param.Type != 0 || param.Texture == null) continue;
                        if (!tex.Name.Contains(param.Texture.Name.ToLowerInvariant())) continue;
                        param.Texture.Width = tex.Width;
                        param.Texture.Height = tex.Height;
                        param.Texture.MipLevels = tex.MipLevels;
                        param.Texture.Format = tex.Format;
                        param.Texture.Pack = tex.Pack;
                        break;
                    }
                }
            }
        }

        public override AudioPack LoadAudioPack(GameArchiveFileInfo file, byte[] data = null)
        {
            throw new NotImplementedException();
        }

        public override T LoadMetaNode<T>(GameArchiveFileInfo file, byte[] data = null)
        {
            throw new NotImplementedException();
        }

        public override DataBagPack LoadDataBagPack(GameArchiveFileInfo file, byte[] data = null)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rpf3ResourceType GetRpf3FileExt(string filename)
        {
            var extstr = Path.GetExtension(filename).Replace(".", "").ToLowerInvariant();
            Enum.TryParse<Rpf3ResourceType>(extstr, out var ext);
            return ext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JenkHash GetRpf3FileHash(string filename)
        {
            return new JenkHash(Path.GetFileNameWithoutExtension(filename).ToLowerInvariant());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetRpf3FileHashExt(string filename, out JenkHash hash, out Rpf3ResourceType ext)
        {
            hash = GetRpf3FileHash(filename);
            ext = GetRpf3FileExt(filename);
        }
    }

    public class Rpf3DataFileMgr
    {
        public Rpf3FileManager FileManager;
        public Dictionary<string, Rpf3DataFileDevice> Devices;
        public Dictionary<Rpf3ResourceType, Dictionary<JenkHash, Rpf3FileEntry>> StreamEntries;
        public Dictionary<JenkHash, XshpFile> XshpFiles;

        public Rpf3DataFileMgr(Rpf3FileManager fman)
        {
            this.FileManager = fman;
        }

        public void Init()
        {
            if (this.StreamEntries != null) return;

            this.Devices = new Dictionary<string, Rpf3DataFileDevice>();
            this.StreamEntries = new Dictionary<Rpf3ResourceType, Dictionary<JenkHash, Rpf3FileEntry>>();
            this.XshpFiles = new Dictionary<JenkHash, XshpFile>();
            this.LoadFiles();
        }

        private void LoadFiles()
        {
            foreach (var archive in this.FileManager.AllArchives)
            {
                if (archive.Path.StartsWith("backup")) continue;
                foreach (var file in archive.AllEntries)
                {
                    if (file is not Rpf3FileEntry fe) continue;
                    if (!fe.IsResource) continue;

                    var hash = fe.ShortNameHash;
                    if (!this.StreamEntries.TryGetValue(fe.ResourceType, out var entries))
                    {
                        entries = new Dictionary<JenkHash, Rpf3FileEntry>();
                        this.StreamEntries[fe.ResourceType] = entries;
                    }
                    entries[hash] = fe;
                }
            }
        }

        public void LoadCityFiles(MCLAMapFileCache mapCache)
        {
            var cache = mapCache.GetCache(Rpf3ResourceType.BitMap);
            foreach (var se in this.StreamEntries[Rpf3ResourceType.BitMap])
            {
                var fe = se.Value;
                if (fe.Parent.Name != "sc") continue;

                var xshpData = fe.Archive.ExtractFile(fe);
                var ident = (Rsc5XshpType)Rpf3Crypto.Swap(BitConverter.ToUInt32(xshpData, 0));

                if (ident == Rsc5XshpType.CITY && xshpData != null)
                {
                    Core.Engine.Console.Write("Rpf3FileManager", fe.Name);
                    var cacheItem = new StreamingCacheEntry();
                    var piecePack = FileManager.LoadPiecePack(fe, xshpData);

                    cacheItem.Object = piecePack;
                    cacheItem.LastUseFrame = mapCache.CurrentFrame;
                    cache[fe.NameHash] = cacheItem;

                    var hash = fe.ShortNameHash;
                    JenkIndex.Ensure(((XshpFile)piecePack).Name, "MCLA");
                    this.XshpFiles[hash] = (XshpFile)piecePack;
                }
            }
        }

        public Rpf3FileEntry TryGetStreamEntry(JenkHash hash, Rpf3ResourceType ext)
        {
            if (this.StreamEntries.TryGetValue(ext, out var entries))
            {
                if (entries.TryGetValue(hash, out var entry))
                {
                    return entry;
                }
            }
            return null;
        }
    }

    public class Rpf3DataFileDevice
    {
        public Rpf3DataFileMgr DataFileMgr;
        public Rpf3FileManager FileManager;
        public string Name;
        public string PhysicalPath;

        public Rpf3DataFileDevice(Rpf3DataFileMgr dfm, string name, string path)
        {
            DataFileMgr = dfm;
            FileManager = dfm.FileManager;
            Name = name;
            PhysicalPath = FileManager.Folder + "\\" + path;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Rpf3Store
    {
        public Rpf3FileManager FileMan;
        public List<Rpf3TextureStoreItem> TexturesCity;

        public Rpf3Store(Rpf3FileManager fileman)
        {
            FileMan = fileman;
        }

        public void SaveStartupCache(BinaryWriter bw)
        {
            var bmp = FileMan.DataFileMgr.StreamEntries[Rpf3ResourceType.BitMap];
            var textureItemsSet = new HashSet<Rpf3TextureStoreItem>();

            Core.Engine.Console.Write("Rpf3FileManager", "Building MCLA startup cache");
            Parallel.ForEach(bmp, kv =>
            {
                var value = kv.Value;
                if (value == null || value.Parent.Name != "sc") return;
                if (value.Name == "0x5674B600.xshp") return; //Not thread-safe

                var pack = (XshpFile)FileMan.LoadPiecePack(value);
                if (pack != null)
                {
                    var localTextureItems = new ConcurrentBag<Rpf3TextureStoreItem>();
                    foreach (var p in pack.Pieces.Values)
                    {
                        if (p == null || p.TexturePack?.Textures == null) continue;
                        foreach (var tex in p.TexturePack.Textures)
                        {
                            var item = new Rpf3TextureStoreItem()
                            {
                                Texture = tex.Key,
                                FileLocation = value.PathLower
                            };
                            localTextureItems.Add(item);
                        }
                    }

                    lock (textureItemsSet)
                    {
                        foreach (var item in localTextureItems)
                        {
                            textureItemsSet.Add(item);
                        }
                    }
                }
            });
            var textureItemsList = textureItemsSet.ToList();
            SerializeItems(bw, textureItemsList);
        }

        public void LoadStartupCache(BinaryReader br)
        {
            TexturesCity = new List<Rpf3TextureStoreItem>();
            DeserializeItems(br, TexturesCity);
        }

        public static void SerializeItems(BinaryWriter bw, List<Rpf3TextureStoreItem> list)
        {
            bw.Write(list.Count);
            foreach (var item in list)
            {
                bw.WriteStringNullTerminated(item.Texture);
                bw.WriteStringNullTerminated(item.FileLocation);
            }
        }

        public static void DeserializeItems(BinaryReader br, List<Rpf3TextureStoreItem> dict)
        {
            var itemCount = br.ReadInt32();
            for (int i = 0; i < itemCount; i++)
            {
                var item = new Rpf3TextureStoreItem
                {
                    Texture = br.ReadStringNullTerminated(),
                    FileLocation = br.ReadStringNullTerminated()
                };
                dict.Add(item);
            }
        }
    }

    public struct Rpf3TextureStoreItem
    {
        public string Texture;
        public string FileLocation;

        public override string ToString()
        {
            return $"{Texture} : {FileLocation}";
        }
    }
}