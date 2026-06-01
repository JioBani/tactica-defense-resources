using Common.Scripts.SpritePreview;
using UnityEngine;

namespace Common.Data.Units.RoleGroups
{
    /// <summary>
    /// 한 역할군의 표시 이름과 고유 아이콘을 정의하는 데이터.
    /// 역할군은 4종(탱커·평타 딜러·스킬 딜러·서포터) 고정이며, 각 역할군마다 자산 하나를 만든다.
    /// 회복·보조 등 전투 행동 수치는 보유하지 않는다(서포터 효과는 스킬 시스템 책임).
    /// </summary>
    [CreateAssetMenu(menuName = "Units/RoleGroupDefinitionData", fileName = "RoleGroupDefinitionData", order = 0)]
    public class RoleGroupDefinitionData : ScriptableObject
    {
        [Header("표시 정보 (Identity)")]
        [Tooltip("역할군 ID")]
        [SerializeField] private RoleGroup roleGroup;

        /// <summary>역할군 고유 식별자</summary>
        public RoleGroup RoleGroup => roleGroup;

        [Tooltip("역할군 표시 이름")]
        [SerializeField] private string displayName;

        /// <summary>역할군 표시 이름</summary>
        public string DisplayName => displayName;

        [Tooltip("역할군 아이콘")]
        [SpritePreview(80)]
        [SerializeField] private Sprite icon;

        /// <summary>역할군 고유 아이콘</summary>
        public Sprite Icon => icon;
    }
}
