namespace FishAndChill.Villagers
{
    /// <summary>
    /// Choix des répliques d'un habitant (logique pure, testée). Premier échange : toute la présentation. Ensuite une
    /// réplique : sous la pluie ou la nuit, d'abord une réplique de circonstance (une fois sur deux) ; sinon, si
    /// l'aquarium a des espèces, une fois sur trois une réplique sur l'aquarium ; sinon une réplique de tous les jours.
    /// <paramref name="roll"/> est un nombre au hasard (≥ 0) tiré par l'appelant.
    /// </summary>
    public static class VillagerDialogue
    {
        public static string[] Pick(VillagerLines lines, DialogueContext context, int roll)
        {
            if (lines == null)
            {
                return new[] { "..." };
            }

            if (context.FirstMeeting && lines.Intro.Length > 0)
            {
                return (string[])lines.Intro.Clone();
            }

            roll = roll < 0 ? -roll : roll;
            if (context.Raining && lines.Rain.Length > 0 && roll % 2 == 0)
            {
                return new[] { lines.Rain[(roll / 2) % lines.Rain.Length] };
            }

            if (context.Night && lines.Night.Length > 0 && roll % 2 == 0)
            {
                return new[] { lines.Night[(roll / 2) % lines.Night.Length] };
            }

            if (context.AquariumSpecies > 0 && lines.Aquarium.Length > 0 && roll % 3 == 1)
            {
                return new[] { string.Format(lines.Aquarium[(roll / 3) % lines.Aquarium.Length], context.AquariumSpecies) };
            }

            if (lines.Daily.Length > 0)
            {
                return new[] { lines.Daily[roll % lines.Daily.Length] };
            }

            return lines.Intro.Length > 0 ? new[] { lines.Intro[0] } : new[] { "..." };
        }
    }
}
