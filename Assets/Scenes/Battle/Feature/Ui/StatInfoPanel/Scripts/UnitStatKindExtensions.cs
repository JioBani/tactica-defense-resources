using Common.Data.Units.UnitStatsByLevel;

namespace Scenes.Battle.Feature.Ui.StatInfoPanel
{
    public static class UnitStatKindExtensions
    {
        public static string GetDisplayName(this UnitStatKind kind) => kind switch
        {
            UnitStatKind.MaxHealth => "체력",
            UnitStatKind.PhysicalAttack => "물리공격력",
            UnitStatKind.MagicAttack => "마법공격력",
            UnitStatKind.PhysicalDefense => "물리방어력",
            UnitStatKind.MagicDefense => "마법방어력",
            UnitStatKind.AttackSpeed => "공격속도",
            UnitStatKind.AttackRange => "사거리",
            UnitStatKind.MoveSpeed => "이동속도",
            UnitStatKind.CriticalChance => "치명타 확률",
            UnitStatKind.CriticalDamageMultiplier => "치명타 피해 배수",
            UnitStatKind.CooldownReduction => "스킬 쿨타임 감소",
            UnitStatKind.StatusResistance => "상태저항력",
            UnitStatKind.DamageDealtIncrease => "입히는 피해 증가",
            UnitStatKind.DamageReduction => "받는 피해 감소",
            _ => kind.ToString()
        };

        public static string FormatStatValue(this UnitStatKind kind, float value)
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
