using System.Numerics;
using System.Security.Cryptography;
using CodeX.Core.Engine;
using CodeX.Core.Numerics;
using CodeX.Core.Utilities;

namespace CodeX.Games.MCLA.RPF3
{
    public static class Rpf3Crypto
    {
        public const int VIRTUAL_BASE = 0x50000000;
        public const int PHYSICAL_BASE = 0x60000000;

        static Aes AesAlg;
        static readonly byte[] AES_KEY = new byte[32]
        {
            0xAF, 0x7C, 0xD2, 0xE9, 0xFA, 0xAA, 0x45, 0xFD,
            0x97, 0x28, 0xAC, 0x24, 0x7D, 0xD0, 0xCE, 0x5E,
            0xD6, 0xE4, 0xA1, 0x82, 0xFF, 0xE2, 0x41, 0xDB,
            0x8F, 0xF0, 0x70, 0x3B, 0x62, 0x9C, 0x47, 0x85
        };

        public static bool Init()
        {
            AesAlg = Aes.Create();
            AesAlg.BlockSize = 128;
            AesAlg.KeySize = 256;
            AesAlg.Mode = CipherMode.ECB;
            AesAlg.Key = AES_KEY;
            AesAlg.IV = new byte[16];
            AesAlg.Padding = PaddingMode.None;
            return true;
        }

        public static byte[] DecryptAES(byte[] data)
        {
            var rijndael = Aes.Create();
            rijndael.KeySize = 256;
            rijndael.Key = AES_KEY;
            rijndael.BlockSize = 128;
            rijndael.Mode = CipherMode.ECB;
            rijndael.Padding = PaddingMode.None;

            var buffer = (byte[])data.Clone();
            var length = data.Length & -16;

            if (length > 0)
            {
                var decryptor = rijndael.CreateDecryptor();
                for (var roundIndex = 0; roundIndex < 16; roundIndex++)
                    decryptor.TransformBlock(buffer, 0, length, buffer, 0);
            }
            return buffer;
        }

        public static byte[] EncryptAES(byte[] data)
        {
            var rijndael = Aes.Create();
            rijndael.KeySize = 256;
            rijndael.Key = AES_KEY;
            rijndael.BlockSize = 128;
            rijndael.Mode = CipherMode.ECB;
            rijndael.Padding = PaddingMode.None;

            var buffer = (byte[])data.Clone();
            var length = data.Length & -16;

            if (length > 0)
            {
                var encryptor = rijndael.CreateEncryptor();
                for (var roundIndex = 0; roundIndex < 16; roundIndex++)
                    encryptor.TransformBlock(buffer, 0, length, buffer, 0);
            }
            return buffer;
        }

        public static bool IsVirtualBase(int value)
        {
            return (value & VIRTUAL_BASE) == VIRTUAL_BASE;
        }

        public static bool IsPhysicalBase(int value)
        {
            return (value & PHYSICAL_BASE) == PHYSICAL_BASE;
        }

        public static long RoundUp(long num, long multiple)
        {
            if (multiple == 0L)
            {
                return 0;
            }
            long num1 = multiple / Math.Abs(multiple);
            return (num + multiple - num1) / multiple * multiple;
        }

        public static int Swap(int value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public static uint Swap(uint value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public static JenkHash Swap(JenkHash value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public static short Swap(short value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public static ushort Swap(ushort value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public static float Swap(float value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            return BitConverter.ToSingle(data, 0);
        }

        public static byte[] Swap(byte[] value)
        {
            byte[] data = new byte[value.Length];
            for (int i = 0; i < value.Length; i += 4)
            {
                data[i] = value[i + 3];
                data[i + 1] = value[i + 2];
                data[i + 2] = value[i + 1];
                data[i + 3] = value[i];
            }
            return data;
        }

        public static Vector2 Swap(Vector2 vector)
        {
            return new Vector2(Swap(vector.X), Swap(vector.Y));
        }

        public static Vector3 Swap(Vector3 vector)
        {
            return new Vector3(Swap(vector.X), Swap(vector.Y), Swap(vector.Z));
        }

        public static Vector4 Swap(Vector4 vector)
        {
            return new Vector4(Swap(vector.X), Swap(vector.Y), Swap(vector.Z), Swap(vector.W));
        }

        public static Vector4[] Swap(Vector4[] vectors)
        {
            for (int i = 0; i < vectors.Length; i++)
            {
                vectors[i] = Swap(vectors[i]);
            }
            return vectors;
        }

        public static BoundingBox Swap(BoundingBox bb)
        {
            var max = Swap(bb.Maximum);
            var min = Swap(bb.Minimum);

            return  new BoundingBox()
            {
                Maximum = max,
                Minimum = min
            };
        }

        public static BoundingBox4 Swap(BoundingBox4 bb)
        {
            Vector4 max = Swap(bb.Max);
            Vector4 min = Swap(bb.Min);

            return new BoundingBox4()
            {
                Max = max,
                Min = min
            };
        }

        public static Matrix3x4 Swap(Matrix3x4 m)
        {
            return new Matrix3x4(Swap(m.Row1), Swap(m.Row2), Swap(m.Row3));
        }

        public static uint[] Swap(uint[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                uint v = values[i];
                values[i] = Swap(v);
            }
            return values;
        }

        public static ushort[] Swap(ushort[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                ushort v = values[i];
                values[i] = Swap(v);
            }
            return values;
        }

        public static BoundingBox4[] Swap(BoundingBox4[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                values[i] = Swap(v);
            }
            return values;
        }

        ///<summary>Swap the axis from XYZ to ZXY</summary>
        public static Vector3 ToZXY(Vector3 vec)
        {
            var x = float.IsNaN(vec.X) ? 0.0f : vec.X;
            var y = float.IsNaN(vec.Y) ? 0.0f : vec.Y;
            var z = float.IsNaN(vec.Z) ? 0.0f : vec.Z;
            return new Vector3(z, x, y);
        }

        ///<summary>Swap the axis from XYZ to ZXY</summary>
        public static Vector4 ToZXY(Vector4 vec)
        {
            var x = float.IsNaN(vec.X) ? 0.0f : vec.X;
            var y = float.IsNaN(vec.Y) ? 0.0f : vec.Y;
            var z = float.IsNaN(vec.Z) ? 0.0f : vec.Z;
            var w = float.IsNaN(vec.W) ? 0.0f : vec.W;
            return new Vector4(z, x, y, w);
        }

        ///<summary>Swap the axis from XYZ to ZXY</summary>
        public static BoundingBox4 ToZXY(BoundingBox4 bb)
        {
            var newBB = new BoundingBox()
            {
                Minimum = ToZXY(bb.Min.XYZ()),
                Maximum = ToZXY(bb.Max.XYZ())
            };
            return new BoundingBox4(newBB);
        }

        ///<summary>Swap the axis from ZXY to XYZ</summary>
        public static BoundingBox4 ToXYZ(BoundingBox4 bb)
        {
            var newBB = new BoundingBox()
            {
                Minimum = ToXYZ(bb.Min.XYZ()),
                Maximum = ToXYZ(bb.Max.XYZ())
            };
            return new BoundingBox4(newBB);
        }

        ///<summary>Swap the axis from XYZ to ZXY</summary>
        public static BoundingBox4[] ToXYZ(BoundingBox4[] bb)
        {
            for (int i = 0; i < bb.Length; i++)
            {
                bb[i] = ToXYZ(bb[i]);
            }
            return bb;
        }

        ///<summary>Swap the axis from XYZ to ZXY</summary>
        public static Quaternion ToZXY(Quaternion quat)
        {
            return new Quaternion(quat.Z, quat.X, quat.Y, quat.W);
        }

        ///<summary>Swap the axis from XYZ to ZXY</summary>
        public static Matrix3x4 ToZXY(Matrix3x4 m)
        {
            m.Translation = ToZXY(m.Translation);
            m.Orientation = ToZXY(m.Orientation);
            return m;
        }

        ///<summary>Swap the axis from ZXY to XYZ</summary>
        public static Matrix3x4 ToXYZ(Matrix3x4 m, bool write = false)
        {
            var r1 = ToXYZ(m.Row1);
            var r2 = ToXYZ(m.Row2);
            var r3 = ToXYZ(m.Row3);
            r1.W = write ? NaN() : (float.IsNaN(r1.W) ? 0.0f : r1.W);
            r2.W = write ? NaN() : (float.IsNaN(r2.W) ? 0.0f : r2.W);
            r3.W = write ? NaN() : (float.IsNaN(r3.W) ? 0.0f : r3.W);
            return new Matrix3x4(r1, r2, r3);
        }

        ///<summary>Swap the axis from XYZ to ZXY</summary>
        public static Matrix3x4[] ToZXY(Matrix3x4[] m)
        {
            for (int i = 0; i < m.Length; i++)
            {
                m[i] = ToZXY(m[i]);
            }
            return m;
        }

        ///<summary>Swap the axis from ZXY to XYZ</summary>
        public static Matrix3x4[] ToXYZ(Matrix3x4[] m, bool write = false)
        {
            if (m == null) return null;
            for (int i = 0; i < m.Length; i++)
            {
                m[i] = ToXYZ(m[i], write);
            }
            return m;
        }

        ///<summary>Swap the axis from XYZ to ZXY</summary>
        public static Matrix4x4 ToZXY(Matrix4x4 m, bool write = false)
        {
            var m14 = write ? NaN() : (float.IsNaN(m.M14) ? 0.0f : m.M14);
            var m24 = write ? NaN() : (float.IsNaN(m.M24) ? 0.0f : m.M24);
            var m34 = write ? NaN() : (float.IsNaN(m.M34) ? 0.0f : m.M34);
            var m44 = write ? NaN() : (float.IsNaN(m.M44) ? 0.0f : m.M44);
            var translation = m.Translation;

            return new Matrix4x4(
                m.M11, m.M12, m.M13, m14,
                m.M21, m.M22, m.M23, m24,
                m.M31, m.M32, m.M33, m34,
                translation.Z, translation.X, translation.Y, m44
            );
        }

        ///<summary>Swap the axis from ZXY to XYZ</summary>
        public static Matrix4x4 ToXYZ(Matrix4x4 m, bool write = false)
        {
            var m14 = write ? NaN() : (float.IsNaN(m.M14) ? 0.0f : m.M14);
            var m24 = write ? NaN() : (float.IsNaN(m.M24) ? 0.0f : m.M24);
            var m34 = write ? NaN() : (float.IsNaN(m.M34) ? 0.0f : m.M34);
            var m44 = write ? NaN() : (float.IsNaN(m.M44) ? 0.0f : m.M44);
            var translation = m.Translation;

            return new Matrix4x4(
                m.M11, m.M12, m.M13, m14,
                m.M21, m.M22, m.M23, m24,
                m.M31, m.M32, m.M33, m34,
                translation.Y, translation.Z, translation.X, m44
            );
        }

        ///<summary>Swap the axis from XYZ to ZXY</summary>
        public static Matrix4x4[] ToZXY(Matrix4x4[] m)
        {
            for (int i = 0; i < m.Length; i++)
            {
                m[i] = ToZXY(m[i]);
            }
            return m;
        }

        ///<summary>Swap the axis from ZXY to XYZ</summary>
        public static Matrix4x4[] ToXYZ(Matrix4x4[] m, bool write = false)
        {
            if (m == null) return null;
            for (int i = 0; i < m.Length; i++)
            {
                m[i] = ToXYZ(m[i], write);
            }
            return m;
        }

        ///<summary>
        ///Converts a <see cref="System.Numerics.Vector3" /> from ZXY to XYZ format.
        ///</summary>
        ///<param name="vector">The input Vector3 in ZXY format.</param>
        ///<returns>A new Vector3 in XYZ format.</returns>
        public static Vector3 ToXYZ(Vector3 vector)
        {
            return new Vector3(vector.Y, vector.Z, vector.X);
        }

        ///<summary>
        ///Converts a <see cref="System.Numerics.Vector4" /> from ZXYW to XYZW format.
        ///</summary>
        ///<param name="vector">The input Vector4 in ZXYW format.</param>
        ///<returns>A new Vector4 in XYZW format.</returns>
        public static Vector4 ToXYZ(Vector4 vector)
        {
            return new Vector4(vector.Y, vector.Z, vector.X, (vector.W == 0.0f) ? NaN() : vector.W);
        }

        ///<summary>Returns NaN as 0x0100807F (float.NaN = 0x0000C0FF).</summary>
        public static float NaN()
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(0x7F800001), 0);
        }

        ///<summary>Returns a <see cref="System.Numerics.Vector4" /> with NaN values.</summary>
        public static Vector4 GetVec4NaN()
        {
            return new Vector4(NaN(), NaN(), NaN(), NaN());
        }

        ///<summary>Returns a <see cref="System.Numerics.Matrix4x4" /> with NaN values.</summary>
        public static Matrix4x4 GetMatrix4x4NaN()
        {
            return new Matrix4x4(NaN(), NaN(), NaN(), NaN(), NaN(), NaN(), NaN(), NaN(), NaN(), NaN(), NaN(), NaN(), NaN(), NaN(), NaN(), NaN());
        }

        ///<summary>
        ///Swaps the axis and writes a <see cref="System.Numerics.Vector3" /> at the given offset in a buffer
        ///</summary>
        ///<param name="vec">The Vector3 to be written.</param>
        ///<param name="buffer">The buffer to write to.</param>
        ///<param name="offset">The offset in the buffer where writing starts.</param>
        ///<param name="zxy">Determines whether to swap the axis according to the ZXY (MCLA>CX) or YZX (CX>MCLA) convention. Default is true (ZXY).</param>
        public static void WriteVector3AtIndex(Vector3 vec, byte[] buffer, int offset, bool zxy = true)
        {
            var x = BitConverter.GetBytes(vec.X);
            var y = BitConverter.GetBytes(vec.Y);
            var z = BitConverter.GetBytes(vec.Z);

            if (zxy) //XYZ > ZXY (MCLA to CX)
            {
                Buffer.BlockCopy(z, 0, buffer, offset, sizeof(float));
                Buffer.BlockCopy(x, 0, buffer, offset + 4, sizeof(float));
                Buffer.BlockCopy(y, 0, buffer, offset + 8, sizeof(float));
            }
            else //XYZ > YZX (CX to MCLA)
            {
                Buffer.BlockCopy(y, 0, buffer, offset, sizeof(float));
                Buffer.BlockCopy(z, 0, buffer, offset + 4, sizeof(float));
                Buffer.BlockCopy(x, 0, buffer, offset + 8, sizeof(float));
            }
        }

        ///<summary>
        ///Swaps the axis and writes a <see cref="System.Numerics.Vector4" /> at the given offset in a buffer
        ///</summary>
        ///<param name="vec">The Vector4 to be written.</param>
        ///<param name="buffer">The buffer to write to.</param>
        ///<param name="offset">The offset in the buffer where writing starts.</param>
        ///<param name="zxy">Determines whether to swap the axis according to the ZXY (MCLA>CX) or YZX (CX>MCLA) convention. Default is true (ZXY).</param>
        public static void WriteVector4AtIndex(Vector4 vec, byte[] buffer, int offset, bool zxy = true)
        {
            if (float.IsNaN(vec.W))
            {
                vec.W = 0.0f;
            }

            var x = BitConverter.GetBytes(vec.X);
            var y = BitConverter.GetBytes(vec.Y);
            var z = BitConverter.GetBytes(vec.Z);
            var w = BitConverter.GetBytes(vec.W);

            if (zxy) //XYZ > ZXY (MCLA to CX)
            {
                Buffer.BlockCopy(z, 0, buffer, offset, sizeof(float));
                Buffer.BlockCopy(x, 0, buffer, offset + 4, sizeof(float));
                Buffer.BlockCopy(y, 0, buffer, offset + 8, sizeof(float));
                Buffer.BlockCopy(w, 0, buffer, offset + 12, sizeof(float));
            }
            else //XYZ > YZX (CX to MCLA)
            {
                Buffer.BlockCopy(y, 0, buffer, offset, sizeof(float));
                Buffer.BlockCopy(z, 0, buffer, offset + 4, sizeof(float));
                Buffer.BlockCopy(x, 0, buffer, offset + 8, sizeof(float));
                Buffer.BlockCopy(w, 0, buffer, offset + 12, sizeof(float));
            }
        }

        ///<summary>
        ///Rescales <see cref="CodeX.Core.Numerics.Half2" /> values
        ///</summary>
        ///<param name="val">The Half2 value to be rescaled.</param>
        ///<param name="scale">The scaling factor.</param>
        ///<returns>A new <see cref="Vector2" /> with the rescaled values.</returns>
        public static Vector2 RescaleHalf2(Half2 val, float scale)
        {
            return new Half2((float)val.X * scale, (float)val.Y * scale);
        }

        ///<summary>
        ///Reads UShort2N array and rescales values depending of the LOD level
        ///</summary>
        ///<param name="buffer">The buffer containing the UShort2N values.</param>
        ///<param name="offset">The offset in the buffer where the UShort2N values start.</param>
        ///<returns>The rescaled float array.</returns>
        public static float[] ReadRescaleUShort2N(byte[] buffer, int offset)
        {
            var xBuf = BufferUtil.ReadArray<byte>(buffer, offset, 2);
            var yBuf = BufferUtil.ReadArray<byte>(buffer, offset + 2, 2);
            var xVal = BitConverter.ToUInt16(xBuf, 0) * 3.05185094e-005f;
            var yVal = BitConverter.ToUInt16(yBuf, 0) * 3.05185094e-005f;
            var values = new float[2] { xVal, yVal };
            BufferUtil.WriteArray(buffer, offset, BitConverter.GetBytes((ushort)(xVal / 3.05185094e-005f)));
            BufferUtil.WriteArray(buffer, offset + 2, BitConverter.GetBytes((ushort)(yVal / 3.05185094e-005f)));
            return values;
        }

        public static byte[] UnswizzleXbox360Data(byte[] data, int width, int height, TextureFormat format)
        {
            int texelPitch, blockSizeRow;
            switch (format)
            {
                case TextureFormat.L8:
                case TextureFormat.A8R8G8B8:
                    return data;
                case TextureFormat.BC1:
                    blockSizeRow = 4;
                    texelPitch = 8;
                    break;
                case TextureFormat.BC2:
                case TextureFormat.BC3:
                    blockSizeRow = 4;
                    texelPitch = 16;
                    break;
                default:
                    throw new NotImplementedException("Unsupported format for compression");
            }

            //Calculate block dimensions
            static int getVirtualSize(int size)
            {
                if ((size % 128 != 0) && size < 128)
                {
                    return 128;
                }
                return size;
            }
             
            //Calculate virtual block dimensions
            var virtualWidth = getVirtualSize(width);
            var virtualHeight = getVirtualSize(height);
            var virtualBlockWidth = virtualWidth / blockSizeRow;
            var virtualBlockHeight = virtualHeight / blockSizeRow;
            var unswizzledBuffer = new byte[data.Length];

            //Perform unswizzling
            for (int j = 0; j < virtualBlockHeight; j++)
            {
                for (int i = 0; i < virtualBlockWidth; i++)
                {
                    var blockOffset = j * virtualBlockWidth + i;
                    var x = XGAddress2DTiledX(blockOffset, virtualBlockWidth, texelPitch);
                    var y = XGAddress2DTiledY(blockOffset, virtualBlockWidth, texelPitch);

                    var srcOffset = j * virtualBlockWidth * texelPitch + i * texelPitch; //Source offset from the swizzled texture data
                    var destOffset = y * virtualBlockWidth * texelPitch + x * texelPitch; //Destination offset in the unswizzled buffer
                    Array.Copy(data, srcOffset, unswizzledBuffer, destOffset, texelPitch);
                }
            }

            //Swap the DXT color & alpha bytes
            SwapDXTData(unswizzledBuffer, virtualWidth, virtualHeight, format);

            //If the texture uses virtual dimensions, remove the extra padding
            if (width < 128 || height < 128)
            {
                //Fit the texture to the actual dimensions (width, height)
                var actualBlockWidth = width / blockSizeRow;
                var actualBlockHeight = height / blockSizeRow;
                var trimmedBuffer = new byte[actualBlockWidth * actualBlockHeight * texelPitch];

                //Copy the relevant part of the unswizzled buffer into a trimmed buffer
                for (int j = 0; j < actualBlockHeight; j++)
                {
                    var srcOffset = j * virtualBlockWidth * texelPitch;
                    var destOffset = j * actualBlockWidth * texelPitch;
                    Array.Copy(unswizzledBuffer, srcOffset, trimmedBuffer, destOffset, actualBlockWidth * texelPitch);
                }
                unswizzledBuffer = trimmedBuffer;
            }
            return unswizzledBuffer;
        }

        //Translates a linear texture memory offset to a 2D tiled X address
        //Calculates the swizzled address by breaking the texture memory into tiles and reordering it
        public static int XGAddress2DTiledX(int offset, int width, int texelPitch)
        {
            int alignedWidth = (width + 31) & ~31;

            int logBpp = (texelPitch >> 2) + ((texelPitch >> 1) >> (texelPitch >> 2));
            int offsetB = offset << logBpp;
            int offsetT = ((offsetB & ~4095) >> 3) + ((offsetB & 1792) >> 2) + (offsetB & 63);
            int offsetM = offsetT >> (7 + logBpp);

            int macroX = (offsetM % (alignedWidth >> 5)) << 2;
            int tile = (((offsetT >> (5 + logBpp)) & 2) + (offsetB >> 6)) & 3;
            int macro = (macroX + tile) << 3;
            int micro = ((((offsetT >> 1) & ~15) + (offsetT & 15)) & ((texelPitch << 3) - 1)) >> logBpp;

            return macro + micro;
        }

        //Translates a linear texture memory offset to a 2D tiled Y address
        //Calculates the swizzled address by breaking the texture memory into tiles and reordering it
        public static int XGAddress2DTiledY(int offset, int width, int texelPitch)
        {
            int alignedWidth = (width + 31) & ~31;

            int logBpp = (texelPitch >> 2) + ((texelPitch >> 1) >> (texelPitch >> 2));
            int offsetB = offset << logBpp;
            int offsetT = ((offsetB & ~4095) >> 3) + ((offsetB & 1792) >> 2) + (offsetB & 63);
            int offsetM = offsetT >> (7 + logBpp);

            int macroY = (offsetM / (alignedWidth >> 5)) << 2;
            int tile = ((offsetT >> (6 + logBpp)) & 1) + (((offsetB & 2048) >> 10));
            int macro = (macroY + tile) << 3;
            int micro = (((offsetT & ((texelPitch << 6) - 1) & ~31) + ((offsetT & 15) << 1)) >> (3 + logBpp)) & ~1;

            return macro + micro + ((offsetT & 16) >> 4);
        }

        public static void SwapDXTData(byte[] data, int width, int height, TextureFormat format)
        {
            var xBlocks = width / 4;
            var yBlocks = height / 4;

            for (int y = 0; y < yBlocks; y++)
            {
                for (int x = 0; x < xBlocks; x++)
                {
                    //Calculate the starting position of the block
                    var blockDataStart = ((y * xBlocks) + x) * ((format == TextureFormat.BC1) ? 8 : 16);

                    //Swap the color & alpha bytes
                    (data[blockDataStart + 1], data[blockDataStart + 0]) = (data[blockDataStart + 0], data[blockDataStart + 1]);
                    (data[blockDataStart + 2], data[blockDataStart + 3]) = (data[blockDataStart + 3], data[blockDataStart + 2]);
                    (data[blockDataStart + 4], data[blockDataStart + 5]) = (data[blockDataStart + 5], data[blockDataStart + 4]);
                    (data[blockDataStart + 6], data[blockDataStart + 7]) = (data[blockDataStart + 7], data[blockDataStart + 6]);

                    if (format == TextureFormat.BC3)
                    {
                        (data[blockDataStart + 9], data[blockDataStart + 8]) = (data[blockDataStart + 8], data[blockDataStart + 9]);
                        (data[blockDataStart + 11], data[blockDataStart + 10]) = (data[blockDataStart + 10], data[blockDataStart + 11]);
                        (data[blockDataStart + 13], data[blockDataStart + 12]) = (data[blockDataStart + 12], data[blockDataStart + 13]);
                        (data[blockDataStart + 15], data[blockDataStart + 14]) = (data[blockDataStart + 14], data[blockDataStart + 15]);
                    }
                }
            }
        }
    }
}
