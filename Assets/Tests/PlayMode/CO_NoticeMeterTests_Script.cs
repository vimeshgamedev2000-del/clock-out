using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ClockOut.Tests
{
    /// <summary>
    /// The notice meter: sight fills it, and only a full meter turns a glimpse
    /// into a chase. Time runs fast; every wait has a real-time deadline.
    /// </summary>
    public class CO_NoticeMeterTests_Script
    {
        private const float TimeScale = 4f;

        // Straight ahead of an enemy at the origin facing +Z, well inside its cone.
        private static readonly Vector3 InView = new Vector3(0f, 0f, 4f);

        // Behind it and far outside every radius.
        private static readonly Vector3 OutOfView = new Vector3(0f, 0f, -40f);

        private CO_DistractionTestScene_Script scene;
        private GameObject player;

        [SetUp]
        public void SetUp()
        {
            scene = new CO_DistractionTestScene_Script();
            scene.BuildFloor();
            player = scene.SpawnPlayer(OutOfView);

            Time.timeScale = TimeScale;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            scene.TearDown();
        }

        private GameObject SpawnEnemyWithNoticeTime(float noticeTime)
        {
            GameObject enemy = scene.SpawnEnemy("Enemy", Vector3.zero);
            CO_DistractionTestScene_Script.SetField(
                enemy.GetComponent<CO_EnemyDetection_Script>(), "noticeTime", noticeTime);
            return enemy;
        }

        private static bool IsReacting(GameObject enemy)
        {
            EnemyState state = enemy.GetComponent<CO_EnemyAI_Script>().CurrentState;
            return state == EnemyState.Chasing || state == EnemyState.Attacking;
        }

        [UnityTest]
        public IEnumerator AGlimpseShorterThanTheNoticeTimeIsNotActedOn()
        {
            GameObject enemy = SpawnEnemyWithNoticeTime(1f);
            CO_EnemyDetection_Script detection = enemy.GetComponent<CO_EnemyDetection_Script>();

            player.transform.position = InView;

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => detection.HasDetectedPlayer, "the enemy to see the player", 5f);

            // A third of the notice time in view, then gone.
            float leaveAt = Time.time + 0.3f;
            bool reacted = false;

            while (Time.time < leaveAt)
            {
                reacted |= IsReacting(enemy);
                yield return null;
            }

            Assert.That(detection.NoticeLevel, Is.GreaterThan(0f).And.LessThan(1f),
                "the meter should be part-filled after a short look");

            player.transform.position = OutOfView;

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () =>
                {
                    reacted |= IsReacting(enemy);
                    return detection.NoticeLevel <= 0f;
                },
                "the meter to drain once the player is gone", 5f);

            Assert.That(reacted, Is.False, "a glimpse must not start a chase");
        }

        [UnityTest]
        public IEnumerator StayingInViewFillsTheMeterAndStartsAChase()
        {
            const float noticeTime = 1f;
            GameObject enemy = SpawnEnemyWithNoticeTime(noticeTime);
            CO_EnemyDetection_Script detection = enemy.GetComponent<CO_EnemyDetection_Script>();

            player.transform.position = InView;

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => detection.HasDetectedPlayer, "the enemy to see the player", 5f);

            float seenAt = Time.time;

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => IsReacting(enemy), "the enemy to chase once the meter is full", 5f);

            // Allow a frame or two either side for the scan interval.
            Assert.That(Time.time - seenAt, Is.GreaterThanOrEqualTo(noticeTime * 0.9f),
                "the chase should wait for the meter to fill");
            Assert.That(detection.NoticeLevel, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator AZeroNoticeTimeReactsOnFirstSight()
        {
            GameObject enemy = SpawnEnemyWithNoticeTime(0f);
            CO_EnemyDetection_Script detection = enemy.GetComponent<CO_EnemyDetection_Script>();

            player.transform.position = InView;

            yield return CO_DistractionTestScene_Script.WaitUntil(
                () => detection.HasDetectedPlayer, "the enemy to see the player", 5f);

            // The meter fills in the same Update that saw the player; the AI acts on it within a frame.
            yield return null;
            yield return null;

            Assert.That(IsReacting(enemy), Is.True, "an instant noticer chases at once");
        }
    }
}
