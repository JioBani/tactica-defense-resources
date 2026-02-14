using System.Collections.Generic;
using Common.Data.Units.UnitStatsByLevel;

// 별 레벨에 따라 다시 계산 필요한 경우 설계가 변경되는지 확인 필요
namespace Scenes.Battle.Feature.Units.UnitStats.UnitStatSheets
{
    /// <summary>
    /// 유닛의 런타임 스탯 묶음(옵저버블).
    /// - 생성자에서 UnitStatsByLevelData와 star(기본 1)를 받아 초기화
    /// - 별 변화 시 ApplyStar(star)로 재적용 가능
    /// - enum으로 스탯 접근: Get(UnitStatKind)
    /// </summary>
    public class UnitStatSheet
    {
        // 원본 데이터(별 단계별 기본값)
        private UnitStatsByLevelData _data;

        // 개별 스탯(옵저버블)
        public readonly UnitStats<float> MaxHealth = new();
        public readonly UnitStats<float> Health = new();
        public readonly UnitStats<float> PhysicalAttack = new();
        public readonly UnitStats<float> MagicAttack = new();
        public readonly UnitStats<float> PhysicalDefense = new();
        public readonly UnitStats<float> MagicDefense = new();
        public readonly UnitStats<float> AttackSpeed = new();
        public readonly UnitStats<float> AttackRange = new();
        public readonly UnitStats<float> MoveSpeed = new();

        public readonly UnitStats<float> CriticalChance = new();
        public readonly UnitStats<float> CriticalDamageMultiplier = new();
        public readonly UnitStats<float> CooldownReduction = new();
        public readonly UnitStats<float> StatusResistance = new();
        public readonly UnitStats<float> DamageDealtIncrease = new();
        public readonly UnitStats<float> DamageReduction = new();

        public void Init(UnitStatsByLevelData data, int star = 1)
        {
            _data = data;
            foreach (var stat in Enumerate())
            {
                stat.stat.SetInitValue(data.GetStat(stat.kind , 0));
            }
        }

        /// <summary>
        /// enum으로 해당 스탯(UnitStats<float>)을 얻는다.
        /// </summary>
        public UnitStats<float> Get(UnitStatKind kind) => kind switch
        {
            UnitStatKind.MaxHealth                => MaxHealth,
            UnitStatKind.PhysicalAttack           => PhysicalAttack,
            UnitStatKind.MagicAttack              => MagicAttack,
            UnitStatKind.PhysicalDefense          => PhysicalDefense,
            UnitStatKind.MagicDefense             => MagicDefense,
            UnitStatKind.AttackSpeed              => AttackSpeed,
            UnitStatKind.AttackRange              => AttackRange,
            UnitStatKind.MoveSpeed                => MoveSpeed,
            UnitStatKind.CriticalChance           => CriticalChance,
            UnitStatKind.CriticalDamageMultiplier => CriticalDamageMultiplier,
            UnitStatKind.CooldownReduction        => CooldownReduction,
            UnitStatKind.StatusResistance         => StatusResistance,
            UnitStatKind.DamageDealtIncrease      => DamageDealtIncrease,
            UnitStatKind.DamageReduction          => DamageReduction,
            _ => null
        };

        /// <summary>
        /// 모든 스탯을 열거(편의).
        /// </summary>
        public IEnumerable<(UnitStatKind kind, UnitStats<float> stat)> Enumerate()
        {
            yield return (UnitStatKind.MaxHealth,                MaxHealth);
            yield return (UnitStatKind.MaxHealth,                Health);
            yield return (UnitStatKind.PhysicalAttack,           PhysicalAttack);
            yield return (UnitStatKind.MagicAttack,              MagicAttack);
            yield return (UnitStatKind.PhysicalDefense,          PhysicalDefense);
            yield return (UnitStatKind.MagicDefense,             MagicDefense);
            yield return (UnitStatKind.AttackSpeed,              AttackSpeed);
            yield return (UnitStatKind.AttackRange,              AttackRange);
            yield return (UnitStatKind.MoveSpeed,                MoveSpeed);
            yield return (UnitStatKind.CriticalChance,           CriticalChance);
            yield return (UnitStatKind.CriticalDamageMultiplier, CriticalDamageMultiplier);
            yield return (UnitStatKind.CooldownReduction,        CooldownReduction);
            yield return (UnitStatKind.StatusResistance,         StatusResistance);
            yield return (UnitStatKind.DamageDealtIncrease,      DamageDealtIncrease);
            yield return (UnitStatKind.DamageReduction,          DamageReduction);
        }
    }
}
