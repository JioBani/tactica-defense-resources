using Common.Scripts.ObjectPool;
using Common.Scripts.SceneSingleton;
using UnityEngine;

namespace Common.Scripts.BubbleMessage
{
    public class BubbleMessageSpawner : SceneSingleton<BubbleMessageSpawner>
    {
        [SerializeField] private BubbleMessageConfig config;
        [SerializeField] private GameObject bubbleMessagePrefab;

        [SerializeField] private RectTransform screenSpaceParent;
        [SerializeField] private Camera mainCamera;

        public void SpawnAtWorld(string text, Vector3 worldPosition,
            BubbleMessageParams param = default)
        {
            Vector2 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                screenSpaceParent, screenPoint, null, out var anchoredPosition
            );
            SpawnAtScreen(text, anchoredPosition, param);
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
