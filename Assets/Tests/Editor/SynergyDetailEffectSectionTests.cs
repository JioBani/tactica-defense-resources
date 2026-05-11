using System.Collections.Generic;
using System.Reflection;
using Common.Data.Synergies;
using Common.Scripts.SerializableDictionary;
using NUnit.Framework;
using Scenes.Battle.Feature.Synergy;
using Scenes.Battle.Feature.Ui.SynergyInfo;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tests.Editor
{
    /// <summary>
    /// TACD-296 effect-info 구현 단위의 요구사항 테스트.
    /// SR 그룹 B (DoD-S7 ~ DoD-S10) — 시너지 효과 정보 표시.
    ///
    /// 기대값 도출 근거: 시스템요구사항.md / 사용자요구사항.md 의 그룹 B DoD 항목.
    /// 구현 코드의 분기 로직을 따라가지 않는다.
    ///
    /// 픽스처 전략:
    /// - sectionRoot VisualElement 를 수동 구성 (synergy-name / synergy-description / tier-list 명명 자식).
    /// - SynergyDefinitionData private 필드 리플렉션 주입.
    /// - SynergyTier struct private 필드 박싱 후 리플렉션 주입.
    /// - SynergyActivation.Recalculate(int) 로 ActiveTier 를 자연스럽게 유도하여 RxValue OnChange 발화.
    /// </summary>
    public class SynergyDetailEffectSectionTests
    {
        private const string TierItemActiveClass = "tier-item--active";
        private const string TierItemClass = "tier-item";

        private VisualElement _sectionRoot;
        private Label _nameLabel;
        private Label _descriptionLabel;
        private VisualElement _tierListContainer;
        private SynergyDetailEffectSection _section;

        private readonly List<ScriptableObject> _createdAssets = new();

        [SetUp]
        public void SetUp()
        {
            _sectionRoot = new VisualElement { name = "effect-section" };
            _nameLabel = new Label { name = "synergy-name" };
            _descriptionLabel = new Label { name = "synergy-description" };
            _tierListContainer = new VisualElement { name = "tier-list" };
            _sectionRoot.Add(_nameLabel);
            _sectionRoot.Add(_descriptionLabel);
            _sectionRoot.Add(_tierListContainer);

            _section = new SynergyDetailEffectSection(_sectionRoot);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject so in _createdAssets)
            {
                if (so != null) Object.DestroyImmediate(so);
            }
            _createdAssets.Clear();
        }

        // ── DoD-S7: 정적 정보 렌더 ──

        [Test]
        public void Test_TC701_Bind_RendersDisplayNameToNameLabel()
        {
            SynergyActivation act = MakeActivation(
                displayName: "테스트시너지",
                description: "임의 설명",
                tiers: new[] { (tier: 1, required: 2, consts: Empty()) });

            _section.Bind(act);

            Assert.That(_nameLabel.text, Is.EqualTo("테스트시너지"));
        }

        [Test]
        public void Test_TC702_Bind_RendersDescriptionToDescriptionLabel()
        {
            const string desc = "이 시너지는 강력하다.";
            SynergyActivation act = MakeActivation(
                displayName: "X",
                description: desc,
                tiers: new[] { (tier: 1, required: 2, consts: Empty()) });

            _section.Bind(act);

            Assert.That(_descriptionLabel.text, Is.EqualTo(desc));
        }

        [Test]
        public void Test_TC703_Bind_PopulatesTierListWithCountMatchingDefinition()
        {
            SynergyActivation act = MakeActivation(
                displayName: "X",
                description: "Y",
                tiers: new[]
                {
                    (tier: 1, required: 2, consts: ConstsOf(("a", 1f))),
                    (tier: 2, required: 4, consts: ConstsOf(("b", 2f))),
                    (tier: 3, required: 6, consts: ConstsOf(("c", 3f))),
                });

            _section.Bind(act);

            Assert.That(_tierListContainer.childCount, Is.EqualTo(3),
                "tier-list 자식 개수는 Definition.Tiers 의 수와 일치해야 한다.");
        }

        [Test]
        public void Test_TC704_EachTierItemHasTierItemUssClass()
        {
            SynergyActivation act = MakeActivation(
                displayName: "X", description: "Y",
                tiers: new[]
                {
                    (tier: 1, required: 2, consts: Empty()),
                    (tier: 2, required: 4, consts: Empty()),
                });

            _section.Bind(act);

            foreach (VisualElement child in _tierListContainer.Children())
            {
                Assert.That(child.ClassListContains(TierItemClass), Is.True,
                    "각 티어 항목은 tier-item USS 클래스를 가져야 한다.");
            }
        }

        // ── DoD-S8: 플레이스홀더 원본 그대로 ──

        [Test]
        public void Test_TC801_Bind_PreservesPlaceholdersInDescription_NoSubstitution()
        {
            const string desc = "공격력 +@AttackPower@% / @Bonus*3@턴 동안";
            SynergyActivation act = MakeActivation(
                displayName: "X", description: desc,
                tiers: new[] { (tier: 1, required: 2, consts: ConstsOf(("AttackPower", 10f), ("Bonus", 2f))) });

            _section.Bind(act);

            Assert.That(_descriptionLabel.text, Is.EqualTo(desc),
                "Description 의 @Placeholder@ / @Name*N@ 는 본 이슈에서 치환되지 않고 원본 그대로여야 한다.");
        }

        [Test]
        public void Test_TC802_Bind_NullDescription_RendersEmptyString()
        {
            SynergyActivation act = MakeActivation(
                displayName: "X", description: null,
                tiers: new[] { (tier: 1, required: 2, consts: Empty()) });

            _section.Bind(act);

            Assert.That(_descriptionLabel.text, Is.EqualTo(string.Empty));
        }

        // ── DoD-S9: 활성 티어 강조 ──

        [Test]
        public void Test_TC901_ActiveTier_HighlightsOnlyActiveStageItem()
        {
            SynergyActivation act = MakeActivation(
                displayName: "X", description: "Y",
                tiers: new[]
                {
                    (tier: 1, required: 2, consts: Empty()),
                    (tier: 2, required: 4, consts: Empty()),
                });

            // 카운트 2 → 1단계 활성
            act.Recalculate(2);

            _section.Bind(act);

            AssertActiveStage(expectedActiveStage: 1, allStages: new[] { 1, 2 });
        }

        [Test]
        public void Test_TC902_BelowAllThresholds_NoItemHighlighted()
        {
            SynergyActivation act = MakeActivation(
                displayName: "X", description: "Y",
                tiers: new[]
                {
                    (tier: 1, required: 2, consts: Empty()),
                    (tier: 2, required: 4, consts: Empty()),
                });

            // 카운트 1 → 비활성
            act.Recalculate(1);

            _section.Bind(act);

            foreach (VisualElement child in _tierListContainer.Children())
            {
                Assert.That(child.ClassListContains(TierItemActiveClass), Is.False,
                    "비활성 상태에서는 어떤 티어 항목에도 강조 USS 클래스가 붙어선 안 된다.");
            }
        }

        [Test]
        public void Test_TC903_TierItems_AppearInDefinitionOrder_AscendingThreshold()
        {
            SynergyActivation act = MakeActivation(
                displayName: "X", description: "Y",
                tiers: new[]
                {
                    (tier: 1, required: 2, consts: Empty()),
                    (tier: 2, required: 4, consts: Empty()),
                    (tier: 3, required: 6, consts: Empty()),
                });

            _section.Bind(act);

            // tier 단계 라벨의 텍스트를 추출하여 순서를 검증한다.
            // CreateTierItem 의 첫 자식이 stage Label("{Tier}단계").
            var stageOrder = new List<string>();
            foreach (VisualElement item in _tierListContainer.Children())
            {
                var stageLabel = item.ElementAt(0) as Label;
                Assert.That(stageLabel, Is.Not.Null);
                stageOrder.Add(stageLabel.text);
            }

            Assert.That(stageOrder, Is.EqualTo(new List<string> { "1단계", "2단계", "3단계" }),
                "tier-list 의 자식 순서는 Definition.Tiers 의 입력 순서(임계치 오름차순)와 동일해야 한다.");
        }

        // ── DoD-S10: 활성 티어 변경 알림 갱신 ──

        [Test]
        public void Test_TC1001_ActiveTierChange_MovesHighlightToNewStage()
        {
            SynergyActivation act = MakeActivation(
                displayName: "X", description: "Y",
                tiers: new[]
                {
                    (tier: 1, required: 2, consts: Empty()),
                    (tier: 2, required: 4, consts: Empty()),
                });

            act.Recalculate(2); // 1단계 활성
            _section.Bind(act);

            // 변경: 2단계 임계치로 진입
            act.Recalculate(4);

            AssertActiveStage(expectedActiveStage: 2, allStages: new[] { 1, 2 });
        }

        [Test]
        public void Test_TC1002_ActiveTierChange_ToInactive_RemovesAllHighlights()
        {
            SynergyActivation act = MakeActivation(
                displayName: "X", description: "Y",
                tiers: new[]
                {
                    (tier: 1, required: 2, consts: Empty()),
                    (tier: 2, required: 4, consts: Empty()),
                });

            act.Recalculate(2); // 1단계 활성
            _section.Bind(act);

            // 변경: 카운트 0 → ActiveTier null
            act.Recalculate(0);

            foreach (VisualElement child in _tierListContainer.Children())
            {
                Assert.That(child.ClassListContains(TierItemActiveClass), Is.False,
                    "ActiveTier 가 null 로 변경되면 모든 항목에서 강조가 제거되어야 한다.");
            }
        }

        [Test]
        public void Test_TC1003_AfterUnbind_ActiveTierChange_DoesNotAffectHighlights()
        {
            SynergyActivation act = MakeActivation(
                displayName: "X", description: "Y",
                tiers: new[]
                {
                    (tier: 1, required: 2, consts: Empty()),
                    (tier: 2, required: 4, consts: Empty()),
                });

            act.Recalculate(2);
            _section.Bind(act);
            _section.Unbind();

            // Unbind 후 tier-list 가 비었으므로 강조 검증 대신 *구독 해제 결과* 를
            // "재 Bind 없이 ActiveTier 가 변해도 어떤 측의 부작용도 없음" 으로 확인.
            Assert.DoesNotThrow(() => act.Recalculate(4),
                "Unbind 후 ActiveTier 변경이 섹션 측 핸들러를 통해 예외를 일으켜선 안 된다.");
            Assert.That(_tierListContainer.childCount, Is.EqualTo(0),
                "Unbind 후 tier-list 컨테이너는 비어 있어야 한다.");
        }

        // ── 안전망 / 라이프사이클 정리 ──

        [Test]
        public void Test_TC1101_Bind_NullActivation_IsIgnored_NoThrow()
        {
            Assert.DoesNotThrow(() => _section.Bind(null));
            Assert.That(_nameLabel.text, Is.EqualTo(string.Empty));
            Assert.That(_descriptionLabel.text, Is.EqualTo(string.Empty));
            Assert.That(_tierListContainer.childCount, Is.EqualTo(0));
        }

        [Test]
        public void Test_TC1102_AfterRebind_PreviousActivationChange_DoesNotAffectHighlights()
        {
            SynergyActivation actA = MakeActivation(
                displayName: "A", description: "A",
                tiers: new[]
                {
                    (tier: 1, required: 2, consts: Empty()),
                    (tier: 2, required: 4, consts: Empty()),
                });
            SynergyActivation actB = MakeActivation(
                displayName: "B", description: "B",
                tiers: new[]
                {
                    (tier: 1, required: 3, consts: Empty()),
                    (tier: 2, required: 6, consts: Empty()),
                });

            actA.Recalculate(2); // A 1단계 활성
            _section.Bind(actA);
            _section.Rebind(actB);  // 이제 actB 의 tier-list 로 교체됨

            // actA 측의 카운트 변경
            actA.Recalculate(4);

            // actB 의 tier-list 강조 상태는 actA 의 영향을 받지 않아야 한다.
            // actB 는 Recalculate 호출 없이 Bind 되었으므로 ActiveTier == null → 강조 항목 없음.
            foreach (VisualElement child in _tierListContainer.Children())
            {
                Assert.That(child.ClassListContains(TierItemActiveClass), Is.False,
                    "Rebind 후 이전 시너지의 ActiveTier 변경은 현 시너지의 강조에 영향을 줘선 안 된다.");
            }
            Assert.That(_nameLabel.text, Is.EqualTo("B"),
                "Rebind 후 라벨은 새 시너지의 정보를 표시해야 한다.");
        }

        [Test]
        public void Test_TC1103_Unbind_ClearsLabelsAndTierList()
        {
            SynergyActivation act = MakeActivation(
                displayName: "X", description: "Y",
                tiers: new[] { (tier: 1, required: 2, consts: Empty()) });

            _section.Bind(act);
            Assume.That(_tierListContainer.childCount, Is.GreaterThan(0));

            _section.Unbind();

            Assert.That(_nameLabel.text, Is.EqualTo(string.Empty));
            Assert.That(_descriptionLabel.text, Is.EqualTo(string.Empty));
            Assert.That(_tierListContainer.childCount, Is.EqualTo(0));
        }

        // ── helpers ──

        private void AssertActiveStage(int expectedActiveStage, int[] allStages)
        {
            // tier-list 자식의 stage 라벨에서 단계 추출 후 active 클래스 보유 여부 확인.
            foreach (VisualElement item in _tierListContainer.Children())
            {
                var stageLabel = item.ElementAt(0) as Label;
                Assert.That(stageLabel, Is.Not.Null);
                int stage = ParseStage(stageLabel.text);
                bool hasActive = item.ClassListContains(TierItemActiveClass);
                if (stage == expectedActiveStage)
                {
                    Assert.That(hasActive, Is.True,
                        $"{stage} 단계 항목은 활성 강조 클래스를 가져야 한다.");
                }
                else
                {
                    Assert.That(hasActive, Is.False,
                        $"{stage} 단계 항목은 활성 강조 클래스를 가져선 안 된다.");
                }
            }
        }

        private static int ParseStage(string stageText)
        {
            // "{N}단계" 에서 N 추출
            int idx = stageText.IndexOf('단');
            return int.Parse(stageText.Substring(0, idx));
        }

        private SynergyActivation MakeActivation(
            string displayName,
            string description,
            (int tier, int required, SerializableDictionary<string, float> consts)[] tiers)
        {
            SynergyDefinitionData def = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            _createdAssets.Add(def);

            SetPrivateField(def, "displayName", displayName);
            SetPrivateField(def, "description", description);

            var tierList = new List<SynergyTier>();
            foreach (var t in tiers)
            {
                tierList.Add(MakeTier(t.tier, t.required, t.consts));
            }
            SetPrivateField(def, "tiers", tierList);

            return new SynergyActivation(def);
        }

        private static SynergyTier MakeTier(int tier, int requiredCount, SerializableDictionary<string, float> constants)
        {
            object boxed = new SynergyTier();
            SetPrivateFieldOnBox(boxed, "tier", tier);
            SetPrivateFieldOnBox(boxed, "requiredCount", requiredCount);
            SetPrivateFieldOnBox(boxed, "constants", constants);
            return (SynergyTier)boxed;
        }

        private static SerializableDictionary<string, float> Empty()
        {
            return new SerializableDictionary<string, float>();
        }

        private static SerializableDictionary<string, float> ConstsOf(params (string key, float value)[] entries)
        {
            var sd = new SerializableDictionary<string, float>();
            foreach (var (key, value) in entries)
            {
                sd[key] = value;
            }
            return sd;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                $"리플렉션 픽스처: {target.GetType().Name}.{fieldName} 필드를 찾지 못했다.");
            field.SetValue(target, value);
        }

        private static void SetPrivateFieldOnBox(object boxed, string fieldName, object value)
        {
            FieldInfo field = boxed.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                $"리플렉션 픽스처: {boxed.GetType().Name}.{fieldName} 필드를 찾지 못했다.");
            field.SetValue(boxed, value);
        }
    }
}
