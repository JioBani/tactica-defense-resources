using System;
using System.Collections.Generic;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.Draggable;
using Common.Scripts.Enums;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.StatusEffect;
using Scenes.Battle.Feature.Events.RoundEvents;
using Scenes.Battle.Feature.Units.ActionStates;
using Scenes.Battle.Feature.Units.Attackers;
using Scenes.Battle.Feature.Units.HealthBars;
using Scenes.Battle.Feature.Units.UnitStats.UnitStatSheets;
using Scenes.Battle.Feature.WaitingAreas;
using UnityEngine;

namespace Scenes.Battle.Feature.Units
{
    // TODO: 기능이 많아지는 경우 분리
    public class Unit : MonoBehaviour
    {
        [SerializeField] protected HealthBar healthBar;
        [SerializeField] private ActionStateController actionStateController;
        public ActionStateController ActionStateController => actionStateController;

        [SerializeField] private StatusEffectController statusEffectController;

        /// <summary>이 유닛의 SE 관리자. 시너지 효과 등 SE를 부여받을 때 사용.</summary>
        public StatusEffectController StatusEffectController => statusEffectController;

        protected Draggable2D Draggable;

        public UnitLoadOutData UnitLoadOutData;
        
        // TODO: unit load out data 로 이동
        public Fraction fraction;
        public readonly UnitStatSheet StatSheet = new();

        public Action<Unit> OnSpawnEvent;
        
        protected virtual void Awake()
        {
            Draggable = GetComponent<Draggable2D>();

            StatSheet.Health.OnChange += (value) =>
            {
                healthBar.Display(value / StatSheet.MaxHealth.CurrentValue);
            };

            StatSheet.OnGradeChanged += OnGradeChanged;
        }

        /// <summary>성급 또는 강화 단계 변경 시 HP 바 테두리 색을 갱신한다.</summary>
        private void OnGradeChanged(GradeChangedInfo info)
        {
            healthBar.SetStarGrade(info.Star);
        }

        

        public void MoveToWaitingArea()
        {
            List<ExclusiveDropZone2D> areas = WaitingAreaReferences.Instance.waitingAreas;

            // TODO: ?
            areas.Find((zone) =>
            {
                if (zone.occupant == null)
                {
                    Draggable.MoveToDropZone(zone);
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// UnitGenerator에 의해 소환되었을 때 호출된다.
        /// 성급/스탯/상태를 초기 세팅한다.
        /// </summary>
        /// <param name="unitLoadOutData">유닛 설정 데이터.</param>
        /// <param name="star">초기 성급. 마켓 등장 시 결정된다.</param>
        public void SetSpawn(UnitLoadOutData unitLoadOutData, int star = 1)
        {
            UnitLoadOutData = unitLoadOutData;
            StatSheet.Init(unitLoadOutData.Stats, star);
            healthBar.SetStarGrade(StatSheet.Star);

            OnSpawn(unitLoadOutData);
            OnSpawnEvent?.Invoke(this);
        }

        protected virtual void OnSpawn(UnitLoadOutData unitLoadOutData)
        {
            
        }
    }
}

