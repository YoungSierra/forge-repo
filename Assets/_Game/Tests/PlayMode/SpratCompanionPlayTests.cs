using System.Collections;
using NUnit.Framework;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Sprat;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProfessorSprat.Tests.PlayMode
{
    /// <summary>TDD §B SpratCompanion PlayMode criteria in SCN_SpratCompanion_Test.</summary>
    public sealed class SpratCompanionPlayTests : PlayTestBase
    {
        #region Fields

        private SpratCompanion _sprat;

        #endregion

        #region Public Methods

        [UnityTest]
        public IEnumerator ACSPR02_FollowLag()
        {
            yield return Begin();
            Professor.SetMoveInput(Vector3.forward);
            float worst = 0f;
            for (int i = 0; i < 50; i++)
            {
                yield return WaitStep();
                worst = Mathf.Max(worst, (_sprat.transform.position - _sprat.Target).magnitude);
            }

            Assert.That(worst, Is.LessThanOrEqualTo(1.5f), "lag ≤ 1.5 m while running");
            Professor.SetMoveInput(Vector3.zero);
            yield return WaitSeconds(0.75f);
            Assert.That((_sprat.transform.position - _sprat.Target).magnitude, Is.LessThanOrEqualTo(0.3f), "settles 0.5 s after stopping");
        }

        [UnityTest]
        public IEnumerator ACSPR03_Reactions()
        {
            yield return Begin();
            EventBus.Publish(new KeySpawnedEvent("Z1", Vector3.zero, null));
            yield return null;
            yield return null;
            Assert.AreEqual(SpratState.Spin, _sprat.State);
            yield return WaitSeconds(1.2f);
            Professor.ApplyKnockback(Vector3.back);
            yield return null;
            yield return null;
            Assert.AreEqual(SpratState.Freeze, _sprat.State);
            yield return WaitSeconds(0.7f);
            yield return null;
            Assert.AreEqual(SpratState.Idle, _sprat.State);
        }

        [UnityTest]
        public IEnumerator ACSPR04_NoGameplayEffect()
        {
            yield return Begin();
            Assert.AreEqual(0, _sprat.GetComponentsInChildren<Collider>(true).Length, "Sprat has no collider");
            Vector3 withSprat = default;
            yield return Run(result => withSprat = result);
            _sprat.gameObject.SetActive(false);
            Vector3 withoutSprat = default;
            yield return Run(result => withoutSprat = result);
            Assert.That(Vector3.Distance(withSprat, withoutSprat), Is.LessThanOrEqualTo(0.001f));
        }

        #endregion

        #region Private Methods

        private IEnumerator Begin()
        {
            yield return LoadScene("SCN_SpratCompanion_Test");
            _sprat = Object.FindAnyObjectByType<SpratCompanion>();
            _sprat.Bind(Professor.transform);
        }

        private IEnumerator Run(System.Action<Vector3> result)
        {
            yield return Place(new Vector3(-5f, 0.05f, -10f), 0f);
            yield return WaitSeconds(0.3f);
            Professor.SetMoveInput(new Vector3(0.6f, 0f, 0.8f));
            yield return WaitSeconds(3f);
            Professor.SetMoveInput(Vector3.zero);
            result(Professor.transform.position);
        }

        #endregion
    }
}
