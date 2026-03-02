using System.Collections.Generic;
using System.Linq;
using Common.Scripts.ObjectPool;
using Common.Scripts.StateBase;
using Common.Scripts.TransformChildrenIterator;
using Scenes.Battle.Feature.Aggressors;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Units;
using UnityEngine;

namespace Scenes.Battle.Feature.Rounds
{
    public class RoundInfoViewer : MonoBehaviour, IStateListener<PhaseType>
    {
        [SerializeField] private UnitGenerator unitGenerator;

        /// <summary>프리뷰 슬롯의 부모 오브젝트. 자식 Transform들을 슬롯으로 사용한다.</summary>
        [SerializeField] private Transform previewSlotParent;

         private readonly List<Transform> _previewSlots = new();
        private readonly List<Feature.Units.Unit> _previewUnits = new();

        private void Awake()
        {
            // 자식 Transform을 수집하고, 왼→오른(X 오름) → 위→아래(Y 내림) 순으로 정렬하여
            // 왼쪽 열부터 위에서 아래로 채우는 배치 순서를 보장한다.
            foreach (var child in previewSlotParent.ChildrenForward())
            {
                _previewSlots.Add(child);
            }

            _previewSlots.Sort((a, b) =>
            {
                int xCompare = a.position.x.CompareTo(b.position.x);
                return xCompare != 0 ? xCompare : b.position.y.CompareTo(a.position.y);
            });

            RoundManager.Instance.RegisterListener(this);
        }

        void IStateListener<PhaseType>.OnStateEnter(PhaseType phaseType)
        {
            if (phaseType == PhaseType.Maintenance)
            {
                ShowRoundInfo();
            }
        }

        void IStateListener<PhaseType>.OnStateRun(PhaseType phaseType)
        {
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

        /// <summary>
        /// 다음 웨이브의 침략자를 종류+성급 기준으로 그룹핑하여 프리뷰 Aggressor를 소환한다.
        /// 각 Aggressor는 Freeze 상태로 배치되며, 등장 숫자가 CountText로 표시된다.
        /// </summary>
        private void ShowRoundInfo()
        {
            var roundData = RoundManager.Instance.GetCurrentRoundData();

            var grouped = roundData.spawnEntries
                .GroupBy(entry => (entry.aggressor.Unit.ID, entry.star))
                .Select(group => (
                    Aggressor: group.First().aggressor,
                    Star: group.Key.star,
                    Count: group.Sum(entry => entry.count)
                ));

            int slotIndex = 0;
            foreach (var group in grouped)
            {
                if (slotIndex >= _previewSlots.Count) break;

                Feature.Units.Unit unit = unitGenerator.GenerateAggressor(group.Aggressor, group.Star);

                var preview = unit.GetComponent<AggressorPreview>();
                preview.Activate(group.Count);

                Transform slot = _previewSlots[slotIndex];
                unit.transform.position = new Vector3(
                    slot.position.x,
                    slot.position.y,
                    unit.transform.position.z);

                _previewUnits.Add(unit);
                slotIndex++;
            }
        }

        /// <summary>
        /// 프리뷰 Aggressor를 모두 비활성화하고 DeSpawn한다.
        /// </summary>
        private void HideRoundInfo()
        {
            foreach (var unit in _previewUnits)
            {
                unit.GetComponent<AggressorPreview>().Deactivate();
                unit.GetComponent<Poolable>().DeSpawn();
            }

            _previewUnits.Clear();
        }
    }
}
