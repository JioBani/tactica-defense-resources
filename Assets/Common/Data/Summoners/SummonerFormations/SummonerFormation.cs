using System.Collections.Generic;
using Common.Data.Summoners.SummonerLoadOuts;

namespace Common.Data.Summoners.SummonerFormations
{
    /// <summary>출전 편성 소환술사 목록을 보유하는 런타임 데이터.</summary>
    public class SummonerFormation
    {
        private readonly IReadOnlyList<SummonerLoadOutData> _summoners;

        /// <summary>편성된 소환술사 목록. 인덱스 순서가 배치 순서(위→아래)이다.</summary>
        public IReadOnlyList<SummonerLoadOutData> Summoners => _summoners;

        public SummonerFormation(IReadOnlyList<SummonerLoadOutData> summoners)
        {
            _summoners = summoners;
        }
    }
}
