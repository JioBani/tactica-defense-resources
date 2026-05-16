using Common.Data.Synergies;
using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Events.RoundEvents;
using Scenes.Battle.Feature.Unit.Summoners;
using UnityEngine;

namespace Scenes.Battle.Feature.SummonTrait
{
    /// <summary>전장 시작 이벤트를 받아 소환수 특성 분배를 실행하고 SummonTraitStore에 결과를 저장한다.</summary>
    public class SummonTraitDistributor : MonoBehaviour
    {
        /// <summary>분배 대상 특성 풀 레지스트리. 인스펙터에서 연결 필수.</summary>
        [SerializeField] private SummonTraitRegistry registry;

        /// <summary>균등 랜덤 분배 로직 서비스.</summary>
        private readonly SummonTraitService _service = new();

        /// <summary>활성화 시 전장 시작 이벤트를 구독한다.</summary>
        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnBattleStartEventDto>(OnBattleStart);
        }

        /// <summary>비활성화 시 이벤트 구독을 해제한다.</summary>
        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnBattleStartEventDto>(OnBattleStart);
        }

        /// <summary>전장 시작 시 소환수 풀 전체에 특성을 분배하고 저장소에 저장한다.</summary>
        private void OnBattleStart(OnBattleStartEventDto _)
        {
            if (registry == null)
            {
                Debug.LogError("[SummonTraitDistributor] registry 미연결");
                return;
            }

            if (SummonerManager.Instance == null)
            {
                Debug.LogError("[SummonTraitDistributor] SummonerManager 미존재");
                return;
            }

            var traitMap = _service.Distribute(SummonerManager.Instance.Summoners, registry.Traits);

            if (SummonTraitStore.Instance == null)
            {
                Debug.LogError("[SummonTraitDistributor] SummonTraitStore 미존재");
                return;
            }

            SummonTraitStore.Instance.Initialize(traitMap);
        }
    }
}
