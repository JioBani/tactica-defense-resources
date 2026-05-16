using Scenes.Battle.Feature.Synergy;
using Scenes.Battle.Feature.Unit.Defenders;
using UnityEngine;
using UnityEngine.UIElements;

namespace Scenes.Battle.Feature.Ui.SynergyInfo
{
    /// <summary>
    /// 시너지 상세 패널의 라이프사이클·진입점 통합 컨트롤러.
    /// UIDocument 호스트로서 표시 대상 슬롯을 단일로 유지하며,
    /// 외부 진입(Show)·내부 닫기 입력(닫기 버튼·바깥 클릭)·표시 대상 교체를 책임진다.
    /// 자식 섹션 뷰의 라이프사이클(Bind/Rebind/Unbind) 전달만 담당하고
    /// 표시 콘텐츠 책임은 각 섹션 뷰에 위임한다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SynergyDetailPanel : MonoBehaviour
    {
        private const string HiddenUssClass = "synergy-detail-overlay--hidden";
        private const string IconHasSpriteClass = "synergy-card__icon--has-sprite";
        private const string RootName = "synergy-detail-root";
        private const string CloseButtonName = "close-button";
        private const string EffectSectionName = "effect-section";
        private const string MemberSectionName = "member-section";
        private const string SynergyIconName = "synergy-icon";

        [SerializeField] private UIDocument document;
        [SerializeField] private DefenderManager defenderManager;

        private VisualElement _root;
        private VisualElement _synergyIcon;
        private Button _closeButton;
        private SynergyDetailEffectSection _effectSection;
        private SynergyDetailMemberList _memberList;

        private SynergyActivation _current;

        /// <summary>현재 표시 상태 여부. 표시 대상 슬롯이 채워져 있으면 true.</summary>
        public bool IsVisible => _current != null;

        /// <summary>현재 표시 대상 시너지. 비표시 상태이면 null.</summary>
        public SynergyActivation Current => _current;

        private bool _initialized;
        private bool _loggedDocumentMissing;
        private bool _loggedVisualTreeMissing;
        private bool _loggedRootMissing;

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }
            if (document == null)
            {
                if (!_loggedDocumentMissing)
                {
                    Debug.LogError($"[SynergyDetailPanel] UIDocument 컴포넌트가 부착되지 않았고 [SerializeField] document 도 비어 있습니다. 같은 GameObject 에 UIDocument 컴포넌트를 추가하거나 Inspector 에서 document 필드를 연결하세요. (GameObject: {gameObject.name})", this);
                    _loggedDocumentMissing = true;
                }
                return;
            }

            VisualElement docRoot = document.rootVisualElement;
            if (docRoot == null)
            {
                // UIDocument 의 visualTree 가 아직 빌드되지 않은 시점일 수 있음 — 다음 호출에서 재시도.
                // 다만 sourceAsset / panelSettings 가 미연결이면 영원히 null 이므로 진단 로그를 1회 남긴다.
                if (!_loggedVisualTreeMissing)
                {
                    Debug.LogWarning($"[SynergyDetailPanel] UIDocument.rootVisualElement 가 null 입니다. UIDocument 의 Source Asset 과 Panel Settings 가 연결되어 있는지 확인하세요. (GameObject: {gameObject.name})", this);
                    _loggedVisualTreeMissing = true;
                }
                return;
            }

            _root = docRoot.Q<VisualElement>(RootName);
            _closeButton = docRoot.Q<Button>(CloseButtonName);
            _synergyIcon = docRoot.Q<VisualElement>(SynergyIconName);
            VisualElement effectRoot = docRoot.Q<VisualElement>(EffectSectionName);
            VisualElement memberRoot = docRoot.Q<VisualElement>(MemberSectionName);

            if (_root == null)
            {
                Debug.LogError($"[SynergyDetailPanel] UXML 트리에서 루트 노드 '{RootName}' 를 찾을 수 없습니다. UIDocument.Source Asset 이 올바른 SynergyDetailPanel.uxml 인지 확인하세요. (GameObject: {gameObject.name})", this);
                return;
            }

            _effectSection = new SynergyDetailEffectSection(effectRoot);
            _memberList = new SynergyDetailMemberList(memberRoot, defenderManager);

            if (_closeButton != null)
            {
                _closeButton.clicked += HandleCloseButton;
            }

            ApplyHiddenStyle();
            _initialized = true;
        }

        private void OnDisable()
        {
            if (_closeButton != null)
            {
                _closeButton.clicked -= HandleCloseButton;
            }

            UnregisterOutsideClickCallback();
            _current = null;

            // UIDocument 는 OnDisable 시 visualTree 를 panel 에서 detach 하고, 다음 OnEnable 시
            // visualTreeAsset 을 새로 clone 하여 attach 한다. 캐시한 _root / _closeButton /
            // _synergyIcon 은 stale 상태가 되므로 _initialized 가드를 풀어 다음 OnEnable 의
            // EnsureInitialized 가 새 visualTree 에서 다시 Q 하도록 한다.
            _initialized = false;
            _root = null;
            _closeButton = null;
            _synergyIcon = null;
            _effectSection = null;
            _memberList = null;
        }

        /// <summary>
        /// 외부 진입점. 표시 대상 슬롯의 현재 상태로 분기한다.
        /// - 비표시 → 표시 진입.
        /// - 표시 + 동일 시너지 → 토글 닫기 (재클릭 트리거).
        /// - 표시 + 다른 시너지 → 비표시를 거치지 않고 교체.
        /// </summary>
        public void Show(SynergyActivation activation)
        {
            if (activation == null)
            {
                return;
            }

            if (_current == null)
            {
                EnterVisible(activation);
            }
            else if (_current == activation)
            {
                ExitVisible();
            }
            else
            {
                Replace(activation);
            }
        }

        /// <summary>외부 비표시 전이. 현재 표시 상태일 때만 작동.</summary>
        public void Hide()
        {
            if (_current != null)
            {
                ExitVisible();
            }
        }

        private void EnterVisible(SynergyActivation activation)
        {
            // SetActive(true) 는 동기적으로 OnEnable → EnsureInitialized 를 호출하여
            // UIDocument 의 새 visualTree 에서 _root / _closeButton / _synergyIcon 등을 재조회한다.
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            EnsureInitialized();
            if (_root == null)
            {
                // 초기화가 아직 가능하지 않은 시점 — 표시 진입 포기 + 진단 단서 (멱등 로그).
                // 진단 상세는 EnsureInitialized 안에서 컴포넌트/자산/UXML 단위로 1회 출력되므로
                // 여기서는 표시 시도가 실패했다는 사실만 한 번 더 남긴다 (호출 라인 추적용).
                if (!_loggedRootMissing)
                {
                    Debug.LogError($"[SynergyDetailPanel] Show 호출 시 UXML 루트가 초기화되지 않은 상태입니다 — 상세 패널이 표시되지 않습니다. 위의 진단 로그를 확인하세요. (GameObject: {gameObject.name})", this);
                    _loggedRootMissing = true;
                }
                return;
            }

            _current = activation;
            _effectSection.Bind(activation);
            _memberList.Bind(activation);
            ApplySynergyIcon(activation);
            ApplyVisibleStyle();
            RegisterOutsideClickCallback();
        }

        /// <summary>시너지 아이콘 표시 — Sprite 있으면 backgroundImage, 없으면 placeholder 글리프 유지.</summary>
        private void ApplySynergyIcon(SynergyActivation activation)
        {
            if (_synergyIcon == null)
            {
                return;
            }

            // 아이콘 표시 전 항상 has-sprite 마커를 정리한 뒤 재판정한다 — Rebind 시점에도 일관 동작.
            _synergyIcon.RemoveFromClassList(IconHasSpriteClass);
            _synergyIcon.style.backgroundImage = StyleKeyword.Null;

            Sprite icon = activation.Definition != null ? activation.Definition.Icon : null;
            if (icon != null)
            {
                _synergyIcon.style.backgroundImage = new StyleBackground(icon);
                _synergyIcon.AddToClassList(IconHasSpriteClass);
            }
        }

        private void ExitVisible()
        {
            UnregisterOutsideClickCallback();
            _effectSection.Unbind();
            _memberList.Unbind();
            _current = null;
            ApplyHiddenStyle();
            gameObject.SetActive(false);
        }

        private void Replace(SynergyActivation activation)
        {
            _current = activation;
            _effectSection.Rebind(activation);
            _memberList.Rebind(activation);
            ApplySynergyIcon(activation);
        }

        private void HandleCloseButton()
        {
            if (_current != null)
            {
                ExitVisible();
            }
        }

        private void RegisterOutsideClickCallback()
        {
            if (_root?.panel != null)
            {
                _root.panel.visualTree.RegisterCallback<PointerDownEvent>(
                    HandleOutsideClick, TrickleDown.TrickleDown);
            }
        }

        private void UnregisterOutsideClickCallback()
        {
            if (_root?.panel != null)
            {
                _root.panel.visualTree.UnregisterCallback<PointerDownEvent>(
                    HandleOutsideClick, TrickleDown.TrickleDown);
            }
        }

        private void HandleOutsideClick(PointerDownEvent evt)
        {
            if (_current == null)
            {
                return;
            }

            VisualElement target = evt.target as VisualElement;
            if (target == null)
            {
                return;
            }

            if (target == _root || IsDescendant(_root, target))
            {
                return;
            }

            ExitVisible();
        }

        private static bool IsDescendant(VisualElement ancestor, VisualElement candidate)
        {
            VisualElement walker = candidate.parent;
            while (walker != null)
            {
                if (walker == ancestor)
                {
                    return true;
                }
                walker = walker.parent;
            }
            return false;
        }

        private void ApplyHiddenStyle()
        {
            _root.AddToClassList(HiddenUssClass);
        }

        private void ApplyVisibleStyle()
        {
            _root.RemoveFromClassList(HiddenUssClass);
        }
    }
}
