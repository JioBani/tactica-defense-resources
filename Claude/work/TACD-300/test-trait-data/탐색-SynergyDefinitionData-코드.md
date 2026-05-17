# TACD-300 — 코드 탐색 결과 (코드 모드)

> 이 문서는 req 멤버의 위임 요청(SR 작성 단계)에 대한 코드 탐색 응답이며, 도메인 언어로 먼저 답한 뒤 필요한 경우 코드 단계까지 풀어낸 결과입니다.

## 요청 컨텍스트

- **요청자**: req 멤버 (SR 작성 단계)
- **요청 단계**: SR 작성
- **요청 내용**: SynergyDefinitionData 필드 구조 / 소환수 특성 로드 방식 / 테스트 데이터 저장 위치 관례
- **탐색 범위**:
  - `Assets/Common/Data/Synergies/`
  - `Assets/Common/Scripts/` (SynergyManager 등 런타임 로드 코드)
  - `Assets/Common/Data/Units/`
  - `Assets/Tests/`

---

## 응답

### 질문 1: SynergyDefinitionData 필드 구조

**도메인 표현**: SynergyDefinitionData는 특성 하나의 정의를 담는 ScriptableObject다. 표시 정보(이름·설명·아이콘), 부여할 상태 효과 참조, 시너지 종류(소환술사 효과 vs 소환수 특성), 티어별 임계치·상수 목록으로 구성된다. 역할군(Role) 필드는 이 클래스에 없다.

**코드 구조**:

- **진입점**: `Assets/Common/Data/Synergies/SynergyDefinitionData.cs`

```
SynergyDefinitionData : ScriptableObject
│
├─ [SerializeField] id : SynergyId                        // 시너지 고유 ID (enum)
├─ [SerializeField] displayName : string                  // 표시 이름
├─ [SerializeField] description : string                  // 설명 (플레이스홀더 @ConstantName@ 포함)
├─ [SerializeField] icon : Sprite                         // UI 아이콘
│
├─ [SerializeField] statusEffectDefinition                // 부여할 상태 효과 정의 참조
│       : StatusEffectDefinitionData
│
├─ [SerializeField] synergyType : SynergyType             // SummonerEffect=0 / SummonTrait=1
│
└─ [SerializeField] tiers : List<SynergyTier>             // 티어 목록
```

**중첩 타입 — SynergyTier** (`Assets/Common/Data/Synergies/SynergyTier.cs`, `[Serializable]`):

```
SynergyTier
├─ tier : int                                             // 티어 단계 번호 (예: 1, 2, 3)
├─ requiredCount : int                                    // 활성화 최소 유닛 수
└─ constants : SerializableDictionary<string, float>      // 티어별 상수 딕셔너리
                                                          // 키: "healthPercent", "spellPower" 등
                                                          // description의 @ConstantName@ 치환에 사용
```

**핵심 식별자**:
- Enum `SynergyId` (`Assets/Common/Data/Synergies/SynergyId.cs`): `None=0, Bruiser=1, Arcanist=2, Freljord=3, Warmonger=4, Gunslinger=5`
- Enum `SynergyType` (`Assets/Common/Data/Synergies/SynergyType.cs`): `SummonerEffect=0, SummonTrait=1`

**역할군(Role) 필드 부재**: `SynergyDefinitionData`에는 역할군 관련 필드가 없다. 시너지 종류 구분은 `SynergyType` enum으로 처리된다.

---

### 질문 2: 소환수 특성 로드 방식

**도메인 표현**: 직접 참조(SerializeField) 방식이다. Resources.LoadAll이나 Addressables 없이, 소환술사·유닛 정의 데이터에 SynergyDefinitionData를 Inspector에서 직접 연결한다. 런타임에 SynergyManager가 SummonerManager에서 소환술사 목록을 받아 참조를 순회한다.

**코드 구조**:

```
로드 흐름
SynergyManager.Start()
    │
    ├─ SummonerManager.Instance.Summoners  → IReadOnlyList<SummonerLoadOutData>
    │
    └─ foreach summoner
           └─ summoner.Summoner.SummonerEffect  : SynergyDefinitionData
                  (null 이면 시스템이 이 소환술사를 시너지 없음으로 처리)
```

**핵심 식별자**:
- 클래스: `SynergyManager` (위치: `Assets/Common/Scripts/.../SynergyManager.cs`)
- 클래스: `SummonerLoadOutData` (상속: `UnitLoadOutData`)
  - 필드: `summoner : SummonerDefinitionData`
- 클래스: `SummonerDefinitionData`
  - 필드: `summonerEffect : SynergyDefinitionData` — 소환술사 효과 직접 참조
- 클래스: `UnitDefinitionData`
  - 필드: `summonerEffect : SynergyDefinitionData`
  - 속성: `Synergies` — 유닛이 보유한 모든 시너지 목록 반환

**새 SynergyDefinitionData 자산 등록 방법**:

1. **폴더**: `Assets/Common/Data/Synergies/` 에 `.asset` 파일 배치
2. **생성**: Unity 메뉴 → Assets → Create → Synergy → SynergyDefinitionData
3. **등록**: 생성 후 아래 중 해당하는 곳에 Inspector에서 할당
   - 소환술사 효과: `SummonerDefinitionData.summonerEffect`
   - 유닛 특성: `UnitDefinitionData.summonerEffect`
   - 직접 참조 방식이므로 추가 코드 등록 불필요

**현재 존재하는 자산 예시** (`Assets/Common/Data/Synergies/`):
- `난동꾼.asset` (Bruiser), `녹서스.asset` (Warmonger), `데마시아.asset`, `빌지워터.asset`, `슈리마.asset`

---

### 질문 3: 테스트 데이터 저장 위치 관례

**도메인 표현**: 프로젝트의 테스트는 ScriptableObject 자산 파일을 디스크에 만들지 않고 `ScriptableObject.CreateInstance<T>()`로 인메모리 생성하는 방식을 사용한다. 별도 테스트 전용 자산 폴더나 "Test_" 접두어 관례는 없다.

**코드 구조**:

```
Assets/Tests/Editor/SynergyActivationTests.cs (예시)

[SetUp]
void SetUp()
{
    _definition = ScriptableObject.CreateInstance<SynergyDefinitionData>();
    // 리플렉션으로 private SerializeField 값 주입
    SetTiers(_definition, new List<SynergyTier> { ... });
}

[TearDown]
void TearDown()
{
    Object.DestroyImmediate(_definition);
}
```

**관련 테스트 파일** (`Assets/Tests/Editor/`):
- `SynergyActivationTests.cs`, `ArcanistSynergyControllerTests.cs`, `BruiserSynergyStatusEffectTests.cs`, `FreljordSynergyStatusEffectTests.cs`, `GunslingerSynergyStatusEffectTests.cs`, `WarmongerSynergyStatusEffectTests.cs`, `SynergyControllerTests.cs` 등

**예외**: `Assets/Common/Data/Battlefields/Test Data.asset` — 전투 필드 테스트용으로 디스크에 저장된 유일한 테스트 자산이 발견됨. 이 경우 별도 접두어 없이 동일 폴더 내 "Test Data"로 명명.

**TACD-300 테스트 특성 자산 위치 권고 근거**: 기존 관례상 테스트 SO 자산을 디스크에 두는 경우 `Common/Data/{타입}/` 내에 배치하며, 접두어는 별도로 정해진 바 없음.

---

## 발견한 특이사항

- **SynergyId enum 값 범위**: 현재 `None=0, Bruiser=1, Arcanist=2, Freljord=3, Warmonger=4, Gunslinger=5` — 새 특성 추가 시 이 enum에 값을 추가해야 함
- **description 플레이스홀더**: `@ConstantName@`, `@ConstantName*N@` 형식의 치환자가 있어 `SynergyTier.constants` 딕셔너리 키와 연동됨 — 테스트 자산 제작 시 일치 필요
- **SummonTrait vs SummonerEffect**: `SynergyType.SummonTrait=1` 이 이번 TACD-300의 대상 타입. 기존 자산들은 SummonerEffect 중심으로 구성되어 있음

## 탐색 한계

- **SummonTrait 타입의 실제 런타임 처리 코드**: TACD-250(소환수 특성 부여 기능)이 아직 미구현이거나 탐색 범위에서 확인되지 않음. 특성이 실제로 어떻게 적용되는지 런타임 로직은 확인 불가.
- **SynergyId enum 확장 필요 여부**: 8개 테스트 특성이 기존 enum 값을 재사용하는지, 새 값이 필요한지 TACD-250 스펙에 따라 달라짐.

> 본 응답을 도메인 언어로만 정리해야 한다면 §응답 의 "도메인 표현" 부분만 추려 도메인 모드 응답으로 재정리할 수 있습니다.
