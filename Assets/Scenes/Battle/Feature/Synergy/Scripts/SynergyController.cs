// ─────────────────────────────────────────────
// SynergyController: 시너지별 효과 적용 행동을 정의하는 추상 클래스.
// GlobalEventBus를 직접 구독하여 Defender 배치 변경 및 판매에 반응한다.
// 자기 시너지의 전장 Defender 목록, 카운트·티어 재계산, SSE 부여/해제를 일원 관리한다.
// 새 시너지 추가 시 이 클래스를 상속하는 구체 Controller를 만들고 SynergyControllerFactory에 등록한다.
// ─────────────────────────────────────────────
using System;
using System.Collections.Generic;
using Common.Data.Synergies;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.StatusEffect;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Unit.Defenders;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// 시너지별 효과 적용 행동을 정의하는 추상 클래스.
    /// GlobalEventBus를 구독하여 Defender 배치 변경과 판매에 자체적으로 반응하고,
    /// 카운트·티어 재계산 및 SSE 부여/해제를 일원 관리한다.
    /// </summary>
    public abstract class SynergyController : IDisposable
    {
        /// <summary>이 시너지의 카운트·티어 상태.</summary>
        protected SynergyActivation Activation { get; }

        /// <summary>이 시너지의 정의 데이터.</summary>
        protected SynergyDefinitionData Definition => Activation.Definition;

        /// <summary>이 시너지에 해당하는 전장 Defender 목록.</summary>
        private readonly List<Defender> _synergyDefenders = new();

        /// <summary>SSE가 부여된 Defender와 해당 SSE 인스턴스의 매핑. 퇴장 시 RemoveImmediate에 사용한다.</summary>
        private readonly Dictionary<Defender, SynergyStatusEffect> _appliedDefenders = new();

        protected SynergyController(SynergyActivation activation)
        {
            Activation = activation;
            Activation.OnTierActivated += HandleTierActivated;
            Activation.OnTierDeactivated += HandleTierDeactivated;
            GlobalEventBus.Subscribe<OnDefenderPlacementChangedEventDto>(HandlePlacementChanged);
            GlobalEventBus.Subscribe<OnDefenderChangedEventDto>(HandleDefenderChanged);
        }

        /// <summary>이벤트 구독을 해제한다. SynergyManager.OnDisable에서 호출한다.</summary>
        public void Dispose()
        {
            Activation.OnTierActivated -= HandleTierActivated;
            Activation.OnTierDeactivated -= HandleTierDeactivated;
            GlobalEventBus.Unsubscribe<OnDefenderPlacementChangedEventDto>(HandlePlacementChanged);
            GlobalEventBus.Unsubscribe<OnDefenderChangedEventDto>(HandleDefenderChanged);
        }

        /// <summary>이 시너지에 해당하는 SSE 인스턴스를 생성한다.</summary>
        protected abstract SynergyStatusEffect CreateSynergyStatusEffect();

        // ── 티어 이벤트 핸들러 ──

        /// <summary>시너지가 활성화될 때 호출된다.</summary>
        private void HandleTierActivated(SynergyTier tier)
        {
            ApplySSEToDefenders(_synergyDefenders);
        }

        /// <summary>시너지가 비활성화될 때 호출된다.</summary>
        private void HandleTierDeactivated()
        {
            // SSE는 ActiveTier 구독으로 자체 제거되므로, 추적만 초기화한다.
            _appliedDefenders.Clear();
            OnAfterDeactivated();
        }

        // ── Defender 이벤트 핸들러 ──

        /// <summary>Defender 배치 변경 시 호출된다.</summary>
        protected virtual void HandlePlacementChanged(OnDefenderPlacementChangedEventDto dto)
        {
            if (dto.defender.HasSynergy(Definition))
            {
                if (dto.placement == Placement.BattleArea)
                {
                    _synergyDefenders.Add(dto.defender);
                    Activation.Recalculate(CountUnique(_synergyDefenders));
                    ApplySSEToDefender(dto.defender);
                }
                else
                {
                    _synergyDefenders.Remove(dto.defender);
                    RemoveSSEFromDefender(dto.defender);
                    Activation.Recalculate(CountUnique(_synergyDefenders));
                }
            }
        }

        /// <summary>Defender 판매(Despawn) 시 호출된다.</summary>
        protected virtual void HandleDefenderChanged(OnDefenderChangedEventDto dto)
        {
            if (dto.Change == DefenderChanges.Despawn && _synergyDefenders.Remove(dto.Defender))
            {
                RemoveSSEFromDefender(dto.Defender);
                Activation.Recalculate(CountUnique(_synergyDefenders));
            }
        }

        // ── SSE 부여/해제 ──

        /// <summary>
        /// 활성 상태이고 미적용 Defender에 SSE를 부여한다.
        /// 이미 적용된 Defender는 건너뛴다.
        /// </summary>
        private void ApplySSEToDefender(Defender defender)
        {
            if (Activation.ActiveTier.Value.HasValue && !_appliedDefenders.ContainsKey(defender))
            {
                SynergyStatusEffect effect = CreateSynergyStatusEffect();
                var context = new SynergyStatusEffectContext(Activation, Definition, defender);
                defender.StatusEffectController.Apply(effect, context);
                _appliedDefenders[defender] = effect;
            }
        }

        /// <summary>복수 Defender에 SSE를 일괄 부여한다.</summary>
        private void ApplySSEToDefenders(List<Defender> defenders)
        {
            foreach (Defender defender in defenders)
            {
                ApplySSEToDefender(defender);
            }
        }

        /// <summary>퇴장한 Defender에서 SSE를 즉시 제거한다.</summary>
        private void RemoveSSEFromDefender(Defender defender)
        {
            if (_appliedDefenders.Remove(defender, out SynergyStatusEffect sse))
            {
                defender.StatusEffectController.RemoveImmediate(sse);
            }
        }

        // ── 확장점 ──

        /// <summary>
        /// 시너지가 비활성화될 때 호출되는 자식 확장점.
        /// 추가 추적 상태 초기화가 필요한 구체 Controller가 오버라이드한다.
        /// </summary>
        protected virtual void OnAfterDeactivated() { }

        // ── 유틸리티 ──

        /// <summary>UnitDefinitionData.ID 기준으로 중복을 제거한 유니크 유닛 수를 반환한다.</summary>
        private int CountUnique(List<Defender> defenders)
        {
            if (defenders.Count == 0)
            {
                return 0;
            }

            HashSet<int> uniqueIds = new HashSet<int>();
            foreach (Defender defender in defenders)
            {
                uniqueIds.Add(defender.UnitLoadOutData.Unit.ID);
            }
            return uniqueIds.Count;
        }
    }
}
