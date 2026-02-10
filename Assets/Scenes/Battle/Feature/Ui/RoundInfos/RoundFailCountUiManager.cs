using Scenes.Battle.Feature.Rounds;
using TMPro;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.RoundInfos
{
    public class RoundFailCountUiManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI failCountText;

        private void OnEnable()
        {
            RoundManager.Instance.RoundFailCount.OnChange += OnRoundFailCountChange;
            UpdateText();
        }

        private void OnDisable()
        {
            RoundManager.Instance.RoundFailCount.OnChange -= OnRoundFailCountChange;
        }

        private void OnRoundFailCountChange(int _)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            failCountText.text = $"남은 패배 횟수 : {RoundManager.Instance.RemainingFailCount}";
        }
    }
}
