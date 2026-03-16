// ─────────────────────────────────────────────
// ActionStateHookBinder + OnActionStateChangedHookProvider + IOnActionStateChangedHook
// ActionStateController의 상태 변경을 IOnActionStateChangedHook SE들에게 전달한다.
// 프리팹의 HookProviders 오브젝트에 ActionStateHookBinder를 부착하여 등록한다.
// ─────────────────────────────────────────────
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Units.ActionStates;
using UnityEngine;

namespace Common.Scripts.StatusEffect.HookProvider
{
    /// <summary>
    /// OnActionStateChangedHookProvider를 StatusEffectController에 등록하는 바인더 컴포넌트.
    /// 프리팹의 HookProviders 오브젝트에 부착한다.
    /// </summary>
    public class ActionStateHookBinder : MonoBehaviour
    {
        [SerializeField] private StatusEffectController statusEffectController;
        [SerializeField] private ActionStateController actionStateController;

        private void Awake()
        {
            if (statusEffectController != null && actionStateController != null)
                statusEffectController.AddHookProvider(
                    new OnActionStateChangedHookProvider(actionStateController));
        }
    }

    /// <summary>
    /// ActionStateController의 상태 변경을 IOnActionStateChangedHook SE들에게 전달하는 Provider.
    /// IStateListener를 구현하여 상태 Enter/Exit를 수신한다.
    /// </summary>
    public class OnActionStateChangedHookProvider
        : StatusEffectHookProvider<IOnActionStateChangedHook>, IStateListener<ActionStateType>
    {
        private readonly ActionStateController _actionStateController;

        public OnActionStateChangedHookProvider(ActionStateController actionStateController)
        {
            _actionStateController = actionStateController;
            _actionStateController.RegisterListener(this);
        }

        void IStateListener<ActionStateType>.OnStateEnter(ActionStateType stateType)
        {
            foreach (var hook in StatusEffects)
                hook.OnActionStateEnter(stateType);
        }

        void IStateListener<ActionStateType>.OnStateExit(ActionStateType stateType)
        {
            foreach (var hook in StatusEffects)
                hook.OnActionStateExit(stateType);
        }

        void IStateListener<ActionStateType>.OnStateRun(ActionStateType stateType)
        {
            // 성능 고려하여 비활성. 필요 시 IOnActionStateChangedHook에 메서드 추가 후 활성화.
        }

        public override void Dispose()
        {
            if (_actionStateController != null)
                _actionStateController.UnregisterListener(this);
        }
    }

    /// <summary>
    /// 유닛 행동 상태 변경 시 호출되는 SE 훅 인터페이스.
    /// </summary>
    public interface IOnActionStateChangedHook : IStatusEffectHook
    {
        /// <summary>행동 상태에 진입했을 때 호출된다.</summary>
        void OnActionStateEnter(ActionStateType stateType);

        /// <summary>행동 상태에서 나갈 때 호출된다.</summary>
        void OnActionStateExit(ActionStateType stateType);

        // /// <summary>행동 상태가 유지되는 매 프레임 호출된다. 성능 고려하여 비활성.</summary>
        // void OnActionStateRun(ActionStateType stateType);
    }
}
