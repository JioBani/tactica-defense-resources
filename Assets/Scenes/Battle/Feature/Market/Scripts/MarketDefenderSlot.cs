using Common.Data.Units.UnitLoadOuts;

namespace Scenes.Battle.Feature.Markets
{
    public class MarketDefenderSlot
    {
        public bool IsSold { get; private set; }
        public UnitLoadOutData UnitLoadOutData { get; private set; }

        /// <summary>마켓에 등장한 소환수의 초기 성급.</summary>
        public int Star { get; private set; }

        public MarketDefenderSlot(UnitLoadOutData unitLoadOutData, int star = 1)
        {
            UnitLoadOutData = unitLoadOutData;
            Star = star;
            IsSold = false;
        }

        /// <summary>구매 완료 처리.</summary>
        public void MarkAsSold()
        {
            IsSold = true;
        }
    }
}