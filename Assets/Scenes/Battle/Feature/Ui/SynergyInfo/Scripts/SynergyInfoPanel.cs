using Scenes.Battle.Feature.Synergy;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.SynergyInfo
{
    /// <summary>
    /// 시너지 정보 UI의 루트 컴포넌트. SynergyListPanel과 SynergyDetailPanel을 조율한다.
    /// </summary>
    public class SynergyInfoPanel : MonoBehaviour
    {
        /// <summary>시너지 목록 패널.</summary>
        [SerializeField] private SynergyListPanel listPanel;

        /// <summary>시너지 상세 패널.</summary>
        [SerializeField] private SynergyDetailPanel detailPanel;

        private void OnEnable()
        {
            listPanel.OnIndicatorClicked += HandleIndicatorClicked;
        }

        private void OnDisable()
        {
            listPanel.OnIndicatorClicked -= HandleIndicatorClicked;
        }

        /// <summary>인디케이터 클릭을 수신하여 상세 패널에 전달한다. (CD-10)</summary>
        private void HandleIndicatorClicked(SynergyActivation activation)
        {
            detailPanel.Show(activation);
        }
    }
}
