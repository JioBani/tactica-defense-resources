// ─────────────────────────────────────────────
// StatusEffectDefinitionData: SE의 정적 속성을 정의하는 ScriptableObject 기본 클래스.
// 게임 특화 SED가 상속하여 필드를 추가한다. SE 인스턴스 생성은 Factory가 담당한다.
// ─────────────────────────────────────────────
using UnityEngine;

namespace Common.Data.StatusEffects
{
    /// <summary>
    /// SE의 정적 속성을 정의하는 ScriptableObject 기본 클래스.
    /// 게임 특화 SED가 상속하여 필드를 추가한다. SE 인스턴스 생성은 Factory가 담당한다.
    /// </summary>
    public class StatusEffectDefinitionData : ScriptableObject
    {
    }
}