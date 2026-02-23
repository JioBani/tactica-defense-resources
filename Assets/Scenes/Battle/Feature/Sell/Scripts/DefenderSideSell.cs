using Common.Scripts.BubbleMessage;
using Common.Scripts.Draggable;
using Common.Scripts.InspectorDescriptionAttributes;
using Scenes.Battle.Feature.Fusion;
using Scenes.Battle.Feature.Markets;
using Scenes.Battle.Feature.Rounds;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Unit.Defenders;
namespace Scenes.Battle.Feature.Sells
{

    [InspectorDescription("정비 페이즈에서만 수호자 배치 가능하도록 룰 추가한 확장 Sell",InspectorMessageType.Info)]

    public class DefenderSideSell : ExclusiveDropZone2D, IDropRule
    {
        private Defender _defender;

        void Awake()
        {
            AddRule(this);
        }

        /// <summary>
        /// 점유자가 있을 때 강화 합성 조건을 확인하고, 가능하면 스왑 대신 강화를 실행한다.
        /// </summary>
        public override void OnDrop(Draggable2D draggable, DropZone2D before)
        {
            if (occupant != null
                && DefenderFusionManager.Instance != null
                && draggable.TryGetComponent<Defender>(out var material)
                && occupant.TryGetComponent<Defender>(out var target)
                && DefenderFusionManager.Instance.TryEnhanceFusion(material, target))
            {
                return;
            }

            base.OnDrop(draggable, before);
        }

        public bool CanAccept(Draggable2D draggable, DropZone2D before, DropZone2D after)
        {
            if (RoundManager.Instance.CurrentState != PhaseType.Maintenance)
            {
                BubbleMessageSpawner.Instance.SpawnAtWorld(
                    "전투 중에는 배치할 수 없습니다!",
                    draggable.transform.position
                );
                return false;
            }

            if (!draggable.TryGetComponent<Defender>(out var defender))
            {
                return false;
            }

            bool wouldIncreaseBattleCount = WouldIncreaseBattleCount(before, occupant);

            if (wouldIncreaseBattleCount && MarketManager.Instance.IsDefenderLimitExceeded())
            {
                BubbleMessageSpawner.Instance.SpawnAtWorld(
                    "배치 한계 초과!",
                    draggable.transform.position
                );
                return false;
            }

            return true;
        }

        public void OnDropped(Draggable2D draggable, DropZone2D before, DropZone2D after)
        {
            _defender = draggable.GetComponent<Defender>();
            _defender.OnDrop(Placement.BattleArea);
        }

        public void OnDragOut(Draggable2D item, DropZone2D zone)
        {
        }

        /// <summary>
        /// 이 배치가 전장 인원을 증가시키는지 판별한다.
        /// 전장→전장 이동이나 스왑은 인원 변동이 없으므로 false를 반환한다.
        /// </summary>
        public static bool WouldIncreaseBattleCount(DropZone2D before, Draggable2D currentOccupant)
        {
            bool isFromBattleArea = before is DefenderSideSell;
            bool isTargetOccupied = currentOccupant != null;
            return !isFromBattleArea && !isTargetOccupied;
        }
    }
}
