# 개발 지침

## Unity 작업 시 도구 사용 기준

### 스크립트 작업
- 스크립트 생성: MCP `create_script`를 사용한다.
- 스크립트 내용 편집: Claude 기본 도구(`Read`/`Edit`/`Write`)를 사용한다.
- 편집 후 MCP `refresh_unity` + `read_console`로 컴파일 결과를 확인한다.

### UI 기본 폰트
- TextMeshPro 사용 시 기본 폰트는 **Maplestory Bold SDF** (`Assets/TextMesh Pro/Fonts/Maplestory Bold/Maplestory Bold SDF.asset`)를 사용한다.

### Unity Editor 조작: MCP 우선 사용
GameObject, 컴포넌트, 에셋, 씬 등 Unity Editor 조작은 반드시 MCP 도구를 우선 사용한다.

- GameObject 생성/수정/삭제: `manage_gameobject`
- 컴포넌트 추가/제거/속성 변경: `manage_components`
- 에셋 관리: `manage_asset`
- 머티리얼/텍스쳐/셰이더: `manage_material`, `manage_texture`, `manage_shader`
- 프리팹: `manage_prefabs`
- 씬 관리: `manage_scene`
- 콘솔 확인: `read_console`
- 에디터 제어 (Play/Pause/Stop): `manage_editor`

## 코드 스타일

- 변수명은 축약하지 않고 **풀네임**으로 작성한다. (예: `damageCoeff` → `damageCoefficient`)
  - 단, 관용적으로 널리 쓰이는 축약은 허용한다 (예: `info`, `config`, `max`, `min`, `id`)

## 코드 주석 컨벤션

- 새로 추가하는 함수와 변수에는 **한글 주석**을 작성한다.
- 함수는 `/// <summary>` XML doc 형식을 사용한다.
  - 어떤 역할을 하는 함수인지
  - 파라미터가 뭘 의미하는지
  - 왜 추가된 함수인지 (필요 시)
  - 간결하게 작성한다 (1~3줄)
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

// 함수 예시
/// <summary>
/// 사거리 내 적 목록에서 유효한 타겟을 찾아 _victim으로 설정한다.
/// 순회 중 파괴되었거나 다운된 엔트리는 목록에서 제거한다.
/// </summary>
private void TryAcquireTarget() { ... }
```
