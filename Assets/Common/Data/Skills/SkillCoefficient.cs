using System;
using UnityEngine;

namespace Common.Data.Skills
{
    /// <summary>
    /// 스킬 계수. 고정값과 스탯 기반 계수 목록으로 최종값을 산출한다.
    /// 최종값 = baseValue + Σ(스탯값 × 계수)
    /// </summary>
    [Serializable]
    public struct SkillCoefficient
    {
        /// <summary>고정 기본값</summary>
        [Tooltip("고정 기본값")]
        public float baseValue;

        /// <summary>스탯 기반 계수 목록</summary>
        [Tooltip("스탯 기반 계수 목록")]
        public StatScaling[] scalings;
    }
}
