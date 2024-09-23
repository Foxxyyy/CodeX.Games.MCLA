using CodeX.Core.Engine;
using CodeX.Core.Numerics;
using CodeX.Core.Utilities;
using System.Numerics;
using System.Security.Cryptography;

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

        public static int SetBit(int val, int bit, bool trueORfalse)
        {
            bool flag = (uint)(val & 1 << bit) > 0U;
            if (trueORfalse)
            {
                if (!flag)
                    return val |= 1 << bit;
            }
            else if (flag)
            {
                return val ^ 1 << bit;
            }
            return val;
        }

        public static int TrailingZeroes(int n)
        {
            int num1 = 1;
            int num2 = 0;

            while (num2 < 32)
            {
                if ((uint)(n & num1) > 0U)
                {
                    return num2;
                }
                ++num2;
                num1 <<= 1;
            }
            return 32;
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

        public static int NumLeftTill(int current, int roundTo)
        {
            int num = Math.Abs(roundTo - current % roundTo);
            return num == roundTo ? 0 : num;
        }

        public static void BufferCopy(Stream target, uint totalLength, Stream source, uint chunkSize = 65535)
        {
            uint num1 = totalLength / chunkSize;
            uint num2 = totalLength % chunkSize;
            uint[] numArray = new uint[(long)num1 + (num2 > 0U ? 1L : 0L)];

            for (int index = 0; (long)index < (long)num1; ++index)
                numArray[index] = chunkSize;

            if (num2 > 0U)
                numArray[numArray.Length - 1] = num2;

            byte[] buffer = new byte[(int)chunkSize];
            for (int index = 0; index < numArray.Length; ++index)
            {
                source.Read(buffer, 0, (int)numArray[index]);
                target.Write(buffer, 0, (int)numArray[index]);
            }
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

        public static float Swap(float value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            return BitConverter.ToSingle(data, 0);
        }

        public static long Swap(long value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
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
            byte[] data = BitConverter.GetBytes(vector.X);
            Array.Reverse(data);
            float newX = BitConverter.ToSingle(data, 0);

            data = BitConverter.GetBytes(vector.Y);
            Array.Reverse(data);
            float newY = BitConverter.ToSingle(data, 0);

            return new Vector2(newX, newY);
        }

        public static Vector3 Swap(Vector3 vector)
        {
            byte[] data = BitConverter.GetBytes(vector.X);
            Array.Reverse(data);
            float newX = BitConverter.ToSingle(data, 0);

            data = BitConverter.GetBytes(vector.Y);
            Array.Reverse(data);
            float newY = BitConverter.ToSingle(data, 0);

            data = BitConverter.GetBytes(vector.Z);
            Array.Reverse(data);
            float newZ = BitConverter.ToSingle(data, 0);

            return new Vector3(newX, newY, newZ);
        }

        public static Vector4 Swap(Vector4 vector)
        {
            byte[] data = BitConverter.GetBytes(vector.X);
            Array.Reverse(data);
            float newX = BitConverter.ToSingle(data, 0);

            data = BitConverter.GetBytes(vector.Y);
            Array.Reverse(data);
            float newY = BitConverter.ToSingle(data, 0);

            data = BitConverter.GetBytes(vector.Z);
            Array.Reverse(data);
            float newZ = BitConverter.ToSingle(data, 0);

            data = BitConverter.GetBytes(vector.W);
            Array.Reverse(data);
            float newW = BitConverter.ToSingle(data, 0);

            return new Vector4(newX, newY, newZ, newW);
        }

        public static BoundingBox Swap(BoundingBox bb)
        {
            Vector3 max = Swap(bb.Maximum);
            Vector3 min = Swap(bb.Minimum);

            BoundingBox newBB = new BoundingBox()
            {
                Maximum = max,
                Minimum = min
            };
            return newBB;
        }

        public static BoundingBox4 Swap(BoundingBox4 bb)
        {
            Vector4 max = Swap(bb.Max);
            Vector4 min = Swap(bb.Min);

            BoundingBox4 newBB = new BoundingBox4()
            {
                Max = max,
                Min = min
            };
            return newBB;
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
                BoundingBox4 v = values[i];
                values[i] = Swap(v);
            }
            return values;
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
        ///Reads UShort2N values and rescales them depending of the LOD level
        ///</summary>
        ///<param name="buffer">The buffer containing the UShort2N values.</param>
        ///<param name="offset">The offset in the buffer where the UShort2N values start.</param>
        ///<returns>An array of rescaled float values.</returns>
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

        public static byte[] ModifyLinearTexture(byte[] data, int width, int height, TextureFormat format)
        {
            var buffer = new byte[data.Length];
            int blockSizeRow = 0, texelPitch = 0;

            switch (format)
            {
                case TextureFormat.L8:
                    blockSizeRow = 2;
                    texelPitch = 1;
                    break;
                case TextureFormat.A8R8G8B8:
                    blockSizeRow = 8;
                    texelPitch = 4;
                    break;
                case TextureFormat.BC1:
                    blockSizeRow = 4;
                    texelPitch = 8;
                    break;
                case TextureFormat.BC2:
                case TextureFormat.BC3:
                    blockSizeRow = 4;
                    texelPitch = 16;
                    break;
            }

            var blockWidth = width / blockSizeRow;
            var blockHeight = height / blockSizeRow;
            for (int j = 0; j < blockHeight; j++)
            {
                for (int i = 0; i < blockWidth; i++)
                {
                    var blockOffset = j * blockWidth + i;
                    var x = XGAddress2DTiledX(blockOffset, blockWidth, texelPitch);
                    var y = XGAddress2DTiledY(blockOffset, blockWidth, texelPitch);
                    var srcOffset = j * blockWidth * texelPitch + i * texelPitch;
                    var destOffset = y * blockWidth * texelPitch + x * texelPitch;
                    Array.Copy(data, srcOffset, buffer, destOffset, texelPitch);
                }
            }
            return buffer;
        }

        public static int XGAddress2DTiledX(int Offset, int Width, int TexelPitch)
        {
            int num1 = Width + 31 & -32;
            int num2 = (TexelPitch >> 2) + (TexelPitch >> 1 >> (TexelPitch >> 2));
            int num3 = Offset << num2;
            int num4 = ((num3 & -4096) >> 3) + ((num3 & 1792) >> 2) + (num3 & 63);
            return (((num4 >> 7 + num2) % (num1 >> 5) << 2) + ((num4 >> 5 + num2 & 2) + (num3 >> 6) & 3) << 3) + (((num4 >> 1 & -16) + (num4 & 15) & (TexelPitch << 3) - 1) >> num2);
        }

        public static int XGAddress2DTiledY(int Offset, int Width, int TexelPitch)
        {
            int num1 = Width + 31 & -32;
            int num2 = (TexelPitch >> 2) + (TexelPitch >> 1 >> (TexelPitch >> 2));
            int num3 = Offset << num2;
            int num4 = ((num3 & -4096) >> 3) + ((num3 & 1792) >> 2) + (num3 & 63);
            return (((((num4 >> (7 + num2)) / (num1 >> 5)) << 2) + ((num4 >> (6 + num2)) & 1) + ((num3 & 2048) >> 10)) << 3) + ((num4 & ((TexelPitch << 6) - 1 & -32)) + ((num4 & 15) << 1) >> 3 + num2 & -2) + ((num4 & 16) >> 4);
        }

        public static byte[] DecodeDXT1(byte[] data, int width, int height)
        {
            byte[] pixData = new byte[width * height * 4];
            int xBlocks = width / 4;
            int yBlocks = height / 4;

            for (int y = 0; y < yBlocks; y++)
            {
                for (int x = 0; x < xBlocks; x++)
                {
                    var blockDataStart = ((y * xBlocks) + x) * 8;
                    var color0 = ((uint)data[blockDataStart + 0] << 8) + data[blockDataStart + 1];
                    var color1 = ((uint)data[blockDataStart + 2] << 8) + data[blockDataStart + 3];
                    uint code = BitConverter.ToUInt32(data, blockDataStart + 4);

                    var r0 = (ushort)(8 * (color0 & 31));
                    var g0 = (ushort)(4 * ((color0 >> 5) & 63));
                    var b0 = (ushort)(8 * ((color0 >> 11) & 31));
                    var r1 = (ushort)(8 * (color1 & 31));
                    var g1 = (ushort)(4 * ((color1 >> 5) & 63));
                    var b1 = (ushort)(8 * ((color1 >> 11) & 31));

                    for (int k = 0; k < 4; k++)
                    {
                        var j = k ^ 1;
                        for (int i = 0; i < 4; i++)
                        {
                            int pixDataStart = (width * (y * 4 + j) * 4) + ((x * 4 + i) * 4);
                            uint codeDec = code & 0x3;

                            switch (codeDec)
                            {
                                case 0:
                                    pixData[pixDataStart + 0] = (byte)r0;
                                    pixData[pixDataStart + 1] = (byte)g0;
                                    pixData[pixDataStart + 2] = (byte)b0;
                                    pixData[pixDataStart + 3] = 255;
                                    break;
                                case 1:
                                    pixData[pixDataStart + 0] = (byte)r1;
                                    pixData[pixDataStart + 1] = (byte)g1;
                                    pixData[pixDataStart + 2] = (byte)b1;
                                    pixData[pixDataStart + 3] = 255;
                                    break;
                                case 2:
                                    pixData[pixDataStart + 3] = 255;
                                    if (color0 > color1)
                                    {
                                        pixData[pixDataStart + 0] = (byte)((2 * r0 + r1) / 3);
                                        pixData[pixDataStart + 1] = (byte)((2 * g0 + g1) / 3);
                                        pixData[pixDataStart + 2] = (byte)((2 * b0 + b1) / 3);
                                    }
                                    else
                                    {
                                        pixData[pixDataStart + 0] = (byte)((r0 + r1) / 2);
                                        pixData[pixDataStart + 1] = (byte)((g0 + g1) / 2);
                                        pixData[pixDataStart + 2] = (byte)((b0 + b1) / 2);
                                    }
                                    break;
                                case 3:
                                    if (color0 > color1)
                                    {
                                        pixData[pixDataStart + 0] = (byte)((r0 + 2 * r1) / 3);
                                        pixData[pixDataStart + 1] = (byte)((g0 + 2 * g1) / 3);
                                        pixData[pixDataStart + 2] = (byte)((b0 + 2 * b1) / 3);
                                        pixData[pixDataStart + 3] = 255;
                                    }
                                    else
                                    {
                                        pixData[pixDataStart + 0] = 0;
                                        pixData[pixDataStart + 1] = 0;
                                        pixData[pixDataStart + 2] = 0;
                                        pixData[pixDataStart + 3] = 0;
                                    }
                                    break;
                            }

                            code >>= 2;
                        }
                    }
                }
            }
            return pixData;
        }

        public static byte[] DecodeDXT5(byte[] data, int width, int height)
        {
            var pixData = new byte[width * height * 4];
            var xBlocks = width / 4;
            var yBlocks = height / 4;

            for (int y = 0; y < yBlocks; y++)
            {
                for (int x = 0; x < xBlocks; x++)
                {
                    var blockDataStart = ((y * xBlocks) + x) * 16;
                    var alphas = new uint[8];
                    ulong alphaMask = 0;

                    alphas[0] = data[blockDataStart + 1];
                    alphas[1] = data[blockDataStart + 0];
                    alphaMask |= data[blockDataStart + 6];
                    alphaMask <<= 8;
                    alphaMask |= data[blockDataStart + 7];
                    alphaMask <<= 8;
                    alphaMask |= data[blockDataStart + 4];
                    alphaMask <<= 8;
                    alphaMask |= data[blockDataStart + 5];
                    alphaMask <<= 8;
                    alphaMask |= data[blockDataStart + 2];
                    alphaMask <<= 8;
                    alphaMask |= data[blockDataStart + 3];

                    // 8-alpha or 6-alpha block
                    if (alphas[0] > alphas[1])
                    {
                        // 8-alpha block: derive the other 6
                        // Bit code 000 = alpha_0, 001 = alpha_1, others are interpolated.
                        alphas[2] = (byte)((6 * alphas[0] + 1 * alphas[1] + 3) / 7);    // bit code 010
                        alphas[3] = (byte)((5 * alphas[0] + 2 * alphas[1] + 3) / 7);    // bit code 011
                        alphas[4] = (byte)((4 * alphas[0] + 3 * alphas[1] + 3) / 7);    // bit code 100
                        alphas[5] = (byte)((3 * alphas[0] + 4 * alphas[1] + 3) / 7);    // bit code 101
                        alphas[6] = (byte)((2 * alphas[0] + 5 * alphas[1] + 3) / 7);    // bit code 110
                        alphas[7] = (byte)((1 * alphas[0] + 6 * alphas[1] + 3) / 7);    // bit code 111
                    }
                    else
                    {
                        // 6-alpha block.
                        // Bit code 000 = alpha_0, 001 = alpha_1, others are interpolated.
                        alphas[2] = (byte)((4 * alphas[0] + 1 * alphas[1] + 2) / 5);    // Bit code 010
                        alphas[3] = (byte)((3 * alphas[0] + 2 * alphas[1] + 2) / 5);    // Bit code 011
                        alphas[4] = (byte)((2 * alphas[0] + 3 * alphas[1] + 2) / 5);    // Bit code 100
                        alphas[5] = (byte)((1 * alphas[0] + 4 * alphas[1] + 2) / 5);    // Bit code 101
                        alphas[6] = 0x00;                                               // Bit code 110
                        alphas[7] = 0xFF;                                               // Bit code 111
                    }

                    var alpha = new byte[4, 4];
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            alpha[j, i] = (byte)alphas[alphaMask & 7];
                            alphaMask >>= 3;
                        }
                    }

                    var color0 = (ushort)((data[blockDataStart + 8] << 8) + data[blockDataStart + 9]);
                    var color1 = (ushort)((data[blockDataStart + 10] << 8) + data[blockDataStart + 11]);
                    var code = BitConverter.ToUInt32(data, blockDataStart + 8 + 4);
                    var r0 = (ushort)(8 * (color0 & 31));
                    var g0 = (ushort)(4 * ((color0 >> 5) & 63));
                    var b0 = (ushort)(8 * ((color0 >> 11) & 31));
                    var r1 = (ushort)(8 * (color1 & 31));
                    var g1 = (ushort)(4 * ((color1 >> 5) & 63));
                    var b1 = (ushort)(8 * ((color1 >> 11) & 31));

                    for (int k = 0; k < 4; k++)
                    {
                        var j = k ^ 1;
                        for (int i = 0; i < 4; i++)
                        {
                            int pixDataStart = (width * (y * 4 + j) * 4) + ((x * 4 + i) * 4);
                            uint codeDec = code & 0x3;

                            pixData[pixDataStart + 3] = alpha[i, j];

                            switch (codeDec)
                            {
                                case 0:
                                    pixData[pixDataStart + 0] = (byte)r0;
                                    pixData[pixDataStart + 1] = (byte)g0;
                                    pixData[pixDataStart + 2] = (byte)b0;
                                    break;
                                case 1:
                                    pixData[pixDataStart + 0] = (byte)r1;
                                    pixData[pixDataStart + 1] = (byte)g1;
                                    pixData[pixDataStart + 2] = (byte)b1;
                                    break;
                                case 2:
                                    if (color0 > color1)
                                    {
                                        pixData[pixDataStart + 0] = (byte)((2 * r0 + r1) / 3);
                                        pixData[pixDataStart + 1] = (byte)((2 * g0 + g1) / 3);
                                        pixData[pixDataStart + 2] = (byte)((2 * b0 + b1) / 3);
                                    }
                                    else
                                    {
                                        pixData[pixDataStart + 0] = (byte)((r0 + r1) / 2);
                                        pixData[pixDataStart + 1] = (byte)((g0 + g1) / 2);
                                        pixData[pixDataStart + 2] = (byte)((b0 + b1) / 2);
                                    }
                                    break;
                                case 3:
                                    if (color0 > color1)
                                    {
                                        pixData[pixDataStart + 0] = (byte)((r0 + 2 * r1) / 3);
                                        pixData[pixDataStart + 1] = (byte)((g0 + 2 * g1) / 3);
                                        pixData[pixDataStart + 2] = (byte)((b0 + 2 * b1) / 3);
                                    }
                                    else
                                    {
                                        pixData[pixDataStart + 0] = 0;
                                        pixData[pixDataStart + 1] = 0;
                                        pixData[pixDataStart + 2] = 0;
                                    }
                                    break;
                            }
                            code >>= 2;
                        }
                    }
                }
            }
            return pixData;
        }
    }
}
