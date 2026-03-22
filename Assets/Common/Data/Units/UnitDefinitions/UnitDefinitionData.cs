using System.Collections.Generic;
using Common.Data.Synergies;
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
    }
}