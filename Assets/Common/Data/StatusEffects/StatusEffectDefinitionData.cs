// ─────────────────────────────────────────────
// StatusEffectDefinitionData: SE의 정적 속성을 정의하는 ScriptableObject 기본 클래스.
// 게임 특화 SED가 상속하여 필드와 CreateEffect()를 구현한다.
// ─────────────────────────────────────────────
using Common.Scripts.StatusEffect;
using UnityEngine;

namespace Common.Data.StatusEffects
{
    /// <summary>
    /// SE의 정적 속성을 정의하는 ScriptableObject 기본 클래스.
    /// 게임 특화 SED가 상속하여 CreateEffect()를 구현한다.
    /// </summary>
    public abstract class StatusEffectDefinitionData : ScriptableObject
    {
        /// <summary>이 정의에 해당하는 StatusEffect 인스턴스를 생성한다.</summary>
        public abstract StatusEffect CreateEffect();
    }
}