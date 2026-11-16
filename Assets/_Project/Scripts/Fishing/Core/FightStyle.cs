namespace FishAndChill.Fishing
{
    /// <summary>
    /// Façon dont un poisson se bat pendant le QTE :
    /// Calm (jauge simple), Nervous (le curseur accélère et ralentit sans cesse), Drifting (la zone verte glisse),
    /// Rusher (le poisson fonce : la zone saute d'un coup, annoncée par un clignotement), Heavy (une tension monte en
    /// continu ; les réussites la font baisser, les ratés monter ; pleine, la ligne casse).
    /// </summary>
    public enum FightStyle
    {
        Calm,
        Nervous,
        Drifting,
        Rusher,
        Heavy,
    }
}
