using FishAndChill.Collection;
using FishAndChill.Pets;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les animaux de compagnie (<see cref="PetDefinition"/> et <see cref="PetCatalog"/> dans <c>Data/Animaux/</c>, images
    /// <see cref="PetArt"/> dans <c>Art/Animaux/</c>), puis leur pose dans la scène : l'animal qui suit le joueur
    /// (<see cref="PetFollower"/>) et la boutique du vendeur (<see cref="UI.PetShopMenu"/>).
    /// </summary>
    public static class PetSetup
    {
        private const string DataFolder = "Assets/_Project/Data/Animaux";
        private const string ArtFolder = "Assets/_Project/Art/Animaux";

        public static PetCatalog CreateOrUpdate()
        {
            PixelArt.EnsureFolder(DataFolder);
            PixelArt.EnsureFolder(ArtFolder);
            PetDefinition cat = Save("chat", "Chat", PetKind.Cat, PetArt.Kind.Cat, 300, "Les poissons mordent un peu plus vite.",
                0.85f, 1f, 0f, false);
            PetDefinition dog = Save("bouledogue", "Bouledogue", PetKind.Dog, PetArt.Kind.Dog, 300, "Trouve parfois quelques pièces en se promenant avec toi.",
                1f, 1f, 1f / 75f, false);
            PetDefinition parrot = Save("perroquet", "Perroquet", PetKind.Parrot, PetArt.Kind.Parrot, 450, "Repère les poissons rares : un peu plus de chances d'en attraper.",
                1f, 1.35f, 0f, true);

            string path = $"{DataFolder}/Animaux.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<PetCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PetCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("_pets");
            PetDefinition[] pets = { cat, dog, parrot };
            list.arraySize = pets.Length;
            for (int i = 0; i < pets.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = pets[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private static PetDefinition Save(string id, string displayName, PetKind kind, PetArt.Kind art, int price, string bonus,
            float biteWait, float rare, float coinChance, bool flies)
        {
            string path = $"{DataFolder}/{displayName}.asset";
            var pet = AssetDatabase.LoadAssetAtPath<PetDefinition>(path);
            if (pet == null)
            {
                pet = ScriptableObject.CreateInstance<PetDefinition>();
                AssetDatabase.CreateAsset(pet, path);
            }

            // Le perroquet vole : ses images sont posées plus haut que son point de suivi (pivot sous les pattes).
            Vector2 pivot = flies ? new Vector2(0.5f, -0.55f) : PetArt.Pivot;
            Sprite Frame(PetArt.Facing facing, int frame) =>
                PixelArt.Save($"{ArtFolder}/{displayName}_{facing}_{frame}.png", PetArt.Draw(art, facing, frame), CharacterSprites.PixelsPerUnit, pivot);

            var so = new SerializedObject(pet);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_price").intValue = price;
            so.FindProperty("_bonusText").stringValue = bonus;
            so.FindProperty("_biteWaitFactor").floatValue = biteWait;
            so.FindProperty("_rareMultiplier").floatValue = rare;
            so.FindProperty("_coinChancePerSecond").floatValue = coinChance;
            so.FindProperty("_coinMin").intValue = 5;
            so.FindProperty("_coinMax").intValue = 15;
            so.FindProperty("_flies").boolValue = flies;
            foreach ((string field, PetArt.Facing facing) in new[] { ("_down", PetArt.Facing.Down), ("_up", PetArt.Facing.Up), ("_side", PetArt.Facing.Side) })
            {
                SerializedProperty frames = so.FindProperty(field);
                frames.arraySize = 3;
                for (int f = 0; f < 3; f++) frames.GetArrayElementAtIndex(f).objectReferenceValue = Frame(facing, f);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return pet;
        }

        /// <summary>L'animal qui suit le joueur, et la boutique du vendeur sur l'objet « Partie ».</summary>
        public static void Place(GameObject partie, PlayerMovement player, PlayerProgress progress, UI.UiSkin skin)
        {
            GameObject follower = GameObject.Find("Animal de compagnie");
            if (follower == null) follower = new GameObject("Animal de compagnie");
            var renderer = follower.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = follower.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 10;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            follower.transform.position = player.transform.position + Vector3.left * 0.8f;
            var pet = follower.GetComponent<PetFollower>();
            if (pet == null) pet = follower.AddComponent<PetFollower>();
            var petSo = new SerializedObject(pet);
            petSo.FindProperty("_player").objectReferenceValue = player;
            petSo.FindProperty("_progress").objectReferenceValue = progress;
            petSo.FindProperty("_coinBubble").objectReferenceValue =
                PixelArt.Save("Assets/_Project/Art/UI/Bulle_Piece.png", BubbleArt.Coin(), CharacterSprites.PixelsPerUnit, BubbleArt.ExclamationPivot);
            petSo.ApplyModifiedPropertiesWithoutUndo();

            var menu = partie.GetComponent<UI.PetShopMenu>();
            if (menu == null) menu = partie.AddComponent<UI.PetShopMenu>();
            var menuSo = new SerializedObject(menu);
            menuSo.FindProperty("_skin").objectReferenceValue = skin;
            menuSo.FindProperty("_player").objectReferenceValue = player;
            menuSo.FindProperty("_progress").objectReferenceValue = progress;
            menuSo.FindProperty("_shop").objectReferenceValue = Object.FindAnyObjectByType<PetShopBuilding>();
            menuSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
