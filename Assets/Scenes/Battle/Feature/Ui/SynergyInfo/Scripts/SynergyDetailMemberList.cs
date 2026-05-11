using Scenes.Battle.Feature.Synergy;
using UnityEngine.UIElements;

namespace Scenes.Battle.Feature.Ui.SynergyInfo
{
    /// <summary>
    /// 시너지 상세 패널의 보유 소환수 목록 섹션 뷰.
    /// 시너지 매니저의 시너지→보유 소환수 역참조 통로에서 목록을 조회하여 항목을 생성하고,
    /// 각 소환수의 전장 배치 여부를 시각 상태로 적용하며, 배치/제거 변경 알림을 구독한다.
    /// 본체는 member-list 구현 단위에서 채워지며, 현재는 lifecycle 단위의 컴파일 의존을 위한 시그니처 stub.
    /// </summary>
    public class SynergyDetailMemberList
    {
        private readonly VisualElement _sectionRoot;

        public SynergyDetailMemberList(VisualElement sectionRoot)
        {
            _sectionRoot = sectionRoot;
        }

        /// <summary>표시 대상 설정 + 항목 생성 + 배치 상태 시각 적용 + 알림 구독 개시.</summary>
        public void Bind(SynergyActivation activation)
        {
        }

        /// <summary>표시 상태 유지 교체 — 이전 구독 해제 + 항목 재생성·구독.</summary>
        public void Rebind(SynergyActivation activation)
        {
        }

        /// <summary>알림 구독 해제.</summary>
        public void Unbind()
        {
        }
    }
}
