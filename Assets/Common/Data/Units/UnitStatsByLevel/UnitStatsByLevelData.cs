using System;
using System.Collections.Generic;
using Common.Scripts.InspectorHint;
using UnityEngine;
using UnityEngine.Serialization;

namespace Common.Data.Units.UnitStatsByLevel
{
    // ──────────────────────────────────────────────────────────────
    // enum
    // ──────────────────────────────────────────────────────────────
    public enum UnitStatKind
    {
        [InspectorName("체력")]                  MaxHealth,
        [InspectorName("물리공격력")]            PhysicalAttack,
        [InspectorName("마법공격력")]            MagicAttack,
        [InspectorName("물리방어력")]            PhysicalDefense,
        [InspectorName("마법방어력")]            MagicDefense,
        [InspectorName("공격속도")]              AttackSpeed,
        [InspectorName("사거리")]                AttackRange,
        [InspectorName("이동속도")]              MoveSpeed,
        [InspectorName("치명타 확률")]            CriticalChance,
        [InspectorName("치명타 피해 배수")]       CriticalDamageMultiplier,
        [InspectorName("스킬 쿨타임 감소")]       CooldownReduction,
        [InspectorName("상태저항력")]            StatusResistance,
        [InspectorName("입히는 피해 증가")]       DamageDealtIncrease,
        [InspectorName("받는 피해 감소")]         DamageReduction,
    }

    // ──────────────────────────────────────────────────────────────
    // ScriptableObject
    // ──────────────────────────────────────────────────────────────
    [CreateAssetMenu(fileName = "UnitStatsByLevelData",
                     menuName = "Game/Unit/Unit Stats By Star Level", order = 0)]
    public class UnitStatsByLevelData : ScriptableObject
    {
        [Header("기본 능력치 (별 단계별)")]
        [InspectorHint("체력", InspectorHintPlacement.Right, 100)]
        [SerializeField] private StarStatRecord maxHealth;
        public StarStatRecord MaxHealth => maxHealth;

        [InspectorHint("물리공격력", InspectorHintPlacement.Right, 100)]
        [FormerlySerializedAs("physicalAttack")]
        [SerializeField] private StarStatRecord physicalAttack;
        public StarStatRecord PhysicalAttack => physicalAttack;

        [InspectorHint("마법공격력", InspectorHintPlacement.Right, 100)]
        [FormerlySerializedAs("magicAttack")]
        [SerializeField] private StarStatRecord magicAttack;
        public StarStatRecord MagicAttack => magicAttack;

        [InspectorHint("물리방어력", InspectorHintPlacement.Right, 100)]
        [FormerlySerializedAs("physicalDefense")]
        [SerializeField] private StarStatRecord physicalDefense;
        public StarStatRecord PhysicalDefense => physicalDefense;

        [InspectorHint("마법방어력", InspectorHintPlacement.Right, 100)]
        [FormerlySerializedAs("magicDefense")]
        [SerializeField] private StarStatRecord magicDefense;
        public StarStatRecord MagicDefense => magicDefense;

        [InspectorHint("공격속도", InspectorHintPlacement.Right, 100)]
        [FormerlySerializedAs("attackSpeedAPS")]
        [SerializeField] private StarStatRecord attackSpeed;
        public StarStatRecord AttackSpeed => attackSpeed;

        [InspectorHint("사거리", InspectorHintPlacement.Right, 100)]
        [FormerlySerializedAs("attackRangeTiles")]
        [SerializeField] private StarStatRecord attackRange;
        public StarStatRecord AttackRange => attackRange;

        [InspectorHint("이동속도", InspectorHintPlacement.Right, 100)]
        [FormerlySerializedAs("moveSpeedTilesPerSec")]
        [SerializeField] private StarStatRecord moveSpeed;
        public StarStatRecord MoveSpeed => moveSpeed;

        [Header("치명/쿨감/저항/피해증가 (비율은 0~1 권장)")]
        [InspectorHint("치명타 확률(%)", InspectorHintPlacement.Right, 100)]
        [FormerlySerializedAs("criticalChance")]
        [SerializeField] private StarStatRecord criticalChance;
        public StarStatRecord CriticalChance => criticalChance;

        [InspectorHint("치명타 피해 배수", InspectorHintPlacement.Right, 100)]
        [FormerlySerializedAs("criticalDamageMultiplier")]
        [SerializeField] private StarStatRecord criticalDamageMultiplier;
        public StarStatRecord CriticalDamageMultiplier => criticalDamageMultiplier;

        [InspectorHint("스킬 쿨타임 감소", InspectorHintPlacement.Right, 100)]
        [FormerlySerializedAs("cooldownReduction")]
        [SerializeField] private StarStatRecord cooldownReduction;
        public StarStatRecord CooldownReduction => cooldownReduction;

        [InspectorHint("상태저항력", InspectorHintPlacement.Right, 100)]
        [FormerlySerializedAs("statusResistance")]
        [SerializeField] private StarStatRecord statusResistance;
        public StarStatRecord StatusResistance => statusResistance;

        [InspectorHint("입히는 피해 증가", InspectorHintPlacement.Right, 100)]
        [FormerlySerializedAs("damageDealtIncrease")]
        [SerializeField] private StarStatRecord damageDealtIncrease;
        public StarStatRecord DamageDealtIncrease => damageDealtIncrease;

        [InspectorHint("받는 피해 감소", InspectorHintPlacement.Right, 100)]
        [SerializeField] private StarStatRecord damageReduction;
        public StarStatRecord DamageReduction => damageReduction;

        // enum으로 조회 (필요 시)
        public float GetStat(UnitStatKind kind, int star) => kind switch
        {
            UnitStatKind.MaxHealth                => maxHealth.GetValue(star),
            UnitStatKind.PhysicalAttack           => physicalAttack.GetValue(star),
            UnitStatKind.MagicAttack              => magicAttack.GetValue(star),
            UnitStatKind.PhysicalDefense          => physicalDefense.GetValue(star),
            UnitStatKind.MagicDefense             => magicDefense.GetValue(star),
            UnitStatKind.AttackSpeed              => attackSpeed.GetValue(star),
            UnitStatKind.AttackRange              => attackRange.GetValue(star),
            UnitStatKind.MoveSpeed                => moveSpeed.GetValue(star),
            UnitStatKind.CriticalChance           => criticalChance.GetValue(star),
            UnitStatKind.CriticalDamageMultiplier => criticalDamageMultiplier.GetValue(star),
            UnitStatKind.CooldownReduction        => cooldownReduction.GetValue(star),
            UnitStatKind.StatusResistance         => statusResistance.GetValue(star),
            UnitStatKind.DamageDealtIncrease      => damageDealtIncrease.GetValue(star),
            UnitStatKind.DamageReduction          => damageReduction.GetValue(star),
            _ => 0f
        };
    }

    // ──────────────────────────────────────────────────────────────
    // StarStatRecord: 내부 필드에도 항상 보이는 힌트 추가
    // ──────────────────────────────────────────────────────────────
    [Serializable]
    public struct StarStatRecord
    {
        [Tooltip("1성부터 순서대로 입력하는 값 리스트 (예: 1~3성 값)")]
        [SerializeField] private List<float> baseValuesByStar;

        [Tooltip("기록된 마지막 별 이후(예: 3성 초과) 매 성마다 더해질 고정 증가치")]
        [SerializeField] private float additionalPerExtraStar;

        public IReadOnlyList<float> BaseValuesByStar => baseValuesByStar;
        public float AdditionalPerExtraStar => additionalPerExtraStar;

        public bool HasAnyValue => baseValuesByStar != null && baseValuesByStar.Count > 0;

        /// <summary>
        /// star(1부터)에 해당하는 값을 반환.
        /// - 리스트에 값이 없으면 0
        /// - star가 리스트 길이를 초과하면: 마지막값 + (초과별 * 추가증가치)
        /// </summary>
        public float GetValue(int star)
        {
            if (!HasAnyValue) return 0f;
            if (star < 1) star = 1;

            int count = baseValuesByStar.Count;
            if (star <= count) return baseValuesByStar[star - 1];

            int extra = star - count;
            return baseValuesByStar[count - 1] + additionalPerExtraStar * extra;
        }
    }
}


