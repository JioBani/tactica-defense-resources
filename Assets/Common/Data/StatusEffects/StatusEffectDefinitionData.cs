// ─────────────────────────────────────────────
// StatusEffectDefinitionData: SE의 정적 속성을 정의하는 ScriptableObject.
// 모든 SE는 이 SO를 통해 ID, 이름, 아이콘 등의 메타데이터를 갖는다.
// ─────────────────────────────────────────────
using UnityEngine;

namespace Common.Data.StatusEffects
{
    /// <summary>
    /// SE의 정적 속성을 정의하는 ScriptableObject.
    /// 모든 SE는 이 SO를 통해 ID, 이름, 아이콘 등의 메타데이터를 갖는다.
    /// </summary>
    [CreateAssetMenu(menuName = "StatusEffect/StatusEffectDefinitionData", fileName = "StatusEffectDefinitionData")]
    public class StatusEffectDefinitionData : ScriptableObject
    {
        [Tooltip("SE 고유 ID")]
        [SerializeField] private int id;

        /// <summary>SE 고유 ID</summary>
        public int Id => id;

        [Tooltip("SE 표시 이름")]
        [SerializeField] private string displayName;

        /// <summary>SE 표시 이름</summary>
        public string DisplayName => displayName;

        [Tooltip("UI에서 표시할 SE 아이콘")]
        [SerializeField] private Sprite icon;

        /// <summary>UI에서 표시할 SE 아이콘</summary>
        public Sprite Icon => icon;
    }
}