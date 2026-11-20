namespace FishAndChill.Villagers
{
    /// <summary>Où en est la commande d'un habitant (liste des quêtes de l'inventaire).</summary>
    public enum QuestStatus
    {
        /// <summary>Pas de commande (aucun poisson possible dans ses zones).</summary>
        None,

        /// <summary>Une commande attend : il faut aller lui parler pour savoir laquelle.</summary>
        NotHeard,

        /// <summary>Commande connue, le poisson reste à pêcher.</summary>
        Fishing,

        /// <summary>On a le poisson, mais c'est le seul exemplaire d'une espèce pas encore exposée.</summary>
        KeepForAquarium,

        /// <summary>On a de quoi le servir : il n'y a plus qu'à aller le voir.</summary>
        Ready,

        /// <summary>Servi aujourd'hui : nouvelle commande demain matin.</summary>
        ServedToday,
    }
}
