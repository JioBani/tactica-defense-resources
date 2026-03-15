// ─────────────────────────────────────────────
// SynergyController: 시너지별 효과 적용 행동을 정의하는 추상 클래스.
// Template Method 패턴으로 SSE 부여를 보장하고, 자식 클래스가 추가 로직을 확장한다.
// 새 시너지 추가 시 이 클래스를 상속하는 구체 Controller를 만들고 SynergyControllerFactory에 등록한다.
// ─────────────────────────────────────────────
using System.Collections.Generic;
using Common.Data.Synergies;
using Common.Scripts.StatusEffect;
using Scenes.Battle.Feature.Unit.Defenders;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// 시너지별 효과 적용 행동을 정의하는 추상 클래스.
    /// OnActivated가 호출되면 SSE 부여를 보장한 뒤 자식 확장점(OnAfterActivated)을 호출한다.
    /// </summary>
    public abstract class SynergyController
    {
        /// <summary>이 시너지의 카운트·티어 상태.</summary>
        protected SynergyActivation Activation { get; }

        /// <summary>이 시너지의 정의 데이터.</summary>
        protected SynergyDefinitionData Definition => Activation.Definition;

        private readonly HashSet<Defender> _appliedDefenders = new();

        protected SynergyController(SynergyActivation activation)
        {
            Activation = activation;
        }

        /// <summary>이 시너지에 해당하는 SSE 인스턴스를 생성한다.</summary>
        protected abstract SynergyStatusEffect CreateSynergyStatusEffect();

        /// <summary>
        /// 시너지가 활성화될 때 호출된다.
        /// 시너지 보유 Defender에 SSE를 부여한 뒤, 자식 확장점을 호출한다.
        /// </summary>
        public void OnActivated(List<Defender> synergyDefenders)
        {
            ApplySSEToDefenders(synergyDefenders);
            OnAfterActivated(synergyDefenders);
        }

        /// <summary>
        /// SSE 부여 후 호출되는 자식 확장점.
        /// 추가 효과(전체 아군 버프 등)가 필요한 구체 Controller가 오버라이드한다.
        /// </summary>
        protected virtual void OnAfterActivated(List<Defender> synergyDefenders) { }

        /// <summary>
        /// 시너지가 비활성화될 때 호출된다.
        /// 적용 추적을 초기화한다. SSE/SE는 ActiveTier 구독으로 자체 제거된다.
        /// </summary>
        public virtual void OnDeactivated()
        {
            _appliedDefenders.Clear();
        }

        /// <summary>
        /// 미적용 Defender에 SSE를 생성하여 Apply한다.
        /// 이미 적용된 Defender는 건너뛴다.
        /// </summary>
        private void ApplySSEToDefenders(List<Defender> defenders)
        {
            if (defenders == null) return;

            foreach (Defender defender in defenders)
            {
                if (!_appliedDefenders.Add(defender)) continue;

                StatusEffectController controller = defender.StatusEffectController;
                if (controller == null)
                    throw new UnityEngine.MissingComponentException(
                        $"{defender.name}에 StatusEffectController가 없습니다.");

                SynergyStatusEffect effect = CreateSynergyStatusEffect();
                var context = new SynergyStatusEffectContext(Activation, Definition, defender);
                controller.Apply(effect, context);
            }
        }
    }
}