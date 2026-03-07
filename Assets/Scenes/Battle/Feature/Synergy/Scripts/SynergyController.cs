// ─────────────────────────────────────────────
// SynergyController: Defender에 부착되어 시너지 효과의 생명주기를 관리한다.
// GlobalEventBus로 티어 변경 이벤트를 구독하고, 자기 Defender의 시너지에 해당하는 효과만 처리한다.
// ─────────────────────────────────────────────
using System.Collections.Generic;
using Common.Data.Synergies;
using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Synergy.Contexts;
using Scenes.Battle.Feature.Unit.Defenders;
using UnityEngine;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// Defender에 부착되어 시너지 효과의 생명주기를 관리한다.
    /// 티어 변경 이벤트를 구독하고, 자기 Defender의 시너지에 해당하는 효과만 적용/해제한다.
    /// </summary>
    public class SynergyController : MonoBehaviour
    {
        [SerializeField] private Defender defender;

        private readonly Dictionary<SynergyDefinitionData, SynergyEffect> _activeEffects = new();
        private readonly SynergyEffectFactory _factory = new();

        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnSynergyTierChangedEventDto>(OnSynergyTierChanged);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnSynergyTierChangedEventDto>(OnSynergyTierChanged);
        }

        /// <summary>시너지 티어 변경 이벤트 수신. 자기 Defender의 시너지만 처리한다.</summary>
        private void OnSynergyTierChanged(OnSynergyTierChangedEventDto dto)
        {
            if (!HasSynergy(dto.Definition)) return;

            bool wasActive = dto.PreviousTier.HasValue;
            bool isActive = dto.CurrentTier.HasValue;

            if (!wasActive && isActive)
                HandleActivation(dto.Definition);
            else if (wasActive && isActive)
                HandleTierChange(dto.Definition);
            else if (wasActive && !isActive)
                HandleDeactivation(dto.Definition);
        }

        /// <summary>시너지 활성화: 효과를 생성하고 OnSynergyOn을 호출한다.</summary>
        private void HandleActivation(SynergyDefinitionData definition)
        {
            SynergyEffect effect = _factory.Create(definition);
            if (effect == null) return;

            _activeEffects[definition] = effect;
            effect.OnSynergyOn(new OnSynergyOnContext());
        }

        /// <summary>티어 변경: 기존 효과에 티어 변경을 알린다.</summary>
        private void HandleTierChange(SynergyDefinitionData definition)
        {
            if (_activeEffects.TryGetValue(definition, out SynergyEffect effect))
            {
                effect.OnTierChange(new OnTierChangeContext());
            }
        }

        /// <summary>시너지 비활성화: 효과를 해제하고 제거한다.</summary>
        private void HandleDeactivation(SynergyDefinitionData definition)
        {
            if (_activeEffects.Remove(definition, out SynergyEffect effect))
            {
                effect.OnSynergyOff(new OnSynergyOffContext());
            }
        }

        /// <summary>이 Defender가 해당 시너지를 보유하는지 확인한다.</summary>
        private bool HasSynergy(SynergyDefinitionData definition)
        {
            IReadOnlyList<SynergyDefinitionData> synergies = defender.UnitLoadOutData.Unit.Synergies;
            foreach (SynergyDefinitionData synergy in synergies)
            {
                if (synergy == definition) return true;
            }
            return false;
        }
    }
}