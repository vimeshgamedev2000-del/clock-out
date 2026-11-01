using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace ClockOut.Tests
{
    /// <summary>
    /// Hold E on CO_PlayerInteractor_Script: a hold object is used once the key has
    /// been held for its HoldDuration, and any break before that cancels it.
    /// Input comes from a virtual keyboard, so a real key press on the machine
    /// running the tests cannot get in.
    /// </summary>
    public class CO_HoldInteractionTests_Script : InputTestFixture
    {
        private sealed class FakeHoldObject : MonoBehaviour, CO_IHoldInteractable_Script
        {
            public bool IsUsable = true;
            public float Duration = 0.2f;
            public int InteractCount;

            public string InteractionPrompt => "Turn on radio";
            public bool CanInteract => IsUsable;
            public float HoldDuration => Duration;

            public void Interact(GameObject interactor) => InteractCount++;
        }

        private sealed class FakePressObject : MonoBehaviour, CO_IInteractable_Script
        {
            public int InteractCount;

            public string InteractionPrompt => "Open door";
            public bool CanInteract => true;

            public void Interact(GameObject interactor) => InteractCount++;
        }

        private readonly List<GameObject> spawned = new List<GameObject>();
        private Keyboard keyboard;
        private CO_PlayerInteractor_Script interactor;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();

            GameObject player = Spawn("TestPlayer", Vector3.zero);
            interactor = player.AddComponent<CO_PlayerInteractor_Script>();
        }

        public override void TearDown()
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                {
                    Object.DestroyImmediate(spawned[i]);
                }
            }

            spawned.Clear();
            base.TearDown();
        }

        private GameObject Spawn(string name, Vector3 position)
        {
            GameObject spawnedObject = new GameObject(name);
            spawnedObject.transform.position = position;
            spawned.Add(spawnedObject);
            return spawnedObject;
        }

        private T SpawnInteractable<T>(Vector3 position) where T : MonoBehaviour
        {
            GameObject prop = Spawn(typeof(T).Name, position);
            prop.AddComponent<SphereCollider>().isTrigger = true;
            return prop.AddComponent<T>();
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator HoldingForTheFullTimeUsesItOnce()
        {
            FakeHoldObject radio = SpawnInteractable<FakeHoldObject>(new Vector3(1f, 0f, 0f));
            yield return null;

            Press(keyboard.eKey);
            yield return Frames(2);

            Assert.That(radio.InteractCount, Is.Zero, "nothing happens on the press itself");
            Assert.That(interactor.IsHolding, Is.True);

            yield return WaitSeconds(radio.Duration + 0.1f);
            Assert.That(radio.InteractCount, Is.EqualTo(1), "a full hold uses it");
            Assert.That(interactor.IsHolding, Is.False);

            yield return WaitSeconds(radio.Duration + 0.1f);
            Assert.That(radio.InteractCount, Is.EqualTo(1), "keeping the key down does not use it again");

            Release(keyboard.eKey);
            yield return null;
            Press(keyboard.eKey);
            yield return WaitSeconds(radio.Duration + 0.1f);
            Assert.That(radio.InteractCount, Is.EqualTo(2), "a fresh press and hold uses it again");
        }

        [UnityTest]
        public IEnumerator ProgressFillsWhileHeld()
        {
            FakeHoldObject radio = SpawnInteractable<FakeHoldObject>(new Vector3(1f, 0f, 0f));
            radio.Duration = 1f;
            yield return null;

            Press(keyboard.eKey);
            yield return WaitSeconds(0.4f);

            Assert.That(interactor.HoldProgress, Is.GreaterThan(0.2f).And.LessThan(1f));
        }

        [UnityTest]
        public IEnumerator LettingGoEarlyDoesNothing()
        {
            FakeHoldObject radio = SpawnInteractable<FakeHoldObject>(new Vector3(1f, 0f, 0f));
            radio.Duration = 1f;
            yield return null;

            Press(keyboard.eKey);
            yield return WaitSeconds(0.3f);
            Release(keyboard.eKey);
            yield return WaitSeconds(1f);

            Assert.That(radio.InteractCount, Is.Zero);
            Assert.That(interactor.IsHolding, Is.False);
            Assert.That(interactor.HoldProgress, Is.Zero, "the bar empties");
        }

        [UnityTest]
        public IEnumerator WalkingOutOfReachCancelsTheHold()
        {
            FakeHoldObject radio = SpawnInteractable<FakeHoldObject>(new Vector3(1f, 0f, 0f));
            radio.Duration = 0.5f;
            yield return null;

            Press(keyboard.eKey);
            yield return Frames(3);

            interactor.transform.position = new Vector3(20f, 0f, 0f);
            yield return WaitSeconds(0.7f);

            Assert.That(radio.InteractCount, Is.Zero);
            Assert.That(interactor.IsHolding, Is.False);
        }

        [UnityTest]
        public IEnumerator AnObjectThatStopsBeingUsableCancelsTheHold()
        {
            FakeHoldObject radio = SpawnInteractable<FakeHoldObject>(new Vector3(1f, 0f, 0f));
            radio.Duration = 0.5f;
            yield return null;

            Press(keyboard.eKey);
            yield return Frames(3);

            radio.IsUsable = false;
            yield return WaitSeconds(0.7f);

            Assert.That(radio.InteractCount, Is.Zero);
            Assert.That(interactor.IsHolding, Is.False);
        }

        [UnityTest]
        public IEnumerator ANearerObjectDoesNotStealAHoldInProgress()
        {
            FakeHoldObject shredder = SpawnInteractable<FakeHoldObject>(new Vector3(2f, 0f, 0f));
            shredder.Duration = 0.5f;
            yield return null;

            Press(keyboard.eKey);
            yield return Frames(3);

            FakePressObject door = SpawnInteractable<FakePressObject>(new Vector3(0.5f, 0f, 0f));
            yield return Frames(3);

            Assert.That(interactor.CurrentTarget, Is.SameAs(shredder));
            Assert.That(door.InteractCount, Is.Zero);

            yield return WaitSeconds(0.6f);
            Assert.That(shredder.InteractCount, Is.EqualTo(1), "the hold it started still completes");
        }

        [UnityTest]
        public IEnumerator TheInternTakesOneFolderPerHoldUpToThree()
        {
            // The desk takes folders off the tagged player's stack.
            GameObject player = interactor.gameObject;
            player.SetActive(false);
            player.tag = "Player";
            CO_WorkStack_Script stack = CO_DistractionTestScene_Script.AddWorkStack(player);
            player.SetActive(true);
            stack.TryHandWork(5, player);

            CO_InternDesk_Script desk = SpawnInteractable<CO_InternDesk_Script>(new Vector3(1f, 0f, 0f));
            CO_DistractionTestScene_Script.SetField(desk, "holdDuration", 0.1f);
            yield return null;

            for (int i = 0; i < 4; i++)
            {
                Press(keyboard.eKey);
                yield return WaitSeconds(0.25f);
                Release(keyboard.eKey);
                yield return Frames(2);
            }

            Assert.That(desk.FoldersTaken, Is.EqualTo(3), "one per hold, no more than the capacity");
            Assert.That(stack.Count, Is.EqualTo(2), "each folder the intern took came off the player's stack");
            Assert.That(desk.CanInteract, Is.False, "a full tray is no longer usable");
            Assert.That(interactor.CurrentTarget, Is.Null, "and stops being offered");
        }

        [UnityTest]
        public IEnumerator APressObjectIsUsedOnThePressOnlyOnce()
        {
            FakePressObject door = SpawnInteractable<FakePressObject>(new Vector3(1f, 0f, 0f));
            yield return null;

            Press(keyboard.eKey);
            yield return Frames(2);
            Assert.That(door.InteractCount, Is.EqualTo(1), "used on the press, no hold needed");

            yield return Frames(10);
            Assert.That(door.InteractCount, Is.EqualTo(1));
            Assert.That(interactor.IsHolding, Is.False);
        }
    }
}
