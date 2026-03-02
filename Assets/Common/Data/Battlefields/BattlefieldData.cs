using System;
using System.Collections.Generic;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.SerializableTime;
using UnityEngine;

namespace Common.Data.Battlefields
{
    [CreateAssetMenu(menuName = "Battlefields/BattlefieldData", fileName = "BattlefieldData")]
    public class BattlefieldData : ScriptableObject
    {
        [Header("전선 정보")]
        [Tooltip("전선 이름")]
        [SerializeField] private string battlefrontName;
        public string BattlefrontName => battlefrontName;

        [Header("전장 정보")]
        [Tooltip("전장 타입")]
        [SerializeField] private BattlefieldType battlefieldType;
        public BattlefieldType BattlefieldType => battlefieldType;

        [Tooltip("전장 이름")]
        [SerializeField] private string battlefieldName;
        public string BattlefieldName => battlefieldName;

        [Tooltip("전장 아이디")]
        [SerializeField] private int battlefieldId;
        public int BattlefieldId => battlefieldId;

        [Tooltip("전장 순서 (전선 내에서의 순서)")]
        [SerializeField] private int battlefieldIndex;
        public int BattlefieldIndex => battlefieldIndex;

        [Header("라운드 정보")]
        [Tooltip("최대 라운드 수")]
        [SerializeField] private int maxRoundCount;
        public int MaxRoundCount => maxRoundCount;

        [Tooltip("라운드 데이터 목록")]
        [SerializeField] private List<RoundData> rounds = new List<RoundData>();
        public IReadOnlyList<RoundData> Rounds => rounds;

        [Header("보상 정보")]
        [Tooltip("전장 클리어 보상")]
        [SerializeField] private RewardData reward;
        public RewardData Reward => reward;
    }

    public enum BattlefieldType
    {
        Normal,
        Elite,
        Boss
    }

    [Serializable]
    public class RoundData
    {
        [Tooltip("라운드 순서")]
        public int roundIndex;

        [Tooltip("스폰 정보 목록")]
        public List<SpawnEntry> spawnEntries = new List<SpawnEntry>();
    }

    [Serializable]
    public class SpawnEntry
    {
        [Tooltip("소환할 적 유닛")]
        public UnitLoadOutData aggressor;

        [Tooltip("소환 수")]
        [Min(1)] public int count = 1;

        [Tooltip("소환 시간")]
        public SerializableTime spawnTime;

        /// <summary>침략자 성급. 같은 유닛이라도 성급이 다르면 프리뷰에서 별도 항목으로 표시된다.</summary>
        [Tooltip("침략자 성급 (1~3)")]
        [Range(1, 3)] public int star = 1;
    }

    [Serializable]
    public class RewardData
    {
        [Tooltip("마나 보상")]
        public int mana;

        [Tooltip("경험치 보상")]
        public int experience;

        // TODO: 아이템 보상 등 추가 필요시 확장
    }
}
