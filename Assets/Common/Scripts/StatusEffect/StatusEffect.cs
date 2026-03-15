// ─────────────────────────────────────────────
// StatusEffect: 유닛에 부여되는 효과의 추상 기본 클래스.
// 순수 C# 클래스로, MonoBehaviour에 의존하지 않는다.
// ─────────────────────────────────────────────
using Common.Data.StatusEffects;

namespace Common.Scripts.StatusEffect
{
    /// <summary>
    /// 유닛에 부여되는 효과의 추상 기본 클래스.
    /// 순수 C# 클래스로, MonoBehaviour에 의존하지 않는다.
    /// </summary>
    public abstract class StatusEffect
    {
        /// <summary>이 SE의 정적 메타데이터(ID, 이름, 아이콘).</summary>
        public StatusEffectDefinitionData Definition { get; }

        /// <summary>이 SE를 관리하는 컨트롤러. Apply 시 설정, Remove 시 null.</summary>
        public StatusEffectController Controller { get; internal set; }

        protected StatusEffect(StatusEffectDefinitionData definition)
        {
            Definition = definition;
        }

        /// <summary>SE가 부여될 때 호출된다.</summary>
        public virtual void OnApply(StatusEffectContext context) { }

        /// <summary>매 프레임 호출된다.</summary>
        public virtual void OnUpdate() { }

        /// <summary>SE가 제거될 때 호출된다.</summary>
        public virtual void OnRemove() { }

        /// <summary>만료 여부. true이면 Update 루프 끝에 일괄 제거된다.</summary>
        public bool IsExpired { get; private set; }

        /// <summary>자기 만료를 요청한다. Update 루프 끝에 일괄 제거된다.</summary>
        protected void RequestRemove()
        {
            IsExpired = true;
        }
    }
}