using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ClockOut.Tests
{
    /// <summary>
    /// The reply-all PC (GDD §5.8): it forwards the whole stack at once, works once
    /// per floor whichever PC is used, isn't offered with an empty stack, and its
    /// noise sends exactly one coworker to the PC while the rest only glance.
    /// </summary>
    public class CO_ReplyAllTests_Script
    {
        private const float TimeScale = 4f;

        private CO_DistractionTestScene_Script scene;
        private GameObject player;
        private CO_WorkStack_Script stack;

        [SetUp]
        public void SetUp()
        {
            scene = new CO_DistractionTestScene_Script();
            scene.BuildFloor();
            Time.timeScale = TimeScale;

            // Far from every guard, so nobody catches the player during a test.
            player = scene.SpawnPlayer(new Vector3(0f, 0f, -40f));
            stack = player.GetComponent<CO_WorkStack_Script>();
            CO_DistractionTestScene_Script.SetTuning(stack, "graceDuration", 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            scene.TearDown();
        }

        private CO_ReplyAllPC_Script SpawnPC(Vector3 position)
        {
            return scene.SpawnBlock(position).AddComponent<CO_ReplyAllPC_Script>();
        }

        private static EnemyState StateOf(GameObject enemy)
        {
            return enemy.GetComponent<CO_EnemyAI_Script>().CurrentState;
        }

        [UnityTest]
        public IEnumerator AReplyAllForwardsTheWholeStackOncePerFloor()
        {
            CO_ReplyAllPC_Script first = SpawnPC(new Vector3(-30f, 0f, -38f));
            CO_ReplyAllPC_Script second = SpawnPC(new Vector3(30f, 0f, -38f));
            yield return null;

            stack.TryHandWork(7, player);
            Assert.That(first.CanInteract, Is.True, "a lit PC is offered while the player carries work");

            first.Interact(player);

            Assert.That(stack.Count, Is.EqualTo(0), "the whole stack goes at once");
            Assert.That(first.FoldersForwarded, Is.EqualTo(7));
            Assert.That(stack.HasRepliedAll, Is.True, "the stack remembers it for the timesheet");

            stack.TryHandWork(2, player);
            yield return null;

            Assert.That(first.CanInteract, Is.False, "the same PC can't be used twice");
            Assert.That(second.CanInteract, Is.False, "nor can another PC on the floor");
            Assert.That(first.IsLit, Is.False, "the used PC goes dark");
            Assert.That(second.IsLit, Is.False, "and so does every other PC");

            second.Interact(player);
            Assert.That(stack.Count, Is.EqualTo(2), "a second reply-all forwards nothing");
            Assert.That(second.FoldersForwarded, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator AReplyAllIsNotOfferedWithAnEmptyStack()
        {
            CO_ReplyAllPC_Script pc = SpawnPC(new Vector3(0f, 0f, -38f));
            yield return null;

            Assert.That(pc.CanInteract, Is.False, "nothing to forward, so no prompt");
            Assert.That(pc.IsLit, Is.True, "it stays lit: the floor's reply-all is still unused");

            pc.Interact(player);
            Assert.That(stack.HasRepliedAll, Is.False, "an empty reply-all doesn't use up the floor's one");
        }

        [UnityTest]
        public IEnumerator AReplyAllSendsExactlyOneEnemyToThePC()
        {
            CO_ReplyAllPC_Script pc = SpawnPC(new Vector3(0f, 0f, 6f));
            stack.TryHandWork(5, player);

            GameObject near = scene.SpawnEnemy("Near", Vector3.zero);
            GameObject far = scene.SpawnEnemy("Far", new Vector3(8f, 0f, 0f));
            CO_DistractionTestScene_Script.SetField(far.GetComponent<CO_EnemyHearing_Script>(), "standDownDelay", 0.4f);
            yield return null;

            // What the interactor calls on the E press.
            pc.Interact(player);
            Assert.That(stack.Count, Is.EqualTo(0));

            bool farInvestigated = false;
            bool farGlanced = false;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (Vector3.Distance(near.transform.position, pc.transform.position) > 3f)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "the nearest enemy to reach the PC");
                farInvestigated |= StateOf(far) == EnemyState.Investigating;
                farGlanced |= StateOf(far) == EnemyState.Alerted;
                yield return null;
            }

            Assert.That(farInvestigated, Is.False, "only one enemy goes");
            Assert.That(farGlanced, Is.True, "the other one heard it and glanced up");
        }
    }
}
