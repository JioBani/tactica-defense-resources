// ─────────────────────────────────────────────
// TierLinkedStatusEffect: ActiveTier를 구독하여 생명주기를 자동 관리하는 일반 SE.
// SSE(SynergyStatusEffect)와 달리 시너지 소속 정체성을 나타내지 않는다.
// 시너지의 부수 효과로 버프를 받는 비소속 유닛에 사용한다.
// ─────────────────────────────────────────────
using Common.Data.StatusEffects;
using Common.Data.Synergies;
using Common.Scripts.StatusEffect;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// ActiveTier를 구독하여 생명주기를 자동 관리하는 일반 SE.
    /// SSE가 아니므로 시너지 소속 정체성을 나타내지 않는다.
    /// </summary>
    /// <typeparam name="TContext">TierLinkedStatusEffectContext 또는 그 서브클래스.</typeparam>
    public abstract class TierLinkedStatusEffect<TContext> : StatusEffect
        where TContext : TierLinkedStatusEffectContext
    {
        protected TierLinkedStatusEffect(StatusEffectDefinitionData definition) : base(definition) { }
        /// <summary>이 효과가 속한 시너지 정의 데이터.</summary>
        protected SynergyDefinitionData Definition { get; private set; }

        /// <summary>이 시너지의 카운트·티어 상태.</summary>
        protected SynergyActivation Activation { get; private set; }

        /// <summary>Apply 시 전달된 Context.</summary>
        protected TContext TierContext { get; private set; }

        /// <summary>
        /// Context에서 SynergyActivation을 수신하고, ActiveTier를 구독한 뒤 활성화 콜백을 호출한다.
        /// </summary>
        public override void OnApply(StatusEffectContext context)
        {
            TierContext = (TContext)context;
            Definition = TierContext.Definition;
            Activation = TierContext.Activation;

            Activation.ActiveTier.OnChange += OnActiveTierChanged;

            if (Activation.ActiveTier.Value.HasValue)
                OnTierActivated(Activation.ActiveTier.Value.Value);
        }

        /// <summary>ActiveTier 구독을 해제한다.</summary>
        public override void OnRemove()
        {
            Activation.ActiveTier.OnChange -= OnActiveTierChanged;
        }

        /// <summary>ActiveTier 변경 시 호출. 비활성화면 자기만료, 아니면 티어 변경 콜백.</summary>
        private void OnActiveTierChanged(SynergyTier? newTier)
        {
            if (newTier == null)
            {
                OnTierDeactivated();
                RequestRemove();
            }
            else
            {
                OnTierChanged(newTier.Value);
            }
        }

        /// <summary>시너지가 활성화될 때 호출된다. 현재 티어의 상수로 효과를 적용한다.</summary>
        protected virtual void OnTierActivated(SynergyTier tier) { }

        /// <summary>시너지가 비활성화될 때 호출된다. 적용한 효과를 해제한다.</summary>
        protected virtual void OnTierDeactivated() { }

        /// <summary>시너지가 활성 상태에서 티어가 변경될 때 호출된다.</summary>
        protected virtual void OnTierChanged(SynergyTier newTier) { }
    }
}