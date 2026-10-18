using System.Collections.Generic;
using NUnit.Framework;
using Nova.Gameplay.Match;
using UnityEditor;
using UnityEngine;

namespace Nova.Gameplay.Tests
{
    public class UnitRigAnimatorTests
    {
        private readonly List<Object> _owned = new List<Object>();
        private Random.State _randomState;

        [SetUp]
        public void SetUp() => _randomState = Random.state;

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _owned) Object.DestroyImmediate(obj);
            _owned.Clear();
            Random.state = _randomState;
        }

        private UnitRigAnimator CreateRig()
        {
            var root = new GameObject("TestRig");
            _owned.Add(root);
            Transform body = Child(root.transform, "Body");
            Child(body, "Leg_L").localPosition = Vector3.up;
            Child(body, "Spin_Test");
            Child(body, "Scan_Test");
            Child(body, "Arm_Test");
            Child(body, "Gun");
            return root.AddComponent<UnitRigAnimator>();
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private UnitRigAnimator LoadArt(string category, string faction, string role, string prefix)
        {
            string path = $"Assets/_Project/Art/{category}/{faction}/{role}/PF_{prefix}_{faction}_{role}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) Assert.Ignore($"Optional separate art bundle missing: {path}");
            var instance = Object.Instantiate(prefab);
            _owned.Add(instance);
            return instance.GetComponent<UnitRigAnimator>() ?? instance.AddComponent<UnitRigAnimator>();
        }

        private static Transform Find(UnitRigAnimator rig, string prefix)
        {
            foreach (Transform node in rig.GetComponentsInChildren<Transform>(true))
                if (node.name.StartsWith(prefix, System.StringComparison.Ordinal)) return node;
            Assert.Fail($"Expected authored animation node {prefix}");
            return null;
        }

        private static void AssertContinuousSweep(UnitRigAnimator rig)
        {
            Transform scan = Find(rig, "Scan_");
            Quaternion rest = scan.localRotation;
            Quaternion previous = rest;
            float excursion = 0f;
            // Cross multiple spin wraps AND a complete sweep wrap.
            for (int i = 0; i < 1500; i++)
            {
                rig.Tick(default, 1f / 60f);
                Assert.That(Quaternion.Angle(previous, scan.localRotation), Is.LessThan(0.6f), $"frame {i}");
                excursion = Mathf.Max(excursion, Quaternion.Angle(rest, scan.localRotation));
                previous = scan.localRotation;
            }
            Assert.That(excursion, Is.GreaterThan(45f), "A frozen dish must not pass continuity.");
        }

        [Test]
        public void Scan_RemainsContinuousAcrossSpinAndSweepWraps() => AssertContinuousSweep(CreateRig());

        [Test]
        public void LegionRadarPrefab_ScanRemainsContinuous() =>
            AssertContinuousSweep(LoadArt("Buildings", "Legion", "Radar", "BLDG"));

        [Test]
        public void AllianceRadarPrefab_SpinRemainsContinuousAcrossWraps()
        {
            UnitRigAnimator rig = LoadArt("Buildings", "Alliance", "Radar", "BLDG");
            Transform spin = Find(rig, "Spin_");
            Quaternion previous = spin.localRotation;
            for (int i = 0; i < 1500; i++)
            {
                rig.Tick(default, 1f / 60f);
                Assert.That(Quaternion.Angle(previous, spin.localRotation), Is.InRange(0.85f, 1f), $"frame {i}");
                previous = spin.localRotation;
            }
        }

        private static void AssertCombatAim(UnitRigAnimator rig, bool fired)
        {
            Transform body = Find(rig, "Body");
            Quaternion rest = body.localRotation;
            // Wait for an observable idle glance, independent of the random seed.
            bool glanced = false;
            for (int i = 0; i < 3600; i++)
            {
                rig.Tick(default, 1f / 60f);
                if (Quaternion.Angle(rest, body.localRotation) > 5f) { glanced = true; break; }
            }
            Assert.IsTrue(glanced, "Infantry should still glance while idle.");
            // Measure yaw in the root frame, including FBX axis conversion; firing pitch is allowed.
            Vector3 restForward = Quaternion.Inverse(rest) * body.parent.InverseTransformDirection(rig.transform.forward);
            for (int i = 0; i < (fired ? 1 : 600); i++)
            {
                rig.Tick(new UnitRigInput { HasTarget = !fired, Fired = fired,
                    TargetPosition = rig.transform.position + rig.transform.forward * 10f }, 1f / 60f);
                Vector3 actual = rig.transform.InverseTransformDirection(body.TransformDirection(restForward));
                Assert.That(Mathf.Abs(Mathf.Atan2(actual.x, actual.z) * Mathf.Rad2Deg), Is.LessThan(0.05f));
            }
            glanced = false;
            for (int i = 0; i < 3600; i++)
            {
                rig.Tick(default, 1f / 60f);
                Vector3 actual = rig.transform.InverseTransformDirection(body.TransformDirection(restForward));
                if (Mathf.Abs(Mathf.Atan2(actual.x, actual.z) * Mathf.Rad2Deg) > 5f) { glanced = true; break; }
            }
            Assert.IsTrue(glanced, "Idle glance should resume after combat.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Infantry_IdleLookDoesNotDeflectCombatAim(bool fired) => AssertCombatAim(CreateRig(), fired);

        [TestCase("Alliance", false)]
        [TestCase("Alliance", true)]
        [TestCase("Legion", false)]
        [TestCase("Legion", true)]
        public void InfantryPrefab_IdleLookDoesNotDeflectCombatAim(string faction, bool fired) =>
            AssertCombatAim(LoadArt("Units", faction, "BasicInfantry", "UNIT"), fired);

        [Test]
        public void ResetPose_ReplaysFreshMotionWithoutPreviousPhasesOrLookTargets()
        {
            UnitRigAnimator rig = CreateRig();
            rig.ResetPose();
            Random.State replayRandom = Random.state;
            Transform[] nodes = rig.GetComponentsInChildren<Transform>(true);
            var positions = new Vector3[nodes.Length];
            var rotations = new Quaternion[nodes.Length];
            for (int i = 0; i < 90; i++) rig.Tick(default, 1f / 60f);
            for (int i = 0; i < nodes.Length; i++)
            {
                positions[i] = nodes[i].localPosition;
                rotations[i] = nodes[i].localRotation;
            }
            for (int i = 0; i < 400; i++)
                rig.Tick(new UnitRigInput { Speed = 2f, Working = true, Fired = true }, 1f / 60f);
            rig.ResetPose();
            Random.state = replayRandom;
            for (int i = 0; i < 90; i++) rig.Tick(default, 1f / 60f);
            for (int i = 0; i < nodes.Length; i++)
            {
                Assert.That(Vector3.Distance(positions[i], nodes[i].localPosition), Is.LessThan(0.0001f), nodes[i].name);
                Assert.That(Quaternion.Angle(rotations[i], nodes[i].localRotation), Is.LessThan(0.05f), nodes[i].name);
            }
        }

        [Test]
        public void ResetPose_RestoresEmissionEvenOffscreenAndPreservesFactionTint()
        {
            UnitRigAnimator rig = CreateRig();
            var renderer = Find(rig, "Body").gameObject.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _owned.Add(material);
            material.EnableKeyword("_EMISSION");
            Color emission = new Color(0.2f, 0.4f, 0.6f, 1f);
            material.SetColor("_EmissionColor", emission);
            renderer.sharedMaterial = material;
            rig.EmissivePulse = true;
            rig.ResetPose(); // Bind the authored emission before simulating a prior pulse.
            var block = new MaterialPropertyBlock();
            block.SetColor("_EmissionColor", emission * 2f);
            block.SetColor("_BaseColor", Color.red);
            renderer.SetPropertyBlock(block);
            rig.Tick(new UnitRigInput { Working = true }, 0.1f);
            rig.ResetPose();
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor("_EmissionColor"), Is.EqualTo(emission));
            Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(Color.red));
        }
    }
}
