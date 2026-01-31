#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Common.Scripts.GridArranger.Editor
{
    public class GridArrangerWindow : EditorWindow
    {
        private enum GrowDirection
        {
            RightDown,
            RightUp,
            LeftDown,
            LeftUp,
        }

        [SerializeField] private int columns = 3;
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private GrowDirection growDirection = GrowDirection.RightDown;
        [SerializeField] private Vector2 spacing = new Vector2(1f, 1f);

        private readonly List<GameObject> _targets = new List<GameObject>();
        private Vector2 _scrollPos;

        [MenuItem("Tools/Grid Arranger")]
        private static void Open()
        {
            GetWindow<GridArrangerWindow>("Grid Arranger");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", columns));
            startPosition = EditorGUILayout.Vector3Field("Start Position", startPosition);
            growDirection = (GrowDirection)EditorGUILayout.EnumPopup("Grow Direction", growDirection);
            spacing = EditorGUILayout.Vector2Field("Spacing", spacing);

            EditorGUILayout.Space(10);

            // 선택된 오브젝트 목록
            EditorGUILayout.LabelField($"Targets ({_targets.Count})", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Selection"))
            {
                foreach (var go in Selection.gameObjects)
                {
                    if (!_targets.Contains(go))
                        _targets.Add(go);
                }
            }

            if (_targets.Count > 0 && GUILayout.Button("Clear All"))
            {
                _targets.Clear();
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(200));
            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                _targets[i] = (GameObject)EditorGUILayout.ObjectField(_targets[i], typeof(GameObject), true);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                    _targets.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            // null 제거
            _targets.RemoveAll(t => t == null);

            EditorGUILayout.Space(10);

            GUI.enabled = _targets.Count > 0;
            if (GUILayout.Button("Arrange", GUILayout.Height(32)))
            {
                Arrange();
            }
            GUI.enabled = true;
        }

        private void Arrange()
        {
            int colDir = growDirection is GrowDirection.RightDown or GrowDirection.RightUp ? 1 : -1;
            int rowDir = growDirection is GrowDirection.RightDown or GrowDirection.LeftDown ? -1 : 1;

            Undo.RecordObjects(_targets.Select(t => t.transform).ToArray(), "Grid Arrange");

            for (int i = 0; i < _targets.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;

                Vector3 pos = startPosition + new Vector3(
                    col * spacing.x * colDir,
                    row * spacing.y * rowDir,
                    0f
                );

                _targets[i].transform.position = pos;
            }
        }
    }
}
#endif
