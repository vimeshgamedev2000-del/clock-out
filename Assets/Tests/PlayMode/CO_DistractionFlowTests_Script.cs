using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ClockOut.Tests
{
    /// <summary>
    /// Drives the whole stack on a baked floor: a radio the player switches on,
    /// an enemy that walks over, looks around and goes back to its route.
    ///
    /// Time runs fast so a four second inspection does not cost four seconds of
    /// test time. Every wait has a real-time deadline, so a broken build fails
    /// with a message rather than hanging the run.
    /// </summary>
    public class CO_DistractionFlowTests_Script
    {
        private const float TimeScale = 4f;

        private CO_DistractionTestScene_Script scene;
        private GameObject player;

        [SetUp]
        public void SetUp()
        {
            scene = new CO_DistractionTestScene_Script();
            scene.BuildFloor();

            // Parked well outside every detection radius, so tests that are about
            // hearing are not cut short by an enemy spotting the player.
            player = scene.SpawnPlayer(new Vector3(0f, 0f, -40f));

            Time.timeScale = TimeScale;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            scene.TearDown();
        }

        private static EnemyState StateOf(GameObject enemy)
        {
            return enemy.GetComponent<CO_EnemyAI_Script>().CurrentState;
        }

        [UnityTest]
        public IEnumerator TheInvestigatorWalksOverLooksAroundAndSwitchesTheRadioOff()
        {
            CO_DistractionDevice_Script radio = scene.SpawnRadio("Radio", new Vector3(0f, 0f, 10f));
            GameObject enemy = scene.SpawnEnemy("Enemy", Vector3.zero);
            CO_EnemyInvestigation_Script investigation = enemy.GetComponent<CO_EnemyInvestigation_Script>();

            radio.Interact(player);

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => StateOf(enemy) == EnemyState.Investigating, "the enemy to set off");

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => investigation.IsInspecting, "the enemy to arrive and start looking around");

            float distance = Vector3.Distance(
                enemy.transform.position, radio.transform.position);

            Assert.That(distance, Is.LessThan(2.5f),
                "the enemy should stop at the radio, not somewhere near it");
            Assert.That(radio.IsActive, Is.True, "the radio plays until the inspection finishes");

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => !radio.IsActive, "the radio to be found and switched off");

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => StateOf(enemy) == EnemyState.Patrolling, "the enemy to go back to its route");

            Assert.That(enemy.GetComponent<CO_EnemyHearing_Script>().HasNoise, Is.False,
                "the noise is dealt with once it has been looked at");
        }

        [UnityTest]
        public IEnumerator OnlyOneEnemyLeavesItsRouteAndTheRestGlanceAndCarryOn()
        {
            CO_DistractionDevice_Script radio = scene.SpawnRadio("Radio", Vector3.zero, noiseRadius: 25f);

            GameObject near = scene.SpawnEnemy("Near", new Vector3(6f, 0f, 0f));
            GameObject middle = scene.SpawnEnemy("Middle", new Vector3(12f, 0f, 0f));
            GameObject far = scene.SpawnEnemy("Far", new Vector3(18f, 0f, 0f));

            CO_DistractionTestScene_Script.SetField(middle.GetComponent<CO_EnemyHearing_Script>(), "standDownDelay", 0.4f);
            CO_DistractionTestScene_Script.SetField(far.GetComponent<CO_EnemyHearing_Script>(), "standDownDelay", 0.4f);

            radio.Interact(player);

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => StateOf(near) == EnemyState.Investigating, "the nearest enemy to take the job");

            Assert.That(StateOf(middle), Is.EqualTo(EnemyState.Alerted),
                "the others look that way rather than setting off");
            Assert.That(StateOf(far), Is.EqualTo(EnemyState.Alerted));

            // The radio is still pulsing throughout, which must not keep them
            // standing there - the glance runs out and they walk on.
            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => StateOf(middle) == EnemyState.Patrolling && StateOf(far) == EnemyState.Patrolling,
                "the bystanders to get on with their routes", 10f);

            Assert.That(StateOf(near), Is.EqualTo(EnemyState.Investigating),
                "and the one that took the job is still going");
        }

        [UnityTest]
        public IEnumerator SeeingThePlayerCancelsAnInvestigationInProgress()
        {
            // A long walk, with a radius wide enough to cover it - the enemy has
            // to still be travelling when the player steps out in front of it.
            CO_DistractionDevice_Script radio = scene.SpawnRadio("Radio", new Vector3(0f, 0f, 20f), noiseRadius: 30f);
            GameObject enemy = scene.SpawnEnemy("Enemy", Vector3.zero);
            CO_EnemyHearing_Script hearing = enemy.GetComponent<CO_EnemyHearing_Script>();

            radio.Interact(player);

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => StateOf(enemy) == EnemyState.Investigating, "the enemy to set off");

            // Step into its path, mid-walk.
            player.transform.position = enemy.transform.position + enemy.transform.forward * 3f;

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => StateOf(enemy) == EnemyState.Chasing || StateOf(enemy) == EnemyState.Attacking,
                "the enemy to break off and chase");

            Assert.That(hearing.HasNoise, Is.False,
                "seeing the player outranks the noise, so it is dropped rather than resumed later");
            Assert.That(radio.IsActive, Is.True, "nobody reached the radio, so it keeps playing");
        }

        [UnityTest]
        public IEnumerator AnInvestigationThatRunsOutOfTimeIsAbandoned()
        {
            CO_DistractionDevice_Script radio = scene.SpawnRadio("Radio", new Vector3(0f, 0f, 25f), noiseRadius: 40f);

            // One pulse only, so the enemy is not immediately re-recruited after
            // giving up and the test can assert on a settled state.
            CO_DistractionTestScene_Script.SetField(radio, "pulseInterval", 1000f);

            GameObject enemy = scene.SpawnEnemy("Enemy", Vector3.zero);
            CO_DistractionTestScene_Script.SetField(enemy.GetComponent<CO_EnemyInvestigation_Script>(), "travelTimeout", 0.3f);

            radio.Interact(player);

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => StateOf(enemy) == EnemyState.Investigating, "the enemy to set off");

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => StateOf(enemy) == EnemyState.Patrolling, "the enemy to give up and resume patrolling");

            Assert.That(radio.IsActive, Is.True, "a radio nobody reached is still playing");
        }

        [UnityTest]
        public IEnumerator ATimedRadioRunsOutAndOnlyEverDisturbsTheOneEnemy()
        {
            CO_DistractionDevice_Script radio = scene.SpawnRadio("Radio", Vector3.zero, noiseRadius: 25f);
            CO_DistractionTestScene_Script.SetField(radio, "playDuration", 0.3f);

            GameObject near = scene.SpawnEnemy("Near", new Vector3(6f, 0f, 0f));
            GameObject far = scene.SpawnEnemy("Far", new Vector3(18f, 0f, 0f));

            CO_DistractionTestScene_Script.SetField(far.GetComponent<CO_EnemyHearing_Script>(), "standDownDelay", 0.4f);

            radio.Interact(player);

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => StateOf(near) == EnemyState.Investigating, "the near enemy to take the job");

            Assert.That(StateOf(far), Is.Not.EqualTo(EnemyState.Investigating),
                "the far enemy heard it but is not the one going");

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => !radio.IsActive, "the radio to run out of play time");

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => StateOf(far) == EnemyState.Patrolling, "the far enemy to resume its route");
        }

        [UnityTest]
        public IEnumerator ARunningRadioCannotBeRestartedAndHonoursItsReuseDelay()
        {
            CO_DistractionDevice_Script radio = scene.SpawnRadio("Radio", Vector3.zero, noiseRadius: 5f);
            CO_DistractionTestScene_Script.SetField(radio, "playDuration", 0.2f);
            CO_DistractionTestScene_Script.SetField(radio, "reuseDelay", 0.5f);

            Assert.That(radio.CanInteract, Is.True, "an idle radio can be used");

            radio.Interact(player);
            Assert.That(radio.IsActive, Is.True);
            Assert.That(radio.CanInteract, Is.False, "it cannot be started twice");

            yield return CO_DistractionTestScene_Script.WaitUntil(() => !radio.IsActive, "the radio to run out");

            Assert.That(radio.CanInteract, Is.False, "the reuse delay has not elapsed yet");

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => radio.CanInteract, "the reuse delay to elapse");
        }

        [UnityTest]
        public IEnumerator ANonReusableRadioIsSpentAfterOneUse()
        {
            CO_DistractionDevice_Script radio = scene.SpawnRadio("Radio", Vector3.zero, noiseRadius: 5f);
            CO_DistractionTestScene_Script.SetField(radio, "playDuration", 0.2f);
            CO_DistractionTestScene_Script.SetField(radio, "reuseDelay", 0f);
            CO_DistractionTestScene_Script.SetField(radio, "reusable", false);

            radio.Interact(player);

            yield return CO_DistractionTestScene_Script.WaitUntil(() => !radio.IsActive, "the radio to run out");
            yield return CO_DistractionTestScene_Script.Wait(0.5f);

            Assert.That(radio.CanInteract, Is.False, "a one-shot distraction stays spent");
        }

        [UnityTest]
        public IEnumerator TheInteractorTargetsTheNearestUsableDevice()
        {
            player.transform.position = Vector3.zero;

            CO_DistractionDevice_Script near = scene.SpawnRadio("Near", new Vector3(0f, 0f, 1f), noiseRadius: 5f);
            CO_DistractionDevice_Script far = scene.SpawnRadio("Far", new Vector3(0f, 0f, 2f), noiseRadius: 5f);
            CO_PlayerInteractor_Script interactor = player.GetComponent<CO_PlayerInteractor_Script>();

            yield return null;
            Assert.That(interactor.CurrentTarget, Is.SameAs(near), "the closer radio is the target");

            // Once it is running it is no longer usable, so the target should fall
            // through to the other one rather than sticking on a dead object.
            near.Interact(player);
            yield return null;
            Assert.That(interactor.CurrentTarget, Is.SameAs(far), "an unusable device is skipped");

            player.transform.position = new Vector3(0f, 0f, -40f);
            yield return null;
            Assert.That(interactor.CurrentTarget, Is.Null, "nothing in reach means no target");
        }

        [UnityTest]
        public IEnumerator TheEnemyStillSeesThePlayerWhenSurroundedByGeometry()
        {
            // Regression: detection used to run through a fixed size overlap
            // buffer, and a crowded corner filled it with walls before the player
            // was ever reached. Two dozen colliders would have hidden them entirely.
            GameObject enemy = scene.SpawnEnemy("Enemy", Vector3.zero);

            for (int i = 0; i < 24; i++)
            {
                float angle = i / 24f * Mathf.PI * 2f;
                scene.SpawnBlock(new Vector3(Mathf.Cos(angle) * 2.2f, 0f, Mathf.Sin(angle) * 2.2f));
            }

            player.transform.position = new Vector3(0f, 0f, 3f);

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => enemy.GetComponent<CO_EnemyDetection_Script>().HasDetectedPlayer,
                "the enemy to spot the player through a crowd of colliders", 5f);

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => StateOf(enemy) == EnemyState.Chasing || StateOf(enemy) == EnemyState.Attacking,
                "the enemy to react to what it saw");
        }

        [UnityTest]
        public IEnumerator AStartedShredderSendsExactlyOneEnemyWhoSwitchesItOff()
        {
            GameObject shredderObject = scene.SpawnBlock(new Vector3(0f, 0f, 6f));
            CO_Shredder_Script shredder = shredderObject.AddComponent<CO_Shredder_Script>();

            // Long enough that only the coworker can stop it.
            CO_DistractionTestScene_Script.SetField(shredder, "runDuration", 60f);

            GameObject near = scene.SpawnEnemy("Near", Vector3.zero);
            GameObject far = scene.SpawnEnemy("Far", new Vector3(4f, 0f, 0f));
            CO_DistractionTestScene_Script.SetField(far.GetComponent<CO_EnemyHearing_Script>(), "standDownDelay", 0.4f);

            // What the interactor calls once E has been held for HoldDuration.
            shredder.Interact(player);
            Assert.That(shredder.IsRunning, Is.True, "it runs without the key held");
            Assert.That(shredder.CanInteract, Is.False, "a running shredder cannot be started again");

            bool farInvestigated = false;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (shredder.IsRunning)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "the coworker to switch it off");
                farInvestigated |= StateOf(far) == EnemyState.Investigating;
                yield return null;
            }

            Assert.That(farInvestigated, Is.False, "only one enemy goes");
            Assert.That(shredder.FoldersShredded, Is.GreaterThan(0), "it shredded while it ran");
            Assert.That(Vector3.Distance(near.transform.position, shredder.transform.position), Is.LessThan(3f),
                "the nearest enemy is the one that came and switched it off");
        }
    }
}
