using System;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Disposition des salles du lac, de l'Amazonie et du Japon : quel meuble, où (pied, en cases, depuis le bord gauche
    /// de la salle) et quel encombrement au sol. Sert à poser le mobilier (MapSetup) et à dessiner les sols qui en
    /// tiennent compte (tapis sous la barque, gravier ratissé autour des rochers).
    /// </summary>
    public static class AquariumRoomLayout
    {
        public readonly struct Prop
        {
            public readonly string Name;
            public readonly Func<Color[,]> Draw;
            public readonly Vector2 Foot;
            public readonly Vector2 Size;

            public Prop(string name, Func<Color[,]> draw, float x, float y, float width, float depth)
            {
                Name = name;
                Draw = draw;
                Foot = new Vector2(x, y);
                Size = new Vector2(width, depth);
            }
        }

        /// <summary>Taille des pixels de la salle : 16 par case ; la salle commence après le passage (2 cases = 32 px).</summary>
        public static Vector2 ToPixels(Vector2 cell) => new Vector2(32f + cell.x * 16f, cell.y * 16f);

        public static readonly Prop[] Lake =
        {
            new Prop("Barque exposée", AquariumPropArt.RowboatDisplay, 12f, 3.2f, 4.4f, 0.9f),
            new Prop("Banc en rondin", AquariumPropArt.LogBench, 5f, 7f, 3.2f, 0.6f),
            new Prop("Banc en rondin", AquariumPropArt.LogBench, 19f, 7f, 3.2f, 0.6f),
            new Prop("Sapin en pot", AquariumPropArt.PottedFir, 1.4f, 1f, 1f, 0.5f),
            new Prop("Sapin en pot", AquariumPropArt.PottedFir, 22.6f, 1f, 1f, 0.5f),
            new Prop("Sapin en pot", AquariumPropArt.PottedFir, 1.4f, 8.2f, 1f, 0.5f),
            new Prop("Sapin en pot", AquariumPropArt.PottedFir, 22.6f, 8.2f, 1f, 0.5f),
        };

        public static readonly Prop[] Amazon =
        {
            new Prop("Pirogue", AquariumPropArt.Pirogue, 12f, 3.4f, 5.4f, 0.8f),
            new Prop("Tronc moussu", AquariumPropArt.MossyTrunk, 12f, 6.8f, 3.8f, 0.7f),
            new Prop("Monstera", AquariumPropArt.Monstera, 1.6f, 1f, 1.4f, 0.5f),
            new Prop("Monstera", AquariumPropArt.Monstera, 22.4f, 1f, 1.4f, 0.5f),
            new Prop("Palmier en pot", AquariumPropArt.PalmPot, 1.6f, 8.2f, 1.2f, 0.5f),
            new Prop("Palmier en pot", AquariumPropArt.PalmPot, 22.4f, 8.2f, 1.2f, 0.5f),
        };

        public static readonly Prop[] Reef =
        {
            new Prop("Bénitier géant", AquariumPropArt.GiantClam, 16f, 3.6f, 2.6f, 0.7f),
            new Prop("Transat", AquariumPropArt.DeckChair, 4.5f, 6.4f, 2.2f, 0.6f),
            new Prop("Transat", AquariumPropArt.DeckChair, 19.5f, 6.4f, 2.2f, 0.6f),
            new Prop("Ancre", AquariumPropArt.Anchor, 21.6f, 1.2f, 1.2f, 0.5f),
            new Prop("Bouée", AquariumPropArt.Lifebuoy, 1.4f, 8.2f, 0.6f, 0.4f),
            new Prop("Bouée", AquariumPropArt.Lifebuoy, 22.6f, 8.2f, 0.6f, 0.4f),
        };

        /// <summary>Salle Maldives : dhoni en modèle devant le bassin, lanternes en rotin, palmiers, transat entre les vitres du sol.</summary>
        public static readonly Prop[] Atoll =
        {
            new Prop("Dhoni", AquariumAtollArt.Dhoni, 12f, 8.3f, 4.6f, 0.6f),
            new Prop("Lanterne en rotin", AquariumAtollArt.RattanLantern, 1.3f, 8.4f, 0.5f, 0.3f),
            new Prop("Lanterne en rotin", AquariumAtollArt.RattanLantern, 22.7f, 8.4f, 0.5f, 0.3f),
            new Prop("Palmier en pot", AquariumPropArt.PalmPot, 1.4f, 1.1f, 1.2f, 0.5f),
            new Prop("Palmier en pot", AquariumPropArt.PalmPot, 22.6f, 1.1f, 1.2f, 0.5f),
            new Prop("Transat", AquariumPropArt.DeckChair, 17.5f, 1.2f, 2.2f, 0.6f),   // à côté du tapis de l'ascenseur
        };

        /// <summary>Salle Côte des Squelettes : barre à roue devant le bassin, scaphandre, lanternes de navire, bouée.</summary>
        public static readonly Prop[] Skeleton =
        {
            new Prop("Barre à roue", AquariumSkeletonArt.ShipWheel, 6f, 8.2f, 1.6f, 0.5f),
            new Prop("Scaphandre", AquariumSkeletonArt.DivingHelmet, 18f, 8.2f, 1.6f, 0.5f),
            new Prop("Lanterne de navire", AquariumSkeletonArt.ShipLantern, 1.3f, 8.4f, 0.5f, 0.3f),
            new Prop("Lanterne de navire", AquariumSkeletonArt.ShipLantern, 22.7f, 8.4f, 0.5f, 0.3f),
            new Prop("Bouée", AquariumPropArt.Lifebuoy, 2.2f, 1.2f, 0.6f, 0.4f),
        };

        /// <summary>Rochers du jardin zen : le sol dessine des cercles ratissés autour.</summary>
        public static readonly Vector2[] JapanRocks = { new Vector2(8f, 3.6f), new Vector2(10.2f, 3.0f), new Vector2(17.5f, 4.8f) };

        public static readonly Prop[] Japan =
        {
            new Prop("Rocher", () => AquariumPropArt.ZenRock(0), JapanRocks[0].x, JapanRocks[0].y, 2f, 0.6f),
            new Prop("Rocher", () => AquariumPropArt.ZenRock(1), JapanRocks[1].x, JapanRocks[1].y, 1.2f, 0.4f),
            new Prop("Rocher", () => AquariumPropArt.ZenRock(2), JapanRocks[2].x, JapanRocks[2].y, 1.7f, 0.5f),
            new Prop("Bonsaï", AquariumPropArt.Bonsai, 21f, 1.2f, 2.2f, 0.6f),
            new Prop("Lanterne de pierre", JapanArt.StoneLantern, 3f, 1f, 0.7f, 0.4f),
            new Prop("Lanterne de pierre", JapanArt.StoneLantern, 14f, 1f, 0.7f, 0.4f),
            new Prop("Banc de pierre", AquariumPropArt.StoneBench, 5.5f, 7.4f, 2.6f, 0.5f),
            new Prop("Banc de pierre", AquariumPropArt.StoneBench, 19f, 7.4f, 2.6f, 0.5f),
        };
    }
}
