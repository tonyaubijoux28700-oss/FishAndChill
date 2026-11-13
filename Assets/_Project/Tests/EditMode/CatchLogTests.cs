using FishAndChill.Collection;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class CatchLogTests
    {
        [Test]
        public void Register_ReportsNewSpeciesThenDuplicates()
        {
            var log = new CatchLog();

            Assert.That(log.Register("perch"), Is.True);
            Assert.That(log.Register("perch"), Is.False);
            Assert.That(log.Register("pike"), Is.True);

            Assert.That(log.SpeciesCount, Is.EqualTo(2));
            Assert.That(log.TotalCatches, Is.EqualTo(3));
            Assert.That(log.CountOf("perch"), Is.EqualTo(2));
            Assert.That(log.CountOf("trout"), Is.EqualTo(0));
        }
    }
}
