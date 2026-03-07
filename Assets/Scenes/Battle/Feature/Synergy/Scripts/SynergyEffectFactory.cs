// ─────────────────────────────────────────────
// SynergyEffectFactory: SynergyDefinitionData.Id에 따라 SynergyEffect 인스턴스를 생성한다.
// 새 시너지 효과 추가 시 switch 분기를 추가한다.
// ─────────────────────────────────────────────
using Common.Data.Synergies;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// SynergyDefinitionData.Id에 따라 SynergyEffect 인스턴스를 생성한다.
    /// 매칭되는 효과가 없으면 null을 반환한다.
    /// </summary>
    public class SynergyEffectFactory
    {
        /// <summary>
        /// 시너지 정의에 해당하는 SynergyEffect를 생성한다.
        /// 효과가 구현되지 않은 시너지는 null을 반환한다.
        /// </summary>
        public SynergyEffect Create(SynergyDefinitionData definition)
        {
            return definition.Id switch
            {
                // TODO: 시너지별 효과 구현 시 분기 추가
                // 예: 1 => new FireSynergyEffect(definition),
                _ => null
            };
        }
    }
}