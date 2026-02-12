using Common.Scripts.ObjectPool;
using Common.Scripts.SceneSingleton;
using UnityEngine;

namespace Common.Scripts.BubbleMessage
{
    public class BubbleMessageSpawner : SceneSingleton<BubbleMessageSpawner>
    {
        [SerializeField] private BubbleMessageConfig config;
        [SerializeField] private GameObject bubbleMessagePrefab;

        [Header("Parents")]
        [SerializeField] private Transform worldSpaceParent;
        [SerializeField] private RectTransform screenSpaceParent;

        public void SpawnAtWorld(string text, Vector3 worldPosition,
            BubbleMessageParams param = default)
        {
            GameObject go = ObjectPooler.Instance.Spawn(
                bubbleMessagePrefab,
                worldSpaceParent,
                worldPosition
            );

            var bubble = go.GetComponent<BubbleMessage>();
            bubble.Play(text, config, param);
        }

        public void SpawnAtScreen(string text, Vector2 anchoredPosition,
            BubbleMessageParams param = default)
        {
            GameObject go = ObjectPooler.Instance.SpawnUI(
                bubbleMessagePrefab,
                screenSpaceParent,
                anchoredPosition
            );

            var bubble = go.GetComponent<BubbleMessage>();
            bubble.Play(text, config, param);
        }
    }
}
