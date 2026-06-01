using System.Collections.Generic;
using Common.Data.Synergies;
using Common.Data.Units.RoleGroups;
using Common.Scripts.SpritePreview;
using UnityEngine;

namespace Common.Data.Units.UnitDefinitions
{
    [CreateAssetMenu( menuName = "Units/UnitDefinitionData", fileName = "UnitDefinitionData", order = 0)]
    public class UnitDefinitionData : ScriptableObject
    {
        //TODO: enum 으로 변경
        [Header("표시 정보 (Identity)")]
        [Tooltip("유닛 ID")]
        [SerializeField] private int id;
        public int ID => id;
        
        [Tooltip("게임 내에 표시될 유닛 이름")]
        [SerializeField] private string displayName = "New Unit";
        public string DisplayName => displayName;
        
        [Tooltip("유닛 설명")]
        [SerializeField] private string description;
        public string Description => description;
        
        [Tooltip("비용")]
        [SerializeField] private int cost;
        public int Cost => cost;

        [Tooltip("UI 등에서 사용할 유닛 아이콘")]
        [SpritePreview(80)]
        [SerializeField] private Sprite icon;
        public Sprite Icon => icon;
        
        [Tooltip("일러스트")]
        [SpritePreview(180)]
        [SerializeField] private Sprite illustration;
        public Sprite Illustration => illustration;

        [Header("애니메이션")]
        [Tooltip("성급별 AnimatorOverrideController. 인덱스 = 성급 (0은 미사용)")]
        [SerializeField] private AnimatorOverrideController[] animatorByStars;

        /// <summary>해당 성급의 AnimatorOverrideController를 반환한다. 미설정이면 null을 반환한다.</summary>
        public AnimatorOverrideController GetAnimatorByStar(int star)
        {
            if (animatorByStars == null || animatorByStars.Length == 0)
            {
                return null;
            }
            if (star < 0 || star >= animatorByStars.Length)
            {
                return animatorByStars[1];
            }
            return animatorByStars[star];
        }

        [Header("시너지")]
        [Tooltip("이 유닛이 보유한 소환술사 효과")]
        [SerializeField] private SynergyDefinitionData summonerEffect;

        /// <summary>이 유닛이 보유한 소환술사 효과. 소속 소환술사에 의해 고정된다.</summary>
        public SynergyDefinitionData SummonerEffect => summonerEffect;

        /// <summary>
        /// 이 유닛이 보유한 모든 시너지 목록을 반환한다.
        /// 시너지 종류(소환술사 효과, 소환수 특성 등)를 구분하지 않는다.
        /// </summary>
        public IReadOnlyList<SynergyDefinitionData> Synergies
        {
            get
            {
                var list = new List<SynergyDefinitionData>();
                if (summonerEffect != null) list.Add(summonerEffect);
                return list;
            }
        }

        [Header("역할군")]
        [Tooltip("이 소환수가 속한 역할군. 소환수 종류 단위로 고정된다. 소환술사·침략자에는 설정하지 않는다.")]
        [SerializeField] private RoleGroupDefinitionData roleGroup;

        /// <summary>이 소환수가 속한 역할군. 종류 단위로 고정되어 합성·성급 상승·추가 소환에도 바뀌지 않는다. 소환수에만 설정되며 미설정이면 null이다.</summary>
        public RoleGroupDefinitionData RoleGroup => roleGroup;
    }
}