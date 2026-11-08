using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ClockOut.Tests
{
    /// <summary>
    /// Closing the loop (GDD §3, §5.5, §8): the floor clock, the timesheet title
    /// rules, the elevator exit that ends the floor, and the "tasks dodged" count.
    /// </summary>
    public class CO_TimesheetTests_Script
    {
        private const float FivePm = 17f * 3600f;

        private CO_DistractionTestScene_Script scene;
        private CO_LevelFlow_Script levelFlow;

        [SetUp]
        public void SetUp()
        {
            scene = new CO_DistractionTestScene_Script();
            scene.BuildFloor();
        }

        [TearDown]
        public void TearDown()
        {
            if (levelFlow != null)
            {
                PlayerPrefs.DeleteKey(levelFlow.BestKey);
                PlayerPrefs.DeleteKey(levelFlow.BestKey + ".Title");
            }

            Time.timeScale = 1f;
            scene.TearDown();
        }

        /// <summary>
        /// Clock tuning made from the field's type: the type lives in ClockOut.Data,
        /// which this test assembly doesn't reference.
        /// </summary>
        private static ScriptableObject MakeClockTuning()
        {
            FieldInfo tuningField = typeof(CO_GameClock_Script).GetField(
                "tuning", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tuningField, Is.Not.Null, "CO_GameClock_Script has no field 'tuning' - the test needs updating.");
            return ScriptableObject.CreateInstance(tuningField.FieldType);
        }

        private static string Title(int catches, float clockOutSeconds, ScriptableObject tuning)
        {
            MethodInfo choose = typeof(CO_LevelFlow_Script).GetMethod("ChooseTitle");
            return (string)choose.Invoke(null, new object[] { catches, clockOutSeconds, tuning });
        }

        private static float At(int hour, int minute)
        {
            return hour * 3600f + minute * 60f;
        }

        private CO_GameClock_Script SpawnClock()
        {
            GameObject clockObject = scene.SpawnBlock(new Vector3(30f, 0f, -30f));
            clockObject.SetActive(false);
            CO_GameClock_Script clock = clockObject.AddComponent<CO_GameClock_Script>();
            CO_DistractionTestScene_Script.SetField(clock, "tuning", MakeClockTuning());
            clockObject.SetActive(true);
            return clock;
        }

        [UnityTest]
        public IEnumerator TheClockStartsAtFiveAndRunsTenTimesFaster()
        {
            CO_GameClock_Script clock = SpawnClock();
            Assert.That(clock.GameSeconds, Is.EqualTo(FivePm), "floors start at 5:00 PM");

            float realStart = Time.time;
            yield return CO_DistractionTestScene_Script.Wait(1.5f);
            float elapsed = Time.time - realStart;

            Assert.That(clock.GameSeconds - FivePm, Is.EqualTo(elapsed * 10f).Within(0.5f),
                "1 real second = 10 game seconds");

            clock.Stop();
            float stoppedAt = clock.GameSeconds;
            yield return CO_DistractionTestScene_Script.Wait(0.3f);
            Assert.That(clock.GameSeconds, Is.EqualTo(stoppedAt), "a stopped clock stays put");
        }

        [Test]
        public void TheClockReadsLikeATaskbar()
        {
            Assert.That(CO_GameClock_Script.Format(At(17, 7)), Is.EqualTo("5:07 PM"));
            Assert.That(CO_GameClock_Script.Format(At(17, 7) + 59f), Is.EqualTo("5:07 PM"), "seconds don't round up");
            Assert.That(CO_GameClock_Script.Format(At(12, 0)), Is.EqualTo("12:00 PM"));
            Assert.That(CO_GameClock_Script.Format(At(0, 5)), Is.EqualTo("12:05 AM"));
            Assert.That(CO_GameClock_Script.Format(At(18, 30)), Is.EqualTo("6:30 PM"));
        }

        [Test]
        public void TitlesFollowTheRulesTopToBottom()
        {
            ScriptableObject tuning = MakeClockTuning();

            Assert.That(Title(0, At(18, 30), tuning), Is.EqualTo("PROFESSIONAL GHOST"), "never caught beats everything");
            Assert.That(Title(1, At(17, 10), tuning), Is.EqualTo("QUIET QUITTER"), "left before 5:15");
            Assert.That(Title(4, At(17, 10), tuning), Is.EqualTo("QUIET QUITTER"), "checked before Team Player");
            Assert.That(Title(3, At(17, 30), tuning), Is.EqualTo("TEAM PLAYER"), "caught 3+ times");
            Assert.That(Title(3, At(18, 30), tuning), Is.EqualTo("TEAM PLAYER"), "checked before Overtime Legend");
            Assert.That(Title(2, At(18, 1), tuning), Is.EqualTo("OVERTIME LEGEND"), "left after 6:00");
            Assert.That(Title(2, At(17, 40), tuning), Is.EqualTo("NINE-TO-FIVER"), "anything else");
            Assert.That(Title(1, At(17, 15), tuning), Is.EqualTo("NINE-TO-FIVER"), "5:15 itself isn't before 5:15");
        }

        [UnityTest]
        public IEnumerator SteppingIntoTheCabClocksOutOnlyOnceTheDoorIsUnlocked()
        {
            // The door, the exit and the level flow find the player by tag, so it comes first.
            GameObject player = scene.SpawnPlayer(new Vector3(0f, 0f, -40f));
            CO_WorkStack_Script stack = player.GetComponent<CO_WorkStack_Script>();
            CO_DistractionTestScene_Script.SetTuning(stack, "graceDuration", 0f);

            GameObject trackerObject = scene.SpawnBlock(new Vector3(30f, 0f, 30f));
            trackerObject.SetActive(false);
            CO_ObjectiveTracker_Script tracker = trackerObject.AddComponent<CO_ObjectiveTracker_Script>();
            CO_DistractionTestScene_Script.SetField(tracker, "requiredCount", 1);
            trackerObject.SetActive(true);

            GameObject doorObject = scene.SpawnBlock(new Vector3(0f, 0.5f, -39f));
            CO_MainDoor_Script door = doorObject.AddComponent<CO_MainDoor_Script>();

            CO_GameClock_Script clock = SpawnClock();

            GameObject flowObject = scene.SpawnBlock(new Vector3(-30f, 0f, -30f));
            flowObject.SetActive(false);
            levelFlow = flowObject.AddComponent<CO_LevelFlow_Script>();
            CO_DistractionTestScene_Script.SetField(levelFlow, "stack", stack);
            CO_DistractionTestScene_Script.SetField(levelFlow, "clock", clock);
            CO_DistractionTestScene_Script.SetField(levelFlow, "endingOverlay", flowObject.AddComponent<CanvasGroup>());
            flowObject.SetActive(true);

            // The cab: a trigger box 2 m behind the door.
            GameObject cab = new GameObject("TestCab");
            cab.transform.position = new Vector3(0f, 1f, -37f);
            scene.Track(cab);
            cab.SetActive(false);
            BoxCollider zone = cab.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(2f, 2f, 2f);
            CO_LevelExit_Script exit = cab.AddComponent<CO_LevelExit_Script>();
            CO_DistractionTestScene_Script.SetField(exit, "door", door);
            CO_DistractionTestScene_Script.SetField(exit, "levelFlow", levelFlow);
            cab.SetActive(true);

            yield return null;

            // Items in, but carrying work: standing in the cab does nothing.
            stack.TryHandWork(2, player);
            tracker.CollectPowerSource(null);
            player.transform.position = new Vector3(0f, 0f, -37f);
            yield return CO_DistractionTestScene_Script.Wait(0.3f);
            Assert.That(levelFlow.HasEnded, Is.False, "no clocking out with work on your head");
            Assert.That(clock.IsRunning, Is.True);

            // The last folder goes: the door unlocks and the player is already in the cab.
            stack.TryTakeFolder();
            stack.TryTakeFolder();
            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => levelFlow.HasClockedOut, "the player in the cab to clock out", 5f);

            Assert.That(Time.timeScale, Is.EqualTo(0f), "the floor stops");
            Assert.That(clock.IsRunning, Is.False, "the clock stops at the clock-out time");

            CO_LevelFlow_Script.Timesheet sheet = levelFlow.LastTimesheet;
            Assert.That(sheet.ClockOutSeconds, Is.EqualTo(clock.GameSeconds));
            Assert.That(sheet.TasksTaken, Is.EqualTo(1), "one catch, though it was 2 folders");
            Assert.That(sheet.RepliedAll, Is.False);
            Assert.That(sheet.Title, Is.EqualTo("QUIET QUITTER"), "caught once and out within seconds of 5:00");
            Assert.That(sheet.IsNewBest, Is.True, "the first clock-out is the best so far");

            // Walking back out and in again doesn't clock out twice.
            levelFlow.ClockOut();
            Assert.That(levelFlow.LastTimesheet.ClockOutSeconds, Is.EqualTo(sheet.ClockOutSeconds));
        }

        [UnityTest]
        public IEnumerator ALostChaseCountsAsDodgedButACaughtOneDoesNot()
        {
            Time.timeScale = 4f;

            // In the guard's cone, 4 m ahead.
            GameObject player = scene.SpawnPlayer(new Vector3(0f, 0f, 4f));
            CO_WorkStack_Script stack = player.GetComponent<CO_WorkStack_Script>();
            GameObject enemy = scene.SpawnEnemy("Enemy", Vector3.zero);
            CO_DistractionTestScene_Script.SetGuardTuning(enemy, "noticeTime", 0f);
            CO_EnemyAI_Script ai = enemy.GetComponent<CO_EnemyAI_Script>();

            GameObject statsObject = scene.SpawnBlock(new Vector3(30f, 0f, 0f));
            CO_RunStats_Script stats = statsObject.AddComponent<CO_RunStats_Script>();

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => ai.CurrentState == EnemyState.Chasing, "the guard to start chasing", 10f);

            // Out of sight in an instant: the chase ends with nothing handed over.
            player.transform.position = new Vector3(0f, 0f, -30f);
            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => ai.CurrentState != EnemyState.Chasing && ai.CurrentState != EnemyState.Attacking,
                "the guard to lose the player", 10f);
            yield return null;
            Assert.That(stats.TasksDodged, Is.EqualTo(1), "a lost chase is a task dodged");

            // Back in front of it, close enough to be caught.
            player.transform.position = enemy.transform.position + enemy.transform.forward * 1.5f;
            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => stack.Count > 0, "the guard to catch the player", 10f);
            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => ai.CurrentState != EnemyState.Chasing && ai.CurrentState != EnemyState.Attacking,
                "the guard to walk off after the catch", 10f);
            yield return null;

            Assert.That(stats.TasksDodged, Is.EqualTo(1), "a chase that ended in a catch isn't a dodge");
            Assert.That(stack.CatchCount, Is.EqualTo(1));
        }
    }
}
