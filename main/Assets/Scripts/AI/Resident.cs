using UnityEngine;
using UnityEngine.AI;

namespace RobaYCorre
{
    public enum ResidentState { Sleeping, Waking, Routine, Investigating, Searching, Chasing, Returning, Celebrating }

    /// <summary>
    /// Habitante de la casa. Etapas: dormido/rutina → oye ruido e investiga → ve al mapache y lo persigue
    /// → si lo pierde, busca un rato → vuelve a su rutina (o a dormir).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Resident : MonoBehaviour, IWatcher
    {
        public string displayName = "el habitante";

        [Header("Rutina")]
        public bool startsAsleep = true;
        [Tooltip("Punto junto a la cama donde se levanta")]
        public Transform wakeSpot;
        public Transform[] patrolPoints;
        public float patrolWait = 2.5f;

        [Header("Velocidades")]
        public float walkSpeed = 1.5f;
        public float investigateSpeed = 2.2f;
        public float searchSpeed = 3.0f;
        public float chaseSpeed = 3.9f;

        [Header("Vista")]
        public float viewDistance = 7.5f;
        public float viewAngle = 100f;
        [Tooltip("Agachado se te ve desde menos distancia")]
        [Range(0.2f, 1f)] public float crouchedViewFactor = 0.6f;
        [Tooltip("Segundos viéndote antes de empezar a perseguirte")]
        public float detectTime = 0.8f;
        public float eyeHeight = 1.5f;

        [Header("Oído")]
        public float hearingRadius = 13f;
        [Tooltip("Ruido percibido necesario para despertarse")]
        public float wakeThreshold = 40f;
        public float investigateThreshold = 30f;
        [Tooltip("A partir de aquí busca activamente (más rápido)")]
        public float activeSearchThreshold = 60f;

        [Header("Persecución")]
        public float catchDistance = 0.95f;
        public float loseSightTime = 2.5f;
        public float searchDuration = 8f;
        public float searchRadius = 3.5f;

        [Header("Referencias")]
        public HumanVisual visual;

        public ResidentState State => state;
        public float Suspicion => suspicion;

        NavMeshAgent agent;
        VisionCone cone;
        ResidentState state;
        Vector3 sleepPos;
        Quaternion sleepRot;
        Vector3 target;
        Vector3 searchCenter;
        Vector3 lastSeen;
        float timer, suspicion, lostTimer, repathTimer, waitTimer;
        int patrolIndex;
        bool activeSearch;
        bool seesNow;
        bool couldSee;
        bool pendingUrgent;
        Vector3 pendingPos;

        public bool IsWatchingPlayer => seesNow;
        public bool CouldSeePlayer => couldSee;
        public Vector3 IconPosition => transform.position + Vector3.up * (state == ResidentState.Sleeping ? 1.2f : 2.15f);

        public string IconText
        {
            get
            {
                switch (state)
                {
                    case ResidentState.Sleeping: return "Zzz";
                    case ResidentState.Waking: return "?!";
                    case ResidentState.Chasing: return "!";
                    case ResidentState.Celebrating: return "¡Ja!";
                    case ResidentState.Investigating:
                    case ResidentState.Searching: return "?";
                }
                return suspicion > 0.05f ? "?" : "";
            }
        }

        public Color IconColor
        {
            get
            {
                switch (state)
                {
                    case ResidentState.Sleeping: return new Color(0.7f, 0.85f, 1f);
                    case ResidentState.Chasing: return new Color(1f, 0.25f, 0.2f);
                    case ResidentState.Celebrating: return new Color(1f, 0.25f, 0.2f);
                }
                return new Color(1f, 0.85f, 0.2f);
            }
        }

        Vector3 Eye => transform.position + Vector3.up * eyeHeight;
        bool Awake_ => state != ResidentState.Sleeping && state != ResidentState.Waking;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (visual == null) visual = GetComponentInChildren<HumanVisual>();
        }

        void OnEnable() { if (!Perception.Watchers.Contains(this)) Perception.Watchers.Add(this); }
        void OnDisable() { Perception.Watchers.Remove(this); }

        void Start()
        {
            sleepPos = transform.position;
            sleepRot = transform.rotation;
            cone = VisionCone.Create(name);
            agent.speed = walkSpeed;
            agent.angularSpeed = 540f;
            agent.acceleration = 14f;
            agent.stoppingDistance = 0.2f;
            if (startsAsleep)
            {
                agent.enabled = false;
                state = ResidentState.Sleeping;
                if (visual != null) visual.pose = HumanPose.Sleep;
            }
            else
            {
                EnableAgent(transform.position);
                state = ResidentState.Routine;
            }
        }

        void EnableAgent(Vector3 pos)
        {
            if (NavMesh.SamplePosition(pos, out var hit, 3f, NavMesh.AllAreas)) pos = hit.position;
            transform.position = pos;
            agent.enabled = true;
            agent.Warp(pos);
            agent.isStopped = false;
        }

        bool AgentReady => agent != null && agent.enabled && agent.isOnNavMesh;

        void GoTo(Vector3 p, float speed)
        {
            if (!AgentReady) return;
            agent.speed = speed;
            if (NavMesh.SamplePosition(p, out var hit, 2.5f, NavMesh.AllAreas)) p = hit.position;
            agent.SetDestination(p);
        }

        bool Arrived()
        {
            if (!AgentReady) return true;
            if (agent.pathPending) return false;
            return agent.remainingDistance <= Mathf.Max(0.5f, agent.stoppingDistance + 0.3f);
        }

        void Update()
        {
            var lm = LevelManager.I;
            if (lm == null || lm.player == null) return;
            var player = lm.player;

            if (lm.State != LevelState.Playing)
            {
                if (AgentReady) agent.isStopped = true;
                seesNow = couldSee = false;
                if (state == ResidentState.Celebrating && visual != null) visual.pose = HumanPose.Cheer;
                if (cone != null) cone.Hide();
                if (visual != null) visual.speed = 0f;
                return;
            }
            if (AgentReady) agent.isStopped = false;

            float dt = Time.deltaTime;
            couldSee = Awake_ && CanSeePlayer(player, false);
            seesNow = couldSee && !player.IsHidden;
            var noise = NoiseSystem.I;
            float heard = noise != null ? noise.HeardAt(transform.position, hearingRadius) : 0f;
            Vector3 noisePos = noise != null ? noise.LastNoisePosition : transform.position;

            switch (state)
            {
                case ResidentState.Sleeping:
                    if (heard >= wakeThreshold) StartWaking(noisePos, heard >= activeSearchThreshold);
                    break;

                case ResidentState.Waking:
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        EnableAgent(wakeSpot != null ? wakeSpot.position : transform.position + transform.right * 1.2f);
                        GoInvestigate(pendingPos, pendingUrgent);
                    }
                    break;

                case ResidentState.Routine:
                    UpdateRoutine(dt);
                    if (heard >= investigateThreshold) GoInvestigate(noisePos, heard >= activeSearchThreshold);
                    break;

                case ResidentState.Investigating:
                    if (heard >= investigateThreshold && Perception.FlatDistance(noisePos, target) > 1.5f)
                        GoInvestigate(noisePos, activeSearch || heard >= activeSearchThreshold);
                    if (Arrived()) StartSearch(target);
                    break;

                case ResidentState.Searching:
                    timer -= dt;
                    waitTimer -= dt;
                    if (heard >= investigateThreshold && Perception.FlatDistance(noisePos, searchCenter) > 2f)
                        GoInvestigate(noisePos, heard >= activeSearchThreshold);
                    else if (waitTimer <= 0f && Arrived())
                    {
                        Vector2 r = Random.insideUnitCircle * searchRadius;
                        GoTo(searchCenter + new Vector3(r.x, 0f, r.y), activeSearch ? investigateSpeed : walkSpeed * 1.2f);
                        waitTimer = Random.Range(0.8f, 1.6f);
                    }
                    if (timer <= 0f) StartReturn();
                    break;

                case ResidentState.Chasing:
                    UpdateChase(player, lm, dt);
                    break;

                case ResidentState.Returning:
                    if (heard >= investigateThreshold) GoInvestigate(noisePos, heard >= activeSearchThreshold);
                    else if (Arrived())
                    {
                        if (startsAsleep)
                        {
                            agent.enabled = false;
                            transform.SetPositionAndRotation(sleepPos, sleepRot);
                            state = ResidentState.Sleeping;
                        }
                        else state = ResidentState.Routine;
                    }
                    break;
            }

            // Detección visual (se llena una barrita de sospecha).
            if (Awake_ && state != ResidentState.Chasing)
            {
                if (seesNow)
                {
                    float d = Perception.FlatDistance(transform.position, player.transform.position);
                    float closeness = 1f + 2f * (1f - Mathf.Clamp01(d / viewDistance));
                    suspicion += dt / Mathf.Max(0.05f, detectTime) * closeness;
                    if (noise != null && noise.Level >= 99f) suspicion = 1f;
                    FaceTowards(player.transform.position, dt);
                    if (suspicion >= 1f) StartChase(player, lm);
                }
                else suspicion = Mathf.Max(0f, suspicion - dt * 0.4f);
            }

            UpdateVisual();
            UpdateCone();
        }

        void UpdateRoutine(float dt)
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                if (visual != null) visual.pose = HumanPose.Idle;
                return;
            }
            if (waitTimer > 0f)
            {
                waitTimer -= dt;
                return;
            }
            var p = patrolPoints[patrolIndex % patrolPoints.Length];
            if (p == null) { patrolIndex++; return; }
            if (!AgentReady) return;
            if (Perception.FlatDistance(agent.destination, p.position) > 0.6f || !agent.hasPath) GoTo(p.position, walkSpeed);
            if (Arrived())
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                waitTimer = patrolWait;
            }
        }

        void UpdateChase(RaccoonController player, LevelManager lm, float dt)
        {
            float d = Perception.FlatDistance(transform.position, player.transform.position);
            bool senses = seesNow || (!player.IsHidden && d < 2.2f);
            if (senses)
            {
                lastSeen = player.transform.position;
                lostTimer = 0f;
            }
            else lostTimer += dt;

            repathTimer -= dt;
            if (repathTimer <= 0f)
            {
                repathTimer = 0.15f;
                GoTo(lastSeen, chaseSpeed);
            }

            if (!player.IsHidden && d <= catchDistance)
            {
                state = ResidentState.Celebrating;
                if (AgentReady) agent.isStopped = true;
                FaceTowards(player.transform.position, 1f);
                lm.PlayerCaught(displayName);
                return;
            }

            if (lostTimer >= loseSightTime)
            {
                LevelManager.Toast($"{Cap(displayName)} te perdió de vista...", HUD.Info);
                activeSearch = true;
                StartSearch(lastSeen);
            }
        }

        void StartWaking(Vector3 pos, bool urgent)
        {
            state = ResidentState.Waking;
            timer = urgent ? 0.8f : 1.4f;
            pendingPos = pos;
            pendingUrgent = urgent;
            AudioManager.Play(Sfx.Wake);
            LevelManager.Toast($"¡{Cap(displayName)} se despertó!", HUD.Warn);
        }

        void GoInvestigate(Vector3 pos, bool urgent)
        {
            if (state == ResidentState.Chasing) return;
            bool wasCalm = state == ResidentState.Routine || state == ResidentState.Returning || state == ResidentState.Waking;
            state = ResidentState.Investigating;
            activeSearch = urgent;
            target = pos;
            GoTo(pos, urgent ? searchSpeed : investigateSpeed);
            if (wasCalm)
            {
                AudioManager.Play(Sfx.Question);
                LevelManager.Toast(urgent ? $"¡{Cap(displayName)} te está buscando!" : $"{Cap(displayName)} oyó algo y viene a mirar", HUD.Warn);
            }
        }

        void StartSearch(Vector3 center)
        {
            state = ResidentState.Searching;
            searchCenter = center;
            timer = searchDuration;
            waitTimer = 0.6f;
        }

        void StartReturn()
        {
            state = ResidentState.Returning;
            activeSearch = false;
            suspicion = 0f;
            if (startsAsleep)
                GoTo(wakeSpot != null ? wakeSpot.position : sleepPos, walkSpeed);
            else if (patrolPoints != null && patrolPoints.Length > 0)
            {
                // Vuelve al punto de patrulla más cercano.
                int best = 0; float bd = float.MaxValue;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] == null) continue;
                    float d = Perception.FlatDistance(transform.position, patrolPoints[i].position);
                    if (d < bd) { bd = d; best = i; }
                }
                patrolIndex = best;
                GoTo(patrolPoints[best].position, walkSpeed);
            }
            else GoTo(sleepPos, walkSpeed);
            LevelManager.Toast($"{Cap(displayName)} se rinde y vuelve a lo suyo", HUD.Info);
        }

        void StartChase(RaccoonController player, LevelManager lm)
        {
            state = ResidentState.Chasing;
            lastSeen = player.transform.position;
            lostTimer = 0f;
            repathTimer = 0f;
            suspicion = 1f;
            lm.NotifySpotted(displayName);
        }

        /// <summary>Lo llama una cámara, el perro o una alarma.</summary>
        public void Alert(Vector3 pos, bool urgent)
        {
            if (state == ResidentState.Chasing || state == ResidentState.Celebrating) return;
            if (state == ResidentState.Sleeping) { StartWaking(pos, urgent); return; }
            if (state == ResidentState.Waking) { pendingPos = pos; pendingUrgent |= urgent; return; }
            GoInvestigate(pos, urgent);
        }

        bool CanSeePlayer(RaccoonController player, bool respectHidden)
        {
            if (respectHidden && player.IsHidden) return false;
            if (player.IsHidden && player.CurrentSpot != null)
            {
                // Comprobación "si no estuviera escondido": desde la entrada del escondite.
                return false;
            }
            float range = viewDistance * (player.IsCrouching ? crouchedViewFactor : 1f);
            if (state == ResidentState.Chasing) range *= 1.35f;
            return Perception.CanSee(Eye, transform.forward, range, viewAngle, player.TargetPoint);
        }

        void FaceTowards(Vector3 p, float dt)
        {
            Vector3 d = p - transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 360f * dt);
        }

        void UpdateVisual()
        {
            if (visual == null) return;
            float sp = AgentReady ? agent.velocity.magnitude : 0f;
            visual.speed = sp;
            switch (state)
            {
                case ResidentState.Sleeping: visual.pose = HumanPose.Sleep; break;
                case ResidentState.Waking: visual.pose = HumanPose.SitUp; break;
                case ResidentState.Chasing: visual.pose = HumanPose.Chase; break;
                case ResidentState.Investigating: visual.pose = HumanPose.Investigate; break;
                case ResidentState.Searching: visual.pose = HumanPose.Search; break;
                case ResidentState.Celebrating: visual.pose = HumanPose.Cheer; break;
                default: visual.pose = sp > 0.1f ? HumanPose.Walk : HumanPose.Idle; break;
            }
        }

        void UpdateCone()
        {
            if (cone == null) return;
            if (!Awake_) { cone.Hide(); return; }
            Color c;
            if (state == ResidentState.Chasing) c = new Color(1f, 0.2f, 0.15f, 0.32f);
            else if (state == ResidentState.Investigating || state == ResidentState.Searching || suspicion > 0.05f) c = new Color(1f, 0.75f, 0.1f, 0.28f);
            else c = new Color(1f, 1f, 0.75f, 0.18f);
            float range = viewDistance * (state == ResidentState.Chasing ? 1.35f : 1f);
            cone.Show(transform.position, transform.forward, range, viewAngle, c);
        }

        static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        void OnDestroy()
        {
            if (cone != null) Destroy(cone.gameObject);
        }
    }
}
