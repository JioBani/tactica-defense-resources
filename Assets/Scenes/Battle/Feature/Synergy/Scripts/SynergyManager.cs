// ─────────────────────────────────────────────
// SynergyManager: SynergyController 생성과 Dispose, 디버그 표시를 담당하는 얇은 레이어.
// 비즈니스 로직은 SynergyController에서 일원 관리한다.
// 소환술사 편성 시스템 구현 시 allSynergies 주입 방식을 교체한다.
// ─────────────────────────────────────────────
using System.Collections.Generic;
using Common.Data.Synergies;
using Common.Scripts.SceneSingleton;
using Common.Scripts.SerializableDictionary;
using UnityEngine;

namespace Scenes.Battle.Feature.Synergy
{
    public class SynergyManager : SceneSingleton<SynergyManager>
    {
        // TODO: 소환술사 편성 시스템 구현 후, 편성에서 결정된 시너지 목록으로 교체
        [SerializeField] private List<SynergyDefinitionData> allSynergies;

        private readonly Dictionary<SynergyDefinitionData, SynergyActivation> _synergyActivations = new();
        private readonly Dictionary<SynergyDefinitionData, SynergyController> _controllers = new();

        /// <summary>모든 시너지 상태 목록. UI 표시 등 외부 조회용.</summary>
        public IReadOnlyDictionary<SynergyDefinitionData, SynergyActivation> SynergyActivations => _synergyActivations;

        // TODO: 디버그용. 시너지 UI 구현 후 제거한다.
        [Header("디버그 (런타임 확인용)")]
        [SerializeField] private SerializableDictionary<string, string> debugSynergyStatus = new();

        protected override void OnAwakeSingleton()
        {
            InitializeSynergyActivations();
        }

        private void OnDisable()
        {
            foreach (SynergyController controller in _controllers.Values)
            {
                controller.Dispose();
            }
        }

        /// <summary>
        /// allSynergies 목록으로부터 모든 SynergyActivation과 SynergyController를 생성한다.
        /// 디버그 갱신을 위해 ActiveTier.OnChange를 구독한다.
        /// </summary>
        private void InitializeSynergyActivations()
        {
            foreach (SynergyDefinitionData definition in allSynergies)
            {
                if (definition == null)
                {
                    continue;
                }

                var activation = new SynergyActivation(definition);
                _synergyActivations[definition] = activation;
                _controllers[definition] = SynergyControllerFactory.Instance.Create(activation);

                activation.ActiveTier.OnChange += _ => UpdateDebugStatus();
            }
        }

        /// <summary>디버그용: 인스펙터에 시너지 상태를 표시한다.</summary>
        private void UpdateDebugStatus()
        {
            debugSynergyStatus.Clear();

            foreach ((SynergyDefinitionData definition, SynergyActivation activation) in _synergyActivations)
            {
                string tierText = activation.ActiveTier.Value.HasValue
                    ? $"Tier {activation.ActiveTier.Value.Value.Tier}"
                    : "비활성";
                debugSynergyStatus[definition.DisplayName] = $"{activation.Count}명 → {tierText}";
            }
        }
    }
}
