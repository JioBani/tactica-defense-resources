using System.Collections.Generic;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Synergy;
using Scenes.Battle.Feature.Unit.Defenders;
using UnityEngine;
using UnityEngine.UIElements;

namespace Scenes.Battle.Feature.Ui.SynergyInfo
{
    /// <summary>
    /// 시너지 상세 패널의 보유 소환수 목록 섹션 뷰.
    /// SynergyManager 가 노출하는 시너지 → 보유 소환수 역참조 통로에서 목록을 조회하여 항목을 생성하고,
    /// 각 소환수의 전장(BattleArea) 배치 여부를 시각 상태로 적용한다.
    /// 표시 상태 동안 DefenderManager 가 발행하는 배치/제거 변경 알림(GlobalEventBus)을 구독하여
    /// 영향받는 항목 1개의 시각만 재판정한다.
    /// 시너지 종류(소환술사 효과 / 소환수 특성) 무관 일반 책임 — 역참조에 보유 정보가 누적되지 않은 시너지는
    /// 자연스럽게 빈 목록으로 표시된다 (분기 / 배제 로직 없음).
    /// </summary>
    public class SynergyDetailMemberList
    {
        private const string UnitClass = "unit";
        private const string UnitPlacedClass = "unit--placed";
        private const string UnitBenchClass = "unit--bench";
        private const string UnitPortraitClass = "unit__portrait";

        private readonly VisualElement _sectionRoot;
        private readonly VisualElement _listContainer;
        private readonly DefenderManager _defenderManager;
        private readonly Dictionary<UnitLoadOutData, VisualElement> _itemsByUnit = new();

        private SynergyActivation _activation;
        private bool _loggedListContainerMissing;
        private bool _loggedSynergyManagerMissing;
        private bool _loggedDefenderManagerMissing;

        public SynergyDetailMemberList(VisualElement sectionRoot, DefenderManager defenderManager = null)
        {
            _sectionRoot = sectionRoot;
            _listContainer = sectionRoot?.Q<VisualElement>("member-list");
            _defenderManager = defenderManager;
        }

        /// <summary>표시 대상 설정 + 항목 생성 + 배치 상태 시각 적용 + 알림 구독 개시.</summary>
        public void Bind(SynergyActivation activation)
        {
            if (activation == null)
            {
                return;
            }

            _activation = activation;
            CreateItems();
            RefreshAllPlacementStates();
            SubscribeDefenderEvents();
        }

        /// <summary>표시 상태 유지 교체 — 이전 구독 해제 + 항목 재생성·구독.</summary>
        public void Rebind(SynergyActivation activation)
        {
            Unbind();
            Bind(activation);
        }

        /// <summary>알림 구독 해제 + 항목 정리.</summary>
        public void Unbind()
        {
            UnsubscribeDefenderEvents();
            ClearItems();
            _activation = null;
        }

        // ── 항목 생성 ──

        private void CreateItems()
        {
            if (_listContainer == null)
            {
                if (!_loggedListContainerMissing)
                {
                    Debug.LogError("[SynergyDetailMemberList] UXML 의 'member-list' 컨테이너를 찾을 수 없습니다. SynergyDetailPanel.uxml 의 member-section 자식 구조를 확인하세요.");
                    _loggedListContainerMissing = true;
                }
                return;
            }

            SynergyManager manager = SynergyManager.Instance;
            if (manager == null)
            {
                if (!_loggedSynergyManagerMissing)
                {
                    Debug.LogError("[SynergyDetailMemberList] SynergyManager 인스턴스를 찾을 수 없습니다. 씬에 SynergyManager 가 배치되어 있는지 확인하세요.");
                    _loggedSynergyManagerMissing = true;
                }
                return;
            }

            // 키 부재 시 자연스럽게 빈 목록 — 시너지 종류 분기 없음.
            // 이 분기는 *정상 흐름* (소환수 특성 시너지의 자연 상태) 이므로 로그 없음.
            if (!manager.SynergyMembers.TryGetValue(_activation.Definition, out IReadOnlyList<UnitLoadOutData> units))
            {
                return;
            }

            foreach (UnitLoadOutData unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                VisualElement item = CreateItem(unit);
                _listContainer.Add(item);
                _itemsByUnit[unit] = item;
            }
        }

        private VisualElement CreateItem(UnitLoadOutData unit)
        {
            // 시안 구조: unit 박스(32x32) 안에 portrait 색면(18x18). 이름 라벨은 표시하지 않는다.
            var item = new VisualElement();
            item.AddToClassList(UnitClass);

            var portrait = new VisualElement();
            portrait.AddToClassList(UnitPortraitClass);
            if (unit.Unit != null && unit.Unit.Icon != null)
            {
                portrait.style.backgroundImage = new StyleBackground(unit.Unit.Icon);
            }
            item.Add(portrait);

            return item;
        }

        // ── 배치 상태 시각 ──

        private void RefreshAllPlacementStates()
        {
            foreach (KeyValuePair<UnitLoadOutData, VisualElement> kv in _itemsByUnit)
            {
                RefreshItemPlacement(kv.Key, kv.Value);
            }
        }

        private void RefreshItemPlacement(UnitLoadOutData unit, VisualElement item)
        {
            // placed / bench 클래스는 상호 배타적으로 유지한다 — 변경 시 양쪽 다 정리한 뒤 하나만 부여.
            item.RemoveFromClassList(UnitPlacedClass);
            item.RemoveFromClassList(UnitBenchClass);

            if (IsUnitPlacedInBattleArea(unit))
            {
                item.AddToClassList(UnitPlacedClass);
            }
            else
            {
                item.AddToClassList(UnitBenchClass);
            }
        }

        /// <summary>
        /// 동일 UnitLoadOutData 의 디펜더가 하나라도 BattleArea 에 배치되어 있으면 활성으로 판정.
        /// 다운 여부(ActionStateController.CurrentState == Downed)는 본 판정에 영향을 주지 않는다 (DoD-S13).
        /// </summary>
        private bool IsUnitPlacedInBattleArea(UnitLoadOutData unit)
        {
            if (_defenderManager == null)
            {
                if (!_loggedDefenderManagerMissing)
                {
                    Debug.LogError("[SynergyDetailMemberList] DefenderManager 가 주입되지 않았습니다 — 모든 항목이 비활성으로 표시됩니다. SynergyDetailPanel Inspector 의 'Defender Manager' [SerializeField] 를 연결하세요.");
                    _loggedDefenderManagerMissing = true;
                }
                return false;
            }

            foreach (Defender defender in _defenderManager.Defenders)
            {
                if (defender != null
                    && defender.UnitLoadOutData == unit
                    && defender.Placement == Placement.BattleArea)
                {
                    return true;
                }
            }
            return false;
        }

        // ── 알림 구독 ──

        private void SubscribeDefenderEvents()
        {
            GlobalEventBus.Subscribe<OnDefenderChangedEventDto>(HandleDefenderChanged);
            GlobalEventBus.Subscribe<OnDefenderPlacementChangedEventDto>(HandlePlacementChanged);
        }

        private void UnsubscribeDefenderEvents()
        {
            GlobalEventBus.Unsubscribe<OnDefenderChangedEventDto>(HandleDefenderChanged);
            GlobalEventBus.Unsubscribe<OnDefenderPlacementChangedEventDto>(HandlePlacementChanged);
        }

        private void HandleDefenderChanged(OnDefenderChangedEventDto dto)
        {
            if (dto.Defender == null)
            {
                return;
            }
            UnitLoadOutData unit = dto.Defender.UnitLoadOutData;
            if (unit != null && _itemsByUnit.TryGetValue(unit, out VisualElement item))
            {
                RefreshItemPlacement(unit, item);
            }
        }

        private void HandlePlacementChanged(OnDefenderPlacementChangedEventDto dto)
        {
            if (dto.defender == null)
            {
                return;
            }
            UnitLoadOutData unit = dto.defender.UnitLoadOutData;
            if (unit != null && _itemsByUnit.TryGetValue(unit, out VisualElement item))
            {
                RefreshItemPlacement(unit, item);
            }
        }

        // ── 정리 ──

        private void ClearItems()
        {
            _listContainer?.Clear();
            _itemsByUnit.Clear();
        }
    }
}
