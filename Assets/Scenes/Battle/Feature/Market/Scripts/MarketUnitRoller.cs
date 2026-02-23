using System;
using System.Collections.Generic;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.RepeatX;
using Random = UnityEngine.Random;

namespace Scenes.Battle.Feature.Markets
{
    public class MarketUnitRoller
    {
        private List<UnitLoadOutData> _unitList;
        private Dictionary<int, List<UnitLoadOutData>> _unitListByCost;

        // n / 100
        private Dictionary<int, float> _probabilityByCost = new Dictionary<int, float>()
        {
            {1 , 40}, 
            {2 , 30},
            {3 , 15},
            {4 , 10},
            {5 , 5}, 
        };
        
        public Dictionary<int, float> ProbabilityByCost => _probabilityByCost;

        public MarketUnitRoller(List<UnitLoadOutData> appearedUnitList)
        {
            _unitList = appearedUnitList;
            
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
        /// n개의 마켓 슬롯을 뽑는다. 각 슬롯에는 유닛 데이터와 초기 성급이 포함된다.
        /// </summary>
        public List<MarketDefenderSlot> PickUnits(int n)
        {
            return RepeatX.Times(n, _ => PickUnit());
        }

        private MarketDefenderSlot PickUnit()
        {
            // 1. 등장 코스트 선택
            int cost = PickCost();

            // 2. 코스트 안에서 랜덤하게 하나 선택
            var targetList = _unitListByCost[cost];
            int length = targetList.Count;

            int index = Random.Range(0, length - 1);

            // TODO: 배치 상한 레벨에 따른 2성/3성 등장 확률 적용
            int star = 1;

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