using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Onglet « Reliques » de l'inventaire (<see cref="InventoryHud"/>) : un coffre ouvert dont le fond de velours est
    /// creusé d'un emplacement en forme de chaque artefact. Les fragments posés au labo y sont en couleur, ceux en poche
    /// à demi transparents ; les emplacements vides attendent leur artefact. À droite, une étiquette en papier détaille
    /// la relique choisie (souris, ou Haut / Bas) : nom, zone, pierres des fragments, étape et suite.
    /// </summary>
    public sealed class RelicChestPage
    {
        // Le coffre (Editor/RelicChestArt, 300×160 px) est affiché à 3,8 unités par pixel ; ces rectangles reprennent
        // son velours et sa plaque de laiton, en unités d'interface depuis le centre du coffre.
        private const float PixelSize = 3.8f;
        private static readonly Vector2 ChestSize = new Vector2(300f, 160f) * PixelSize;
        private static readonly Vector2 ChestCenter = new Vector2(-210f, -4f);
        private static readonly Rect VelvetArea = new Rect((12f - 150f) * PixelSize, (18f - 80f) * PixelSize, 276f * PixelSize, 92f * PixelSize);
        private static readonly Vector2 PlateCenter = new Vector2(0f, (139f - 80f) * PixelSize);
        private static readonly Vector2 PlateSize = new Vector2(136f, 18f) * PixelSize;

        private static readonly Vector2 LabelCenter = new Vector2(615f, -4f);
        private static readonly Vector2 LabelSize = new Vector2(400f, 608f);

        // Teintes multipliées par la couleur des silhouettes (0,46 ; 0,56 ; 0,68) : creux bordeaux très sombre, bord rosé.
        private const int HollowLayers = 3;
        private static readonly Color Hollow = new Color(0.22f, 0.036f, 0.06f);
        private static readonly Color HollowLip = new Color(1f, 0.46f, 0.47f);
        private static readonly Color PlateInk = new Color(0.24f, 0.13f, 0.04f);
        private static readonly Color Ink = new Color(0.22f, 0.13f, 0.07f);
        private static readonly Color Soft = new Color(0.42f, 0.32f, 0.22f);
        private static readonly Color Title = new Color(0.62f, 0.22f, 0.12f);
        private static readonly Color Gold = new Color(0.86f, 0.62f, 0.12f);
        private static readonly Color Pocket = new Color(0.16f, 0.62f, 0.62f);
        private static readonly Color Missing = new Color(0.62f, 0.55f, 0.46f);

        private readonly RectTransform _page;
        private readonly UiSkin _skin;
        private readonly PlayerProgress _progress;
        private readonly FishingController _fishing;
        private readonly RectTransform _velvet;
        private readonly Text _header;
        private readonly Text _empty;
        private readonly GameObject _label;
        private readonly Text _name;
        private readonly Text _zone;
        private readonly RectTransform _gemRow;
        private readonly List<Image> _gems = new List<Image>();
        private readonly Text _status;
        private readonly Text _hint;
        private readonly List<Slot> _slots = new List<Slot>();
        private int _selected;
        private Vector2 _lastMouse = new Vector2(float.NaN, float.NaN);

        private sealed class Slot
        {
            public RectTransform Root;
            public Image Glow;
            public readonly List<Image> Lip = new List<Image>();
            public readonly List<Image> Hole = new List<Image>();
            public readonly List<Image> Pieces = new List<Image>();
        }

        public RelicChestPage(RectTransform page, UiSkin skin, Sprite chest, Sprite paper, PlayerProgress progress, FishingController fishing)
        {
            _page = page;
            _skin = skin;
            _progress = progress;
            _fishing = fishing;

            Image box = UiFactory.Plain(page, "Coffre aux reliques", chest != null ? Color.white : new Color(0.36f, 0.08f, 0.12f));
            box.sprite = chest;
            UiFactory.Center(box.rectTransform, ChestCenter, ChestSize);

            var velvet = new GameObject("Velours", typeof(RectTransform));
            _velvet = velvet.GetComponent<RectTransform>();
            _velvet.SetParent(box.rectTransform, false);
            UiFactory.Center(_velvet, VelvetArea.center, VelvetArea.size);

            _header = UiFactory.Label(box.rectTransform, "Titre reliques", 28, TextAnchor.MiddleCenter, PlateInk);
            UiFactory.Bold(_header);
            UiFactory.Center(_header.rectTransform, PlateCenter, PlateSize);

            _empty = UiFactory.Label(_velvet, "Aucune légende", 26, TextAnchor.MiddleCenter, new Color(0.95f, 0.85f, 0.8f));
            _empty.text = "Aucune légende connue pour l'instant.";
            UiFactory.Stretch(_empty.rectTransform);

            // L'étiquette en papier de la relique choisie.
            Image label = UiFactory.Sliced(page, "Étiquette de la relique", paper != null ? paper : skin != null ? skin.Slot : null, paper != null ? Color.white : new Color(0.93f, 0.88f, 0.74f));
            _label = label.gameObject;
            RectTransform card = label.rectTransform;
            UiFactory.Center(card, LabelCenter, LabelSize);
            _name = Text(card, "Nom", 34, Title, 236f, 90f);
            UiFactory.Bold(_name);
            _zone = Text(card, "Zone", 22, Soft, 170f, 56f);
            var gemRow = new GameObject("Fragments", typeof(RectTransform));
            _gemRow = gemRow.GetComponent<RectTransform>();
            _gemRow.SetParent(card, false);
            UiFactory.Center(_gemRow, new Vector2(0f, 110f), new Vector2(340f, 40f));
            _status = Text(card, "Étape", 26, Ink, 30f, 100f);
            UiFactory.Bold(_status);
            _hint = Text(card, "Suite", 22, Soft, -110f, 150f);
        }

        /// <summary>Aide en bas de la fenêtre quand cette page est ouverte.</summary>
        public string Help => "Souris ou Haut / Bas : choisir une relique   •   ";

        private static Text Text(RectTransform parent, string name, int size, Color color, float y, float height)
        {
            Text text = UiFactory.Label(parent, name, size, TextAnchor.MiddleCenter, color);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(text.rectTransform, new Vector2(0f, y), new Vector2(340f, height));
            return text;
        }

        /// <summary>Souris sur un emplacement, ou Haut / Bas (croix de la manette) : change de relique.</summary>
        public void HandleInput()
        {
            int count = _slots.Count;
            if (count == 0) return;

            int step = 0;
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            if (k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) step = -1;
            if (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) step = 1;
            if (g != null && g.dpad.up.wasPressedThisFrame) step = -1;
            if (g != null && g.dpad.down.wasPressedThisFrame) step = 1;
            if (step != 0) Select((_selected + step + count) % count);

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 p = mouse.position.ReadValue();
                if (p != _lastMouse || mouse.leftButton.wasPressedThisFrame)
                {
                    _lastMouse = p;
                    for (int i = 0; i < count; i++)
                    {
                        if (_slots[i].Root.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(_slots[i].Root, p, null))
                        {
                            Select(i);
                            break;
                        }
                    }
                }
            }

            // Le halo de l'emplacement choisi respire doucement.
            Slot chosen = _slots[_selected];
            float pulse = 0.55f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f);
            chosen.Glow.color = new Color(1f, 0.82f, 0.38f, pulse);
        }

        private void Select(int index)
        {
            if (index == _selected) return;
            _selected = index;
            Refresh();
        }

        public void Refresh()
        {
            if (_fishing == null || _progress == null) return;

            IReadOnlyList<LegendFish> legends = _fishing.Legends;
            var shown = new List<LegendFish>();
            foreach (LegendFish legend in legends)
            {
                if (legend != null) shown.Add(legend);
            }

            int n = shown.Count;
            _empty.gameObject.SetActive(n == 0);
            _label.SetActive(n > 0);
            _selected = n == 0 ? 0 : Mathf.Clamp(_selected, 0, n - 1);

            // Rangées du coffre : une jusqu'à 5 reliques, deux jusqu'à 12, trois au-delà ; chaque rangée centrée.
            int rows = n <= 5 ? 1 : n <= 12 ? 2 : 3;
            int columns = Mathf.Max(1, Mathf.CeilToInt(n / (float)rows));
            float cellWidth = VelvetArea.width / columns;
            float cellHeight = VelvetArea.height / rows;
            float artHeight = Mathf.Min(cellHeight * 0.84f, cellWidth * 0.84f / 0.75f);
            var artSize = new Vector2(artHeight * 0.75f, artHeight);

            int beaten = 0;
            for (int i = 0; i < n; i++)
            {
                while (_slots.Count <= i) _slots.Add(CreateSlot());
                int row = i / columns;
                int inRow = Mathf.Min(columns, n - row * columns);
                int column = i % columns;
                var position = new Vector2((column - (inRow - 1) * 0.5f) * cellWidth, ((rows - 1) * 0.5f - row) * cellHeight);
                Slot slot = _slots[i];
                slot.Root.gameObject.SetActive(true);
                UiFactory.Center(slot.Root, position, artSize);
                slot.Glow.gameObject.SetActive(i == _selected);
                slot.Root.localScale = Vector3.one * (i == _selected ? 1.06f : 1f);
                FillSlot(slot, shown[i]);
                if (_progress.Legends.State(shown[i].Id) == LegendState.Beaten) beaten++;
            }

            for (int i = n; i < _slots.Count; i++) _slots[i].Root.gameObject.SetActive(false);

            _header.text = $"Reliques des légendes   {beaten} / {n} vaincue{(beaten > 1 ? "s" : string.Empty)}";
            if (n > 0) FillLabel(shown[_selected]);
        }

        private Slot CreateSlot()
        {
            var slot = new Slot();
            var root = new GameObject($"Emplacement {_slots.Count + 1}", typeof(RectTransform));
            slot.Root = root.GetComponent<RectTransform>();
            slot.Root.SetParent(_velvet, false);

            slot.Glow = UiFactory.Plain(slot.Root, "Halo", new Color(1f, 0.82f, 0.38f, 0.6f));
            slot.Glow.sprite = _skin != null ? _skin.Glow : null;
            UiFactory.Center(slot.Glow.rectTransform, Vector2.zero, Vector2.zero);
            slot.Glow.rectTransform.anchorMin = new Vector2(-0.35f, -0.25f);
            slot.Glow.rectTransform.anchorMax = new Vector2(1.35f, 1.25f);
            slot.Glow.rectTransform.offsetMin = slot.Glow.rectTransform.offsetMax = Vector2.zero;
            return slot;
        }

        /// <summary>Une couche d'images (une par fragment), décalée de <paramref name="offset"/> unités.</summary>
        private static void Layer(Slot slot, List<Image> images, int count, string name, Vector2 offset)
        {
            while (images.Count < count)
            {
                Image image = UiFactory.Plain(slot.Root, name, Color.white);
                image.preserveAspect = true;
                image.raycastTarget = false;
                UiFactory.Stretch(image.rectTransform);
                image.rectTransform.offsetMin = offset;
                image.rectTransform.offsetMax = offset;
                images.Add(image);
            }
        }

        private void FillSlot(Slot slot, LegendFish legend)
        {
            LegendProgress progress = _progress.Legends;
            LegendState state = progress.State(legend.Id);
            int total = legend.FragmentCount;
            int found = state == LegendState.Collecting ? progress.Fragments(legend.Id) : total;
            int placed = state == LegendState.Collecting ? progress.Placed(legend.Id) : total;

            // Creux lisse taillé dans le velours à la forme de l'artefact (sa silhouette, à demi transparente, posée trois
            // fois pour être presque opaque) : un bord clair en bas à droite, le fond sombre par-dessus ; puis les fragments.
            Layer(slot, slot.Lip, HollowLayers, "Bord du creux", new Vector2(PixelSize, -PixelSize));
            Layer(slot, slot.Hole, HollowLayers, "Creux", Vector2.zero);
            Layer(slot, slot.Pieces, total, "Fragment", Vector2.zero);
            for (int i = 0; i < HollowLayers; i++)
            {
                slot.Lip[i].sprite = slot.Hole[i].sprite = legend.Silhouette;
                slot.Lip[i].color = HollowLip;
                slot.Hole[i].color = Hollow;
            }

            for (int i = 0; i < slot.Pieces.Count; i++)
            {
                Sprite fragment = i < total ? legend.Fragment(i) : null;
                slot.Pieces[i].sprite = fragment;
                slot.Pieces[i].gameObject.SetActive(fragment != null && i < found);
                slot.Pieces[i].color = i < placed ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            }

            // Ordre d'affichage : halo, bords, creux, fragments.
            slot.Glow.transform.SetAsFirstSibling();
            foreach (Image image in slot.Hole) image.transform.SetAsLastSibling();
            foreach (Image image in slot.Pieces) image.transform.SetAsLastSibling();
        }

        private void FillLabel(LegendFish legend)
        {
            LegendProgress progress = _progress.Legends;
            LegendState state = progress.State(legend.Id);
            int total = legend.FragmentCount;
            int found = state == LegendState.Collecting ? progress.Fragments(legend.Id) : total;
            int placed = state == LegendState.Collecting ? progress.Placed(legend.Id) : total;
            bool known = found > 0;
            string zone = legend.Zone != null ? legend.Zone.DisplayName : "?";

            _name.text = known ? legend.ArtifactName : "Relique inconnue";
            _zone.text = $"Du côté de « {zone} »";

            while (_gems.Count < total)
            {
                Image gem = UiFactory.Plain(_gemRow, "Pierre", Color.white);
                gem.sprite = _skin != null ? _skin.Gem : null;
                _gems.Add(gem);
            }

            for (int i = 0; i < _gems.Count; i++)
            {
                _gems[i].gameObject.SetActive(i < total);
                UiFactory.Center(_gems[i].rectTransform, new Vector2((i - (total - 1) * 0.5f) * 46f, 0f), new Vector2(34f, 34f));
                _gems[i].color = i < placed ? Gold : i < found ? Pocket : Missing;
            }

            switch (state)
            {
                case LegendState.Beaten:
                    _status.text = $"{legend.Title} vaincu, {legend.SizeCm:0} cm";
                    _status.color = Gold;
                    _hint.text = "Une légende de plus à ton palmarès !";
                    break;
                case LegendState.Summoned:
                    _status.text = $"{legend.Title} est réveillé !";
                    _status.color = Title;
                    _hint.text = "Il t'attend au remous légendaire : le point doré sur ta carte.";
                    break;
                case LegendState.Complete:
                    _status.text = "Artefact complet";
                    _status.color = Gold;
                    _hint.text = "Réveille le gardien depuis le socle du laboratoire.";
                    break;
                default:
                    _status.color = Ink;
                    _status.text = known ? $"{found} / {total} trouvés, {placed} posé{(placed > 1 ? "s" : string.Empty)}" : "Aucun fragment trouvé";
                    _hint.text = !known ? "Son emplacement attend. Ses fragments se pêchent dans cette zone."
                        : found > placed ? "Pose tes fragments sur le socle du laboratoire."
                        : "Continue de pêcher : il en manque encore.";
                    break;
            }
        }
    }
}
