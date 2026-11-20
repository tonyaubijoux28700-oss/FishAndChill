using FishAndChill.Villagers;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class VillagerTests
    {
        private static VillagerLines Lines() => new VillagerLines
        {
            Intro = new[] { "Bonjour !", "Je suis Tavita." },
            Daily = new[] { "Belle journée.", "Le vent tourne." },
            Rain = new[] { "Quelle pluie !" },
            Night = new[] { "Il est tard." },
            Aquarium = new[] { "Déjà {0} espèces !" },
        };

        [Test]
        public void IsOutside_DuringTheDay_HomeAtNight()
        {
            Assert.That(VillagerSchedule.IsOutside(12f, 8f, 19f), Is.True);
            Assert.That(VillagerSchedule.IsOutside(8f, 8f, 19f), Is.True);
            Assert.That(VillagerSchedule.IsOutside(19f, 8f, 19f), Is.False);
            Assert.That(VillagerSchedule.IsOutside(3f, 8f, 19f), Is.False);
        }

        [Test]
        public void IsOutside_NightOwl_CrossesMidnight()
        {
            Assert.That(VillagerSchedule.IsOutside(23f, 20f, 2f), Is.True);
            Assert.That(VillagerSchedule.IsOutside(1f, 20f, 2f), Is.True);
            Assert.That(VillagerSchedule.IsOutside(12f, 20f, 2f), Is.False);
        }

        [Test]
        public void IsDark_FromNightfallToDawn()
        {
            Assert.That(VillagerSchedule.IsDark(12f), Is.False);
            Assert.That(VillagerSchedule.IsDark(18.5f), Is.False);   // crépuscule : on rentre encore à pied
            Assert.That(VillagerSchedule.IsDark(20f), Is.True);
            Assert.That(VillagerSchedule.IsDark(2f), Is.True);
            Assert.That(VillagerSchedule.IsDark(8f), Is.False);
        }

        [Test]
        public void WanderTarget_StaysInsideTheRadius()
        {
            var random = new System.Random(4);
            var anchor = new Vector2(10f, -3f);
            for (int i = 0; i < 200; i++)
            {
                Assert.That(Vector2.Distance(VillagerSchedule.WanderTarget(random, anchor, 5f), anchor), Is.LessThanOrEqualTo(5.0001f));
            }
        }

        [Test]
        public void Pick_FirstMeeting_GivesTheWholeIntro()
        {
            string[] pages = VillagerDialogue.Pick(Lines(), new DialogueContext { FirstMeeting = true, Raining = true }, 0);
            Assert.That(pages, Is.EqualTo(new[] { "Bonjour !", "Je suis Tavita." }));
        }

        [Test]
        public void Pick_Rain_SaysARainLine_EveryOtherRoll()
        {
            var context = new DialogueContext { Raining = true };
            Assert.That(VillagerDialogue.Pick(Lines(), context, 0), Is.EqualTo(new[] { "Quelle pluie !" }));
            Assert.That(VillagerDialogue.Pick(Lines(), context, 1)[0], Is.Not.EqualTo("Quelle pluie !"));
        }

        [Test]
        public void Pick_Aquarium_InsertsTheSpeciesCount()
        {
            string[] pages = VillagerDialogue.Pick(Lines(), new DialogueContext { AquariumSpecies = 12 }, 1);
            Assert.That(pages, Is.EqualTo(new[] { "Déjà 12 espèces !" }));
        }

        [Test]
        public void Pick_Otherwise_SaysADailyLine()
        {
            string[] pages = VillagerDialogue.Pick(Lines(), new DialogueContext(), 3);
            Assert.That(pages, Is.EqualTo(new[] { "Le vent tourne." }));
        }

        [Test]
        public void Memory_RemembersWhoWasMet_AndReloads()
        {
            VillagerMemory.Load(null);
            Assert.That(VillagerMemory.HasMet("tavita"), Is.False);
            VillagerMemory.MarkMet("tavita");
            Assert.That(VillagerMemory.Capture(), Is.EqualTo(new[] { "tavita" }));
            VillagerMemory.Load(new[] { "rose" });
            Assert.That(VillagerMemory.HasMet("tavita"), Is.False);
            Assert.That(VillagerMemory.HasMet("rose"), Is.True);
            VillagerMemory.Load(null);
        }
    }
}
