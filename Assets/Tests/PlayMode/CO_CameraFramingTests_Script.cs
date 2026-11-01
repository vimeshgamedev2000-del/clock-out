using NUnit.Framework;
using UnityEngine;

namespace ClockOut.Tests
{
    /// <summary>
    /// Covers the camera's level-bounds maths: the view never shows past the
    /// level's edges, and a level smaller than the view is centred.
    /// </summary>
    public class CO_CameraFramingTests_Script
    {
        private const float Aspect = 16f / 9f;
        private const float Pitch = 90f; // straight down, so screen height maps 1:1 onto the ground
        private static readonly Vector2 Min = new Vector2(-15f, -9f);
        private static readonly Vector2 Max = new Vector2(15f, 35f);

        [Test]
        public void AFocusWellInsideTheLevelIsLeftAlone()
        {
            Vector3 focus = new Vector3(0f, 1f, 13f);

            Vector3 clamped = CO_CameraFollow_Script.ClampFocus(focus, 5f, Aspect, Pitch, Min, Max);

            Assert.That(clamped, Is.EqualTo(focus));
        }

        [Test]
        public void TheViewStopsAtTheSouthEdge()
        {
            // Size 8 straight down shows 8 m either side, so the focus can't go below -9 + 8.
            Vector3 clamped = CO_CameraFollow_Script.ClampFocus(new Vector3(0f, 1f, -6f), 8f, Aspect, Pitch, Min, Max);

            Assert.That(clamped.z, Is.EqualTo(-1f).Within(0.001f));
        }

        [Test]
        public void APitchedViewNeedsMoreRoomAlongTheLevel()
        {
            Vector3 straightDown = CO_CameraFollow_Script.ClampFocus(new Vector3(0f, 1f, -6f), 8f, Aspect, 90f, Min, Max);
            Vector3 pitched = CO_CameraFollow_Script.ClampFocus(new Vector3(0f, 1f, -6f), 8f, Aspect, 70f, Min, Max);

            Assert.That(pitched.z, Is.GreaterThan(straightDown.z));
        }

        [Test]
        public void ALevelSmallerThanTheViewIsCentred()
        {
            // Size 21 shows about 75 x 42 m, more than the 30 x 44 m level across.
            Vector3 clamped = CO_CameraFollow_Script.ClampFocus(new Vector3(9f, 1f, 30f), 21f, Aspect, Pitch, Min, Max);

            Assert.That(clamped.x, Is.EqualTo(0f).Within(0.001f));
        }
    }
}
