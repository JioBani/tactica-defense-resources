using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    [MenuItem("Tools/Animation Framework/Slice All Sprite Sheets (64x64)")]
    public static void SliceAllSpriteSheets()
    {
        string[] spriteFolders =
        {
            "Assets/Common/Sprites/Temp/Units/summons/Blue",
            "Assets/Common/Sprites/Temp/Units/summons/Red",
            "Assets/Common/Sprites/Temp/Units/summons/Green",
            "Assets/Common/Sprites/Temp/Units/summons/Yellow"
        };

        foreach (string spriteFolder in spriteFolders)
        {
            if (!AssetDatabase.IsValidFolder(spriteFolder))
            {
                continue;
            }

            SliceSpriteFolder(spriteFolder);
        }
    }

    private static void SliceSpriteFolder(string spriteFolder)
    {
        string[] pngGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { spriteFolder });

        int processedCount = 0;

        foreach (string guid in pngGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            // .png 파일만 처리, Profiles 폴더는 제외
            if (Path.GetExtension(assetPath).ToLower() != ".png")
            {
                continue;
            }

            if (assetPath.Contains("/Profiles/"))
            {
                continue;
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            // 텍스처 크기를 읽어서 64x64 그리드로 슬라이스
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            int columnCount = texture.width / 64;
            int rowCount = texture.height / 64;

            string baseName = Path.GetFileNameWithoutExtension(assetPath);
            SpriteMetaData[] spriteSheet = new SpriteMetaData[columnCount * rowCount];

            int index = 0;
            for (int row = rowCount - 1; row >= 0; row--)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    spriteSheet[index] = new SpriteMetaData
                    {
                        name = $"{baseName}_{index}",
                        rect = new Rect(column * 64, row * 64, 64, 64),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    };
                    index++;
                }
            }

            importer.spritesheet = spriteSheet;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            processedCount++;
            Debug.Log($"슬라이스 완료: {assetPath} ({columnCount}x{rowCount} = {spriteSheet.Length}개)");
        }

        AssetDatabase.Refresh();
        Debug.Log($"전체 슬라이스 완료: {processedCount}개 파일 처리됨");
    }

    // ── 행-상태 매핑 (스프라이트 시트의 위→아래 순서) ──
    private static readonly string[] RowStateNames = { "Idle", "Attack", "Downed" };
    private static readonly bool[] RowLoopStates = { true, true, false };

    private static readonly string BaseControllerPath =
        "Assets/Scenes/Battle/Feature/Unit/Animation/UnitBase.controller";

    private static readonly int SampleRate = 12;

    [MenuItem("Tools/Animation Framework/Generate All Unit Animations")]
    public static void GenerateAllUnitAnimations()
    {
        string[] spriteFolders =
        {
            "Assets/Common/Sprites/Temp/Units/summons/Blue",
            "Assets/Common/Sprites/Temp/Units/summons/Red",
            "Assets/Common/Sprites/Temp/Units/summons/Green",
            "Assets/Common/Sprites/Temp/Units/summons/Yellow"
        };

        foreach (string spriteFolder in spriteFolders)
        {
            if (!AssetDatabase.IsValidFolder(spriteFolder))
            {
                continue;
            }

            GenerateAnimationsForFolder(spriteFolder);
        }
    }

    private static void GenerateAnimationsForFolder(string spriteFolder)
    {
        string[] pngGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { spriteFolder });

        AnimatorController baseController =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(BaseControllerPath);

        if (baseController == null)
        {
            Debug.LogError($"Base Controller를 찾을 수 없습니다: {BaseControllerPath}");
            return;
        }

        int unitCount = 0;

        foreach (string guid in pngGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            if (Path.GetExtension(assetPath).ToLower() != ".png")
            {
                continue;
            }

            if (assetPath.Contains("/Profiles/"))
            {
                continue;
            }

            string folderPath = Path.GetDirectoryName(assetPath).Replace("\\", "/");
            string baseName = Path.GetFileNameWithoutExtension(assetPath);

            // 텍스처를 읽기 가능하게 설정
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            bool wasReadable = importer.isReadable;
            if (!wasReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            // 서브 스프라이트 로드
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            List<Sprite> sprites = new List<Sprite>();
            foreach (Object asset in allAssets)
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            // 이름 기준 정렬 (baseName_0, baseName_1, ...)
            sprites.Sort((a, b) =>
            {
                int indexA = int.Parse(a.name.Substring(baseName.Length + 1));
                int indexB = int.Parse(b.name.Substring(baseName.Length + 1));
                return indexA.CompareTo(indexB);
            });

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            int columnsPerRow = texture.width / 64;
            int totalRows = texture.height / 64;

            // 행별로 클립 생성
            Dictionary<string, AnimationClip> createdClips = new Dictionary<string, AnimationClip>();

            for (int row = 0; row < Mathf.Min(totalRows, RowStateNames.Length); row++)
            {
                int startIndex = row * columnsPerRow;
                int endIndex = Mathf.Min(startIndex + columnsPerRow, sprites.Count);

                // 빈 스프라이트 필터링 (완전 투명 셀 제외)
                List<Sprite> rowSprites = new List<Sprite>();
                for (int i = startIndex; i < endIndex; i++)
                {
                    Sprite sprite = sprites[i];
                    Rect rect = sprite.textureRect;
                    Color[] pixels = texture.GetPixels(
                        (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);

                    bool hasVisiblePixel = false;
                    foreach (Color pixel in pixels)
                    {
                        if (pixel.a > 0.01f)
                        {
                            hasVisiblePixel = true;
                            break;
                        }
                    }

                    if (hasVisiblePixel)
                    {
                        rowSprites.Add(sprite);
                    }
                }

                if (rowSprites.Count == 0)
                {
                    continue;
                }

                string stateName = RowStateNames[row];
                bool loop = RowLoopStates[row];

                // AnimationClip 생성
                AnimationClip clip = new AnimationClip();
                clip.name = $"{baseName}_{stateName.ToLower()}";
                clip.frameRate = SampleRate;

                // 스프라이트 키프레임 설정
                EditorCurveBinding binding = new EditorCurveBinding
                {
                    type = typeof(SpriteRenderer),
                    path = "",
                    propertyName = "m_Sprite"
                };

                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[rowSprites.Count];
                for (int i = 0; i < rowSprites.Count; i++)
                {
                    keyframes[i] = new ObjectReferenceKeyframe
                    {
                        time = (float)i / SampleRate,
                        value = rowSprites[i]
                    };
                }

                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

                // 루프 설정
                AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
                clipSettings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

                string clipPath = $"{folderPath}/{clip.name}.anim";
                AssetDatabase.CreateAsset(clip, clipPath);
                createdClips[$"placeholder_{stateName.ToLower()}"] = clip;

                Debug.Log($"클립 생성: {clipPath} ({rowSprites.Count}프레임, loop={loop})");
            }

            // 읽기 가능 설정 복원
            if (!wasReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            // OverrideController 생성
            if (createdClips.Count > 0)
            {
                AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
                overrideController.name = baseName;

                // placeholder 클립을 실제 클립으로 교체. 실제 클립이 없는 상태는 Idle 클립으로 채운다.
                createdClips.TryGetValue("placeholder_idle", out AnimationClip idleClip);

                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
                overrideController.GetOverrides(overrides);

                for (int i = 0; i < overrides.Count; i++)
                {
                    string originalName = overrides[i].Key.name;
                    if (createdClips.TryGetValue(originalName, out AnimationClip replacement))
                    {
                        overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(
                            overrides[i].Key, replacement);
                    }
                    else if (idleClip != null)
                    {
                        // 실제 클립이 없는 상태(Move, Freeze, Waiting 등)는 Idle 클립으로 대체
                        overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(
                            overrides[i].Key, idleClip);
                    }
                }

                overrideController.ApplyOverrides(overrides);

                string overridePath = $"{folderPath}/{baseName}.overrideController";
                AssetDatabase.CreateAsset(overrideController, overridePath);
                Debug.Log($"OverrideController 생성: {overridePath}");
            }

            unitCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"전체 애니메이션 생성 완료: {unitCount}개 유닛 처리됨");
    }
}
