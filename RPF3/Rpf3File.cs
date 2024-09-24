using Ionic.Zlib;
using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using static CodeX.Games.MCLA.RPF3.Rpf3Crypto;
using TC = System.ComponentModel.TypeConverterAttribute;
using EXP = System.ComponentModel.ExpandableObjectConverter;

namespace CodeX.Games.MCLA.RPF3
{
    [TC(typeof(EXP))] public class Rpf3File : GameArchive
    {
        public long StartPos { get; set; }
        public uint Version { get; set; } = 0x33465052; //Identifier, MCLA = 0x33465052 (860246098)
        public uint EntryCount { get; set; } //Number of entries
        public uint TOCSize { get; set; } //Size of table of content
        public uint UNK1 { get; set; } //Unknown, probably stringtable offset but unused like in RDR1
        public int EncFlag { get; set; } //Encryption flag
        public bool Encrypted
        {
            get => (uint)EncFlag != 0U;
            set
            {
                if (value)
                    EncFlag = -1;
                else
                    EncFlag = 0;
            }
        }

        public Rpf3File(string fpath, string relpath)
        {
            var fi = new FileInfo(fpath);
            Name = fi.Name;
            Path = relpath.ToLowerInvariant();
            FilePath = fpath;
            Size = fi.Length;
        }

        public Rpf3File(string name, string path, long filesize) //For a child RPF
        {
            Name = name;
            Path = path.ToLowerInvariant();
            FilePath = path;
            Size = filesize;
        }

        private void ReadHeader(BinaryReader br)
        {
            StartPos = br.BaseStream.Position;
            Version = br.ReadUInt32();
            TOCSize = br.ReadUInt32();
            EntryCount = br.ReadUInt32();
            UNK1 = br.ReadUInt32();
            EncFlag = br.ReadInt32();

            if (Version != 0x33465052)
            {
                var verbytes = BitConverter.GetBytes(Version);
                var versionstr = BitConverter.ToString(verbytes);
                throw new Exception("Invalid Rpf3 archive - found \"" + versionstr + "\" instead.");
            }

            br.BaseStream.Position = 0x800;
            byte[] entriesdata = br.ReadBytes((int)TOCSize);

            if (Encrypted)
            {
                entriesdata = DecryptAES(entriesdata);
            }

            var entriesrdr = new BinaryReader(new MemoryStream(entriesdata));
            AllEntries = new List<GameArchiveEntry>();

            for (uint i = 0; i < EntryCount; i++)
            {
                var e = Rpf3Entry.ReadEntry(this, entriesrdr);
                e.StartIndex = (int)i;

                if (e is Rpf3ResourceFileEntry entry && entry.IsCompressed && !entry.Name.EndsWith(".rpf"))
                {
                    br.BaseStream.Position = e.Offset;
                    var data = br.ReadBytes((int)entry.Size);
                    var temp = BufferUtil.DecompressDeflate(data, (int)entry.Size);
                    var dr = new BinaryReader(new MemoryStream(temp));

                    if (dr.ReadUInt32() == 860246098) //'RPF3'
                    {
                        entry.Name += ".rpf";
                    }
                }
                AllEntries.Add(e);
            }
            CreateDirectories();
        }

        private void CreateDirectories()
        {
            var r = (Rpf3DirectoryEntry)AllEntries[0];
            Root = r;
            Root.Path = Path.ToLowerInvariant();
            var stack = new Stack<Rpf3DirectoryEntry>();
            stack.Push(r);

            while (stack.Count > 0)
            {
                var item = stack.Pop();
                int starti = item.EntriesIndex;
                int endi = item.EntriesIndex + item.EntriesCount;
                item.Children = new List<Rpf3Entry>();

                for (int i = starti; i < endi; i++)
                {
                    var e = AllEntries[i];
                    e.Parent = item;
                    ((Rpf3Entry)e).EntryParent = item;

                    if (e is Rpf3DirectoryEntry rde)
                    {
                        rde.Path = item.Path + "\\" + rde.NameLower;
                        item.Directories.Add(rde);
                        item.Children.Add(rde);
                        stack.Push(rde);
                    }
                    else if (e is Rpf3FileEntry rfe)
                    {
                        rfe.Path = item.Path + "\\" + rfe.NameLower;
                        item.Files.Add(rfe);
                        item.Children.Add(rfe);
                    }
                }
            }
        }

        public static void RenameArchive(Rpf3File file, string newname)
        {
            //updates all items in the RPF with the new path - no actual file changes made here
            //(since all the paths are generated at runtime and not stored)

            file.Name = newname;
            file.Path = GetParentPath(file.Path) + newname;
            file.FilePath = GetParentPath(file.FilePath) + newname;
            file.UpdatePaths();
        }

        public static void RenameEntry(Rpf3Entry entry, string newname)
        {
            //rename the entry in the RPF header... 
            //also make sure any relevant child paths are updated...

            string dirpath = GetParentPath(entry.Path);

            entry.Name = newname;
            entry.NameOffset = JenkHash.GenHash(newname);
            entry.Path = dirpath + newname;

            string sname = entry.ShortNameLower;
            JenkIndex.Ensure(sname, "MCLA"); //could be anything... but it needs to be there

            var parent = (Rpf3File)entry.Archive;
            string fpath = parent.GetPhysicalFilePath();

            using (var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                using var bw = new BinaryWriter(fstream);
                parent.EnsureAllEntries();
                parent.WriteHeader(bw);
            }

            if (entry is Rpf3DirectoryEntry dir)
            {
                //A folder was renamed, make sure all its children's paths get updated
                parent.UpdatePaths(dir);
            }
        }

        private void UpdatePaths(Rpf3DirectoryEntry dir = null)
        {
            //Recursively update paths, including in child RPFs.
            if (dir == null)
            {
                Root.Path = Path.ToLowerInvariant();
                dir = (Rpf3DirectoryEntry)Root;
            }

            foreach (var file in dir.Files)
            {
                file.Path = dir.Path + "\\" + file.NameLower;

                if ((file is Rpf3ResourceFileEntry binf) && file.NameLower.EndsWith(".rpf"))
                {
                    if (FindChildArchive(binf) is Rpf3File childrpf)
                    {
                        childrpf.Path = binf.Path;
                        childrpf.FilePath = binf.Path;
                        childrpf.UpdatePaths();
                    }
                }

            }

            foreach (Rpf3DirectoryEntry subdir in dir.Directories.Cast<Rpf3DirectoryEntry>())
            {
                subdir.Path = dir.Path + "\\" + subdir.NameLower;
                UpdatePaths(subdir);
            }
        }

        public static void DeleteEntry(Rpf3Entry entry)
        {
            //Delete this entry from the RPF header.
            //Also remove any references to this item in its parent directory...
            //If this is a directory entry, this will delete the contents first

            var parent = (Rpf3File)entry.Archive;
            string fpath = parent.GetPhysicalFilePath();
            if (!File.Exists(fpath))
            {
                throw new Exception("Root RPF file " + fpath + " does not exist!");
            }

            var entryasdir = entry as Rpf3DirectoryEntry;
            if (entryasdir != null)
            {
                var deldirs = entryasdir.Directories.ToArray();
                var delfiles = entryasdir.Files.ToArray();
                foreach (Rpf3DirectoryEntry deldir in deldirs.Cast<Rpf3DirectoryEntry>())
                {
                    DeleteEntry(deldir);
                }
                foreach (Rpf3FileEntry delfile in delfiles.Cast<Rpf3FileEntry>())
                {
                    DeleteEntry(delfile);
                }
            }

            if (entry.Parent == null)
            {
                throw new Exception("Parent directory is null! This shouldn't happen - please refresh the folder!");
            }

            if (entryasdir != null)
            {
                entry.Parent.Directories.Remove(entryasdir);
                ((Rpf3DirectoryEntry)entry.Parent).Children.Remove(entryasdir);
            }
            if (entry is Rpf3FileEntry entryasfile)
            {
                entry.Parent.Files.Remove(entryasfile);
                ((Rpf3DirectoryEntry)entry.Parent).Children.Remove(entryasfile);

                var child = parent.FindChildArchive(entryasfile);
                if (child != null)
                {
                    parent.Children.Remove(child); //RPF file being deleted...
                }
            }

            using var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite);
            using var bw = new BinaryWriter(fstream);
            parent.EnsureAllEntries();
            parent.WriteHeader(bw);
        }

        private void EnsureAllEntries()
        {
            if (AllEntries == null)
            {
                AllEntries = new List<GameArchiveEntry>(); //Assume this is a new RPF, create the root directory entry
                Root = new Rpf3DirectoryEntry
                {
                    Archive = this,
                    Name = string.Empty,
                    Path = Path.ToLowerInvariant()
                };
            }

            Children ??= new List<GameArchive>();
            var newSupers = new List<GameArchiveEntry>();
            var sortSupers = (Action<GameArchiveEntry>)null;

            sortSupers = entry =>
            {
                if (!newSupers.Contains(entry))
                    newSupers.Add(entry);

                var fileEntry = entry as Rpf3Entry;
                if (fileEntry.IsDirectory)
                {
                    var e = (Rpf3DirectoryEntry)entry;
                    if (e.Children != null && e.Children.Count > 0)
                    {
                        e.StartIndex = newSupers.Count;
                        newSupers.AddRange(e.Children.OrderBy(o => o.NameOffset.Hash));

                        foreach (var child in e.Children)
                            sortSupers(child);
                    }
                }
            };
            sortSupers(AllEntries[0]);
            AllEntries.Clear();

            foreach (var superEntry in newSupers.Cast<Rpf3Entry>())
            {
                if (superEntry.IsDirectory)
                {
                    var asDirectory = superEntry as Rpf3DirectoryEntry;
                    asDirectory.EntriesIndex = superEntry.StartIndex;
                    asDirectory.EntriesCount = superEntry.Children.Count;
                }
                AllEntries.Add(superEntry);
            }
            EntryCount = (uint)AllEntries.Count;
        }

        private void WriteHeader(BinaryWriter bw)
        {
            //Entries may have been updated, so need to do this after ensuring header space
            var tocdata = GetTOCData();
            if (Encrypted)
            {
                var buffer = new byte[tocdata.Length + 64];
                var unkArr = new byte[]
                {
                    0x2F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0xA2, 0xBD, 0xC6, 0x25, 0x9A, 0x37, 0xA2, 0xDA, 0x62, 0x10, 0x8C, 0x2C, 0x5C, 0x8C, 0xB0, 0x91,
                    0xA2, 0xBD, 0xC6, 0x25, 0x9A, 0x37, 0xA2, 0xDA, 0x62, 0x10, 0x8C, 0x2C, 0x5C, 0x8C, 0xB0, 0x91,
                    0xA2, 0xBD, 0xC6, 0x25, 0x9A, 0x37, 0xA2, 0xDA, 0x62, 0x10, 0x8C, 0x2C, 0x5C, 0x8C, 0xB0, 0x91
                };
                Buffer.BlockCopy(tocdata, 0, buffer, 0, tocdata.Length);
                Buffer.BlockCopy(unkArr, 0, buffer, tocdata.Length, unkArr.Length);
                tocdata = EncryptAES(buffer);
            }

            //Now there's enough space, it's safe to write the header data...
            bw.BaseStream.Position = StartPos;
            bw.Write(Version);
            bw.Write(TOCSize);
            bw.Write(EntryCount);
            bw.Write(UNK1);
            bw.Write(EncFlag);

            while (bw.BaseStream.Position < 0x800)
            {
                bw.Write(0);
            }
            bw.Write(tocdata);
        }

        private uint GetHeaderBlockCount() //Make sure EntryCount is updated before calling this...
        {
            uint headerusedbytes = 20 + (EntryCount * 16);
            uint headerblockcount = GetBlockCount(headerusedbytes);
            return headerblockcount;
        }

        private static uint GetBlockCount(long bytecount)
        {
            uint b0 = (uint)(bytecount & 0x1FF); //511;
            uint b1 = (uint)(bytecount >> 9);
            if (b0 == 0) return b1;
            return b1 + 1;
        }

        private byte[] GetTOCData()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            foreach (var entry in AllEntries.Cast<Rpf3Entry>())
            {
                entry.Write(bw);
            }

            var buf = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(buf, 0, buf.Length);

            return buf;
        }

        private void InsertFileSpace(BinaryWriter bw, Rpf3FileEntry entry)
        {
            long blockcount = entry.GetFileSize();
            long hole = FindHole(blockcount, 0, 0, entry, out long roundup);
            entry.SetOffset(hole + roundup);

            EnsureAllEntries();
            WriteHeader(bw);
        }

        private long FindHole(long reqblocks, long ignorestart, long ignoreend, Rpf3FileEntry e, out long roundup)
        {
            var allfiles = new List<Rpf3FileEntry>();
            foreach (var entry in AllEntries)
            {
                if (entry is Rpf3FileEntry rfe)
                {
                    allfiles.Add(rfe);
                }
            }
            allfiles.Sort((e1, e2) => e1.Offset.CompareTo(e2.Offset));

            (long, Rpf3FileEntry) block = FindEndBlock();

            long length = 0;
            roundup = 0;

            if (!e.IsResource)
            {
                if (reqblocks >= 131072)
                {
                    length = RoundUp(block.Item1, 2048L) - block.Item1;
                    if (length == 0L)
                        length = 2048L;
                }
                else
                {
                    length = RoundUp(block.Item1, 8L) - block.Item1;
                    if (length == 0L)
                        length = 8L;
                }
            }
            else
            {
                length = RoundUp(block.Item1, 2048L) - block.Item1;
                if (length == 0L)
                    length = 2048L;
            }

            roundup = length;
            return block.Item1;
        }

        private (long, Rpf3FileEntry) FindEndBlock()
        {
            long endblock = 0;
            Rpf3FileEntry lastFile = null;

            foreach (var entry in AllEntries) //Find the next available block after all other files (or after header if there's no files)
            {
                if (entry is Rpf3FileEntry e)
                {
                    long ecnt = e.GetOffset();
                    long eend = ecnt + e.GetFileSize();

                    if (eend > endblock)
                    {
                        endblock = eend;
                        lastFile = e;
                    }
                }
            }

            if (endblock == 0) //Must be no files present, end block comes directly after the header.
            {
                endblock = 671744L;
            }
            return (endblock, lastFile);
        }

        private void UpdateStartPos(long newpos)
        {
            StartPos = newpos;
            if (Children != null)
            {
                foreach (var child in Children.Cast<Rpf3File>()) //Make sure children also get their StartPos updated !
                {
                    if (child.ParentFileInfo is not Rpf3FileEntry cpfe)
                        continue; //Shouldn't really happen...

                    var cpos = StartPos + cpfe.Offset * 8;
                    child.UpdateStartPos(cpos);
                }
            }
        }

        private static string GetParentPath(string path)
        {
            string dirpath = path.Replace('/', '\\'); //just to make sure..
            int lidx = dirpath.LastIndexOf('\\');
            if (lidx > 0)
            {
                dirpath = dirpath[..(lidx + 1)];
            }
            if (!dirpath.EndsWith("\\"))
            {
                dirpath += "\\";
            }
            return dirpath;
        }

        public override void ReadStructure(BinaryReader br)
        {
            ReadHeader(br);
            Children = new List<GameArchive>();

            foreach (Rpf3Entry entry in AllEntries.Cast<Rpf3Entry>())
            {
                if (entry is Rpf3ResourceFileEntry binentry)
                {
                    string lname = binentry.NameLower;
                    if (lname.EndsWith(".rpf"))
                    {
                        br.BaseStream.Position = binentry.Offset;
                        long l = binentry.GetFileSize();

                        var subfile = new Rpf3File(binentry.Name, binentry.Path, l)
                        {
                            Parent = this,
                            ParentFileInfo = binentry
                        };

                        var br1 = CreateRPFReader(binentry, br);
                        subfile.ReadStructure(br1);
                        Children.Add(subfile);
                    }
                }
            }
        }

        public override bool EnsureEditable(Func<string, string, bool> confirm)
        {
            return true;
        }

        public override byte[] ExtractFile(GameArchiveFileInfo f, bool compressed = false)
        {
            try
            {
                using BinaryReader br = new(File.OpenRead(GetPhysicalFilePath()));
                if (f is Rpf3FileEntry rf)
                    return ExtractFileResource(rf, br);
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

        public static byte[] ExtractFileResource(Rpf3Entry entry, BinaryReader br)
        {
            if (entry == null) return null;
            var rentry = entry as Rpf3ResourceFileEntry;
            if (entry.Archive.Size != br.BaseStream.Length) //To read internal RPFs
            {
                var rpf = rentry.Archive.GetTopParent().AllEntries.FirstOrDefault(s => s.Name == entry.Archive.Name);
                var br1 = CreateRPFReader((Rpf3Entry)rpf, br);
                br = br1;
            }

            br.BaseStream.Position = rentry.IsResource ? (rentry.Offset & 0x7fffff00) : rentry.Offset;
            byte[] data = br.ReadBytes((int)rentry.Size);

            //Let's handle the scripts like this for now...
            if (entry.Name.EndsWith(".sco") && rentry.Size > 50)
            {
                try
                {
                    var br2 = new BinaryReader(new MemoryStream(data));
                    var header = br2.ReadBytes(24);
                    var uncompressedSize = br2.ReadUInt32();
                    var uncompressedData = br2.ReadBytes((int)uncompressedSize);
                    var decrypted = DecryptAES(uncompressedData);
                    var decompressed = ZlibStream.UncompressBuffer(decrypted);

                    var buffer = new byte[24 + 4 + decompressed.Length];
                    Buffer.BlockCopy(header, 0, buffer, 0, header.Length);
                    Buffer.BlockCopy(BitConverter.GetBytes(uncompressedSize), 0, buffer, header.Length, 4);
                    Buffer.BlockCopy(decompressed, 0, buffer, 28, decompressed.Length);
                    return buffer;
                }
                catch
                {

                }
            }

            if (!rentry.IsResource)
            {
                if (!rentry.IsCompressed)
                    return data;
                else
                    return BufferUtil.DecompressDeflate(data, (int)rentry.Size);
            }

            //Read as resource
            br.BaseStream.Position = rentry.Offset;
            return rentry?.GetDataFromStream(new MemoryStream(data)) ?? null;
        }

        internal void ReadStartupCache(BinaryReader br)
        {
            StartPos = br.ReadInt64();
            Version = br.ReadUInt32();
            EntryCount = br.ReadUInt32();
            TOCSize = br.ReadUInt32();
            UNK1 = br.ReadUInt32();
            EncFlag = br.ReadInt32();

            AllEntries = new List<GameArchiveEntry>();
            var entrydict = new Dictionary<string, GameArchiveFileInfo>();

            for (int i = 0; i < EntryCount; i++)
            {
                var entry = Rpf3Entry.ReadEntry(this, br);
                AllEntries.Add(entry);
                if ((entry is GameArchiveFileInfo finfo) && (finfo.IsArchive))
                {
                    entrydict[finfo.Path.ToLowerInvariant()] = finfo;
                }
            }
            CreateDirectories();
        }

        internal void WriteStartupCache(BinaryWriter bw)
        {
            bw.Write(StartPos);
            bw.Write(Version);
            bw.Write(EntryCount);
            bw.Write(TOCSize);
            bw.Write(UNK1);
            bw.Write(EncFlag);

            for (int i = 0; i < EntryCount; i++)
            {
                (AllEntries[i] as Rpf3Entry)?.Write(bw);
            }
        }

        private void WriteNewArchive(BinaryWriter bw)
        {
            var stream = bw.BaseStream;
            Version = 0x52504633; //'RPF3'
            StartPos = stream.Position;
            EnsureAllEntries();
            WriteHeader(bw);
            Size = stream.Position - StartPos;
        }

        public static Rpf3File CreateNew(string gtafolder, string relpath)
        {
            //Create a new, empty RPF file in the filesystem
            //This will assume that the folder the file is going into already exists!

            string fpath = gtafolder;
            relpath = relpath.Replace("RDR1\\\\", "");
            fpath = fpath.EndsWith("\\") ? fpath : fpath + "\\";
            fpath += relpath;

            if (File.Exists(fpath))
            {
                throw new Exception("File " + fpath + " already exists!");
            }

            File.Create(fpath).Dispose(); //Just write a placeholder, will fill it out later
            var file = new Rpf3File(fpath, relpath);

            using (var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                using var bw = new BinaryWriter(fstream);
                file.WriteNewArchive(bw);
            }
            return file;
        }

        public static Rpf3FileEntry CreateFile(Rpf3DirectoryEntry dir, string name, byte[] data, bool overwrite = true)
        {
            string namel = name.ToLowerInvariant();
            if (namel.EndsWith(".rpf"))
            {
                throw new Exception("Cannot import RPF!");
            }

            if (overwrite)
            {
                foreach (Rpf3Entry exfile in dir.Files.Cast<Rpf3Entry>())
                {
                    if (exfile.NameLower == namel)
                    {
                        //File already exists. delete the existing one first!
                        //This should probably be optimised to just replace the existing one...
                        DeleteEntry(exfile);
                        break;
                    }
                }
            }

            var parent = (Rpf3File)dir.Archive;
            string fpath = parent.GetPhysicalFilePath();
            string rpath = dir.Path + "\\" + namel;
            if (!File.Exists(fpath))
            {
                throw new Exception("Root RPF file " + fpath + " does not exist!");
            }

            Rpf3FileEntry entry = null;
            uint len = (uint)data.Length;

            if (entry == null) //no RSC5 header present, import as a binary file.
            {
                var bentry = new Rpf3ResourceFileEntry
                {
                    Flag = len,
                    IsCompressed = false,
                    IsResource = false,
                    IsEncrypted = false,
                    Size = len,
                    SizeInArchive = (int)len
                };
                entry = bentry;
            }

            entry.Parent = entry.EntryParent = dir;
            entry.Archive = parent;
            entry.Path = rpath;
            entry.Name = name;
            entry.NameOffset = JenkHash.GenHash(name);
            entry.ReadBackFromRPF = false;
            entry.IsDirectory = false;
            entry.CustomDataStream = new MemoryStream(data);
            entry.Entry = entry;

            foreach (var exfile in dir.Files)
            {
                if (exfile.NameLower == entry.NameLower)
                {
                    throw new Exception("File \"" + entry.Name + "\" already exists!");
                }
            }

            dir.Files.Add(entry);
            dir.Children.Add(entry);

            using (var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                using var bw = new BinaryWriter(fstream);
                parent.InsertFileSpace(bw, entry);

                long bbeg = parent.StartPos + entry.GetOffset();
                long bend = bbeg + entry.GetFileSize();

                fstream.Position = bbeg;
                fstream.Write(data, 0, data.Length);

                byte[] buffer = new byte[(int)(RoundUp(fstream.Position, 2048L) - fstream.Position)];
                fstream.Write(buffer, 0, buffer.Length);
            }
            return entry;
        }

        public static Rpf3DirectoryEntry CreateDirectory(Rpf3DirectoryEntry dir, string name)
        {
            var parent = (Rpf3File)dir.Archive;
            string namel = name.ToLowerInvariant();
            string fpath = parent.GetPhysicalFilePath();
            string rpath = dir.Path + "\\" + namel;

            if (!File.Exists(fpath))
            {
                throw new Exception("Root RPF file " + fpath + " does not exist!");
            }

            var entry = new Rpf3DirectoryEntry
            {
                Parent = dir,
                Archive = parent,
                Path = rpath,
                Name = name,
                NameOffset = JenkHash.GenHash(name),
                IsDirectory = true,
                Children = new List<Rpf3Entry>()
            };

            foreach (var exdir in dir.Directories)
            {
                if (exdir.NameLower == entry.NameLower)
                {
                    throw new Exception("RPF Directory \"" + entry.Name + "\" already exists!");
                }
            }

            dir.Directories.Add(entry);
            dir.Children.Add(entry);

            using (var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                using var bw = new BinaryWriter(fstream);
                parent.EnsureAllEntries();
                parent.WriteHeader(bw);
            }
            return entry;
        }

        public static BinaryReader CreateRPFReader(Rpf3Entry entry, BinaryReader br)
        {
            var decompressed = ExtractFileResource(entry, br);
            return new BinaryReader(new MemoryStream(decompressed));
        }
    }

    [TC(typeof(EXP))]
    public abstract class Rpf3Entry : GameArchiveEntryBase, GameArchiveEntry
    {
        public Rpf3Entry Entry { get; set; }
        public long Offset { get; set; }
        public Rpf3DirectoryEntry EntryParent { get; set; }
        public List<Rpf3Entry> Children { get; set; }
        public Stream CustomDataStream { get; set; }

        private string _Attributes;
        public override string Attributes
        {
            get
            {
                if (_Attributes == null)
                {
                    _Attributes = "";
                    if (this is Rpf3FileEntry)
                    {
                        _Attributes += "File";
                    }
                    if (IsEncrypted)
                    {
                        if (_Attributes.Length > 0) _Attributes += ", ";
                        _Attributes += "Encrypted";
                    }
                }
                return _Attributes;
            }
        }

        public bool IsEncrypted { get; set; }
        public uint Flags { get; set; }
        public JenkHash NameOffset { get; set; }
        public bool IsDirectory { get; set; }
        public int StartIndex { get; set; }
        public bool ReadBackFromRPF { get; set; } = true;

        public abstract void Read(BinaryReader r);
        public abstract void Write(BinaryWriter w);

        public static Rpf3Entry ReadEntry(GameArchive archive, BinaryReader br)
        {
            br.BaseStream.Seek(8L, SeekOrigin.Current);
            int type = br.ReadInt32();

            Rpf3Entry entry;
            if (type < 0)
                entry = new Rpf3DirectoryEntry();
            else
                entry = new Rpf3ResourceFileEntry();

            br.BaseStream.Seek(-12L, SeekOrigin.Current);
            entry.Archive = archive;
            entry.Read(br);

            return entry;
        }

        public string GetFilePath()
        {
            var name = JenkIndex.TryGetStringNoCollision(NameOffset);
            if (name != string.Empty)
            {
                var idx = name.LastIndexOf('.');
                if (idx < 0)
                {
                    return name;
                }
                return name;
            }
            return $"0x{NameOffset.Hash:X}";
        }

        public override string ToString()
        {
            return Path;
        }
    }

    [TC(typeof(EXP))]
    public class Rpf3DirectoryEntry : Rpf3Entry, GameArchiveDirectory
    {
        public int EntriesIndex { get; set; }
        public int EntriesCount { get; set; }

        public List<GameArchiveDirectory> Directories { get; set; } = new List<GameArchiveDirectory>();
        public List<GameArchiveFileInfo> Files { get; set; } = new List<GameArchiveFileInfo>();

        public override void Read(BinaryReader r)
        {
            NameOffset = r.ReadUInt32();
            Flags = r.ReadUInt32();
            EntriesIndex = r.ReadInt32() & int.MaxValue;
            EntriesCount = r.ReadInt32() & 268435455;
            IsDirectory = true;

            if (NameOffset == 0)
            {
                Name = "root";
            }
            else
            {
                Path = GetFilePath();
                Name = System.IO.Path.GetFileName(Path);
            }
        }

        public override void Write(BinaryWriter w)
        {
            w.Write(NameOffset);
            w.Write(Flags);
            w.Write((int)(2147483648L | (EntriesIndex & int.MaxValue)));
            w.Write(EntriesCount & 268435455);
        }

        public override string ToString()
        {
            return "Directory: " + Path;
        }
    }

    [TC(typeof(EXP))] public abstract class Rpf3FileEntry : Rpf3Entry, GameArchiveFileInfo
    {
        public bool IsArchive { get => NameLower?.EndsWith(".rpf") ?? false; }
        public int SizeInArchive { get; set; }
        public uint Flag { get; set; }
        public bool IsResource { get; set; }
        public bool IsCompressed { get; set; }
        public Rpf3ResourceType ResourceType
        {
            get => (Rpf3ResourceType)((ulong)Offset & byte.MaxValue);
            set => Offset = (Offset & -256L) | (byte)value;
        }

        public override void Read(BinaryReader r)
        {
            NameOffset = r.ReadUInt32();
            Size = r.ReadInt32();
        }

        public override void Write(BinaryWriter w)
        {
            w.Write(NameOffset);
            w.Write((int)Size);
        }

        public void SetOffset(long offset)
        {
            if (IsResource)
                Offset = offset | (byte)ResourceType;
            else
                Offset = offset;
        }

        public long GetOffset()
        {
            return IsResource ? (Offset & 2147483392L) : (Offset & int.MaxValue);
        }

        public abstract long GetFileSize();
    }

    [TC(typeof(EXP))]
    public class Rpf3ResourceFileEntry : Rpf3FileEntry
    {
        public override void Read(BinaryReader r)
        {
            base.Read(r);
            Offset = r.ReadInt32();
            Flag = r.ReadUInt32();

            IsResource = (Flag & 0xC0000000) == 0xC0000000;
            if (!IsResource)
            {
                IsCompressed = (Flag & 0x40000000) != 0;
                SizeInArchive = (int)(Flag & 0xBFFFFFFF);
            }

            IsDirectory = false;
            Entry = this;

            Path = GetFilePath();
            Name = System.IO.Path.GetFileName(Path);

            if (IsResource && Name.StartsWith("0x"))
            {
                switch (ResourceType)
                {
                    case Rpf3ResourceType.Fragment:
                        Name += ".xft";
                        break;
                    case Rpf3ResourceType.BitMap:
                        Name += ".xshp";
                        break;
                    case Rpf3ResourceType.Animation:
                        Name += ".xbtm";
                        break;
                    case Rpf3ResourceType.Flash:
                        Name += ".xsf";
                        break;
                }
            }
        }

        public override void Write(BinaryWriter w)
        {
            base.Write(w);
            SetOffset(Offset);

            if (IsResource)
            {
                w.Write((uint)Offset);
                w.Write(Flag);
            }
            else
            {
                w.Write((uint)Offset);
                var temp = SizeInArchive;

                if (IsCompressed)
                {
                    temp |= 0x40000000;
                }
                w.Write((uint)temp);
            }
        }

        public override long GetFileSize()
        {
            return (Size == 0) ? (GetVirtualSize() + GetPhysicalSize()) : SizeInArchive;
        }

        public int GetVirtualSize()
        {
            return (int)(Flag & 0x7FF) << ((int)((Flag >> 11) & 15) + 8);
        }

        public int GetPhysicalSize()
        {
            return (int)((Flag >> 15) & 0x7FF) << ((int)((Flag >> 26) & 15) + 8);
        }

        public byte[] GetDataFromStream(Stream resourceStream)
        {
            using var br = new DataReader(resourceStream, DataEndianess.BigEndian);
            var rscVersion = br.ReadUInt32();
            var rscType = br.ReadInt32();
            var flagInfo = br.ReadInt32();

            if (rscVersion != 88298322) //RSC5
            {
                return null;
            }

            var unk = br.ReadUInt32();
            var len = br.ReadInt32();
            byte[] data = br.ReadBytes(len);
            int dLen = GetPhysicalSize() + GetVirtualSize();
            return BufferUtil.DecompressLZX(data, dLen);
        }

        public override string ToString()
        {
            return "Resource file: " + Path;
        }
    }

    public enum Rpf3ResourceType
    {
        None = 0,
        BitMap = 1, //xshp
        Animation,
        Texture = 9, //xtd
        Flash = 27, //xsf
        Fragment = 63, //xft

        //xst, stringtable
        //xapk, animations
        //?? = 102, meshes
        //xprp? //also in rdr1
    }
}