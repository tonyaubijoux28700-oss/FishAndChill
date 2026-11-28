using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.UI
{
    /// <summary>Une étape d'une quête : son texte, atteinte ou non (sinon « ??? »), faite ou non.</summary>
    public sealed class QuestStep
    {
        public string Text;
        public bool Reached = true;
        public bool Done;
    }

    /// <summary>
    /// Une quête du journal (<see cref="QuestJournal"/>), quelle que soit sa sorte : titre, où elle se passe, une image
    /// (artefact, habitant, poisson demandé), ses étapes, une note (récompense, souvenir), et la rangée des perles de Chiyo.
    /// </summary>
    public sealed class QuestEntry
    {
        public string Title;
        public string Where;
        public Sprite Picture;
        public Color PictureTint = Color.white;
        public readonly List<QuestStep> Steps = new List<QuestStep>();
        public string Note;
        public bool Done;
        public bool Pearls;

        public QuestEntry Step(string text, bool done, bool reached = true)
        {
            Steps.Add(new QuestStep { Text = text, Done = done, Reached = reached });
            return this;
        }
    }
}
