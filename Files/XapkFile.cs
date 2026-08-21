using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace CodeX.Games.MCLA.Files
{
    //.xapk - mcAnimPack, a RAGE animation pack. Resource type 1 (Generic), so by the time it gets
    //here Rpf3File has already LZX-decompressed it and what we hold is the raw virtual segment with
    //base 0x50000000: an image of the classes as they sat in memory on the console, pointers and
    //all, big endian.
    //
    //Layout read out of the retail XEX - crAnimation::Serialize (sub_82505570),
    //crAnimTrack::Serialize (sub_8253B5F0), the channel factory (sub_8253C238) and the quantized
    //channel's evaluator (sub_82546120):
    //
    //  mcAnimPack   +8    192 crAnimation pointers, +776 count
    //               +780  192 atString pointers {char* text, u16 len, u16 capacity}, +1548 count
    //  crAnimation  +4 u16 flags, +8 u16 frame count, +10 u16 frames per chunk,
    //               +12 float duration, +16 u32 signature,
    //               +20 chunk array +24 u16 count, +32 track array +36 u16 count
    //  crAnimTrack  +0 u8 track id, +1 u8 type, +2 u16 bone id,
    //               +8 one channel group per chunk, +12 u16 count
    //  group        +4..+16 up to four channel pointers, +20 u32 count
    //
    //Chunks overlap by one frame: chunk c covers frames [c*framesPerChunk .. c*framesPerChunk+n-1]
    //and its last frame is the next chunk's first.
    public class XapkFile : PiecePack
    {
        public const uint VirtualBase = 0x50000000;
        public const int MaxAnimations = 192;

        public XapkAnimation[] Anims { get; set; }

        public XapkFile()
        {
        }

        public XapkFile(GameArchiveFileInfo info) : base(info)
        {
        }

        public override void Load(byte[] data)
        {
            Anims = [];
            Pieces = [];   //a pack has no geometry of its own; the preview gets its model separately
            if (data == null || data.Length < 1552)
            {
                LoadException = new Exception($"Not an animation pack: {data?.Length ?? 0} bytes");
                return;
            }

            try
            {
                var r = new XapkReader(data);
                var count = (int)r.U32(776);
                var nameCount = (int)r.U32(1548);
                if (count < 0 || count > MaxAnimations)
                {
                    LoadException = new Exception($"Bad animation count {count}");
                    return;
                }

                var anims = new List<XapkAnimation>(count);
                for (int i = 0; i < count; i++)
                {
                    var animOff = r.Ptr(8 + i * 4);
                    string name = null;
                    if (i < nameCount)
                    {
                        var strObj = r.Ptr(780 + i * 4);
                        if (strObj >= 0) name = r.String(r.Ptr(strObj));
                    }
                    if (animOff >= 0) anims.Add(new XapkAnimation(r, animOff, name ?? $"anim_{i}"));
                }
                Anims = [.. anims];
            }
            catch (Exception ex)
            {
                LoadException = ex;
            }
        }

        public override byte[] Save()
        {
            throw new NotImplementedException();
        }

        public override void Read(MetaNodeReader reader)
        {
            throw new NotImplementedException();
        }

        public override void Write(MetaNodeWriter writer)
        {
            writer.WriteInt32("AnimationCount", Anims?.Length ?? 0);
        }

        private static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        private static string V(Vector3 v) => $"{F(v.X)}, {F(v.Y)}, {F(v.Z)}";

        public string ToText()
        {
            var sb = new StringBuilder();
            var name = FileInfo?.Name ?? FilePath ?? "(unnamed)";
            sb.AppendLine($"; {name}");

            if (LoadException != null)
            {
                sb.AppendLine($"; {LoadException.Message}");
                return sb.ToString();
            }

            sb.AppendLine($"; RAGE animation pack, {Anims.Length} animations");
            sb.AppendLine();

            foreach (var a in Anims)
            {
                sb.AppendLine(a.Name);
                sb.AppendLine($"    {a.Duration.ToString("0.###", CultureInfo.InvariantCulture)}s, " +
                              $"{a.FrameCount} frames, {a.FramesPerChunk} per chunk, " +
                              $"{a.Tracks.Count} tracks, flags 0x{a.Flags:X4}");

                //Group the tracks by what they animate - that is the useful summary, one line per
                //track would be hundreds of lines on a character animation.
                var kinds = new Dictionary<string, int>();
                var bones = new HashSet<int>();
                foreach (var t in a.Tracks)
                {
                    var k = t.KindName;
                    kinds.TryGetValue(k, out var n);
                    kinds[k] = n + 1;
                    bones.Add(t.BoneId);
                }
                var parts = new List<string>();
                foreach (var kv in kinds) parts.Add($"{kv.Key} x{kv.Value}");
                sb.AppendLine($"    {string.Join(", ", parts)}");
                sb.AppendLine($"    {bones.Count} bones/DOFs");

                //A camera track is short and is what a cutscene reader actually wants to see.
                var last = Math.Max(0, a.FrameCount - 1);
                foreach (var t in a.Tracks)
                {
                    if (t.TrackId != XapkTrack.TrackCameraFov) continue;
                    sb.AppendLine($"    camera fov: {F(t.EvaluateFloat(0))} .. " +
                                  $"{F(t.EvaluateFloat(a.FrameCount / 2))} .. {F(t.EvaluateFloat(last))} degrees");
                }

                //Where the thing actually travels: the mover track when there is one, otherwise
                //the root bone's own translation.
                XapkTrack move = null;
                foreach (var t in a.Tracks)
                {
                    if (t.TrackId == XapkTrack.TrackMoverTranslation) { move = t; break; }
                    if (move == null && t.TrackId == XapkTrack.TrackTranslation && t.BoneId == 0) move = t;
                }
                if (move != null)
                {
                    var p0 = move.EvaluateGameVector(0);
                    var p1 = move.EvaluateGameVector(last);
                    var what = move.TrackId == XapkTrack.TrackMoverTranslation ? "mover" : "root";
                    sb.AppendLine($"    {what} moves ({V(p0)}) -> ({V(p1)})");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }

    //Big endian reader over the virtual segment. Offsets are file offsets; Ptr() turns a stored
    //pointer into one, and returns -1 for anything that lands outside the buffer.
    public class XapkReader
    {
        public byte[] Data;

        public XapkReader(byte[] data)
        {
            Data = data;
        }

        public byte U8(int o) => Data[o];

        public ushort U16(int o) => (ushort)((Data[o] << 8) | Data[o + 1]);

        public uint U32(int o) =>
            (uint)((Data[o] << 24) | (Data[o + 1] << 16) | (Data[o + 2] << 8) | Data[o + 3]);

        public float F32(int o) => BitConverter.Int32BitsToSingle((int)U32(o));

        public int Ptr(int o)
        {
            var v = U32(o);
            if (v < XapkFile.VirtualBase) return -1;
            var off = (int)(v - XapkFile.VirtualBase);
            return off >= 0 && off < Data.Length ? off : -1;
        }

        public string String(int o)
        {
            if (o < 0) return null;
            int end = o;
            while (end < Data.Length && Data[end] != 0) end++;
            return Encoding.ASCII.GetString(Data, o, end - o);
        }
    }

    //Name, FrameCount, Duration, Looping and Interpolate come from CodeX's own Animation, so the
    //engine's animation plumbing can drive one of these directly.
    public class XapkAnimation : Animation
    {
        public ushort Flags { get; set; }
        public int FramesPerChunk { get; set; }
        public uint Signature { get; set; }
        public List<XapkTrack> Tracks { get; set; }

        public float FrameRate => Duration > 0 ? (FrameCount - 1) / Duration : 30.0f;

        public XapkAnimation(XapkReader r, int off, string name)
        {
            Name = name;
            Flags = r.U16(off + 4);
            FrameCount = r.U16(off + 8);
            FramesPerChunk = r.U16(off + 10);
            Duration = r.F32(off + 12);
            Signature = r.U32(off + 16);
            Looping = false;

            Tracks = [];
            var n = r.U16(off + 36);
            var arr = r.Ptr(off + 32);
            if (arr < 0) return;
            for (int i = 0; i < n; i++)
            {
                var t = r.Ptr(arr + i * 4);
                if (t >= 0) Tracks.Add(new XapkTrack(r, t, FramesPerChunk));
            }
        }

        //How much of this animation actually moves. A cutscene ships two variants of each actor:
        //the body animation, and an "_IM" one that only drives the face - constant channels on the
        //root and the spine, samples on the lip and eyebrow DOFs. Without this the two look
        //identical in a list and the face-only one looks like playback is broken.
        private int _movingTracks = -1;
        public int MovingTrackCount
        {
            get
            {
                if (_movingTracks >= 0) return _movingTracks;
                var n = 0;
                foreach (var t in Tracks)
                {
                    var moves = false;
                    foreach (var group in t.Chunks)
                    {
                        foreach (var c in group)
                        {
                            if (c == null) continue;
                            if (c.Type is XapkChannel.TypeQuantized or XapkChannel.TypeRawFloats) { moves = true; break; }
                        }
                        if (moves) break;
                    }
                    if (moves) n++;
                }
                _movingTracks = n;
                return n;
            }
        }

        //Body tracks are the ones on the bones a skeleton actually poses (translation, rotation and
        //scale); the rest are face DOF floats.
        private int _movingBodyTracks = -1;
        public int MovingBodyTrackCount
        {
            get
            {
                if (_movingBodyTracks >= 0) return _movingBodyTracks;
                var n = 0;
                foreach (var t in Tracks)
                {
                    if (t.TrackId is not (XapkTrack.TrackTranslation or XapkTrack.TrackRotation or 2)) continue;
                    foreach (var group in t.Chunks)
                    {
                        var moves = false;
                        foreach (var c in group)
                        {
                            if (c == null) continue;
                            if (c.Type is XapkChannel.TypeQuantized or XapkChannel.TypeRawFloats) { moves = true; break; }
                        }
                        if (moves) { n++; break; }
                    }
                }
                _movingBodyTracks = n;
                return n;
            }
        }

        //CodeX's Animation contract: one Vector4 per track, blended between the two frames the
        //engine picked. Rotations get normalized here because the blend is a plain lerp.
        public override Vector4 Evaluate(in AnimationFramePosition frame, int track)
        {
            if (Tracks == null || track < 0 || track >= Tracks.Count) return Vector4.Zero;
            var t = Tracks[track];
            var a = t.EvaluateVector4(frame.Frame0);
            var b = t.EvaluateVector4(frame.Frame1);
            var v = a * frame.Alpha0 + b * frame.Alpha1;
            if (t.Type == XapkTrack.TypeQuaternion)
            {
                var len = v.Length();
                if (len > 0) v /= len;
            }
            return v;
        }

        public override string ToString() => $"{Name} ({Duration:0.##}s)";
    }

    public class XapkTrack
    {
        //Track ids seen in the shipped packs. 0/1 animate a bone, 5/6 are the mover (root motion)
        //of an entity, 10 is the cutscene camera's field of view in degrees, 21 and 23 are the
        //0..1 float tracks a character animation carries per face DOF.
        public const int TrackTranslation = 0;
        public const int TrackRotation = 1;
        public const int TrackMoverTranslation = 5;
        public const int TrackMoverRotation = 6;
        public const int TrackCameraFov = 10;

        //Value type, from the track's second byte.
        public const int TypeVector3 = 0;
        public const int TypeQuaternion = 1;
        public const int TypeFloat = 2;

        public int TrackId { get; set; }
        public int Type { get; set; }
        public int BoneId { get; set; }
        public int FramesPerChunk { get; set; }
        public List<XapkChannel[]> Chunks { get; set; }

        public XapkTrack(XapkReader r, int off, int framesPerChunk)
        {
            TrackId = r.U8(off);
            Type = r.U8(off + 1);
            BoneId = r.U16(off + 2);
            FramesPerChunk = Math.Max(1, framesPerChunk);

            Chunks = [];
            var n = r.U16(off + 12);
            var arr = r.Ptr(off + 8);
            if (arr < 0) return;
            for (int c = 0; c < n; c++)
            {
                var g = r.Ptr(arr + c * 4);
                if (g < 0) continue;
                var cnt = (int)Math.Min(r.U32(g + 20), 4);
                var chans = new XapkChannel[cnt];
                for (int k = 0; k < cnt; k++)
                {
                    var ch = r.Ptr(g + 4 + k * 4);
                    chans[k] = ch >= 0 ? new XapkChannel(r, ch) : null;
                }
                Chunks.Add(chans);
            }
        }

        public string KindName
        {
            get
            {
                var what = TrackId switch
                {
                    TrackTranslation => "translation",
                    TrackRotation => "rotation",
                    TrackMoverTranslation => "mover translation",
                    TrackMoverRotation => "mover rotation",
                    TrackCameraFov => "camera fov",
                    _ => $"track {TrackId}",
                };
                return what;
            }
        }

        private XapkChannel[] Locate(int frame, out int local)
        {
            local = 0;
            if (Chunks.Count == 0) return null;
            if (frame < 0) frame = 0;
            var c = frame / FramesPerChunk;
            if (c >= Chunks.Count) c = Chunks.Count - 1;
            local = frame - c * FramesPerChunk;
            return Chunks[c];
        }

        //One component of the track at an integer frame. A group holds one channel per component,
        //except for the constant vector and quaternion channels, which are a single channel that
        //answers for every component.
        public float Component(int frame, int comp)
        {
            var g = Locate(frame, out var local);
            if (g == null || g.Length == 0) return 0.0f;
            if (g.Length == 1 && g[0] != null && g[0].IsVector) return g[0].Constant(comp);
            var ch = comp < g.Length ? g[comp] : null;
            return ch?.Value(local) ?? 0.0f;
        }

        public float EvaluateFloat(int frame) => Component(frame, 0);

        //In CodeX's axis order, like everything else loaded out of the resource.
        public Vector3 EvaluateVector(int frame)
        {
            var v = EvaluateVector4(frame);
            return new Vector3(v.X, v.Y, v.Z);
        }

        public Quaternion EvaluateQuaternion(int frame)
        {
            var v = EvaluateVector4(frame);
            var q = new Quaternion(v.X, v.Y, v.Z, v.W);
            var len = q.Length();
            return len > 0 ? Quaternion.Multiply(q, 1.0f / len) : Quaternion.Identity;
        }

        //As the game stored it, for reports that talk about world coordinates.
        public Vector3 EvaluateGameVector(int frame)
        {
            var v = EvaluateRaw(frame);
            return new Vector3(v.X, v.Y, v.Z);
        }

        //Everything CodeX loads out of an RSC5 comes through Rsc5DataReader, which swaps every
        //vector from the game's (x, y, z) to (z, x, y) - so the skeleton and the meshes live in
        //that space. Animations are read straight out of the pack, so they have to be swapped the
        //same way or the pose lands on the wrong axes: heads tilt up and limbs twist.
        public Vector4 EvaluateVector4(int frame) => Type switch
        {
            TypeQuaternion => new Vector4(Component(frame, 2), Component(frame, 0),
                                          Component(frame, 1), Component(frame, 3)),
            TypeVector3 => new Vector4(Component(frame, 2), Component(frame, 0),
                                       Component(frame, 1), 0.0f),
            _ => new Vector4(Component(frame, 0), 0.0f, 0.0f, 0.0f),
        };

        //The same values without the axis swap, for reading the animation as the game stored it.
        public Vector4 EvaluateRaw(int frame) => new(Component(frame, 0), Component(frame, 1),
                                                     Component(frame, 2), Component(frame, 3));

        public override string ToString() => $"{KindName} bone {BoneId}";
    }

    //A single animated component. Five kinds show up in the shipped packs; the type byte is at +5
    //and is the same id the game's channel factory (sub_8253C238) dispatches on.
    public class XapkChannel
    {
        public const int TypeRawFloats = 1;
        public const int TypeConstantFloat = 4;
        public const int TypeQuantized = 6;
        public const int TypeConstantQuaternion = 9;
        public const int TypeConstantVector3 = 13;

        public int Type { get; set; }
        public float[] Values { get; set; }   //raw samples, or the constant vector
        public int BitData { get; set; }      //file offset of the packed bit stream
        public int Bits { get; set; }
        public int SampleCount { get; set; }
        public float Scale { get; set; }
        public float Offset { get; set; }
        private readonly XapkReader _r;

        public bool IsVector => Type is TypeConstantQuaternion or TypeConstantVector3;

        public XapkChannel(XapkReader r, int off)
        {
            _r = r;
            Type = r.U8(off + 5);
            switch (Type)
            {
                case TypeConstantFloat:
                    Values = [r.F32(off + 8)];
                    break;
                case TypeRawFloats:
                {
                    var p = r.Ptr(off + 8);
                    var n = r.U16(off + 12);
                    Values = new float[n];
                    for (int i = 0; i < n && p >= 0; i++) Values[i] = r.F32(p + i * 4);
                    break;
                }
                case TypeQuantized:
                    BitData = r.Ptr(off + 8);
                    Bits = (int)r.U32(off + 12);
                    SampleCount = (int)r.U32(off + 16);
                    Scale = r.F32(off + 20);
                    Offset = r.F32(off + 24);
                    break;
                case TypeConstantQuaternion:
                case TypeConstantVector3:
                {
                    var p = r.Ptr(off + 8);
                    var n = Type == TypeConstantQuaternion ? 4 : 3;
                    Values = new float[n];
                    for (int i = 0; i < n && p >= 0; i++) Values[i] = r.F32(p + i * 4);
                    break;
                }
                default:
                    throw new Exception($"Unknown animation channel type {Type}");
            }
        }

        public float Constant(int comp)
        {
            if (Values == null || Values.Length == 0) return 0.0f;
            return comp < Values.Length ? Values[comp] : 0.0f;
        }

        public float Value(int frame)
        {
            switch (Type)
            {
                case TypeConstantFloat:
                    return Values[0];
                case TypeRawFloats:
                    if (Values.Length == 0) return 0.0f;
                    return Values[Math.Clamp(frame, 0, Values.Length - 1)];
                case TypeConstantQuaternion:
                case TypeConstantVector3:
                    return Constant(0);
                case TypeQuantized:
                    return Dequantize(Math.Clamp(frame, 0, SampleCount - 1));
                default:
                    return 0.0f;
            }
        }

        //The bit stream is a run of big endian dwords, samples packed low bit first: the sample at
        //index i starts at bit i*Bits, and a sample straddling a dword boundary continues in the
        //low bits of the next one. Validated against every quantized quaternion in the shipped
        //packs - all 112960 of them come out unit length.
        private float Dequantize(int index)
        {
            if (BitData < 0 || Bits <= 0 || Bits > 32 || SampleCount <= 0) return Offset;
            var bitpos = (long)Bits * index;
            var b = BitData + (int)((bitpos >> 3) & ~3);
            if (b < 0 || b + 8 > _r.Data.Length) return Offset;
            ulong pair = ((ulong)_r.U32(b + 4) << 32) | _r.U32(b);
            var mask = Bits == 32 ? uint.MaxValue : (1u << Bits) - 1;
            var raw = (uint)((pair >> (int)(bitpos & 0x1F)) & mask);
            return raw * Scale + Offset;
        }

        public override string ToString() => Type switch
        {
            TypeQuantized => $"quantized {Bits} bits x{SampleCount}",
            TypeRawFloats => $"raw x{Values.Length}",
            TypeConstantFloat => $"constant {Values[0]}",
            TypeConstantQuaternion => "constant quaternion",
            TypeConstantVector3 => "constant vector",
            _ => $"type {Type}",
        };
    }
}
