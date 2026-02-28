// ─────────────────────────────────────────────
// SwitchViewManager: 카메라 전환 버튼 UI와 라운드 정보 표시를 관리한다.
// ─────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Linq;
using Common.Data.Battlefields;
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.CameraControl;
using Scenes.Battle.Feature.Rounds.Phases;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.Battle.Feature.Rounds.Ui
{
    public class SwitchViewManager : MonoBehaviour, IStateListener<PhaseType>
    {
        [SerializeField] private bool isEnemySideView;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private GameObject roundPanel;
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI roundInfoText;
        [SerializeField] private CameraControlManager cameraControlManager;

        /// <summary>카메라 전환 버튼. 전투 페이즈에서는 비활성화된다.</summary>
        [SerializeField] private Button cameraToggleButton;

        public Action<bool> switchViewEvent;

        private void Awake()
        {
            // IStateListener 등록
            RoundManager.Instance.RegisterListener(this);
        }

        // IStateListener 명시적 구현
        void IStateListener<PhaseType>.OnStateEnter(PhaseType phaseType)
        {
            if (phaseType == PhaseType.Maintenance)
            {
                cameraToggleButton.interactable = true;
                SetRoundInfo();
            }

            if (phaseType == PhaseType.Ready || phaseType == PhaseType.Combat)
            {
                ResetPreviewState();
                cameraToggleButton.interactable = false;
            }
        }

        void IStateListener<PhaseType>.OnStateRun(PhaseType phaseType)
        {
            // Run 단계에서는 특별한 동작 없음
        }

        void IStateListener<PhaseType>.OnStateExit(PhaseType phaseType)
        {
            // Exit 단계에서는 특별한 동작 없음
        }

        private void OnDestroy()
        {
            RoundManager.Instance.UnregisterListener(this);
        }

        public void SwitchView()
        {
            isEnemySideView = !isEnemySideView;

            if (isEnemySideView)
            {
                cameraControlManager.ShowAggressorSide();
                buttonText.text = "아군 진영";
                roundPanel.SetActive(true);
            }
            else
            {
                cameraControlManager.ShowDefenderSide();
                buttonText.text = "적 진영";
                roundPanel.SetActive(false);
            }
            
            switchViewEvent?.Invoke(isEnemySideView);
        }

        /// <summary>프리뷰 상태를 초기화한다. 카메라 이동은 CameraControlManager가 처리한다.</summary>
        private void ResetPreviewState()
        {
            if (!isEnemySideView) return;

            isEnemySideView = false;
            buttonText.text = "적 진영";
            roundPanel.SetActive(false);
            switchViewEvent?.Invoke(false);
        }

        /// <summary>
        /// 라운드 데이터 UI 세팅
        /// </summary>
        void SetRoundInfo()
        {
            RoundData roundData = RoundManager.Instance.GetCurrentRoundData();

            roundText.text = $"{RoundManager.Instance.RoundIndex} 라운드";

            Dictionary<string, int> enemyInfos = roundData.spawnEntries
                .GroupBy(spawn => spawn.aggressor.Unit.DisplayName)
                .ToDictionary(group => group.Key, group => group.Sum(s => s.count));

            roundInfoText.text = String.Empty;

            foreach (var pair in enemyInfos)
            {
                roundInfoText.text += $"{pair.Key} x {pair.Value}, ";
            }
        }
    }
}


