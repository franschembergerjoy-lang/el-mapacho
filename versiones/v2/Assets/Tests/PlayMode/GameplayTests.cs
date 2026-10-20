using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RobaYCorre.Tests
{
    /// <summary>
    /// Pruebas automáticas en modo juego: cada nivel carga, todo el botín y la salida son alcanzables,
    /// y las mecánicas principales (robar, guardar, escapar, ser atrapado, esconderse, ruido, perro,
    /// cámaras) funcionan.
    /// </summary>
    public class GameplayTests
    {
        [SetUp]
        public void Setup()
        {
            PlayerPrefs.DeleteAll();
            Time.timeScale = 1f;
        }

        IEnumerator Load(int level)
        {
            SceneManager.LoadScene(GameProgress.LevelScenes[level - 1]);
            yield return null;
            yield return null;
            yield return null;
            Assert.IsNotNull(LevelManager.I, "Falta LevelManager");
            Assert.AreEqual(level, LevelManager.I.levelNumber);
        }

        static void Teleport(RaccoonController p, Vector3 pos)
        {
            var cc = p.GetComponent<CharacterController>();
            cc.enabled = false;
            p.transform.position = new Vector3(pos.x, 0.02f, pos.z);
            cc.enabled = true;
        }

        static bool Reachable(Vector3 from, Vector3 to, float tolerance, out float gap)
        {
            gap = 999f;
            if (!NavMesh.SamplePosition(from, out var a, 2f, NavMesh.AllAreas)) return false;
            to.y = 0f;
            if (!NavMesh.SamplePosition(to, out var b, tolerance + 0.5f, NavMesh.AllAreas)) return false;
            gap = Perception.FlatDistance(b.position, to);
            var path = new NavMeshPath();
            return NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete && gap <= tolerance;
        }

        [UnityTest]
        public IEnumerator MainMenuLoads()
        {
            SceneManager.LoadScene(GameProgress.MenuScene);
            yield return null;
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<MainMenu>());
            Assert.IsNotNull(Object.FindAnyObjectByType<Canvas>());
        }

        [UnityTest]
        public IEnumerator EveryLevelIsCompletable([Values(1, 2, 3, 4, 5)] int level)
        {
            yield return Load(level);
            var lm = LevelManager.I;
            var p = lm.player;
            Assert.IsNotNull(p, "Falta el mapache");
            Assert.IsNotNull(lm.exit, "Falta la salida");

            // Botín suficiente y alcanzable desde la entrada.
            var loot = Lootable.All.ToList();
            int reachableValue = 0;
            var problems = new List<string>();
            foreach (var l in loot)
            {
                if (Reachable(p.transform.position, l.transform.position, p.interactRadius - 0.1f, out float gap)) reachableValue += l.value;
                else problems.Add($"{l.itemName} en {l.transform.position} (distancia {gap:0.00})");
            }
            Assert.IsEmpty(problems, "Botín inalcanzable: " + string.Join(", ", problems));
            Assert.GreaterOrEqual(reachableValue, lm.lootGoal * 1.5f, "No hay botín suficiente para las estrellas");
            if (lm.requiresSpecial) Assert.IsTrue(loot.Any(l => l.isSpecial), "Falta el objeto especial");

            Assert.IsTrue(Reachable(p.transform.position, lm.exit.transform.position, lm.exit.radius, out _), "Salida inalcanzable");

            // Escondites utilizables.
            foreach (var h in HidingSpot.All)
            {
                bool ok = h.exitPoints.Any(e => !Physics.CheckSphere(new Vector3(e.position.x, 0.4f, e.position.z), 0.26f, Layers.ObstacleMask, QueryTriggerInteraction.Ignore)
                                               && Reachable(p.transform.position, e.position, 0.6f, out _));
                Assert.IsTrue(ok, $"Escondite sin salida libre: {h.name} en {h.transform.position}");
            }

            // Rutas de patrulla válidas.
            foreach (var r in Object.FindObjectsByType<Resident>())
            {
                if (r.patrolPoints != null)
                    foreach (var pt in r.patrolPoints)
                        Assert.IsTrue(Reachable(r.startsAsleep ? r.wakeSpot.position : r.transform.position, pt.position, 0.6f, out _), $"Ruta imposible para {r.name}: {pt.position}");
                if (r.startsAsleep)
                    Assert.IsTrue(Reachable(p.transform.position, r.wakeSpot.position, 0.6f, out _), $"Punto de despertar inválido para {r.name}");
            }
            foreach (var d in Object.FindObjectsByType<Dog>())
                foreach (var pt in d.patrolPoints)
                    Assert.IsTrue(Reachable(d.transform.position, pt.position, 0.6f, out _), $"Ruta imposible para el perro: {pt.position}");

            // Contenido por nivel.
            int residents = Object.FindObjectsByType<Resident>().Length;
            int dogs = Object.FindObjectsByType<Dog>().Length;
            int cams = Object.FindObjectsByType<SecurityCamera>().Length;
            int sensors = Object.FindObjectsByType<MotionSensor>().Length;
            int[] expRes = { 1, 1, 1, 2, 3 };
            Assert.AreEqual(expRes[level - 1], residents, "Número de habitantes");
            Assert.AreEqual(level == 3 || level == 5 ? 1 : 0, dogs, "Perro");
            if (level >= 4) { Assert.GreaterOrEqual(cams, 3); Assert.GreaterOrEqual(sensors, 2); }
            else Assert.AreEqual(0, cams + sensors);

            // Jugar: robar hasta el objetivo, volver a la salida y escapar.
            lm.BeginPlay();
            yield return null;
            Assert.AreEqual(LevelState.Playing, lm.State);
            var ordered = loot.OrderByDescending(l => l.isSpecial).ThenByDescending(l => l.value / (float)l.weight).ToList();
            int guard = 0;
            while (!lm.GoalMet && guard++ < 40)
            {
                bool tookAny = false;
                foreach (var l in ordered)
                {
                    if (l == null || l.IsCarried || !l.gameObject.activeInHierarchy) continue;
                    if (l.weight > p.FreeSpace) continue;
                    Teleport(p, l.transform.position + Vector3.back * 0.5f);
                    if (p.TryPickup(l, lm)) tookAny = true;
                }
                Assert.IsTrue(tookAny || p.carried.Count > 0, "No se pudo recoger nada");
                // Ignoramos a los vigilantes en esta prueba: solo se comprueba el flujo de botín.
                NoiseSystem.I.Decay(1000f);
                Teleport(p, lm.exit.transform.position);
                yield return null;
                yield return null;
                if (lm.State != LevelState.Playing) break;
            }
            Assert.IsTrue(lm.GoalMet, $"No se alcanzó el objetivo ({lm.Banked}/{lm.lootGoal})");
            lm.Escape();
            Assert.AreEqual(LevelState.Won, lm.State);
            Assert.GreaterOrEqual(GameProgress.GetStars(level), 1);
            if (level < 5) Assert.AreEqual(level + 1, GameProgress.UnlockedLevels);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NoiseWakesSleepingResident()
        {
            yield return Load(1);
            var lm = LevelManager.I;
            lm.BeginPlay();
            var r = Object.FindAnyObjectByType<Resident>();
            Assert.AreEqual(ResidentState.Sleeping, r.State);
            NoiseSystem.I.Burst(100f, r.transform.position + Vector3.right * 2f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(ResidentState.Waking, r.State);
            yield return new WaitForSeconds(2f);
            Assert.That(r.State == ResidentState.Investigating || r.State == ResidentState.Searching, "Debería investigar: " + r.State);
        }

        [UnityTest]
        public IEnumerator AwakeResidentCatchesPlayer()
        {
            yield return Load(4);
            var lm = LevelManager.I;
            lm.BeginPlay();
            yield return null;
            var r = Object.FindObjectsByType<Resident>().First(x => !x.startsAsleep);
            var p = lm.player;
            Teleport(p, r.transform.position + r.transform.forward * 1.6f);
            float t = 0f;
            while (lm.State == LevelState.Playing && t < 6f)
            {
                Teleport(p, r.transform.position + r.transform.forward * 1.4f);
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(lm.EverSpotted, "El habitante no vio al mapache");
            Assert.AreEqual(LevelState.Lost, lm.State, "El habitante no atrapó al mapache");
            yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(LevelState.Lost, lm.State);
        }

        [UnityTest]
        public IEnumerator HidingWorksAndIsBlockedWhenWatched()
        {
            yield return Load(4);
            var lm = LevelManager.I;
            lm.BeginPlay();
            yield return null;
            var r = Object.FindObjectsByType<Resident>().First(x => !x.startsAsleep);
            var p = lm.player;

            // Delante del habitante: no puede esconderse.
            var near = HidingSpot.All.OrderBy(h => Vector3.Distance(h.transform.position, r.transform.position)).First();
            Teleport(p, r.transform.position + r.transform.forward * 2.5f);
            yield return null;
            yield return null;
            if (Perception.AnyWatcherCouldSeePlayer())
                Assert.IsFalse(p.EnterHiding(near, lm), "Se escondió delante de un habitante");

            // Lejos de todos: sí puede, y nadie lo ve.
            var far = HidingSpot.All.OrderByDescending(h => Vector3.Distance(h.transform.position, r.transform.position)).First();
            Teleport(p, far.exitPoints[0].position);
            yield return null;
            yield return null;
            Assert.IsTrue(p.EnterHiding(far, lm), "No se pudo esconder lejos de todos");
            Assert.IsTrue(p.IsHidden);
            yield return null;
            Assert.IsFalse(Perception.Watchers.Any(w => w.IsWatchingPlayer), "Lo ven estando escondido");
            p.ExitHiding();
            Assert.IsFalse(p.IsHidden);
        }

        [UnityTest]
        public IEnumerator DogDetectsAndBarks()
        {
            yield return Load(3);
            var lm = LevelManager.I;
            lm.BeginPlay();
            yield return new WaitForSeconds(0.2f);
            var dog = Object.FindAnyObjectByType<Dog>();
            var p = lm.player;
            float t = 0f;
            while (dog.State != DogState.Chase && t < 3f)
            {
                Teleport(p, dog.transform.position + dog.transform.forward * 1.5f);
                t += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(DogState.Chase, dog.State, "El perro no detectó al mapache");
            yield return new WaitForSeconds(0.2f);
            Assert.GreaterOrEqual(NoiseSystem.I.Level, 30f, "El perro no ladró");
            var r = Object.FindAnyObjectByType<Resident>();
            yield return new WaitForSeconds(0.2f);
            Assert.AreNotEqual(ResidentState.Sleeping, r.State, "El ladrido no despertó al habitante");
        }

        [UnityTest]
        public IEnumerator SecurityCameraTriggersAlarm()
        {
            yield return Load(4);
            var lm = LevelManager.I;
            lm.BeginPlay();
            yield return null;
            var cam = Object.FindObjectsByType<SecurityCamera>().OrderBy(c => c.transform.position.z).First();
            var p = lm.player;
            float t = 0f;
            while (NoiseSystem.I.Level < 99f && t < 12f && lm.State == LevelState.Playing)
            {
                // Se coloca frente al cabezal, allá donde mire.
                Vector3 fwd = cam.head.forward; fwd.y = 0f; fwd.Normalize();
                Teleport(p, cam.transform.position + fwd * 3f);
                t += Time.deltaTime;
                yield return null;
            }
            Assert.GreaterOrEqual(NoiseSystem.I.Level, 99f, "La cámara no activó la alarma");
        }

        [UnityTest]
        public IEnumerator PickupNoiseAndBackpackLimits()
        {
            yield return Load(1);
            var lm = LevelManager.I;
            lm.BeginPlay();
            var p = lm.player;
            var chips = Lootable.All.First(l => l.itemName.Contains("papas"));
            Teleport(p, chips.transform.position + Vector3.back * 0.5f);
            Assert.IsTrue(p.TryPickup(chips, lm));
            Assert.GreaterOrEqual(NoiseSystem.I.Level, 29f, "Las papas deberían hacer ruido (10 x 3)");
            Assert.AreEqual(12, p.CarriedValue);
            p.DropLast();
            Assert.AreEqual(0, p.CarriedValue);
            Assert.IsFalse(chips.IsCarried);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WinScreenStaysUntilPlayerChooses()
        {
            yield return Load(1);
            var lm = LevelManager.I;
            lm.BeginPlay();
            var p = lm.player;
            foreach (var name in new[] { "Queso", "Pastel" })
            {
                var l = Lootable.All.First(x => x.itemName == name);
                Teleport(p, l.transform.position + Vector3.back * 0.5f);
                Assert.IsTrue(p.TryPickup(l, lm));
            }
            Teleport(p, lm.exit.transform.position);
            yield return null;
            yield return null;
            Assert.IsTrue(lm.GoalMet);
            lm.Escape();
            yield return new WaitForSecondsRealtime(3f);
            Assert.AreEqual(GameProgress.LevelScenes[0], SceneManager.GetActiveScene().name, "La escena cambió sola tras ganar");
            Assert.AreEqual(LevelState.Won, lm.State);
            Assert.IsNotNull(GameObject.Find("Victoria"), "No aparece la pantalla de victoria");
        }
    }
}
