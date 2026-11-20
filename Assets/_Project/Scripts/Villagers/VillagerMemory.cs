using System.Collections.Generic;

namespace FishAndChill.Villagers
{
    /// <summary>Les habitants déjà rencontrés (ils se présentent une seule fois). Sauvegardé avec la partie.</summary>
    public static class VillagerMemory
    {
        private static readonly HashSet<string> Met = new HashSet<string>();

        public static bool HasMet(string id) => Met.Contains(id);

        public static void MarkMet(string id)
        {
            if (!string.IsNullOrEmpty(id)) Met.Add(id);
        }

        public static List<string> Capture() => new List<string>(Met);

        public static void Load(IEnumerable<string> ids)
        {
            Met.Clear();
            if (ids == null) return;
            foreach (string id in ids) MarkMet(id);
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Met.Clear();
    }
}
