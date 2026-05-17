using System.Collections.Generic;
using UnityEngine;

namespace Common.Data.Synergies
{
    /// <summary>분배 대상 SummonTrait 자산 목록을 보관하는 레지스트리 ScriptableObject.</summary>
    [CreateAssetMenu(menuName = "SummonTrait/SummonTraitRegistry", fileName = "SummonTraitRegistry")]
    public class SummonTraitRegistry : ScriptableObject
    {
        /// <summary>분배 대상 SummonTrait 자산 배열. 인스펙터에서 TACD-300 자산 8개를 연결한다.</summary>
        [SerializeField] private SynergyDefinitionData[] traits = {};

        /// <summary>분배 대상 특성 목록.</summary>
        public IReadOnlyList<SynergyDefinitionData> Traits => traits;
    }
}
