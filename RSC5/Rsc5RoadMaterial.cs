using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using System.Collections.Concurrent;

namespace CodeX.Games.MCLA.RSC5
{
    //Road and ground materials don't have a plain albedo map. The game's pixel shader builds the
    //surface colour out of two textures:
    //
    //    r5.xw = DiffuseSampler(TEXCOORD0).yx   // control map: .r = luminance, .g = specular mask
    //    r8    = DecalSampler(TEXCOORD1)        // markings atlas, its alpha is the mask
    //    result = lerp(r5.w, r8.rgb, r8.a)      // grey base with the decal composited over it
    //
    //Blue is never read, which is why the control map looks green on its own - showing it raw is
    //what turned every road and sidewalk green.
    //
    //The base tiles (TEXCOORD0 runs to 20 and beyond) while the decal atlas is placed once per
    //surface (TEXCOORD1 stays inside 0..1), so the two cannot be baked together: doing that
    //repeats the atlas and litters the pavement with manhole covers and lane arrows. Until the
    //renderer can sample a second UV set, only the grey base is reconstructed.
    public static class Rsc5RoadMaterial
    {
        private static readonly ConcurrentDictionary<Texture, Texture> Cache = new();

        public static Texture GetAlbedo(Texture control, Texture decal)
        {
            if (control?.Data == null) return decal;
            return Cache.GetOrAdd(control, key => Build(key, null) ?? key);
        }

        private static Texture Build(Texture control, Texture decal)
        {
            var basePixels = DDSIO.GetPixels(control, 0); //BGRA
            if (basePixels == null) return null;

            var w = control.Width;
            var h = control.Height;
            var outPixels = new byte[w * h * 4];

            byte[] decalPixels = null;
            int dw = 0, dh = 0;
            if (decal?.Data != null)
            {
                decalPixels = DDSIO.GetPixels(decal, 0);
                dw = decal.Width;
                dh = decal.Height;
                if (decalPixels == null || dw <= 0 || dh <= 0) decalPixels = null;
            }

            for (int y = 0; y < h; y++)
            {
                var row = y * w * 4;
                var drow = decalPixels != null ? (y * dh / h) * dw * 4 : 0;
                for (int x = 0; x < w; x++)
                {
                    var o = row + x * 4;
                    if (o + 3 >= basePixels.Length) break;

                    var grey = basePixels[o + 2]; //red channel carries the surface luminance
                    byte r = grey, g = grey, b = grey;

                    if (decalPixels != null)
                    {
                        var d = drow + (x * dw / w) * 4;
                        if (d + 3 < decalPixels.Length)
                        {
                            var a = decalPixels[d + 3];
                            if (a != 0)
                            {
                                b = Mix(b, decalPixels[d], a);
                                g = Mix(g, decalPixels[d + 1], a);
                                r = Mix(r, decalPixels[d + 2], a);
                            }
                        }
                    }

                    outPixels[o] = b;
                    outPixels[o + 1] = g;
                    outPixels[o + 2] = r;
                    outPixels[o + 3] = 255;
                }
            }

            var name = control.Name + (decal != null ? "+" + decal.Name : "");
            return Texture.Create(name, (ushort)w, (ushort)h, TextureFormat.A8R8G8B8, outPixels, false, TextureSamplerPreset.AnisotropicWrap);
        }

        private static byte Mix(byte dst, byte src, byte alpha)
        {
            return (byte)((dst * (255 - alpha) + src * alpha) / 255);
        }
    }
}
