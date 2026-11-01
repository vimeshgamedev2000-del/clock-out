using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ClockOut.Tests
{
    /// <summary>
    /// No work leaves the building (GDD §5.6): the exit opens only with every item
    /// collected and an empty stack, and a catch at the door shuts it again.
    /// </summary>
    public class CO_ExitDoorTests_Script
    {
        private CO_DistractionTestScene_Script scene;
        private GameObject player;
        private CO_WorkStack_Script stack;
        private CO_ObjectiveTracker_Script tracker;
        private CO_MainDoor_Script door;
        private Vector3 doorClosedPosition;

        [SetUp]
        public void SetUp()
        {
            scene = new CO_DistractionTestScene_Script();
            scene.BuildFloor();

            // The door finds the player by tag in Awake, so the player comes first.
            // Standing 1 m away, well inside the door's 3 m open distance.
            player = scene.SpawnPlayer(new Vector3(0f, 0f, -40f));
            stack = player.GetComponent<CO_WorkStack_Script>();
            CO_DistractionTestScene_Script.SetTuning(stack, "graceDuration", 0f);

            // One item needed, set before Awake so it doesn't count the (empty) scene.
            GameObject trackerObject = scene.SpawnBlock(new Vector3(30f, 0f, 30f));
            trackerObject.SetActive(false);
            tracker = trackerObject.AddComponent<CO_ObjectiveTracker_Script>();
            CO_DistractionTestScene_Script.SetField(tracker, "requiredCount", 1);
            trackerObject.SetActive(true);

            GameObject doorObject = scene.SpawnBlock(new Vector3(0f, 0.5f, -39f));
            door = doorObject.AddComponent<CO_MainDoor_Script>();
            doorClosedPosition = doorObject.transform.localPosition;
        }

        [TearDown]
        public void TearDown()
        {
            scene.TearDown();
        }

        [UnityTest]
        public IEnumerator TheDoorStaysShutWhileThePlayerCarriesWork()
        {
            yield return null;

            stack.TryHandWork(2, player);
            tracker.CollectPowerSource(null);

            Assert.That(door.HasAllItems, Is.True);
            Assert.That(door.IsUnlocked, Is.False, "items alone don't open it while folders are carried");
            Assert.That(door.IsBlockedByWork, Is.True);

            yield return CO_DistractionTestScene_Script.Wait(0.5f);
            Assert.That(door.transform.localPosition, Is.EqualTo(doorClosedPosition),
                "a blocked door doesn't open for a player standing in front of it");

            stack.TryTakeFolder();
            Assert.That(door.IsUnlocked, Is.False, "one folder is still work");

            stack.TryTakeFolder();
            Assert.That(door.IsUnlocked, Is.True, "the last folder gone unlocks it");
            Assert.That(door.IsBlockedByWork, Is.False);

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => door.transform.localPosition.y > doorClosedPosition.y + 0.5f,
                "the unlocked door opening for the player in front of it");
        }

        [UnityTest]
        public IEnumerator ACatchAtTheDoorLocksItAgain()
        {
            yield return null;

            tracker.CollectPowerSource(null);
            Assert.That(door.IsUnlocked, Is.True, "all items and an empty stack unlock it");

            stack.TryHandWork(1, player);
            Assert.That(door.IsUnlocked, Is.False, "a catch at the door shuts it again");
            Assert.That(door.IsBlockedByWork, Is.True);

            stack.TryReplyAll();
            Assert.That(door.IsUnlocked, Is.True, "a reply-all clears the way out");
        }

        [UnityTest]
        public IEnumerator AnEmptyStackStillNeedsEveryItem()
        {
            yield return null;

            Assert.That(stack.Count, Is.EqualTo(0));
            Assert.That(door.IsUnlocked, Is.False, "no items, no exit");
            Assert.That(door.IsBlockedByWork, Is.False, "it's the items missing, not the work");
        }
    }
}
