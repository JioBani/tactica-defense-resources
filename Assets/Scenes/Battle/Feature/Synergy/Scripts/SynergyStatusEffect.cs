// ─────────────────────────────────────────────
// SynergyStatusEffect: StatusEffect를 상속하는 시너지 전용 SE 추상 클래스.
// ActiveTier를 구독하여 티어 변경/비활성화에 자체적으로 반응한다.
// 구체 시너지 구현체는 OnSynergyActivated/OnSynergyDeactivated/OnSynergyTierChanged를 오버라이드한다.
// TContext 제네릭으로 확장 Context를 타입 안전하게 사용할 수 있다.
// ─────────────────────────────────────────────
using System;
using Common.Data.StatusEffects;
using Common.Data.Synergies;
using Common.Scripts.StatusEffect;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// 시너지 전용 SE의 비제네릭 기본 클래스. SynergyController가 생성하여 Defender에 부여한다.
    /// </summary>
    public abstract class SynergyStatusEffect : StatusEffect
    {
        protected SynergyStatusEffect(StatusEffectDefinitionData definition) : base(definition) { }
    }

    /// <summary>
    /// StatusEffect를 상속하는 시너지 전용 SE 추상 클래스.
    /// SynergyActivation.ActiveTier를 구독하여 티어 변경과 비활성화에 자체적으로 반응한다.
    /// </summary>
    /// <typeparam name="TContext">SynergyStatusEffectContext 또는 그 서브클래스.</typeparam>
    public abstract class SynergyStatusEffect<TContext> : SynergyStatusEffect
        where TContext : SynergyStatusEffectContext
    {
        protected SynergyStatusEffect(StatusEffectDefinitionData definition) : base(definition) { }
        /// <summary>이 효과가 속한 시너지 정의 데이터.</summary>
        protected SynergyDefinitionData Definition { get; private set; }

        /// <summary>이 시너지의 카운트·티어 상태.</summary>
        protected SynergyActivation Activation { get; private set; }

        /// <summary>Apply 시 전달된 Context. 서브클래스가 확장 데이터에 접근할 때 사용.</summary>
        protected TContext SynergyContext { get; private set; }

        /// <summary>SE가 Defender에 부여될 때 호출된다.</summary>
        public override void OnApply(StatusEffectContext context)
        {
            SynergyContext = (TContext)context;
            Definition = SynergyContext.Definition;
            Activation = SynergyContext.Activation;

            Activation.ActiveTier.OnChange += OnActiveTierChanged;

            if (Activation.ActiveTier.Value.HasValue)
                OnSynergyActivated(Activation.ActiveTier.Value.Value);
        }

        /// <summary>SE가 제거될 때 호출된다.</summary>
        public override void OnRemove()
        {
            // 외부에서 RemoveImmediate로 제거된 경우(시너지가 아직 활성 상태),
            // 스탯 수정자를 정리한다.
            if (Activation.ActiveTier.Value.HasValue)
            {
                OnSynergyDeactivated();
            }

            Activation.ActiveTier.OnChange -= OnActiveTierChanged;
        }

        /// <summary>ActiveTier 변경 시 호출. 비활성화면 자기만료, 아니면 티어 변경 콜백.</summary>
        private void OnActiveTierChanged(SynergyTier? newTier)
        {
            if (newTier == null)
            {
                OnSynergyDeactivated();
                RequestRemove();
            }
            else
            {
                OnSynergyTierChanged(newTier.Value);
            }
        }

        /// <summary>시너지가 활성화될 때 호출된다.</summary>
        protected virtual void OnSynergyActivated(SynergyTier tier) { }

        /// <summary>시너지가 비활성화될 때 호출된다.</summary>
        protected virtual void OnSynergyDeactivated() { }

        /// <summary>시너지가 활성 상태에서 티어가 변경될 때 호출된다.</summary>
        protected virtual void OnSynergyTierChanged(SynergyTier newTier) { }
    }
}
