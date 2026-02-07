using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Events.RoundEvents;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.RoundInfos
{
    public class BattleWinUiManager : MonoBehaviour
    {
        [SerializeField] GameObject battleWinPanel;

        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnBattleWinEventDto>(OnBattleWin);
            battleWinPanel.SetActive(false);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnBattleWinEventDto>(OnBattleWin);
        }

        private void OnBattleWin(OnBattleWinEventDto _)
        {
            battleWinPanel.SetActive(true);
        }
    }
}
