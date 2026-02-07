using Common.Scripts.GlobalEventBus;
using Common.Scripts.TaskQueue;
using Common.Scripts.SceneDataManager;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Scenes.Battle.Feature.Events.RoundEvents;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scenes.Battle.Feature.Ui.RoundInfos
{
    public class GameOverUiManager : MonoBehaviour
    {
        [SerializeField] GameObject gameOverPanel;

        private const float ShowAnimationDuration = 0.5f;

        private UniTaskCompletionSource _confirmClickSource;

        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnBattleLoseEventDto>(OnBattleLose);
            gameOverPanel.SetActive(false);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnBattleLoseEventDto>(OnBattleLose);
        }

        private void OnDestroy()
        {
            gameOverPanel.transform.DOKill();
        }

        private void OnBattleLose(OnBattleLoseEventDto _)
        {
            EnqueueShowPanel();
        }

        private void EnqueueShowPanel()
        {
            GlobalTaskQueue.Enqueue(TaskQueueChannel.BattleUi, new QueuedTask(async () =>
            {
                _confirmClickSource = new UniTaskCompletionSource();
                ShowPanel();
                await _confirmClickSource.Task;
            }));
        }

        private void ShowPanel()
        {
            gameOverPanel.transform.DOComplete();
            gameOverPanel.transform.localScale = Vector3.one * 0.5f;
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.DOScale(Vector3.one, ShowAnimationDuration).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// 확인 버튼 클릭 시 BattleFront 씬으로 이동
        /// </summary>
        public void OnConfirmButtonClick()
        {
            _confirmClickSource?.TrySetResult();
            GlobalTaskQueue.Clear(TaskQueueChannel.BattleUi);
            SceneDataManager.Instance.ClearAll();
            SceneManager.LoadScene("BattleFront");
        }
    }
}   
