// ─────────────────────────────────────────────
// SynergyStatusEffectFactory: SynergyId에 따라 SynergyStatusEffect 인스턴스를 생성한다.
// 새 시너지 효과 추가 시 switch 분기를 추가한다.
// ─────────────────────────────────────────────
using System;
using Common.Data.Synergies;
using Scenes.Battle.Feature.Synergy.SynergyEffects;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// SynergyId에 따라 SynergyStatusEffect 인스턴스를 생성한다.
    /// 새 시너지 효과 추가 시 switch 분기를 추가한다.
    /// </summary>
    public class SynergyStatusEffectFactory
    {
        /// <summary>
        /// 시너지 ID에 해당하는 SSE를 생성한다.
        /// 효과가 구현되지 않은 시너지는 예외를 던진다.
        /// </summary>
        public SynergyStatusEffect Create(SynergyId id)
        {
            return id switch
            {
                SynergyId.Bruiser => new BruiserSynergyStatusEffect(),
                // _ => throw new ArgumentException(
                //     $"시너지 '{id}'에 대한 SSE가 구현되지 않았습니다.")
                _ =>  new BruiserSynergyStatusEffect()
            };
        }
    }
}