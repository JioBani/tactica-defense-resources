// ─────────────────────────────────────────────
// ArcanistSynergyController: 비전 마법사 시너지.
// 시너지 보유 유닛에는 ArcanistSSE(높은 주문력), 비보유 유닛에는 ArcanistSpellPowerEffect(낮은 주문력)를 부여한다.
// ─────────────────────────────────────────────
using System.Collections.Generic;
using Common.Data.StatusEffects;
using Common.Scripts.StatusEffect;
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
        private readonly DefenderManager _defenderManager;
        private readonly StatusEffectDefinitionData _spellPowerEffectDefinition;
        private readonly HashSet<Defender> _appliedNonSynergyDefenders = new();

        public ArcanistSynergyController(
            SynergyActivation activation,
            DefenderManager defenderManager,
            StatusEffectDefinitionData spellPowerEffectDefinition)
            : base(activation)
        {
            _defenderManager = defenderManager;
            _spellPowerEffectDefinition = spellPowerEffectDefinition;
        }

        /// <summary>ArcanistSynergyStatusEffect 인스턴스를 생성한다.</summary>
        protected override SynergyStatusEffect CreateSynergyStatusEffect()
        {
            return new ArcanistSynergyStatusEffect(Definition.StatusEffectDefinition);
        }

        /// <summary>
        /// SSE 부여 후, 비전 마법사가 아닌 전체 아군에게 ArcanistSpellPowerEffect를 부여한다.
        /// </summary>
        protected override void OnAfterActivated(List<Defender> synergyDefenders)
        {
            List<Defender> allDefenders = _defenderManager.GetBattleAreaDefenders();
            HashSet<Defender> synergySet = new HashSet<Defender>(synergyDefenders);

            foreach (Defender defender in allDefenders)
            {
                if (synergySet.Contains(defender)) continue;
                if (!_appliedNonSynergyDefenders.Add(defender)) continue;

                StatusEffectController controller = defender.StatusEffectController;
                if (controller == null)
                    throw new UnityEngine.MissingComponentException(
                        $"{defender.name}에 StatusEffectController가 없습니다.");

                var effect = new ArcanistSpellPowerEffect(_spellPowerEffectDefinition);
                var context = new TierLinkedStatusEffectContext(Activation, Definition, defender);
                controller.Apply(effect, context);
            }
        }

        /// <summary>추적 상태를 모두 초기화한다.</summary>
        public override void OnDeactivated()
        {
            base.OnDeactivated();
            _appliedNonSynergyDefenders.Clear();
        }
    }
}