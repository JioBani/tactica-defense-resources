using Common.Scripts.Rxs;
using UnityEngine;

namespace Scenes.Battle.Feature.LifeCrystals
{
    public class LifeCrystalManager : MonoBehaviour
    {
        private const int MaxFailCount = 3;

        public RxValue<int> RoundFailCount;

        public bool IsMissionFailed => RoundFailCount.Value >= MaxFailCount;

        private void Awake()
        {
            RoundFailCount = new RxValue<int>(0);
        }

        public void IncrementFailCount()
        {
            RoundFailCount.Value++;
        }
    }
}
