# Common 인프라 클래스 맵

> `Assets/Common/` 에 위치한 공용 프레임워크·유틸리티·데이터 정의를 정리한 문서.
> 전장(인게임) 피처에서 공유하는 기반 코드이며, "이 기능을 수정하려면 어떤 클래스를 봐야 하는가"를 빠르게 파악하기 위한 용도.

---

## 1. StateBase 프레임워크

상태 머신 패턴의 공용 기반. 행동 상태(ActionState), 라운드(RoundManager) 등에서 상속하여 사용한다.

| 클래스 | 역할 | 경로 |
|--------|------|------|
| StateBaseController\<T\> | 제네릭 상태 머신 기반 클래스. Enter → Run → Exit 라이프사이클, GlobalTransition, 리스너 알림 | Common/Scripts/StateBase/StateBaseController.cs |
| IStateListener\<T\> | 상태 라이프사이클 리스너 인터페이스 (OnStateEnter, OnStateRun, OnStateExit) | Common/Scripts/StateBase/IStateListener.cs |

---

## 2. 오브젝트 풀링

반복 스폰되는 오브젝트(발사체, 침략자, VFX 등)를 Instantiate/Destroy 대신 풀에서 재사용한다.

| 클래스 | 역할 | 경로 |
|--------|------|------|
| ObjectPooler | 프리팹 기반 오브젝트 풀 관리. Spawn/DeSpawn 제공. 싱글톤 | Common/Scripts/ObjectPool/ObjectPooler.cs |
| Poolable | 풀링 대상 오브젝트에 부착하는 마커 컴포넌트. DeSpawn() 편의 메서드 제공 | Common/Scripts/ObjectPool/Poolable.cs |

---

## 3. 리액티브 & 비동기 유틸리티

### 3.1 RxValue (반응형 값)

| 클래스 | 역할 | 경로 |
|--------|------|------|
| RxValue\<T\> | 값 변경 시 OnChange 이벤트를 발행하는 반응형 컨테이너. 폴링 대신 구독 방식 사용 | Common/Scripts/Rx/RxValue.cs |

### 3.2 DynamicRepeater (동적 반복기)

| 클래스 | 역할 | 경로 |
|--------|------|------|
| DynamicRepeater | 동적 간격으로 비동기 작업을 반복 실행. intervalNow() + job() 조합. 공격 루프 등에 사용 | Common/Scripts/DynamicRepeater/DynamicRepeater.cs |

### 3.3 Timer (타이머)

| 클래스 | 역할 | 경로 |
|--------|------|------|
| Timer | 카운트다운 타이머. Start/Stop/Pause/Resume/Restart, OnTimeOutChange 콜백 제공 | Common/Scripts/Timer/Timer.cs |
| TimerManager | Timer 라이프사이클 관리. Make/Delete, Update에서 자동 틱. 싱글톤 | Common/Scripts/Timer/TimerManager.cs |

### 3.4 UniTaskHandle (비동기 핸들)

| 클래스 | 역할 | 경로 |
|--------|------|------|
| UniTaskHandle | UniTask/Task 상태(IsCompleted, IsFaulted, IsCanceled)를 즉시 await 없이 조회하는 래퍼. 제네릭 버전은 TryGetResult 제공 | Common/Scripts/UniTaskHandle/UniTaskHandle.cs |

---

## 4. 이벤트 시스템 (GlobalEventBus)

피처 간 직접 참조 없이 struct 기반 이벤트로 통신하는 발행/구독 시스템.

| 클래스 | 역할 | 경로 |
|--------|------|------|
| GlobalEventBus | 타입 안전 Publish/Subscribe 이벤트 버스. struct 이벤트 제약 | Common/Scripts/GlobalEventBus/GlobalEventBus.cs |
| OnObjectSelectedEvent | 오브젝트 선택 이벤트 DTO (SelectedObject, ScreenPosition) | Common/Scripts/GlobalEventBus/OnObjectSelectedEvent.cs |

---

## 5. 드래그 앤 드롭 시스템

소환수 배치, 합성, 판매 등에 사용되는 2D 드래그/드롭 프레임워크.

| 클래스 | 역할 | 경로 |
|--------|------|------|
| Draggable2D | 드래그 컨트롤러. 시작/종료 처리, 카메라 좌표 변환, 드롭 존 검증, OnDrop 콜백 | Common/Scripts/Draggable/Draggable2D.cs |
| DropZone2D | 드롭 영역 기본 클래스. 태그 기반 검증 및 IDropRule 목록으로 수락/처리 | Common/Scripts/Draggable/DropZone2D.cs |
| ExclusiveDropZone2D | 단일 점유 드롭 영역. 새 오브젝트 드롭 시 기존 점유자와 자동 교환 | Common/Scripts/Draggable/ExclusiveDropZone2D.cs |
| IDropRule | 드롭 검증 규칙 인터페이스 (CanAccept, OnDropped, OnDragOut) | Common/Scripts/Draggable/IDropRule.cs |
| DragState | 드래그 상태 enum (DragStart, Dragging, DragEnd) | Common/Scripts/Draggable/DragState.cs |

---

## 6. 씬 관리 & 싱글톤

### 6.1 SceneSingleton

| 클래스 | 역할 | 경로 |
|--------|------|------|
| SceneSingleton\<T\> | 제네릭 싱글톤 베이스. 인스턴스 캐싱, 중복 방지, DontDestroyOnLoad 옵션, OnAwakeSingleton 훅 | Common/Scripts/SceneSingleton/SceneSingleton.cs |

### 6.2 씬 간 데이터 전달

| 클래스 | 역할 | 경로 |
|--------|------|------|
| SceneData\<T\> | 씬 전환 시 데이터 전달 컨테이너. Set/Get/TryGet/Clear, HasData 플래그 | Common/Scripts/SceneDataManager/SceneData.cs |
| SceneDataManager | 씬 간 공유 데이터 관리 (예: selectedBattlefield). 싱글톤 | Common/Scripts/SceneDataManager/SceneDataManager.cs |

---

## 7. 태스크 큐

비동기 작업을 채널별로 순차 실행하는 큐 시스템. UI 연출 등 순서 보장이 필요한 작업에 사용.

| 클래스 | 역할 | 경로 |
|--------|------|------|
| GlobalTaskQueue | 채널 기반 태스크 큐 정적 관리. Enqueue/Clear by TaskQueueChannel | Common/Scripts/TaskQueue/GlobalTaskQueue.cs |
| TaskQueue | 인스턴스 큐. IQueuedTask를 순차 실행, 큐잉 시 자동 시작 | Common/Scripts/TaskQueue/TaskQueue.cs |
| QueuedTask | Func\<UniTask\> 람다를 IQueuedTask로 래핑 | Common/Scripts/TaskQueue/QueuedTask.cs |
| IQueuedTask | 큐 작업 인터페이스 (ExecuteAsync) | Common/Scripts/TaskQueue/IQueuedTask.cs |
| TaskQueueChannel | 큐 채널 enum (BattleUi) | Common/Scripts/TaskQueue/TaskQueueChannel.cs |

---

## 8. 상태이상 시스템

유닛에 적용되는 상태이상(버프/디버프)의 라이프사이클 관리 프레임워크.

### 8.1 코어

| 클래스 | 역할 | 경로 |
|--------|------|------|
| StatusEffect | 상태이상 추상 기본 클래스. OnApply → OnUpdate(매 프레임) → OnRemove 라이프사이클. IsExpired, RequestRemove() | Common/Scripts/StatusEffect/StatusEffect.cs |
| StatusEffectContext | 상태이상 적용 시 전달하는 동적 데이터 컨테이너. 서브클래스에서 확장 | Common/Scripts/StatusEffect/StatusEffectContext.cs |
| StatusEffectController | 유닛에 부착하는 상태이상 관리 MonoBehaviour. Apply/RemoveImmediate, 매 프레임 만료 체크 | Common/Scripts/StatusEffect/StatusEffectController.cs |

### 8.2 훅 프로바이더

상태이상과 외부 시스템을 연결하는 플러그인 구조.

| 클래스 | 역할 | 경로 |
|--------|------|------|
| IStatusEffectHook | 훅 인터페이스 정의를 위한 마커 인터페이스 | Common/Scripts/StatusEffect/HookProvider/IStatusEffectHook.cs |
| IStatusEffectHookProvider | SEController 플러그인 인터페이스. OnStatusEffectAdded/Removed 콜백, IDisposable | Common/Scripts/StatusEffect/HookProvider/IStatusEffectHookProvider.cs |
| StatusEffectHookProvider\<T\> | 특정 훅 인터페이스를 구현하는 StatusEffect를 자동 필터링/캐싱하는 제네릭 추상 베이스 | Common/Scripts/StatusEffect/HookProvider/StatusEffectHookProvider.cs |

---

## 9. 버블 메시지

월드/스크린 위치에 떠오르며 사라지는 텍스트 버블 시스템.

| 클래스 | 역할 | 경로 |
|--------|------|------|
| BubbleMessageSpawner | 월드/스크린 좌표에 버블 메시지 스폰. ObjectPooler 재사용. 싱글톤 | Common/Scripts/BubbleMessage/BubbleMessageSpawner.cs |
| BubbleMessage | 텍스트 버블 애니메이션 (떠오름 + 페이드아웃). DOTween 시퀀스. Poolable | Common/Scripts/BubbleMessage/BubbleMessage.cs |
| BubbleMessageConfig | 버블 설정 ScriptableObject (duration, floatDistance, ease, 텍스트 기본값) | Common/Scripts/BubbleMessage/BubbleMessageConfig.cs |
| BubbleMessageParams | 버블 파라미터 struct (Color, FontSize, Duration, FloatDistance) | Common/Scripts/BubbleMessage/BubbleMessageParams.cs |

---

## 10. 클릭/선택

| 클래스 | 역할 | 경로 |
|--------|------|------|
| Selectable2D | 좌클릭 시 OnObjectSelectedEvent를 GlobalEventBus로 발행. 드래그 임계값으로 클릭/드래그 구분 | Common/Scripts/Selectable/Selectable2D.cs |

---

## 11. 유틸리티

### 11.1 컬렉션 & 반복

| 클래스 | 역할 | 경로 |
|--------|------|------|
| SafeIterationList\<T\> | 순회 중 Add/Remove를 버퍼링하는 스레드 안전 리스트. 중첩 순회 깊이 추적 | Common/Scripts/SafeIterationList/SafeIterationList.cs |
| SerializableDictionary\<TKey,TValue\> | Inspector 직렬화 가능 Dictionary. ISerializationCallbackReceiver 기반 | Common/Scripts/SerializableDictionary/SerializableDictionary.cs |
| TransformChildrenIterator | Transform 자식 순방향/역방향 안전 순회 확장 메서드 (ChildrenForward, ChildrenBackward) | Common/Scripts/TransformChildrenIterator/TransformChildrenIterator.cs |
| RepeatX | 반복 유틸리티. Times(count, action), Times\<T\>(count, func) → List\<T\> | Common/Scripts/RepeatX/RepeatX.cs |

### 11.2 라이프사이클 & 바인딩

| 클래스 | 역할 | 경로 |
|--------|------|------|
| CallbackLifetimeBinder | OnEnable/OnDisable 쌍 바인딩 MonoBehaviour. 중복 호출 방지 | Common/Scripts/CallbackLifetimeBinders/CallbackLifetimeBinder.cs |

### 11.3 시간

| 클래스 | 역할 | 경로 |
|--------|------|------|
| SerializableTime | Inspector용 시간 입력 struct (h/m/s). TotalSeconds, ToTimeSpan(), 암시적 변환 | Common/Scripts/SerializableTime/SerializableTime.cs |

### 11.4 확장 메서드

| 클래스 | 역할 | 경로 |
|--------|------|------|
| DOTweenExtensions | DOTween Tween → UniTask 변환 (awaitable tween) | Common/Scripts/Extensions/DOTweenExtensions.cs |

### 11.5 열거형

| 클래스 | 역할 | 경로 |
|--------|------|------|
| Fraction | 유닛 진영 enum (Guardian, Aggressor) | Common/Scripts/Enums/Fraction.cs |

---

## 12. 에디터 도구

Inspector 확장 및 에디터 유틸리티. 런타임에는 사용되지 않는다.

| 클래스 | 역할 | 경로 |
|--------|------|------|
| InspectorHintAttribute | 필드 라벨/힌트 표시 속성 (Right / Below 배치) | Common/Scripts/InspectorHint/InspectorHintAttribute.cs |
| InspectorHintDrawer | InspectorHintAttribute 에디터 드로어 | Common/Scripts/InspectorHint/InspectorHintDrawer.cs |
| InspectorDescriptionAttribute | 컴포넌트 Inspector 최상단 설명 박스 표시 속성 | Common/Scripts/InspectorDescriptionAttribute/InspectorDescriptionAttribute.cs |
| UniversalInspectorDescriptionEditor | InspectorDescriptionAttribute 범용 에디터 드로어 | Common/Scripts/InspectorDescriptionAttribute/UniversalInspectorDescriptionEditor.cs |
| SpritePreviewAttribute | Inspector 스프라이트 미리보기 속성 (높이 설정 가능) | Common/Scripts/SpritePreview/SpritePreviewAttribute.cs |
| SpritePreviewDrawer | 스프라이트 프리뷰 렌더링 드로어 | Common/Scripts/SpritePreview/SpritePreviewDrawer.cs |
| SerializableTimeDrawer | SerializableTime Inspector 드로어 ("Xh Ym Zs" 인라인) | Common/Scripts/SerializableTime/SerializableTimeDrawer.cs |
| SerializableDictionaryDrawer | SerializableDictionary Inspector 드로어 (K → V 인라인, 추가/제거) | Common/Scripts/SerializableDictionary/SerializableDictionaryDrawer.cs |
| GridArrangerWindow | Tools 메뉴 에디터 유틸리티. 선택된 GameObject를 그리드 배치 (열 수, 간격, 방향) | Common/Scripts/GridArranger/Editor/GridArrangerWindow.cs |

---

## 13. 게임 데이터 (ScriptableObject)

전장 클래스 맵에서 다루는 데이터 정의와 동일한 항목이므로, 여기서는 경로와 카테고리만 정리한다. 상세는 [battlefield-architecture-map.md](battlefield-architecture-map.md) § 1.11, § 5.3, § 3.3, § 6 참조.

| 카테고리 | 주요 클래스 | 경로 |
|----------|------------|------|
| 유닛 정의 | UnitDefinitionData, UnitStatsByLevelData, UnitLoadOutData | Common/Data/Units/ |
| 스킬 정의 | SkillDefinitionData, SkillCoefficient, SkillCoefficientMap, StatScaling | Common/Data/Skills/ |
| 데미지 | DamageType | Common/Data/Damage/ |
| 시너지 | SynergyDefinitionData, SynergyTier, SynergyType | Common/Data/Synergies/ |
| 전장 | BattlefieldData (RoundData, SpawnEntry, RewardData) | Common/Data/Battlefields/ |
| 설정 | ManaIncomeConfig, PlacementConfig, StarProbabilityConfig | Common/Data/Configs/ |
