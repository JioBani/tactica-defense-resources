using System.Collections.Generic;
using Common.Data.StatusEffects;
using Common.Scripts.SpritePreview;
using UnityEngine;

namespace Common.Data.Synergies
{
    /// <summary>
    /// 개별 시너지를 정의하는 데이터. 시너지 이름, 설명, 종류, 티어별 상수를 포함한다.
    /// 시너지별 구현 클래스(ISynergyEffect)가 상수를 읽어 실제 효과를 적용한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Synergy/SynergyDefinitionData", fileName = "SynergyDefinitionData", order = 0)]
    public class SynergyDefinitionData : ScriptableObject
    {
        [Header("표시 정보 (Identity)")]
        [Tooltip("시너지 ID")]
        [SerializeField] private SynergyId id;

        /// <summary>시너지 고유 ID</summary>
        public SynergyId Id => id;

        [Tooltip("시너지 이름")]
        [SerializeField] private string displayName;

        /// <summary>시너지 표시 이름</summary>
        public string DisplayName => displayName;

        [Tooltip("시너지 설명. @ConstantName@ 또는 @ConstantName*N@ 플레이스홀더를 사용하여 티어별 수치를 표시한다.")]
        [TextArea(3, 6)]
        [SerializeField] private string description;

        /// <summary>시너지 설명 (플레이스홀더 포함 원본)</summary>
        public string Description => description;

        [Tooltip("시너지 아이콘")]
        [SpritePreview(80)]
        [SerializeField] private Sprite icon;

        /// <summary>시너지 아이콘</summary>
        public Sprite Icon => icon;

        [Header("상태 효과")]
        [Tooltip("이 시너지가 부여하는 SE의 정의 데이터")]
        [SerializeField] private StatusEffectDefinitionData statusEffectDefinition;

        /// <summary>이 시너지가 부여하는 SE의 정의 데이터</summary>
        public StatusEffectDefinitionData StatusEffectDefinition => statusEffectDefinition;

        [Header("시너지 설정")]
        [Tooltip("시너지 종류 (소환술사 효과 / 소환수 특성)")]
        [SerializeField] private SynergyType synergyType;

        /// <summary>시너지 종류</summary>
        public SynergyType SynergyType => synergyType;

        [Tooltip("티어 목록. requiredCount 오름차순으로 설정한다.")]
        [SerializeField] private List<SynergyTier> tiers;

        /// <summary>티어 목록</summary>
        public IReadOnlyList<SynergyTier> Tiers => tiers;

    }
}
