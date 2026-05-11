using Scenes.Battle.Feature.Synergy;
using UnityEngine.UIElements;

namespace Scenes.Battle.Feature.Ui.SynergyInfo
{
    /// <summary>
    /// 시너지 상세 패널의 효과 정보 섹션 뷰.
    /// 표시 대상 시너지의 이름·효과 설명·티어 임계치·티어별 효과 수치·활성 티어 강조를 책임진다.
    /// 본체는 effect-info 구현 단위에서 채워지며, 현재는 lifecycle 단위의 컴파일 의존을 위한 시그니처 stub.
    /// </summary>
    public class SynergyDetailEffectSection
    {
        private readonly VisualElement _sectionRoot;

        public SynergyDetailEffectSection(VisualElement sectionRoot)
        {
            _sectionRoot = sectionRoot;
        }

        /// <summary>표시 대상 설정 + 정적 정보 렌더 + 활성 티어 강조 + 알림 구독 개시.</summary>
        public void Bind(SynergyActivation activation)
        {
        }

        /// <summary>표시 상태 유지 교체 — 이전 구독 해제 + 새 시너지 렌더·구독.</summary>
        public void Rebind(SynergyActivation activation)
        {
        }

        /// <summary>알림 구독 해제.</summary>
        public void Unbind()
        {
        }
    }
}
