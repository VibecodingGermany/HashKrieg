using System.Collections.Generic;
using UnityEngine;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// What the view knows about one entity this frame — everything the rig
    /// animation reads. Pure view data: nothing here flows back into the
    /// simulation (docs/tech/AnimationSystem.md, Architektur-Grundsatz 1).
    /// </summary>
    public struct UnitRigInput
    {
        /// <summary>Ground speed of the VIEW in metres per second (measured, not the sim's nominal speed).</summary>
        public float Speed;

        /// <summary>Yaw rate of the view in degrees per second, positive clockwise from above.</summary>
        public float TurnRate;

        public bool HasTarget;
        public Vector3 TargetPosition;

        /// <summary>True on the frame a shot of this entity became visible.</summary>
        public bool Fired;

        /// <summary>Harvesting, building, producing: the role's "busy" motion runs.</summary>
        public bool Working;
    }

    /// <summary>
    /// Rig-less code animation for vehicles, infantry and buildings
    /// (docs/tech/AnimationSystem.md §Rig-lose Code-/Transform-Animation).
    /// <para>
    /// The art pass (<c>tools/art/asset_pass.py</c>) splits a model into
    /// pivoted nodes and names them; this component finds them by name and
    /// drives them from <see cref="UnitRigInput"/>:
    /// </para>
    /// <list type="bullet">
    /// <item><c>Body</c> — suspension: nods on acceleration, leans into turns,
    /// bobs while driving (infantry: steps).</item>
    /// <item><c>Turret</c> — yaws toward the attack target with a capped rate,
    /// returns to the bow when idle.</item>
    /// <item><c>Gun</c> — recoils on every visible shot.</item>
    /// <item><c>Wheel_*</c> — roll with the distance driven.</item>
    /// <item><c>Spin_*</c> — turn continuously (radar dishes, rotors).</item>
    /// <item><c>Scan_*</c> — sweep back and forth (dishes mounted off-axis).</item>
    /// <item><c>Arm_*</c> — swing while the unit works.</item>
    /// </list>
    /// <para>
    /// Axes are measured once in each node's parent space from the root's
    /// up/right/forward, so the rig stays correct whatever axis baking the
    /// FBX import applied. A prefab without a <c>Body</c> node gets one
    /// inserted at bind time, so old single-mesh prefabs still move.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnitRigAnimator : MonoBehaviour
    {
        public const string BodyNode = "Body";
        public const string TurretNode = "Turret";
        public const string GunNode = "Gun";
        public const string WheelPrefix = "Wheel_";
        public const string SpinPrefix = "Spin_";
        public const string ArmPrefix = "Arm_";
        public const string ScanPrefix = "Scan_";

        [SerializeField] private float _turretTurnRate = 140f;
        [SerializeField] private float _spinRate = 55f;
        [SerializeField] private float _recoilDistance = 0.35f;
        [SerializeField] private float _recoilRecovery = 0.35f;
        [SerializeField] private float _armSwingDegrees = 14f;
        [SerializeField] private bool _infantryGait;
        [SerializeField] private bool _emissivePulse;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static MaterialPropertyBlock _glowBlock;

        private struct Node
        {
            public Transform T;
            public Vector3 RestPos;
            public Quaternion RestRot;
            public Vector3 Up;       // root up, in parent space
            public Vector3 Right;    // root right, in parent space
            public Vector3 Forward;  // root forward, in parent space
            public float Scale;      // one root-local unit, measured in parent-space units
            public float Radius;     // wheels only
            public float Phase;
        }

        private bool _bound;
        private Node _body;
        private bool _hasBody;
        private Node _turret;
        private bool _hasTurret;
        private Node _gun;
        private bool _hasGun;
        private readonly List<Node> _wheels = new List<Node>(4);
        private readonly List<Node> _spins = new List<Node>(2);
        private readonly List<Node> _arms = new List<Node>(2);
        private readonly List<Node> _scans = new List<Node>(1);

        private Renderer[] _glowRenderers = System.Array.Empty<Renderer>();
        private Color[] _glowBase = System.Array.Empty<Color>();
        private float _glowPhase;
        private float _glowBusy;

        private float _size = 1f;
        private float _turretYaw;
        private float _recoil;
        private float _bobPhase;
        private float _wheelAngle;
        private float _spinAngle;
        private float _armPhase;
        private float _armBlend;
        private float _lastSpeed;
        private float _pitch;
        private float _roll;
        private float _moveBlend;

        /// <summary>True when the model carries a turret node: the hull then keeps its heading while fighting.</summary>
        public bool HasTurret
        {
            get { EnsureBound(); return _hasTurret; }
        }

        /// <summary>Walks like a soldier (step bob and sway) instead of riding a suspension.</summary>
        public bool InfantryGait
        {
            get => _infantryGait;
            set => _infantryGait = value;
        }

        /// <summary>
        /// Buildings breathe through their light strips (the _EM map of the art
        /// pass) and pulse faster while they work. Units keep a steady glow.
        /// </summary>
        public bool EmissivePulse
        {
            get => _emissivePulse;
            set => _emissivePulse = value;
        }

        /// <summary>Back to the authored pose; called when the view returns to its pool.</summary>
        public void ResetPose()
        {
            EnsureBound();
            _turretYaw = 0f; _recoil = 0f; _bobPhase = 0f; _wheelAngle = 0f;
            _armPhase = 0f; _armBlend = 0f; _lastSpeed = 0f; _pitch = 0f; _roll = 0f; _moveBlend = 0f;
            Restore(_body, _hasBody);
            Restore(_turret, _hasTurret);
            Restore(_gun, _hasGun);
            for (int i = 0; i < _wheels.Count; i++) Restore(_wheels[i], true);
            for (int i = 0; i < _spins.Count; i++) Restore(_spins[i], true);
            for (int i = 0; i < _arms.Count; i++) Restore(_arms[i], true);
            for (int i = 0; i < _scans.Count; i++) Restore(_scans[i], true);
        }

        /// <summary>Advances every animated node by one rendered frame.</summary>
        public void Tick(in UnitRigInput input, float dt)
        {
            EnsureBound();
            if (dt <= 0f) return;

            float speed = input.Speed;
            float moving = speed > 0.15f ? 1f : 0f;
            _moveBlend = Mathf.MoveTowards(_moveBlend, moving, dt * 4f);

            TickBody(input, speed, dt);
            TickTurret(input, dt);
            TickGun(input, dt);

            _wheelAngle += speed * dt;
            for (int i = 0; i < _wheels.Count; i++)
            {
                Node w = _wheels[i];
                float deg = _wheelAngle / Mathf.Max(0.05f, w.Radius) * Mathf.Rad2Deg;
                w.T.localRotation = Quaternion.AngleAxis(deg, w.Right) * w.RestRot;
            }

            _spinAngle = Mathf.Repeat(_spinAngle + _spinRate * dt, 360f);
            for (int i = 0; i < _spins.Count; i++)
            {
                Node s = _spins[i];
                s.T.localRotation = Quaternion.AngleAxis(_spinAngle + s.Phase, s.Up) * s.RestRot;
            }

            for (int i = 0; i < _scans.Count; i++)
            {
                Node s = _scans[i];
                float sweep = Mathf.Sin((_spinAngle + s.Phase) * Mathf.Deg2Rad * 0.6f) * 50f;
                s.T.localRotation = Quaternion.AngleAxis(sweep, s.Up) * s.RestRot;
            }

            _armBlend = Mathf.MoveTowards(_armBlend, input.Working ? 1f : 0f, dt * 2f);
            _armPhase += dt * 2.6f;
            for (int i = 0; i < _arms.Count; i++)
            {
                Node a = _arms[i];
                float swing = Mathf.Sin(_armPhase + a.Phase) * _armSwingDegrees * _armBlend;
                a.T.localRotation = Quaternion.AngleAxis(swing, a.Right) * a.RestRot;
            }

            TickGlow(input.Working, dt);
            _lastSpeed = speed;
        }

        private void TickGlow(bool working, float dt)
        {
            if (!_emissivePulse || _glowRenderers.Length == 0) return;
            _glowBusy = Mathf.MoveTowards(_glowBusy, working ? 1f : 0f, dt * 1.5f);
            _glowPhase += dt * Mathf.Lerp(1.4f, 5.5f, _glowBusy);
            float k = 1f + Mathf.Sin(_glowPhase) * Mathf.Lerp(0.22f, 0.55f, _glowBusy) + 0.25f * _glowBusy;
            if (_glowBlock == null) _glowBlock = new MaterialPropertyBlock();
            for (int i = 0; i < _glowRenderers.Length; i++)
            {
                Renderer r = _glowRenderers[i];
                if (r == null || !r.isVisible) continue;
                // Read-modify-write keeps the faction tint UnitViewManager put
                // into the same block.
                r.GetPropertyBlock(_glowBlock);
                _glowBlock.SetColor(EmissionColorId, _glowBase[i] * k);
                r.SetPropertyBlock(_glowBlock);
            }
        }

        /// <summary>
        /// Editor/preview hook (ArtPreviewCapture): a representative mid-drive,
        /// mid-fight pose after <paramref name="seconds"/> of simulated time.
        /// </summary>
        public void PreviewPose(float seconds)
        {
            ResetPose();
            var input = new UnitRigInput
            {
                Speed = 4f,
                TurnRate = 25f,
                HasTarget = true,
                TargetPosition = transform.position + transform.rotation * new Vector3(20f, 0f, 12f),
                Working = true,
            };
            const float step = 1f / 30f;
            for (float t = 0f; t < seconds; t += step)
            {
                input.Fired = t + step >= seconds - 0.05f && t < seconds - 0.05f;
                Tick(input, step);
            }
        }

        private void TickBody(in UnitRigInput input, float speed, float dt)
        {
            if (!_hasBody) return;

            float accel = (speed - _lastSpeed) / dt;
            Vector3 offset = Vector3.zero;
            float pitchTarget, rollTarget;

            if (_infantryGait)
            {
                // Two steps per ~1.4 m: a vertical bob at step rate, a sway at
                // stride rate and a slight forward lean while moving.
                _bobPhase += speed * dt / 0.7f * Mathf.PI;
                float bob = Mathf.Abs(Mathf.Sin(_bobPhase)) * 0.06f * _size * _moveBlend;
                offset = _body.Up * bob;
                pitchTarget = 6f * _moveBlend;
                rollTarget = Mathf.Sin(_bobPhase) * 4f * _moveBlend;
            }
            else
            {
                // Suspension: nose dips when braking and lifts on throttle,
                // the hull leans out of turns, and a small road-noise bob.
                _bobPhase += dt * (6f + speed * 1.5f);
                float bob = (Mathf.Sin(_bobPhase) * 0.6f + Mathf.Sin(_bobPhase * 2.3f) * 0.4f) * 0.012f * _size * _moveBlend;
                offset = _body.Up * bob;
                pitchTarget = Mathf.Clamp(-accel * 0.9f, -3.5f, 3.5f);
                rollTarget = Mathf.Clamp(-input.TurnRate * 0.05f, -3f, 3f) * _moveBlend;
            }

            float k = 1f - Mathf.Exp(-dt * 8f);
            _pitch = Mathf.Lerp(_pitch, pitchTarget, k);
            _roll = Mathf.Lerp(_roll, rollTarget, k);

            _body.T.localPosition = _body.RestPos + offset * _body.Scale;
            _body.T.localRotation = Quaternion.AngleAxis(_pitch, _body.Right)
                                    * Quaternion.AngleAxis(_roll, _body.Forward)
                                    * _body.RestRot;
        }

        private void TickTurret(in UnitRigInput input, float dt)
        {
            if (!_hasTurret) return;

            float desired = 0f;
            if (input.HasTarget)
            {
                Vector3 local = transform.InverseTransformPoint(input.TargetPosition);
                if (local.x * local.x + local.z * local.z > 0.01f)
                {
                    desired = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                }
            }
            float delta = Mathf.DeltaAngle(_turretYaw, desired);
            _turretYaw += Mathf.Clamp(delta, -_turretTurnRate * dt, _turretTurnRate * dt);
            _turret.T.localRotation = Quaternion.AngleAxis(_turretYaw, _turret.Up) * _turret.RestRot;
        }

        private void TickGun(in UnitRigInput input, float dt)
        {
            if (!_hasGun) return;
            if (input.Fired) _recoil = 1f;
            _recoil = Mathf.MoveTowards(_recoil, 0f, dt / Mathf.Max(0.05f, _recoilRecovery));
            // Fast kick, eased return.
            float back = _recoil * _recoil * _recoilDistance * _size * 0.25f;
            _gun.T.localPosition = _gun.RestPos - _gun.Forward * (back * _gun.Scale);
        }

        private void EnsureBound()
        {
            if (_bound) return;
            _bound = true;

            // Offsets are written in the root's LOCAL space, so the footprint
            // is measured there too: UnitViewManager scales the instance root
            // to the role's view size, and a world-space size would apply
            // that scale twice.
            Bounds b = RendererBounds(transform);
            float rootScale = Mathf.Max(1e-4f, transform.lossyScale.x);
            _size = Mathf.Max(0.5f, Mathf.Max(b.size.x, b.size.z) / rootScale);

            Transform body = FindDeep(transform, BodyNode);
            if (body == null)
            {
                body = InsertBody();
            }
            _hasBody = body != null;
            if (_hasBody) _body = Capture(body);

            Transform turret = FindDeep(transform, TurretNode);
            _hasTurret = turret != null;
            if (_hasTurret) _turret = Capture(turret);

            Transform gun = FindDeep(transform, GunNode);
            _hasGun = gun != null;
            if (_hasGun) _gun = Capture(gun);

            var glowing = new List<Renderer>();
            var glowBase = new List<Color>();
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                Material m = r.sharedMaterial;
                if (m == null || !m.IsKeywordEnabled("_EMISSION") || !m.HasProperty(EmissionColorId)) continue;
                glowing.Add(r);
                glowBase.Add(m.GetColor(EmissionColorId));
            }
            _glowRenderers = glowing.ToArray();
            _glowBase = glowBase.ToArray();
            _glowPhase = Random.value * 6.28f;

            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                string n = t.name;
                if (n.StartsWith(WheelPrefix, System.StringComparison.Ordinal))
                {
                    Node w = Capture(t);
                    w.Radius = Mathf.Max(0.05f, RendererBounds(t).extents.y);
                    _wheels.Add(w);
                }
                else if (n.StartsWith(SpinPrefix, System.StringComparison.Ordinal))
                {
                    Node s = Capture(t);
                    s.Phase = _spins.Count * 47f;
                    _spins.Add(s);
                }
                else if (n.StartsWith(ScanPrefix, System.StringComparison.Ordinal))
                {
                    _scans.Add(Capture(t));
                }
                else if (n.StartsWith(ArmPrefix, System.StringComparison.Ordinal))
                {
                    Node a = Capture(t);
                    a.Phase = _arms.Count * 1.7f;
                    _arms.Add(a);
                }
            }
        }

        /// <summary>
        /// Single-mesh prefabs have no suspension node: wrap every direct child
        /// in a fresh <c>Body</c> so the bob/lean has something to move without
        /// fighting the root transform that UnitViewManager writes.
        /// </summary>
        private Transform InsertBody()
        {
            if (transform.childCount == 0) return null;
            var go = new GameObject(BodyNode);
            Transform body = go.transform;
            body.SetParent(transform, false);
            var children = new List<Transform>(transform.childCount);
            foreach (Transform child in transform)
            {
                if (child != body) children.Add(child);
            }
            for (int i = 0; i < children.Count; i++)
            {
                children[i].SetParent(body, true);
            }
            return body;
        }

        private Node Capture(Transform t)
        {
            Transform parent = t.parent != null ? t.parent : transform;
            return new Node
            {
                T = t,
                RestPos = t.localPosition,
                RestRot = t.localRotation,
                Up = parent.InverseTransformDirection(transform.up).normalized,
                Right = parent.InverseTransformDirection(transform.right).normalized,
                Forward = parent.InverseTransformDirection(transform.forward).normalized,
                // Blender FBX hierarchies import with a x100 "Root" node: a
                // translation must be converted into the parent's units, a
                // rotation axis need not.
                Scale = parent.InverseTransformVector(transform.TransformVector(Vector3.up)).magnitude,
            };
        }

        private static void Restore(Node n, bool has)
        {
            if (!has || n.T == null) return;
            n.T.localPosition = n.RestPos;
            n.T.localRotation = n.RestRot;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform hit = FindDeep(child, name);
                if (hit != null) return hit;
            }
            return null;
        }

        private static Bounds RendererBounds(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(t.position, Vector3.one);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
