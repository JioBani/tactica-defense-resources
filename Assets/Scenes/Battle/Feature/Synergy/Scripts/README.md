# 시너지 프레임워크

같은 특성을 가진 유닛이 일정 수 이상 전장에 배치되면 활성화되는 버프 시스템.

## 전체 흐름

```
Defender 배치 변경
  -> SynergyManager.Recalculate()
    -> 시너지별 유니크 카운트 산출
    -> SynergyActivation.Recalculate(count)  -> 티어 판정
    -> 티어 변경 시 SynergyController에 위임
      -> OnActivated: SSE 부여 + 자식 확장점
      -> OnDeactivated: 추적 초기화
```

## 클래스 역할

| 클래스 | 역할 |
|--------|------|
| **SynergyManager** | 트리거. Defender 배치 변경 감지 -> 카운트 산출 -> Controller 호출 |
| **SynergyActivation** | 카운트->티어 판정. `ActiveTier`(RxValue)로 구독 가능 |
| **SynergyController** | Template Method. SSE 부여 보장 + 자식 확장점(`OnAfterActivated`) |
| **SynergyControllerFactory** | SceneSingleton. SynergyId별 Controller 생성 + 의존성 주입 |
| **SynergyStatusEffect\<T\>** | 보유 유닛용 SSE. ActiveTier 구독으로 생명주기 자동 관리 |
| **TierLinkedStatusEffect\<T\>** | 비보유 유닛용 부수 SE. SSE와 동일한 티어 구독 구조 |

## SE 타입 구분

| 타입 | 기본 클래스 | 대상 | 용도 |
|------|------------|------|------|
| SSE | `SynergyStatusEffect<TContext>` | 시너지 보유 유닛 | 시너지 정체성 + 효과 |
| TLSE | `TierLinkedStatusEffect<TContext>` | 비보유 유닛 | 부수 버프 (정체성 없음) |

## SSE/TLSE 생명주기 콜백

```csharp
// SSE
OnSynergyActivated(SynergyTier tier)      // 최초 활성화
OnSynergyTierChanged(SynergyTier newTier) // 티어 변경
OnSynergyDeactivated()                    // 비활성화 -> 자동 RequestRemove
OnRemove()                                // 정리

// TLSE -- 동일 구조, 메서드명만 다름
OnTierActivated / OnTierChanged / OnTierDeactivated / OnRemove
```

## Controller 확장점

```csharp
// 필수: 이 시너지의 SSE 인스턴스 생성
protected abstract SynergyStatusEffect CreateSynergyStatusEffect();

// 선택: SSE 부여 후 추가 로직 (전체 아군 버프 등)
protected virtual void OnAfterActivated(List<Defender> synergyDefenders) { }

// 선택: 비활성화 시 추가 정리
public virtual void OnDeactivated() { }
```

## 데이터 구조

```
SynergyDefinitionData (SO)
+-- Id (SynergyId enum)
+-- DisplayName, Icon
+-- StatusEffectDefinition -> StatusEffectDefinitionData (SSE 메타데이터)
+-- Tiers[]
    +-- RequiredCount (활성화 임계치)
    +-- Constants (Dictionary<string, float>)
```

## 파일 구조

```
Synergy/Scripts/
+-- SynergyManager.cs                    # 트리거 + 그룹핑
+-- SynergyActivation.cs                 # 카운트->티어 판정
+-- SynergyController.cs                 # Template Method 추상 클래스
+-- SynergyControllerFactory.cs          # SceneSingleton, Controller 생성
+-- SynergyStatusEffect.cs              # SSE 추상 클래스
+-- TierLinkedStatusEffect.cs           # TLSE 추상 클래스
+-- SynergyStatusEffectContext.cs       # SSE용 Context
+-- TierLinkedStatusEffectContext.cs    # TLSE용 Context
+-- SynergyControllers/
|   +-- BruiserSynergyController.cs     # 단순형 예시
|   +-- ArcanistSynergyController.cs    # 복합형 예시
+-- SynergyEffects/
    +-- BruiserSynergyStatusEffect.cs   # SSE 예시
    +-- ArcanistSynergyStatusEffect.cs  # SSE 예시
    +-- ArcanistSpellPowerEffect.cs     # TLSE 예시
```

---

## 새 시너지 추가 가이드

### 단순형 (난동꾼 패턴 -- 보유 유닛에만 SSE)

#### 1. SSE 구현

`SynergyEffects/`에 `SynergyStatusEffect<SynergyStatusEffectContext>`를 상속하는 클래스를 만든다.

```csharp
public class NewSynergyStatusEffect : SynergyStatusEffect<SynergyStatusEffectContext>
{
    public NewSynergyStatusEffect(StatusEffectDefinitionData definition) : base(definition) { }

    private const string BuffKey = "buffValue"; // SynergyTier.Constants의 키

    protected override void OnSynergyActivated(SynergyTier tier)
    {
        float? value = tier.Get(BuffKey);
        if (!value.HasValue) return;
        SynergyContext.Unit.StatSheet.SomeStat.AddModifier(
            new StatModifier(this, StatModifierType.Flat, value.Value));
    }

    protected override void OnSynergyTierChanged(SynergyTier newTier)
    {
        RemoveModifier();
        OnSynergyActivated(newTier); // 새 티어로 재적용
    }

    protected override void OnSynergyDeactivated()
    {
        RemoveModifier();
    }

    public override void OnRemove()
    {
        base.OnRemove(); // ActiveTier 구독 해제
        RemoveModifier();
    }

    private void RemoveModifier()
    {
        SynergyContext.Unit.StatSheet.SomeStat.RemoveModifiersBySource(this);
    }
}
```

#### 2. Controller 구현

`SynergyControllers/`에 `SynergyController`를 상속하는 클래스를 만든다.
단순형은 `CreateSynergyStatusEffect()`만 구현하면 된다.

```csharp
public class NewSynergyController : SynergyController
{
    public NewSynergyController(SynergyActivation activation)
        : base(activation) { }

    protected override SynergyStatusEffect CreateSynergyStatusEffect()
    {
        return new NewSynergyStatusEffect(Definition.StatusEffectDefinition);
    }
}
```

#### 3. Factory 등록

`SynergyControllerFactory.Create()`의 switch에 분기를 추가한다.

```csharp
SynergyId.NewSynergy => new NewSynergyController(activation),
```

#### 4. 데이터 에셋 생성

1. `SynergyId` enum에 새 값 추가
2. `Common/Data/StatusEffects/`에 StatusEffectDefinitionData SO 생성
3. `Common/Data/Synergies/`에 SynergyDefinitionData SO 생성
   - `StatusEffectDefinition`에 위에서 만든 SE 에셋 할당
   - `Tiers`에 임계치와 상수(Constants) 설정
4. `SynergyManager.allSynergies` 리스트에 추가

---

### 복합형 (비전 마법사 패턴 -- 비보유 유닛에도 부수 효과)

1~4는 단순형과 동일. 추가로:

#### 5. 부수 SE(TLSE) 구현

`SynergyEffects/`에 `TierLinkedStatusEffect<TierLinkedStatusEffectContext>`를 상속한다.
SSE와 콜백 구조가 동일하되, 메서드명이 다르다.

```csharp
public class NewSubEffect : TierLinkedStatusEffect<TierLinkedStatusEffectContext>
{
    public NewSubEffect(StatusEffectDefinitionData definition) : base(definition) { }

    private const string BuffKey = "subBuffValue";

    protected override void OnTierActivated(SynergyTier tier)
    {
        float? value = tier.Get(BuffKey);
        if (!value.HasValue) return;
        TierContext.Unit.StatSheet.SomeStat.AddModifier(
            new StatModifier(this, StatModifierType.Flat, value.Value));
    }

    protected override void OnTierChanged(SynergyTier newTier)
    {
        RemoveModifier();
        OnTierActivated(newTier);
    }

    protected override void OnTierDeactivated()
    {
        RemoveModifier();
    }

    public override void OnRemove()
    {
        base.OnRemove();
        RemoveModifier();
    }

    private void RemoveModifier()
    {
        TierContext.Unit.StatSheet.SomeStat.RemoveModifiersBySource(this);
    }
}
```

#### 6. Controller에서 부수 SE 부여

`OnAfterActivated`를 오버라이드하여 비보유 유닛에 TLSE를 Apply한다.

```csharp
public class NewComplexController : SynergyController
{
    private readonly DefenderManager _defenderManager;
    private readonly StatusEffectDefinitionData _subEffectDefinition;
    private readonly HashSet<Defender> _appliedNonSynergyDefenders = new();

    public NewComplexController(
        SynergyActivation activation,
        DefenderManager defenderManager,
        StatusEffectDefinitionData subEffectDefinition)
        : base(activation)
    {
        _defenderManager = defenderManager;
        _subEffectDefinition = subEffectDefinition;
    }

    protected override SynergyStatusEffect CreateSynergyStatusEffect()
    {
        return new NewSynergyStatusEffect(Definition.StatusEffectDefinition);
    }

    protected override void OnAfterActivated(List<Defender> synergyDefenders)
    {
        List<Defender> allDefenders = _defenderManager.GetBattleAreaDefenders();
        HashSet<Defender> synergySet = new(synergyDefenders);

        foreach (Defender defender in allDefenders)
        {
            if (synergySet.Contains(defender)) continue;
            if (!_appliedNonSynergyDefenders.Add(defender)) continue;

            var effect = new NewSubEffect(_subEffectDefinition);
            var context = new TierLinkedStatusEffectContext(Activation, Definition, defender);
            defender.StatusEffectController.Apply(effect, context);
        }
    }

    public override void OnDeactivated()
    {
        base.OnDeactivated();
        _appliedNonSynergyDefenders.Clear();
    }
}
```

#### 7. Factory에 의존성 주입

부수 SE용 StatusEffectDefinitionData를 `SynergyControllerFactory`에 SerializeField로 추가하고,
Controller 생성자에 전달한다.

```csharp
// SynergyControllerFactory
[SerializeField] private StatusEffectDefinitionData newSubEffectDefinition;

// Create() switch 분기
SynergyId.NewSynergy => new NewComplexController(
    activation, defenderManager, newSubEffectDefinition),
```

인스펙터에서 `newSubEffectDefinition`에 SO 에셋을 할당한다.
