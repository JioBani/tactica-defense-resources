# StatusEffect 프레임워크

유닛에 영향을 주는 모든 효과(시너지, CC, 버프/디버프, 스킬 등)를 통합 관리하는 프레임워크.

## 용어

| 이름 | 설명 |
|------|------|
| **SE** (StatusEffect) | 유닛에 부여되는 효과 인스턴스. 순수 C# 클래스 |
| **SED** (StatusEffectDefinitionData) | SE의 정적 속성을 정의하는 ScriptableObject |
| **SEController** (StatusEffectController) | MonoBehaviour. SE 컬렉션 관리 + 생명주기 실행 |
| **HookProvider** (IStatusEffectHookProvider) | SEController에 붙는 플러그인. SE에 새로운 생명주기 훅을 추가 |
| **IStatusEffectHook** | 훅 인터페이스의 마커. 새 훅 정의 시 상속 |

## 사용법

### 1. SED 정의

`StatusEffectDefinitionData`를 상속하여 SE의 정적 데이터와 팩토리를 정의한다.

```csharp
[CreateAssetMenu(menuName = "Data/StatusEffects/AttackBuff")]
public class AttackBuffDefinitionData : StatusEffectDefinitionData
{
    public float attackMultiplier = 1.5f;
    public float duration = 5f;

    public override StatusEffect CreateEffect()
    {
        return new AttackBuffEffect(this);
    }
}
```

### 2. SE 구현

`StatusEffect`를 상속하여 효과 로직을 구현한다.

```csharp
public class AttackBuffEffect : StatusEffect
{
    private readonly AttackBuffDefinitionData _data;
    private float _elapsed;

    public AttackBuffEffect(AttackBuffDefinitionData data)
    {
        _data = data;
    }

    public override void OnApply(StatusEffectContext context)
    {
        // 스탯 수정자 추가 등
    }

    public override void OnUpdate()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _data.duration)
            RequestRemove();
    }

    public override void OnRemove()
    {
        // 스탯 수정자 제거 등
    }
}
```

### 3. SEController에 Apply

```csharp
StatusEffect effect = definitionData.CreateEffect();
statusEffectController.Apply(effect, new StatusEffectContext());
```

외부에서 즉시 제거할 때:

```csharp
statusEffectController.RemoveImmediate(effect);
```

## API 레퍼런스

### StatusEffect

| 메서드 | 설명 |
|--------|------|
| `OnApply(context)` | SE 부여 시 호출 |
| `OnUpdate()` | 매 프레임 호출 |
| `OnRemove()` | SE 제거 시 호출 |
| `RequestRemove()` | 자기 만료 요청. Update 루프 끝에 일괄 제거됨 |

| 프로퍼티 | 설명 |
|----------|------|
| `Controller` | 이 SE를 관리하는 SEController |
| `IsExpired` | 만료 여부 (RequestRemove 호출 시 true) |

### StatusEffectController

| 메서드 | 접근 | 설명 |
|--------|------|------|
| `Apply(se, context)` | public | SE 부여 |
| `RemoveImmediate(se)` | public | SE 즉시 제거 |
| `AddHookProvider(hh)` | public | HookProvider 등록 |
| `AddStatusEffectController(hh...)` | public | 여러 HookProvider 등록 |
| `RemoveHookProvider(hh)` | public | HookProvider 해제 |

### IStatusEffectHookProvider

| 메서드 | 설명 |
|--------|------|
| `OnStatusEffectAdded(se)` | SE 추가 알림 |
| `OnStatusEffectRemoved(se)` | SE 제거 알림 |
| `Dispose()` | 리소스 정리 |

### StatusEffectHookProvider\<T\>

`IStatusEffectHookProvider`의 추상 기본 클래스. `IStatusEffectHook`을 구현한 SE를 자동 캐싱한다.

| 멤버 | 설명 |
|------|------|
| `StatusEffects` | 훅 인터페이스를 구현한 SE 목록 (protected) |

## HookProvider 확장

### 훅(Hook)이란?

SE는 기본 생명주기로 `OnApply`, `OnUpdate`, `OnRemove` 세 가지를 가진다. 이것만으로는 "피격당했을 때", "공격했을 때", "라운드가 시작될 때" 같은 **게임 이벤트에 반응하는 SE**를 만들 수 없다.

**훅(Hook)** 은 SE가 반응할 수 있는 새로운 시점을 의미한다. 예를 들어 `OnDamaged`라는 훅을 정의하면, SE는 "피격 시"라는 시점에 끼어들 수 있게 된다.

이 훅은 프레임워크가 미리 정해놓는 것이 아니라, **게임 쪽에서 필요할 때 자유롭게 정의**한다. 프레임워크는 훅을 정의하고 전달하는 구조만 제공한다.

**HookProvider**는 이 훅을 SE에 제공하는 플러그인이다. 구체적으로는:

1. 새로운 훅 인터페이스를 정의한다 (예: `IOnDamagedHook`)
2. SE는 반응하고 싶은 훅 인터페이스를 구현한다
3. HookProvider가 해당 인터페이스를 구현한 SE를 자동으로 캐싱한다
4. 게임 이벤트 발생 시, HookProvider가 캐싱된 SE들에게 훅을 실행한다

```
[기본 생명주기]        SEController가 직접 실행
  OnApply
  OnUpdate
  OnRemove

[확장 훅]             HookProvider가 제공
  OnDamaged           ← DamageHookProvider
  OnAttack            ← AttackHookProvider
  OnRoundStart        ← RoundHookProvider
  ...                 ← 게임에서 필요한 만큼 추가
```

### 1. 훅 인터페이스 정의

```csharp
public interface IOnDamagedHook : IStatusEffectHook
{
    void OnDamaged(float damage);
}
```

### 2. HookProvider 구현

`StatusEffectHookProvider<T>`를 상속하면 캐싱이 자동 처리된다.

```csharp
public class DamageHookProvider : StatusEffectHookProvider<IOnDamagedHook>
{
    public void OnDamaged(float damage)
    {
        foreach (var se in StatusEffects)
            se.OnDamaged(damage);
    }
}
```

### 3. 바인더 컴포넌트로 등록 (조립식)

HookProvider는 **바인더 컴포넌트**(MonoBehaviour)를 통해 프리팹에 조립한다.
SEController를 서브클래싱하지 않고, 필요한 훅만 선택적으로 부착할 수 있다.

바인더 파일에는 **Binder(MonoBehaviour) + Provider + Hook 인터페이스**를 한 파일에 선언한다.
파일명은 MonoBehaviour 기준으로 한다 (Unity 제약).

```csharp
// DamageHookBinder.cs — 한 파일에 Binder + Provider + Interface
public class DamageHookBinder : MonoBehaviour
{
    [SerializeField] private StatusEffectController statusEffectController;
    [SerializeField] private Victim victim; // 이벤트 소스

    private void Awake()
    {
        if (statusEffectController != null && victim != null)
            statusEffectController.AddHookProvider(new DamageHookProvider(victim));
    }
}

public class DamageHookProvider : StatusEffectHookProvider<IOnDamagedHook>
{
    public DamageHookProvider(Victim victim)
    {
        victim.OnDamaged += HandleDamaged;
    }

    private void HandleDamaged(float damage)
    {
        foreach (var se in StatusEffects)
            se.OnDamaged(damage);
    }

    public override void Dispose() { /* 구독 해제 */ }
}

public interface IOnDamagedHook : IStatusEffectHook
{
    void OnDamaged(float damage);
}
```

#### 프리팹 구조

바인더들은 Unit.prefab의 `StatusEffectController` 자식 오브젝트에 부착한다.
Variant(Defender, Aggressor)가 자동으로 상속받는다.

```
Unit
├── Attacker
├── Victim
├── HealthBar
└── StatusEffectController          ← 빈 GameObject
    ├── [AttackHitHookBinder]
    ├── [ActionStateHookBinder]
    └── [새 바인더 추가 시 여기에 부착]
```

### 기존 훅 구현 목록

| 파일 | 훅 인터페이스 | 이벤트 소스 | 용도 |
|------|--------------|------------|------|
| `AttackHitHookBinder.cs` | `IOnAttackHitHook` | `Attacker.OnAttackHitEvent` | 공격 적중 시 반응 (프렐요드 둔화 등) |
| `ActionStateHookBinder.cs` | `IOnActionStateChangedHook` | `ActionStateController` (IStateListener) | 상태 전환 시 반응 (전쟁기계 사망 회복 등) |

## 제거 메커니즘

- **외부 제거**: `RemoveImmediate(se)` — 호출 즉시 OnRemove + 제거
- **자체 만료**: `RequestRemove()` — IsExpired = true로 설정, Update 루프 끝에 일괄 제거
- **연쇄 제거 안전**: 모든 SE의 OnUpdate를 실행한 뒤, 만료된 SE를 일괄 제거하여 실행 순서를 보장
