using System.Collections.Generic;
using Common.Data.Summoners.SummonerFormations;
using Common.Data.Summoners.SummonerLoadOuts;
using Common.Scripts.SceneSingleton;
using Scenes.Battle.Feature.Units;
using UnityEngine;

namespace Scenes.Battle.Feature.Unit.Summoners
{
    /// <summary>소환술사의 생성·배치·정리를 관리한다.</summary>
    public class SummonerManager : SceneSingleton<SummonerManager>
    {
        [SerializeField] private UnitGenerator unitGenerator;
        [SerializeField] private List<Transform> spawnPoints;

        [Header("테스트용 (편성 시스템 구현 전까지 사용)")]
        [SerializeField] private SummonerLoadOutData[] testSummoners;

        private SummonerFormation _formation;
        private readonly List<Summoner> _summoners = new();

        /// <summary>편성에 포함된 소환술사 로드아웃 목록.</summary>
        public IReadOnlyList<SummonerLoadOutData> Summoners => _formation.Summoners;

        protected override void OnAwakeSingleton()
        {
            // 편성 데이터가 외부에서 주입되지 않았으면 테스트 데이터로 생성한다.
            if (_formation == null && testSummoners is { Length: > 0 })
            {
                _formation = new SummonerFormation(testSummoners);
            }
        }

        private void Start()
        {
            SpawnSummoners();
        }

        /// <summary>외부에서 편성 데이터를 주입한다. 편성 선택 UI 완성 시 사용.</summary>
        public void SetFormation(SummonerFormation formation)
        {
            _formation = formation;
        }

        // ── 스폰 ──

        /// <summary>편성 데이터를 기반으로 소환술사를 스폰 포인트에 배치한다.</summary>
        private void SpawnSummoners()
        {
            if (_formation == null)
            {
                Debug.LogError("[SummonerManager] SummonerFormation이 설정되지 않았습니다.");
                return;
            }

            for (int i = 0; i < _formation.Summoners.Count && i < spawnPoints.Count; i++)
            {
                SummonerLoadOutData loadOut = _formation.Summoners[i];
                if (loadOut == null)
                {
                    continue;
                }

                Units.Unit unit = unitGenerator.GenerateSummoner(loadOut);

                unit.transform.position = new Vector3(
                    spawnPoints[i].position.x,
                    spawnPoints[i].position.y,
                    unit.transform.position.z
                );

                Summoner summoner = unit.GetComponent<Summoner>();
                _summoners.Add(summoner);
            }
        }
    }
}
