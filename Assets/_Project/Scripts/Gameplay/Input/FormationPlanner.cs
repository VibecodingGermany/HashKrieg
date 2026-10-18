using System;
using System.Collections.Generic;
using UnityEngine;
using Nova.Core;
using EntityId = Nova.Core.EntityId;

namespace Nova.Gameplay
{
    /// <summary>Marching orders a group can be told to keep (D-117).</summary>
    public enum FormationKind : byte
    {
        /// <summary>No shape: the simulation spreads the group on rings around the click (D-088).</summary>
        Loose = 0,
        Line = 1,
        Column = 2,
        Wedge = 3,
        Block = 4,
    }

    /// <summary>One unit as the planner sees it: where it stands and how much room it takes.</summary>
    public readonly struct FormationMember
    {
        public readonly EntityId Id;
        public readonly Vector2 Position;
        public readonly float Radius;

        public FormationMember(EntityId id, Vector2 position, float radius)
        {
            Id = id;
            Position = position;
            Radius = radius;
        }
    }

    /// <summary>
    /// Client-side formation slots (D-117): turns one group order into one
    /// destination per unit. The shape is laid out around the clicked point,
    /// facing from the group's centre toward it, and the units are assigned
    /// without crossing — the ones in front take the front row, the ones on
    /// the left the left slots. The simulation then receives a plain Move per
    /// unit; nothing about formations exists in the command register.
    /// <para>
    /// Pure math on the ground plane (x = world X, y = world Z), no Unity
    /// scene — EditMode-tested.
    /// </para>
    /// </summary>
    public static class FormationPlanner
    {
        /// <summary>Gap added to two touching radii between neighbouring slots.</summary>
        public const float SlotGap = 0.35f;

        public static string DisplayName(FormationKind kind)
        {
            switch (kind)
            {
                case FormationKind.Line: return "Linie";
                case FormationKind.Column: return "Kolonne";
                case FormationKind.Wedge: return "Keil";
                case FormationKind.Block: return "Block";
                default: return "Locker";
            }
        }

        /// <summary>
        /// Writes one slot per member (same order as <paramref name="members"/>)
        /// into <paramref name="slots"/>. Returns false for
        /// <see cref="FormationKind.Loose"/> or fewer than two members — the
        /// caller then sends one group Move and lets the simulation spread it.
        /// </summary>
        public static bool Plan(FormationKind kind, IReadOnlyList<FormationMember> members, Vector2 target,
                                Vector2 mapMin, Vector2 mapMax, List<Vector2> slots)
        {
            slots.Clear();
            int n = members.Count;
            if (kind == FormationKind.Loose || n < 2) return false;

            Vector2 centroid = Vector2.zero;
            float maxRadius = 0.3f;
            for (int i = 0; i < n; i++)
            {
                centroid += members[i].Position;
                maxRadius = Mathf.Max(maxRadius, members[i].Radius);
            }
            centroid /= n;

            Vector2 forward = target - centroid;
            forward = forward.sqrMagnitude > 1f ? forward.normalized : Vector2.up;
            var right = new Vector2(forward.y, -forward.x);
            float spacing = 2f * maxRadius + SlotGap;

            // Local slot layout: x = lateral (right), y = forward.
            var local = new List<Vector2>(n);
            Layout(kind, n, spacing, local);

            // Rows front to back; inside a row left to right. Members likewise
            // by how far forward / right they already stand.
            var slotOrder = new int[n];
            var memberOrder = new int[n];
            for (int i = 0; i < n; i++)
            {
                slotOrder[i] = i;
                memberOrder[i] = i;
            }
            var memberLocal = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 d = members[i].Position - centroid;
                memberLocal[i] = new Vector2(Vector2.Dot(d, right), Vector2.Dot(d, forward));
            }
            Array.Sort(slotOrder, (a, b) => CompareRowMajor(local[a], local[b], spacing));
            Array.Sort(memberOrder, (a, b) =>
            {
                int c = memberLocal[b].y.CompareTo(memberLocal[a].y);
                return c != 0 ? c : members[a].Id.Index.CompareTo(members[b].Id.Index);
            });

            // Hand out row by row: the next k frontmost members fill a row of
            // k slots, sorted left to right on both sides.
            var assignment = new int[n];
            int start = 0;
            while (start < n)
            {
                float rowY = local[slotOrder[start]].y;
                int end = start;
                while (end < n && Mathf.Abs(local[slotOrder[end]].y - rowY) < spacing * 0.25f) end++;

                int count = end - start;
                var rowMembers = new int[count];
                Array.Copy(memberOrder, start, rowMembers, 0, count);
                Array.Sort(rowMembers, (a, b) =>
                {
                    int c = memberLocal[a].x.CompareTo(memberLocal[b].x);
                    return c != 0 ? c : members[a].Id.Index.CompareTo(members[b].Id.Index);
                });
                for (int k = 0; k < count; k++) assignment[rowMembers[k]] = slotOrder[start + k];
                start = end;
            }

            for (int i = 0; i < n; i++)
            {
                Vector2 l = local[assignment[i]];
                Vector2 world = target + right * l.x + forward * l.y;
                world.x = Mathf.Clamp(world.x, mapMin.x, mapMax.x);
                world.y = Mathf.Clamp(world.y, mapMin.y, mapMax.y);
                slots.Add(world);
            }
            return true;
        }

        /// <summary>Slot positions centred on the origin, front row at +y.</summary>
        private static void Layout(FormationKind kind, int n, float s, List<Vector2> local)
        {
            switch (kind)
            {
                case FormationKind.Line:
                {
                    int perRow = n <= 12 ? n : Mathf.CeilToInt(n / 2f);
                    Grid(n, perRow, s, local);
                    break;
                }
                case FormationKind.Column:
                {
                    int width = n >= 4 ? 2 : 1;
                    Grid(n, width, s, local);
                    break;
                }
                case FormationKind.Block:
                {
                    int width = Mathf.CeilToInt(Mathf.Sqrt(n));
                    Grid(n, width, s, local);
                    break;
                }
                case FormationKind.Wedge:
                {
                    // Leader at the tip, then pairs falling back to either side.
                    float step = s * 0.85f;
                    for (int i = 0; i < n; i++)
                    {
                        int k = (i + 1) / 2;
                        float side = i == 0 ? 0f : (i % 2 == 1 ? -1f : 1f);
                        local.Add(new Vector2(side * k * step, -k * step));
                    }
                    Centre(local);
                    break;
                }
            }
        }

        /// <summary>Rows of <paramref name="perRow"/>; a short last row is centred.</summary>
        private static void Grid(int n, int perRow, float s, List<Vector2> local)
        {
            int rows = Mathf.CeilToInt(n / (float)perRow);
            for (int i = 0; i < n; i++)
            {
                int row = i / perRow;
                int inRow = Mathf.Min(perRow, n - row * perRow);
                int col = i % perRow;
                local.Add(new Vector2((col - (inRow - 1) / 2f) * s, -row * s));
            }
            Centre(local);
        }

        /// <summary>Shifts the layout so its depth is centred on the click.</summary>
        private static void Centre(List<Vector2> local)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < local.Count; i++)
            {
                minY = Mathf.Min(minY, local[i].y);
                maxY = Mathf.Max(maxY, local[i].y);
            }
            float mid = (minY + maxY) / 2f;
            for (int i = 0; i < local.Count; i++) local[i] = new Vector2(local[i].x, local[i].y - mid);
        }

        private static int CompareRowMajor(Vector2 a, Vector2 b, float spacing)
        {
            if (Mathf.Abs(a.y - b.y) >= spacing * 0.25f) return b.y.CompareTo(a.y);
            return a.x.CompareTo(b.x);
        }
    }
}
