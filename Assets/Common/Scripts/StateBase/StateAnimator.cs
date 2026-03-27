using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.Scripts.StateBase
{
    /// <summary>
    /// 상태 머신의 상태 변경에 연동하여 애니메이션을 자동 전환하는 제네릭 베이스 클래스.
    /// 특정 상태의 애니메이션 1루프를 지정된 시간에 맞추는 속도 조절 메커니즘을 제공한다.
    /// </summary>
    public class StateAnimator<T> : MonoBehaviour, IStateListener<T> where T : struct, Enum
    {
        private Animator _animator;
        private StateBaseController<T> _stateBaseController;

        private bool _initialized;
        private readonly Dictionary<T, float> _targetDurations = new();
        private readonly Dictionary<T, float> _clipLengthCache = new();

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
            _stateBaseController = GetComponent<StateBaseController<T>>();
            _stateBaseController.RegisterListener(this);
        }

        /// <summary>애니메이터 컨트롤러를 할당하고 애니메이션을 시작한다.</summary>
        protected void Initialize(RuntimeAnimatorController controller)
        {
            if (controller != null)
            {
                _animator.runtimeAnimatorController = controller;
                CacheClipLengths();
                _initialized = true;
                // 초기 OnStateEnter는 이미 호출된 뒤이므로, 현재 상태의 애니메이션을 즉시 재생한다.
                PlayState(_stateBaseController.CurrentState);
            }
            else
            {
                _initialized = false;
            }
        }

        /// <summary>특정 상태의 애니메이션 1루프를 목표 시간에 맞추도록 등록한다.</summary>
        protected void SetTargetDuration(T state, float duration)
        {
            _targetDurations[state] = duration;

            if (_initialized && _stateBaseController.CurrentState.Equals(state))
            {
                RecalculateSpeed(state);
            }
        }

        /// <summary>상태에 해당하는 애니메이션을 재생하고 속도를 계산한다.</summary>
        private void PlayState(T state)
        {
            _animator.Play(state.ToString());
            RecalculateSpeed(state);
        }

        /// <summary>clipLength / targetDuration으로 애니메이션 속도를 계산한다.</summary>
        private void RecalculateSpeed(T state)
        {
            if (_targetDurations.TryGetValue(state, out float targetDuration) &&
                _clipLengthCache.TryGetValue(state, out float clipLength) &&
                targetDuration > 0f)
            {
                _animator.speed = clipLength / targetDuration;
            }
            else
            {
                _animator.speed = 1.0f;
            }
        }

        /// <summary>OverrideController의 전체 상태별 클립 길이를 캐싱한다.</summary>
        private void CacheClipLengths()
        {
            _clipLengthCache.Clear();

            if (_animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
            {
                foreach (T state in Enum.GetValues(typeof(T)))
                {
                    string placeholderName = $"placeholder_{state.ToString().ToLower()}";
                    AnimationClip clip = overrideController[placeholderName];
                    if (clip != null)
                    {
                        _clipLengthCache[state] = clip.length;
                    }
                }
            }
        }

        void IStateListener<T>.OnStateEnter(T stateType)
        {
            if (_initialized)
            {
                PlayState(stateType);
            }
        }

        void IStateListener<T>.OnStateRun(T stateType)
        {
        }

        void IStateListener<T>.OnStateExit(T stateType)
        {
        }

        protected virtual void OnDestroy()
        {
            _stateBaseController.UnregisterListener(this);
        }
    }
}
