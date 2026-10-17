using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nova.Gameplay;
using EntityId = Nova.Core.EntityId;

namespace Nova.Gameplay.Tests
{
    /// <summary>Client-side formation slots (D-117): shape, facing, spacing and crossing-free assignment.</summary>
    [TestFixture]
    public class FormationPlannerTests
    {
        private static readonly Vector2 MapMin = Vector2.zero;
        private static readonly Vector2 MapMax = new Vector2(127f, 127f);

        private static List<FormationMember> Group(int n, Vector2 at, float radius = 0.3f)
        {
            var list = new List<FormationMember>();
            for (int i = 0; i < n; i++)
            {
                list.Add(new FormationMember(new EntityId(i, 1), at + new Vector2((i % 4) * 1.2f, (i / 4) * 1.2f), radius));
            }
            return list;
        }

        [Test]
        public void Loose_AndSingleUnits_LeaveTheSpreadToTheSimulation()
        {
            var slots = new List<Vector2>();
            Assert.That(FormationPlanner.Plan(FormationKind.Loose, Group(6, new Vector2(10, 10)), new Vector2(40, 40), MapMin, MapMax, slots), Is.False);
            Assert.That(FormationPlanner.Plan(FormationKind.Line, Group(1, new Vector2(10, 10)), new Vector2(40, 40), MapMin, MapMax, slots), Is.False);
        }

        [TestCase(FormationKind.Line)]
        [TestCase(FormationKind.Column)]
        [TestCase(FormationKind.Wedge)]
        [TestCase(FormationKind.Block)]
        public void EverySlot_KeepsAtLeastTheContactSpacing(FormationKind kind)
        {
            var members = Group(9, new Vector2(10, 10), 0.6f);
            var slots = new List<Vector2>();
            Assert.That(FormationPlanner.Plan(kind, members, new Vector2(40, 30), MapMin, MapMax, slots), Is.True);
            Assert.That(slots.Count, Is.EqualTo(members.Count));
            for (int a = 0; a < slots.Count; a++)
            {
                for (int b = a + 1; b < slots.Count; b++)
                {
                    Assert.That((slots[a] - slots[b]).magnitude, Is.GreaterThanOrEqualTo(2f * 0.6f * 0.84f),
                        $"{kind}: slots {a} and {b} overlap");
                }
            }
        }

        [Test]
        public void Line_StandsAcrossTheDirectionOfTravel()
        {
            var members = Group(5, new Vector2(10, 10));
            var slots = new List<Vector2>();
            Vector2 centroid = Vector2.zero;
            foreach (FormationMember m in members) centroid += m.Position;
            centroid /= members.Count;
            Vector2 target = centroid + new Vector2(0f, 30f);    // travel due north
            FormationPlanner.Plan(FormationKind.Line, members, target, MapMin, MapMax, slots);
            foreach (Vector2 s in slots)
            {
                Assert.That(s.y, Is.EqualTo(target.y).Within(0.01f), "a line facing north lies east-west");
            }
        }

        [Test]
        public void Column_RunsAlongTheDirectionOfTravel()
        {
            var members = Group(6, new Vector2(10, 10));
            var slots = new List<Vector2>();
            FormationPlanner.Plan(FormationKind.Column, members, new Vector2(40f, 11f), MapMin, MapMax, slots);
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector2 s in slots)
            {
                minX = Mathf.Min(minX, s.x); maxX = Mathf.Max(maxX, s.x);
                minY = Mathf.Min(minY, s.y); maxY = Mathf.Max(maxY, s.y);
            }
            Assert.That(maxX - minX, Is.GreaterThan(maxY - minY), "a column heading east is longer east-west");
        }

        [Test]
        public void Wedge_PutsOneUnitAtTheTip()
        {
            var members = Group(5, new Vector2(10, 10));
            var slots = new List<Vector2>();
            FormationPlanner.Plan(FormationKind.Wedge, members, new Vector2(10.6f, 40f), MapMin, MapMax, slots);
            float best = float.MinValue;
            int tips = 0;
            foreach (Vector2 s in slots) best = Mathf.Max(best, s.y);
            foreach (Vector2 s in slots) if (Mathf.Abs(s.y - best) < 0.01f) tips++;
            Assert.That(tips, Is.EqualTo(1));
        }

        [Test]
        public void Assignment_DoesNotCrossLeftAndRight()
        {
            // Three units side by side moving north: the westmost keeps the west slot.
            var members = new List<FormationMember>
            {
                new FormationMember(new EntityId(0, 1), new Vector2(20, 10), 0.3f),
                new FormationMember(new EntityId(1, 1), new Vector2(10, 10), 0.3f),
                new FormationMember(new EntityId(2, 1), new Vector2(15, 10), 0.3f),
            };
            var slots = new List<Vector2>();
            FormationPlanner.Plan(FormationKind.Line, members, new Vector2(15, 40), MapMin, MapMax, slots);
            Assert.That(slots[1].x, Is.LessThan(slots[2].x));
            Assert.That(slots[2].x, Is.LessThan(slots[0].x));
        }

        [Test]
        public void Slots_StayInsideTheMap()
        {
            var members = Group(12, new Vector2(3, 3));
            var slots = new List<Vector2>();
            FormationPlanner.Plan(FormationKind.Line, members, new Vector2(0.5f, 0.5f), MapMin, MapMax, slots);
            foreach (Vector2 s in slots)
            {
                Assert.That(s.x, Is.InRange(0f, 127f));
                Assert.That(s.y, Is.InRange(0f, 127f));
            }
        }
    }
}
