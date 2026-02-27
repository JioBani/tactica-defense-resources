using Common.Data.Configs;
using Scenes.Battle.Feature.Markets;
using TMPro;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.Markets
{
    /// <summary>
    /// 성급별 등장 확률을 표시하는 패널.
    /// MarketManager.Level이 변경될 때마다 StarProbabilityConfig에서 확률을 조회하여 갱신한다.
    /// </summary>
    public class StarRatesPanel : MonoBehaviour
    {
        /// <summary>1성 등장 확률 텍스트.</summary>
        [SerializeField] private TextMeshProUGUI oneStarRateText;

        /// <summary>2성 등장 확률 텍스트.</summary>
        [SerializeField] private TextMeshProUGUI twoStarRateText;

        /// <summary>3성 등장 확률 텍스트.</summary>
        [SerializeField] private TextMeshProUGUI threeStarRateText;

        private StarProbabilityConfig _config;

        private void OnEnable()
        {
            _config = MarketManager.Instance.StarProbabilityConfig;
            MarketManager.Instance.Level.OnChange += UpdatePanel;
            UpdatePanel(MarketManager.Instance.Level.Value);
        }

        private void OnDisable()
        {
            MarketManager.Instance.Level.OnChange -= UpdatePanel;
        }

        /// <summary>
        /// 주어진 레벨의 성급 등장 확률을 조회하여 텍스트를 갱신한다.
        /// </summary>
        private void UpdatePanel(int level)
        {
            StarProbabilityEntry? entry = _config.FindEntry(level);

            float twoStarRate = 0f;
            float threeStarRate = 0f;

            if (entry.HasValue)
            {
                twoStarRate = entry.Value.twoStarRate;
                threeStarRate = entry.Value.threeStarRate;
            }

            float oneStarRate = 1f - twoStarRate - threeStarRate;

            oneStarRateText.text = $"{oneStarRate * 100f:F1}%";
            twoStarRateText.text = $"{twoStarRate * 100f:F1}%";
            threeStarRateText.text = $"{threeStarRate * 100f:F1}%";
        }
    }
}
