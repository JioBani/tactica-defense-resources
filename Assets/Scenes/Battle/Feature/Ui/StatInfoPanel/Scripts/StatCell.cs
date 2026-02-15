using Common.Data.Units.UnitStatsByLevel;
using Scenes.Battle.Feature.Units.UnitStats;
using TMPro;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.StatInfoPanel
{
    public class StatCell : MonoBehaviour
    {
        [SerializeField] private TMP_Text statLabel;
        [SerializeField] private TMP_Text statValue;

        public void Bind(UnitStatKind kind, UnitStat stat)
        {
            statLabel.text = UnitStatKindHelper.GetDisplayName(kind);
            statValue.text = FormatValue(kind, stat.CurrentValue);
        }

        private static string FormatValue(UnitStatKind kind, float value)
        {
            return kind switch
            {
                UnitStatKind.CriticalChance or
                UnitStatKind.CooldownReduction or
                UnitStatKind.StatusResistance or
                UnitStatKind.DamageDealtIncrease or
                UnitStatKind.DamageReduction => $"{value * 100f:F0}%",
                UnitStatKind.AttackSpeed => $"{value:F2}",
                _ => $"{value:F0}"
            };
        }
    }
}
