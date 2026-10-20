using System.Collections.Generic;
using UnityEngine;

namespace RobaYCorre
{
    /// <summary>Algo que puede ver al mapache (habitante, perro, cámara).</summary>
    public interface IWatcher
    {
        /// <summary>Lo está viendo ahora mismo (respetando escondites).</summary>
        bool IsWatchingPlayer { get; }
        /// <summary>Lo vería si no estuviera escondido (para impedir esconderse delante de alguien).</summary>
        bool CouldSeePlayer { get; }
        Vector3 IconPosition { get; }
        string IconText { get; }
        Color IconColor { get; }
    }

    public static class Perception
    {
        public static readonly List<IWatcher> Watchers = new List<IWatcher>();

        /// <summary>Cono de visión plano (en el suelo) más comprobación de línea de visión.</summary>
        public static bool CanSee(Vector3 eye, Vector3 forward, float range, float angle, Vector3 target)
        {
            Vector3 flat = target - eye; flat.y = 0f;
            float dist = flat.magnitude;
            if (dist > range) return false;
            forward.y = 0f;
            if (dist > 0.3f && Vector3.Angle(forward, flat) > angle * 0.5f) return false;
            return !Physics.Linecast(eye, target, Layers.ObstacleMask, QueryTriggerInteraction.Ignore);
        }

        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        public static bool AnyWatcherCouldSeePlayer()
        {
            foreach (var w in Watchers)
                if (w != null && w.CouldSeePlayer) return true;
            return false;
        }
    }
}
