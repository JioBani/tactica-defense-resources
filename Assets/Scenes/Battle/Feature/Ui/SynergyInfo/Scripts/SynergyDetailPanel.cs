using Scenes.Battle.Feature.Synergy;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.SynergyInfo
{
    /// <summary>
    /// 시너지 상세 패널. 시너지 클릭 시 상세 정보를 표시한다.
    /// 내부 구현은 TACD-296에서 진행한다.
    /// </summary>
    public class SynergyDetailPanel : MonoBehaviour
    {
        /// <summary>상세 패널을 열고 시너지 데이터를 바인딩한다.</summary>
        public void Show(SynergyActivation activation)
        {
            gameObject.SetActive(true);
        }

        /// <summary>상세 패널을 닫는다.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
