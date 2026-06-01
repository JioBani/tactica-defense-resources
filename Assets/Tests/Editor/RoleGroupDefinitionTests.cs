using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common.Data.Synergies;
using Common.Data.Units.RoleGroups;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// TACD-301 role-group-definition (그룹 A) 요구사항 테스트.
    /// 검증 기준은 UR/SR 완료 정의(DoD-S1·S2)이며 구현 동작이 아니다.
    /// </summary>
    public class RoleGroupDefinitionTests
    {
        // 명세된 4종 역할군(None 제외)
        private static readonly RoleGroup[] SpecifiedKinds =
        {
            RoleGroup.Tank,
            RoleGroup.AttackDealer,
            RoleGroup.SkillDealer,
            RoleGroup.Supporter,
        };

        // 정의가 노출해야 하는 공개 인스턴스 프로퍼티(전투 수치 없음 검증용 기준 집합)
        private static readonly HashSet<string> AllowedPublicProperties = new HashSet<string>
        {
            nameof(RoleGroupDefinitionData.RoleGroup),
            nameof(RoleGroupDefinitionData.DisplayName),
            nameof(RoleGroupDefinitionData.Icon),
        };

        private RoleGroupDefinitionData CreateDefinition(RoleGroup kind, string displayName, Sprite icon = null)
        {
            var definition = ScriptableObject.CreateInstance<RoleGroupDefinitionData>();
            SetPrivateField(definition, "roleGroup", kind);
            SetPrivateField(definition, "displayName", displayName);
            SetPrivateField(definition, "icon", icon);
            return definition;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"private 필드 '{fieldName}' 를 찾지 못했습니다.");
            field.SetValue(target, value);
        }

        // DoD-S1: None 을 제외한 역할군 식별자는 정확히 4종이어야 한다.
        [Test]
        public void RoleGroup_DefinesExactlyFourRoleGroupsBesidesNone()
        {
            int nonNoneCount = Enum.GetValues(typeof(RoleGroup))
                .Cast<RoleGroup>()
                .Count(kind => kind != RoleGroup.None);

            Assert.AreEqual(4, nonNoneCount, "역할군은 None 외 정확히 4종이어야 한다.");
        }

        // DoD-S1: 탱커·평타 딜러·스킬 딜러·서포터 4종이 모두 정의되어야 한다.
        [Test]
        public void RoleGroup_ContainsTheFourSpecifiedKinds()
        {
            List<RoleGroup> defined = Enum.GetValues(typeof(RoleGroup))
                .Cast<RoleGroup>()
                .ToList();

            foreach (RoleGroup expected in SpecifiedKinds)
            {
                Assert.Contains(expected, defined, $"역할군 '{expected}' 가 정의되어 있지 않다.");
            }
        }

        // DoD-S1/방어: 미설정 방어값 None 은 기본값(0)이어야 한다.
        [Test]
        public void RoleGroup_NoneIsDefaultZero()
        {
            Assert.AreEqual(0, (int)RoleGroup.None, "미설정 방어값 None 은 0 이어야 한다.");
            Assert.AreEqual(RoleGroup.None, default(RoleGroup), "기본값은 None 이어야 한다.");
        }

        // DoD-S2: 정의는 자신이 어느 역할군인지(식별값)를 보유·노출해야 한다.
        [Test]
        public void RoleGroupDefinition_ExposesAssignedRoleGroupKind()
        {
            RoleGroupDefinitionData definition = CreateDefinition(RoleGroup.Tank, "탱커");

            Assert.AreEqual(RoleGroup.Tank, definition.RoleGroup);
        }

        // DoD-S2: 정의는 표시 이름을 보유·노출해야 한다.
        [Test]
        public void RoleGroupDefinition_ExposesAssignedDisplayName()
        {
            const string expectedDisplayName = "서포터";
            RoleGroupDefinitionData definition = CreateDefinition(RoleGroup.Supporter, expectedDisplayName);

            Assert.AreEqual(expectedDisplayName, definition.DisplayName);
        }

        // DoD-S2: 정의는 고유 아이콘을 보유·노출하는 구조여야 한다(값 round-trip).
        [Test]
        public void RoleGroupDefinition_CanHoldUniqueIcon()
        {
            var sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f));
            RoleGroupDefinitionData definition = CreateDefinition(RoleGroup.SkillDealer, "스킬 딜러", sprite);

            Assert.AreSame(sprite, definition.Icon, "정의는 할당된 아이콘을 그대로 노출해야 한다.");
        }

        // DoD-S2: 역할군 정의는 전투 수치를 보유하지 않는다 — 공개 API 는 {RoleGroup, DisplayName, Icon} 뿐.
        [Test]
        public void RoleGroupDefinition_CarriesNoCombatStats()
        {
            IEnumerable<string> publicProps = typeof(RoleGroupDefinitionData)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(p => p.Name);

            List<string> unexpected = publicProps
                .Where(name => !AllowedPublicProperties.Contains(name))
                .ToList();

            Assert.IsEmpty(
                unexpected,
                $"역할군 정의에 허용되지 않은(전투 수치 의심) 공개 프로퍼티가 있다: {string.Join(", ", unexpected)}");
        }

        // DoD-S1: 역할군 정의는 시너지 정의와 별개 타입(독립축)이어야 한다.
        [Test]
        public void RoleGroupDefinition_IsSeparateTypeFromSynergyDefinition()
        {
            Assert.AreNotEqual(
                typeof(SynergyDefinitionData),
                typeof(RoleGroupDefinitionData),
                "역할군 정의는 시너지 정의와 같은 타입이면 안 된다.");
            Assert.IsFalse(
                typeof(SynergyDefinitionData).IsAssignableFrom(typeof(RoleGroupDefinitionData)),
                "역할군 정의는 시너지 정의를 상속/대체하면 안 된다(독립축).");
        }

        // DoD-S1: 출하된 역할군 정의 자산은 4종 역할군에 1:1 로 대응해야 한다.
        [Test]
        public void RoleGroupAssets_OneAssetPerRoleGroupKind()
        {
            List<RoleGroupDefinitionData> assets = LoadAllRoleGroupAssets();

            List<RoleGroup> kinds = assets.Select(a => a.RoleGroup).ToList();

            CollectionAssert.AreEquivalent(
                SpecifiedKinds,
                kinds,
                "출하 자산은 4종 역할군에 정확히 1:1 대응해야 한다(중복·누락·None 금지).");
        }

        // DoD-S2: 출하된 역할군 정의 자산은 모두 표시 이름을 가져야 한다.
        [Test]
        public void RoleGroupAssets_EachHasNonEmptyDisplayName()
        {
            List<RoleGroupDefinitionData> assets = LoadAllRoleGroupAssets();

            foreach (RoleGroupDefinitionData asset in assets)
            {
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(asset.DisplayName),
                    $"역할군 '{asset.RoleGroup}' 자산에 표시 이름이 비어 있다.");
            }
        }

        private static List<RoleGroupDefinitionData> LoadAllRoleGroupAssets()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(RoleGroupDefinitionData)}");
            List<RoleGroupDefinitionData> assets = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<RoleGroupDefinitionData>)
                .Where(asset => asset != null)
                .ToList();

            Assert.AreEqual(
                4,
                assets.Count,
                "역할군 정의 자산은 4개여야 한다(탱커·평타 딜러·스킬 딜러·서포터).");
            return assets;
        }
    }
}
