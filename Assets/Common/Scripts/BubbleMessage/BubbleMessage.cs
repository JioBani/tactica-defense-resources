using Common.Scripts.ObjectPool;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Common.Scripts.BubbleMessage
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Poolable))]
    public class BubbleMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Poolable poolable;

        private RectTransform _rectTransform;
        private Sequence _sequence;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Play(string text, BubbleMessageConfig config, BubbleMessageParams param)
        {
            messageText.text = text;
            messageText.color = param.color ?? config.defaultColor;
            messageText.fontSize = param.fontSize ?? config.defaultFontSize;

            canvasGroup.alpha = 1f;

            float duration = param.duration ?? config.duration;
            float floatDist = param.floatDistance ?? config.floatDistance;
            float fadeDelay = duration * config.fadeStartRatio;
            float fadeDuration = duration - fadeDelay;

            _sequence?.Kill();

            _sequence = DOTween.Sequence();

            _sequence.Join(
                _rectTransform.DOAnchorPosY(floatDist, duration)
                    .SetRelative(true)
                    .SetEase(config.moveEase)
            );

            _sequence.Insert(fadeDelay,
                canvasGroup.DOFade(0f, fadeDuration)
                    .SetEase(config.fadeEase)
            );

            _sequence.OnComplete(() =>
            {
                _sequence = null;
                poolable.DeSpawn();
            });
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
        }
    }
}
