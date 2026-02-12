using DG.Tweening;
using UnityEngine;

namespace Common.Scripts.BubbleMessage
{
    [CreateAssetMenu(menuName = "GameConfig/BubbleMessageConfig")]
    public class BubbleMessageConfig : ScriptableObject
    {
        [Header("애니메이션")]
        [Tooltip("버블 메시지 전체 지속 시간")]
        public float duration = 1.0f;

        [Tooltip("위로 떠오르는 거리 (anchoredPosition 기준)")]
        public float floatDistance = 80f;

        [Tooltip("이동 애니메이션 Ease")]
        public Ease moveEase = Ease.OutCubic;

        [Tooltip("페이드아웃 Ease")]
        public Ease fadeEase = Ease.InCubic;

        [Tooltip("페이드 시작 시점 (전체 duration 대비 비율, 0~1)")]
        [Range(0f, 1f)]
        public float fadeStartRatio = 0.3f;

        [Header("텍스트 기본값")]
        public Color defaultColor = Color.white;
        public float defaultFontSize = 36f;
    }
}
