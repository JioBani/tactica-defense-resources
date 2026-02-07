using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Events.RoundEvents;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.RoundInfos
{
    public class GameOverUiManager : MonoBehaviour
    {
        [SerializeField] GameObject gameOverPanel;

        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnBattleLoseEventDto>(OnBattleLose);
            gameOverPanel.SetActive(false);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnBattleLoseEventDto>(OnBattleLose);
        }

        private void OnBattleLose(OnBattleLoseEventDto _)
        {
            gameOverPanel.SetActive(true);
        }
    }
}   
