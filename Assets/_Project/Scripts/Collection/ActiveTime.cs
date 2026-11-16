namespace FishAndChill.Collection
{
    /// <summary>Moment où une espèce mord : à toute heure, seulement le jour ou seulement la nuit (crépuscules compris).</summary>
    public enum ActiveTime
    {
        Always,
        Day,
        Night,
    }
}
