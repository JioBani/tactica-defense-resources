using System;
using Common.Data.Units.UnitStatsByLevel;
using UnityEngine;

namespace Common.Data.Skills
{
    /// <summary>스탯 계수 항목. 기준 스탯과 비율을 정의한다.</summary>
    [Serializable]
    public struct StatScaling
    {
        /// <summary>계수의 기준이 되는 스탯 종류</summary>
        [Tooltip("계수의 기준이 되는 스탯")]
        public UnitStatKind statKind;

        /// <summary>기준 스탯에 곱할 비율 (1.5 = 150%)</summary>
        [Tooltip("기준 스탯에 곱할 비율 (1.5 = 150%)")]
        public float coefficient;
    }
}
