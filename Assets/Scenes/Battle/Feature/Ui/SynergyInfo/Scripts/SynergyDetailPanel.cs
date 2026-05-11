using Scenes.Battle.Feature.Synergy;
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
        private const string HiddenUssClass = "synergy-detail-panel--hidden";
        private const string RootName = "synergy-detail-root";
        private const string CloseButtonName = "close-button";
        private const string EffectSectionName = "effect-section";
        private const string MemberSectionName = "member-section";

        [SerializeField] private UIDocument document;

        private VisualElement _root;
        private Button _closeButton;
        private SynergyDetailEffectSection _effectSection;
        private SynergyDetailMemberList _memberList;

        private SynergyActivation _current;

        /// <summary>현재 표시 상태 여부. 표시 대상 슬롯이 채워져 있으면 true.</summary>
        public bool IsVisible => _current != null;

        /// <summary>현재 표시 대상 시너지. 비표시 상태이면 null.</summary>
        public SynergyActivation Current => _current;

        private bool _initialized;

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
                return;
            }

            VisualElement docRoot = document.rootVisualElement;
            if (docRoot == null)
            {
                // UIDocument 의 visualTree 가 아직 빌드되지 않은 시점.
                // [DefaultExecutionOrder(100)] 로 UIDocument 보다 늦게 OnEnable 되도록 보장하지만,
                // 안전망으로 null 가드 후 다음 OnEnable / Show 호출 시 재시도한다.
                return;
            }

            _root = docRoot.Q<VisualElement>(RootName);
            _closeButton = docRoot.Q<Button>(CloseButtonName);
            VisualElement effectRoot = docRoot.Q<VisualElement>(EffectSectionName);
            VisualElement memberRoot = docRoot.Q<VisualElement>(MemberSectionName);

            _effectSection = new SynergyDetailEffectSection(effectRoot);
            _memberList = new SynergyDetailMemberList(memberRoot);

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
            // prefab 셋업이 Detail Panel GameObject 를 비활성으로 두는 경우, SetActive(true) 가
            // 동기적으로 OnEnable 을 호출하여 _effectSection / _memberList / _root 를 초기화한다.
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            EnsureInitialized();
            if (_root == null)
            {
                // 초기화가 아직 가능하지 않은 시점 — 표시 진입을 포기 (다음 Show 호출에서 재시도).
                return;
            }

            _current = activation;
            _effectSection.Bind(activation);
            _memberList.Bind(activation);
            ApplyVisibleStyle();
            RegisterOutsideClickCallback();
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
