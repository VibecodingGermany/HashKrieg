using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Nova.Core;
using Nova.Gameplay.Match;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.PlayMode.Tests
{
    public sealed class BuilderWorkPresentationTests
    {
        [UnityTest]
        public IEnumerator BuilderArms_WorkAtSite_AndRestWhenStoppedOutsideReach()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity", LoadSceneMode.Single);
            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            Assert.NotNull(bootstrap);
            bootstrap.StartGrayboxMatch();
            Assert.IsTrue(bootstrap.IsMatchReady);
            MatchRunner runner = bootstrap.Runner;
            var views = Object.FindAnyObjectByType<UnitViewManager>();
            Assert.NotNull(views);

            EntityId builder = EntityId.Invalid;
            foreach (UnitState unit in runner.Entities.RawUnits)
                if (unit.IsActive && unit.PlayerId == MatchBootstrap.LocalSlot && unit.Role == UnitRole.Builder)
                { builder = unit.Id; break; }
            Assert.IsTrue(builder.IsValid, "canonical opening must contain an own builder");
            // Fixture position beside the canonical refinery footprint (8,4)..(10,6).
            // Subsequent placement, movement and Stop use the real command intake.
            runner.Entities.GetUnitRef(builder).Transform = new Transform2D(SimFixed.FromInt(7), SimFixed.FromInt(5));
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(views.TryGetView(builder, out GameObject view) && view != null);
            Assert.NotNull(view.GetComponent<UnitRigAnimator>(), "requires the current art bundle");
            Transform arm = null;
            foreach (Transform node in view.GetComponentsInChildren<Transform>(true))
                if (node.name.StartsWith("Arm_", System.StringComparison.Ordinal)) { arm = node; break; }
            Assert.NotNull(arm, "builder prefab needs an authored Arm_ node");
            Quaternion rest = arm.localRotation;
            uint rawBuilder = UnitCommandStateView.ToRawEntityId(builder);
            Assert.AreEqual(CommandIngressResult.Accepted,
                runner.Ingress.TrySubmitIntent(CommandIntent.Create(new PlaceBuildingPayload(4, 8, 4)), out _));

            float maxWorkAngle = 0f;
            float until = Time.time + 1.8f;
            while (Time.time < until)
            {
                yield return null;
                maxWorkAngle = Mathf.Max(maxWorkAngle, Quaternion.Angle(rest, arm.localRotation));
            }
            uint siteRaw = 0;
            foreach (UnitState unit in runner.Entities.RawUnits)
                if (unit.IsActive && unit.PlayerId == MatchBootstrap.LocalSlot
                    && runner.Construction.IsActiveSite(unit.Id))
                { siteRaw = UnitCommandStateView.ToRawEntityId(unit.Id); break; }
            Assert.AreNotEqual(0u, siteRaw, "placement must create a real construction site");
            Assert.IsTrue(runner.Construction.TryGetSite(siteRaw, out _, out int nearProgress, out uint assigned));
            Assert.AreEqual(rawBuilder, assigned);
            Assert.Greater(nearProgress, 0, "in-reach builder must progress construction");
            Assert.Greater(maxWorkAngle, 3f, "working builder's authored arm must visibly swing");

            Assert.AreEqual(CommandIngressResult.Accepted,
                runner.Ingress.TrySubmitIntent(CommandIntent.Create(new MovePayload(new[] { rawBuilder },
                    SimFixed.FromInt(7), SimFixed.FromInt(12))), out _));
            until = Time.time + 10f;
            while (Time.time < until
                && SimFixed.WorldToGrid(runner.Entities.GetUnitRef(builder).Transform.PositionY) < 9)
                yield return null;
            Assert.GreaterOrEqual(SimFixed.WorldToGrid(runner.Entities.GetUnitRef(builder).Transform.PositionY), 9,
                "builder must actually leave footprint reach through movement");
            Assert.AreEqual(CommandIngressResult.Accepted,
                runner.Ingress.TrySubmitIntent(CommandIntent.Create(new StopPayload(new[] { rawBuilder })), out _));
            yield return new WaitForSeconds(0.9f); // command latency and arm-blend settling
            Assert.IsFalse(runner.Entities.GetUnitRef(builder).IsMoving, "Stop must land before checking idle work");
            Assert.IsTrue(runner.Construction.TryGetSite(siteRaw, out _, out int farProgress, out assigned));
            Assert.AreEqual(rawBuilder, assigned, "assignment intentionally survives departure");
            uint tickBefore = runner.Session.CurrentTick;
            float maxIdleAngle = 0f;
            until = Time.time + 1.2f;
            while (Time.time < until)
            {
                yield return null;
                maxIdleAngle = Mathf.Max(maxIdleAngle, Quaternion.Angle(rest, arm.localRotation));
            }
            Assert.Greater(runner.Session.CurrentTick, tickBefore, "the match must keep ticking during the pause check");
            Assert.IsTrue(runner.Construction.TryGetSite(siteRaw, out _, out int finalProgress, out _));
            Assert.AreEqual(farProgress, finalProgress, "out-of-reach builder must not advance the site");
            Assert.Less(maxIdleAngle, 0.1f, "assigned but out-of-reach builder must not fake construction with its arms");
        }
    }
}
