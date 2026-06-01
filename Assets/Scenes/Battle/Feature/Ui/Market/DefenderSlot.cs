using System.Collections.Generic;
using Common.Data.Units.UnitDefinitions;
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

        /// <summary>카드 오른쪽 위에 표시되는 역할군 아이콘.</summary>
        [SerializeField] private Image roleGroupIcon;

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
            SetRoleGroupIcon(slotData.UnitLoadOutData.Unit);
            IsPurchased = false;
            ActivateImage();
        }

        /// <summary>카드 오른쪽 위에 소환수 종류의 역할군 아이콘을 표시한다. 소환 카드는 소환수 전용이므로 역할군이 없으면 데이터 누락이다.</summary>
        private void SetRoleGroupIcon(UnitDefinitionData unit)
        {
            if (roleGroupIcon == null)
            {
                // 비정상 셋업: 카드 프리팹에 역할군 아이콘 Image가 연결되지 않음. 나머지 표시는 진행하되 단서를 남긴다.
                Debug.LogError("[DefenderSlot] roleGroupIcon이 연결되지 않았습니다. 카드 프리팹의 역할군 아이콘 Image 연결을 확인하세요.", this);
            }
            else if (unit.RoleGroup == null)
            {
                // 소환수인데 역할군 미설정 — 데이터 누락이므로 단서를 남기고 빈 슬롯으로 둔다.
                Debug.LogWarning($"[DefenderSlot] 소환수 '{unit.DisplayName}'에 역할군이 설정되지 않았습니다. UnitDefinitionData.RoleGroup을 확인하세요.");
                roleGroupIcon.sprite = null;
                roleGroupIcon.enabled = false;
            }
            else
            {
                // 역할군은 있으나 아이콘이 아직 없으면(아트 미할당) 빈 슬롯으로 둔다.
                roleGroupIcon.sprite = unit.RoleGroup.Icon;
                roleGroupIcon.enabled = unit.RoleGroup.Icon != null;
            }
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
