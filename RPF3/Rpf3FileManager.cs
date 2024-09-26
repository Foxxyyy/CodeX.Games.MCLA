using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.Files;
using System.Text;

namespace CodeX.Games.MCLA.RPF3
{
    public class Rpf3FileManager : FileManager
    {
        public override string ArchiveTypeName => "RPF4";
        public override string ArchiveExtension => ".rpf";
        public Rpf3DataFileMgr DataFileMgr { get; set; }

        public Rpf3FileManager(MCLAGame game) : base(game)
        {
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
            InitFileType(".fxc", "Compiled Shaders", FileTypeIcon.SystemFile, FileTypeAction.ViewXml);
            InitFileType(".xed", "Expression Dictionary", FileTypeIcon.SystemFile, FileTypeAction.ViewXml, true);
            InitFileType(".xld", "Cloth Dictionary", FileTypeIcon.SystemFile, FileTypeAction.ViewXml, true, true);
            InitFileType(".xapb", "Ambient Ped", FileTypeIcon.Piece, FileTypeAction.ViewModels, true, true);
            InitFileType(".xft", "Fragment", FileTypeIcon.Piece, FileTypeAction.ViewModels, true, true);
            InitFileType(".xdd", "Drawable Dictionary", FileTypeIcon.Level, FileTypeAction.ViewModels, true, true);
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

            if (DataFileMgr == null)
            {
                DataFileMgr = new Rpf3DataFileMgr(this);
            }
            DataFileMgr.Init();

            Core.Engine.Console.Write("MCLA.InitGameFiles", "MCLA Initialised.");
        }

        public override void SaveStartupCache()
        {
            
        }

        public override void LoadStartupCache()
        {
            
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
                return xshp;
            }
            return null;
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
    }

    public class Rpf3DataFileMgr
    {
        public Rpf3FileManager FileManager;
        public Dictionary<string, Rpf3DataFileDevice> Devices;
        public Dictionary<Rpf3ResourceType, Dictionary<JenkHash, Rpf3FileEntry>> StreamEntries;

        public Rpf3DataFileMgr(Rpf3FileManager fman)
        {
            FileManager = fman;
        }

        public void ReadStartupCache(BinaryReader br)
        {

        }

        public void WriteStartupCache(BinaryWriter bw)
        {

        }

        public void Init()
        {
            if (StreamEntries != null)
                return;

            Devices = new Dictionary<string, Rpf3DataFileDevice>();
            StreamEntries = new Dictionary<Rpf3ResourceType, Dictionary<JenkHash, Rpf3FileEntry>>();
            LoadFiles();
        }

        private void LoadFiles()
        {
            foreach (var archive in FileManager.AllArchives)
            {
                foreach (var file in archive.AllEntries)
                {

                }
            }
        }

        public Rpf3FileEntry TryGetStreamEntry(JenkHash hash, Rpf3ResourceType ext)
        {
            if (StreamEntries.TryGetValue(ext, out var entries))
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
}