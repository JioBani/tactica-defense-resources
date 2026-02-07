using System.Collections.Generic;
using System.Linq;
using Common.Data.Battlefields;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.ObjectPool;
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Aggressors;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Sells;
using Scenes.Battle.Feature.Units;
using UnityEngine;

namespace Scenes.Battle.Feature.Rounds
{
    public class RoundInfoViewer : MonoBehaviour, IStateListener<PhaseType>
    {
        [SerializeField] private UnitGenerator unitGenerator;
        [SerializeField] private AggressorSideSellManager  aggressorSideSellManager;

        private RoundData _currentRoundData;
        private readonly List<AggressorSample> _samples = new ();

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
                ShowRoundInfo();
            }
        }

        void IStateListener<PhaseType>.OnStateRun(PhaseType phaseType)
        {
            // Run 단계에서는 특별한 동작 없음
        }

        void IStateListener<PhaseType>.OnStateExit(PhaseType phaseType)
        {
            if (phaseType == PhaseType.Maintenance)
            {
                HideRoundInfo();
            }
        }

        private void OnDestroy()
        {
            RoundManager.Instance.UnregisterListener(this);
        }

        void ShowRoundInfo()
        {
            _currentRoundData = RoundManager.Instance.GetCurrentRoundData();

            // 유닛 ID 로 그룹화 해서 하나의 유닛당 하나씩만 미리보기 소환
            Dictionary<int, UnitLoadOutData> enemyInfos = _currentRoundData.spawnEntries
                .GroupBy(spawn => spawn.aggressor.Unit.ID)
                .ToDictionary(group => group.Key, group => group.First().aggressor);

            Dictionary<int, int> aggressorCounts = _currentRoundData.spawnEntries
                .GroupBy(spawn => spawn.aggressor.Unit.ID)
                .ToDictionary(group => group.Key, group => group.Sum(e => e.count));
            
            foreach (var pair in enemyInfos)
            {
                AggressorSideSell sell = aggressorSideSellManager.GetEmptySell();

                if (sell != null)
                {
                    Feature.Units.Unit unit = unitGenerator.GenerateAggressorSample(pair.Value);

                    AggressorSample sample = unit.GetComponent<AggressorSample>();
                    
                    sample.SetCount(aggressorCounts[pair.Key]);
                    
                    sell.SetUnit(unit);

                    unit.transform.position = new Vector3(
                        sell.transform.position.x,
                        sell.transform.position.y,
                        unit.transform.position.z
                    );
                    
                    _samples.Add(sample);
                }
            }
        }

        void HideRoundInfo()
        {
            foreach (var sample in _samples)
            {
                sample.GetComponent<Poolable>().DeSpawn();
            }

            _samples.Clear();
        }
    }
}
