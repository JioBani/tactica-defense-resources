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

        private readonly List<StatCell> _spawnedCells = new();

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
                Show(unit);
        }

        public void Show(Units.Unit unit)
        {
            gameObject.SetActive(true);

            var def = unit.UnitLoadOutData.Unit;
            nameText.text = def.DisplayName;
            illustrationImage.sprite = def.Illustration;

            // TODO: 유닛에 성급(star) 정보가 추가되면 unit.Star 등으로 교체
            UpdateStars(1);

            BindStats(unit.StatSheet);
        }

        public void Hide()
        {
            ClearCells();
            gameObject.SetActive(false);
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
