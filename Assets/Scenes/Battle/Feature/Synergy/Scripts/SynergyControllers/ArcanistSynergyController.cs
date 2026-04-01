// ─────────────────────────────────────────────
// ArcanistSynergyController: 비전 마법사 시너지.
// 시너지 보유 유닛에는 ArcanistSSE(높은 주문력), 비보유 유닛에는 ArcanistSpellPowerEffect(낮은 주문력)를 부여한다.
// HandlePlacementChanged/HandleDefenderChanged를 override하여 비시너지 Defender 출입도 처리한다.
// ─────────────────────────────────────────────
using System.Collections.Generic;
using Common.Data.StatusEffects;
using Common.Scripts.StatusEffect;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Synergy.SynergyEffects;
using Scenes.Battle.Feature.Unit.Defenders;

namespace Scenes.Battle.Feature.Synergy.SynergyControllers
{
    /// <summary>
    /// 비전 마법사 시너지 컨트롤러.
    /// 시너지 보유 유닛에는 ArcanistSSE를, 비보유 유닛에는 ArcanistSpellPowerEffect를 부여한다.
    /// </summary>
    public class ArcanistSynergyController : SynergyController
    {
        private readonly StatusEffectDefinitionData _spellPowerEffectDefinition;

        /// <summary>비시너지 Defender와 부여된 SpellPowerEffect 인스턴스의 매핑.</summary>
        private readonly Dictionary<Defender, ArcanistSpellPowerEffect> _appliedNonSynergyDefenders = new();

        public ArcanistSynergyController(
            SynergyActivation activation,
            StatusEffectDefinitionData spellPowerEffectDefinition)
            : base(activation)
        {
            _spellPowerEffectDefinition = spellPowerEffectDefinition;
        }

        /// <summary>ArcanistSynergyStatusEffect 인스턴스를 생성한다.</summary>
        protected override SynergyStatusEffect CreateSynergyStatusEffect()
        {
            return new ArcanistSynergyStatusEffect(Definition.StatusEffectDefinition);
        }

        /// <summary>Defender 배치 변경 시 호출된다. 비시너지 Defender의 SpellPowerEffect도 처리한다.</summary>
        protected override void HandlePlacementChanged(OnDefenderPlacementChangedEventDto dto)
        {
            base.HandlePlacementChanged(dto);

            if (!dto.defender.HasSynergy(Definition) && Activation.ActiveTier.Value.HasValue)
            {
                if (dto.placement == Placement.BattleArea)
                {
                    ApplySpellPowerToDefender(dto.defender);
                }
                else
                {
                    RemoveSpellPowerFromDefender(dto.defender);
                }
            }
        }

        /// <summary>Defender 판매(Despawn) 시 호출된다. 비시너지 Defender의 SpellPowerEffect도 해제한다.</summary>
        protected override void HandleDefenderChanged(OnDefenderChangedEventDto dto)
        {
            base.HandleDefenderChanged(dto);

            if (dto.Change == DefenderChanges.Despawn)
            {
                RemoveSpellPowerFromDefender(dto.Defender);
            }
        }

        /// <summary>비시너지 Defender 추적 상태를 초기화한다.</summary>
        protected override void OnAfterDeactivated()
        {
            _appliedNonSynergyDefenders.Clear();
        }

        /// <summary>비시너지 Defender에 ArcanistSpellPowerEffect를 부여한다. 중복 적용을 방지한다.</summary>
        private void ApplySpellPowerToDefender(Defender defender)
        {
            if (!_appliedNonSynergyDefenders.ContainsKey(defender))
            {
                StatusEffectController controller = defender.StatusEffectController;
                if (controller == null)
                {
                    throw new UnityEngine.MissingComponentException(
                        $"{defender.name}에 StatusEffectController가 없습니다.");
                }

                var effect = new ArcanistSpellPowerEffect(_spellPowerEffectDefinition);
                var context = new TierLinkedStatusEffectContext(Activation, Definition, defender);
                controller.Apply(effect, context);
                _appliedNonSynergyDefenders[defender] = effect;
            }
        }

        /// <summary>비시너지 Defender에서 ArcanistSpellPowerEffect를 즉시 제거한다.</summary>
        private void RemoveSpellPowerFromDefender(Defender defender)
        {
            if (_appliedNonSynergyDefenders.Remove(defender, out ArcanistSpellPowerEffect effect))
            {
                defender.StatusEffectController.RemoveImmediate(effect);
            }
        }
    }
}
