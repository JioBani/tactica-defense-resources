using System;
using System.Collections.Generic;
using Common.Data.Configs;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.RepeatX;
using Random = UnityEngine.Random;

namespace Scenes.Battle.Feature.Markets
{
    public class MarketUnitRoller
    {
        private List<UnitLoadOutData> _unitList;
        private Dictionary<int, List<UnitLoadOutData>> _unitListByCost;

        /// <summary>배치 상한 레벨별 성급 등장 확률 설정.</summary>
        private readonly StarProbabilityConfig _starProbabilityConfig;

        // n / 100
        //TODO: 코스트별 유닛이 추가되면 확률 분배 복원
        private Dictionary<int, float> _probabilityByCost = new Dictionary<int, float>()
        {
            {1 , 100},
        };

        public Dictionary<int, float> ProbabilityByCost => _probabilityByCost;

        public MarketUnitRoller(List<UnitLoadOutData> appearedUnitList, StarProbabilityConfig starProbabilityConfig)
        {
            _unitList = appearedUnitList;
            _starProbabilityConfig = starProbabilityConfig;

            _unitListByCost = new Dictionary<int, List<UnitLoadOutData>>();

            foreach (var unit in _unitList)
            {
                int cost = unit.Unit.Cost;

                if (_unitListByCost.ContainsKey(cost))
                {
                    _unitListByCost[cost].Add(unit);
                }
                else
                {
                    _unitListByCost[cost] = new List<UnitLoadOutData>() { unit };
                }
            }
        }

        /// <summary>
        /// n개의 마켓 슬롯을 뽑는다. 각 슬롯에는 유닛 데이터와 배치 상한 레벨에 따른 성급이 포함된다.
        /// </summary>
        public List<MarketDefenderSlot> PickUnits(int n, int placementLevel)
        {
            return RepeatX.Times(n, _ => PickUnit(placementLevel));
        }

        private MarketDefenderSlot PickUnit(int placementLevel)
        {
            // 1. 등장 코스트 선택
            int cost = PickCost();

            // 2. 코스트 안에서 랜덤하게 하나 선택
            var targetList = _unitListByCost[cost];
            int length = targetList.Count;

            int index = Random.Range(0, length - 1);

            // 3. 배치 상한 레벨에 따른 성급 결정
            int star = _starProbabilityConfig.PickStar(placementLevel);

            return new MarketDefenderSlot(targetList[index], star);
        }

        // 확률에 따라 등장 코스트 선택
        private int PickCost()
        {
            int random = Random.Range(0, 100);
            
            float cumulative = 0f;
            int lastCost = 1;
            
            foreach (var kvp in _probabilityByCost)
            {
                int cost = kvp.Key;
                float prob = kvp.Value;

                cumulative += prob;

                if (random < cumulative)
                {
                    return cost;
                }
            }

            throw new Exception("확률 합이 맞지 않습니다.");
        }
    }
}