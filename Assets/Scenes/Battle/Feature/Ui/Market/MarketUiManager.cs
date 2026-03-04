using DG.Tweening;
using Scenes.Battle.Feature.Markets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────
// MarketUiManager: 소환터미널 UI를 관리한다.
// 마나 표시, 배치 상한 증가 버튼, 재스캔 버튼, 스캔 잠금 버튼, 터미널 열기/닫기.
// 비즈니스 로직은 MarketManager가 담당한다.
// ─────────────────────────────────────────────
namespace Scenes.Battle.Feature.Ui.Markets
{
    public class MarketUiManager : MonoBehaviour
    {
        [SerializeField] private MarketManager marketManager;
        [SerializeField] private TextMeshProUGUI manaText;
        [SerializeField] private TextMeshProUGUI levelUpCostText;
        [SerializeField] private GameObject levelUpButton;
        [SerializeField] private TextMeshProUGUI scanLockText;
        [SerializeField] private RectTransform marketPanel;
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private TextMeshProUGUI toggleText;

        /// <summary>터미널 열기/닫기 버튼. 상점 비활성 시 interactable을 끈다.</summary>
        [SerializeField] private Button toggleButton;

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
            marketManager.IsScanLocked.OnChange += OnScanLockChange;
            marketManager.IsMarketAvailable.OnChange += OnMarketAvailableChange;
            OnLevelChange(marketManager.Level.Value);
        }

        private void OnDisable()
        {
            marketManager.Mana.OnChange -= OnManaChange;
            marketManager.Level.OnChange -= OnLevelChange;
            marketManager.IsScanLocked.OnChange -= OnScanLockChange;
            marketManager.IsMarketAvailable.OnChange -= OnMarketAvailableChange;
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

        /// <summary>스캔 잠금 버튼 클릭 핸들러.</summary>
        public void OnClickScanLock()
        {
            marketManager.ToggleScanLock();
        }

        private void OnScanLockChange(bool isLocked)
        {
            scanLockText.text = isLocked ? "잠금 해제" : "스캔 잠금";
        }

        public void OnClickToggle()
        {
            if (!marketManager.IsMarketAvailable.Value) return;
            SlidePanel(!_isOpen);
        }

        private void OnMarketAvailableChange(bool isAvailable)
        {
            SlidePanel(isAvailable);
            toggleButton.interactable = isAvailable;
        }

        /// <summary>패널 열기/닫기 슬라이드 애니메이션을 실행한다.</summary>
        private void SlidePanel(bool open)
        {
            _isOpen = open;
            float targetX = _isOpen ? _shownX : _hiddenX;

            _tween?.Kill();
            _tween = marketPanel.DOAnchorPosX(targetX, slideDuration)
                .SetEase(Ease.InOutSine);

            toggleText.text = _isOpen ? "터미널\n닫기" : "터미널\n열기";
        }
    }
}
