using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace ClockOut.Tests
{
    /// <summary>
    /// Builds throwaway scenes for the play mode tests - a baked floor, enemies
    /// wired the way the scene wires them, radios and a tagged player - and tears
    /// the lot down again afterwards.
    ///
    /// The components keep their configuration in private [SerializeField] fields,
    /// which the Inspector fills. Adding public setters purely so tests could
    /// reach them would widen the runtime API for no gameplay reason, so the
    /// reflection needed to stand in for the Inspector lives here instead.
    /// </summary>
    internal sealed class CO_DistractionTestScene_Script
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        /// <summary>
        /// A floor with a baked NavMesh. Enemies are spawned after this so the
        /// bake sees an empty world and they land on a walkable surface.
        /// </summary>
        public void BuildFloor(float size = 60f)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TestFloor";

            // A plane primitive is 10 units across at scale 1.
            floor.transform.localScale = Vector3.one * (size / 10f);
            Track(floor);

            NavMeshSurface surface = floor.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();
        }

        /// <summary>
        /// An enemy carrying the full stack, facing along +Z unless told otherwise.
        /// Built inactive so every field is in place before Awake runs, which is
        /// what the Inspector guarantees for a real prefab.
        /// </summary>
        public GameObject SpawnEnemy(string name, Vector3 position, float yaw = 0f)
        {
            GameObject enemy = new GameObject(name);
            enemy.SetActive(false);
            enemy.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Track(enemy);

            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 2f;
            agent.acceleration = 30f;
            agent.angularSpeed = 720f;

            // Two waypoints either side of the spawn, so the route is valid and
            // the enemy has somewhere to go back to after a distraction.
            Transform[] route =
            {
                SpawnMarker(name + "_wp0", position + Vector3.forward * 3f).transform,
                SpawnMarker(name + "_wp1", position - Vector3.forward * 3f).transform
            };

            CO_EnemyPatrol_Script patrol = enemy.AddComponent<CO_EnemyPatrol_Script>();
            SetField(patrol, "waypoints", route);

            enemy.AddComponent<CO_EnemyDetection_Script>();
            enemy.AddComponent<CO_EnemyAttack_Script>();
            enemy.AddComponent<CO_EnemyHearing_Script>();
            enemy.AddComponent<CO_EnemyInvestigation_Script>();
            enemy.AddComponent<CO_EnemyAI_Script>();

            enemy.SetActive(true);
            return enemy;
        }

        /// <summary>
        /// A bare ear with no body: used by the routing tests, which are about
        /// which listener the CO_NoiseManager_Script picks and need no navigation at all.
        /// With sight it also gets eyes, added before activation because
        /// CO_EnemyHearing_Script looks for them in Awake - exactly as a prefab would.
        /// </summary>
        public CO_EnemyHearing_Script SpawnListener(
            string name,
            Vector3 position,
            float alertDuration = 2.5f,
            bool withSight = false,
            float standDownDelay = 2f)
        {
            GameObject listener = new GameObject(name);
            listener.SetActive(false);
            listener.transform.position = position;
            Track(listener);

            if (withSight)
            {
                listener.AddComponent<CO_EnemyDetection_Script>();
            }

            CO_EnemyHearing_Script hearing = listener.AddComponent<CO_EnemyHearing_Script>();
            SetField(hearing, "alertDuration", alertDuration);
            SetField(hearing, "standDownDelay", standDownDelay);

            listener.SetActive(true);
            return hearing;
        }

        /// <summary>
        /// Somewhere for a noise to have come from. Deliberately deaf: giving it
        /// an ear would put a listener at the exact centre of every noise, and it
        /// would win the investigation every time.
        /// </summary>
        public T SpawnNoiseSource<T>(string name, Vector3 position) where T : MonoBehaviour
        {
            GameObject source = new GameObject(name);
            source.transform.position = position;
            Track(source);

            return source.AddComponent<T>();
        }

        /// <summary>A radio, with the collider its interaction needs.</summary>
        public CO_DistractionDevice_Script SpawnRadio(string name, Vector3 position, float noiseRadius = 16f)
        {
            GameObject radio = new GameObject(name);
            radio.SetActive(false);
            radio.transform.position = position;
            Track(radio);

            radio.AddComponent<BoxCollider>();

            CO_DistractionDevice_Script device = radio.AddComponent<CO_DistractionDevice_Script>();
            SetField(device, "noiseRadius", noiseRadius);

            radio.SetActive(true);
            return device;
        }

        /// <summary>
        /// The player: tagged, and carrying the interactor and an empty work stack.
        /// With a body it gets a CharacterController instead of a box, so it can
        /// walk into walls.
        /// </summary>
        public GameObject SpawnPlayer(Vector3 position, bool withBody = false)
        {
            GameObject player = new GameObject("TestPlayer");
            player.SetActive(false);
            player.transform.position = position;
            Track(player);

            player.tag = "Player";
            if (withBody)
            {
                player.AddComponent<CharacterController>();
            }
            else
            {
                player.AddComponent<BoxCollider>();
            }

            player.AddComponent<CO_PlayerInteractor_Script>();
            AddWorkStack(player);

            player.SetActive(true);
            return player;
        }

        /// <summary>
        /// Adds a work stack with default tuning. Call it while the object is
        /// inactive, as a prefab would have it, so the tuning is in place before Awake.
        /// </summary>
        public static CO_WorkStack_Script AddWorkStack(GameObject player)
        {
            CO_WorkStack_Script stack = player.AddComponent<CO_WorkStack_Script>();

            // Made from the field's type: the tuning type lives in ClockOut.Data,
            // which this test assembly doesn't reference.
            FieldInfo tuningField = typeof(CO_WorkStack_Script).GetField(
                "tuning", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tuningField, Is.Not.Null, "CO_WorkStack_Script has no field 'tuning' - the test needs updating.");
            tuningField.SetValue(stack, ScriptableObject.CreateInstance(tuningField.FieldType));

            return stack;
        }

        /// <summary>Changes one number on a stack's tuning, standing in for the asset's Inspector.</summary>
        public static void SetTuning(CO_WorkStack_Script stack, string fieldName, object value)
        {
            FieldInfo tuningField = typeof(CO_WorkStack_Script).GetField(
                "tuning", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tuningField, Is.Not.Null, "CO_WorkStack_Script has no field 'tuning' - the test needs updating.");

            SetField(tuningField.GetValue(stack), fieldName, value);
        }

        /// <summary>A solid cube, for crowding an enemy with colliders.</summary>
        public GameObject SpawnBlock(Vector3 position)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "TestBlock";
            block.transform.position = position;
            Track(block);
            return block;
        }

        private GameObject SpawnMarker(string name, Vector3 position)
        {
            GameObject marker = new GameObject(name);
            marker.transform.position = position;
            Track(marker);
            return marker;
        }

        /// <summary>Destroys the object in TearDown with everything else this scene spawned.</summary>
        public void Track(GameObject spawnedObject)
        {
            spawned.Add(spawnedObject);
        }

        /// <summary>
        /// Destroys everything immediately rather than at the end of the frame, so
        /// each enemy is off the CO_NoiseManager_Script's list before the next test spawns
        /// its own and starts counting listeners.
        /// </summary>
        public void TearDown()
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(spawned[i]);
                }
            }

            spawned.Clear();
        }

        /// <summary>Stands in for the Inspector filling a [SerializeField] field.</summary>
        public static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null,
                $"{target.GetType().Name} has no field '{fieldName}' - the test needs updating.");

            field.SetValue(target, value);
        }

        /// <summary>
        /// Waits for a condition, failing with a readable message instead of
        /// hanging the run. The deadline is in unscaled real seconds, so raising
        /// Time.timeScale to speed a test up does not shorten its patience.
        /// </summary>
        public static IEnumerator WaitUntil(Func<bool> condition, string description, float timeoutSeconds = 15f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;

            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail($"Timed out after {timeoutSeconds}s waiting for: {description}");
                }

                yield return null;
            }
        }

        /// <summary>Lets a fixed number of scaled seconds pass.</summary>
        public static IEnumerator Wait(float seconds)
        {
            float remaining = seconds;

            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                yield return null;
            }
        }
    }
}
