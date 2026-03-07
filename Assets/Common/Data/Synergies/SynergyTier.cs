using System;
using Common.Scripts.SerializableDictionary;
using UnityEngine;

namespace Common.Data.Synergies
{
    /// <summary>시너지 티어. 단계, 임계치, 해당 단계의 상수 목록을 정의한다.</summary>
    [Serializable]
    public struct SynergyTier
    {
        [Tooltip("티어 단계 (1, 2, 3 ...)")]
        [SerializeField] private int tier;

        /// <summary>티어 단계</summary>
        public int Tier => tier;

        [Tooltip("이 티어가 활성화되기 위한 최소 유닛 수")]
        [SerializeField] private int requiredCount;

        /// <summary>활성화에 필요한 최소 유닛 수</summary>
        public int RequiredCount => requiredCount;

        [Tooltip("이 티어의 상수 목록. 구현 클래스가 이름으로 조회한다.")]
        [SerializeField] private SerializableDictionary<string, float> constants;

        /// <summary>이 티어의 상수 딕셔너리</summary>
        public SerializableDictionary<string, float> Constants => constants;

        /// <summary>
        /// 이름으로 상수값을 조회한다. 존재하지 않으면 null을 반환한다.
        /// </summary>
        public readonly float? Get(string name)
        {
            if (constants == null) return null;
            return constants.TryGetValue(name, out var value) ? value : null;
        }
    }
}
