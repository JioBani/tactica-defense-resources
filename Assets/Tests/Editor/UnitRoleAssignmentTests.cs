using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common.Data.Summoners.SummonerUnitDefinitions;
using Common.Data.Units.RoleGroups;
using Common.Data.Units.UnitDefinitions;
using Common.Data.Units.UnitLoadOuts;
using NUnit.Framework;
using Scenes.Battle.Feature.Unit.Defenders;
using Scenes.Battle.Feature.Units;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// TACD-302 unit-role-assignment (그룹 B) 요구사항 테스트.
    /// 검증 기준은 UR/SR 완료 정의(DoD-S3·S4·S5)이며 구현 동작이 아니다.
    /// </summary>
    public class UnitRoleAssignmentTests
    {
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"private 필드 '{fieldName}' 를 찾지 못했습니다.");
            field.SetValue(target, value);
        }

        private RoleGroupDefinitionData CreateRoleGroup(RoleGroup kind, string displayName)
        {
            var definition = ScriptableObject.CreateInstance<RoleGroupDefinitionData>();
            SetPrivateField(definition, "roleGroup", kind);
            SetPrivateField(definition, "displayName", displayName);
            return definition;
        }

        // DoD-S3: 소환수 종류 데이터는 할당된 역할군을 보유·노출해야 한다.
        [Test]
        public void RoleGroup_UnitDefinitionHoldsAssignedRoleGroup()
        {
            RoleGroupDefinitionData tank = CreateRoleGroup(RoleGroup.Tank, "탱커");
            var unitDefinition = ScriptableObject.CreateInstance<UnitDefinitionData>();
            SetPrivateField(unitDefinition, "roleGroup", tank);

            Assert.AreSame(tank, unitDefinition.RoleGroup, "종류 데이터는 할당된 역할군을 그대로 보유해야 한다.");
        }

        // DoD-S3: 종류 데이터의 역할군 홀더는 단수(컬렉션이 아님)여야 한다 — "역할군 하나".
        [Test]
        public void RoleGroup_UnitDefinitionRoleGroupHolderIsSingular()
        {
            PropertyInfo roleGroupProperty = typeof(UnitDefinitionData)
                .GetProperty(nameof(UnitDefinitionData.RoleGroup), BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(roleGroupProperty, "종류 데이터에 RoleGroup 프로퍼티가 있어야 한다.");
            Assert.AreEqual(
                typeof(RoleGroupDefinitionData),
                roleGroupProperty.PropertyType,
                "역할군 홀더는 단일 RoleGroupDefinitionData(컬렉션 아님)여야 한다 — 소환수는 역할군 하나.");
        }

        // DoD-S4: 전장 소환수는 자기 종류 데이터를 통해 역할군에 도달한다 — 종류와 동일 출처.
        [Test]
        public void RoleGroup_BattlefieldUnitResolvesRoleGroupFromKindDefinition()
        {
            RoleGroupDefinitionData skillDealer = CreateRoleGroup(RoleGroup.SkillDealer, "스킬 딜러");
            var kindDefinition = ScriptableObject.CreateInstance<UnitDefinitionData>();
            SetPrivateField(kindDefinition, "roleGroup", skillDealer);
            var loadOut = ScriptableObject.CreateInstance<UnitLoadOutData>();
            SetPrivateField(loadOut, "unit", kindDefinition);

            RoleGroupDefinitionData resolved = loadOut.Unit.RoleGroup;

            Assert.AreSame(
                skillDealer,
                resolved,
                "전장 소환수가 로드아웃→종류 데이터 경로로 도달한 역할군은 종류의 역할군과 같아야 한다.");
        }

        // DoD-S4: 런타임(Unit·Defender)은 역할군 전용 상태를 들지 않는다 →
        //         합성·성급 상승·추가 소환에도 역할군이 종류에 묶여 불변임을 구조적으로 보장.
        [Test]
        public void RoleGroup_RuntimeUnitCarriesNoOwnRoleGroupState()
        {
            List<string> runtimeRoleGroupMembers = CollectRoleGroupMemberNames(typeof(Unit))
                .Concat(CollectRoleGroupMemberNames(typeof(Defender)))
                .ToList();

            Assert.IsEmpty(
                runtimeRoleGroupMembers,
                "런타임 소환수에 역할군 전용 필드/프로퍼티가 있으면 안 된다(종류 데이터 단일 출처 → 불변). "
                + $"발견: {string.Join(", ", runtimeRoleGroupMembers)}");
        }

        // DoD-S5: 소환술사 종류(SummonerUnitDefinitionData)는 기본적으로 역할군 값을 갖지 않는다.
        [Test]
        public void RoleGroup_SummonerUnitDefinitionHasNoRoleGroupValueByDefault()
        {
            var summonerDefinition = ScriptableObject.CreateInstance<SummonerUnitDefinitionData>();

            Assert.IsNull(summonerDefinition.RoleGroup, "소환술사 종류는 역할군 값을 갖지 않아야 한다.");
        }

        // DoD-S5: 출하된 소환술사 자산은 모두 역할군 값이 없어야 한다.
        [Test]
        public void RoleGroup_ShippedSummonerUnitDefinitionsHaveNoRoleGroupValue()
        {
            List<SummonerUnitDefinitionData> summoners = LoadAllAssets<SummonerUnitDefinitionData>();

            foreach (SummonerUnitDefinitionData summoner in summoners)
            {
                Assert.IsNull(
                    summoner.RoleGroup,
                    $"소환술사 자산 '{summoner.name}' 에 역할군 값이 설정되어 있다(소환술사는 역할군 미보유).");
            }
        }

        private static IEnumerable<string> CollectRoleGroupMemberNames(System.Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

            IEnumerable<string> fields = type.GetFields(flags)
                .Where(f => f.Name.ToLowerInvariant().Contains("rolegroup"))
                .Select(f => $"{type.Name}.{f.Name}");
            IEnumerable<string> properties = type.GetProperties(flags)
                .Where(p => p.Name.ToLowerInvariant().Contains("rolegroup"))
                .Select(p => $"{type.Name}.{p.Name}");

            return fields.Concat(properties);
        }

        private static List<T> LoadAllAssets<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .ToList();
        }
    }
}
