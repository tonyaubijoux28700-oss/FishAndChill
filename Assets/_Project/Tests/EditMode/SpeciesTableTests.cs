using FishAndChill.Collection;
using FishAndChill.Fishing;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class SpeciesTableTests
    {
        private const string Header = "Id;Nom;Scientifique;Zone;Rarete;Nature;Forme;TailleMin;TailleMax;Prix;Frequence;Vitesse;Combat;Heure;Appats;Description;Couleur;Dessin;Dos\n";

        [Test]
        public void Reads_FrenchValues_AndQuotedText()
        {
            string csv = "﻿# Table de test\n" + Header +
                "takifugu-rubripes;Fugu tigre;Takifugu rubripes;BaieJaponaise;Peu commune;Poisson;;25;70;22;12;1,1;Nerveux;Nuit;crevette, ver;\"Il se gonfle ; attention !\";#5C5C61;rond;#202024\n";
            SpeciesTable.Result result = SpeciesTable.Parse(csv);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Rows.Count, Is.EqualTo(1));
            SpeciesRow row = result.Rows[0];
            Assert.That(row.Rarity, Is.EqualTo(Rarity.Uncommon));
            Assert.That(row.Fight, Is.EqualTo(FightStyle.Nervous));
            Assert.That(row.Time, Is.EqualTo(ActiveTime.Night));
            Assert.That(row.Speed, Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(row.SizeCm, Is.EqualTo(new Vector2(25f, 70f)));
            Assert.That(row.Baits, Is.EqualTo(new[] { "crevette", "ver" }));
            Assert.That(row.Description, Is.EqualTo("Il se gonfle ; attention !"));
            Assert.That(row.Drawing, Is.EqualTo("rond"));
            Assert.That(row.Back.r, Is.EqualTo(0x20 / 255f).Within(0.01f));
        }

        [Test]
        public void Cephalopods_AndRays_HaveTheirOwnShapes()
        {
            string csv = Header + "o-v;Poulpe;O v;LacAnnecy;Rare;Céphalopode;;10;20;;;;;;;;;;\n"
                + "s-o;Seiche;S o;LacAnnecy;Rare;cephalopode;seiche;10;20;;;;;;;;;;\n"
                + "t-l;Pastenague;T l;LacAnnecy;Rare;Poisson;raie;10;20;;;;;;;;;;\n";
            SpeciesTable.Result result = SpeciesTable.Parse(csv);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Rows[0].Kind, Is.EqualTo(CreatureKind.Cephalopod));
            Assert.That(result.Rows[0].Shape, Is.EqualTo(BodyShape.Octopus), "un céphalopode sans forme : poulpe par défaut");
            Assert.That(result.Rows[1].Shape, Is.EqualTo(BodyShape.Squid));
            Assert.That(result.Rows[2].Shape, Is.EqualTo(BodyShape.Ray));
            Assert.That(CreatureKind.Cephalopod.LivesOnBottom(), Is.False, "les céphalopodes nagent dans l'aquarium");
            Assert.That(result.Rows[0].Method, Is.EqualTo(CatchMethod.Net), "un céphalopode se ramasse à l'épuisette");
            Assert.That(result.Rows[2].Method, Is.EqualTo(CatchMethod.Rod));
        }

        [Test]
        public void Engin_PutsAFishInTheNet_AndNewNaturesAreRead()
        {
            string csv = "Id;Nom;Nature;Engin\n"
                + "l-p;Lamproie;Poisson;Épuisette\n"
                + "b-u;Bulot;Mollusque;\n"
                + "a-r;Étoile;échinoderme;\n"
                + "c-c;Physalie;Cnidaire;\n"
                + "s-g;Sangsue;ver;\n"
                + "t-t;Limule;limule;\n"
                + "c-r;Crabe;Crustacé;canne\n";
            SpeciesTable.Result result = SpeciesTable.Parse(csv);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Rows[0].Method, Is.EqualTo(CatchMethod.Net), "la lamproie est un poisson, mais à l'épuisette");
            Assert.That(result.Rows[1].Kind, Is.EqualTo(CreatureKind.Mollusc));
            Assert.That(result.Rows[2].Kind, Is.EqualTo(CreatureKind.Echinoderm));
            Assert.That(result.Rows[3].Kind, Is.EqualTo(CreatureKind.Cnidarian));
            Assert.That(result.Rows[4].Kind, Is.EqualTo(CreatureKind.Worm));
            Assert.That(result.Rows[5].Kind, Is.EqualTo(CreatureKind.Chelicerate));
            Assert.That(result.Rows[1].Method, Is.EqualTo(CatchMethod.Net));
            Assert.That(result.Rows[6].Method, Is.EqualTo(CatchMethod.Rod), "la colonne Engin passe avant la nature");
        }

        [Test]
        public void EmptySettings_UseTheRarityDefaults()
        {
            string csv = Header + "x-y;Poisson X;X y;LacAnnecy;Épique;;;10;20;;;;;;;;;;\n";
            SpeciesRow row = SpeciesTable.Parse(csv).Rows[0];

            Assert.That(row.Speed, Is.EqualTo(SpeciesTable.DefaultSpeed(Rarity.Epic)));
            Assert.That(row.Misses, Is.EqualTo(SpeciesTable.DefaultMisses(Rarity.Epic)));
            Assert.That(row.Price, Is.EqualTo(SpeciesTable.DefaultPrice(Rarity.Epic)));
            Assert.That(row.Kind, Is.EqualTo(CreatureKind.Fish));
            Assert.That(row.Fight, Is.EqualTo(FightStyle.Calm));
            Assert.That(row.Time, Is.EqualTo(ActiveTime.Always));
            Assert.That(row.Weather, Is.EqualTo(WeatherNeed.Any), "pas de colonne Meteo : par tous les temps");
        }

        [Test]
        public void Meteo_IsReadInFrench()
        {
            string csv = "Id;Nom;Scientifique;Zone;Rarete;TailleMin;TailleMax;Meteo\n" +
                         "a;A;A a;LacAnnecy;rare;10;20;pluie\nb;B;B b;LacAnnecy;rare;10;20;Orage\n" +
                         "c;C;C c;LacAnnecy;rare;10;20;brouillard\nd;D;D d;LacAnnecy;rare;10;20;beau temps\ne;E;E e;LacAnnecy;rare;10;20;\n";
            SpeciesTable.Result result = SpeciesTable.Parse(csv);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Rows[0].Weather, Is.EqualTo(WeatherNeed.Rain));
            Assert.That(result.Rows[1].Weather, Is.EqualTo(WeatherNeed.Storm));
            Assert.That(result.Rows[2].Weather, Is.EqualTo(WeatherNeed.Fog));
            Assert.That(result.Rows[3].Weather, Is.EqualTo(WeatherNeed.Clear));
            Assert.That(result.Rows[4].Weather, Is.EqualTo(WeatherNeed.Any));
        }

        [Test]
        public void UnknownValue_IsReported_AndTheRowSkipped()
        {
            string csv = Header + "a-b;A;A b;LacAnnecy;super rare;;;10;20;;;;;;;;;;\nc-d;C;C d;LacAnnecy;rare;Crustace;;5;9;;;;;;;;;;\n";
            SpeciesTable.Result result = SpeciesTable.Parse(csv);

            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("super rare"));
            Assert.That(result.Rows.Count, Is.EqualTo(1));
            Assert.That(result.Rows[0].Shape, Is.EqualTo(BodyShape.Crab), "un crustacé sans forme : crabe par défaut");
        }

        [Test]
        public void Quote_RoundTripsThroughTheParser()
        {
            string text = "Dit « bonjour » ; puis \"part\"";
            string csv = "Id;Description\nz-z;" + SpeciesTable.Quote(text) + "\n";
            Assert.That(SpeciesTable.Parse(csv).Rows[0].Description, Is.EqualTo(text));
        }
    }
}
