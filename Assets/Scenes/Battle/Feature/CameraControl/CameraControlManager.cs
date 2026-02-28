// ─────────────────────────────────────────────
// CameraControlManager: 페이즈별 카메라 전환과 아군/침략자 뷰 전환을 담당한다.
// ─────────────────────────────────────────────
using Common.Scripts.StateBase;
using DG.Tweening;
using Scenes.Battle.Feature.Rounds;
using Scenes.Battle.Feature.Rounds.Phases;
using UnityEngine;

namespace Scenes.Battle.Feature.CameraControl
{
    public class CameraControlManager : MonoBehaviour, IStateListener<PhaseType>
    {
        Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;

            // IStateListener 등록
            RoundManager.Instance.RegisterListener(this);
        }

        // IStateListener 명시적 구현
        void IStateListener<PhaseType>.OnStateEnter(PhaseType phaseType)
        {
            if (phaseType == PhaseType.Combat)
            {
                SetCombatMode();
            }
        }

        void IStateListener<PhaseType>.OnStateRun(PhaseType phaseType)
        {
            // Run 단계에서는 특별한 동작 없음
        }

        void IStateListener<PhaseType>.OnStateExit(PhaseType phaseType)
        {
            if (phaseType == PhaseType.Combat)
            {
                SetMaintenanceMode();
            }
        }

        private void OnDestroy()
        {
            RoundManager.Instance.UnregisterListener(this);
        }
        
        private void SetCombatMode()
        {
            KillActiveTweens();

            _mainCamera.DOOrthoSize(4.0f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Late, isIndependentUpdate: false);

            _mainCamera.transform.DOMoveX(1.5f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Late, isIndependentUpdate: false);
        }

        private void SetMaintenanceMode()
        {
            KillActiveTweens();

            _mainCamera.DOOrthoSize(3.3f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Late, isIndependentUpdate: false);

            _mainCamera.transform.DOMoveX(0f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Late, isIndependentUpdate: false);
        }

        /// <summary>침략자 프리뷰 카메라 위치(X=3)로 이동한다.</summary>
        public void ShowAggressorSide()
        {
            KillActiveTweens();

            _mainCamera.transform.DOMoveX(3f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Late, isIndependentUpdate: false);
        }

        /// <summary>아군 배치 카메라 위치(X=0)로 이동한다.</summary>
        public void ShowDefenderSide()
        {
            KillActiveTweens();

            _mainCamera.transform.DOMoveX(0f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Late, isIndependentUpdate: false);
        }

        /// <summary>진행 중인 카메라 DOTween 애니메이션을 모두 중단한다.</summary>
        private void KillActiveTweens()
        {
            _mainCamera.DOKill();
            _mainCamera.transform.DOKill();
        }
    }
}
