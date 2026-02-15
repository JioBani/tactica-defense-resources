using System.Reflection;
using Common.Data.Units.UnitStatsByLevel;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.StatInfoPanel
{
    public static class UnitStatKindHelper
    {
        public static string GetDisplayName(UnitStatKind kind)
        {
            var field = typeof(UnitStatKind).GetField(kind.ToString());
            var attr = field?.GetCustomAttribute<InspectorNameAttribute>();
            return attr?.displayName ?? kind.ToString();
        }

        public static string FormatStatValue(UnitStatKind kind, float value)
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
