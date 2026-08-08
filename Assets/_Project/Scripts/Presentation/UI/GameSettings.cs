using System;
using System.IO;
using UnityEngine;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The player's persistent game settings (main-menu sprint). Plain
    /// JsonUtility-serializable data — public fields, no properties — stored
    /// as one JSON file under <c>Application.persistentDataPath</c>. There is
    /// deliberately no PlayerPrefs behind this: engine settings map onto
    /// <see cref="GameSettingsStore.ApplyToEngine"/>, while the audio adapters
    /// apply music and SFX values to their respective backends.
    /// <para>
    /// SFX volume/enabled is consumed by <see cref="SfxSettingsBridge"/> and
    /// reaches the SFX mixer bus immediately.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class GameSettings
    {
        /// <summary>
        /// The menu track is mastered loud (about -11.8 LUFS integrated), so
        /// the default music volume sits well below 1 instead of normalizing
        /// the asset (the master file stays untouched).
        /// </summary>
        public const float DefaultMusicVolume = 0.4f;

        public const float DefaultSfxVolume = 0.8f;

        public bool musicEnabled = true;
        public float musicVolume = DefaultMusicVolume;

        public bool sfxEnabled = true;
        public float sfxVolume = DefaultSfxVolume;

        /// <summary>Index into <see cref="QualitySettings.names"/>. -1 = resolve to the engine's current level on first apply.</summary>
        public int qualityLevel = -1;

        public bool vsync = true;

        /// <summary>0 = keep the current/native resolution (default before the player ever touches the dropdown).</summary>
        public int resolutionWidth;
        public int resolutionHeight;

        /// <summary>Full-screen window vs. windowed.</summary>
        public bool fullScreen = true;

        /// <summary>Music volume a source should actually use right now (0 when muted).</summary>
        public float EffectiveMusicVolume => musicEnabled ? musicVolume : 0f;

        /// <summary>Linear SFX-bus volume. 0 when muted.</summary>
        public float EffectiveSfxVolume => sfxEnabled ? sfxVolume : 0f;

        /// <summary>Clamps every field into a range the engine calls tolerate.</summary>
        public void Sanitize()
        {
            musicVolume = Mathf.Clamp01(musicVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);

            int qualityCount = QualitySettings.names.Length;
            if (qualityLevel < 0)
            {
                qualityLevel = QualitySettings.GetQualityLevel();
            }
            qualityLevel = Mathf.Clamp(qualityLevel, 0, Mathf.Max(0, qualityCount - 1));

            if (resolutionWidth < 0) resolutionWidth = 0;
            if (resolutionHeight < 0) resolutionHeight = 0;
        }
    }

    /// <summary>
    /// Load/save/apply gateway for <see cref="GameSettings"/>. Settings are
    /// applied once at boot via
    /// <see cref="RuntimeInitializeOnLoadMethodAttribute"/> (BeforeSceneLoad)
    /// — scene-independent, so no bootstrap object in any scene has to
    /// remember to do it — and again on every change from the settings UI
    /// through <see cref="ApplyAndSave"/>.
    /// </summary>
    public static class GameSettingsStore
    {
        public const string FileName = "settings.json";

        private static string _filePathOverride;

        /// <summary>
        /// Where the JSON lives. Defaults to
        /// <c>persistentDataPath/settings.json</c>; tests redirect this to a
        /// temp path so a test run never touches the player's real file.
        /// </summary>
        public static string FilePath
        {
            get => _filePathOverride ?? Path.Combine(Application.persistentDataPath, FileName);
            set => _filePathOverride = value;
        }

        /// <summary>The live settings. Mutate fields, then call <see cref="ApplyAndSave"/>.</summary>
        public static GameSettings Current { get; private set; } = new GameSettings();

        /// <summary>Raised after every <see cref="ApplyAndSave"/> — audio sources and open menus listen to this.</summary>
        public static event Action<GameSettings> Applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyAtBoot()
        {
            Current = Load();
            ApplyToEngine(Current);
        }

        /// <summary>
        /// Reads the JSON file. Missing or corrupt files fall back to defaults
        /// — a broken settings file must never keep the game from booting.
        /// </summary>
        public static GameSettings Load()
        {
            var settings = new GameSettings();
            try
            {
                if (File.Exists(FilePath))
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath), settings);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameSettings] Could not read {FilePath} ({exception.GetType().Name}) — falling back to defaults.");
                settings = new GameSettings();
            }

            settings.Sanitize();
            return settings;
        }

        /// <summary>Applies the live settings to the engine and persists them. Call after every UI change.</summary>
        public static void ApplyAndSave()
        {
            Current.Sanitize();
            ApplyToEngine(Current);
            Save(Current);
            Applied?.Invoke(Current);
        }

        /// <summary>
        /// Maps the values onto engine calls: quality level, vSync and
        /// resolution/fullscreen. Audio remains adapter-owned: music and SFX
        /// listeners consume <see cref="Applied"/> without coupling this data
        /// gateway to a concrete backend.
        /// </summary>
        public static void ApplyToEngine(GameSettings settings)
        {
            settings.Sanitize();

            if (QualitySettings.GetQualityLevel() != settings.qualityLevel)
            {
                QualitySettings.SetQualityLevel(settings.qualityLevel, true);
            }
            QualitySettings.vSyncCount = settings.vsync ? 1 : 0;

            FullScreenMode mode = settings.fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (settings.resolutionWidth > 0 && settings.resolutionHeight > 0)
            {
                Screen.SetResolution(settings.resolutionWidth, settings.resolutionHeight, mode);
            }
            else
            {
                Screen.fullScreenMode = mode;
            }
        }

        private static void Save(GameSettings settings)
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(settings, prettyPrint: true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameSettings] Could not write {FilePath} ({exception.GetType().Name}) — settings live on for this session only.");
            }
        }
    }
}
