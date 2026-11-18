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
        public const float InteriorWidth = 11f;
        public const float InteriorHeight = 8f;

        /// <summary>Nom de la scientifique, affiché sur sa boîte de dialogue.</summary>
        public const string ScientistName = "Dr Hina Teva";

        [SerializeField] private UiSkin _skin;
        [SerializeField, Tooltip("Progression du joueur : la scientifique parle des fragments trouvés.")] private Collection.PlayerProgress _progress;
        [SerializeField, Tooltip("La pêche, qui connaît les poissons de légende.")] private Fishing.FishingController _fishing;
        [SerializeField, Min(0.5f)] private float _talkRadius = 1.1f;

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
                Workshop.Init(_skin, _progress, _fishing.Legends, Player);
            }
        }

        protected override void Update()
        {
            if (DialogOpen)
            {
                UpdateDialog();
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
                _prompt.gameObject.SetActive((atDesk || atPedestal) && !workshopOpen);
                _prompt.text = atPedestal ? "E : atelier des artefacts" : $"E : parler au {ScientistName}";
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
                        $"Par tous les coraux… L'artefact « {legend.ArtifactName} » est complet ! Tu as réussi !",
                        "Laisse-moi l'étudier un peu. Ces gravures parlent d'un gardien immense qui dort quelque part du côté de "
                        + $"« {ZoneName(legend)} »…",
                        "Reviens me voir bientôt : je cherche encore comment le réveiller.",
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
