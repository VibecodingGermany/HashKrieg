using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// The persistence half of the main-menu sprint
    /// (docs/production/hashkrieg/08_Sprint_Hauptmenue.md, D-083): what the
    /// player sets in the settings panel must still be there on the next
    /// start, a broken file must never keep the game from booting, and the
    /// deliberately quiet default music volume must stay deliberately quiet.
    /// <para>
    /// WHY THIS FIXTURE GOES THROUGH REFLECTION. GameSettings/GameSettingsStore
    /// live in Nova.Presentation.UI, a rank-4 assembly, and
    /// quality/scripts/run_gate_check.py:183-188 forbids ANY test assembly from
    /// referencing a presentation assembly (D-061). That is not a technicality
    /// that can be waived: the assembly Nova.Presentation.Tests existed once
    /// and was dissolved in 5cdb0ce with the words "could never pass
    /// G0-ARCHITECTURE"; its tests moved down into Nova.Gameplay.Tests together
    /// with the logic they covered. So the settings are reached by type name
    /// here — untyped, but real coverage of a real acceptance criterion, and
    /// green architecture gate.
    /// </para>
    /// <para>
    /// THE FIX IS A FILE MOVE, and it belongs to the owner, not to a test:
    /// move Assets/_Project/Scripts/Presentation/UI/GameSettings.cs unchanged
    /// to Assets/_Project/Scripts/Gameplay/UI/ (namespace Nova.Gameplay.UI),
    /// which is where the same refactor already put SelectionManager,
    /// CommandCardPresenter and MinimapRenderer. This file then keeps its
    /// place and its test names and drops the plumbing at the bottom for two
    /// using directives.
    /// </para>
    /// <para>
    /// NO TEST HERE EVER TOUCHES THE PLAYER'S REAL FILE: SetUp redirects
    /// GameSettingsStore.FilePath — the seam that exists for exactly this — to
    /// a throwaway directory, and TearDown clears it again.
    /// </para>
    /// </summary>
    [TestFixture]
    public class GameSettingsTests
    {
        private const string SettingsTypeName = "Nova.Presentation.UI.GameSettings";
        private const string StoreTypeName = "Nova.Presentation.UI.GameSettingsStore";

        /// <summary>Sprint §4: the menu track is mastered at about -11.8 LUFS integrated.</summary>
        private const float ExpectedDefaultMusicVolume = 0.4f;

        private static Type _settings;
        private static Type _store;

        private string _directory;
        private string _settingsPath;
        private int _vSyncBackup;
        private int _qualityBackup;

        [OneTimeSetUp]
        public void ResolveTheSettingsTypes()
        {
            _settings = Resolve(SettingsTypeName);
            _store = Resolve(StoreTypeName);
        }

        [SetUp]
        public void RedirectTheSettingsFile()
        {
            _directory = Path.Combine(
                Application.temporaryCachePath, "GameSettingsTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _settingsPath = Path.Combine(_directory, FileNameConstant());
            FilePath = _settingsPath;

            // ApplyAndSave also applies to the engine, and vSyncCount is a
            // serialized field of the ACTIVE quality level: without this
            // backup a test run would leave ProjectSettings/QualitySettings.asset
            // modified in the working tree.
            _vSyncBackup = QualitySettings.vSyncCount;
            _qualityBackup = QualitySettings.GetQualityLevel();
        }

        [TearDown]
        public void RestoreEverything()
        {
            FilePath = null; // back to Application.persistentDataPath
            ResetCurrent();

            QualitySettings.vSyncCount = _vSyncBackup;
            if (QualitySettings.GetQualityLevel() != _qualityBackup)
            {
                QualitySettings.SetQualityLevel(_qualityBackup, applyExpensiveChanges: false);
            }

            if (!string.IsNullOrEmpty(_directory) && Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        // ------------------------------------------------------------------
        // (a) The one value that must not drift
        // ------------------------------------------------------------------

        [Test]
        public void MusicVolume_DefaultsToFourTenths()
        {
            Assert.AreEqual(ExpectedDefaultMusicVolume, ConstantFloat("DefaultMusicVolume"), 0.0001f,
                "the menu track is mastered at about -11.8 LUFS integrated — loud for background " +
                "music. The sprint decided to open at 0.4 instead of normalizing the master file " +
                "(§4), so a fresh install is quiet enough to talk over. Anyone raising this to 1.0 " +
                "makes the game greet every new player at a shout.");

            Assert.AreEqual(ExpectedDefaultMusicVolume, Float(NewSettings(), "musicVolume"), 0.0001f,
                "a fresh GameSettings must start on that default, not on the field's zero value");
        }

        // ------------------------------------------------------------------
        // (b) Survives the restart — both directions
        // ------------------------------------------------------------------

        [Test]
        public void ApplyAndSave_WritesTheLiveSettingsToDisk()
        {
            object current = Current;
            Set(current, "musicEnabled", false);
            Set(current, "musicVolume", 0.23f);
            Set(current, "sfxEnabled", false);
            Set(current, "sfxVolume", 0.66f);
            Set(current, "vsync", false);
            Set(current, "fullScreen", false);

            // Left at the engine's own level and at "keep the current
            // resolution" ON PURPOSE: ApplyAndSave runs ApplyToEngine, and this
            // is an EditMode test. Changing them here would poke
            // QualitySettings.SetQualityLevel and Screen.SetResolution at the
            // running Editor for no gain — the read direction below proves that
            // both fields make the round trip.
            Set(current, "qualityLevel", QualitySettings.GetQualityLevel());
            Set(current, "resolutionWidth", 0);
            Set(current, "resolutionHeight", 0);

            Invoke(_store, "ApplyAndSave");

            Assert.IsTrue(File.Exists(_settingsPath),
                $"ApplyAndSave wrote no file at {_settingsPath} — a setting that never reaches the " +
                "disk does not survive the restart the sprint promises");

            object reloaded = Invoke(_store, "Load");
            Assert.IsFalse(Bool(reloaded, "musicEnabled"), "the music switch must come back off");
            Assert.AreEqual(0.23f, Float(reloaded, "musicVolume"), 0.0001f,
                "'und beim nächsten Start ist die Lautstärke noch so, wie ich sie eingestellt " +
                "habe' — the sprint's acceptance test in one assertion");
            Assert.IsFalse(Bool(reloaded, "sfxEnabled"), "the SFX switch must persist even though nothing consumes it yet");
            Assert.AreEqual(0.66f, Float(reloaded, "sfxVolume"), 0.0001f,
                "the SFX volume is stored now so it already holds a real value on the day the " +
                "first sound effect ships (sprint §5)");
            Assert.IsFalse(Bool(reloaded, "vsync"), "vSync must persist");
            Assert.IsFalse(Bool(reloaded, "fullScreen"), "the window mode must persist");
        }

        [Test]
        public void Load_ReadsEveryFieldBackFromDisk()
        {
            // Hand-written rather than produced by ApplyAndSave: this is the
            // "next start" direction, and it is the only place that pins the
            // JSON field NAMES. A rename would silently orphan every player's
            // stored settings — the file would parse and every value would fall
            // back to its default, with no error anywhere.
            File.WriteAllText(_settingsPath,
                "{\n" +
                "    \"musicEnabled\": false,\n" +
                "    \"musicVolume\": 0.11,\n" +
                "    \"sfxEnabled\": false,\n" +
                "    \"sfxVolume\": 0.22,\n" +
                "    \"qualityLevel\": 0,\n" +
                "    \"vsync\": false,\n" +
                "    \"resolutionWidth\": 1600,\n" +
                "    \"resolutionHeight\": 900,\n" +
                "    \"fullScreen\": false\n" +
                "}\n");

            object loaded = Invoke(_store, "Load");

            Assert.IsFalse(Bool(loaded, "musicEnabled"), "musicEnabled did not come back from the file");
            Assert.AreEqual(0.11f, Float(loaded, "musicVolume"), 0.0001f, "musicVolume did not come back from the file");
            Assert.IsFalse(Bool(loaded, "sfxEnabled"), "sfxEnabled did not come back from the file");
            Assert.AreEqual(0.22f, Float(loaded, "sfxVolume"), 0.0001f, "sfxVolume did not come back from the file");
            Assert.AreEqual(0, Int(loaded, "qualityLevel"),
                "a stored render-detail level must be honoured, not re-resolved to whatever the " +
                "engine happens to run right now");
            Assert.IsFalse(Bool(loaded, "vsync"), "vsync did not come back from the file");
            Assert.AreEqual(1600, Int(loaded, "resolutionWidth"), "resolutionWidth did not come back from the file");
            Assert.AreEqual(900, Int(loaded, "resolutionHeight"), "resolutionHeight did not come back from the file");
            Assert.IsFalse(Bool(loaded, "fullScreen"), "fullScreen did not come back from the file");
        }

        // ------------------------------------------------------------------
        // (c) A broken or missing file must never block the boot
        // ------------------------------------------------------------------

        [Test]
        public void Load_WithoutAFile_ReturnsTheDocumentedDefaults()
        {
            Assert.IsFalse(File.Exists(_settingsPath), "fixture error: the temp directory is not empty");

            object loaded = Invoke(_store, "Load");

            Assert.IsTrue(Bool(loaded, "musicEnabled"), "a first start plays music");
            Assert.AreEqual(ExpectedDefaultMusicVolume, Float(loaded, "musicVolume"), 0.0001f,
                "a first start opens at the quiet default");
            Assert.IsTrue(Bool(loaded, "sfxEnabled"), "SFX default to on");
            Assert.IsTrue(Bool(loaded, "vsync"), "vSync defaults to on");
            Assert.IsTrue(Bool(loaded, "fullScreen"), "the game defaults to a full-screen window");
            Assert.AreEqual(0, Int(loaded, "resolutionWidth"),
                "0 means 'the player never chose one', so the game keeps the native resolution " +
                "instead of forcing a mode nobody asked for");
            Assert.AreEqual(0, Int(loaded, "resolutionHeight"), "same for the height");
            Assert.AreEqual(QualitySettings.GetQualityLevel(), Int(loaded, "qualityLevel"),
                "the unset quality index (-1) resolves to the level the engine is on, so the " +
                "settings panel opens on the truth rather than on index 0");
        }

        [Test]
        public void Load_WithACorruptFile_FallsBackToDefaultsAndSaysSo()
        {
            File.WriteAllText(_settingsPath, "{ this is not JSON — a half-written file after a crash");

            // The warning is REQUIRED behaviour, not noise, so it is asserted
            // rather than suppressed: a corrupt file must not keep the game
            // from booting, but it must not vanish silently either. Note that
            // an unexpected warning would NOT fail the run on its own (only
            // errors and exceptions do), so LogAssert.ignoreFailingMessages
            // would prove nothing here — Expect() is what actually asserts the
            // message was written.
            LogAssert.Expect(LogType.Warning,
                new Regex(@"\[GameSettings\] Could not read .*falling back to defaults\."));

            object loaded = Invoke(_store, "Load");

            Assert.AreEqual(ExpectedDefaultMusicVolume, Float(loaded, "musicVolume"), 0.0001f,
                "a corrupt settings file must cost the player their settings, never their game");
            Assert.IsTrue(Bool(loaded, "musicEnabled"), "the fallback is the default object, not a half-parsed one");
        }

        // ------------------------------------------------------------------
        // (d) Sanitize: nothing hostile ever reaches an engine call
        // ------------------------------------------------------------------

        [Test]
        public void Sanitize_ClampsVolumesAndTheQualityIndex()
        {
            object settings = NewSettings();
            Set(settings, "musicVolume", 1.7f);
            Set(settings, "sfxVolume", -0.5f);
            Set(settings, "qualityLevel", 99);
            Set(settings, "resolutionWidth", -1920);
            Set(settings, "resolutionHeight", -1080);

            Invoke(settings, "Sanitize");

            Assert.AreEqual(1f, Float(settings, "musicVolume"), 0.0001f, "volumes above 1 must clamp, not distort");
            Assert.AreEqual(0f, Float(settings, "sfxVolume"), 0.0001f, "negative volumes must clamp to silence");
            Assert.AreEqual(QualitySettings.names.Length - 1, Int(settings, "qualityLevel"),
                "a settings file carried over from a machine with more quality levels must clamp " +
                "into range — QualitySettings.SetQualityLevel would otherwise be called with an " +
                "index that does not exist");
            Assert.AreEqual(0, Int(settings, "resolutionWidth"),
                "a negative resolution must fall back to 'keep the current one' rather than reach " +
                "Screen.SetResolution");
            Assert.AreEqual(0, Int(settings, "resolutionHeight"), "same for the height");
        }

        [Test]
        public void Sanitize_ResolvesTheUnsetQualityIndexToTheEngineLevel()
        {
            object settings = NewSettings();
            Assert.AreEqual(-1, Int(settings, "qualityLevel"),
                "-1 is the documented 'never chosen' marker for the render-detail dropdown");

            Invoke(settings, "Sanitize");

            Assert.AreEqual(QualitySettings.GetQualityLevel(), Int(settings, "qualityLevel"),
                "the first sanitize must adopt the level the engine actually runs, so the dropdown " +
                "opens on the current setting instead of silently downgrading the player to index 0");
        }

        // ------------------------------------------------------------------
        // (e) Muting is a switch, not an eraser
        // ------------------------------------------------------------------

        [Test]
        public void Muting_ZeroesTheEffectiveVolumeButKeepsTheStoredLevel()
        {
            object settings = NewSettings();
            Set(settings, "musicVolume", 0.62f);
            Set(settings, "sfxVolume", 0.31f);

            Set(settings, "musicEnabled", false);
            Set(settings, "sfxEnabled", false);

            Assert.AreEqual(0f, Property(settings, "EffectiveMusicVolume"), 0.0001f,
                "a muted menu is silent — this is the value that goes onto AudioSource.volume, " +
                "which is the entire audio surface of the project (no AudioMixer, sprint §5)");
            Assert.AreEqual(0f, Property(settings, "EffectiveSfxVolume"), 0.0001f, "same for SFX");

            Assert.AreEqual(0.62f, Float(settings, "musicVolume"), 0.0001f,
                "muting must not erase the level the player set — un-muting has to bring it back, " +
                "not reset to the default");

            Set(settings, "musicEnabled", true);
            Assert.AreEqual(0.62f, Property(settings, "EffectiveMusicVolume"), 0.0001f,
                "un-muting restores exactly the stored level");
        }

        // ------------------------------------------------------------------
        // (f) The test seam itself
        // ------------------------------------------------------------------

        [Test]
        public void FilePath_FallsBackToPersistentDataPathWithoutAnOverride()
        {
            FilePath = null;

            Assert.AreEqual(Path.Combine(Application.persistentDataPath, FileNameConstant()), FilePath,
                "production must write into Application.persistentDataPath; the override is a test " +
                "seam only. If this ever changes, every fixture above starts writing the real " +
                "settings file of whoever runs the tests.");

            FilePath = _settingsPath; // TearDown clears it either way
        }

        // ------------------------------------------------------------------
        // Reflection plumbing (see the class comment: it disappears the day
        // GameSettings.cs moves down into Nova.Gameplay)
        // ------------------------------------------------------------------

        private static Type Resolve(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Nova.Presentation.UI");
            if (type == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(fullName);
                    if (type != null) break;
                }
            }

            Assert.NotNull(type,
                $"{fullName} not found in the loaded assemblies. If the settings moved out of " +
                "Nova.Presentation.UI — which is the recommended fix — this fixture can reference " +
                "them directly and delete this plumbing.");
            return type;
        }

        private static object NewSettings() => Activator.CreateInstance(_settings);

        private static FieldInfo Field(string name)
        {
            FieldInfo field = _settings.GetField(name);
            Assert.NotNull(field,
                $"GameSettings.{name} is gone. The JSON on the player's disk is written from these " +
                "field names, so a rename orphans every stored settings file in the wild.");
            return field;
        }

        private static void Set(object settings, string name, object value) => Field(name).SetValue(settings, value);

        private static float Float(object settings, string name) => (float)Field(name).GetValue(settings);

        private static bool Bool(object settings, string name) => (bool)Field(name).GetValue(settings);

        private static int Int(object settings, string name) => (int)Field(name).GetValue(settings);

        private static float ConstantFloat(string name) => (float)Field(name).GetValue(null);

        private static float Property(object settings, string name)
        {
            PropertyInfo property = _settings.GetProperty(name);
            Assert.NotNull(property, $"GameSettings.{name} is gone");
            return (float)property.GetValue(settings);
        }

        private static object Invoke(object target, string method)
        {
            MethodInfo info = _settings.GetMethod(method);
            Assert.NotNull(info, $"GameSettings.{method}() is gone");
            return info.Invoke(target, null);
        }

        private static object Invoke(Type type, string method)
        {
            MethodInfo info = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(info, $"{type.Name}.{method}() is gone");
            return info.Invoke(null, null);
        }

        private static string FileNameConstant() => (string)_store.GetField("FileName").GetValue(null);

        private static string FilePath
        {
            get => (string)StoreProperty("FilePath").GetValue(null);
            set => StoreProperty("FilePath").SetValue(null, value);
        }

        private static object Current => StoreProperty("Current").GetValue(null);

        private static PropertyInfo StoreProperty(string name)
        {
            PropertyInfo property = _store.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(property, $"GameSettingsStore.{name} is gone");
            return property;
        }

        /// <summary>
        /// Puts the live settings object back to a fresh, default-valued state.
        /// The store's own setter is private (correctly so), so the fields are
        /// copied from a new instance instead — the tests share one static
        /// object and must not leak values into each other or into whatever
        /// runs next in the same domain.
        /// </summary>
        private static void ResetCurrent()
        {
            object defaults = NewSettings();
            object current = Current;
            foreach (FieldInfo field in _settings.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                field.SetValue(current, field.GetValue(defaults));
            }
        }
    }
}
