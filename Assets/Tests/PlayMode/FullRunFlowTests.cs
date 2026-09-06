using System.Collections;
using NUnit.Framework;
using ProtocolArena.Core;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ProtocolArena.PlayModeTests
{
    public sealed class FullRunFlowTests
    {
        [UnityTest]
        public IEnumerator ThreeRewardsBattleAndNewRun_CompleteWithoutErrors()
        {
            yield return null;
            ProtocolArenaGame game = Object.FindAnyObjectByType<ProtocolArenaGame>();
            Assert.That(game, Is.Not.Null);

            Transform preparation = FindChild(game.transform, "Preparation");
            Transform calibration = FindChild(game.transform, "Calibration");
            Transform reward = FindChild(game.transform, "Reward");
            Transform battle = FindChild(game.transform, "Battle");
            Transform result = FindChild(game.transform, "Result");
            Assert.That(preparation.gameObject.activeSelf, Is.True);

            for (int round = 0; round < 3; round++)
            {
                preparation.Find("Preparation Action").GetComponent<Button>().onClick.Invoke();
                Assert.That(calibration.gameObject.activeSelf, Is.True);
                Button lockButton = calibration.Find("Lock In").GetComponent<Button>();
                Time.timeScale = 30f;
                while (!lockButton.interactable) yield return null;
                lockButton.onClick.Invoke();
                lockButton.onClick.Invoke();
                lockButton.onClick.Invoke();
                while (!reward.gameObject.activeSelf) yield return null;
                Time.timeScale = 1f;
                Assert.That(reward.gameObject.activeSelf, Is.True);
                Assert.That(reward.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(3));
                reward.Find("Reward Card 1").GetComponent<Button>().onClick.Invoke();
                Assert.That(preparation.gameObject.activeSelf, Is.True);
            }

            Text actionLabel = preparation.Find("Preparation Action").GetComponentInChildren<Text>();
            Assert.That(actionLabel.text, Is.EqualTo("전투 시작"));
            preparation.Find("Preparation Action").GetComponent<Button>().onClick.Invoke();
            Assert.That(battle.gameObject.activeSelf, Is.True);

            Time.timeScale = 30f;
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!result.gameObject.activeSelf && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Time.timeScale = 1f;

            Assert.That(result.gameObject.activeSelf, Is.True, "Auto battle should reach a result.");
            Assert.That(result.Find("Result Title").GetComponent<Text>().text, Is.EqualTo("승리").Or.EqualTo("패배"));
            Assert.That(result.Find("Run Report/Modules").GetComponent<Text>().text, Does.Contain("최종 능력치"));

            result.Find("New Run").GetComponent<Button>().onClick.Invoke();
            Assert.That(preparation.gameObject.activeSelf, Is.True);
            Assert.That(preparation.Find("Preparation Round").GetComponent<Text>().text, Does.Contain("1 / 3"));
            Assert.That(preparation.Find("Acquired Modules/Modules").GetComponent<Text>().text, Does.Contain("아직 획득한 강화"));
        }

        private static Transform FindChild(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                if (children[index].name == name) return children[index];
            }

            Assert.Fail($"Could not find child named '{name}'.");
            return null;
        }
    }
}
