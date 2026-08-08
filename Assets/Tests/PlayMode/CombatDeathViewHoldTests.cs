using System.Collections;
using System.Reflection;
using Nova.Core;
using Nova.Gameplay.Match;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EntityId = Nova.Core.EntityId;

namespace Nova.PlayMode.Tests
{
    /// <summary>
    /// Regression for the dangerous slot-reuse case: a new EntityId version
    /// may occupy the dead unit's slot in the same simulation tick. The corpse
    /// must stay detached while the replacement receives a different view.
    /// </summary>
    public sealed class CombatDeathViewHoldTests
    {
        [UnityTest]
        public IEnumerator RecycledSlotCannotReuseTheHeldCorpseView()
        {
            var root = new GameObject("CombatDeathViewHoldTest");
            try
            {
                MatchRunner runner = root.AddComponent<MatchRunner>();
                UnitViewManager views = root.AddComponent<UnitViewManager>();
                runner.InitializeMatch(
                    seed: 0x12BUL,
                    width: 32,
                    height: 32,
                    maxUnits: 16,
                    enableSkirmishAi: false);
                views.Initialize(runner);

                EntityId first = runner.Entities.SpawnUnit(
                    runner.Session.LocalSlot,
                    new Transform2D(SimFixed.FromInt(5), SimFixed.FromInt(5)),
                    SimFixed.FromInt(1),
                    role: UnitRole.BasicInfantry);
                Assert.That(runner.StartMatch(), Is.True);

                // One rendered baseline gives the original unit its view.
                yield return null;
                Assert.That(views.TryGetView(first, out GameObject corpse), Is.True);
                Renderer[] corpseRenderers = corpse.GetComponentsInChildren<Renderer>(true);
                Collider[] corpseColliders = corpse.GetComponentsInChildren<Collider>(true);
                var originalMaterials = new Material[corpseRenderers.Length][];
                var originalColliderStates = new bool[corpseColliders.Length];
                for (int i = 0; i < corpseRenderers.Length; i++)
                {
                    originalMaterials[i] = corpseRenderers[i].sharedMaterials;
                }
                for (int i = 0; i < corpseColliders.Length; i++)
                {
                    originalColliderStates[i] = corpseColliders[i].enabled;
                }

                Assert.That(runner.Entities.DespawnUnit(first), Is.True);
                EntityId replacement = runner.Entities.SpawnUnit(
                    runner.Session.LocalSlot,
                    new Transform2D(SimFixed.FromInt(8), SimFixed.FromInt(8)),
                    SimFixed.FromInt(1),
                    role: UnitRole.BasicInfantry);
                Assert.That(replacement.Index, Is.EqualTo(first.Index));
                Assert.That(replacement.Version, Is.Not.EqualTo(first.Version));

                InvokePrivate(runner, "TryStepFixedTick");
                InvokePrivate(views, "LateUpdate");

                Assert.That(views.ActiveDeathHoldCount, Is.EqualTo(1));
                Assert.That(views.TryGetView(first, out _), Is.False);
                Assert.That(views.TryGetView(replacement, out GameObject replacementView), Is.True);
                Assert.That(replacementView, Is.Not.SameAs(corpse));
                Assert.That(corpse.activeSelf, Is.True, "corpse remains visible during the 0.8-second hold");
                foreach (Collider collider in corpse.GetComponentsInChildren<Collider>(true))
                {
                    Assert.That(collider.enabled, Is.False, "held corpse must not remain pickable");
                }

                // Exercise the exact 0.8-second contract without depending on
                // wall-clock timing in a headless player. The corpse must stay
                // held before the boundary, then restore its pooled identity.
                InvokePrivate(views, "AdvanceDeathHolds", 0.79f);
                Assert.That(views.ActiveDeathHoldCount, Is.EqualTo(1));
                Assert.That(corpse.activeSelf, Is.True);

                InvokePrivate(views, "AdvanceDeathHolds", 0.02f);
                Assert.That(views.ActiveDeathHoldCount, Is.Zero);
                Assert.That(corpse.activeSelf, Is.False);
                for (int i = 0; i < corpseRenderers.Length; i++)
                {
                    Assert.That(corpseRenderers[i].sharedMaterials,
                        Is.EqualTo(originalMaterials[i]),
                        $"renderer {i} must recover its exact authored materials");
                }
                for (int i = 0; i < corpseColliders.Length; i++)
                {
                    Assert.That(corpseColliders[i].enabled,
                        Is.EqualTo(originalColliderStates[i]),
                        $"collider {i} must recover its pre-hold enabled state");
                }

                // With the hold complete, the same primitive pool may hand
                // that exact object to a later entity; this closes the full
                // detach -> restore -> reuse lifecycle, not just the detach.
                EntityId third = runner.Entities.SpawnUnit(
                    runner.Session.LocalSlot,
                    new Transform2D(SimFixed.FromInt(11), SimFixed.FromInt(11)),
                    SimFixed.FromInt(1),
                    role: UnitRole.BasicInfantry);
                InvokePrivate(runner, "TryStepFixedTick");
                InvokePrivate(views, "LateUpdate");

                Assert.That(views.TryGetView(third, out GameObject thirdView), Is.True);
                Assert.That(thirdView, Is.SameAs(corpse),
                    "the completed corpse must return to its exact role/prefab pool");
                Collider[] reusedColliders = thirdView.GetComponentsInChildren<Collider>(true);
                Assert.That(reusedColliders.Length, Is.EqualTo(originalColliderStates.Length));
                for (int i = 0; i < reusedColliders.Length; i++)
                {
                    Assert.That(reusedColliders[i].enabled, Is.EqualTo(originalColliderStates[i]),
                        $"reused collider {i} must recover its pre-hold state");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static object InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"missing private test seam {target.GetType().Name}.{methodName}");
            return method.Invoke(target, arguments);
        }
    }
}
