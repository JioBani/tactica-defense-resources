using System;
using Common.Scripts.StateBase;
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

        private const float ShowAnimationDuration = 0.5f; // 패널 등장 애니메이션 시간
        private const float HideAnimationDuration = 0.3f; // 패널 사라짐 애니메이션 시간

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
                    ShowPhasePanel("정비하세요!");
                    AutoHideAfterDelay(2f).Forget();
                    break;

                case PhaseType.Ready:
                    ShowPhasePanel("전투 시작!");
                    break;

                case PhaseType.Combat:
                    HidePhasePanel();
                    break;

                case PhaseType.End:
                    int currentRound = RoundManager.Instance.RoundIndex;
                    ShowPhasePanel($"라운드 {currentRound} 클리어!");
                    break;

                case PhaseType.BattleLose:
                    ShowPhasePanel("패배");
                    break;

                case PhaseType.BattleWin:
                    ShowPhasePanel("승리!");
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
        /// 지정된 시간(초) 후에 패널을 자동으로 숨김
        /// 등장 애니메이션이 완료된 후부터 시간을 카운트합니다.
        /// </summary>
        private async UniTaskVoid AutoHideAfterDelay(float delaySeconds)
        {
            try
            {
                // 등장 애니메이션이 완료될 때까지 대기
                await UniTask.Delay(TimeSpan.FromSeconds(ShowAnimationDuration), cancellationToken: this.GetCancellationTokenOnDestroy());

                // 지정된 시간만큼 추가 대기
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: this.GetCancellationTokenOnDestroy());

                HidePhasePanel();
            }
            catch (OperationCanceledException)
            {
                // 취소된 경우 아무 작업도 하지 않음
            }
        }
    }
}
