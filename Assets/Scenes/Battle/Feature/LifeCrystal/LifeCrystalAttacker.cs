using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Aggressors;
using Scenes.Battle.Feature.Events.RoundEvents;
using UnityEngine;

namespace Scenes.Battle.Feature.LifeCrystals
{
    public class LifeCrystalAttacker : MonoBehaviour
    {
        private Aggressor _aggressors;

        private void Awake()
        {
            _aggressors = GetComponent<Aggressor>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("DefenseLine"))
            {
                GlobalEventBus.Publish(new OnRoundLoseEventDto());
                _aggressors.OnEnterDefenseLine();
            }
        }
    }
}
