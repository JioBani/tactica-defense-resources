using UnityEngine;

namespace Common.Data.Skills.SkillDefinitions
{
    public class SkillDefinitionData : ScriptableObject
    {
        //TODO: enum 으로 변경
        [Header("표시 정보 (Identity)")]
        [Tooltip("스킬 ID")]
        [SerializeField] private int id;
        /// <summary>스킬 ID</summary>
        public int Id => id;

        [Tooltip("스킬 이름")]
        [SerializeField] private string displayName;
        /// <summary>스킬 표시 이름</summary>
        public string DisplayName => displayName;

        [Tooltip("스킬 설명")]
        [SerializeField] private string description;
        /// <summary>스킬 설명 텍스트</summary>
        public string Description => description;

        [Tooltip("스킬 쿨타임")]
        [SerializeField] private float coolTime;
        public float CoolTime => coolTime;

        [Tooltip("아이콘")]
        [SerializeField] private Sprite icon;
        /// <summary>스킬 아이콘 스프라이트</summary>
        public Sprite Icon => icon;

        /// <summary>스킬 계수 맵. 문자열 키로 각 효과의 계수를 조회한다.</summary>
        [Header("스킬 계수")]
        [SerializeField] private SkillCoefficientMap coefficients = new();
        public SkillCoefficientMap Coefficients => coefficients;
    }
}
