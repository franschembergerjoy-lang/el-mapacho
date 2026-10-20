using UnityEngine;
using UnityEngine.AI;

namespace RobaYCorre
{
    public enum DogState { Patrol, Sniffing, Chase, Returning, Celebrating }

    /// <summary>Perro guardián: patrulla, te huele si pasas cerca, ladra (despierta a los habitantes) y te persigue.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Dog : MonoBehaviour, IWatcher
    {
        public string displayName = "el perro";
        public Transform[] patrolPoints;
        public float patrolWait = 1.5f;

        [Header("Velocidades")]
        public float walkSpeed = 1.8f;
        public float chaseSpeed = 4.4f;

        [Header("Sentidos")]
        public float viewDistance = 5.5f;
        public float viewAngle = 130f;
        [Tooltip("Te huele a esta distancia aunque no te vea")]
        public float smellRadius = 2.6f;
        public float crouchedSmellRadius = 1.4f;
        public float detectTime = 0.5f;
        public float hearingRadius = 12f;
        public float hearThreshold = 35f;

        [Header("Persecución")]
        public float catchDistance = 0.8f;
        public float loseTime = 3f;
        public float barkNoise = 45f;
        public float barkInterval = 1.4f;
        [Tooltip("Habitantes a esta distancia acuden al oír ladrar")]
        public float alertRadius = 20f;

        [Header("Partes animadas")]
        public Transform model;
        public Transform head;
        public Transform tail;
        public Transform[] legs;

        NavMeshAgent agent;
        VisionCone cone;
        DogState state;
        int patrolIndex;
        float waitTimer, suspicion, lostTimer, barkTimer, repath, sniffTimer, phase;
        Vector3 lastSeen, sniffTarget;
        bool seesNow, couldSee;

        public DogState State => state;
        public bool IsWatchingPlayer => seesNow;
        public bool CouldSeePlayer => couldSee;
        public Vector3 IconPosition => transform.position + Vector3.up * 1.1f;
        public string IconText => state == DogState.Chase || state == DogState.Celebrating ? "¡Guau!" : (state == DogState.Sniffing || suspicion > 0.05f ? "?" : "");
        public Color IconColor => state == DogState.Chase ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.85f, 0.2f);

        void Awake() { agent = GetComponent<NavMeshAgent>(); }
        void OnEnable() { if (!Perception.Watchers.Contains(this)) Perception.Watchers.Add(this); }
        void OnDisable() { Perception.Watchers.Remove(this); }

        void Start()
        {
            cone = VisionCone.Create(name);
            Vector3 pos = transform.position;
            if (NavMesh.SamplePosition(pos, out var hit, 3f, NavMesh.AllAreas)) pos = hit.position;
            agent.enabled = true;
            agent.Warp(pos);
            agent.speed = walkSpeed;
            agent.angularSpeed = 600f;
            agent.acceleration = 16f;
            agent.radius = 0.25f;
        }

        bool Ready => agent != null && agent.enabled && agent.isOnNavMesh;

        void GoTo(Vector3 p, float speed)
        {
            if (!Ready) return;
            agent.speed = speed;
            if (NavMesh.SamplePosition(p, out var hit, 2.5f, NavMesh.AllAreas)) p = hit.position;
            agent.SetDestination(p);
        }

        bool Arrived() => !Ready || (!agent.pathPending && agent.remainingDistance <= 0.5f);

        void Update()
        {
            var lm = LevelManager.I;
            if (lm == null || lm.player == null) return;
            var player = lm.player;
            if (lm.State != LevelState.Playing)
            {
                if (Ready) agent.isStopped = true;
                seesNow = couldSee = false;
                cone?.Hide();
                Animate(0f);
                return;
            }
            if (Ready) agent.isStopped = false;
            float dt = Time.deltaTime;

            float d = Perception.FlatDistance(transform.position, player.transform.position);
            float smell = player.IsCrouching ? crouchedSmellRadius : smellRadius;
            float range = viewDistance * (player.IsCrouching ? 0.7f : 1f);
            couldSee = d < smell || Perception.CanSee(transform.position + Vector3.up * 0.6f, transform.forward, range, viewAngle, player.TargetPoint);
            seesNow = couldSee && !player.IsHidden;

            var noise = NoiseSystem.I;
            float heard = noise != null ? noise.HeardAt(transform.position, hearingRadius) : 0f;

            switch (state)
            {
                case DogState.Patrol:
                    Patrol(dt);
                    if (heard >= hearThreshold) { state = DogState.Sniffing; sniffTarget = noise.LastNoisePosition; sniffTimer = 5f; GoTo(sniffTarget, walkSpeed * 1.6f); }
                    break;
                case DogState.Sniffing:
                    sniffTimer -= dt;
                    if (Arrived() || sniffTimer <= 0f) { state = DogState.Returning; GoToNearestPatrol(); }
                    break;
                case DogState.Chase:
                    if (seesNow || (!player.IsHidden && d < smellRadius * 1.5f)) { lastSeen = player.transform.position; lostTimer = 0f; }
                    else lostTimer += dt;
                    repath -= dt;
                    if (repath <= 0f) { repath = 0.15f; GoTo(lastSeen, chaseSpeed); }
                    barkTimer -= dt;
                    if (barkTimer <= 0f) Bark(lm, player.transform.position);
                    if (!player.IsHidden && d <= catchDistance)
                    {
                        state = DogState.Celebrating;
                        lm.PlayerCaught(displayName);
                        return;
                    }
                    if (lostTimer > loseTime)
                    {
                        LevelManager.Toast("El perro perdió tu rastro", HUD.Info);
                        state = DogState.Returning;
                        GoToNearestPatrol();
                    }
                    break;
                case DogState.Returning:
                    if (Arrived()) state = DogState.Patrol;
                    break;
            }

            if (state != DogState.Chase)
            {
                if (seesNow)
                {
                    suspicion += dt / detectTime;
                    if (suspicion >= 1f)
                    {
                        state = DogState.Chase;
                        lastSeen = player.transform.position;
                        lostTimer = 0f;
                        barkTimer = 0f;
                        lm.NotifySpotted(displayName);
                    }
                }
                else suspicion = Mathf.Max(0f, suspicion - dt * 0.6f);
            }

            Animate(Ready ? agent.velocity.magnitude : 0f);
            if (cone != null)
            {
                Color c = state == DogState.Chase ? new Color(1f, 0.2f, 0.15f, 0.3f) : new Color(1f, 0.7f, 0.3f, 0.2f);
                cone.Show(transform.position, transform.forward, range, viewAngle, c);
            }
        }

        void Bark(LevelManager lm, Vector3 playerPos)
        {
            barkTimer = barkInterval;
            AudioManager.Play(Sfx.Bark);
            if (NoiseSystem.I != null) NoiseSystem.I.Burst(barkNoise, transform.position);
            foreach (var w in Perception.Watchers)
            {
                if (w is Resident r && Perception.FlatDistance(r.transform.position, transform.position) <= alertRadius)
                    r.Alert(playerPos, true);
            }
        }

        void Patrol(float dt)
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            if (waitTimer > 0f) { waitTimer -= dt; return; }
            var p = patrolPoints[patrolIndex % patrolPoints.Length];
            if (p == null) { patrolIndex++; return; }
            if (!Ready) return;
            if (!agent.hasPath || Perception.FlatDistance(agent.destination, p.position) > 0.6f) GoTo(p.position, walkSpeed);
            if (Arrived()) { patrolIndex = (patrolIndex + 1) % patrolPoints.Length; waitTimer = patrolWait; }
        }

        void GoToNearestPatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                float dd = Perception.FlatDistance(transform.position, patrolPoints[i].position);
                if (dd < bd) { bd = dd; best = i; }
            }
            patrolIndex = best;
            GoTo(patrolPoints[best].position, walkSpeed);
        }

        void Animate(float speed)
        {
            float dt = Time.deltaTime;
            phase += dt * Mathf.Lerp(6f, 18f, speed / Mathf.Max(0.1f, chaseSpeed)) * (speed > 0.05f ? 1f : 0f);
            if (legs != null)
                for (int i = 0; i < legs.Length; i++)
                    if (legs[i] != null)
                        legs[i].localRotation = Quaternion.Euler(Mathf.Sin(phase + ((i == 0 || i == 3) ? 0f : Mathf.PI)) * 38f * Mathf.Clamp01(speed), 0f, 0f);
            if (tail != null)
            {
                float wag = state == DogState.Chase || state == DogState.Celebrating ? 30f : 12f;
                tail.localRotation = Quaternion.Euler(35f, Mathf.Sin(Time.time * (state == DogState.Celebrating ? 25f : 9f)) * wag, 0f);
            }
            if (head != null)
            {
                float sniff = state == DogState.Sniffing ? 25f + Mathf.Sin(Time.time * 10f) * 6f : 0f;
                float barkUp = state == DogState.Chase && barkTimer > barkInterval - 0.25f ? -20f : 0f;
                head.localRotation = Quaternion.Euler(sniff + barkUp, Mathf.Sin(Time.time * 1.5f) * 10f, 0f);
            }
            if (model != null && state == DogState.Celebrating)
                model.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.15f;
        }

        void OnDestroy()
        {
            if (cone != null) Destroy(cone.gameObject);
        }
    }
}
