using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Base AnimatorController 및 placeholder 클립을 자동 생성하는 에디터 유틸리티.
/// 메뉴: Tools > Animation Framework > Create Base Controller
/// </summary>
public static class AnimationFrameworkSetup
{
    private static readonly string OutputFolder = "Assets/Scenes/Battle/Feature/Unit/Animation";

    private static readonly string[] StateNames =
    {
        "Idle", "Move", "Attack", "Downed", "Freeze", "Waiting"
    };

    private static readonly bool[] LoopStates =
    {
        true, true, false, false, false, true
    };

    [MenuItem("Tools/Animation Framework/Create Base Controller")]
    public static void CreateBaseController()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            Debug.LogError($"폴더가 존재하지 않습니다: {OutputFolder}");
            return;
        }

        // 1. Placeholder 클립 생성
        AnimationClip[] clips = new AnimationClip[StateNames.Length];

        for (int i = 0; i < StateNames.Length; i++)
        {
            string clipPath = $"{OutputFolder}/placeholder_{StateNames[i].ToLower()}.anim";

            AnimationClip clip = new AnimationClip();
            clip.name = $"placeholder_{StateNames[i].ToLower()}";

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = LoopStates[i];
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, clipPath);
            clips[i] = clip;

            Debug.Log($"클립 생성: {clipPath} (loop={LoopStates[i]})");
        }

        // 2. AnimatorController 생성
        string controllerPath = $"{OutputFolder}/UnitBase.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // 기본 레이어의 상태 머신
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // 3. 각 상태 추가
        for (int i = 0; i < StateNames.Length; i++)
        {
            AnimatorState state = stateMachine.AddState(StateNames[i]);
            state.motion = clips[i];

            // Idle을 기본 상태로 설정
            if (StateNames[i] == "Idle")
            {
                stateMachine.defaultState = state;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Base AnimatorController 생성 완료: {controllerPath}");
    }
}
