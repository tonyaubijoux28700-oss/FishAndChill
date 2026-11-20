namespace FishAndChill.Villagers
{
    /// <summary>Ce qui compte pour choisir la réplique d'un habitant (<see cref="VillagerDialogue"/>).</summary>
    public struct DialogueContext
    {
        public bool FirstMeeting;
        public bool Raining;
        public bool Night;
        public int AquariumSpecies;
    }
}
