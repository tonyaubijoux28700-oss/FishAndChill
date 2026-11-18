namespace FishAndChill.Collection
{
    /// <summary>
    /// Où en est le joueur avec un poisson de légende : il pêche les fragments de son artefact, l'artefact est complet
    /// (à donner à la scientifique), la légende est invoquée dans sa zone, ou elle a été vaincue.
    /// </summary>
    public enum LegendState
    {
        Collecting,
        Complete,
        Summoned,
        Beaten,
    }
}
