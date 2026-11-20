using System.Collections.Generic;

namespace FishAndChill.Villagers
{
    /// <summary>Les commandes en cours de tous les habitants, par identifiant. Sauvegardées avec la partie.</summary>
    public static class VillagerRequests
    {
        private static readonly Dictionary<string, VillagerRequest> ById = new Dictionary<string, VillagerRequest>();

        /// <summary>La commande d'un habitant (créée vide au besoin).</summary>
        public static VillagerRequest Get(string villagerId)
        {
            if (!ById.TryGetValue(villagerId, out VillagerRequest request))
            {
                request = new VillagerRequest { VillagerId = villagerId };
                ById[villagerId] = request;
            }

            return request;
        }

        public static List<VillagerRequest> Capture()
        {
            var list = new List<VillagerRequest>();
            foreach (VillagerRequest request in ById.Values)
            {
                list.Add(new VillagerRequest { VillagerId = request.VillagerId, SpeciesId = request.SpeciesId, Asked = request.Asked, DoneToday = request.DoneToday });
            }

            return list;
        }

        public static void Load(IEnumerable<VillagerRequest> requests)
        {
            ById.Clear();
            if (requests == null) return;
            foreach (VillagerRequest request in requests)
            {
                if (request == null || string.IsNullOrEmpty(request.VillagerId)) continue;
                ById[request.VillagerId] = new VillagerRequest { VillagerId = request.VillagerId, SpeciesId = request.SpeciesId, Asked = request.Asked, DoneToday = request.DoneToday };
            }
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => ById.Clear();
    }
}
