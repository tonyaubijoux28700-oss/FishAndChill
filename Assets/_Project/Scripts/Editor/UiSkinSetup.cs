using FishAndChill.UI;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>Génère les sprites de la charte d'interface et l'asset <see cref="UiSkin"/> qui les regroupe.</summary>
    public static class UiSkinSetup
    {
        private const string Folder = "Assets/_Project/Art/UI/Charte";
        private const string AssetPath = "Assets/_Project/Data/UiSkin.asset";

        /// <summary>
        /// Jersey 10 (SIL Open Font License 1.1, licence à côté du fichier) : grille de pixels grossière, lettres
        /// bien distinctes aux tailles de l'interface (Pixelify Sans, essayée avant, rendait « c » comme « o »).
        /// </summary>
        private const string FontPath = "Assets/_Project/Art/Fonts/Jersey10-Regular.ttf";

        public static UiSkin CreateOrUpdate()
        {
            int ppu = UiSkinArt.PixelsPerUnit;
            Vector2 center = new Vector2(0.5f, 0.5f);

            Sprite panel = PixelArt.Save($"{Folder}/Cadre.png", UiSkinArt.Panel(), ppu, center, new Vector4(10, 10, 10, 10), FilterMode.Point);
            Sprite slot = PixelArt.Save($"{Folder}/Case.png", UiSkinArt.Slot(), ppu, center, new Vector4(8, 8, 8, 8), FilterMode.Point);
            Sprite plaque = PixelArt.Save($"{Folder}/Bandeau.png", UiSkinArt.Plaque(), ppu, center, new Vector4(10, 7, 10, 7), FilterMode.Point);
            Sprite water = PixelArt.Save($"{Folder}/FondEau.png", UiSkinArt.WaterBackdrop(), ppu, center, new Vector4(8, 8, 8, 8), FilterMode.Point);
            Sprite gem = PixelArt.Save($"{Folder}/Pierre.png", UiSkinArt.Gem(), ppu, center, Vector4.zero, FilterMode.Point);
            Sprite glow = PixelArt.Save($"{Folder}/Halo.png", UiSkinArt.Glow(), ppu, center, Vector4.zero, FilterMode.Bilinear);

            // Police pixel art : rendu lissé. Le rendu « sans lissage » (Hinted Raster) déforme ses lettres
            // aux tailles de l'interface (« B » lu « G », « c » lu « o »).
            if (AssetImporter.GetAtPath(FontPath) is TrueTypeFontImporter fontImporter
                && fontImporter.fontRenderingMode != FontRenderingMode.Smooth)
            {
                fontImporter.fontRenderingMode = FontRenderingMode.Smooth;
                fontImporter.SaveAndReimport();
            }

            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(AssetPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<UiSkin>();
                AssetDatabase.CreateAsset(skin, AssetPath);
            }

            var so = new SerializedObject(skin);
            so.FindProperty("_panel").objectReferenceValue = panel;
            so.FindProperty("_slot").objectReferenceValue = slot;
            so.FindProperty("_plaque").objectReferenceValue = plaque;
            so.FindProperty("_waterBackdrop").objectReferenceValue = water;
            so.FindProperty("_gem").objectReferenceValue = gem;
            so.FindProperty("_glow").objectReferenceValue = glow;
            so.FindProperty("_font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            return skin;
        }
    }
}
