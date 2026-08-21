using CodeX.Core.Engine;
using CodeX.Core.Numerics;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.Files;
using CodeX.Games.MCLA.RSC5;
using SharpDX.Direct3D11;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
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

        private readonly ConcurrentDictionary<string, TexturePack> TexturePackCache = new();
        private readonly ConcurrentDictionary<string, PiecePack> PiecePackCache = new();

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
            InitFileType(".mtl", "Material", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".xnd", "Path Nodes", FileTypeIcon.LinkFile, FileTypeAction.ViewModels);
            InitFileType(".xnv", "Nav Mesh", FileTypeIcon.SystemFile, FileTypeAction.ViewModels);
            InitFileType(".xvr", "Vehicle Record", FileTypeIcon.SystemFile, FileTypeAction.ViewModels);
            InitFileType(".fxc", "Compiled Shaders", FileTypeIcon.SystemFile);
            InitFileType(".xapb", "Ambient Ped Body", FileTypeIcon.Piece, FileTypeAction.ViewModels);
            InitFileType(".xaph", "Ambient Ped Hierarchy", FileTypeIcon.File);
            InitFileType(".xdr", "Drawable", FileTypeIcon.Piece, FileTypeAction.ViewModels);
            InitFileType(".xft", "Fragment", FileTypeIcon.Piece, FileTypeAction.ViewModels);
            InitFileType(".cut", "Cutscene", FileTypeIcon.Level, FileTypeAction.ViewXml);
            InitFileType(".xtd", "Texture Dictionary", FileTypeIcon.Image, FileTypeAction.ViewTextures);
            InitFileType(".xcd", "Clip Dictionary", FileTypeIcon.Animation, FileTypeAction.ViewXml);
            InitFileType(".xpt", "Particle Effect", FileTypeIcon.Animation, FileTypeAction.ViewModels);
            InitFileType(".xbn", "Static Collisions", FileTypeIcon.Collisions, FileTypeAction.ViewModels);
            InitFileType(".xbd", "Collision Dictionary", FileTypeIcon.Collisions, FileTypeAction.ViewModels);
            InitFileType(".ide", "Item Definitions", FileTypeIcon.Library, FileTypeAction.ViewText);
            InitFileType(".ipl", "Item Placements", FileTypeIcon.Process, FileTypeAction.ViewText);
            InitFileType(".awc", "Audio Wave Container", FileTypeIcon.Audio, FileTypeAction.ViewAudio);
            InitFileType(".rel", "Audio Data (REL)", FileTypeIcon.AudioPlayback, FileTypeAction.ViewAudio);
            InitFileType(".nametable", "Name Table", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".xpdb", "Pose Matcher Database", FileTypeIcon.SystemFile, FileTypeAction.ViewXml);
            InitFileType(".sco", "Script", FileTypeIcon.Script, FileTypeAction.ViewText);
            InitFileType(".xat", "Action Tree", FileTypeIcon.Animation);
            InitFileType(".xpfl", "Particle Effects Library", FileTypeIcon.Animation);
            InitFileType(".xsd", "XSD File", FileTypeIcon.Library, FileTypeAction.ViewXml);
            InitFileType(".xshp", "Car Vinyl Shape", FileTypeIcon.Image, FileTypeAction.ViewTextures);
            InitFileType(".xsf", "Flash UI", FileTypeIcon.Image, FileTypeAction.ViewTextures);
            InitFileType(".xrsc", "Model Resource", FileTypeIcon.Piece, FileTypeAction.ViewModels);
            InitFileType(".xtp", "Vehicle Part Textures", FileTypeIcon.Image, FileTypeAction.ViewTextures);
            InitFileType(".xtl", "Damage Textures", FileTypeIcon.Image, FileTypeAction.ViewTextures);
            InitFileType(".xspm", "Streaming Pack Map", FileTypeIcon.File);
            InitFileType(".xct", "City File/ Car Tuning", FileTypeIcon.File);
            InitFileType(".xcc", "Car Config", FileTypeIcon.XmlFile, FileTypeAction.ViewXml);
            InitFileType(".dds", "DirectDraw Surface", FileTypeIcon.Image, FileTypeAction.ViewTextures);
            InitFileType(".xcs", "City Sector", FileTypeIcon.Piece, FileTypeAction.ViewModels);
            InitFileType(".xapk", "Animation Pack", FileTypeIcon.Animation, FileTypeAction.ViewModels);
            InitFileType(".xov", "Overlay", FileTypeIcon.File);
            InitFileType(".xwt", "Wheel Texture", FileTypeIcon.Image, FileTypeAction.ViewTextures);
            InitFileType(".xlm", "Light Manager", FileTypeIcon.File);
            InitFileType(".xck", "Prop Instance Chunk", FileTypeIcon.File);
            InitFileType(".xmp", "MultiProp Definition", FileTypeIcon.File);
            InitFileType(".xmd", "Map District LOD", FileTypeIcon.Piece, FileTypeAction.ViewModels);
            InitFileType(".ppp", "Post-Processing Pipeline", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".mccp", "Midnight Club Checkpoint", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".tune", "TUNE File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".ped", "PED File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".list", "LIST File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".lst", "LIST File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".type", "TYPE File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".mcform", "MCFORM File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".mcuiclass", "MCUICLASS File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".uilogic", "UILOGIC File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".hudmap", "HUDMAP File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".mesh", "MESH File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".map", "MAP File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".maps", "MAPS File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".grid", "GRID File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".aogrid", "AOGRID File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".career", "CAREER File", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".sharetex", "Shared Texture List", FileTypeIcon.XmlFile, FileTypeAction.ViewXml);
            InitFileType(".mcpowerup", "Powerup Definition", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".dcl", "Shader Declaration", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".skel", "Skeleton Definition", FileTypeIcon.TextFile, FileTypeAction.ViewText);
            InitFileType(".odr", "Drawable Definition", FileTypeIcon.TextFile, FileTypeAction.ViewText);
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
            LoadStartupCache(); //Load cache to access textures
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

        public override void Defragment(GameArchive file, Action<string, float> progress = null, bool recursive = true)
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
                case ".xcc":
                    return ConvertToXml<XccFile>(file, data, out newfilename, "MCLACarConfig");
            }

            newfilename = file.Name + ".xml";
            var pack = LoadDataBagPack(file, data);

            if (pack != null)
            {
                infoObject = pack;
                return pack.Bag.ToXml();
            }
            return "Sorry, CodeX currently cannot convert this file to XML.";
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
            if (file == null)
            {
                newfilename = "External File";
                return string.Empty;
            }
            newfilename = file.Name;

            //Animation packs are resource type 1 (Generic), which shares its type with several
            //other formats, so an entry whose name never got resolved arrives without an
            //extension. mcAnimPack always starts with the same tag, so check the bytes too.
            if (file.Name.EndsWith(".xapk", StringComparison.OrdinalIgnoreCase) || LooksLikeAnimPack(data))
            {
                var pack = new XapkFile(file);
                pack.Load(data);
                newfilename = file.Name + ".txt";
                return pack.ToText();
            }

            if (file.Name.EndsWith(".sco", StringComparison.OrdinalIgnoreCase))
            {
                var sco = new ScoFile(file);
                sco.Load(data);
                newfilename = file.Name + ".txt";
                return sco.ToText();
            }

            return TextUtil.GetUTF8Text(data);
        }

        //mcAnimPack always starts with this tag.
        private static bool LooksLikeAnimPack(byte[] data)
        {
            return data != null && data.Length > 4
                && data[0] == 0x9C && data[1] == 0x55 && data[2] == 0x44 && data[3] == 0x00;
        }

        //LoadPiecePack runs before the preview window exists, so wait for the PreviewForm holding
        //this pack to appear and put the animation window on top of it.
        private void ShowAnimationWindowFor(XapkFile pack)
        {
            var timer = new System.Windows.Forms.Timer { Interval = 150 };
            var tries = 0;
            timer.Tick += (s, e) =>
            {
                if (++tries > 40) { timer.Stop(); timer.Dispose(); return; }
                foreach (System.Windows.Forms.Form form in System.Windows.Forms.Application.OpenForms)
                {
                    if (form is not CodeX.Forms.Explorer.PreviewForm preview) continue;
                    if (!ReferenceEquals(preview.PiecePack, pack)) continue;
                    timer.Stop();
                    timer.Dispose();
                    var win = new MclaAnimationForm(preview, this);
                    win.Show(preview);
                    win.Location = new System.Drawing.Point(preview.Right - win.Width - 20, preview.Top + 40);
                    win.LoadPack(pack);
                    return;
                }
            };
            timer.Start();
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

            if (file.NameLower.EndsWith(".dds"))
            {
                var dds = new DdsFile(entry);
                dds.Load(data);
                return dds;
            }
            else if (file.NameLower.EndsWith(".xsf"))
            {
                var xsf = new XsfFile(entry);
                xsf.Load(data);
                return xsf;
            }
            else if (file.NameLower.EndsWith(".xtd"))
            {
                var xtd = new XtdFile(entry);
                xtd.Load(data);
                return xtd;
            }
            else if (file.NameLower.EndsWith(".xtl") || file.NameLower.EndsWith(".xtp"))
            {
                var xtl = new XtlFile(entry);
                xtl.Load(data);
                return xtl;
            }
            else if (file.NameLower.EndsWith(".xwt"))
            {
                var xwt = new XwtFile(entry);
                xwt.Load(data);
                return xwt;
            }
            else if (file.NameLower.EndsWith(".xshp"))
            {
                var xshp = new XshpFile(entry);
                xshp.Load(data);
                return xshp;
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

            //An animation pack has no geometry: it goes to the preview as an empty piece pack, and
            //the animation window opens over it once the preview exists.
            if (entry.NameLower.EndsWith(".xapk") || LooksLikeAnimPack(data))
            {
                var xapk = new XapkFile(entry);
                xapk.Load(data);
                if (xapk.LoadException == null)
                {
                    ShowAnimationWindowFor(xapk);
                    return xapk;
                }
            }

            XapbFile.Textures?.Clear();
            if (entry.NameLower.EndsWith(".xapb"))
            {
                var xapb = new XapbFile(entry);
                xapb.Load(data);
                return xapb;
            }
            else if (entry.NameLower.EndsWith(".xrsc"))
            {
                var xrsc = new XrscFile(entry);
                xrsc.Load(data);
                return xrsc;
            }
            else if (entry.NameLower.EndsWith(".xdr"))
            {
                var drawable = new DrawableFile(entry);
                drawable.Load(data);
                return drawable;
            }
            else if (entry.NameLower.EndsWith(".xmd"))
            {
                var xmd = new XmdFile(entry);
                xmd.Load(data);
                if (loadDependencies) LoadDependencies(xmd);
                return xmd;
            }
            else //xcs assumed
            {
                var xcs = new XcsFile(entry);
                xcs.Load(data);
                if (loadDependencies)
                {
                    LoadDependencies(xcs);
                    xcs.DependenciesLoaded = true;
                }
                return xcs;
            }
        }

        public void LoadDependencies(PiecePack pack)
        {
            if (pack?.Pieces == null) return;
            foreach (var piece in pack.Pieces.Values)
            {
                if (piece is not IRsc5DrawableRoot drawable) continue;
                ResolveShaderTextures(drawable);
                SetGeoShaderTextures(drawable);
            }
        }

        private void ResolveShaderTextures(IRsc5DrawableRoot drawable)
        {
            var fm = Game.GetFileManager() as Rpf3FileManager;
            var dfm = fm?.DataFileMgr;
            var cityDict = fm.Store.TexturesCityDict;
            var files = dfm.StreamEntries[Rpf3ResourceType.Generic];
            var cache = new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase);

            foreach (var shader in drawable.ShaderGroup.Shaders.Items)
            {
                if (shader?.Params == null) continue;
                foreach (var param in shader.Params)
                {
                    if (param.Type != 0 || param.Texture == null || param.Texture.Data != null) continue;
                    var key = Rpf3Crypto.NormalizeTexName(param.Texture.Name);
                    
                    if (TryResolveTexture(key, cityDict, files, cache, out var result))
                    {
                        param.Texture = (Rsc5Texture)result;
                    }
                }
            }
        }

        private static void SetGeoShaderTextures(IRsc5DrawableRoot drawable)
        {
            foreach (var model in drawable?.Lod?.ModelsData.Items)
            {
                if (model?.Geometries.Items == null) continue;
                var geoms = model.Geometries.Items;
                
                foreach (var geom in geoms)
                {
                    if (geom?.ShaderRef?.Params == null || geom.Textures == null) continue;
                    var parms = geom.ShaderRef.Params;
                    var slots = geom.Textures;

                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] != null && slots[i].Data != null) continue;
                        for (int p = 0; p < parms.Length; p++)
                        {
                            var prm = parms[p];
                            if (prm.Type != 0 || prm.Texture == null) continue;

                            if (Rpf3Crypto.NormalizeTexName(prm.Texture.Name) == Rpf3Crypto.NormalizeTexName(slots[i]?.Name))
                            {
                                slots[i] = prm.Texture;
                                break;
                            }
                        }
                    }

                    //A road material only gets its albedo once the control and decal maps have
                    //resolved, which happens here rather than back when the shader was set up
                    if (geom.RoadMaterial && slots.Length > 2)
                    {
                        slots[0] = Rsc5RoadMaterial.GetAlbedo(slots[1], slots[2]);
                    }
                }
            }
        }

        private bool TryResolveTexture(string baseName, Dictionary<JenkHash, List<Rpf3TextureStoreItem>> cityDict, Dictionary<JenkHash, Rpf3FileEntry> files, Dictionary<string, Texture> cache, out Texture resolved)
        {
            if (cache.TryGetValue(baseName, out resolved)) return true;
            var dds = baseName + ".dds";
            var hash = new JenkHash(dds);

            if (!cityDict.TryGetValue(hash, out var matches)) return false;
            foreach (var item in matches)
            {
                if (!files.TryGetValue(item.FileHash, out var entry)) continue;

                var resolvedTex = entry.NameLower.EndsWith(".xtd") ? ResolveFromXtd(entry, dds) : ResolveFromXcs(entry, dds);
                if (resolvedTex != null)
                {
                    cache[baseName] = resolved = resolvedTex;
                    return true;
                }
            }
            return false;
        }

        private Texture ResolveFromXtd(Rpf3FileEntry entry, string texNameDds)
        {
            var pack = GetOrLoadTexturePack(entry);
            return pack?.Textures.TryGetValue(texNameDds, out var t) == true ? t : null;
        }

        private Texture ResolveFromXcs(Rpf3FileEntry entry, string texNameDds)
        {
            var pack = GetOrLoadPiecePack(entry);
            if (pack == null) return null;

            foreach (var p in pack.Pieces.Values)
            {
                if (p.TexturePack.Textures.TryGetValue(texNameDds, out var t))
                {
                    return t;
                }
            }
            return null;
        }

        private TexturePack GetOrLoadTexturePack(Rpf3FileEntry entry)
        {
            return TexturePackCache.GetOrAdd(entry.PathLower, _ => LoadTexturePack(entry));
        }

        private PiecePack GetOrLoadPiecePack(Rpf3FileEntry entry)
        {
            return PiecePackCache.GetOrAdd(entry.PathLower, _ => LoadPiecePack(entry));
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

    public class Rpf3DataFileMgr(Rpf3FileManager fman)
    {
        public Rpf3FileManager FileManager = fman;
        public Dictionary<string, Rpf3DataFileDevice> Devices;
        public Dictionary<Rpf3ResourceType, Dictionary<JenkHash, Rpf3FileEntry>> StreamEntries;
        public Dictionary<JenkHash, XcsFile> XcsFiles;

        public void Init()
        {
            if (this.StreamEntries != null)
            {
                return;
            }

            this.Devices = [];
            this.StreamEntries = [];
            this.XcsFiles = [];
            
            this.LoadFiles();
            this.IndexCityFiles();
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

                    var hash = fe.NameOffset;
                    if (!this.StreamEntries.TryGetValue(fe.ResourceType, out var entries))
                    {
                        entries = [];
                        this.StreamEntries[fe.ResourceType] = entries;
                    }
                    entries[hash] = fe;
                }
            }
        }

        public void IndexCityFiles()
        {
            XcsFiles ??= [];
            foreach (var se in StreamEntries[Rpf3ResourceType.Generic])
            {
                var fe = se.Value;
                if (fe.Parent == null || new JenkHash(fe.Parent.Name) != 0x45A0781) continue; //sc
                if (!fe.Name.EndsWith(".xcs")) continue;

                var xcs = new XcsFile(fe);
                XcsFiles[xcs.Hash] = xcs;
            }
        }

        public XctFile GetSectorBounds()
        {
            if (StreamEntries[Rpf3ResourceType.Generic].TryGetValue(0x3CDD416, out var entry)) //city.xct
            {
                var data = this.FileManager.EnsureFileData(entry, null);
                var xct = new XctFile(entry);
                xct.Load(data);
                return xct;
            }
            return null;
        }

        public XmdFile LoadMapLod()
        {
            if (StreamEntries[Rpf3ResourceType.Generic].TryGetValue(0xA6E77868, out var entry)) //lod.xmd
            {
                var data = this.FileManager.EnsureFileData(entry, null);
                var xmd = this.FileManager.LoadPiecePack(entry, data, true) as XmdFile;
                return xmd;
            }
            return null;
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

    public class Rpf3Store(Rpf3FileManager fileman)
    {
        public Rpf3FileManager FileMan = fileman;
        public List<Rpf3TextureStoreItem> TexturesCity;
        public Dictionary<JenkHash, List<Rpf3TextureStoreItem>> TexturesCityDict;

        public void SaveStartupCache(BinaryWriter bw)
        {
            Core.Engine.Console.Write("Rpf3FileManager", "Building MCLA startup cache");

            var bmp = FileMan.DataFileMgr.StreamEntries[Rpf3ResourceType.Generic];
            var textureItemsSet = new HashSet<Rpf3TextureStoreItem>();

            var lodTxds = bmp.Where(e => e.Key == 0x5674B600 || e.Key == 0x5DDDB21D).Select(e => e.Value).ToList(); //lod_0.xtd & common.xtd
            foreach (var txd in lodTxds)
            {
                var lodTxp = FileMan.LoadTexturePack(txd);
                foreach (var tex in lodTxp.Textures)
                {
                    var item = new Rpf3TextureStoreItem()
                    {
                        Texture = tex.Key,
                        FileHash = txd.NameOffset
                    };
                    textureItemsSet.Add(item);
                }
            }

            Parallel.ForEach(bmp, kv =>
            {
                var entry = kv.Value;
                if (entry == null) return;
                if (!entry.NameLower.EndsWith(".xcs")) return;

                var pack = (XcsFile)FileMan.LoadPiecePack(entry);
                if (pack != null)
                {
                    var localBag = new ConcurrentBag<Rpf3TextureStoreItem>();
                    foreach (var p in pack.Pieces.Values)
                    {
                        foreach (var tex in p?.TexturePack?.Textures ?? Enumerable.Empty<KeyValuePair<string, Texture>>())
                        {
                            var item = new Rpf3TextureStoreItem()
                            {
                                Texture = tex.Key,
                                FileHash = entry.NameOffset
                            };
                            localBag.Add(item);
                        }
                    }

                    lock (textureItemsSet)
                    {
                        foreach (var item in localBag)
                        {
                            textureItemsSet.Add(item);
                        }
                    }
                }
            });
            SerializeItems(bw, [.. textureItemsSet]);
        }

        public void LoadStartupCache(BinaryReader br)
        {
            TexturesCity = [];
            DeserializeItems(br, TexturesCity);
            BuildTextureDict();
        }

        public void BuildTextureDict()
        {
            TexturesCityDict = [];
            foreach (var item in TexturesCity)
            {
                var hash = new JenkHash(item.Texture);
                if (!TexturesCityDict.TryGetValue(hash, out var list))
                {
                    list = [];
                    TexturesCityDict[hash] = list;
                }
                list.Add(item);
            }
        }

        public static void SerializeItems(BinaryWriter bw, List<Rpf3TextureStoreItem> list)
        {
            bw.Write(list.Count);
            foreach (var item in list)
            {
                bw.WriteStringNullTerminated(item.Texture);
                bw.Write(item.FileHash);
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
                    FileHash = br.ReadUInt32()
                };
                dict.Add(item);
            }
        }
    }

    public struct Rpf3TextureStoreItem
    {
        public string Texture;
        public JenkHash FileHash;

        public override readonly string ToString()
        {
            return $"{Texture} : {FileHash}";
        }
    }
}