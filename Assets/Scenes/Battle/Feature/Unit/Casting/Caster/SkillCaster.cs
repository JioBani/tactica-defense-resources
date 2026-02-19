using Common.Data.Skills.SkillDefinitions;
using Common.Scripts.StateBase;
using Common.Scripts.Timers;
using Scenes.Battle.Feature.Rounds;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Unit.Skills;
using Scenes.Battle.Feature.Unit.Skills.Castables;
using Scenes.Battle.Feature.Unit.Skills.Executables;
using Scenes.Battle.Feature.Unit.Skills.Skills;
using Scenes.Battle.Feature.Units.ActionStates;
using Scenes.Battle.Feature.Units.Attackers;
using UnityEngine;

namespace Scenes.Battle.Feature.Unit.Castables
{
    public class SkillCaster : MonoBehaviour, IStateListener<PhaseType>, IStateListener<ActionStateType>
    {
        [SerializeField] private Units.Unit unit; // 이 스킬 실행기를 가진 유닛 참조
        [SerializeField] private Attacker attacker; // 공격자(타겟을 가진 컴포넌트)
        /// <summary>다운 상태 진입 시 스킬 차단을 위한 상태 머신 참조</summary>
        [SerializeField] private Units.ActionStates.ActionStateController actionStateController;
        private SkillDefinitionData _skillData; // 스킬 정의 데이터
        private float _coolTime; // 스킬의 쿨타임(초)
        private Timer _skillTimer; // 쿨타임 타이머
        private bool _isSkillReady = false; // 스킬 사용 가능 여부
        private SkillCast _skill;

        private void Awake()
        {
            // 컴포넌트 초기화: 같은 게임오브젝트의 Unit 컴포넌트를 가져옴
            unit = GetComponent<Units.Unit>();

            // IStateListener 등록
            RoundManager.Instance.RegisterListener(this);
            actionStateController.RegisterListener(this);
        }

        private void OnEnable()
        {
            // 유닛 스폰 이벤트 등록
            unit.OnSpawnEvent += OnSpawn;
        }

        private void OnDisable()
        {
            // 이벤트 해제 정리
            unit.OnSpawnEvent -= OnSpawn;
        }

        // IStateListener 명시적 구현
        void IStateListener<PhaseType>.OnStateEnter(PhaseType phaseType)
        {
            if (phaseType == PhaseType.Combat)
            {
                OnEnterCombat();
            }
        }

        void IStateListener<PhaseType>.OnStateRun(PhaseType phaseType)
        {
            // Run 단계에서는 특별한 동작 없음
        }

        void IStateListener<PhaseType>.OnStateExit(PhaseType phaseType)
        {
            if (phaseType == PhaseType.Combat)
            {
                OnExitCombat();
            }
        }

        private void OnDestroy()
        {
            RoundManager.Instance.UnregisterListener(this);
            actionStateController.UnregisterListener(this);
        }

        // IStateListener<ActionStateType> 명시적 구현
        /// <summary>
        /// 유닛이 Downed 상태에 진입하면 스킬 사용을 차단하고 타이머를 정지한다.
        /// </summary>
        void IStateListener<ActionStateType>.OnStateEnter(ActionStateType stateType)
        {
            if (stateType == ActionStateType.Downed)
            {
                _isSkillReady = false;
                _skillTimer?.Stop();
            }
        }

        void IStateListener<ActionStateType>.OnStateRun(ActionStateType stateType) { }

        void IStateListener<ActionStateType>.OnStateExit(ActionStateType stateType) { }

        private void Update()
        {
            if (_isSkillReady && _skill.CanCast())
            {
                Cast();
            }
        }

        private void OnSpawn(Units.Unit _)
        {
            // 유닛 스폰 시 초기화 수행
            Initialize();
        }

        private void Initialize()
        {
            // 스킬 데이터와 인스턴스 초기화, 타이머 생성
            _skillData = unit.UnitLoadOutData.Skill;
            _coolTime = _skillData.CoolTime;

            _skillTimer = TimerManager.Instance.Make(_coolTime, SetSkillReady);
            
            _skill = SkillFactory.Instance.CreateSkill(new SkillCreateContext(
                data : unit.UnitLoadOutData.Skill,
                caster: this,
                attacker: attacker
            ));

            _skill.OnCastEvent += OnCast;
            _skill.OnExecuteEndEvent += OnExecuteEnd;
        }

        private void OnEnterCombat()
        {
            // 전투 시작 시 스킬을 사용 가능으로 만들고 타이머 시작
            _isSkillReady = true;
            _skillTimer.Start();
        }

        private void OnExitCombat()
        {
            // 전투 종료 시 타이머 정지(스킬 비활성화 등 추가 로직 필요 시 여기에 추가)
            _skillTimer.Stop();
        }

        // 스킬 타임아웃에 따라 스킬 사용 가능여부 변경
        private void SetSkillReady(bool isTimeout, float _)
        {
            _isSkillReady = isTimeout;
        }

        private void Cast()
        {
            _skill.Cast();
        }
        
        private void OnCast(Castable _, Executable __)
        {
            // 스킬 캐스트시 타이머 정지
            _skillTimer.Reset();
        }
        
        private void OnExecuteEnd(Executable executable)
        {
            // 스킬 사용 종료시 타이머 실행
            _skillTimer.Resume();
        }
    }
}