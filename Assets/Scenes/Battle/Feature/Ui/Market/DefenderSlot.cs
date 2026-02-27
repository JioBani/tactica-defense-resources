using System.Collections.Generic;
using Scenes.Battle.Feature.Unit.Defenders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.Battle.Feature.Markets
{
    /// <summary>
    /// 상점 UI의 소환수 구매 슬롯. 리롤 시 MarketDefenderSlot을 수신하여 표시한다.
    /// </summary>
    public class DefenderSlot : MonoBehaviour
    {
        private MarketManager _marketManager;
        [SerializeField] private DefenderManager defenderManager;

        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI purchasedText;
        [SerializeField] private TextMeshProUGUI starText;
        [SerializeField] private TextMeshProUGUI manaText;

        [SerializeField] private int index;

        /// <summary>현재 슬롯에 장착된 마켓 데이터 (유닛 + 성급).</summary>
        private MarketDefenderSlot _slotData;
        private bool IsPurchased { get; set; } = false;

        private void Awake()
        {
            _marketManager = MarketManager.Instance;
        }

        private void OnEnable()
        {
            _marketManager.OnSlotRerolled += OnSlotRerolled;
        }

        private void OnDisable()
        {
            _marketManager.OnSlotRerolled -= OnSlotRerolled;
        }

        private void OnSlotRerolled(List<MarketDefenderSlot> slotList)
        {
            // 자신의 슬롯 번호에 해당하는 데이터 장착
            SetSlotData(slotList[index]);
        }

        private void SetSlotData(MarketDefenderSlot slotData)
        {
            _slotData = slotData;
            image.sprite = slotData.UnitLoadOutData.Unit.Illustration;
            starText.text = new string('★', slotData.Star);
            manaText.text = slotData.UnitLoadOutData.GetCostByStar(slotData.Star).ToString();
            IsPurchased = false;
            ActivateImage();
        }

        public void OnClick()
        {
            if (!IsPurchased)
            {
                Purchase();
            }
        }

        private void Purchase()
        {
            IsPurchased = MarketManager.Instance.BuyDefender(_slotData);

            if (IsPurchased)
            {
                DeactivateImage();
            }
        }

        private void DeactivateImage()
        {
            image.gameObject.SetActive(false);
            purchasedText.gameObject.SetActive(true);
        }

        private void ActivateImage()
        {
            image.gameObject.SetActive(true);
            purchasedText.gameObject.SetActive(false);
        }
    }
}
