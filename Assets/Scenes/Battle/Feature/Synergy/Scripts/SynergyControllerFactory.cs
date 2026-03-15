// ─────────────────────────────────────────────
// SynergyControllerFactory: SynergyId에 따라 SynergyController 인스턴스를 생성한다.
// 새 시너지 추가 시 switch 분기를 추가한다.
// ─────────────────────────────────────────────
using Common.Data.StatusEffects;
using Common.Data.Synergies;
using Common.Scripts.SceneSingleton;
using Scenes.Battle.Feature.Synergy.SynergyControllers;
using Scenes.Battle.Feature.Unit.Defenders;
using UnityEngine;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// SynergyId에 따라 SynergyController 인스턴스를 생성한다.
    /// 새 시너지 추가 시 switch 분기를 추가한다.
    /// </summary>
    public class SynergyControllerFactory : SceneSingleton<SynergyControllerFactory>
    {
        [SerializeField] private DefenderManager defenderManager;

        [Header("부수 SE 정의")]
        [Tooltip("비전 마법사 주문력 부수 효과의 SE 정의 데이터")]
        [SerializeField] private StatusEffectDefinitionData arcanistSpellPowerDefinition;

        /// <summary>
        /// 시너지 ID에 해당하는 SynergyController를 생성한다.
        /// </summary>
        public SynergyController Create(SynergyActivation activation)
        {
            return activation.Definition.Id switch
            {
                SynergyId.Bruiser => new BruiserSynergyController(activation),
                SynergyId.Arcanist => new ArcanistSynergyController(
                    activation, defenderManager, arcanistSpellPowerDefinition),
                // _ => throw new ArgumentException(
                //     $"시너지 '{activation.Definition.Id}'에 대한 SynergyController가 구현되지 않았습니다.")
                _ => new BruiserSynergyController(activation)
            };
        }
    }
}
