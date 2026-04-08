# Plan: TACD-295 단위 4 — GameObject 계층 구성 및 연결

> 완료 정의:
> - CD-11. Canvas 하위에 SynergyInfoPanel > SynergyListPanel > SynergyIndicator 계층 구조가 구성된다
> - CD-12. SynergyIndicator는 에디터에서 충분한 수로 사전 배치되어 있으며, 동적 Instantiate를 하지 않는다
> - CD-13. SynergyDetailPanel GameObject가 비활성 상태로 존재한다

---

## 1. 현황 분석

### 기존 Canvas 구조
- Canvas (instanceID: 49730) 아래에 UI 요소들이 배치되어 있음
- Layer: 5 (UI)
- 기존 자식: DefenderPlacementLimitText, Market, ReadyButton, StatInfoPanel 등

### 이미 구현된 스크립트
- `SynergyIndicator.cs`: SerializeField — icon(Image), countText(TMP_Text), tierDots(Image[]), activeDotColor(Color), inactiveDotColor(Color), button(Button)
- `SynergyListPanel.cs`: SerializeField — indicators(SynergyIndicator[])
- `SynergyInfoPanel.cs`: SerializeField — listPanel(SynergyListPanel), detailPanel(SynergyDetailPanel)
- `SynergyDetailPanel.cs`: Show/Hide 빈 껍데기

---

## 2. 구현 계획

### 2.1 GameObject 계층 트리

```
Canvas
└── SynergyInfoPanel                     [SynergyInfoPanel 스크립트]
    │                                     anchor: top-left
    │
    ├── SynergyListPanel                  [SynergyListPanel 스크립트, VerticalLayoutGroup]
    │   │
    │   ├── SynergyIndicator_0            [SynergyIndicator 스크립트, Button, Image(배경)]
    │   │   ├── Icon                      [Image]
    │   │   ├── TierIndicator             [HorizontalLayoutGroup]
    │   │   │   ├── TierDot_0             [Image]
    │   │   │   ├── TierDot_1             [Image]
    │   │   │   ├── TierDot_2             [Image]
    │   │   │   └── TierDot_3             [Image]
    │   │   └── CountText                 [TMP_Text]
    │   │
    │   ├── SynergyIndicator_1 ~ _9       [동일 구조, 총 10개]
    │   └── ...
    │
    └── SynergyDetailPanel                [SynergyDetailPanel 스크립트, 비활성]
```

### 2.2 생성 순서

1. **SynergyInfoPanel** 생성 (Canvas 자식)
   - RectTransform: anchor top-left, pivot top-left
   - SynergyInfoPanel 스크립트 부착

2. **SynergyListPanel** 생성 (SynergyInfoPanel 자식)
   - VerticalLayoutGroup 부착 (spacing: 4, childForceExpandWidth: true, childForceExpandHeight: false)
   - ContentSizeFitter (verticalFit: PreferredSize)
   - SynergyListPanel 스크립트 부착

3. **SynergyIndicator x 10개** 생성 (SynergyListPanel 자식)
   - 각각 초기 비활성 상태 (SetActive: false)
   - Button 컴포넌트 부착
   - SynergyIndicator 스크립트 부착
   - 자식: Icon(Image), TierIndicator(HorizontalLayoutGroup > TierDot x4), CountText(TMP_Text)

4. **SynergyDetailPanel** 생성 (SynergyInfoPanel 자식)
   - 비활성 상태 (SetActive: false)
   - SynergyDetailPanel 스크립트 부착

### 2.3 SerializeField 연결

- SynergyInfoPanel: listPanel → SynergyListPanel, detailPanel → SynergyDetailPanel
- SynergyListPanel: indicators → SynergyIndicator_0 ~ _9 배열
- SynergyIndicator (각각): icon → Icon, countText → CountText, tierDots → TierDot_0~3, button → self Button, activeDotColor → 노란색, inactiveDotColor → 회색

### 2.4 레이아웃 수치 (프로토타입)

- SynergyInfoPanel: anchoredPosition (10, -10), width 120
- SynergyListPanel: stretch to parent
- SynergyIndicator: height 50, width 120
- Icon: 30x30
- TierDot: 8x8
- CountText: fontSize 14, Maplestory Bold SDF 폰트

---

## 3. 변경 파일

- Battle.unity (씬 파일 — GameObject 추가)
- 스크립트 파일 변경 없음
