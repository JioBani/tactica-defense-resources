# 코드 컨벤션

## Unity 작업 시 도구 사용 기준

## 아키텍처: MonoBehaviour와 비즈니스 로직 분리

MonoBehaviour에는 비즈니스 로직을 최소화하고, 별도의 **서비스 클래스(순수 C#)** 에 로직을 구현한다.
MonoBehaviour는 Unity 콜백(입력, 충돌, 라이프사이클)과 서비스 호출만 담당하는 얇은 레이어로 유지한다.

### 목적

- 비즈니스 로직을 `new`로 인스턴스화하여 **유닛 테스트 가능**하게 만든다.
- MonoBehaviour의 Unity 의존성(Instantiate, MonoBehaviour 콜백 등)으로부터 로직을 격리한다.

### 원칙

- **MonoBehaviour**: Unity 이벤트 수신, 입력 처리, 물리/렌더링 연동, 서비스 메서드 호출
- **서비스 클래스**: 계산, 판정, 상태 전이 등 비즈니스 로직 구현. Unity API에 의존하지 않는다.
- 서비스 클래스는 `new`로 생성 가능해야 하며, MonoBehaviour를 상속하지 않는다.

### 예시

```csharp
// 서비스 클래스 — 순수 C#, 테스트 가능
public class DamageCalculator
{
    public float Calculate(float baseDamage, float multiplier)
    {
        return baseDamage * multiplier;
    }
}

// MonoBehaviour — 얇은 레이어, 서비스 호출만 담당
public class Attacker : MonoBehaviour
{
    private DamageCalculator _damageCalculator = new();

    private void ApplyDamage(Victim victim)
    {
        float damage = _damageCalculator.Calculate(baseDamage, multiplier);
        victim.Hit(damage);
    }
}
```

### 적용 범위

- **새로 작성하는 코드**에 적용한다. 기존 코드는 수정 시점에 점진적으로 분리한다.
- 단순 위임(1~2줄)만으로 끝나는 경우에는 서비스 분리 없이 MonoBehaviour에 직접 작성해도 된다.

## 코드 맥락 파악

- 코드의 설계 의도나 변경 이유가 불분명할 때, `git log -- <파일경로>`로 커밋 메시지를 확인한다.
- 코드베이스 탐색 시 관련 Jira 티켓 번호를 알고 있다면, 키워드 검색보다 **git log 기반 탐색을 우선**한다.
  - `git log --grep="TACD-XXX" --oneline`으로 해당 티켓의 커밋 목록을 먼저 확인한다.
  - 커밋에서 변경된 파일을 `git show --stat <커밋해시>`로 파악한 뒤, 관련 파일을 읽는다.
  - 이 방법이 키워드 기반 Grep/Glob보다 정확하고 빠르다.
- **서브에이전트로 코드베이스를 탐색할 때는 `/explore-code` 스킬을 사용한다.** 아키텍처 맵 활용과 Git 기반 탐색이 포함되어 기본 탐색보다 효율적이다.

## 설계 원칙

### FindObjectOfType 사용 금지

- `FindObjectOfType`, `FindFirstObjectByType` 등 Find 계열 API는 가능하면 사용하지 않는다.
- 전역 접근이 필요하면 `SceneSingleton<T>`을 사용한다.
- 반드시 Find가 필요한 상황이라면, 싱글톤으로 전환할지 사용자에게 먼저 확인한다.

### static 함수 사용 제한

- `static` 함수는 아주 특별한 경우가 아니면 사용하지 않는다.
- `static` 함수를 사용해야 할 것 같으면 사용자에게 먼저 확인한다.
- 가능하면 인스턴스 함수로 만든다.

### 함수의 소속 클래스 결정

- 어떤 함수가 어느 클래스에 속해야 할지 역할을 충분히 고려한다.
- 데이터를 가장 잘 아는 클래스, 또는 해당 로직의 책임을 지는 클래스에 배치한다.

## 코드 스타일

- 변수명은 축약하지 않고 **풀네임**으로 작성한다. (예: `damageCoeff` → `damageCoefficient`)
  - 단, 관용적으로 널리 쓰이는 축약은 허용한다 (예: `info`, `config`, `max`, `min`, `id`)

## 코드 주석 컨벤션

- 새로 추가하는 함수와 변수에는 **한글 주석**을 작성한다.
- 함수는 `/// <summary>` XML doc 형식을 사용한다.
  - **summary는 함수의 역할(정의)** 을 작성한다: 언제, 왜 호출되는지.
  - **구현 세부사항은 함수 본문 안에 inline 주석** 으로 작성한다.
  - summary에 함수가 내부적으로 하는 일(구현)을 나열하지 않는다.
  - 파라미터가 뭘 의미하는지 (필요 시)
  - 간결하게 작성한다 (1~2줄)
- 변수/필드는 `/// <summary>` 한 줄 형식을 사용한다.
- 기존 코드에 주석을 추가하지 않는다 (변경한 코드에만 작성)
- **파일 상단 역할 주석**: 파일의 역할이나 수정 가이드가 클래스명만으로 전달되지 않는 경우, 파일 최상단(namespace 위)에 파일의 책임과 수정 시 참고사항을 간결히 명시한다.
  - 대상: 팩토리, 매니저, 여러 책임이 섞일 수 있는 파일, 수정 빈도가 높은 파일
  - 클래스명만으로 역할이 명확한 단순 파일(예: `DamageCalculator`, `StatScaling`)에는 불필요

```csharp
// 파일 상단 역할 주석 예시
// ─────────────────────────────────────────────
// SkillFactory: SkillDefinitionData 타입에 따라 SkillCast 인스턴스를 생성한다.
// 새 스킬 추가 시 switch 분기를 추가한다.
// ─────────────────────────────────────────────
```

```csharp
// 변수 예시
/// <summary>사거리 내에 있는 적 Victim 목록. 타겟 소실 시 재탐색에 사용한다.</summary>
private readonly List<Victim> _victimsInRange = new();

// 함수 예시 — summary는 역할, 구현 세부사항은 inline
/// <summary>유효한 타겟이 없을 때 호출된다.</summary>
private void TryAcquireTarget()
{
    // 순회 중 파괴되었거나 다운된 엔트리는 목록에서 제거한다.
    ...
}

// BAD — summary에 구현 세부사항을 나열
/// <summary>
/// 사거리 내 적 목록에서 유효한 타겟을 찾아 _victim으로 설정한다.
/// 순회 중 파괴되었거나 다운된 엔트리는 목록에서 제거한다.
/// </summary>
private void TryAcquireTarget() { ... }
```