namespace Common.Data.Units.RoleGroups
{
    /// <summary>
    /// 소환수 역할군 고유 식별자. 게임 전반에서 공통으로 쓰는 분류 기준이며,
    /// 기존 시너지 분류(SynergyId)와는 별개의 독립축이다.
    /// </summary>
    public enum RoleGroup
    {
        /// <summary>미설정 방어값. 소환수 종류 데이터는 None을 가지면 안 된다.</summary>
        None = 0,

        /// <summary>탱커. 체력·방어력이 높아 전열을 지키고 피격을 대신 받는다.</summary>
        Tank = 1,

        /// <summary>평타 딜러. 기본 공격을 반복해 꾸준한 피해를 준다.</summary>
        AttackDealer = 2,

        /// <summary>스킬 딜러. 기본 공격은 약하지만 강력한 스킬로 변수를 만든다.</summary>
        SkillDealer = 3,

        /// <summary>서포터. 아군 체력을 회복하고 팀을 보조한다(회복·보조는 스킬로 구현).</summary>
        Supporter = 4,
    }
}
