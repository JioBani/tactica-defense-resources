using System;
using System.Reflection;
using Common.Data.Synergies;
using NUnit.Framework;
using Scenes.Battle.Feature.Synergy;
using Scenes.Battle.Feature.Ui.SynergyInfo;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tests.Editor
{
    /// <summary>
    /// TACD-296 lifecycle 구현 단위의 요구사항 테스트.
    /// SR 그룹 A (DoD-S1 ~ DoD-S6) — 상세 패널 라이프사이클 + 진입점 통합.
    ///
    /// 기대값 도출 근거는 시스템요구사항.md / 사용자요구사항.md 의 그룹 A DoD 항목이며,
    /// 구현 코드의 분기 로직을 따라가지 않는다.
    ///
    /// 픽스처 전략: SynergyDetailPanel 의 OnEnable 은 UIDocument 의 visualTree (PlayMode 영역) 에
    /// 의존하므로, 비활성 GameObject 에 AddComponent 하여 OnEnable 을 회피하고
    /// private 필드 (_root / _closeButton / _effectSection / _memberList) 를 수동 구축한
    /// VisualElement 트리로 리플렉션 주입한다. 닫기 버튼 핸들러(`HandleCloseButton`)도
    /// 수동으로 wire-up 하여 클릭 트리거 경로를 EditMode 에서 재현한다.
    /// </summary>
    public class SynergyDetailPanelLifecycleTests
    {
        private const string HiddenUssClass = "synergy-detail-panel--hidden";

        private GameObject _go;
        private SynergyDetailPanel _panel;
        private VisualElement _root;
        private Button _closeButton;

        private SynergyDefinitionData _defA;
        private SynergyDefinitionData _defB;
        private SynergyActivation _activationA;
        private SynergyActivation _activationB;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("SynergyDetailPanel(Test)");
            _go.SetActive(false);
            _panel = _go.AddComponent<SynergyDetailPanel>();

            _root = new VisualElement { name = "synergy-detail-root" };
            _root.AddToClassList("synergy-detail-panel");
            _root.AddToClassList(HiddenUssClass);

            _closeButton = new Button { name = "close-button" };
            _root.Add(_closeButton);

            var effectRoot = new VisualElement { name = "effect-section" };
            _root.Add(effectRoot);

            var memberRoot = new VisualElement { name = "member-section" };
            _root.Add(memberRoot);

            SetPrivateField(_panel, "_root", _root);
            SetPrivateField(_panel, "_closeButton", _closeButton);
            SetPrivateField(_panel, "_effectSection", new SynergyDetailEffectSection(effectRoot));
            SetPrivateField(_panel, "_memberList", new SynergyDetailMemberList(memberRoot));

            MethodInfo handleClose = typeof(SynergyDetailPanel).GetMethod(
                "HandleCloseButton", BindingFlags.Instance | BindingFlags.NonPublic);
            _closeButton.clicked += () => handleClose.Invoke(_panel, null);

            _defA = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            _defB = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            _activationA = new SynergyActivation(_defA);
            _activationB = new SynergyActivation(_defB);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }
            if (_defA != null) UnityEngine.Object.DestroyImmediate(_defA);
            if (_defB != null) UnityEngine.Object.DestroyImmediate(_defB);
        }

        // ── DoD-S1: 시너지 목록 패널의 클릭 입력 발행 표면 존재 ──

        [Test]
        public void Test_TC101_SynergyListPanel_OnIndicatorClicked_PublishesActivationKey()
        {
            EventInfo evt = typeof(SynergyListPanel).GetEvent(
                "OnIndicatorClicked", BindingFlags.Instance | BindingFlags.Public);

            Assert.That(evt, Is.Not.Null,
                "SynergyListPanel 은 인디케이터 클릭 입력 발행 표면(OnIndicatorClicked)을 공개 표면으로 가져야 한다.");
            Assert.That(evt.EventHandlerType, Is.EqualTo(typeof(Action<SynergyActivation>)),
                "클릭 입력 발행은 SynergyActivation 도메인 키를 전달하는 형태여야 한다.");
        }

        // ── DoD-S2: 비표시 → Show → 표시 진입 ──

        [Test]
        public void Test_TC201_Show_FromHidden_EntersVisibleWithCurrent()
        {
            Assume.That(_panel.IsVisible, Is.False, "사전 조건: 패널은 비표시 상태로 시작한다.");
            Assume.That(_panel.Current, Is.Null);

            _panel.Show(_activationA);

            Assert.That(_panel.IsVisible, Is.True, "Show 호출 후 IsVisible 는 true 여야 한다.");
            Assert.That(_panel.Current, Is.SameAs(_activationA), "Current 는 Show 의 인자와 동일 인스턴스여야 한다.");
            Assert.That(_root.ClassListContains(HiddenUssClass), Is.False,
                "표시 진입 후 루트의 hidden USS 클래스는 제거되어야 한다.");
        }

        // ── DoD-S2 라우팅: SynergyInfoPanel.HandleIndicatorClicked → detailPanel.Show ──

        [Test]
        public void Test_TC202_SynergyInfoPanel_RoutesIndicatorClick_ToDetailPanelShow()
        {
            var infoGo = new GameObject("SynergyInfoPanel(Test)");
            infoGo.SetActive(false);
            SynergyInfoPanel infoPanel = infoGo.AddComponent<SynergyInfoPanel>();

            SetPrivateField(infoPanel, "detailPanel", _panel);

            MethodInfo routerMethod = typeof(SynergyInfoPanel).GetMethod(
                "HandleIndicatorClicked", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(routerMethod, Is.Not.Null,
                "SynergyInfoPanel 은 인디케이터 클릭 입력을 상세 패널로 라우팅하는 통로(HandleIndicatorClicked)를 가져야 한다.");

            routerMethod.Invoke(infoPanel, new object[] { _activationA });

            Assert.That(_panel.IsVisible, Is.True,
                "목록 패널의 클릭 입력이 라우팅되면 상세 패널이 표시 상태로 전이되어야 한다.");
            Assert.That(_panel.Current, Is.SameAs(_activationA),
                "라우팅된 활성화 인스턴스가 상세 패널의 표시 대상 슬롯이 되어야 한다.");

            UnityEngine.Object.DestroyImmediate(infoGo);
        }

        // ── DoD-S2 안전망: Show(null) 무시 ──

        [Test]
        public void Test_TC203_Show_NullActivation_IsIgnored()
        {
            _panel.Show(null);

            Assert.That(_panel.IsVisible, Is.False,
                "Show(null) 은 표시 상태로 전이시키지 않아야 한다.");
            Assert.That(_panel.Current, Is.Null);
        }

        // ── DoD-S3: 시너지 종류 무관 — 두 임의 인스턴스 모두 동일 진입 ──

        [Test]
        public void Test_TC301_Show_TwoDifferentActivations_BothEnterVisible_NoTypeBranching()
        {
            // 정의 A 로 진입 — IsVisible/Current 결과
            _panel.Show(_activationA);
            bool enteredWithA = _panel.IsVisible && ReferenceEquals(_panel.Current, _activationA);

            // 토글 닫기 후 정의 B 로 진입
            _panel.Show(_activationA);
            Assume.That(_panel.IsVisible, Is.False, "사전 정리: 토글 닫기 후 비표시 상태.");
            _panel.Show(_activationB);
            bool enteredWithB = _panel.IsVisible && ReferenceEquals(_panel.Current, _activationB);

            Assert.That(enteredWithA, Is.True, "정의 A 의 activation 으로도 동일하게 표시 진입해야 한다.");
            Assert.That(enteredWithB, Is.True, "정의 B 의 activation 으로도 동일하게 표시 진입해야 한다 (시너지 종류 분기 없음).");
        }

        // ── DoD-S4: 재클릭 토글 닫기 ──

        [Test]
        public void Test_TC401_Show_SameActivationTwice_TogglesHide()
        {
            _panel.Show(_activationA);
            Assume.That(_panel.IsVisible, Is.True);

            _panel.Show(_activationA);

            Assert.That(_panel.IsVisible, Is.False,
                "표시 상태에서 동일 시너지로 재진입하면 비표시로 토글되어야 한다.");
            Assert.That(_panel.Current, Is.Null);
            Assert.That(_root.ClassListContains(HiddenUssClass), Is.True,
                "비표시 전이 시 루트의 hidden USS 클래스가 적용되어야 한다.");
        }

        // ── DoD-S4: 닫기 버튼 click 으로 비표시 ──

        [Test]
        public void Test_TC402_CloseButtonClick_ExitsVisible()
        {
            _panel.Show(_activationA);
            Assume.That(_panel.IsVisible, Is.True);

            InvokeButtonClicked(_closeButton);

            Assert.That(_panel.IsVisible, Is.False, "닫기 버튼 click 으로 비표시 상태로 전이해야 한다.");
            Assert.That(_panel.Current, Is.Null);
        }

        // ── DoD-S4: Hide() 외부 호출로 비표시 ──

        [Test]
        public void Test_TC403_Hide_FromVisible_ExitsVisible()
        {
            _panel.Show(_activationA);
            Assume.That(_panel.IsVisible, Is.True);

            _panel.Hide();

            Assert.That(_panel.IsVisible, Is.False);
            Assert.That(_panel.Current, Is.Null);
        }

        // ── DoD-S4: 비표시 상태 Hide 멱등 ──

        [Test]
        public void Test_TC404_Hide_FromHidden_RemainsHidden_NoThrow()
        {
            Assume.That(_panel.IsVisible, Is.False);

            Assert.DoesNotThrow(() => _panel.Hide(),
                "비표시 상태에서의 Hide 호출은 예외를 던지지 않아야 한다.");
            Assert.That(_panel.IsVisible, Is.False);
            Assert.That(_panel.Current, Is.Null);
        }

        // ── DoD-S5: 표시 상태에서 다른 시너지 클릭 → 교체 ──

        [Test]
        public void Test_TC501_Show_DifferentActivationWhileVisible_ReplacesCurrent()
        {
            _panel.Show(_activationA);
            Assume.That(_panel.IsVisible, Is.True);
            Assume.That(_panel.Current, Is.SameAs(_activationA));

            _panel.Show(_activationB);

            Assert.That(_panel.IsVisible, Is.True, "교체 시 표시 상태가 유지되어야 한다.");
            Assert.That(_panel.Current, Is.SameAs(_activationB),
                "Current 는 새 시너지로 갱신되어야 한다.");
            Assert.That(_root.ClassListContains(HiddenUssClass), Is.False,
                "교체 도중 hidden USS 클래스가 다시 붙어선 안 된다.");
        }

        // ── DoD-S5: 교체 도중 비표시(IsVisible=false) 를 거치지 않음 ──

        [Test]
        public void Test_TC502_Replace_DoesNotPassThroughHidden()
        {
            _panel.Show(_activationA);
            bool visibleAfterA = _panel.IsVisible;

            _panel.Show(_activationB);
            bool visibleAfterB = _panel.IsVisible;

            Assert.That(visibleAfterA, Is.True);
            Assert.That(visibleAfterB, Is.True,
                "Show(actA) → Show(actB) 사이에 IsVisible 가 false 로 떨어지지 않아야 한다 (DoD-S5).");
        }

        // ── DoD-S6: 단일 슬롯 불변식 ──

        [Test]
        public void Test_TC601_Current_IsAlwaysSingleSlot_AcrossMultipleShows()
        {
            _panel.Show(_activationA);
            Assert.That(_panel.Current, Is.SameAs(_activationA));

            _panel.Show(_activationB);
            Assert.That(_panel.Current, Is.SameAs(_activationB),
                "다회 Show 후에도 Current 는 가장 최근의 단일 슬롯만 보유해야 한다.");

            // 동일 시너지 재진입 후
            _panel.Show(_activationB);
            Assert.That(_panel.Current, Is.Null,
                "단일 슬롯이 토글 닫기로 비워지면 Current 는 null — 동시에 둘 이상이 채워지지 않는다.");
        }

        // ── helpers ──

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                $"리플렉션 픽스처: {target.GetType().Name}.{fieldName} 필드를 찾지 못했다. " +
                "필드 명명이 변경되었을 수 있다 — 본 단위 픽스처 컨벤션과 함께 검토 필요.");
            field.SetValue(target, value);
        }

        private static void InvokeButtonClicked(Button button)
        {
            // UI Toolkit Button.clicked 는 내부적으로 Clickable.clicked Action 에 위임된다.
            // Clickable 의 clicked 는 표준 이벤트라 컴파일러 생성 백킹 필드(`clicked`)가 존재.
            Clickable clickable = button.clickable;
            Assert.That(clickable, Is.Not.Null,
                "Button.clicked 구독으로 Clickable manipulator 가 자동 생성되어 있어야 한다.");

            FieldInfo backing = typeof(Clickable).GetField(
                "clicked", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(backing, Is.Not.Null, "Clickable.clicked 백킹 필드를 찾지 못했다.");

            var action = backing.GetValue(clickable) as Action;
            Assert.That(action, Is.Not.Null,
                "Clickable.clicked 에 핸들러가 등록되어 있어야 한다 (픽스처 wire-up 확인).");
            action.Invoke();
        }
    }
}
