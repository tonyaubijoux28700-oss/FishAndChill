using System;
using FishAndChill.Save;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class SaveSlotsTests
    {
        [Test]
        public void FileName_FirstSlotKeepsTheOldName()
        {
            Assert.That(SaveSlots.FileName(0), Is.EqualTo("sauvegarde.json"));
            Assert.That(SaveSlots.FileName(1), Is.EqualTo("sauvegarde2.json"));
            Assert.That(SaveSlots.FileName(2), Is.EqualTo("sauvegarde3.json"));
            Assert.That(SaveSlots.FileName(7), Is.EqualTo("sauvegarde3.json"));
        }

        [Test]
        public void SpeciesDiscovered_CountsDistinctCaughtSpecies()
        {
            var data = new SaveData();
            data.CatchLog.Add(new SaveData.Catches { Id = "a", Count = 2 });
            data.CatchLog.Add(new SaveData.Catches { Id = "b", Count = 1 });
            data.CatchLog.Add(new SaveData.Catches { Id = "a", Count = 1 });
            data.CatchLog.Add(new SaveData.Catches { Id = "c", Count = 0 });
            Assert.That(SaveSlots.SpeciesDiscovered(data), Is.EqualTo(2));
            Assert.That(SaveSlots.SpeciesDiscovered(null), Is.EqualTo(0));
        }

        [Test]
        public void PlayTimeText_UsesMinutesThenHours()
        {
            Assert.That(SaveSlots.PlayTimeText(30f), Is.EqualTo("0 min"));
            Assert.That(SaveSlots.PlayTimeText(12 * 60f + 5f), Is.EqualTo("12 min"));
            Assert.That(SaveSlots.PlayTimeText(65 * 60f), Is.EqualTo("1 h 05"));
        }

        [Test]
        public void SavedAtText_ShowsDayAndTime()
        {
            Assert.That(SaveSlots.SavedAtText(new DateTime(2026, 11, 16, 14, 30, 0).Ticks), Is.EqualTo("16/11 à 14:30"));
            Assert.That(SaveSlots.SavedAtText(0), Is.Empty);
        }
    }
}
