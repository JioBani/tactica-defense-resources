using Common.Data.Summoners.SummonerLoadOuts;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.ObjectPool;
using UnityEngine;

namespace Scenes.Battle.Feature.Units
{
    public class UnitGenerator : MonoBehaviour
    {
        private ObjectPooler _objectPooler;
        [SerializeField] private GameObject unitPrefab;
        [SerializeField] private GameObject defenderPrefab;
        [SerializeField] private GameObject aggressorPrefab;
        [SerializeField] private GameObject aggressorSamplePrefab;
        [SerializeField] private GameObject summonerPrefab;
        
        [SerializeField] private Transform enemyField;
        
        private Unit Generate(UnitLoadOutData data, GameObject prefab, int star = 1)
        {
            if (_objectPooler == null)
            {
                _objectPooler = ObjectPooler.Instance;
            }

            GameObject newUnit = _objectPooler.Spawn(
                prefab,
                enemyField
            );

            var unitComponent = newUnit.GetComponent<Unit>();

            unitComponent.SetSpawn(data, star);

            return unitComponent;
        }

        /// <summary>
        /// 소환수(디펜더) 프리팹을 스폰하고 초기화한다.
        /// </summary>
        /// <param name="data">유닛 설정 데이터.</param>
        /// <param name="star">초기 성급.</param>
        public Unit GenerateDefender(UnitLoadOutData data, int star = 1)
        {
            return Generate(data, defenderPrefab, star);
        }
        
        /// <summary>
        /// 침략자 프리팹을 스폰하고 초기화한다.
        /// </summary>
        /// <param name="data">유닛 설정 데이터.</param>
        /// <param name="star">침략자 성급. SpawnEntry에서 전달받는다.</param>
        public Unit GenerateAggressor(UnitLoadOutData data, int star = 1)
        {
            return Generate(data, aggressorPrefab, star);
        }

        /// <summary>소환술사 프리팹을 스폰하고 초기화한다.</summary>
        public Unit GenerateSummoner(SummonerLoadOutData data)
        {
            return Generate(data, summonerPrefab);
        }

        public Unit GenerateAggressorSample(UnitLoadOutData data)
        {
            return Generate(data, aggressorSamplePrefab);
        }

        public void RemoveUnit(Unit unit)
        {
            _objectPooler.DeSpawn(unit.GetComponent<Poolable>());
        }
    }
}
