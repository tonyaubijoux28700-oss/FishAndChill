using System;
using UnityEngine;

namespace FishAndChill.Gear
{
    /// <summary>
    /// Un niveau de matériel (le niveau 0 est celui du départ, gratuit). Le sens des valeurs dépend de la famille :
    /// canne = bonus de largeur de zone (0,15 = +15 %) ; hameçon = ratés tolérés en plus ;
    /// appât = bonus de rareté (Value) et accélération des touches (Value2, 0,15 = touche 15 % plus vite) ;
    /// sac = nombre de places.
    /// </summary>
    [Serializable]
    public sealed class GearLevel
    {
        [SerializeField] private string _name;
        [SerializeField, Min(0)] private int _price;
        [SerializeField] private float _value;
        [SerializeField] private float _value2;
        [SerializeField, Tooltip("Icône de l'objet (inventaire, marchand).")] private Sprite _icon;

        public GearLevel()
        {
        }

        public GearLevel(string name, int price, float value, float value2 = 0f)
        {
            _name = name;
            _price = price;
            _value = value;
            _value2 = value2;
        }

        public string Name => _name;
        public int Price => _price;
        public float Value => _value;
        public float Value2 => _value2;
        public Sprite Icon => _icon;

        /// <summary>Donne son icône à l'objet (génération des données).</summary>
        public GearLevel WithIcon(Sprite icon)
        {
            _icon = icon;
            return this;
        }
    }
}
