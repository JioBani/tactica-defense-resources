# 코드 컨벤션

## 1. 조기 return 지양

- 함수 중간에 `return`으로 탈출하는 패턴을 지양한다.
- 조기 return이 코드를 **확실히 간단하게** 만드는 경우(guard clause 1줄 등)에만 허용한다.
- 그렇지 않으면 함수의 완결 지점이 분산되어 흐름을 따라가기 어려워진다.
- 대안: `if-else` 블록으로 분기를 명시적으로 표현한다.

```csharp
// BAD — 완결 지점이 3곳
void Process(Data data)
{
    if (data == null) return;
    if (!data.IsValid) return;
    DoWork(data);
}

// GOOD — 완결 지점이 1곳
void Process(Data data)
{
    if (data != null && data.IsValid)
    {
        DoWork(data);
    }
}
```

## 2. if 문 중괄호 필수

- `if`, `else if`, `else` 블록은 본문이 한 줄이라도 반드시 `{}`로 감싼다.

```csharp
// BAD
if (count > 0)
    Execute();

// GOOD
if (count > 0)
{
    Execute();
}
```

## 3. 함수 주석: 역할과 구현의 분리

- `/// <summary>`는 함수의 **역할(정의)** 만 작성한다: 언제, 왜 호출되는지.
- 함수가 내부적으로 하는 일(구현 세부사항)은 **함수 본문 안에 inline 주석**으로 작성한다.
- summary에 구현을 나열하면, 함수의 정의와 실제 동작이 섞여 의도 파악이 어려워진다.

```csharp
// BAD — summary가 구현 세부사항을 나열
/// <summary>
/// 시너지 활성화 시 모든 시너지 Defender에 SSE를 일괄 부여한다.
/// </summary>
private void HandleTierActivated(SynergyTier tier)
{
    ApplySSEToDefenders(_synergyDefenders);
}

// GOOD — summary는 역할, 구현은 본문에서 설명
/// <summary>시너지가 활성화될 때 호출된다.</summary>
private void HandleTierActivated(SynergyTier tier)
{
    ApplySSEToDefenders(_synergyDefenders);
}

// GOOD — 비자명한 구현은 inline 주석으로
/// <summary>시너지가 비활성화될 때 호출된다.</summary>
private void HandleTierDeactivated()
{
    // SSE는 ActiveTier 구독으로 자체 제거되므로, 추적만 초기화한다.
    _appliedDefenders.Clear();
    OnAfterDeactivated();
}
```
