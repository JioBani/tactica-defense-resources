# 개발 지침

## Unity 작업 시 도구 사용 기준

### 스크립트 작업
- 스크립트 생성: MCP `create_script`를 사용한다.
- 스크립트 내용 편집: Claude 기본 도구(`Read`/`Edit`/`Write`)를 사용한다.
- 편집 후 MCP `refresh_unity` + `read_console`로 컴파일 결과를 확인한다.

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
