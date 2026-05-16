using System.Collections.Generic;
using Common.Data.Synergies;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.SceneSingleton;
using Common.Scripts.SerializableDictionary;
using Scenes.Battle.Feature.Events.RoundEvents;
using UnityEngine;

namespace Scenes.Battle.Feature.SummonTrait
{
    /// <summary>전장 중 소환수별 배정 특성을 보관한다. 전장 종료(BattleWin/BattleLose) 시 자동 초기화된다.</summary>
    /// <remarks>인스펙터에서 _dontDestroyOnLoad = false 로 설정해야 한다.</remarks>
    public class SummonTraitStore : SceneSingleton<SummonTraitStore>
    {
        /// <summary>소환수별 배정 특성. 플레이 모드 인스펙터에서 런타임 상태 확인 가능.</summary>
        [SerializeField] private SerializableDictionary<UnitLoadOutData, SynergyDefinitionData> _traitMap = new();

        /// <summary>소환수별 배정 특성 전체.</summary>
        public SerializableDictionary<UnitLoadOutData, SynergyDefinitionData> All => _traitMap;

        /// <summary>활성화 시 전장 종료 이벤트를 구독한다.</summary>
        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnBattleWinEventDto>(OnBattleWin);
            GlobalEventBus.Subscribe<OnBattleLoseEventDto>(OnBattleLose);
        }

        /// <summary>비활성화 시 이벤트 구독을 해제한다.</summary>
        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnBattleWinEventDto>(OnBattleWin);
            GlobalEventBus.Unsubscribe<OnBattleLoseEventDto>(OnBattleLose);
        }

        /// <summary>전장 승리 시 저장소를 초기화한다.</summary>
        private void OnBattleWin(OnBattleWinEventDto _) => Clear();

        /// <summary>전장 패배 시 저장소를 초기화한다.</summary>
        private void OnBattleLose(OnBattleLoseEventDto _) => Clear();

        /// <summary>분배 결과 딕셔너리로 내부 저장소를 초기화한다.</summary>
        public void Initialize(Dictionary<UnitLoadOutData, SynergyDefinitionData> traitMap)
        {
            if (traitMap == null)
            {
                Debug.LogError("[SummonTraitStore] traitMap이 null입니다.");
                return;
            }

            _traitMap.Clear();
            foreach (var kvp in traitMap)
            {
                _traitMap[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>배정 데이터를 전체 삭제한다.</summary>
        public void Clear()
        {
            _traitMap.Clear();
        }

        /// <summary>소환수의 배정 특성을 반환한다. 미배정이면 null.</summary>
        public SynergyDefinitionData GetTrait(UnitLoadOutData unit)
        {
            if (unit == null)
            {
                Debug.LogError("[SummonTraitStore] unit이 null입니다.");
                return null;
            }

            return _traitMap.TryGetValue(unit, out var trait) ? trait : null;
        }
    }
}
