using System;
using System.Collections.Generic;
using System.IO;
using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Games.MCLA.RPF3;
using CodeX.Games.MCLA.RSC5;

namespace CodeX.Games.MCLA.Files
{
    public class XtdFile : TexturePack
    {
        public Rsc5TextureDictionary TextureDictionary = null;

        public XtdFile() : base(null) { }
        public XtdFile(GameArchiveFileInfo file) : base(file) { }

        public override void Load(byte[] data)
        {
            if (data == null || data.Length == 0) return;

            int virtualSize = 0;
            int physicalSize = 0;

            if (data.Length >= 12)
            {
                uint magic = BufferUtil.ReadUint(data, 0);
                if (magic == 0x05435352u || magic == 0x52534305u)
                {
                    bool isBigEndian = (magic == 0x52534305u);
                    uint type = isBigEndian ? Rpf3Crypto.Swap(BufferUtil.ReadUint(data, 4)) : BufferUtil.ReadUint(data, 4);
                    uint flag = isBigEndian ? Rpf3Crypto.Swap(BufferUtil.ReadUint(data, 8)) : BufferUtil.ReadUint(data, 8);

                    virtualSize = (int)(flag & 0x7FF) << ((int)((flag >> 11) & 15) + 8);
                    physicalSize = (int)((flag >> 15) & 0x7FF) << ((int)((flag >> 26) & 15) + 8);

                    if ((flag & 0x40000000) != 0)
                    {
                        // Compressed RSC5: read stream length and payload at offset 16
                        int dLen = virtualSize + physicalSize;
                        if (data.Length >= 20)
                        {
                            int streamLen = (int)Rpf3Crypto.Swap(BufferUtil.ReadUint(data, 16));
                            var compressed = new byte[streamLen];
                            System.Buffer.BlockCopy(data, 20, compressed, 0, Math.Min(streamLen, data.Length - 20));
                            data = Rpf3Crypto.DecompressLZX(compressed, dLen);
                        }
                    }
                    else
                    {
                        // Uncompressed RSC5 (strip 16-byte or 12-byte header)
                        int totalSize = virtualSize + physicalSize;
                        int headerSize = (data.Length >= 16 && (data.Length - 16) == totalSize) ? 16 : 12;
                        if (data.Length >= headerSize)
                        {
                            var payload = new byte[data.Length - headerSize];
                            System.Buffer.BlockCopy(data, headerSize, payload, 0, payload.Length);
                            data = payload;
                        }
                    }

                    if (FileInfo is Rpf3ResourceFileEntry rfe)
                    {
                        rfe.Flag = flag;
                        rfe.ResourceType = (Rpf3ResourceType)type;
                        rfe.IsResource = true;
                    }
                }
            }

            var e = FileInfo as Rpf3ResourceFileEntry;
            if (e != null)
            {
                if (virtualSize == 0) virtualSize = e.GetVirtualSize();
                if (physicalSize == 0) physicalSize = e.GetPhysicalSize();
            }

            var r = new Rsc5DataReader(data, virtualSize, physicalSize, DataEndianess.BigEndian)
            {
                FileEntry = e
            };

            TextureDictionary = r.ReadBlock<Rsc5TextureDictionary>();
            Textures = [];

            var textures = TextureDictionary?.Textures.Items;
            var hashes = TextureDictionary?.Hashes.Items;

            if (textures != null && hashes != null)
            {
                for (int i = 0; i < textures.Length; i++)
                {
                    var tex = textures[i];
                    var hash = hashes[i];

                    tex.Name ??= hash.ToString();
                    Textures[tex.Name] = tex;
                }
            }
        }

        public override byte[] Save()
        {
            if (TextureDictionary == null)
            {
                return null;
            }

            var writer = new Rsc5DataWriter();
            writer.WriteBlock(TextureDictionary);
            return writer.Build((uint)Rpf3ResourceType.Texture);
        }

        public override void Read(MetaNodeReader reader)
        {
            TextureDictionary = new();
            TextureDictionary.Read(reader);
            BuildDict(TextureDictionary.Textures.Items);
        }

        public override void Write(MetaNodeWriter writer)
        {
            TextureDictionary?.Write(writer);
        }

        public override void BuildFromTextureList(List<Texture> textures)
        {
            TextureDictionary ??= new Rsc5TextureDictionary();

            var rsc5Textures = new List<Rsc5Texture>();
            if (textures != null)
            {
                foreach (var tex in textures)
                {
                    if (tex is Rsc5Texture rsc5Tex)
                    {
                        rsc5Textures.Add(rsc5Tex);
                    }
                    else if (tex != null)
                    {
                        rsc5Textures.Add(Rsc5Texture.Create(tex));
                    }
                }
            }

            TextureDictionary.Build(rsc5Textures);
            base.BuildFromTextureList(new List<Texture>(rsc5Textures));
        }
    }
}
