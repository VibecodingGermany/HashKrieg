using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Nova.Core;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;
using Object = UnityEngine.Object;

namespace Nova.PlayMode.Tests
{
    /// <summary>
    /// The unit command bar and its group orders in the running match (D-117).
    /// Presentation types are reached by reflection — the test assemblies may
    /// not reference the presentation layer (quality/scripts/run_gate_check.py),
    /// the same arrangement MainMenuTests use. The match is started through the
    /// menu's own StartMatch, so the cockpit is exactly what a player sees.
    /// Evidence: output/demo/commandbar_*.png (world plus UI composited).
    /// </summary>
    public sealed class CommandBarTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string ShotDir = "output/demo";

        [UnityTest]
        public IEnumerator SelectedTanks_GetTheBar_AndPatrolInLine()
        {
            Directory.CreateDirectory(ShotDir);
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;

            Component menu = FindByTypeName("MainMenuController");
            Assert.NotNull(menu, "no MainMenuController in the Bootstrap scene");
            Invoke(menu, "StartMatch");
            yield return null;

            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            Assert.IsTrue(bootstrap.IsMatchReady, "the menu's StartMatch did not build a match");
            MatchRunner runner = bootstrap.Runner;
            Component input = FindByTypeName("RtsDeviceInput");
            Assert.NotNull(input);
            FactionId faction = runner.Economy.GetSlotFaction(0);
            Assert.IsTrue(SimDefinitions.TryGetUnit(faction, UnitRole.LightTank, out SimUnitDefinition tank));

            var ids = new List<EntityId>();
            for (int i = 0; i < 6; i++)
            {
                ids.Add(runner.Entities.SpawnUnit(0,
                    new Transform2D(SimFixed.FromInt(24 + (i % 3) * 2), SimFixed.FromInt(20 + (i / 3) * 2)),
                    tank.MoveSpeed, role: tank.Role));
            }
            yield return new WaitForSeconds(0.5f);

            var selection = (SelectionManager)Get(input, "Selection");
            selection.ReplaceSelection(ids.ToArray());
            yield return null;
            yield return null;

            Type barType = FindType("Nova.Presentation.UI.UnitCommandBar");
            Assert.NotNull(barType, "UnitCommandBar type missing");
            Assert.IsTrue((bool)barType.GetProperty("ShowsUnits").GetValue(null), "the command bar must show for selected units");

            Invoke(input, "SetFormation", FormationKind.Line);
            Vector3 patrolEnd = new Vector3(44f, 0f, 22f);
            Invoke(input, "OrderPatrolAt", patrolEnd);

            yield return new WaitForSeconds(1.2f);
            yield return CaptureWithUi($"{ShotDir}/commandbar_patrol.png", new Vector3(32f, 0f, 21f), 16f);

            // Out to the far end, then back: the patrol turns around on its own.
            float maxX = float.MinValue;
            bool turned = false;
            float deadline = Time.time + 25f;
            while (Time.time < deadline && !turned)
            {
                yield return new WaitForSeconds(0.25f);
                float x = MeanX(runner, ids);
                if (x > maxX) maxX = x;
                if (maxX > 41f && x < maxX - 2f) turned = true;
            }
            Assert.IsTrue(turned, $"the patrol never turned around (max mean x {maxX:F1})");

            // A line facing east stands north-south at the far end.
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (EntityId id in ids)
            {
                runner.Entities.TryGetUnit(id, out UnitState u);
                minY = Mathf.Min(minY, u.Transform.PositionY.ToFloat());
                maxY = Mathf.Max(maxY, u.Transform.PositionY.ToFloat());
            }
            Assert.That(maxY - minY, Is.GreaterThan(3f), "the line spreads across the direction of travel");
            Debug.Log($"[CommandBar] patrol turned at mean x {maxX:F1}, line spread {maxY - minY:F1}");
        }

        [UnityTest]
        public IEnumerator SelectedHarvesters_ShowCargoReadout_AndNoCombatOrders()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;

            Component menu = FindByTypeName("MainMenuController");
            Assert.NotNull(menu, "no MainMenuController in the Bootstrap scene");
            Invoke(menu, "StartMatch");
            yield return null;

            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            Assert.IsTrue(bootstrap.IsMatchReady);
            MatchRunner runner = bootstrap.Runner;
            Component input = FindByTypeName("RtsDeviceInput");
            Assert.NotNull(input);
            FactionId faction = runner.Economy.GetSlotFaction(0);
            Assert.IsTrue(SimDefinitions.TryGetUnit(faction, UnitRole.Harvester, out SimUnitDefinition harvester));

            var ids = new List<EntityId>();
            for (int i = 0; i < 2; i++)
            {
                EntityId id = runner.Entities.SpawnUnit(0,
                    new Transform2D(SimFixed.FromInt(24 + i * 2), SimFixed.FromInt(20)),
                    harvester.MoveSpeed, role: harvester.Role);
                runner.Entities.GetUnitRef(id).CargoAE = 100;
                ids.Add(id);
            }
            yield return new WaitForSeconds(0.3f);

            var selection = (SelectionManager)Get(input, "Selection");
            selection.ReplaceSelection(ids.ToArray());
            yield return null;
            yield return null;

            Component bar = FindByTypeName("UnitCommandBar");
            Assert.NotNull(bar, "UnitCommandBar missing");
            var document = (UIDocument)bar.GetType().GetField("_document", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bar);
            VisualElement root = document.rootVisualElement;

            foreach (string hidden in new[] { "Patrol", "Guard", "AttackMove", "AttackTarget", "Scatter", "Loose", "Line", "Column", "Wedge", "Block", "Repair" })
            {
                VisualElement button = root.Q($"cmd-{hidden}");
                Assert.NotNull(button, $"cmd-{hidden} missing");
                Assert.AreEqual(DisplayStyle.None, button.style.display.value, $"{hidden} must be hidden for harvesters");
            }
            foreach (string shown in new[] { "Move", "Stop", "Harvest", "Deliver" })
            {
                VisualElement button = root.Q($"cmd-{shown}");
                Assert.NotNull(button, $"cmd-{shown} missing");
                Assert.AreNotEqual(DisplayStyle.None, button.style.display.value, $"{shown} must stay visible for harvesters");
            }

            VisualElement cargo = root.Q("cargo-panel");
            Assert.NotNull(cargo, "cargo panel missing");
            Assert.AreEqual(DisplayStyle.Flex, cargo.style.display.value, "harvester selection must show the cargo readout");
            int capacity = SimDefinitions.HarvesterCargoCapacityAE(faction);
            Assert.AreEqual($"Ladung 200 / {2 * capacity}", root.Q<Label>("cargo-text").text);
        }

        private static float MeanX(MatchRunner runner, List<EntityId> ids)
        {
            float sum = 0f;
            int n = 0;
            foreach (EntityId id in ids)
            {
                if (!runner.Entities.TryGetUnit(id, out UnitState u)) continue;
                sum += u.Transform.PositionX.ToFloat();
                n++;
            }
            return n > 0 ? sum / n : 0f;
        }

        /// <summary>
        /// World through the main camera, the UI panel rendered into a texture
        /// on top. Panels drawing into a texture repaint in the player loop, so
        /// the UI side waits two frames after the target is assigned.
        /// </summary>
        private static IEnumerator CaptureWithUi(string path, Vector3 focus, float height)
        {
            const int W = 1600, H = 900;
            PanelSettings panel = null;
            foreach (UIDocument doc in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (doc.gameObject.name == "UnitCommandBar") panel = doc.panelSettings;
            }
            var uiRt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            Texture2D ui = null;
            if (panel != null)
            {
                bool oldClear = panel.clearColor;
                Color oldValue = panel.colorClearValue;
                panel.targetTexture = uiRt;
                panel.clearColor = true;
                panel.colorClearValue = new Color(0f, 0f, 0f, 0f);
                // Not WaitForEndOfFrame: it never resumes in -batchmode.
                yield return null;
                yield return null;
                yield return null;
                ui = Read(uiRt, W, H);
                panel.targetTexture = null;
                panel.clearColor = oldClear;
                panel.colorClearValue = oldValue;
            }
            uiRt.Release();

            Camera camera = Camera.main;
            Transform t = camera.transform;
            Vector3 oldPos = t.position;
            Quaternion oldRot = t.rotation;
            var worldRt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            try
            {
                Quaternion rot = Quaternion.Euler(55f, 0f, 0f);
                t.SetPositionAndRotation(focus - rot * Vector3.forward * (height / Mathf.Sin(55f * Mathf.Deg2Rad)), rot);
                camera.targetTexture = worldRt;
                camera.Render();
                Texture2D world = Read(worldRt, W, H);
                if (ui != null)
                {
                    Color[] w = world.GetPixels();
                    Color[] u = ui.GetPixels();
                    for (int i = 0; i < w.Length; i++) w[i] = Color.Lerp(w[i], new Color(u[i].r, u[i].g, u[i].b, 1f), u[i].a);
                    world.SetPixels(w);
                    world.Apply();
                }
                File.WriteAllBytes(path, world.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                t.SetPositionAndRotation(oldPos, oldRot);
                worldRt.Release();
            }
        }

        private static Texture2D Read(RenderTexture rt, int w, int h)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        private static Component FindByTypeName(string typeName)
        {
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb.GetType().Name == typeName) return mb;
            }
            return null;
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = a.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        private static object Get(object target, string property)
        {
            return target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance).GetValue(target);
        }

        private static object Invoke(object target, string method, params object[] args)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(m, $"{target.GetType().Name}.{method} not found");
            return m.Invoke(target, args);
        }
    }
}
