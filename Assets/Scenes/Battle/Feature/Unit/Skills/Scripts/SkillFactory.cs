using System;
using Common.Data.Skills.SkillDefinitions;
using Common.Scripts.SceneSingleton;
using Scenes.Battle.Feature.Unit.Skills.Castables;
using Scenes.Battle.Feature.Unit.Skills.Skills;

namespace Scenes.Battle.Feature.Unit.Skills
{
    /// <summary>
    /// SkillDefinitionData 타입에 따라 올바른 SkillCast 인스턴스를 생성한다.
    /// 새 스킬 추가 시 switch 분기를 추가한다.
    /// </summary>
    public class SkillFactory : SceneSingleton<SkillFactory>
    {
        public SkillCast CreateSkill(SkillCreateContext context)
        {
            return context.Data switch
            {
                FireArrowDefinitionData => new FireArrow(context.Data, context.Attacker),
                _ => throw new ArgumentException(
                    $"지원하지 않는 SkillDefinitionData 타입: {context.Data.GetType().Name}")
            };
        }
    }
}
