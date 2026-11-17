using FishAndChill.Player;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class CharacterOptionsTests
    {
        [Test]
        public void DefaultLook_IsTheOriginalFisher()
        {
            var look = new CharacterLook();
            Assert.That(CharacterOptions.ValueName(look, CharacterPart.Hat), Is.EqualTo("Paille"));
            Assert.That(CharacterOptions.ValueName(look, CharacterPart.HairStyle), Is.EqualTo("Courte"));
            Assert.That(CharacterOptions.ValueName(look, CharacterPart.Top), Is.EqualTo("Chemise à fleurs"));
            Assert.That(CharacterOptions.DisplayName(look), Is.EqualTo(CharacterOptions.DefaultName));
        }

        [Test]
        public void Cycle_WrapsAround()
        {
            var look = new CharacterLook { Hat = 0 };
            CharacterOptions.Cycle(look, CharacterPart.Hat, -1);
            Assert.That(look.Hat, Is.EqualTo(CharacterOptions.Hats.Length - 1));
            CharacterOptions.Cycle(look, CharacterPart.Hat, 1);
            Assert.That(look.Hat, Is.EqualTo(0));
        }

        [Test]
        public void Sanitize_ClampsBadValues()
        {
            var look = new CharacterLook { Skin = 99, HairColor = -4, Name = "  <b>Marina</b>  " };
            CharacterLook clean = CharacterOptions.Sanitize(look);
            Assert.That(clean.Skin, Is.EqualTo(CharacterOptions.SkinColors.Length - 1));
            Assert.That(clean.HairColor, Is.EqualTo(0));
            Assert.That(clean.Name, Is.EqualTo("bMarinab"));
            Assert.That(look.Skin, Is.EqualTo(99), "l'original n'est pas modifié");
            Assert.That(CharacterOptions.Sanitize(null).Hat, Is.EqualTo(1));
        }

        [Test]
        public void EveryOption_HasAColorOrName()
        {
            Assert.That(CharacterOptions.SkinColors.Length, Is.EqualTo(CharacterOptions.SkinNames.Length));
            Assert.That(CharacterOptions.HairColors.Length, Is.EqualTo(CharacterOptions.HairColorNames.Length));
            Assert.That(CharacterOptions.TopColors.Length, Is.EqualTo(CharacterOptions.TopColorNames.Length));
            Assert.That(CharacterOptions.BottomColors.Length, Is.EqualTo(CharacterOptions.BottomNames.Length));
        }

        [Test]
        public void Typing_RespectsTheLimit()
        {
            string name = string.Empty;
            name = CharacterOptions.Type(name, ' ');
            Assert.That(name, Is.Empty, "pas d'espace au début");
            foreach (char c in "Jean-Ève Le Pêcheur du lac")
            {
                name = CharacterOptions.Type(name, c);
            }

            Assert.That(name.Length, Is.EqualTo(CharacterOptions.MaxNameLength));
            Assert.That(CharacterOptions.Type("Léa", '<'), Is.EqualTo("Léa"));
            Assert.That(CharacterOptions.Backspace("Léa"), Is.EqualTo("Lé"));
            Assert.That(CharacterOptions.Backspace(string.Empty), Is.Empty);
        }

        [Test]
        public void Randomize_StaysInRange()
        {
            var random = new System.Random(3);
            var look = new CharacterLook { Name = "Tom" };
            for (int i = 0; i < 50; i++)
            {
                CharacterOptions.Randomize(look, random);
                foreach (CharacterPart part in CharacterOptions.Parts)
                {
                    Assert.That(look.Get(part), Is.InRange(0, CharacterOptions.Count(part) - 1));
                }
            }

            Assert.That(look.Name, Is.EqualTo("Tom"));
        }

        [Test]
        public void Painter_DrawsEveryHatHairAndTop()
        {
            var look = new CharacterLook();
            for (int hat = 0; hat < CharacterOptions.Hats.Length; hat++)
            {
                for (int hair = 0; hair < CharacterOptions.HairStyles.Length; hair++)
                {
                    for (int top = 0; top < CharacterOptions.Tops.Length; top++)
                    {
                        look.Hat = hat;
                        look.HairStyle = hair;
                        look.Top = top;
                        foreach (Color[,] image in new[] { CharacterPainter.Front(look, 1, false), CharacterPainter.Front(look, 0, true), CharacterPainter.Side(look, 2) })
                        {
                            Assert.That(image.GetLength(0), Is.EqualTo(CharacterPainter.Width));
                            Assert.That(image.GetLength(1), Is.EqualTo(CharacterPainter.Height));
                            Assert.That(PixelShading.IsFilled(image, 8, 12), Is.True, "la tête est dessinée");
                        }
                    }
                }
            }
        }

        [Test]
        public void Painter_ChangesWithTheLook()
        {
            Color[,] straw = CharacterPainter.Front(new CharacterLook(), 0, false);
            Color[,] cap = CharacterPainter.Front(new CharacterLook { Hat = 2 }, 0, false);
            Color[,] red = CharacterPainter.Front(new CharacterLook { TopColor = 1 }, 0, false);
            Assert.That(cap[2, 17], Is.Not.EqualTo(straw[2, 17]), "chapeau différent");
            Assert.That(red[4, 7], Is.Not.EqualTo(straw[4, 7]), "haut d'une autre couleur");
        }
    }
}
