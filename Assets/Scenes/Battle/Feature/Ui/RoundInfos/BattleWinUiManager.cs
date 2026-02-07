using Common.Scripts.GlobalEventBus;
using Common.Scripts.SceneDataManager;
using DG.Tweening;
using Scenes.Battle.Feature.Events.RoundEvents;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scenes.Battle.Feature.Ui.RoundInfos
{
    public class BattleWinUiManager : MonoBehaviour
    {
        [SerializeField] GameObject battleWinPanel;

        private const float ShowAnimationDuration = 0.5f;

        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnBattleWinEventDto>(OnBattleWin);
            battleWinPanel.SetActive(false);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnBattleWinEventDto>(OnBattleWin);
        }

        private void OnDestroy()
        {
            battleWinPanel.transform.DOKill();
        }

        private void OnBattleWin(OnBattleWinEventDto _)
        {
            ShowPanel();
        }

        private void ShowPanel()
        {
            battleWinPanel.transform.DOComplete();
            battleWinPanel.transform.localScale = Vector3.one * 0.5f;
            battleWinPanel.SetActive(true);
            battleWinPanel.transform.DOScale(Vector3.one, ShowAnimationDuration).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// 확인 버튼 클릭 시 BattleFront 씬으로 이동
        /// </summary>
        public void OnConfirmButtonClick()
        {
            SceneDataManager.Instance.ClearAll();
            SceneManager.LoadScene("BattleFront");
        }
    }
}
