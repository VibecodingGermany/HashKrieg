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

        /// <summary>Harvester load, 0 (empty) .. 1 (full). Drives the cargo pile.</summary>
        public float CargoFill;

        /// <summary>True on the frame the harvester's load was unloaded (cargo &gt; 0 to 0).</summary>
        public bool Dump;

        /// <summary>Fill the harvester carried right before the unload; read when <see cref="Dump"/> is set.</summary>
        public float DumpFromFill;
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
    /// <item><c>Smoke_*</c> — chimneys: puff smoke, faster while the building works.</item>
    /// <item>Ambient: vehicles kick up sand while driving and idle-rumble when
    /// standing, turrets scan the ground when they have no target, infantry
    /// breathe and look around (D-115).</item>
    /// <item><c>Bed</c> — dump body of a harvester, pivoted at its rear hinge: tips
    /// back when the load is unloaded. A procedural Aetherium pile rides on it and
    /// grows with the fill; models without a bed (<c>Cargo_Anchor</c> only) carry
    /// the pile on their back and tip the whole body instead.</item>
    /// <item><c>Leg_*</c> / <c>Shin_*</c> — rigid thighs and shins of infantry:
    /// a two-joint walk cycle whose cadence follows the measured speed, so feet
    /// do not slide; <c>_R</c> runs half a cycle behind <c>_L</c>.</item>
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
        public const string LegPrefix = "Leg_";
        public const string ShinPrefix = "Shin_";
        public const string SmokePrefix = "Smoke_";
        public const string BedNode = "Bed";
        public const string CargoAnchorNode = "Cargo_Anchor";

        /// <summary>How far the dump bed tips back, in degrees.</summary>
        public const float BedTiltDegrees = 50f;

        /// <summary>How far a bed-less harvester leans back to shed its load, in degrees.</summary>
        public const float BodyTiltDegrees = 11f;

        /// <summary>Thigh swing to either side of vertical, in degrees.</summary>
        private const float ThighSwing = 25f;

        /// <summary>Knee bend at the height of the swing phase, in degrees.</summary>
        private const float KneeBend = 35f;

        /// <summary>
        /// Cadence ceiling. Views are scaled to their graybox size while the
        /// sim moves infantry 4 cells/s — about 1 m of soldier sprinting
        /// 4 m/s. A slide-free cadence would be ~9 steps/s and read as
        /// twitching; above this rate the feet slide a little instead.
        /// </summary>
        private const float MaxStepsPerSecond = 3.2f;

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
        private readonly List<Node> _legs = new List<Node>(2);
        private readonly List<Node> _shins = new List<Node>(2);
        private Node _bed;
        private bool _hasBed;
        private Transform _cargoAnchor;
        private CargoPile _pile;
        private HarvesterDumpCycle _dump;
        private float _bedSign = -1f;       // measured at bind: which way lifts the front
        private float _bodyTiltSign = -1f;
        private float _cargoShown;
        private bool _cargoInit;
        private float _frozenFill;
        private float _workBlend;
        private float _workDustTimer;
        private float _burstAccum;
        private float _bodyTiltDeg;
        private float _armShake;
        private Vector3 _tiltPivotBody;     // body-local point that stays put while the body leans back
        private Vector3 _tiltPivotRoot;     // the same point in root-local space at rest
        private float _hipHeight;     // root-local units, from the Leg_ pivots
        private float _gaitPhase;
        private float _fireKick;

        private readonly List<Transform> _smokes = new List<Transform>(2);
        private float _time;
        private float _idleTime;
        private float _dustAccum;
        private float _smokeTimer;
        private float _lookYaw;
        private float _lookTarget;
        private float _lookTimer;
        private float _footprintWorld = 1f;

        private Renderer[] _glowRenderers = System.Array.Empty<Renderer>();
        private Color[] _glowBase = System.Array.Empty<Color>();
        private float _glowPhase;
        private float _glowInitialPhase;
        private float _glowBusy;

        private float _size = 1f;
        private float _turretYaw;
        private float _recoil;
        private float _bobPhase;
        private float _wheelAngle;
        private float _spinAngle;
        private float _scanPhase;
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
            _armShake = 0f;
            _spinAngle = 0f; _scanPhase = 0f; _time = 0f;
            _lookTarget = 0f; _lookTimer = 0f; _smokeTimer = 0f;
            _glowPhase = _glowInitialPhase; _glowBusy = 0f;
            ApplyGlow(1f, false);
            _armPhase = 0f; _armBlend = 0f; _lastSpeed = 0f; _pitch = 0f; _roll = 0f; _moveBlend = 0f;
            Restore(_body, _hasBody);
            Restore(_turret, _hasTurret);
            Restore(_gun, _hasGun);
            for (int i = 0; i < _wheels.Count; i++) Restore(_wheels[i], true);
            for (int i = 0; i < _spins.Count; i++) Restore(_spins[i], true);
            for (int i = 0; i < _arms.Count; i++) Restore(_arms[i], true);
            for (int i = 0; i < _scans.Count; i++) Restore(_scans[i], true);
            for (int i = 0; i < _legs.Count; i++) Restore(_legs[i], true);
            for (int i = 0; i < _shins.Count; i++) Restore(_shins[i], true);
            _gaitPhase = 0f;
            _fireKick = 0f;
            _idleTime = 0f;
            _lookYaw = 0f;
            _dustAccum = 0f;
            ResetCargo();
        }

        /// <summary>True when the model can carry a load (a dump bed or a back anchor).</summary>
        public bool CarriesCargo
        {
            get { EnsureBound(); return _pile != null; }
        }

        /// <summary>True while the unload sequence plays.</summary>
        public bool Dumping => _dump.Active;

        /// <summary>Bed tilt, 0 (down) .. 1 (fully tipped); 0 for models without a bed.</summary>
        public float BedTilt01 => _dump.Tilt01;

        /// <summary>The procedural cargo pile, or null when the model carries none.</summary>
        public CargoPile Pile
        {
            get { EnsureBound(); return _pile; }
        }

        /// <summary>The dump bed node, or null.</summary>
        public Transform BedTransform
        {
            get { EnsureBound(); return _hasBed ? _bed.T : null; }
        }

        private void ResetCargo()
        {
            _dump.Reset();
            _cargoShown = 0f;
            _cargoInit = false;
            _frozenFill = 0f;
            _workBlend = 0f;
            _workDustTimer = 0f;
            _burstAccum = 0f;
            _bodyTiltDeg = 0f;
            Restore(_bed, _hasBed);
            if (_pile != null) _pile.Apply(0f, 0f);
        }

        /// <summary>Advances every animated node by one rendered frame.</summary>
        public void Tick(in UnitRigInput input, float dt)
        {
            EnsureBound();
            if (dt <= 0f) return;

            float speed = input.Speed;
            float moving = speed > 0.15f ? 1f : 0f;
            _moveBlend = Mathf.MoveTowards(_moveBlend, moving, dt * 4f);
            _time += dt;
            _idleTime = moving > 0f || input.HasTarget ? 0f : _idleTime + dt;

            TickDust(speed, dt);
            TickSmoke(input.Working, dt);
            TickCargo(in input, dt);

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

            // A sweep has its own full period; scaling a wrapped spin angle jumps at 360 degrees.
            _scanPhase = Mathf.Repeat(_scanPhase + _spinRate * Mathf.Deg2Rad * 0.6f * dt, Mathf.PI * 2f);
            for (int i = 0; i < _scans.Count; i++)
            {
                Node s = _scans[i];
                float sweep = Mathf.Sin(_scanPhase + s.Phase * Mathf.Deg2Rad * 0.6f) * 50f;
                s.T.localRotation = Quaternion.AngleAxis(sweep, s.Up) * s.RestRot;
            }

            _armBlend = Mathf.MoveTowards(_armBlend, input.Working ? 1f : 0f, dt * 2f);
            _armPhase += dt * 2.6f;
            for (int i = 0; i < _arms.Count; i++)
            {
                Node a = _arms[i];
                float swing = Mathf.Sin(_armPhase + a.Phase) * _armSwingDegrees * _armBlend + _armShake;
                a.T.localRotation = Quaternion.AngleAxis(swing, a.Right) * a.RestRot;
            }

            TickGlow(input.Working, dt);
            _lastSpeed = speed;
        }

        /// <summary>
        /// Harvester load: the pile follows the fill, an unload plays the dump
        /// sequence (bed or body tips, pile slides out in a burst of glowing
        /// dust, everything returns) and a working harvester labours.
        /// </summary>
        private void TickCargo(in UnitRigInput input, float dt)
        {
            if (_pile == null) return;

            _workBlend = Mathf.MoveTowards(_workBlend, input.Working && !_dump.Active ? 1f : 0f, dt * 2.5f);
            TickWorkDust(dt);

            if (input.Dump)
            {
                _dump.Start();
                _frozenFill = Mathf.Clamp01(input.DumpFromFill > 0f ? input.DumpFromFill : Mathf.Max(_cargoShown, 0.5f));
                _cargoShown = 0f;
                _burstAccum = 0f;
            }
            _dump.Advance(dt);

            float fill, release = 0f;
            if (_dump.Active)
            {
                fill = _frozenFill;
                release = _dump.Release01;
            }
            else
            {
                float target = Mathf.Clamp01(input.CargoFill);
                if (!_cargoInit)
                {
                    _cargoShown = target;
                    _cargoInit = true;
                }
                else
                {
                    // Eases toward the sim value: the sim fills in steps, the view should not.
                    _cargoShown = Mathf.Lerp(_cargoShown, target, 1f - Mathf.Exp(-dt * 3f));
                    if (Mathf.Abs(_cargoShown - target) < 0.001f) _cargoShown = target;
                }
                fill = _cargoShown;
            }

            float tilt = _dump.Tilt01;
            bool releasing = _dump.Active && release > 0f && release < 1f;
            // The bed shudders while the load lets go.
            float shake = releasing ? Mathf.Sin(_time * 46f) * 1.4f * Mathf.Sin(release * Mathf.PI) : 0f;
            if (_hasBed)
            {
                _bed.T.localRotation = Quaternion.AngleAxis(_bedSign * (BedTiltDegrees * tilt + shake), _bed.Right) * _bed.RestRot;
            }
            else
            {
                _bodyTiltDeg = BodyTiltDegrees * tilt;
            }
            _armShake = releasing ? Mathf.Sin(_time * 17f) * 16f * Mathf.Sin(release * Mathf.PI) : 0f;

            _pile.Apply(fill, release);

            if (releasing)
            {
                _burstAccum += dt * 55f;
                Vector3 back = -transform.forward;
                while (_burstAccum >= 1f)
                {
                    _burstAccum -= 1f;
                    Vector3 pos = _pile.WorldCenter + Random.insideUnitSphere * (_footprintWorld * 0.12f);
                    Vector3 drift = back * Random.Range(0.4f, 1.1f) + Vector3.up * Random.Range(0.2f, 0.6f);
                    float size = _footprintWorld * Random.Range(0.10f, 0.20f);
                    AmbientFx.GlowDust(pos, drift, size);
                    if (Random.value < 0.5f) AmbientFx.Dust(pos - transform.up * (_footprintWorld * 0.08f), drift * 0.5f, size * 1.4f);
                }
            }
        }

        /// <summary>Digging while harvesting: a little sand now and then in front of the vehicle.</summary>
        private void TickWorkDust(float dt)
        {
            if (_workBlend < 0.8f) return;
            _workDustTimer -= dt;
            if (_workDustTimer > 0f) return;
            _workDustTimer = Random.Range(0.35f, 0.6f);
            Vector3 pos = transform.position + transform.forward * (_footprintWorld * 0.5f)
                          + transform.right * Random.Range(-0.25f, 0.25f) * _footprintWorld;
            AmbientFx.Dust(pos, transform.forward * 0.3f, _footprintWorld * 0.22f);
        }

        private void TickGlow(bool working, float dt)
        {
            if (!_emissivePulse || _glowRenderers.Length == 0) return;
            _glowBusy = Mathf.MoveTowards(_glowBusy, working ? 1f : 0f, dt * 1.5f);
            _glowPhase += dt * Mathf.Lerp(1.4f, 5.5f, _glowBusy);
            float k = 1f + Mathf.Sin(_glowPhase) * Mathf.Lerp(0.22f, 0.55f, _glowBusy) + 0.25f * _glowBusy;
            ApplyGlow(k, true);
        }

        private void ApplyGlow(float intensity, bool visibleOnly)
        {
            if (_glowBlock == null) _glowBlock = new MaterialPropertyBlock();
            for (int i = 0; i < _glowRenderers.Length; i++)
            {
                Renderer r = _glowRenderers[i];
                if (r == null || (visibleOnly && !r.isVisible)) continue;
                // Read-modify-write keeps the faction tint UnitViewManager put
                // into the same block.
                r.GetPropertyBlock(_glowBlock);
                _glowBlock.SetColor(EmissionColorId, _glowBase[i] * intensity);
                r.SetPropertyBlock(_glowBlock);
            }
        }

        /// <summary>
        /// Editor/preview hook (ArtPreviewCapture): a representative mid-drive,
        /// mid-fight pose after <paramref name="seconds"/> of simulated time.
        /// </summary>
        public void PreviewPose(float seconds) => PreviewPose(seconds, -1f);

        /// <summary>
        /// As <see cref="PreviewPose(float)"/>; with <paramref name="dumpSeconds"/> &gt;= 0 a harvester
        /// then unloads and is shown that far into the dump (ArtPreviewCapture -captureDump).
        /// </summary>
        public void PreviewPose(float seconds, float dumpSeconds)
        {
            ResetPose();
            var input = new UnitRigInput
            {
                Speed = 4f,
                TurnRate = 25f,
                HasTarget = true,
                TargetPosition = transform.position + transform.rotation * new Vector3(20f, 0f, 12f),
                Working = true,
                CargoFill = 0.5f,
            };
            const float step = 1f / 30f;
            for (float t = 0f; t < seconds; t += step)
            {
                input.Fired = t + step >= seconds - 0.05f && t < seconds - 0.05f;
                Tick(input, step);
            }
            if (dumpSeconds < 0f) return;
            var unload = new UnitRigInput { CargoFill = 0f, Dump = true, DumpFromFill = 0.9f };
            Tick(unload, step);
            unload.Dump = false;
            for (float t = step; t < dumpSeconds; t += step) Tick(unload, step);
        }

        private void TickBody(in UnitRigInput input, float speed, float dt)
        {
            if (!_hasBody) return;

            float accel = (speed - _lastSpeed) / dt;
            Vector3 offset = Vector3.zero;
            float pitchTarget, rollTarget;

            if (input.Fired) _fireKick = 1f;
            _fireKick = Mathf.MoveTowards(_fireKick, 0f, dt * 5f);

            if (_legs.Count > 0)
            {
                TickLegs(speed, dt);
                // Rigid legs get shorter as they spread (cos of the swing); the
                // hips drop by exactly that, so the feet stay on the ground.
                float spread = Mathf.Abs(Mathf.Sin(_gaitPhase)) * ThighSwing * _moveBlend;
                offset = -_body.Up * (_hipHeight * (1f - Mathf.Cos(spread * Mathf.Deg2Rad)));
                pitchTarget = 5f * _moveBlend - 6f * _fireKick;
                rollTarget = Mathf.Sin(_gaitPhase) * 3f * _moveBlend;
                // Standing: breathe, and now and then look around.
                float rest = 1f - _moveBlend;
                offset += _body.Up * (Mathf.Sin(_time * 1.6f) * 0.006f * _size * rest);
                // Root heading already tracks combat aim; an idle glance must not turn the weapon away.
                if (input.HasTarget || input.Fired) _lookYaw = 0f;
                else TickLook(dt, rest);
            }
            else if (_infantryGait)
            {
                // No leg nodes: a vertical bob at step rate, a sway at stride
                // rate and a slight forward lean while moving.
                _bobPhase += speed * dt / 0.7f * Mathf.PI;
                float bob = Mathf.Abs(Mathf.Sin(_bobPhase)) * 0.06f * _size * _moveBlend;
                offset = _body.Up * bob;
                pitchTarget = 6f * _moveBlend - 6f * _fireKick;
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
                if (_workBlend > 0f)
                {
                    // Harvesting: the engine labours (fast small shake) and the
                    // vehicle nods slowly with the pull of the load.
                    offset += _body.Up * ((Mathf.Sin(_time * 33f) * 0.0045f + Mathf.Sin(_time * 21f) * 0.003f) * _size * _workBlend);
                    pitchTarget += Mathf.Sin(_time * 2.1f) * 1.1f * _workBlend;
                }
                rollTarget = Mathf.Clamp(-input.TurnRate * 0.05f, -3f, 3f) * _moveBlend;
                if (!_emissivePulse)
                {
                    // Engine idle: a faint high-frequency rumble while standing.
                    float idle = (1f - _moveBlend) * 0.0025f * _size;
                    offset += _body.Up * (Mathf.Sin(_time * 47f) * idle + Mathf.Sin(_time * 31f) * idle * 0.6f);
                }
            }

            float k = 1f - Mathf.Exp(-dt * 8f);
            _pitch = Mathf.Lerp(_pitch, pitchTarget, k);
            _roll = Mathf.Lerp(_roll, rollTarget, k);

            _body.T.localPosition = _body.RestPos + offset * _body.Scale;
            _body.T.localRotation = Quaternion.AngleAxis(_lookYaw, _body.Up)
                                    * Quaternion.AngleAxis(_pitch + _bodyTiltSign * _bodyTiltDeg, _body.Right)
                                    * Quaternion.AngleAxis(_roll, _body.Forward)
                                    * _body.RestRot;
            if (_bodyTiltDeg > 0.01f) KeepTiltPivotInPlace();
        }

        /// <summary>
        /// Leaning back about the body origin would sink the rear into the ground;
        /// the body is shifted so the rear-bottom point stays where it was.
        /// </summary>
        private void KeepTiltPivotInPlace()
        {
            Vector3 now = transform.InverseTransformPoint(_body.T.TransformPoint(_tiltPivotBody));
            Vector3 worldDelta = transform.TransformVector(_tiltPivotRoot - now);
            Transform parent = _body.T.parent != null ? _body.T.parent : transform;
            _body.T.localPosition += parent.InverseTransformVector(worldDelta);
        }

        /// <summary>Infantry glance: a new look direction every few seconds, eased.</summary>
        private void TickLook(float dt, float rest)
        {
            _lookTimer -= dt;
            if (_lookTimer <= 0f)
            {
                _lookTimer = Random.Range(2.5f, 5.5f);
                _lookTarget = Random.value < 0.4f ? 0f : Random.Range(-22f, 22f);
            }
            _lookYaw = Mathf.MoveTowards(_lookYaw, _lookTarget * rest, dt * 40f);
        }

        /// <summary>Sand kicked up behind a driving vehicle, one puff per stretch of ground.</summary>
        private void TickDust(float speed, float dt)
        {
            if (_infantryGait || _legs.Count > 0 || _emissivePulse || speed < 0.3f) return;
            _dustAccum += speed * dt;
            float every = Mathf.Max(0.18f, _footprintWorld * 0.35f);
            if (_dustAccum < every) return;
            _dustAccum = 0f;
            Vector3 back = -transform.forward * (_footprintWorld * 0.45f);
            Vector3 side = transform.right * (_footprintWorld * 0.3f);
            Vector3 drift = -transform.forward * speed * 0.15f;
            float size = _footprintWorld * 0.45f;
            AmbientFx.Dust(transform.position + back + side, drift, size);
            AmbientFx.Dust(transform.position + back - side, drift, size);
        }

        /// <summary>Chimney smoke: a steady puff, twice as often while working.</summary>
        private void TickSmoke(bool working, float dt)
        {
            if (_smokes.Count == 0) return;
            _smokeTimer -= dt;
            if (_smokeTimer > 0f) return;
            _smokeTimer = working ? 0.22f : 0.5f;
            float size = _footprintWorld * 0.16f;
            for (int i = 0; i < _smokes.Count; i++)
            {
                if (_smokes[i] != null) AmbientFx.Smoke(_smokes[i].position, size, working ? 0.55f : 0.35f);
            }
        }

        /// <summary>
        /// Walk cycle: half a cycle per step, one step per ~0.85 hip heights
        /// of ground covered (capped at <see cref="MaxStepsPerSecond"/>). Thighs swing about the hip (negative angle =
        /// foot forward), the knee folds while the leg swings forward
        /// (cos &gt; 0) and straightens for the stance.
        /// </summary>
        private void TickLegs(float speed, float dt)
        {
            float rootScale = Mathf.Max(1e-4f, transform.lossyScale.y);
            float stride = Mathf.Max(0.05f, _hipHeight * rootScale * 0.85f);
            float stepsPerSecond = Mathf.Min(speed / stride, MaxStepsPerSecond);
            _gaitPhase = Mathf.Repeat(_gaitPhase + stepsPerSecond * dt * Mathf.PI, Mathf.PI * 2f);
            if (_moveBlend <= 0f) _gaitPhase = Mathf.MoveTowards(_gaitPhase, Mathf.Round(_gaitPhase / Mathf.PI) * Mathf.PI, dt * 4f);

            for (int i = 0; i < _legs.Count; i++)
            {
                Node leg = _legs[i];
                float thigh = -Mathf.Sin(_gaitPhase + leg.Phase) * ThighSwing * _moveBlend;
                leg.T.localRotation = Quaternion.AngleAxis(thigh, leg.Right) * leg.RestRot;
            }
            for (int i = 0; i < _shins.Count; i++)
            {
                Node shin = _shins[i];
                float knee = Mathf.Max(0f, Mathf.Cos(_gaitPhase + shin.Phase)) * KneeBend * _moveBlend;
                shin.T.localRotation = Quaternion.AngleAxis(knee, shin.Right) * shin.RestRot;
            }
        }

        private void TickTurret(in UnitRigInput input, float dt)
        {
            if (!_hasTurret) return;

            float desired = 0f;
            float rate = _turretTurnRate;
            if (input.HasTarget)
            {
                Vector3 local = transform.InverseTransformPoint(input.TargetPosition);
                if (local.x * local.x + local.z * local.z > 0.01f)
                {
                    desired = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                }
            }
            else if (_idleTime > 2.5f)
            {
                // Idle scan: the turret sweeps the ground slowly, with pauses
                // at the ends of the arc (the sine flattens there).
                desired = Mathf.Sin((_idleTime - 2.5f) * 0.32f + _gaitPhase) * 38f;
                rate = 25f;
            }
            float delta = Mathf.DeltaAngle(_turretYaw, desired);
            _turretYaw += Mathf.Clamp(delta, -rate * dt, rate * dt);
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
            _footprintWorld = Mathf.Max(0.3f, Mathf.Max(b.size.x, b.size.z));

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
            _glowInitialPhase = Random.value * 6.28f;
            _glowPhase = _glowInitialPhase;

            Transform bed = FindLive(transform, BedNode);
            _hasBed = bed != null;
            if (_hasBed) _bed = Capture(bed);
            BuildCargoPile(bed, FindLive(transform, CargoAnchorNode));

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
                else if (n.StartsWith(LegPrefix, System.StringComparison.Ordinal)
                         || n.StartsWith(ShinPrefix, System.StringComparison.Ordinal))
                {
                    Node limb = Capture(t);
                    limb.Phase = n.EndsWith("_R", System.StringComparison.Ordinal) ? Mathf.PI : 0f;
                    if (n.StartsWith(LegPrefix, System.StringComparison.Ordinal))
                    {
                        _legs.Add(limb);
                        float hip = transform.InverseTransformPoint(t.position).y;
                        _hipHeight = Mathf.Max(_hipHeight, hip);
                    }
                    else
                    {
                        _shins.Add(limb);
                    }
                }
                else if (n.StartsWith(SmokePrefix, System.StringComparison.Ordinal))
                {
                    _smokes.Add(t);
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
        /// Hangs the procedural cargo pile on the bed (tips with it) or, for a
        /// model without one, on its <c>Cargo_Anchor</c> (the body leans back instead).
        /// Everything is measured in the unit root's local space.
        /// </summary>
        private void BuildCargoPile(Transform bed, Transform anchor)
        {
            if (bed != null && TryLocalBounds(bed, out Bounds lb))
            {
                var size = new Vector3(lb.size.x * 0.86f, lb.size.y * 0.5f, lb.size.z * 0.84f);
                var floor = new Vector3(lb.center.x, lb.min.y + lb.size.y * 0.10f, lb.center.z);
                _pile = CargoPile.Create(transform, bed, floor, size);
                _bedSign = LiftSign(_bed, new Vector3(lb.center.x, lb.center.y, lb.max.z));
            }
            else if (anchor != null && _hasBody)
            {
                float f = _size;
                // Slightly forward and up onto the rear deck: at the anchor itself half the load hung
                // over the tail, further forward the hull hides it from the RTS camera.
                Vector3 p = transform.InverseTransformPoint(anchor.position) + new Vector3(0f, f * 0.07f, f * 0.04f);
                _pile = CargoPile.Create(transform, anchor, p, new Vector3(f * 0.30f, f * 0.09f, f * 0.22f));
                if (_pile != null && TryLocalBounds(transform, out Bounds all))
                {
                    _tiltPivotRoot = new Vector3(all.center.x, all.min.y, all.min.z);
                    _tiltPivotBody = _body.T.InverseTransformPoint(transform.TransformPoint(_tiltPivotRoot));
                    _bodyTiltSign = LiftSign(_body, new Vector3(all.center.x, all.center.y, all.max.z));
                }
            }
        }

        /// <summary>
        /// Which sign of a rotation about the node's right axis lifts a point at the
        /// front of the model. Measured instead of assumed, because the FBX axis
        /// conversion may mirror the node's coordinate system.
        /// </summary>
        private float LiftSign(Node n, Vector3 frontRootLocal)
        {
            Vector3 inNode = n.T.InverseTransformPoint(transform.TransformPoint(frontRootLocal));
            Quaternion rot = n.T.localRotation;
            Vector3 pos = n.T.localPosition;
            n.T.localRotation = Quaternion.AngleAxis(-10f, n.Right) * n.RestRot;
            float y = transform.InverseTransformPoint(n.T.TransformPoint(inNode)).y;
            n.T.localRotation = rot;
            n.T.localPosition = pos;
            return y > frontRootLocal.y ? -1f : 1f;
        }

        /// <summary>Mesh bounds of <paramref name="sub"/> in the unit root's local space (wreck excluded).</summary>
        private bool TryLocalBounds(Transform sub, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            Transform wreck = transform.Find(ViewWrecks.WreckNode);
            foreach (MeshFilter mf in sub.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                if (wreck != null && mf.transform.IsChildOf(wreck)) continue;
                Bounds mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 p = transform.InverseTransformPoint(mf.transform.TransformPoint(c));
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                    else bounds.Encapsulate(p);
                }
            }
            return any;
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

        /// <summary>Like <see cref="FindDeep"/>, but never looks into the wreck child.</summary>
        private static Transform FindLive(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                if (child.name == ViewWrecks.WreckNode) continue;
                Transform hit = FindLive(child, name);
                if (hit != null) return hit;
            }
            return null;
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

        /// <summary>World bounds of the live model (the wreck child, D-115, is not part of it).</summary>
        private static Bounds RendererBounds(Transform t)
        {
            Transform wreck = t.Find(ViewWrecks.WreckNode);
            bool any = false;
            Bounds b = new Bounds(t.position, Vector3.one);
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (wreck != null && r.transform.IsChildOf(wreck)) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}
