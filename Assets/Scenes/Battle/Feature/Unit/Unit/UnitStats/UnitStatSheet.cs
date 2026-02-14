using System;
using System.Collections.Generic;
using Common.Data.Units.UnitStatsByLevel;
using UnityEngine;

namespace Scenes.Battle.Feature.Units.UnitStats.UnitStatSheets
{
    public class UnitStatSheet
    {
        private UnitStatsByLevelData _data;

        // ── 14종 능력치 (수정자 지원) ──
        public readonly UnitStat MaxHealth = new();
        public readonly UnitStat PhysicalAttack = new();
        public readonly UnitStat MagicAttack = new();
        public readonly UnitStat PhysicalDefense = new(StatCalculationMode.SeparatedMultiplicative);
        public readonly UnitStat MagicDefense = new(StatCalculationMode.SeparatedMultiplicative);
        public readonly UnitStat AttackSpeed = new();
        public readonly UnitStat AttackRange = new();
        public readonly UnitStat MoveSpeed = new();
        public readonly UnitStat CriticalChance = new();
        public readonly UnitStat CriticalDamageMultiplier = new();
        public readonly UnitStat CooldownReduction = new();
        public readonly UnitStat StatusResistance = new();
        public readonly UnitStat DamageDealtIncrease = new();
        public readonly UnitStat DamageReduction = new();

        // ── 현재 체력 (런타임 값, 수정자 대상 아님) ──
        private float _health;

        public float Health
        {
            get => _health;
            set
            {
                float clamped = Mathf.Clamp(value, 0f, MaxHealth.CurrentValue);
                if (Mathf.Approximately(_health, clamped)) return;
                _health = clamped;
                OnHealthChange?.Invoke(_health);
            }
        }

        public event Action<float> OnHealthChange;

        public void Init(UnitStatsByLevelData data, int star = 1)
        {
            _data = data;

            foreach (var (kind, stat) in Enumerate())
            {
                stat.ClearModifiers();
                stat.SetBaseValue(data.GetStat(kind, star));
            }

            // 현재 체력 = 최대 체력
            _health = MaxHealth.CurrentValue;

            // MaxHealth 변경 시 현재 체력 상한 보정
            MaxHealth.OnChange += OnMaxHealthChanged;
        }

        private void OnMaxHealthChanged(float newMax)
        {
            if (_health > newMax) Health = newMax;
        }

        public UnitStat Get(UnitStatKind kind) => kind switch
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

        public IEnumerable<(UnitStatKind kind, UnitStat stat)> Enumerate()
        {
            yield return (UnitStatKind.MaxHealth,                MaxHealth);
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
