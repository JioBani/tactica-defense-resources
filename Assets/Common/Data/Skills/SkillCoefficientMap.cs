using System;
using Common.Scripts.SerializableDictionary;

namespace Common.Data.Skills
{
    /// <summary>문자열 키로 SkillCoefficient를 조회하는 직렬화 가능한 Dictionary.</summary>
    [Serializable]
    public class SkillCoefficientMap : SerializableDictionary<string, SkillCoefficient> { }
}
