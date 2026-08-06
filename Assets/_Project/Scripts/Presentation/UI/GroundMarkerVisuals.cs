using UnityEngine;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// Shared runtime visual of the graybox HUD ground markers (selection
    /// markers, placement ghost, rally flags): ONE generated 64x64 box
    /// texture (opaque border, translucent fill), ONE unlit transparent
    /// material and a factory for flat ground quads. Sharing keeps every
    /// marker in the scene on a single material/texture pair — the whole
    /// marker layer batches instead of paying one material per component.
    /// <para>
    /// The texture is white with alpha; the actual colour rides a
    /// MaterialPropertyBlock per marker (<see cref="Gameplay.Match.FactionTint.ApplyToPropertyBlock"/>
    /// writes both <c>_BaseColor</c> and <c>_Color</c>), so tinting never
    /// instantiates materials. Everything here is runtime-generated, marked
    /// HideAndDontSave and destroyed via <see cref="DestroyShared"/> from the
    /// owning components' OnDestroy — the project forbids hand-made material
    /// assets in this slice, and the lazy getters simply rebuild after a
    /// teardown, so destruction order between the three consumers cannot
    /// break anything.
    /// </para>
    /// </summary>
    internal static class GroundMarkerVisuals
    {
        private const int TextureSize = 64;
        private const int BorderPixels = 6;

        private static Texture2D _texture;
        private static Material _material;

        /// <summary>The shared box texture: opaque border ring, translucent fill.</summary>
        public static Texture2D MarkerTexture
        {
            get
            {
                if (_texture == null) _texture = CreateTexture();
                return _texture;
            }
        }

        /// <summary>The shared unlit transparent material over <see cref="MarkerTexture"/>.</summary>
        public static Material MarkerMaterial
        {
            get
            {
                if (_material == null) _material = CreateMaterial();
                return _material;
            }
        }

        /// <summary>
        /// A flat ground quad (two triangles) facing up, parented under
        /// <paramref name="parent"/> and carrying the shared marker material.
        /// The collider Unity adds to primitives is destroyed: HUD markers
        /// are pure visuals and world picking projects onto the mathematical
        /// ground plane, never onto colliders.
        /// </summary>
        public static GameObject CreateFlatQuad(string name, Transform parent)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            // Quad lies in the XY plane; pitching 90 degrees lays it flat on
            // the ground with its local Y pointing along world +Z, so
            // localScale (x, y) maps to world (x, z).
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = MarkerMaterial;
            return quad;
        }

        /// <summary>Destroys the shared texture/material; the next access lazily recreates them.</summary>
        public static void DestroyShared()
        {
            if (_texture != null)
            {
                Object.Destroy(_texture);
                _texture = null;
            }
            if (_material != null)
            {
                Object.Destroy(_material);
                _material = null;
            }
        }

        private static Texture2D CreateTexture()
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var border = new Color(1f, 1f, 1f, 0.95f);
            var fill = new Color(1f, 1f, 1f, 0.28f);
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    bool isBorder = x < BorderPixels || y < BorderPixels
                        || x >= TextureSize - BorderPixels || y >= TextureSize - BorderPixels;
                    texture.SetPixel(x, y, isBorder ? border : fill);
                }
            }
            texture.Apply();
            return texture;
        }

        private static Material CreateMaterial()
        {
            // Sprites/Default is the one built-in shader that renders
            // transparent and unlit in EVERY render pipeline (the URP
            // Lit/Unlit shaders need script-side surface/blend property setup
            // before they go transparent). The markers want exactly "flat
            // colour, no lighting, alpha from the texture".
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            return new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = MarkerTexture
            };
        }
    }
}
