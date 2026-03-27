# StatusEffect 프레임워크 설계

> TACD-123 브레인스토밍을 기반으로 정리한 구조 설계 문서.
> 코드 수준의 상세 구현이 아닌, 구조적 설계에 집중한다.

---

## 용어 정리

| 약어 | 정식 명칭 | 설명 |
|------|-----------|------|
| SE | StatusEffect | 유닛에 부여되는 효과 인스턴스 |
| SED | StatusEffect Definition | SE의 정의 (ScriptableObject) |
| SEController | StatusEffect Controller | 유닛에 부착, SE 컬렉션 관리 및 생명주기 실행 |
| Extension | SE Extension | SEController에 붙는 트리거 확장 플러그인 |

---

## 전체 구조

```
[부여 레이어]                    [SE 프레임워크]
SynergyManager, SkillExecutor   SEController (유닛에 부착)
ItemController 등                ├─ SE 컬렉션 관리 (Apply / Remove)
        │                       ├─ 기본 생명주기 실행 (OnApply, OnUpdate, OnRemove)
        │                       └─ Extension에 SE 추가/제거 알림
        │
        └─ SE 생성 + Apply 요청 ─→  Extensions (트리거 확장)
                                     ├─ CombatSEExtension (OnAttack, OnHit, OnKill ...)
                                     ├─ DefenderSEExtension (OnPlaced, OnSynergyActivated ...)
                                     └─ AggressorSEExtension (OnSpawned, OnReachedGoal ...)
                                            │
                                            └─ 트리거 발화 → 해당 인터페이스를 구현한 SE 호출
```

---

## 핵심 설계 결정

### 1. SEController는 범용 컨테이너

- SE 컬렉션의 보유, 추가, 제거만 담당한다.
- 게임 특화 개념(시너지, 스킬, 전투 등)을 일절 알지 못한다.
- SE 추가/제거 시 등록된 Extension들에게 알림을 보낸다.

### 2. 트리거 확장은 Extension(플러그인)으로

- SEController의 보편성을 유지하면서 생명주기를 확장하는 메커니즘이다.
- Extension은 **순수 C# 객체**로 구현한다 (MonoBehaviour가 아님).
  - Unity 콜백은 SEController(MonoBehaviour)가 수신하여 Extension에 위임한다.
  - 게임 이벤트는 GlobalEventBus나 기존 컴포넌트의 C# event/delegate로 구독한다.
  - 향후 MonoBehaviour가 반드시 필요한 Extension이 생기면, ISEExtension 인터페이스를 공유하므로 혼용 가능하도록 확장한다.
- Extension은 공통 인터페이스(ISEExtension)를 구현하며, SEController가 생성·보유한다.
- 게임 이벤트를 감지하여 SE에 트리거를 전달하는 **어댑터** 역할이다.

#### Extension 조합 예시

| 유닛 타입 | Extension 구성 |
|-----------|---------------|
| Defender | CombatSEExtension + DefenderSEExtension |
| Aggressor | CombatSEExtension + AggressorSEExtension |

### 3. SE의 트리거 반응은 인터페이스로

- SE는 반응할 트리거에 해당하는 인터페이스를 구현한다. (예: IOnAttackReactive, IOnHitReactive)
- Extension은 이벤트 발생 시 해당 인터페이스를 구현한 SE만 호출한다.
- 인터페이스별로 전달 데이터(파라미터)를 명확히 정의할 수 있다.
- 새 트리거 추가 시: 인터페이스 정의 + Extension에서 호출 추가. SEController 수정 불필요.

### 4. Extension이 자체 캐싱

- SE 추가/제거 알림을 받을 때, 자기가 관심 있는 인터페이스만 캐싱한다.
- 트리거 발화 시 캐시에서 바로 조회하여 매번 타입 체크하는 비용을 회피한다.

### 5. SE의 생명주기

- 제거 트리거는 별도 개념이 아니라, **SE의 생명주기 안에 포함**된다.
- SEController가 SE의 생명주기를 실행하며, 실행 도중 종료 조건이 충족되면 자연스럽게 제거된다.
- 예: 시간 기반 SE → OnUpdate에서 타이머 감소 → 만료 시 제거 요청

### 6. SE도 확장 가능

- SE는 기본 인터페이스 위에 확장하여 Synergy SE 등 특화된 SE를 만들 수 있다.
- 기본 SE 프레임워크를 유지하면서 도메인별 요구사항을 수용한다.

---

## 의존성 주입

### 데이터의 두 경로

| 구분 | 출처 | 예시 |
|------|------|------|
| 정적 데이터 | SED (ScriptableObject)에 정의 | 데미지 계수, 지속시간, 부여할 하위 SE 참조 |
| 동적 데이터 | 부여자가 Context로 전달 | 시너지 티어 Constants, 시전자 정보 |

### 환경 의존 데이터

- SE가 필요 시 기존 싱글톤 매니저를 통해 직접 조회한다 (Pull 방식).
- 예: 주변 아군 수 → DefenderManager에서 조회

### SE 생성 흐름

```
부여자가 SED + Context를 준비
→ SEController.Apply() 호출
→ SEController가 SE 인스턴스 생성
→ Extensions에 알림
→ SE 생명주기 시작
```

---

## 관련 문서

- [시너지-시스템-변경.md](시너지-시스템-변경.md) — SE 프레임워크 도입에 따른 기존 시너지 시스템 변경 사항