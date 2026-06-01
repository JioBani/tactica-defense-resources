using System.Collections.Generic;
using System.Reflection;
using Common.Data.Units.RoleGroups;
using Common.Data.Units.UnitDefinitions;
using NUnit.Framework;
using Scenes.Battle.Feature.Markets;
using Scenes.Battle.Feature.Units.HealthBars;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.Editor
{
    /// <summary>
    /// TACD-303 role-group-badge-ui (그룹 C) 요구사항 테스트.
    /// 검증 기준은 UR/SR 완료 정의(DoD-S6·S7)이며 구현 분기 로직이 아니다.
    /// 표시 세터의 관찰 가능한 출력(sprite·visible)을 격리 검증한다. 화면 물리 위치는 프리팹 레이아웃 책임이라 검증 범위 밖.
    /// </summary>
    public class RoleGroupBadgeUiTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _spawned)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            _spawned.Clear();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"private 필드 '{fieldName}' 를 찾지 못했습니다.");
            field.SetValue(target, value);
        }

        private Sprite CreateSprite()
        {
            return Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        private RoleGroupDefinitionData CreateRoleGroup(RoleGroup kind, Sprite icon)
        {
            var definition = ScriptableObject.CreateInstance<RoleGroupDefinitionData>();
            SetPrivateField(definition, "roleGroup", kind);
            SetPrivateField(definition, "icon", icon);
            _spawned.Add(definition);
            return definition;
        }

        private UnitDefinitionData CreateUnit(RoleGroupDefinitionData roleGroup)
        {
            var unit = ScriptableObject.CreateInstance<UnitDefinitionData>();
            SetPrivateField(unit, "displayName", "테스트소환수");
            SetPrivateField(unit, "roleGroup", roleGroup);
            _spawned.Add(unit);
            return unit;
        }

        // HealthBar 인스턴스 + 역할군 아이콘 SpriteRenderer 슬롯을 연결해 생성한다.
        private HealthBar CreateHealthBar(out SpriteRenderer iconSlot)
        {
            var go = new GameObject("HealthBar");
            _spawned.Add(go);
            HealthBar healthBar = go.AddComponent<HealthBar>();
            iconSlot = go.AddComponent<SpriteRenderer>();
            SetPrivateField(healthBar, "roleGroupIconSprite", iconSlot);
            return healthBar;
        }

        // DefenderSlot 인스턴스 + 역할군 아이콘 Image 슬롯을 연결해 생성한다.
        private DefenderSlot CreateDefenderSlot(out Image iconSlot)
        {
            var go = new GameObject("DefenderSlot");
            _spawned.Add(go);
            DefenderSlot slot = go.AddComponent<DefenderSlot>();
            iconSlot = go.AddComponent<Image>();
            SetPrivateField(slot, "roleGroupIcon", iconSlot);
            return slot;
        }

        private static void InvokeSetRoleGroupIcon(DefenderSlot slot, UnitDefinitionData unit)
        {
            MethodInfo method = typeof(DefenderSlot)
                .GetMethod("SetRoleGroupIcon", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "DefenderSlot.SetRoleGroupIcon(UnitDefinitionData) 를 찾지 못했습니다.");
            method.Invoke(slot, new object[] { unit });
        }

        // DoD-S6: 체력바는 전달된 역할군 아이콘을 표시한다.
        [Test]
        public void RoleGroup_HealthBarShowsRoleGroupIconWhenProvided()
        {
            HealthBar healthBar = CreateHealthBar(out SpriteRenderer iconSlot);
            Sprite icon = CreateSprite();

            healthBar.SetRoleGroupIcon(icon);

            Assert.AreSame(icon, iconSlot.sprite, "체력바는 전달된 역할군 아이콘을 표시해야 한다.");
            Assert.IsTrue(iconSlot.enabled, "아이콘이 있으면 슬롯이 보여야 한다.");
        }

        // DoD-S6 (+ S5 소비 경계): 아이콘이 없으면(역할군 없는 유닛·아트 미할당) 체력바 슬롯을 비운다.
        [Test]
        public void RoleGroup_HealthBarLeavesIconSlotEmptyWhenNull()
        {
            HealthBar healthBar = CreateHealthBar(out SpriteRenderer iconSlot);

            healthBar.SetRoleGroupIcon(null);

            Assert.IsFalse(iconSlot.enabled, "아이콘이 없으면 체력바 슬롯은 비워(미표시)야 한다.");
        }

        // DoD-S7: 소환 카드는 소환수 종류의 역할군 아이콘을 표시한다.
        [Test]
        public void RoleGroup_SummonCardShowsRoleGroupIconWhenProvided()
        {
            DefenderSlot slot = CreateDefenderSlot(out Image iconSlot);
            Sprite icon = CreateSprite();
            UnitDefinitionData unit = CreateUnit(CreateRoleGroup(RoleGroup.Tank, icon));

            InvokeSetRoleGroupIcon(slot, unit);

            Assert.AreSame(icon, iconSlot.sprite, "카드는 소환수 종류의 역할군 아이콘을 표시해야 한다.");
            Assert.IsTrue(iconSlot.enabled, "아이콘이 있으면 카드 슬롯이 보여야 한다.");
        }

        // DoD-S7: 역할군은 있으나 아이콘이 미할당(아트 대기)이면 카드 슬롯을 비운다(오류 없이 graceful).
        [Test]
        public void RoleGroup_SummonCardLeavesIconSlotEmptyWhenRoleGroupIconMissing()
        {
            DefenderSlot slot = CreateDefenderSlot(out Image iconSlot);
            UnitDefinitionData unit = CreateUnit(CreateRoleGroup(RoleGroup.Supporter, null));

            InvokeSetRoleGroupIcon(slot, unit);

            Assert.IsFalse(iconSlot.enabled, "역할군 아이콘이 아직 없으면 카드 슬롯은 비워야 한다.");
        }

        // DoD-S7: 소환수 역할군이 미설정이면 카드 슬롯을 비운다(데이터 누락 — 빈 표시).
        [Test]
        public void RoleGroup_SummonCardLeavesIconSlotEmptyWhenRoleGroupMissing()
        {
            DefenderSlot slot = CreateDefenderSlot(out Image iconSlot);
            UnitDefinitionData unit = CreateUnit(null);

            InvokeSetRoleGroupIcon(slot, unit);

            Assert.IsFalse(iconSlot.enabled, "역할군이 미설정이면 카드 슬롯은 비워(미표시)야 한다.");
        }
    }
}
