using System.Collections.Generic;
using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Control del mapache: moverse, correr, agacharse, robar, soltar, esconderse.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class RaccoonController : MonoBehaviour
    {
        [Header("Velocidades")]
        public float walkSpeed = 2.6f;
        public float runSpeed = 5f;
        public float crouchSpeed = 1.3f;
        [Tooltip("Ralentización al llevar más peso que 'normalSpeedCapacity'")]
        [Range(0f, 0.6f)] public float heavySlowdown = 0.15f;
        public float turnSpeed = 720f;
        public float gravity = 20f;

        [Header("Mochila")]
        public int capacity = 10;
        [Tooltip("Hasta este peso se va a velocidad normal")]
        public int normalSpeedCapacity = 6;

        [Header("Ruido")]
        public float walkNoise = 2f;
        public float crouchNoise = 0.5f;
        public float runNoise = 12f;
        public float bumpNoise = 15f;
        public float pickupNoiseMultiplier = 3f;
        public float decayStill = 10f;
        public float decayWalk = 4f;

        [Header("Interacción")]
        public float interactRadius = 1.35f;

        [Header("Referencias")]
        public RaccoonVisual visual;
        [Tooltip("Donde se pone un objeto pequeño como sombrero")]
        public Transform headSlot;
        [Tooltip("Donde lleva por encima de la cabeza los objetos grandes")]
        public Transform holdSlot;
        public Transform storage;

        public bool IsCrouching { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsHidden { get; private set; }
        public bool IsCaught { get; private set; }
        public float CurrentSpeed { get; private set; }
        public Vector3 MoveDirection { get; private set; } = Vector3.forward;
        public HidingSpot CurrentSpot { get; private set; }
        public ExitZone AtExit { get; set; }

        public readonly List<Lootable> carried = new List<Lootable>();
        public int CarriedValue { get; private set; }
        public int CarriedWeight { get; private set; }
        public bool CarryingSpecial { get; private set; }
        public bool IsHeavy => CarriedWeight > normalSpeedCapacity;
        public int FreeSpace => capacity - CarriedWeight;

        /// <summary>Punto al que miran los vigilantes.</summary>
        public Vector3 TargetPoint => transform.position + Vector3.up * (IsCrouching ? 0.25f : 0.4f);

        CharacterController cc;
        float vy;
        float nextBumpTime;
        Lootable focusLoot;
        HidingSpot focusSpot;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (visual == null) visual = GetComponentInChildren<RaccoonVisual>();
            if (storage == null)
            {
                storage = new GameObject("Mochila_Contenido").transform;
                storage.SetParent(transform, false);
            }
        }

        void Update()
        {
            var lm = LevelManager.I;
            if (lm == null || lm.State != LevelState.Playing)
            {
                CurrentSpeed = 0f;
                IsRunning = false;
                if (visual != null) visual.SetMotion(0f, IsCrouching, false);
                ClearFocus();
                return;
            }

            var noise = NoiseSystem.I;

            if (IsHidden)
            {
                if (noise != null) noise.Decay(decayStill);
                lm.SetPrompt($"Escondido {CurrentSpot.spotName}   ·   [E] Salir");
                if (GameInput.InteractDown) ExitHiding();
                return;
            }

            // --- Movimiento (cámara fija mirando al norte: WASD = ejes del mundo) ---
            Vector2 input = GameInput.Move;
            Vector3 dir = new Vector3(input.x, 0f, input.y);
            var cam = Camera.main;
            if (cam != null)
            {
                float yaw = cam.transform.eulerAngles.y;
                dir = Quaternion.Euler(0f, yaw, 0f) * dir;
            }

            IsMoving = dir.sqrMagnitude > 0.01f;
            IsCrouching = GameInput.Crouch;
            IsRunning = IsMoving && GameInput.Run && !IsCrouching;
            float speed = IsCrouching ? crouchSpeed : (IsRunning ? runSpeed : walkSpeed);
            if (IsHeavy) speed *= 1f - heavySlowdown;
            float intended = speed * Mathf.Clamp01(dir.magnitude);

            Vector3 velocity = IsMoving ? dir.normalized * intended : Vector3.zero;
            if (cc.isGrounded) vy = -2f; else vy -= gravity * Time.deltaTime;
            cc.Move((velocity + Vector3.up * vy) * Time.deltaTime);
            Vector3 flatVel = cc.velocity; flatVel.y = 0f;
            CurrentSpeed = IsMoving ? intended : 0f;

            if (IsMoving)
            {
                MoveDirection = dir.normalized;
                var targetRot = Quaternion.LookRotation(MoveDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            // --- Ruido ---
            if (noise != null)
            {
                if (!IsMoving) noise.Decay(decayStill);
                else if (IsCrouching) { noise.AddContinuous(crouchNoise * Time.deltaTime, transform.position, false); noise.Decay(decayStill); }
                else if (IsRunning) noise.AddContinuous(runNoise * Time.deltaTime, transform.position, true);
                else { noise.AddContinuous(walkNoise * Time.deltaTime, transform.position, false); noise.Decay(decayWalk); }
            }

            if (visual != null) visual.SetMotion(Mathf.Clamp01(flatVel.magnitude / runSpeed), IsCrouching, IsRunning);

            // --- Interacción ---
            UpdateFocus(lm);
            if (GameInput.InteractDown) Interact(lm);
            if (GameInput.DropDown) DropLast();
        }

        void ClearFocus()
        {
            if (focusLoot != null) focusLoot.SetHighlight(false);
            focusLoot = null;
            focusSpot = null;
        }

        void UpdateFocus(LevelManager lm)
        {
            Lootable bestLoot = null;
            float bestLootD = interactRadius;
            foreach (var l in Lootable.All)
            {
                if (l == null || l.IsCarried) continue;
                float d = Perception.FlatDistance(transform.position, l.transform.position);
                if (d < bestLootD) { bestLootD = d; bestLoot = l; }
            }

            HidingSpot bestSpot = null;
            float bestSpotD = interactRadius + 0.2f;
            foreach (var s in HidingSpot.All)
            {
                if (s == null || s.Occupied) continue;
                float d = s.DistanceTo(transform.position);
                if (d < bestSpotD) { bestSpotD = d; bestSpot = s; }
            }

            if (bestLoot != null && bestSpot != null)
            {
                if (bestLootD <= bestSpotD + 0.3f) bestSpot = null; else bestLoot = null;
            }

            if (focusLoot != bestLoot)
            {
                if (focusLoot != null) focusLoot.SetHighlight(false);
                focusLoot = bestLoot;
                if (focusLoot != null) focusLoot.SetHighlight(true);
            }
            focusSpot = bestSpot;

            if (AtExit != null && lm.GoalMet)
                lm.SetPrompt($"[E] ¡Escapar por {AtExit.exitName}!");
            else if (focusLoot != null)
            {
                string extra = focusLoot.weight > FreeSpace ? "   (¡no cabe! suelta algo con Q)" : "";
                lm.SetPrompt(focusLoot.Prompt + extra);
            }
            else if (focusSpot != null)
                lm.SetPrompt(focusSpot.Prompt);
            else if (AtExit != null)
                lm.SetPrompt($"Faltan {Mathf.Max(0, lm.lootGoal - lm.Banked)} monedas{(lm.requiresSpecial && !lm.SpecialBanked ? " y el " + lm.specialItemName : "")} para escapar");
            else
                lm.SetPrompt(null);
        }

        void Interact(LevelManager lm)
        {
            if (AtExit != null && lm.GoalMet)
            {
                lm.Escape();
                return;
            }
            if (focusLoot != null) TryPickup(focusLoot, lm);
            else if (focusSpot != null) EnterHiding(focusSpot, lm);
        }

        public bool TryPickup(Lootable l, LevelManager lm)
        {
            if (l == null || l.IsCarried) return false;
            if (CarriedWeight + l.weight > capacity)
            {
                LevelManager.Toast("¡La mochila está llena! Suelta algo (Q) o guarda el botín en la salida", HUD.Warn);
                AudioManager.Play(Sfx.Full);
                return false;
            }

            Vector3 pos = l.transform.position;
            l.SetHighlight(false);
            if (focusLoot == l) focusLoot = null;
            carried.Add(l);
            l.OnPicked(storage);
            Recalculate();

            float n = l.pickupNoise * pickupNoiseMultiplier;
            if (NoiseSystem.I != null) NoiseSystem.I.Burst(n, pos);
            AudioManager.Play(Sfx.Pickup);
            AudioManager.Play(Sfx.Coin, 0.7f);
            if (l.itemName.Contains("papas")) AudioManager.Play(Sfx.Crunch);

            LevelManager.Toast($"+{l.value} monedas: {l.itemName}", HUD.Good);
            if (!string.IsNullOrEmpty(l.funnyLine)) LevelManager.Toast(l.funnyLine, HUD.Info);
            if (IsHeavy) LevelManager.Toast("Vas cargado: te mueves más lento", HUD.Warn);

            if (visual != null) visual.PlayPickup();
            UpdateCarryVisuals();
            if (l.triggersAlarm) lm.TriggerAlarm(pos, $"¡ALARMA! El {l.itemName.ToLower()} estaba protegido");
            lm.OnLootPicked(l);
            return true;
        }

        public void DropLast()
        {
            if (carried.Count == 0)
            {
                LevelManager.Toast("No llevas nada que soltar", HUD.Info);
                return;
            }
            var l = carried[carried.Count - 1];
            carried.RemoveAt(carried.Count - 1);
            Vector3 drop = transform.position + MoveDirection * 0.7f;
            if (Physics.Raycast(transform.position + Vector3.up * 0.3f, MoveDirection, 0.9f, Layers.ObstacleMask, QueryTriggerInteraction.Ignore))
                drop = transform.position - MoveDirection * 0.5f;
            drop.y = 0f;
            l.OnDropped(drop, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            Recalculate();
            UpdateCarryVisuals();
            if (NoiseSystem.I != null) NoiseSystem.I.Burst(2f, drop);
            AudioManager.Play(Sfx.Drop);
            LevelManager.Toast($"Soltaste: {l.itemName} (-{l.value})", HUD.Info);
        }

        /// <summary>Vacía la mochila (al llegar a la salida). Devuelve las monedas guardadas.</summary>
        public int DepositAll(out bool hadSpecial)
        {
            int total = CarriedValue;
            hadSpecial = CarryingSpecial;
            foreach (var l in carried)
                if (l != null) Destroy(l.gameObject);
            carried.Clear();
            Recalculate();
            UpdateCarryVisuals();
            return total;
        }

        void Recalculate()
        {
            int v = 0, w = 0;
            bool sp = false;
            foreach (var l in carried)
            {
                if (l == null) continue;
                v += l.value;
                w += l.weight;
                sp |= l.isSpecial;
            }
            CarriedValue = v;
            CarriedWeight = w;
            CarryingSpecial = sp;
        }

        /// <summary>Lo grande se lleva por encima de la cabeza; lo último pequeño, de sombrero.</summary>
        void UpdateCarryVisuals()
        {
            Lootable big = null;
            Lootable hat = null;
            foreach (var l in carried)
            {
                if (l == null) continue;
                if (l.weight >= 3) { if (big == null || l.weight >= big.weight) big = l; }
                else hat = l;
            }
            foreach (var l in carried)
            {
                if (l == null) continue;
                if (l == big && holdSlot != null) l.ShowCarried(holdSlot, 0.85f);
                else if (l == hat && headSlot != null) l.ShowCarried(headSlot, 0.7f);
                else l.HideCarried(storage);
            }
            if (visual != null) visual.SetBackpackLoad(CarriedWeight / (float)Mathf.Max(1, capacity), big != null);
        }

        public bool EnterHiding(HidingSpot spot, LevelManager lm)
        {
            if (spot == null || spot.Occupied) return false;
            if (Perception.AnyWatcherCouldSeePlayer())
            {
                LevelManager.Toast("¡No puedes esconderte mientras te están mirando!", HUD.Bad);
                AudioManager.Play(Sfx.Full);
                return false;
            }
            IsHidden = true;
            IsRunning = false;
            IsMoving = false;
            CurrentSpeed = 0f;
            CurrentSpot = spot;
            ClearFocus();
            spot.Enter();
            cc.enabled = false;
            if (spot.hidePoint != null) transform.position = spot.hidePoint.position;
            if (visual != null) visual.SetHidden(true);
            AudioManager.Play(Sfx.Hide);
            LevelManager.Toast($"Te escondiste {spot.spotName}", HUD.Info);
            lm.OnPlayerHid();
            return true;
        }

        public void ExitHiding()
        {
            if (!IsHidden) return;
            var spot = CurrentSpot;
            Vector3 exit = spot != null ? spot.FindFreeExit(transform.position + MoveDirection) : transform.position;
            transform.position = exit;
            cc.enabled = true;
            IsHidden = false;
            CurrentSpot = null;
            if (spot != null) spot.Exit();
            if (visual != null) visual.SetHidden(false);
            AudioManager.Play(Sfx.Unhide);
        }

        public void OnCaught()
        {
            IsCaught = true;
            if (IsHidden) ExitHiding();
            if (visual != null) visual.PlayCaught();
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.6f) return;
            if (Time.time < nextBumpTime) return;
            var lm = LevelManager.I;
            if (lm == null || lm.State != LevelState.Playing) return;

            var bump = hit.collider.GetComponentInParent<Bumpable>();
            if (bump != null)
            {
                bool knocks = bump.tipsOver ? (!IsCrouching && CurrentSpeed > crouchSpeed + 0.3f) : IsRunning;
                if (knocks)
                {
                    bump.Bump(MoveDirection);
                    nextBumpTime = Time.time + 0.7f;
                    if (visual != null) visual.PlayBump();
                    return;
                }
            }

            if (IsRunning && CurrentSpeed > walkSpeed)
            {
                Vector3 toHit = hit.point - transform.position; toHit.y = 0f;
                if (Vector3.Dot(toHit.normalized, MoveDirection) < 0.4f) return;
                if (NoiseSystem.I != null) NoiseSystem.I.Burst(bumpNoise, hit.point);
                AudioManager.Play(Sfx.Bump);
                LevelManager.Toast("¡Pum! Chocaste con algo", HUD.Warn);
                nextBumpTime = Time.time + 0.9f;
                if (visual != null) visual.PlayBump();
            }
        }
    }
}
