using System;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    /// <summary>
    /// Import settings for the menu's own assets: the menu track, the key art
    /// and the Rajdhani font files. Without it the defaults quietly do the
    /// wrong thing — a 2560x1440 image is capped at 2048 and rescaled towards
    /// a power of two, and a 2:16 music track is decompressed into memory as a
    /// whole.
    /// <para>
    /// TWO OWNERSHIP RULES, on purpose. The four menu assets named in
    /// <see cref="MenuAssetSetup"/> are machine-configured like the generated
    /// scene: their settings are asserted on EVERY import, so a .meta written
    /// before this file existed heals itself. Everything else under the two
    /// roots is configured only on FIRST import
    /// (<c>AssetImporter.importSettingsMissing</c>), so a value someone
    /// deliberately changed in the Inspector survives a reimport.
    /// </para>
    /// <para>
    /// PATH-BOUND ON PURPOSE: only <c>Assets/_Project/Audio/Music/**</c> and
    /// <c>Assets/_Project/UI/**</c>. <c>Assets/_Project/Art/**</c> is the
    /// drop-in art package with its own pipeline (<see cref="ArtAssetAutoSync"/>)
    /// and must not be touched from here.
    /// </para>
    /// <para>
    /// The audio root is the MUSIC folder, not all of Audio/. Streaming is the
    /// right load type for a two-minute background loop and the wrong one for a
    /// sound effect, which wants to be resident and decompressed before it is
    /// first triggered. Sibling folders (Audio/SFX/ and friends) therefore keep
    /// the engine defaults until someone writes rules for them.
    /// </para>
    /// </summary>
    public sealed class MenuAssetImportSettings : AssetPostprocessor
    {
        private const string AudioRoot = "Assets/_Project/Audio/Music/";
        private const string UiRoot = "Assets/_Project/UI/";

        /// <summary>
        /// Vorbis quality for the menu track. 0.7 keeps the 2:16 loop near
        /// 2.4 MB; the file is already a Vorbis .ogg, so this is a re-encode
        /// budget, not a source quality.
        /// </summary>
        private const float MusicVorbisQuality = 0.7f;

        /// <summary>
        /// Part of the import hash: raising it reimports everything this
        /// postprocessor applies to. Bump it whenever a value below changes,
        /// otherwise assets that were already imported keep their old .meta
        /// and the change looks like it did nothing.
        /// </summary>
        public override uint GetVersion() => 1;

        private void OnPreprocessAudio()
        {
            if (!ShouldConfigure(AudioRoot)) return;

            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            // Streaming: a 2:16 track is background music, not a sound effect,
            // and does not belong in memory decompressed (the default).
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = MusicVorbisQuality;
            // Per-platform since 2022.2; AudioImporter.preloadAudioData is the
            // deprecated spelling of the same flag.
            settings.preloadAudioData = false;

            importer.defaultSampleSettings = settings;
            // The track is a stereo mix; folding it to mono would collapse the
            // width it was mastered with.
            importer.forceToMono = false;
        }

        private void OnPreprocessTexture()
        {
            if (!ShouldConfigure(UiRoot)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;

            // The key art is 2560x1440. The default cap of 2048 would silently
            // downscale the only full-screen image in the game...
            importer.maxTextureSize = 4096;
            // ...and the default NPOT handling ("scale to nearest power of
            // two") would additionally squash it to 2048x1024.
            importer.npotScale = TextureImporterNPOTScale.None;

            // No mip chain: this is drawn once, full screen, by a UI panel that
            // never minifies it far. Mips would cost a third more memory and
            // soften the image at the reference resolution. A UI texture that
            // does get minified hard would want them back.
            importer.mipmapEnabled = false;

            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            // JPEG has no alpha channel; declaring that keeps the importer from
            // synthesising one and doubling the memory.
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;

            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            // Crunch is a delivery-size trade for a quality loss that the large
            // flat gradients in this image would show off badly.
            importer.crunchedCompression = false;
        }

        /// <summary>
        /// Fonts. The TrueType defaults are already right for UI Toolkit (a
        /// dynamic font, rasterised on demand), so the only thing asserted
        /// here is that the font data ships inside the build — without it the
        /// menu looks correct in the Editor and falls back to the engine font
        /// in a player.
        /// </summary>
        private void OnPreprocessAsset()
        {
            if (!(assetImporter is TrueTypeFontImporter font)) return;
            if (!ShouldConfigure(UiRoot)) return;

            font.includeFontData = true;
        }

        private bool ShouldConfigure(string root)
        {
            if (!assetPath.StartsWith(root, StringComparison.Ordinal)) return false;
            return IsMenuAsset() || assetImporter.importSettingsMissing;
        }

        /// <summary>The four files the main menu is built from; their import settings are not negotiable.</summary>
        private bool IsMenuAsset()
        {
            return assetPath == MenuAssetSetup.KeyArtPath
                   || assetPath == MenuAssetSetup.MusicClipPath
                   || assetPath == MenuAssetSetup.TitleFontPath
                   || assetPath == MenuAssetSetup.BodyFontPath;
        }
    }
}
