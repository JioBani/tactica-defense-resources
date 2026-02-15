using System.Collections.Generic;
using Common.Data.Units.UnitStatsByLevel;
using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Units;
using Scenes.Battle.Feature.Units.UnitStats;
using Scenes.Battle.Feature.Units.UnitStats.UnitStatSheets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.Battle.Feature.Ui.StatInfoPanel
{
    public class StatInfoPanel : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image[] starImages;
        [SerializeField] private Button closeButton;

        [Header("Body")]
        [SerializeField] private Image illustrationImage;

        [Header("Stats")]
        [SerializeField] private StatCell statCellPrefab;
        [SerializeField] private Transform statGrid;
        [SerializeField] private Transform subStatGrid;

        [Header("Positioning")]
        [SerializeField] private float positionOffsetX = 10f;

        private readonly List<StatCell> _spawnedCells = new();

        private RectTransform _rectTransform;
        private RectTransform _canvasRect;

        // 상단 그리드에 표시할 스탯 (목업 기준)
        private static readonly UnitStatKind[] MainStats =
        {
            UnitStatKind.MaxHealth,
            UnitStatKind.PhysicalAttack,
            UnitStatKind.PhysicalDefense,
            UnitStatKind.MagicAttack,
            UnitStatKind.MagicDefense,
            UnitStatKind.AttackSpeed,
            UnitStatKind.CooldownReduction,
            UnitStatKind.AttackRange,
        };

        // 하단 그리드에 표시할 스탯
        private static readonly UnitStatKind[] SubStats =
        {
            UnitStatKind.MoveSpeed,
            UnitStatKind.CriticalChance,
            UnitStatKind.CriticalDamageMultiplier,
            UnitStatKind.StatusResistance,
            UnitStatKind.DamageDealtIncrease,
            UnitStatKind.DamageReduction,
        };

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

            closeButton.onClick.AddListener(Hide);
            GlobalEventBus.Subscribe<OnObjectSelectedEvent>(OnObjectSelected);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(Hide);
            GlobalEventBus.Unsubscribe<OnObjectSelectedEvent>(OnObjectSelected);
        }

        private void OnObjectSelected(OnObjectSelectedEvent evt)
        {
            if (evt.SelectedObject.TryGetComponent<Units.Unit>(out var unit))
                Show(unit, evt.ScreenPosition);
        }

        public void Show(Units.Unit unit, Vector2 screenPosition)
        {
            gameObject.SetActive(true);

            var def = unit.UnitLoadOutData.Unit;
            nameText.text = def.DisplayName;
            illustrationImage.sprite = def.Illustration;

            // TODO: 유닛에 성급(star) 정보가 추가되면 unit.Star 등으로 교체
            UpdateStars(1);

            BindStats(unit.StatSheet);
            PositionAt(screenPosition);
        }

        public void Hide()
        {
            ClearCells();
            gameObject.SetActive(false);
        }

        private void PositionAt(Vector2 screenPosition)
        {
            // 레이아웃을 즉시 재계산하여 패널 높이를 확정
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

            // 스크린 좌표 → Canvas 로컬 좌표 변환
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPosition, null, out var localPos);

            // anchor (0,0) 기준 오프셋 보정: Canvas 로컬 좌표 → 좌하단 기준 좌표
            var canvasRect = _canvasRect.rect;
            var anchorOffset = new Vector2(
                canvasRect.width * _canvasRect.pivot.x,
                canvasRect.height * _canvasRect.pivot.y);
            var pos = localPos + anchorOffset;

            // 클릭 지점 오른쪽에 배치
            pos.x += positionOffsetX;

            // 하단 잘림 보정: 패널 하단이 화면 밖이면 위로 밀어올림
            float panelHeight = _rectTransform.rect.height;
            float panelBottom = pos.y - panelHeight;
            if (panelBottom < 0f)
                pos.y = panelHeight;

            _rectTransform.anchoredPosition = pos;
        }

        private void BindStats(UnitStatSheet statSheet)
        {
            ClearCells();

            foreach (var kind in MainStats)
                SpawnCell(statGrid, kind, statSheet.Get(kind));

            foreach (var kind in SubStats)
                SpawnCell(subStatGrid, kind, statSheet.Get(kind));
        }

        private void SpawnCell(Transform parent, UnitStatKind kind, UnitStat stat)
        {
            var cell = Instantiate(statCellPrefab, parent);
            cell.Bind(kind, stat);
            _spawnedCells.Add(cell);
        }

        private void ClearCells()
        {
            foreach (var cell in _spawnedCells)
            {
                if (cell != null)
                    Destroy(cell.gameObject);
            }
            _spawnedCells.Clear();
        }

        private void UpdateStars(int count)
        {
            for (int i = 0; i < starImages.Length; i++)
                starImages[i].gameObject.SetActive(i < count);
        }
    }
}
