using DG.Tweening;
using Scenes.Battle.Feature.Markets;
using TMPro;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.Markets
{
    public class MarketUiManager : MonoBehaviour
    {
        [SerializeField] private MarketManager marketManager;
        [SerializeField] private TextMeshProUGUI manaText;
        [SerializeField] private TextMeshProUGUI levelUpCostText;
        [SerializeField] private GameObject levelUpButton;
        [SerializeField] private RectTransform marketPanel;
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private TextMeshProUGUI toggleText;

        private bool _isOpen = true;
        private float _shownX;
        private float _hiddenX;
        private Tweener _tween;

        private void Awake()
        {
            _shownX = marketPanel.anchoredPosition.x;
            _hiddenX = _shownX + marketPanel.rect.width;
        }

        private void OnEnable()
        {
            marketManager.Mana.OnChange += OnManaChange;
            marketManager.Level.OnChange += OnLevelChange;
            OnLevelChange(marketManager.Level.Value);
        }

        private void OnDisable()
        {
            marketManager.Mana.OnChange -= OnManaChange;
            marketManager.Level.OnChange -= OnLevelChange;
        }

        public void OnClickLevelUp()
        {
            marketManager.LevelUp();
        }

        private void OnManaChange(int mana)
        {
            manaText.text = $"{mana} MANA";
        }

        private void OnLevelChange(int level)
        {
            if (marketManager.IsMaxLevel())
            {
                levelUpCostText.text = "MAX";
                levelUpButton.SetActive(false);
            }
            else
            {
                levelUpCostText.text = $"배치 증가\n({marketManager.LevelUpMana.Value})";
                levelUpButton.SetActive(true);
            }
        }

        public void OnClickReroll()
        {
            marketManager.Reroll();
        }

        public void OnClickToggle()
        {
            _isOpen = !_isOpen;
            float targetX = _isOpen ? _shownX : _hiddenX;

            _tween?.Kill();
            _tween = marketPanel.DOAnchorPosX(targetX, slideDuration)
                .SetEase(Ease.InOutSine);

            toggleText.text = _isOpen ? "터미널\n닫기" : "터미널\n열기";
        }
    }
}
