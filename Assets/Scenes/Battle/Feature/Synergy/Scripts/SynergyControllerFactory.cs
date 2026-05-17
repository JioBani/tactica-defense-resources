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
            // 테스트용 통합 라우팅 — 실제 효과 SummonTrait이 추가되면 SummonerEffect와 동일하게
            // 아래 SynergyId switch에 시너지별 케이스로 옮긴다 (SummonTraitSynergyController 폐기 또는 축소).
            if (activation.Definition.SynergyType == SynergyType.SummonTrait)
            {
                return new SummonTraitSynergyController(activation);
            }

            return activation.Definition.Id switch
            {
                SynergyId.Bruiser => new BruiserSynergyController(activation),       // 난동꾼: MaxHealth % 버프
                SynergyId.Arcanist => new ArcanistSynergyController(               // 비전 마법사: 아군 주문력 버프
                    activation, arcanistSpellPowerDefinition),
                SynergyId.Freljord => new FreljordSynergyController(activation),   // 프렐요드: 공격 시 대상 둔화
                SynergyId.Warmonger => new WarmongerSynergyController(          // 전쟁기계: 조건부 피해감소 + 사망 시 회복
                    activation, defenderManager),
                SynergyId.Gunslinger => new GunslingerSynergyController(activation), // 총잡이: 공격력 % + 4회 공격 추가 피해
                // _ => throw new ArgumentException(
                //     $"시너지 '{activation.Definition.Id}'에 대한 SynergyController가 구현되지 않았습니다.")
                _ => new BruiserSynergyController(activation)
            };
        }
    }
}
