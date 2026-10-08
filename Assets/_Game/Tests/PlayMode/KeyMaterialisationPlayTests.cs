using System.Collections;
using NUnit.Framework;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.AccessKey;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProfessorSprat.Tests.PlayMode
{
    /// <summary>TDD §B KeyMaterialisation PlayMode criteria in SCN_KeyMaterialisation_Test (key at z 4, door at z 9).</summary>
    public sealed class KeyMaterialisationPlayTests : PlayTestBase
    {
        #region Fields

        private AccessKeyController _key;
        private ZoneDoorController _door;

        #endregion

        #region Public Methods

        [UnityTest]
        public IEnumerator ACKEY02_SpawnAndAmberRamp()
        {
            yield return Arm();
            using (EventRecorder<KeySpawnedEvent> spawned = new EventRecorder<KeySpawnedEvent>())
            {
                EventBus.Publish(new ZoneCompletedEvent("Z1"));
                Assert.AreEqual(1, spawned.Count, "spawned within 1 frame");
                yield return WaitSeconds(1.12f);
                Assert.That(_key.LightIntensity, Is.LessThan(4.0f));
                yield return WaitSeconds(0.12f);
                Assert.That(_key.LightIntensity, Is.GreaterThanOrEqualTo(3.99f), "amber 4.0 at 1.2 s ± 0.05 s");
                Assert.AreNotEqual(KeyState.Spawning, _key.State);
            }
        }

        [UnityTest]
        public IEnumerator ACKEY03_SpawnIsIdempotent()
        {
            yield return Arm();
            using (EventRecorder<KeySpawnedEvent> spawned = new EventRecorder<KeySpawnedEvent>())
            {
                EventBus.Publish(new ZoneCompletedEvent("Z1"));
                EventBus.Publish(new ZoneCompletedEvent("Z1"));
                yield return WaitStep();
                Assert.AreEqual(1, spawned.Count);
            }
        }

        [UnityTest]
        public IEnumerator ACKEY04_PickupAfterCeremonyThenDoor()
        {
            yield return Arm();
            using (EventRecorder<KeyCollectedEvent> collected = new EventRecorder<KeyCollectedEvent>())
            using (EventRecorder<DoorUnlockedEvent> unlocked = new EventRecorder<DoorUnlockedEvent>())
            {
                yield return Place(_key.transform.position + new Vector3(0f, -0.55f, -1.0f), 0f);
                EventBus.Publish(new ZoneCompletedEvent("Z1"));
                yield return WaitSeconds(1.1f);
                Assert.AreEqual(0, collected.Count, "no pickup during the ceremony");
                yield return WaitSeconds(0.2f);
                Assert.AreEqual(1, collected.Count, "collected when the ceremony ends");
                yield return Place(_door.transform.position + new Vector3(0f, 0.05f, -1.4f), 0f);
                float elapsed = 0f;
                while (unlocked.Count == 0 && elapsed < 2f)
                {
                    yield return WaitStep();
                    elapsed += Time.fixedDeltaTime;
                }

                Assert.AreEqual(1, unlocked.Count);
                Assert.That(elapsed, Is.InRange(0.75f, 0.85f), "door opens in 0.8 s ± 0.05 s");
            }
        }

        [UnityTest]
        public IEnumerator ACKEY05_DoorLockedWithoutKey()
        {
            yield return Arm();
            using (EventRecorder<DoorUnlockedEvent> unlocked = new EventRecorder<DoorUnlockedEvent>())
            {
                yield return Place(_door.transform.position + new Vector3(0f, 0.05f, -1.2f), 0f);
                yield return WaitSeconds(1.5f);
                Assert.AreEqual(0, unlocked.Count);
                Assert.IsFalse(_door.IsOpen);
            }
        }

        #endregion

        #region Private Methods

        private IEnumerator Arm()
        {
            yield return LoadScene("SCN_KeyMaterialisation_Test");
            _key = Object.FindAnyObjectByType<AccessKeyController>();
            _door = Object.FindAnyObjectByType<ZoneDoorController>();
            _key.Arm("Z1", _door);
            yield return Place(new Vector3(0f, 0.05f, -6f), 0f);
        }

        #endregion
    }
}
