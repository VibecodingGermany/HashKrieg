using UnityEngine;

namespace Nova.Presentation.Maps
{
    /// <summary>
    /// Procedural ground textures of the Glutrinne blockout (D-085), built
    /// at Play and marked HideAndDontSave — no texture asset on disk, no
    /// license question, and every fresh clone renders the identical desert
    /// (Assets/_Project/Art/**/*.png is gitignored, so a downloaded texture
    /// would silently vanish there; the procedural ground is the permanent
    /// fallback a CC0 drop-in may later decorate, never replace).
    /// <para>
    /// DETERMINISTIC BY CONSTRUCTION: the noise is value noise over an
    /// integer-lattice hash with a fixed seed — no UnityEngine.Random, no
    /// system clock — and every octave's lattice wraps at its period, so the
    /// sand tile repeats seamlessly under <c>wrapMode = Repeat</c>.
    /// </para>
    /// </summary>
    internal static class GlutrinneTerrainTexture
    {
        /// <summary>Fixed noise seed — changing it reshapes the desert, nothing else.</summary>
        private const uint NoiseSeed = 0x9E3779B9u;

        /// <summary>High-frequency grain octave: lattice cells per tile (512/64 = 8 px per grain cell).</summary>
        private const int GrainPeriod = 64;

        /// <summary>Low-frequency blotch octave against visible tiling: three broad stains per tile.</summary>
        private const int BlotchPeriod = 3;

        /// <summary>Lightness factors of the four sand tones the grain quantizes into.</summary>
        private static readonly float[] ToneFactors = { 0.88f, 0.96f, 1.04f, 1.11f };

        /// <summary>
        /// The repeating sand tile: four sand tones derived from the
        /// Glutrinne palette color, picked by quantized high-frequency value
        /// noise, modulated by a very low-frequency second noise so the
        /// surface reads as dunes and stains instead of a uniform fill.
        /// Grayscale-free: the tones keep the palette's hue, only lightness
        /// varies.
        /// </summary>
        public static Texture2D CreateSandTile(Color sandColor, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float grain = WrappedValueNoise(
                        x / (float)size * GrainPeriod, y / (float)size * GrainPeriod, GrainPeriod);
                    float blotch = WrappedValueNoise(
                        x / (float)size * BlotchPeriod, y / (float)size * BlotchPeriod, BlotchPeriod);

                    int tone = Mathf.Min(ToneFactors.Length - 1, (int)(grain * ToneFactors.Length));
                    float lightness = ToneFactors[tone] * Mathf.Lerp(0.92f, 1.08f, blotch);
                    pixels[y * size + x] = Scale(sandColor, lightness);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: true);
            return texture;
        }

        /// <summary>
        /// The weathered map edge (D-085): a full-map overlay veil whose
        /// alpha rises from nothing to near-solid over the outer
        /// <paramref name="fadeFraction"/> of UV (the caller maps that to
        /// two to three cells), tinting the rim toward the dark rock tone.
        /// The map then reads as embedded in a weathered border instead of
        /// cut off by the old flat dark frame beam. Not tiled — one quad at
        /// map scale, clamped.
        /// </summary>
        public static Texture2D CreateWeatheringVeil(Color edgeColor, int size, float fadeFraction)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float edgeDistance = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    float t = Mathf.Clamp01(edgeDistance / fadeFraction);
                    // Smoothstep falloff, slightly steepened: the weathering
                    // hugs the rim and lets the center fully through.
                    float s = t * t * (3f - 2f * t);
                    float alpha = (1f - s) * (1f - s) * 0.9f;
                    pixels[y * size + x] = new Color(edgeColor.r, edgeColor.g, edgeColor.b, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Value noise on a wrapped lattice: bilinear interpolation of the
        /// hashed corner lattices with smoothstep weights, lattice indices
        /// taken modulo <paramref name="period"/> — sampling across the
        /// 0..period boundary therefore joins seamlessly, which is what makes
        /// the sand tile repeatable without a visible seam.
        /// </summary>
        private static float WrappedValueNoise(float x, float y, int period)
        {
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            float fx = x - ix;
            float fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float a = Lattice01(ix, iy, period);
            float b = Lattice01(ix + 1, iy, period);
            float c = Lattice01(ix, iy + 1, period);
            float d = Lattice01(ix + 1, iy + 1, period);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Hashed lattice value in [0, 1), wrapped at the octave's period, fixed seed.</summary>
        private static float Lattice01(int x, int y, int period)
        {
            int wx = ((x % period) + period) % period;
            int wy = ((y % period) + period) % period;
            unchecked
            {
                uint h = NoiseSeed ^ (uint)(wx * 374761393) ^ (uint)(wy * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        /// <summary>Lightness-scales a palette color, clamped per channel.</summary>
        private static Color32 Scale(Color color, float factor)
        {
            return new Color32(
                (byte)(Mathf.Clamp01(color.r * factor) * 255f),
                (byte)(Mathf.Clamp01(color.g * factor) * 255f),
                (byte)(Mathf.Clamp01(color.b * factor) * 255f),
                255);
        }
    }
}
