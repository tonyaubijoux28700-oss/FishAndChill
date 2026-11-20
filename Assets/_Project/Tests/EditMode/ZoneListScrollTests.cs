using FishAndChill.Collection;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class ZoneListScrollTests
    {
        private const float Row = 70f;
        private const float Gap = 10f;
        private const float View = 300f; // un peu moins de 4 lignes
        private const float Margin = 20f;

        [Test]
        public void Offset_StaysAtZero_WhenEverythingFits()
        {
            Assert.That(ZoneListScroll.Offset(0f, 2, 3, Row, Gap, View, Margin), Is.EqualTo(0f));
            Assert.That(ZoneListScroll.MoreAbove(0f), Is.False);
            Assert.That(ZoneListScroll.MoreBelow(0f, 3, Row, Gap, View), Is.False);
        }

        [Test]
        public void Offset_ScrollsDown_JustEnoughToShowSelection()
        {
            // Ligne 5 : de 400 à 470, plus 20 de marge, dans une vue de 300 → 190.
            float offset = ZoneListScroll.Offset(0f, 5, 10, Row, Gap, View, Margin);

            Assert.That(offset, Is.EqualTo(190f).Within(0.01f));
            Assert.That(ZoneListScroll.MoreAbove(offset), Is.True);
            Assert.That(ZoneListScroll.MoreBelow(offset, 10, Row, Gap, View), Is.True);
        }

        [Test]
        public void Offset_DoesNotMove_WhenSelectionAlreadyVisible()
        {
            Assert.That(ZoneListScroll.Offset(190f, 4, 10, Row, Gap, View, Margin), Is.EqualTo(190f));
        }

        [Test]
        public void Offset_ScrollsUp_AndClampsToTheEnds()
        {
            Assert.That(ZoneListScroll.Offset(190f, 0, 10, Row, Gap, View, Margin), Is.EqualTo(0f));

            float last = ZoneListScroll.Offset(0f, 9, 10, Row, Gap, View, Margin);
            Assert.That(last, Is.EqualTo(ZoneListScroll.MaxOffset(10, Row, Gap, View)));
            Assert.That(ZoneListScroll.MoreBelow(last, 10, Row, Gap, View), Is.False);
        }

        [Test]
        public void ContentHeight_CountsGapsBetweenRows()
        {
            Assert.That(ZoneListScroll.ContentHeight(0, Row, Gap), Is.EqualTo(0f));
            Assert.That(ZoneListScroll.ContentHeight(3, Row, Gap), Is.EqualTo(230f));
        }
    }
}
