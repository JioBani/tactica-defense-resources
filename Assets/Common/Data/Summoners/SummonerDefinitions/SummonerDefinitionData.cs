using Common.Data.Synergies;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.SpritePreview;
using UnityEngine;

namespace Common.Data.Summoners.SummonerDefinitions
{
    /// <summary>소환술사의 정체성과 소유 시너지, 소환수 풀을 정의한다.</summary>
    [CreateAssetMenu(menuName = "Summoners/SummonerDefinitionData", fileName = "SummonerDefinitionData", order = 0)]
    public class SummonerDefinitionData : ScriptableObject
    {
        [Header("표시 정보 (Identity)")]
        [Tooltip("소환술사 ID")]
        [SerializeField] private int id;
        /// <summary>소환술사 식별 ID.</summary>
        public int ID => id;

        [Tooltip("게임 내에 표시될 소환술사 이름")]
        [SerializeField] private string displayName = "New Summoner";
        /// <summary>UI 표시명.</summary>
        public string DisplayName => displayName;

        [Tooltip("소환술사 설명")]
        [SerializeField] private string description;
        /// <summary>소환술사 설명.</summary>
        public string Description => description;

        [Tooltip("UI 등에서 사용할 소환술사 아이콘")]
        [SpritePreview(80)]
        [SerializeField] private Sprite icon;
        /// <summary>UI 아이콘.</summary>
        public Sprite Icon => icon;

        [Tooltip("일러스트")]
        [SpritePreview(180)]
        [SerializeField] private Sprite illustration;
        /// <summary>일러스트.</summary>
        public Sprite Illustration => illustration;

        [Header("시너지")]
        [Tooltip("이 소환술사가 소유하는 시너지")]
        [SerializeField] private SynergyDefinitionData summonerEffect;
        /// <summary>이 소환술사가 소유하는 시너지.</summary>
        public SynergyDefinitionData SummonerEffect => summonerEffect;

        [Header("소환수 풀")]
        [Tooltip("이 소환술사가 소환할 수 있는 소환수 목록 (8체)")]
        [SerializeField] private UnitLoadOutData[] summonPool;
        /// <summary>이 소환술사가 소환할 수 있는 소환수 목록.</summary>
        public UnitLoadOutData[] SummonPool => summonPool;
    }
}