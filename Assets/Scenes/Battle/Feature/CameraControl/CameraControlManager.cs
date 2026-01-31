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
            _mainCamera.DOOrthoSize(4.0f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Late, isIndependentUpdate: false);
            
            _mainCamera.transform.DOMoveX(1.5f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Late, isIndependentUpdate: false);
        }

        private void SetMaintenanceMode()
        {
            _mainCamera.DOOrthoSize(3.3f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Late, isIndependentUpdate: false);
            
            _mainCamera.transform.DOMoveX(0f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Late, isIndependentUpdate: false);
        }

        public void ShowAggressorSide()
        {
            _mainCamera.transform.DOMoveY(6, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Late, isIndependentUpdate: false);
        }

        public void ShowDefenderSide()
        {
            _mainCamera.transform.DOMoveY(0, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Late, isIndependentUpdate: false);
        }
    }
}
