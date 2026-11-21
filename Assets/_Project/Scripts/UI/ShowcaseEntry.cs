using System;
using UnityEngine;

namespace FishAndChill.UI
{
    /// <summary>Un objet posé dans la vitrine du marchand : son dessin, sa fiche, son prix et ce que fait Espace.</summary>
    public sealed class ShowcaseEntry
    {
        public Sprite Picture;

        /// <summary>Une canne : debout dans le râtelier plutôt que sur le coussin.</summary>
        public bool Tall;

        public string Name;
        public string Detail;
        public string Status;
        public Color StatusColor = Color.white;
        public string Price;
        public Color PriceColor = Color.white;

        /// <summary>Ce que fait Espace (« acheter », « prendre en main »…) ; vide si rien à faire.</summary>
        public string Action;

        public Action Buy;
    }
}
