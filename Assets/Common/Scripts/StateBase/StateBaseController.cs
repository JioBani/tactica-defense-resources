using System;
using Common.Scripts.SafeIterationList;
using UnityEngine;

namespace Common.Scripts.StateBase
{
    /// <summary>
    /// 열거형 기반 상태 머신을 관리하는 추상 클래스입니다.
    /// 상태의 라이프사이클(Enter -> Run -> Exit)을 자동으로 관리하며, IStateListener를 통해 상태 변화를 알립니다.
    ///
    /// <para><b>핵심 개념:</b></para>
    /// <list type="bullet">
    ///   <item>상태를 열거형(Enum)으로 정의하여 타입 안전성을 보장합니다</item>
    ///   <item>매 프레임 CheckStateTransition()을 호출하여 자동으로 상태 전환을 체크합니다</item>
    ///   <item>상태 전환 시 이전 상태의 Exit → 새 상태의 Enter 순서로 알림을 보냅니다</item>
    ///   <item>매 프레임 현재 상태의 Run을 호출합니다</item>
    /// </list>
    ///
    /// <para><b>사용 방법:</b></para>
    ///
    /// <para><b>주의사항:</b></para>
    /// <list type="bullet">
    ///   <item>CheckStateTransition()에서는 우선순위가 높은 전환 조건을 먼저 체크하세요</item>
    ///   <item>상태 전환이 필요하면 새 상태를 반환하고, 아니면 currentState를 그대로 반환하세요</item>
    ///   <item>StartStateBase()는 초기 상태 설정 시 한 번만 호출하세요</item>
    ///   <item>외부에서 강제로 상태를 변경하려면 RequestStateChange()를 사용하세요</item>
    /// </list>
    /// </summary>
    /// <typeparam name="T">상태를 나타내는 열거형 타입 (struct, Enum 제약)</typeparam>
    public abstract class StateBaseController<T> : MonoBehaviour where T : struct, Enum
    {
        private T _currentState;

        /// <summary>
        /// 현재 상태를 반환합니다.
        /// </summary>
        public T CurrentState => _currentState;
        
        [SerializeField] private T showState; // 인스펙터 노출용

        // IStateListener 리스너 관리
        private readonly SafeIterationList<IStateListener<T>> _listeners = new();

        protected bool DebugMode = false;

        protected virtual void Awake()
        {
            StateBaseAwake();
        }

        private void Update()
        {
            showState = _currentState;

            // Run 알림
            NotifyRun(_currentState);

            // 상태 전환 조건 체크
            T nextState = CheckStateTransition(_currentState);

            // 상태 변경 실행
            if (!nextState.Equals(_currentState))
            {
                NotifyExit(_currentState);
                ChangeState(nextState);
            }
        }
        
        protected virtual void StateBaseAwake()
        {

        }

        public void StartStateBase(T state)
        {
            _currentState = state;
            NotifyEnter(_currentState);
        }

        private void ChangeState(T nextState)
        {
            _currentState = nextState;
            NotifyEnter(_currentState);
        }

        // 리스너 알림 메서드들
        private void NotifyEnter(T stateType)
        {
            foreach (var listener in _listeners)
            {
                listener.OnStateEnter(stateType);
            }
        }

        private void NotifyRun(T stateType)
        {
            foreach (var listener in _listeners)
            {
                listener.OnStateRun(stateType);
            }
        }

        private void NotifyExit(T stateType)
        {
            foreach (var listener in _listeners)
            {
                listener.OnStateExit(stateType);
            }
        }

        /// <summary>
        /// IStateListener를 등록합니다.
        /// </summary>
        public void RegisterListener(IStateListener<T> listener)
        {
            if (listener != null && !_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        /// <summary>
        /// IStateListener를 해제합니다.
        /// </summary>
        public void UnregisterListener(IStateListener<T> listener)
        {
            if (listener != null)
            {
                _listeners.Remove(listener);
            }
        }

        /// <summary>
        /// 상태를 강제로 변경합니다. (외부에서 호출 가능)
        /// </summary>
        public void RequestStateChange(T nextState)
        {
            if (DebugMode)
            {
                Debug.Log($"RequestStateChange: {CurrentState} -> {nextState}");
            }
            NotifyExit(_currentState);
            ChangeState(nextState);
        }

        /// <summary>
        /// 상태 전환 조건을 체크합니다.
        /// 오버라이드하여 상태별 전환 로직을 구현하세요.
        ///
        /// 팁: 우선순위가 높은 전환(예: 강제 종료 조건)을 먼저 체크하고,
        /// 그 다음 일반적인 상태별 전환을 switch-case로 체크하면 됩니다.
        /// </summary>
        protected virtual T CheckStateTransition(T currentState)
        {
            return currentState;
        }
    }
}