// ─────────────────────────────────────────────
// SynergyEffect: 시너지 동작을 정의하는 추상 클래스.
// 각 시너지 구현체는 필요한 콜백만 오버라이드한다.
// 적용 상태 관리(활성화/비활성화 시점)는 SynergyController가 담당한다.
// ─────────────────────────────────────────────
using Common.Data.Synergies;
using Scenes.Battle.Feature.Synergy.Contexts;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// 시너지 동작을 정의하는 추상 클래스.
    /// 각 시너지 구현체는 필요한 콜백(OnSynergyOn, OnSynergyOff 등)만 오버라이드한다.
    /// </summary>
    public abstract class SynergyEffect
    {
        /// <summary>이 효과가 속한 시너지 정의 데이터.</summary>
        protected readonly SynergyDefinitionData Definition;

        protected SynergyEffect(SynergyDefinitionData definition)
        {
            Definition = definition;
        }

        /// <summary>시너지가 활성화되거나 티어가 변경될 때 호출된다.</summary>
        public virtual void OnSynergyOn(OnSynergyOnContext context) { }

        /// <summary>시너지가 비활성화되거나 티어 변경 전에 호출된다.</summary>
        public virtual void OnSynergyOff(OnSynergyOffContext context) { }

        /// <summary>시너지가 활성 상태에서 티어가 변경될 때 호출된다.</summary>
        public virtual void OnTierChange(OnTierChangeContext context) { }
    }
}