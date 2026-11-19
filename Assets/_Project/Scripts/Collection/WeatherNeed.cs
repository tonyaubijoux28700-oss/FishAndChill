namespace FishAndChill.Collection
{
    /// <summary>Le temps qu'il faut à une espèce pour mordre (colonne Meteo de la table des espèces).</summary>
    public enum WeatherNeed
    {
        /// <summary>Par tous les temps.</summary>
        Any,

        /// <summary>Sous la pluie (l'orage compte aussi).</summary>
        Rain,

        /// <summary>Seulement pendant un orage.</summary>
        Storm,

        /// <summary>Dans le brouillard.</summary>
        Fog,

        /// <summary>Par beau temps (ciel dégagé).</summary>
        Clear,
    }
}
