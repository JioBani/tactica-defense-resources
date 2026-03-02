// ─────────────────────────────────────────────
// AggressorPreview: 프리뷰 모드에서 Aggressor의 표시 상태를 관리한다.
// Freeze 상태 전환과 등장 숫자(CountText) 표시를 담당한다.
// ─────────────────────────────────────────────
using Scenes.Battle.Feature.Units.ActionStates;
using TMPro;
using UnityEngine;

namespace Scenes.Battle.Feature.Aggressors
{
    public class AggressorPreview : MonoBehaviour
    {
        private ActionStateController _actionStateController;
        private TextMeshPro _countText;

        private void Awake()
        {
            _actionStateController = GetComponent<ActionStateController>();
            _countText = transform.Find("CountText").GetComponent<TextMeshPro>();
        }

        /// <summary>
        /// 프리뷰 모드를 활성화한다. Freeze 상태로 전환하고 등장 숫자를 표시한다.
        /// </summary>
        /// <param name="count">해당 그룹의 침략자 등장 숫자.</param>
        public void Activate(int count)
        {
            _actionStateController.RequestStateChange(ActionStateType.Freeze);
            _countText.gameObject.SetActive(true);
            _countText.text = $"x {count}";
        }

        /// <summary>
        /// 프리뷰 모드를 비활성화한다. CountText를 숨긴다.
        /// </summary>
        public void Deactivate()
        {
            _countText.gameObject.SetActive(false);
            _countText.text = "";
        }
    }
}
