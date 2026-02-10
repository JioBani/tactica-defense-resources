using System;
using Common.Scripts.Extensions;
using Common.Scripts.StateBase;
using Common.Scripts.TaskQueue;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Scenes.Battle.Feature.Rounds;
using Scenes.Battle.Feature.Rounds.Phases;
using TMPro;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.RoundInfos
{
    public class RoundUiManager : MonoBehaviour, IStateListener<PhaseType>
    {
        [SerializeField] private GameObject roundPanel;
        [SerializeField] private TextMeshProUGUI phaseText;

        private const float ShowAnimationDuration = 0.5f;
        private const float HideAnimationDuration = 0.3f;
        private const float DisplayDuration = 2f;

        private void Awake()
        {
            // IStateListener 등록
            RoundManager.Instance.RegisterListener(this);
        }

        private void OnEnable()
        {
            roundPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            RoundManager.Instance.UnregisterListener(this);
            // DOTween 정리
            roundPanel.transform.DOKill();
        }

        public void OnStateEnter(PhaseType phaseType)
        {
            switch (phaseType)
            {
                case PhaseType.Maintenance:
                    EnqueueShowAndHide("정비하세요!");
                    break;

                case PhaseType.Ready:
                    ShowPhasePanel("전투 시작!");
                    break;

                case PhaseType.Combat:
                    HidePhasePanel();
                    break;

                case PhaseType.End:
                    int currentRound = RoundManager.Instance.RoundIndex;
                    EnqueueShowAndHide($"라운드 {currentRound} 클리어!");
                    break;

                case PhaseType.RoundLose:
                    EnqueueShowAndHide("라운드 실패!");
                    break;
            }
        }

        public void OnStateRun(PhaseType phaseType)
        {

        }

        public void OnStateExit(PhaseType phaseType)
        {
            switch (phaseType)
            {
                case PhaseType.Ready:
                case PhaseType.End:
                case PhaseType.RoundLose:
                case PhaseType.BattleLose:
                case PhaseType.BattleWin:
                    HidePhasePanel();
                    break;
            }
        }

        private void ShowPhasePanel(string message)
        {
            phaseText.text = message;

            // 진행 중인 애니메이션이 있으면 완료 후 시작, 없으면 바로 시작
            roundPanel.transform.DOComplete();

            // 초기 상태: 약간 작게
            roundPanel.transform.localScale = Vector3.one * 0.5f;
            roundPanel.SetActive(true);

            // DOTween 애니메이션: 스케일업 (튕기는 효과)
            roundPanel.transform.DOScale(Vector3.one, ShowAnimationDuration).SetEase(Ease.OutBack);
        }

        private void HidePhasePanel()
        {
            // 진행 중인 애니메이션이 있으면 완료 후 시작, 없으면 바로 시작
            roundPanel.transform.DOComplete();

            // DOTween 애니메이션: 스케일다운
            roundPanel.transform.DOScale(Vector3.one * 0.5f, HideAnimationDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() => roundPanel.SetActive(false));
        }

        /// <summary>
        /// TaskQueue를 통해 패널 표시 후 숨김 동작을 큐에 추가
        /// </summary>
        private void EnqueueShowAndHide(string message)
        {
            GlobalTaskQueue.Enqueue(TaskQueueChannel.BattleUi, new QueuedTask(async () =>
            {
                await ShowAndHidePanelAsync(message);
            }));
        }

        private async UniTask ShowAndHidePanelAsync(string message)
        {
            phaseText.text = message;
            roundPanel.transform.DOComplete();
            roundPanel.transform.localScale = Vector3.one * 0.5f;
            roundPanel.SetActive(true);

            // 등장 애니메이션
            await roundPanel.transform.DOScale(Vector3.one, ShowAnimationDuration)
                .SetEase(Ease.OutBack)
                .ToUniTask();

            // 표시 시간 대기
            await UniTask.Delay(TimeSpan.FromSeconds(DisplayDuration));

            // 숨김 애니메이션
            await roundPanel.transform.DOScale(Vector3.one * 0.5f, HideAnimationDuration)
                .SetEase(Ease.InBack)
                .ToUniTask();

            roundPanel.SetActive(false);
        }
    }
}
