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

        [Header("Spécialité (canne, hameçon)")]
        [SerializeField, Tooltip("Vitesse du curseur (1 = normale, 0,8 = plus lent).")] private float _cursorFactor = 1f;
        [SerializeField, Tooltip("Montée de la tension des poissons lourds (1 = normale, 0,5 = deux fois moins vite).")] private float _tensionFactor = 1f;
        [SerializeField, Tooltip("Portée du lancer en plus, en cases.")] private float _castBonus;
        [SerializeField, Tooltip("Emballement du curseur des poissons nerveux (1 = normal, 0,5 = moitié moins).")] private float _nervousFactor = 1f;
        [SerializeField, Tooltip("Temps entre deux rushs (1 = normal, 1,5 = rushs plus rares).")] private float _rushFactor = 1f;
        [SerializeField, Tooltip("Glissement de la zone des poissons fuyants (1 = normal, 0,6 = plus lent).")] private float _driftFactor = 1f;
        [SerializeField, TextArea, Tooltip("Ce qui rend cet objet spécial, en une phrase.")] private string _specialty;
        [SerializeField, Tooltip("Grand dessin pour la vitrine de l'inventaire (la canne entière).")] private Sprite _illustration;

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
        public float CursorFactor => _cursorFactor <= 0f ? 1f : _cursorFactor;
        public float TensionFactor => _tensionFactor <= 0f ? 1f : _tensionFactor;
        public float CastBonus => _castBonus;
        public float NervousFactor => _nervousFactor <= 0f ? 1f : _nervousFactor;
        public float RushFactor => _rushFactor <= 0f ? 1f : _rushFactor;
        public float DriftFactor => _driftFactor <= 0f ? 1f : _driftFactor;
        public string Specialty => _specialty;

        /// <summary>Donne sa spécialité à l'objet (génération des données et tests).</summary>
        public GearLevel WithStats(string specialty, float cursor = 1f, float tension = 1f, float cast = 0f, float nervous = 1f, float rush = 1f, float drift = 1f)
        {
            _specialty = specialty;
            _cursorFactor = cursor;
            _tensionFactor = tension;
            _castBonus = cast;
            _nervousFactor = nervous;
            _rushFactor = rush;
            _driftFactor = drift;
            return this;
        }
        public Sprite Illustration => _illustration != null ? _illustration : _icon;

        /// <summary>Donne son grand dessin à l'objet (génération des données).</summary>
        public GearLevel WithIllustration(Sprite illustration)
        {
            _illustration = illustration;
            return this;
        }

        /// <summary>Donne son icône à l'objet (génération des données).</summary>
        public GearLevel WithIcon(Sprite icon)
        {
            _icon = icon;
            return this;
        }
    }
}
