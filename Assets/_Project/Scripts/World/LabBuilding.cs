using FishAndChill.Player;
using FishAndChill.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.World
{
    /// <summary>
    /// Le laboratoire de biologie marine de l'île : pièce de 11×8 cases (aquarium d'étude, étagères à bocaux, bureau de
    /// la scientifique, socle de pierre). Devant le bureau, E fait parler la scientifique (boîte de dialogue en bas de
    /// l'écran, E / Espace pour la suite, Échap pour fermer). Entrée et sortie : voir <see cref="InteriorBuilding"/>.
    /// </summary>
    public sealed class LabBuilding : InteriorBuilding
    {
        public const float InteriorWidth = 13f;
        public const float InteriorHeight = 8f;

        // ---------- Salle des spécimens (derrière la porte blindée, au fond à droite) ----------

        /// <summary>Porte blindée dans le mur du fond (relatif à la pièce, en cases) : on y entre en poussant vers le haut.</summary>
        public static readonly Rect SpecimenDoor = new Rect(11.2f, 4.2f, 1.3f, 0.6f);

        public const float SpecimenRoomWidth = 30f;
        public const float SpecimenRoomHeight = 12f;
        /// <summary>Haut du sol de la salle des spécimens (le pied du mur), en cases depuis le bas.</summary>
        public const float SpecimenFloorTop = 6f;
        /// <summary>Pied des tubes, en cases depuis le bas de la salle.</summary>
        public const float TubeFootY = 5.55f;

        // Mobilier de la salle des spécimens : pied de chaque meuble (relatif à la salle, en cases) et ce qu'il bloque.
        public static readonly Vector2 TableFoot = new Vector2(10.5f, 2.0f);
        public static readonly Vector2 ConsoleFoot = new Vector2(21.5f, 2.7f);
        public static readonly Vector2 CartFoot = new Vector2(26.8f, 1.5f);
        public static readonly Vector2 CratesFoot = new Vector2(2.4f, 1.4f);
        public static readonly Vector2 PoolFoot = new Vector2(18.2f, 0.6f);
        public static readonly Vector2[] VentSpots = { new Vector2(5.5f, 3.7f), new Vector2(24.5f, 0.9f) };

        private static readonly Rect[] SpecimenFurniture =
        {
            new Rect(7.7f, 1.9f, 5.6f, 1.0f),    // table de dissection
            new Rect(19.6f, 2.6f, 3.8f, 0.9f),   // console d'analyse
            new Rect(25.9f, 1.4f, 1.8f, 0.5f),   // chariot d'échantillons
            new Rect(1.0f, 1.3f, 2.8f, 1.2f),    // caisses
            new Rect(16.8f, 0.55f, 2.8f, 1.0f),  // bassin de quarantaine
        };

        /// <summary>Coin bas-gauche de la salle des spécimens (à droite de la pièce, hors de l'île).</summary>
        public Vector2 SpecimenOrigin => InteriorOrigin + new Vector2(InteriorWidth + 7f, 0f);

        public Rect SpecimenRect => new Rect(SpecimenOrigin, new Vector2(SpecimenRoomWidth, SpecimenRoomHeight));

        /// <summary>Le sas de la salle des spécimens, au milieu du bas : on en ressort en descendant.</summary>
        public Vector2 SpecimenExit => SpecimenOrigin + new Vector2(SpecimenRoomWidth * 0.5f, 0.9f);

        /// <summary>Le joueur est dans la salle des spécimens.</summary>
        public bool InSpecimenRoom { get; private set; }

        protected override float EntranceX => 5.5f;   // le paillasson reste sous le bureau, la pièce s'élargit à droite

        protected override Rect CameraRect => InSpecimenRoom ? SpecimenRect : InteriorRect;

        private SpecimenTube[] _tubes = new SpecimenTube[0];

        /// <summary>Nom de la scientifique, affiché sur sa boîte de dialogue.</summary>
        public const string ScientistName = "Dr Hina Teva";

        [SerializeField] private UiSkin _skin;
        [SerializeField, Tooltip("Progression du joueur : la scientifique parle des fragments trouvés.")] private Collection.PlayerProgress _progress;
        [SerializeField, Tooltip("La pêche, qui connaît les poissons de légende.")] private Fishing.FishingController _fishing;
        [SerializeField, Min(0.5f)] private float _talkRadius = 1.1f;
        [SerializeField, Tooltip("Décor de l'atelier des artefacts (établi, dalle de pierre, cases de velours, fiches).")]
        private ArtifactWorkshop.Look _workshopLook = new ArtifactWorkshop.Look();

        private Text _prompt;
        private GameObject _dialog;
        private Text _line;
        private Text _hint;
        private string[] _lines;
        private int _index;
        private bool _metBefore;
        private int _openedFrame = -1;

        /// <summary>Le joueur est devant le bureau (E sert alors à parler, pas à ouvrir le sac).</summary>
        public static bool PlayerAtDesk { get; private set; }

        /// <summary>La scientifique parle (le jeu attend la fin du dialogue).</summary>
        public static bool DialogOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetFlags()
        {
            PlayerAtDesk = false;
            DialogOpen = false;
            PlayerAtPedestal = false;
        }

        public override Vector2 InteriorSize => new Vector2(InteriorWidth, InteriorHeight);

        protected override Rect FloorArea => new Rect(0.3f, 0.15f, InteriorWidth - 0.6f, 4.6f);

        /// <summary>Aquarium d'étude contre le mur, à gauche (relatif à la pièce, en cases).</summary>
        public static readonly Rect StudyTank = new Rect(0.3f, 3.9f, 2.9f, 1.1f);

        /// <summary>Étagères à bocaux contre le mur, à droite.</summary>
        public static readonly Rect Shelves = new Rect(8.0f, 4.1f, 2.7f, 0.9f);

        /// <summary>Bureau de la scientifique, au milieu.</summary>
        public static readonly Rect Desk = new Rect(3.7f, 3.2f, 3.6f, 0.8f);

        /// <summary>Socle de pierre (on y posera les artefacts), devant à droite.</summary>
        public static readonly Rect Pedestal = new Rect(8.4f, 1.5f, 1.2f, 0.6f);

        /// <summary>Plante en pot, devant à gauche.</summary>
        public static readonly Rect Plant = new Rect(0.6f, 1.4f, 0.9f, 0.6f);

        /// <summary>Pied du bureau (pivot de son sprite).</summary>
        public Vector2 DeskSpot => InteriorOrigin + new Vector2(Desk.center.x, Desk.yMin);

        /// <summary>Pied de la scientifique, derrière son bureau.</summary>
        public Vector2 ScientistSpot => InteriorOrigin + new Vector2(Desk.center.x, Desk.yMax + 0.35f);

        /// <summary>Pied du socle (pivot de son sprite).</summary>
        public Vector2 PedestalSpot => InteriorOrigin + new Vector2(Pedestal.center.x, Pedestal.yMin);

        /// <summary>Pied de la plante (pivot de son sprite).</summary>
        public Vector2 PlantSpot => InteriorOrigin + new Vector2(Plant.center.x, Plant.yMin);

        /// <summary>Là où l'on se tient pour parler : devant le bureau.</summary>
        public Vector2 DeskFront => InteriorOrigin + new Vector2(Desk.center.x, Desk.yMin - 0.55f);

        private static readonly string[] FirstMeeting =
        {
            "Oh ! Un visiteur ! Bienvenue au laboratoire de Mahana. Je suis le Dr Hina Teva, biologiste marine.",
            "J'étudie les poissons de légende : des créatures immenses dont parlent les vieux pêcheurs de chaque coin du monde.",
            "On raconte que chacune garde un artefact ancien, brisé en morceaux et dispersé au fond de ses eaux.",
            "Si tu pêches ces fragments et que tu me rapportes un artefact complet, je saurai le restaurer… et réveiller la créature.",
            "Ce sera le combat de ta vie ! Reviens me voir dès que tu auras trouvé un fragment.",
        };

        private static readonly string[] LaterMeetings =
        {
            "Alors, ces fragments ? Ouvre bien les yeux : ils se prennent à l'hameçon comme un poisson.",
            "Tu savais que certains poissons vivent plus de cent ans ? Les légendes, elles, sont bien plus vieilles encore.",
            "Mes bocaux sont pleins d'écailles étranges… Je suis sûre qu'il existe des géants que personne n'a jamais pêchés.",
        };

        protected override void SetupInterior(WorldMap map)
        {
            foreach (Rect r in new[] { StudyTank, Shelves, Desk, Pedestal, Plant })
            {
                map.AddObstacle(new Rect(InteriorOrigin + r.position, r.size));
            }

            // Salle des spécimens : son sol, et le socle de chaque tube.
            map.AddWalkableArea(new Rect(SpecimenOrigin + new Vector2(0.3f, 0.15f), new Vector2(SpecimenRoomWidth - 0.6f, SpecimenFloorTop - 0.15f)));
            foreach (Rect r in SpecimenFurniture)
            {
                map.AddObstacle(new Rect(SpecimenOrigin + r.position, r.size));
            }
            _tubes = GetComponentsInChildren<SpecimenTube>(true);
            if (_tubes.Length == 0)
            {
                _tubes = FindObjectsByType<SpecimenTube>(FindObjectsInactive.Include);
            }

            foreach (SpecimenTube tube in _tubes)
            {
                Vector2 foot = tube.transform.position;
                map.AddObstacle(new Rect(foot.x - 1.75f, foot.y - 0.1f, 3.5f, SpecimenOrigin.y + SpecimenFloorTop - foot.y + 0.2f));
            }
        }

        /// <summary>Passe la porte blindée : du laboratoire à la salle des spécimens, ou retour devant la porte.</summary>
        public void GoToSpecimenRoom(bool enter)
        {
            InSpecimenRoom = enter;
            if (enter)
            {
                Player.Teleport(SpecimenExit + new Vector2(0f, 0.3f), Vector2.up);
            }
            else
            {
                Player.Teleport(InteriorOrigin + new Vector2(SpecimenDoor.center.x, SpecimenDoor.yMin - 0.5f), Vector2.down);
            }

            if (FollowCamera != null)
            {
                FollowCamera.SetBoundsOverride(CameraRect);
                FollowCamera.SnapToTarget();
            }
        }

        /// <summary>Le tube devant lequel se tient le joueur (null : aucun).</summary>
        private SpecimenTube TubeInFront()
        {
            if (!InSpecimenRoom || Player == null) return null;
            Vector2 feet = Player.transform.position;
            foreach (SpecimenTube tube in _tubes)
            {
                if (tube != null && Mathf.Abs(feet.x - tube.Front.x) < 1.4f && feet.y > tube.Front.y - 1.2f) return tube;
            }

            return null;
        }

        /// <summary>Ce que dit la plaque d'un tube.</summary>
        public static string PlaqueText(SpecimenTube tube)
        {
            if (tube.HasSpecimen)
            {
                Collection.LegendFish legend = tube.Legend;
                return $"{legend.Title}  —  {legend.KindName}, {legend.SizeCm:0} cm  —  {legend.Zone?.DisplayName}";
            }

            return tube.Legend != null ? "Spécimen n° " + (tube.Slot + 1) + " : ??? (en attente de capture)" : "Tube vide : aucune légende connue pour l'instant";
        }

        /// <summary>Portes de la salle des spécimens (vers le haut dans le labo, vers le bas au sas) et plaques des tubes.</summary>
        private bool UpdateSpecimenDoors()
        {
            if (!IsPlayerInside || Player == null || Player.Locked)
            {
                return false;
            }

            Vector2 feet = Player.transform.position;
            Vector2 input = Player.LastInput;
            if (!InSpecimenRoom)
            {
                Rect door = new Rect(InteriorOrigin + SpecimenDoor.position, SpecimenDoor.size);
                if (door.Contains(feet) && input.y > 0.5f)
                {
                    GoToSpecimenRoom(true);
                    return true;
                }

                return false;
            }

            if (Mathf.Abs(feet.x - SpecimenExit.x) < 0.9f && feet.y < SpecimenOrigin.y + 0.6f && input.y < -0.5f)
            {
                GoToSpecimenRoom(false);
            }

            return true;
        }

        /// <summary>Là où l'on se tient pour ouvrir l'atelier des artefacts : devant le socle.</summary>
        public Vector2 PedestalFront => InteriorOrigin + new Vector2(Pedestal.center.x, Pedestal.yMin - 0.5f);

        /// <summary>Le joueur est devant le socle (E ouvre l'atelier des artefacts, pas le sac).</summary>
        public static bool PlayerAtPedestal { get; private set; }

        /// <summary>L'atelier des artefacts (créé au démarrage).</summary>
        public ArtifactWorkshop Workshop { get; private set; }

        protected override void Start()
        {
            base.Start();
            BuildUi();
            if (_progress != null && _fishing != null)
            {
                var go = new GameObject("Atelier des artefacts");
                go.transform.SetParent(transform, false);
                Workshop = go.AddComponent<ArtifactWorkshop>();
                Workshop.Init(_skin, _progress, _fishing.Legends, Player, _workshopLook);

                // L'artefact assemblé trône sur le socle (petit, au-dessus du creux).
                var idol = new GameObject("Artefact sur le socle");
                idol.transform.SetParent(transform, false);
                idol.transform.position = PedestalSpot + new Vector2(0f, 1.53f + 0.5f);
                idol.transform.localScale = Vector3.one * 0.5f;
                _pedestalIdol = idol.AddComponent<SpriteRenderer>();
                _pedestalIdol.sortingOrder = 12;
            }
        }

        private SpriteRenderer _pedestalIdol;

        /// <summary>
        /// Montre sur le socle le dernier artefact assemblé ; il bouge davantage une fois son gardien réveillé.
        /// Ancienne sauvegarde (dernier inconnu) : le dernier assemblé dans l'ordre des légendes.
        /// </summary>
        private void UpdatePedestal()
        {
            if (_pedestalIdol == null || !IsPlayerInside)
            {
                return;
            }

            Collection.LegendFish shown = null;
            string last = _progress.Legends.LastAssembled;
            foreach (Collection.LegendFish legend in _fishing.Legends)
            {
                if (legend == null || _progress.Legends.State(legend.Id) == Collection.LegendState.Collecting)
                {
                    continue;
                }

                shown = legend;
                if (legend.Id == last)
                {
                    break;
                }
            }

            _pedestalIdol.enabled = shown != null;
            if (shown == null)
            {
                return;
            }

            _pedestalIdol.sprite = shown.Artifact;
            bool awake = _progress.Legends.State(shown.Id) == Collection.LegendState.Summoned;
            float bob = Mathf.Sin(Time.time * (awake ? 2.6f : 1.2f)) * (awake ? 0.06f : 0.03f);
            _pedestalIdol.transform.position = PedestalSpot + new Vector2(0f, 2.03f + bob);
        }

        protected override void Update()
        {
            UpdatePedestal();
            if (DialogOpen)
            {
                UpdateDialog();
                return;
            }

            if (!IsPlayerInside)
            {
                InSpecimenRoom = false;
            }

            if (UpdateSpecimenDoors())
            {
                // Dans la salle des spécimens : seule la plaque du tube devant soi s'affiche.
                SpecimenTube tube = TubeInFront();
                PlayerAtDesk = false;
                PlayerAtPedestal = false;
                if (_prompt != null)
                {
                    _prompt.gameObject.SetActive(tube != null);
                    if (tube != null) _prompt.text = PlaqueText(tube);
                }

                return;
            }

            bool workshopOpen = Workshop != null && Workshop.IsOpen;
            base.Update();
            bool free = IsPlayerInside && Player != null && !Player.Locked;
            bool atDesk = free && Vector2.Distance(Player.transform.position, DeskFront) <= _talkRadius;
            bool atPedestal = Workshop != null && IsPlayerInside && Player != null
                              && Vector2.Distance(Player.transform.position, PedestalFront) <= _talkRadius;
            PlayerAtDesk = atDesk;
            PlayerAtPedestal = atPedestal;   // reste vrai pendant que l'atelier est ouvert : E ne doit pas ouvrir le sac
            if (_prompt != null)
            {
                Vector2 doorFront = InteriorOrigin + new Vector2(SpecimenDoor.center.x, SpecimenDoor.yMin);
                bool atDoor = free && Vector2.Distance(Player.transform.position, doorFront) <= 1.2f;
                _prompt.gameObject.SetActive((atDesk || atPedestal || atDoor) && !workshopOpen);
                _prompt.text = atPedestal ? "E : atelier des artefacts" : atDesk ? $"E : parler au {ScientistName}" : "Haut : salle des spécimens";
            }

            // L'appui sur E qui vient de fermer l'atelier ne le rouvre pas.
            bool pressed = InteractInput.PressedThisFrame() && (Workshop == null || Time.frameCount != Workshop.ClosedFrame);
            if (atDesk && pressed)
            {
                Talk();
            }
            else if (atPedestal && free && !workshopOpen && pressed)
            {
                Workshop.Open();
            }
        }

        /// <summary>Parle à la scientifique. Public pour pouvoir le déclencher hors clavier (tests).</summary>
        public void Talk()
        {
            string[] legendLines = LegendLines();
            _lines = !_metBefore ? FirstMeeting
                : legendLines ?? new[] { LaterMeetings[Random.Range(0, LaterMeetings.Length)] };
            _metBefore = true;
            _index = 0;
            DialogOpen = true;
            PlayerAtDesk = false;
            _openedFrame = Time.frameCount;
            if (Player != null)
            {
                Player.Locked = true;
            }

            _prompt.gameObject.SetActive(false);
            _dialog.SetActive(true);
            ShowLine();
        }

        /// <summary>
        /// Ce que la scientifique dit des légendes en cours : un artefact complet d'abord, sinon des fragments déjà trouvés
        /// (combien il en manque, et où les pêcher). Null si le joueur n'a encore rien trouvé.
        /// </summary>
        private string[] LegendLines()
        {
            if (_progress == null || _fishing == null)
            {
                return null;
            }

            Collection.LegendFish started = null;
            foreach (Collection.LegendFish legend in _fishing.Legends)
            {
                if (legend == null)
                {
                    continue;
                }

                if (_progress.Legends.State(legend.Id) == Collection.LegendState.Complete)
                {
                    return new[]
                    {
                        $"Incroyable… L'artefact « {legend.ArtifactName} » est complet ! Tu as réussi !",
                        "Ces gravures parlent d'un gardien immense qui dort quelque part du côté de "
                        + $"« {ZoneName(legend)} ». L'artefact peut le réveiller.",
                        "Retourne au socle quand tu te sens prêt… et prends ta meilleure canne.",
                    };
                }

                if (_progress.Legends.State(legend.Id) == Collection.LegendState.Summoned)
                {
                    return new[]
                    {
                        $"{legend.Title} est réveillé ! Mes instruments s'affolent depuis ce matin.",
                        $"Cherche un grand remous sombre du côté de « {ZoneName(legend)} » : il est marqué en or sur ta carte.",
                        "Lance en plein dedans. Et surtout… ne lâche rien.",
                    };
                }

                if (started == null && _progress.Legends.State(legend.Id) == Collection.LegendState.Collecting
                    && _progress.Legends.Fragments(legend.Id) > 0)
                {
                    started = legend;
                }
            }

            if (started == null)
            {
                // Une légende déjà vaincue : la scientifique s'en souvient.
                foreach (Collection.LegendFish legend in _fishing.Legends)
                {
                    if (legend != null && _progress.Legends.State(legend.Id) == Collection.LegendState.Beaten)
                    {
                        return new[]
                        {
                            $"{legend.Title}, {legend.SizeCm:0} cm… Tout le monde ne parle plus que de ça sur l'île !",
                            "Il paraît que d'autres gardiens dorment ailleurs dans le monde. Mes recherches continuent…",
                        };
                    }
                }

                return null;
            }

            int missing = started.FragmentCount - _progress.Legends.Fragments(started.Id);
            int inHand = _progress.Legends.InHand(started.Id);
            var lines = new System.Collections.Generic.List<string>
            {
                $"Montre-moi ça… Des fragments de l'artefact « {started.ArtifactName} » ! Je n'en croyais pas mes yeux.",
            };
            if (inHand > 0)
            {
                lines.Add(inHand == 1
                    ? "Pose ce fragment sur le socle de pierre, là, à droite : l'artefact se reconstituera morceau par morceau."
                    : $"Tu en as {inHand} en poche. Pose-les sur le socle de pierre, là, à droite : l'artefact se reconstituera morceau par morceau.");
            }

            if (missing > 0)
            {
                lines.Add($"Il t'en manque encore {missing} à pêcher, du côté de « {ZoneName(started)} », "
                          + "et plus souvent là où les poissons se rassemblent.");
            }

            return lines.ToArray();
        }

        private static string ZoneName(Collection.LegendFish legend) => legend.Zone != null ? legend.Zone.DisplayName : "sa zone";

        /// <summary>Passe à la réplique suivante (ferme le dialogue après la dernière).</summary>
        public void Next()
        {
            if (!DialogOpen)
            {
                return;
            }

            _index++;
            if (_index >= _lines.Length)
            {
                CloseDialog();
                return;
            }

            ShowLine();
        }

        public void CloseDialog()
        {
            DialogOpen = false;
            _dialog.SetActive(false);
            if (Player != null)
            {
                Player.Locked = false;
            }
        }

        private void UpdateDialog()
        {
            // L'appui qui a ouvert le dialogue ne doit pas déjà le faire avancer.
            if (Time.frameCount == _openedFrame)
            {
                return;
            }

            if (PauseInput.TogglePressedThisFrame() || PauseInput.BackPressedThisFrame())
            {
                CloseDialog();
            }
            else if (InteractInput.PressedThisFrame() || PauseInput.ConfirmPressedThisFrame())
            {
                Next();
            }
        }

        private void ShowLine()
        {
            _line.text = _lines[_index];
            _hint.text = _index < _lines.Length - 1 ? "E / Espace : suite   —   Échap : fermer" : "E / Espace : au revoir";
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Laboratoire - Interface", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 14;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            _prompt = UiFactory.Label(root, "Aide du bureau", 30, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.60f));
            _prompt.text = $"E : parler au {ScientistName}";
            UiFactory.Center(_prompt.rectTransform, new Vector2(0f, -395f), new Vector2(1200f, 50f));
            _prompt.gameObject.SetActive(false);

            // Boîte de dialogue en bas de l'écran : cadre de la charte, bandeau au nom de la scientifique.
            Image box = UiFactory.Sliced(root, "Dialogue", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f, 0.95f));
            _dialog = box.gameObject;
            UiFactory.Place(box.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-90f, 40f), new Vector2(1260f, 250f));
            UiFactory.TitlePlaque(box.rectTransform, _skin, ScientistName, 30, new Vector2(-410f, 125f), new Vector2(360f, 64f), out _);

            _line = UiFactory.Label(box.rectTransform, "Réplique", 30, TextAnchor.UpperLeft, new Color(0.96f, 0.94f, 0.88f));
            _line.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(_line.rectTransform, new Vector2(0f, 6f), new Vector2(1130f, 130f));

            _hint = UiFactory.Label(box.rectTransform, "Aide", 22, TextAnchor.MiddleRight, new Color(0.70f, 0.76f, 0.82f));
            UiFactory.Center(_hint.rectTransform, new Vector2(0f, -92f), new Vector2(1130f, 30f));
            _dialog.SetActive(false);
        }
    }
}
