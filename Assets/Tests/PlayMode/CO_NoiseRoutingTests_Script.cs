using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ClockOut.Tests
{
    /// <summary>
    /// Covers who hears a noise and who is sent to look at it. No navigation and
    /// no movement: these are the CO_NoiseManager_Script's routing rules and the state
    /// CO_EnemyHearing_Script holds afterwards.
    /// </summary>
    public class CO_NoiseRoutingTests_Script
    {
        private CO_DistractionTestScene_Script scene;

        /// <summary>A noise source that records being found, standing in for a radio.</summary>
        private sealed class FakeNoiseSource : MonoBehaviour, CO_INoiseSource_Script
        {
            public Transform Transform => transform;

            public int InvestigatedCount { get; private set; }

            public void OnInvestigated(GameObject investigator)
            {
                InvestigatedCount++;
            }
        }

        [SetUp]
        public void SetUp()
        {
            scene = new CO_DistractionTestScene_Script();
        }

        [TearDown]
        public void TearDown()
        {
            // Restored here as well as in the tests that raise it, so a failed
            // assertion cannot leave the rest of the run running fast.
            Time.timeScale = 1f;
            scene.TearDown();
        }

        private FakeNoiseSource SpawnSource(Vector3 position)
        {
            return scene.SpawnNoiseSource<FakeNoiseSource>("Source", position);
        }

        [Test]
        public void EnemyInsideTheRadiusHearsTheNoise()
        {
            CO_EnemyHearing_Script listener = scene.SpawnListener("A", new Vector3(0f, 0f, 9f));

            CO_NoiseManager_Script.Emit(Vector3.zero, 10f);

            Assert.That(listener.HasNoise, Is.True, "an enemy inside the radius should hear it");
            Assert.That(listener.IsInvestigator, Is.True, "the only listener should be the one sent");
        }

        [Test]
        public void EnemyOutsideTheRadiusIsUnaffected()
        {
            CO_EnemyHearing_Script listener = scene.SpawnListener("A", new Vector3(0f, 0f, 11f));

            CO_NoiseManager_Script.Emit(Vector3.zero, 10f);

            Assert.That(listener.HasNoise, Is.False, "the radius is the whole rule - 11m is outside 10m");
        }

        [Test]
        public void ExactlyOneListenerIsSentToInvestigate()
        {
            CO_EnemyHearing_Script a = scene.SpawnListener("A", new Vector3(2f, 0f, 0f));
            CO_EnemyHearing_Script b = scene.SpawnListener("B", new Vector3(4f, 0f, 0f));
            CO_EnemyHearing_Script c = scene.SpawnListener("C", new Vector3(6f, 0f, 0f));

            CO_NoiseManager_Script.Emit(Vector3.zero, 20f);

            int investigators = (a.IsInvestigator ? 1 : 0) + (b.IsInvestigator ? 1 : 0) + (c.IsInvestigator ? 1 : 0);

            Assert.That(investigators, Is.EqualTo(1), "a noise must pull exactly one enemy off its route");
            Assert.That(a.IsInvestigator, Is.True, "and it should be the nearest one");
            Assert.That(b.HasNoise && c.HasNoise, Is.True,
                "the others still hear it - they just glance rather than go");
        }

        [Test]
        public void TheNearestFreeListenerIsChosen()
        {
            CO_EnemyHearing_Script far = scene.SpawnListener("Far", new Vector3(0f, 0f, 12f));
            CO_EnemyHearing_Script near = scene.SpawnListener("Near", new Vector3(0f, 0f, 3f));

            CO_NoiseManager_Script.Emit(Vector3.zero, 20f);

            Assert.That(near.IsInvestigator, Is.True, "the closest enemy should take the job");
            Assert.That(far.IsInvestigator, Is.False);
        }

        [Test]
        public void TheInvestigatorKeepsTheJobWhenTheNoiseRepeats()
        {
            FakeNoiseSource source = SpawnSource(Vector3.zero);
            CO_EnemyHearing_Script first = scene.SpawnListener("First", new Vector3(0f, 0f, 12f));

            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 20f, source));
            Assert.That(first.IsInvestigator, Is.True, "with nobody else about, the first listener goes");

            // A closer guard wanders in while the first is still on its way. The
            // radio pulses again - the job must not change hands mid-walk.
            CO_EnemyHearing_Script closer = scene.SpawnListener("Closer", new Vector3(0f, 0f, 2f));
            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 20f, source));

            Assert.That(first.IsInvestigator, Is.True, "the enemy already walking over keeps the job");
            Assert.That(closer.IsInvestigator, Is.False, "the closer enemy is only alerted");
        }

        [Test]
        public void AListenerAlreadyInvestigatingIsNotDraggedOntoASecondNoise()
        {
            FakeNoiseSource first = SpawnSource(Vector3.zero);
            FakeNoiseSource second = SpawnSource(new Vector3(0f, 0f, 1f));

            CO_EnemyHearing_Script busy = scene.SpawnListener("Busy", Vector3.zero);
            CO_EnemyHearing_Script free = scene.SpawnListener("Free", new Vector3(0f, 0f, 4f));

            // A quiet noise only the near listener can hear, so it sets off alone.
            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 1f, first));
            Assert.That(busy.IsInvestigating(first), Is.True);
            Assert.That(free.HasNoise, Is.False);

            // A second noise reaches both, and the busy one is still the closer of
            // the two - but it is already on its way somewhere.
            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(new Vector3(0f, 0f, 1f), 20f, second));

            Assert.That(free.IsInvestigating(second), Is.True,
                "a free enemy is preferred over a nearer busy one");
            Assert.That(busy.IsInvestigating(second), Is.False,
                "two enemies must never be sent to the same noise");
            Assert.That(busy.NoiseSource, Is.SameAs(first),
                "and the busy one is still headed for the noise it set off toward");
        }

        [UnityTest]
        public IEnumerator AListenerLeftOnAlertForgetsTheNoiseWhenTheAlertExpires()
        {
            // The only enemy in earshot has eyes on the player, so nobody can be
            // sent - which is the one case that still leaves an enemy standing
            // and looking rather than carrying on.
            CO_EnemyHearing_Script watcher = scene.SpawnListener(
                "Watcher", Vector3.zero, alertDuration: 0.2f, withSight: true);
            CO_EnemyDetection_Script detection = watcher.GetComponent<CO_EnemyDetection_Script>();
            scene.SpawnPlayer(new Vector3(0f, 0f, 3f));

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => detection.HasDetectedPlayer, "the watcher to spot the player", 3f);

            CO_NoiseManager_Script.Emit(Vector3.zero, 20f);
            Assert.That(watcher.HasNoise, Is.True, "it heard the noise even though it cannot go");
            Assert.That(watcher.IsInvestigator, Is.False);

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => !watcher.HasNoise, "the alert to expire", 3f);
        }

        [UnityTest]
        public IEnumerator ALongAlertIsCutShortOnceSomebodyElseTakesTheNoise()
        {
            FakeNoiseSource source = SpawnSource(Vector3.zero);

            // A minute-long alert, so anything that ends it inside the test must
            // be the hand-off rather than the alert simply running out.
            CO_EnemyHearing_Script watcher = scene.SpawnListener(
                "Watcher", Vector3.zero, alertDuration: 60f, withSight: true, standDownDelay: 0.2f);
            CO_EnemyDetection_Script detection = watcher.GetComponent<CO_EnemyDetection_Script>();
            scene.SpawnPlayer(new Vector3(0f, 0f, 3f));

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => detection.HasDetectedPlayer, "the watcher to spot the player", 3f);

            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 20f, source));
            Assert.That(watcher.HasNoise, Is.True, "with nobody free, the watcher is left on alert");

            // A guard who can actually go now hears the same radio.
            CO_EnemyHearing_Script free = scene.SpawnListener("Free", new Vector3(0f, 0f, 8f));
            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 20f, source));

            Assert.That(free.IsInvestigator, Is.True, "the free guard takes the job");
            Assert.That(watcher.HasNoise, Is.True, "the watcher does not snap round instantly");

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => !watcher.HasNoise, "the shortened stand down to run out", 3f);
        }

        [UnityTest]
        public IEnumerator ARepeatingNoiseDoesNotHoldAnEnemyThatIsNotGoingToIt()
        {
            FakeNoiseSource source = SpawnSource(Vector3.zero);

            CO_EnemyHearing_Script investigator = scene.SpawnListener("Investigator", new Vector3(0f, 0f, 2f));
            CO_EnemyHearing_Script bystander = scene.SpawnListener(
                "Bystander", new Vector3(0f, 0f, 8f), standDownDelay: 0.3f);

            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 20f, source));

            Assert.That(investigator.IsInvestigator, Is.True, "the nearer enemy takes the job");
            Assert.That(bystander.HasNoise, Is.True, "and the other one glances that way");

            // Keep the radio pulsing away, the way a real one does, until the
            // bystander gets on with its route.
            float deadline = Time.realtimeSinceStartup + 3f;

            while (bystander.HasNoise)
            {
                CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 20f, source));

                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                    "repeat pulses must not keep extending the bystander's glance");

                yield return null;
            }

            Assert.That(investigator.IsInvestigator, Is.True,
                "meanwhile the one that took it is still on its way");
        }

        [UnityTest]
        public IEnumerator AnEnemyThatHasStoodDownIsNotPulledBackByLaterPulses()
        {
            FakeNoiseSource source = SpawnSource(Vector3.zero);

            CO_EnemyHearing_Script investigator = scene.SpawnListener("Investigator", new Vector3(0f, 0f, 2f));
            CO_EnemyHearing_Script bystander = scene.SpawnListener(
                "Bystander", new Vector3(0f, 0f, 8f), standDownDelay: 0.2f);

            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 20f, source));
            Assert.That(bystander.HasNoise, Is.True, "it glances when it first hears the radio");

            float deadline = Time.realtimeSinceStartup + 3f;

            while (bystander.HasNoise)
            {
                CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 20f, source));

                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                    "the bystander should stand down while the radio is still playing");

                yield return null;
            }

            // The radio plays on while the investigator walks to it. Having
            // decided this sound is not its problem, the bystander should not
            // keep stopping for it every time it pulses.
            for (int i = 0; i < 60; i++)
            {
                CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 20f, source));

                Assert.That(bystander.HasNoise, Is.False,
                    "a guard that has stood down stays on its route while the same noise plays");

                yield return null;
            }

            Assert.That(investigator.IsInvestigator, Is.True);
        }

        [UnityTest]
        public IEnumerator ADismissedSoundCountsAsNewsAgainOnceItHasBeenQuiet()
        {
            // Sped up so the dismissal can be waited out in a fraction of a
            // second of real time.
            Time.timeScale = 8f;

            FakeNoiseSource source = SpawnSource(Vector3.zero);

            scene.SpawnListener("Investigator", new Vector3(0f, 0f, 2f));
            CO_EnemyHearing_Script bystander = scene.SpawnListener(
                "Bystander", new Vector3(0f, 0f, 8f), standDownDelay: 0.2f);

            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 20f, source));

            float deadline = Time.realtimeSinceStartup + 3f;

            while (bystander.HasNoise)
            {
                CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 20f, source));

                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                    "the bystander should stand down");

                yield return null;
            }

            // The radio is switched off for a while, then started again. That is
            // a new disturbance rather than the one already dealt with.
            yield return CO_DistractionTestScene_Script.Wait(4f);

            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(Vector3.zero, 20f, source));

            Assert.That(bystander.HasNoise, Is.True,
                "a sound that has been quiet for a while is worth another look");

            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator AListenerThatCanSeeThePlayerIsNotRecruited()
        {
            // Facing +Z with the player three metres ahead, inside the default cone.
            CO_EnemyHearing_Script watcher = scene.SpawnListener("Watcher", Vector3.zero, withSight: true);
            CO_EnemyDetection_Script detection = watcher.GetComponent<CO_EnemyDetection_Script>();
            scene.SpawnPlayer(new Vector3(0f, 0f, 3f));

            CO_EnemyHearing_Script free = scene.SpawnListener("Free", new Vector3(0f, 0f, 10f));

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => detection.HasDetectedPlayer, "the watcher to spot the player", 3f);

            CO_NoiseManager_Script.Emit(Vector3.zero, 30f);

            Assert.That(watcher.IsInvestigator, Is.False,
                "a guard with eyes on the player does not wander off to check a radio");
            Assert.That(free.IsInvestigator, Is.True, "the free guard takes it instead");
        }

        [UnityTest]
        public IEnumerator NobodyInvestigatesWhenEveryListenerInEarshotSeesThePlayer()
        {
            CO_EnemyHearing_Script watcher = scene.SpawnListener("Watcher", Vector3.zero, withSight: true);
            CO_EnemyDetection_Script detection = watcher.GetComponent<CO_EnemyDetection_Script>();
            scene.SpawnPlayer(new Vector3(0f, 0f, 3f));

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => detection.HasDetectedPlayer, "the watcher to spot the player", 3f);

            CO_NoiseManager_Script.Emit(Vector3.zero, 30f);

            Assert.That(watcher.HasNoise, Is.True, "it still hears the noise");
            Assert.That(watcher.IsInvestigator, Is.False, "but the chase outranks it, so nobody goes");
        }
    }
}
