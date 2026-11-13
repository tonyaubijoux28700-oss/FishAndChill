using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class CameraMathTests
    {
        private static readonly Rect Map = Rect.MinMaxRect(-50f, -30f, 50f, 30f);
        private static readonly Vector2 Half = new Vector2(9f, 5f);

        [Test]
        public void FollowsTarget_InTheMiddleOfTheMap()
        {
            Assert.That(CameraMath.ClampToBounds(new Vector2(3f, -2f), Half, Map), Is.EqualTo(new Vector2(3f, -2f)));
        }

        [Test]
        public void StopsAtTheEdgeOfTheMap()
        {
            Vector2 center = CameraMath.ClampToBounds(new Vector2(49f, -29f), Half, Map);

            Assert.That(center, Is.EqualTo(new Vector2(41f, -25f)));
        }

        [Test]
        public void CentersOnMap_WhenMapIsSmallerThanScreen()
        {
            var tiny = Rect.MinMaxRect(0f, 0f, 4f, 2f);

            Assert.That(CameraMath.ClampToBounds(new Vector2(100f, 100f), Half, tiny), Is.EqualTo(new Vector2(2f, 1f)));
        }
    }
}
