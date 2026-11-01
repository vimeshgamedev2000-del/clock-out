using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ClockOut.Tests
{
    /// <summary>
    /// The work stack (GDD §5.7): a catch hands over folders and sends the coworker
    /// off, the grace stops a second catch straight after, the stack slows the
    /// player, gets loud from 4 folders (one investigator), drops folders on wall
    /// bumps from 8 and ends the run at 10. Time runs fast; every wait has a
    /// real-time deadline.
    /// </summary>
    public class CO_WorkStackTests_Script
    {
        private const float TimeScale = 4f;

        private CO_DistractionTestScene_Script scene;

        [SetUp]
        public void SetUp()
        {
            scene = new CO_DistractionTestScene_Script();
            scene.BuildFloor();
            Time.timeScale = TimeScale;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            scene.TearDown();
        }

        private GameObject SpawnInstantEnemy(string name, Vector3 position, float yaw = 0f)
        {
            GameObject enemy = scene.SpawnEnemy(name, position, yaw);
            CO_DistractionTestScene_Script.SetField(
                enemy.GetComponent<CO_EnemyDetection_Script>(), "noticeTime", 0f);
            return enemy;
        }

        private static EnemyState StateOf(GameObject enemy)
        {
            return enemy.GetComponent<CO_EnemyAI_Script>().CurrentState;
        }

        [UnityTest]
        public IEnumerator ACatchHandsOverFoldersAndTheCoworkerWalksOff()
        {
            GameObject player = scene.SpawnPlayer(new Vector3(0f, 0f, 1.5f));
            CO_WorkStack_Script stack = player.GetComponent<CO_WorkStack_Script>();
            GameObject enemy = SpawnInstantEnemy("Enemy", Vector3.zero);
            CO_EnemyAI_Script ai = enemy.GetComponent<CO_EnemyAI_Script>();
            CO_EnemyDetection_Script detection = enemy.GetComponent<CO_EnemyDetection_Script>();

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => stack.Count > 0, "the coworker to hand over work", 10f);

            Assert.That(stack.Count, Is.EqualTo(1), "one folder per catch by default");
            Assert.That(stack.FoldersReceived, Is.EqualTo(1));
            Assert.That(ai.IsJustDelegated, Is.True, "the coworker starts its cooldown");

            // The hand-off happens while attacking; the brain picks the next state next frame.
            yield return null;

            // The player stays right in front of it, yet it leaves them alone.
            float until = Time.time + 3f;
            while (Time.time < until)
            {
                EnemyState state = StateOf(enemy);
                Assert.That(state, Is.Not.EqualTo(EnemyState.Chasing).And.Not.EqualTo(EnemyState.Attacking),
                    "a coworker who just delegated ignores the player");
                Assert.That(detection.IsIgnoringPlayer, Is.True, "its sight is told to ignore the player");
                Assert.That(detection.NoticeLevel, Is.EqualTo(0f),
                    "the meter stays empty, so no yellow band, red cone or \"!\"");
                yield return null;
            }

            Assert.That(stack.Count, Is.EqualTo(1), "no second catch from the same coworker");
        }

        [UnityTest]
        public IEnumerator TheGraceAfterACatchStopsASecondCoworker()
        {
            GameObject player = scene.SpawnPlayer(new Vector3(0f, 0f, 1.5f));
            CO_WorkStack_Script stack = player.GetComponent<CO_WorkStack_Script>();
            // Both in reach, facing the player from either side.
            SpawnInstantEnemy("First", Vector3.zero);
            SpawnInstantEnemy("Second", new Vector3(0f, 0f, 3f), 180f);

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => stack.Count > 0, "the first catch", 10f);
            float firstCatch = Time.time;

            Assert.That(stack.Count, Is.EqualTo(1), "both are in reach, but only one catch lands");

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => stack.Count > 1, "the second coworker to catch the player after the grace", 10f);

            Assert.That(Time.time - firstCatch, Is.GreaterThanOrEqualTo(2f - 0.05f),
                "nobody can catch the player during the 2 s grace");
            Assert.That(stack.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator EachFolderSlowsThePlayerUpToTheCap()
        {
            CO_WorkStack_Script stack = scene.SpawnPlayer(new Vector3(0f, 0f, -40f)).GetComponent<CO_WorkStack_Script>();
            CO_DistractionTestScene_Script.SetTuning(stack, "graceDuration", 0f);
            yield return null;

            Assert.That(stack.SpeedMultiplier, Is.EqualTo(1f).Within(0.001f));

            stack.TryHandWork(3, stack.gameObject);
            Assert.That(stack.SpeedMultiplier, Is.EqualTo(0.85f).Within(0.001f), "5% per folder");

            stack.TryHandWork(6, stack.gameObject);
            Assert.That(stack.SpeedMultiplier, Is.EqualTo(0.6f).Within(0.001f), "never slower than -40%");
        }

        [UnityTest]
        public IEnumerator FootstepsFromFourFoldersSendExactlyOneCoworker()
        {
            // Two blind coworkers, so only hearing can reach them: a patrol that turned
            // and caught the player would hand over a folder and spoil the count.
            GameObject near = scene.SpawnEnemy("Near", new Vector3(0f, 0f, 0f), 180f);
            GameObject far = scene.SpawnEnemy("Far", new Vector3(2f, 0f, 0f), 180f);
            CO_DistractionTestScene_Script.SetField(near.GetComponent<CO_EnemyDetection_Script>(), "detectionRadius", 0f);
            CO_DistractionTestScene_Script.SetField(far.GetComponent<CO_EnemyDetection_Script>(), "detectionRadius", 0f);
            CO_DistractionTestScene_Script.SetField(far.GetComponent<CO_EnemyHearing_Script>(), "standDownDelay", 0.4f);

            GameObject player = scene.SpawnPlayer(new Vector3(0f, 0f, 2.5f));
            CO_WorkStack_Script stack = player.GetComponent<CO_WorkStack_Script>();
            CO_DistractionTestScene_Script.SetTuning(stack, "graceDuration", 0f);

            // Three folders: walking is still silent.
            stack.TryHandWork(3, player);
            yield return Pace(player, 3f);
            Assert.That(near.GetComponent<CO_EnemyHearing_Script>().HasNoise, Is.False, "3 folders make no noise");
            Assert.That(far.GetComponent<CO_EnemyHearing_Script>().HasNoise, Is.False, "3 folders make no noise");

            // Four: every step is a 4 m noise, and one coworker comes to look. Watched
            // until that coworker stops investigating (it finds the player and chases),
            // since a chaser can't be sent and the next step may then send the other.
            stack.TryHandWork(1, player);
            bool nearInvestigated = false;
            bool farInvestigated = false;
            float until = Time.time + 6f;
            while (Time.time < until)
            {
                yield return Pace(player, 0.1f);
                bool nearInvestigating = StateOf(near) == EnemyState.Investigating;
                if (nearInvestigated && !nearInvestigating)
                {
                    break;
                }

                nearInvestigated |= nearInvestigating;
                farInvestigated |= StateOf(far) == EnemyState.Investigating;
            }

            Assert.That(nearInvestigated, Is.True, "the nearest coworker comes to check the footsteps");
            Assert.That(farInvestigated, Is.False, "and only that one, step after step");
        }

        /// <summary>Walks the player back and forth along X for a while, a little each frame.</summary>
        private static IEnumerator Pace(GameObject player, float seconds)
        {
            float until = Time.time + seconds;
            float direction = 1f;
            while (Time.time < until)
            {
                Vector3 position = player.transform.position + Vector3.right * (direction * 2f * Time.deltaTime);
                if (Mathf.Abs(position.x) > 1f)
                {
                    direction = -direction;
                }

                player.transform.position = position;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AWallBumpFromEightFoldersDropsTheTopOneWithANoise()
        {
            GameObject listenerEnemy = scene.SpawnEnemy("Listener", new Vector3(-4f, 0f, 0f), 180f);
            GameObject player = scene.SpawnPlayer(new Vector3(0f, 1f, 0f), withBody: true);
            CO_WorkStack_Script stack = player.GetComponent<CO_WorkStack_Script>();
            CO_DistractionTestScene_Script.SetTuning(stack, "graceDuration", 0f);
            CO_DistractionTestScene_Script.SetField(stack, "wallMask", (LayerMask)(1 << 0));

            // Silent steps, so the only noise the listener can hear is the drop.
            CO_DistractionTestScene_Script.SetTuning(stack, "footstepFolders", 20);

            GameObject wall = scene.SpawnBlock(new Vector3(1.5f, 1f, 0f));
            wall.transform.localScale = new Vector3(1f, 2f, 4f);

            CharacterController controller = player.GetComponent<CharacterController>();

            // Seven folders: bumping the wall does nothing.
            stack.TryHandWork(7, player);
            yield return Push(controller, 0.5f);
            Assert.That(stack.Count, Is.EqualTo(7), "below 8 the stack holds");

            // Eight: the bump knocks the top one off, once per cooldown.
            stack.TryHandWork(1, player);
            yield return Push(controller, 0.5f);
            Assert.That(stack.Count, Is.EqualTo(7), "one folder drops, not one per frame against the wall");
            Assert.That(listenerEnemy.GetComponent<CO_EnemyHearing_Script>().HasNoise, Is.True,
                "the drop is a 6 m noise");
        }

        /// <summary>Drives the controller into the wall along +X for a while.</summary>
        private static IEnumerator Push(CharacterController controller, float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                controller.Move(new Vector3(3f, -1f, 0f) * Time.deltaTime);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TenFoldersBuryThePlayer()
        {
            CO_WorkStack_Script stack = scene.SpawnPlayer(new Vector3(0f, 0f, -40f)).GetComponent<CO_WorkStack_Script>();
            CO_DistractionTestScene_Script.SetTuning(stack, "graceDuration", 0f);
            bool buried = false;
            stack.Buried += () => buried = true;
            yield return null;

            stack.TryHandWork(9, stack.gameObject);
            Assert.That(stack.IsBuried, Is.False, "9 is survivable");

            stack.TryHandWork(3, stack.gameObject);
            Assert.That(stack.Count, Is.EqualTo(10), "the stack never goes past the limit");
            Assert.That(stack.IsBuried && buried, Is.True, "10 folders end the run");
            Assert.That(stack.CanBeHandedWork, Is.False, "a buried player takes no more work");
            Assert.That(stack.TryTakeFolder(), Is.False, "and gets rid of none: the run is over");
        }

        [UnityTest]
        public IEnumerator TheShredderCannotStartWithAnEmptyStack()
        {
            GameObject player = scene.SpawnPlayer(new Vector3(0f, 0f, -40f));
            CO_Shredder_Script shredder = scene.SpawnBlock(new Vector3(0f, 0f, -38f)).AddComponent<CO_Shredder_Script>();
            yield return null;

            Assert.That(shredder.CanInteract, Is.False, "nothing to shred");

            player.GetComponent<CO_WorkStack_Script>().TryHandWork(1, player);
            Assert.That(shredder.CanInteract, Is.True, "one folder is enough to start it");
        }
    }
}
