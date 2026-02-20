#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Common.Scripts.SerializableDictionary.Editor
{
    /// <summary>
    /// SerializableDictionary 전용 커스텀 PropertyDrawer.
    /// Key → Value 페어를 한 줄에 나란히 표시하고, 중복 키 경고와 추가/삭제 기능을 제공한다.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableDictionaryBase), true)]
    public class SerializableDictionaryDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;
        private const float RemoveButtonWidth = 20f;
        private const float ArrowWidth = 20f;
        private const float LabelWidth = 16f;
        private const float AddButtonHeight = 20f;
        private const float EmptyStateHeight = 36f;
        private const float WarningBoxHeight = 24f;

        private static GUIStyle _keyLabelStyle;
        private static GUIStyle _valueLabelStyle;
        private static GUIStyle _arrowStyle;
        private static GUIStyle _emptyStyle;
        private static GUIStyle _countStyle;
        private static GUIStyle _removeButtonStyle;

        /// <summary>스타일을 초기화한다. OnGUI에서 한 번만 생성되도록 캐싱한다.</summary>
        private static void EnsureStyles()
        {
            if (_keyLabelStyle != null) return;

            _keyLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.29f, 0.62f, 1f) }, // #4a9eff
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };

            _valueLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.75f, 0.52f, 0.99f) }, // #c084fc
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };

            _arrowStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
                fontSize = 12
            };

            _emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 11,
                wordWrap = true
            };

            _countStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                alignment = TextAnchor.MiddleRight
            };

            _removeButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight; // Foldout 헤더

            if (!property.isExpanded) return height;

            var keys = property.FindPropertyRelative("keys");
            var values = property.FindPropertyRelative("values");
            if (keys == null || values == null) return height;

            int count = keys.arraySize;

            if (count == 0)
            {
                // 빈 상태 메시지
                height += Spacing + EmptyStateHeight;
            }
            else
            {
                // 각 엔트리 높이
                for (int i = 0; i < count; i++)
                {
                    float entryHeight = GetEntryHeight(keys, values, i);
                    height += Spacing + entryHeight;
                }
            }

            // + Add 버튼
            height += Spacing + AddButtonHeight;

            // 중복 키 경고
            if (HasDuplicateKeys(keys))
                height += Spacing + WarningBoxHeight;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureStyles();
            EditorGUI.BeginProperty(position, label, property);

            var keys = property.FindPropertyRelative("keys");
            var values = property.FindPropertyRelative("values");

            if (keys == null || values == null)
            {
                EditorGUI.LabelField(position, label.text, "keys/values not found");
                EditorGUI.EndProperty();
                return;
            }

            // 배열 크기 동기화
            SyncArraySizes(keys, values);

            int count = keys.arraySize;

            // ── Foldout 헤더 ──
            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            // 엔트리 수 표시
            var countRect = new Rect(position.x + position.width - 80f, position.y, 80f, EditorGUIUtility.singleLineHeight);
            GUI.Label(countRect, $"{count} entries", _countStyle);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            float y = foldoutRect.yMax;

            // ── 중복 키 감지 ──
            var duplicateIndices = FindDuplicateIndices(keys);

            // ── 빈 상태 ──
            if (count == 0)
            {
                y += Spacing;
                var emptyRect = new Rect(position.x, y, position.width, EmptyStateHeight);
                EditorGUI.HelpBox(emptyRect, "{ }  Dictionary is empty. Click \"+\" to add an entry.", MessageType.None);
                y = emptyRect.yMax;
            }
            else
            {
                // ── 엔트리 그리기 ──
                int removeIndex = -1;

                for (int i = 0; i < count; i++)
                {
                    y += Spacing;
                    float entryHeight = GetEntryHeight(keys, values, i);
                    var entryRect = new Rect(position.x, y, position.width, entryHeight);

                    bool isDuplicate = duplicateIndices.Contains(i);
                    if (DrawEntry(entryRect, keys, values, i, isDuplicate))
                        removeIndex = i;

                    y = entryRect.yMax;
                }

                // 삭제 처리 (루프 밖에서 수행)
                if (removeIndex >= 0)
                {
                    keys.DeleteArrayElementAtIndex(removeIndex);
                    values.DeleteArrayElementAtIndex(removeIndex);
                }
            }

            // ── + Add 버튼 ──
            y += Spacing;
            var addRect = new Rect(position.x, y, position.width, AddButtonHeight);
            if (GUI.Button(addRect, "+"))
            {
                int newIndex = keys.arraySize;
                keys.InsertArrayElementAtIndex(newIndex);
                values.InsertArrayElementAtIndex(newIndex);
            }
            y = addRect.yMax;

            // ── 중복 키 경고 ──
            if (duplicateIndices.Count > 0)
            {
                y += Spacing;
                var warnRect = new Rect(position.x, y, position.width, WarningBoxHeight);
                EditorGUI.HelpBox(warnRect, "Duplicate key detected — only the last value will be used at runtime.", MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>한 엔트리(Key → Value 페어)를 그린다. 삭제 요청 시 true 반환.</summary>
        private bool DrawEntry(Rect rect, SerializedProperty keys, SerializedProperty values, int index, bool isDuplicate)
        {
            var keyProp = keys.GetArrayElementAtIndex(index);
            var valueProp = values.GetArrayElementAtIndex(index);

            // 중복 키 배경 강조
            if (isDuplicate)
            {
                var bgRect = new Rect(rect.x - 2, rect.y - 1, rect.width + 4, rect.height + 2);
                EditorGUI.DrawRect(bgRect, new Color(0.7f, 0.45f, 0.05f, 0.15f));
            }

            float x = rect.x;
            float lineHeight = EditorGUIUtility.singleLineHeight;

            // Value가 복합 타입인지 판별
            bool isComplexValue = IsComplexProperty(valueProp);

            if (!isComplexValue)
            {
                // ── 단순 값: 한 줄 레이아웃 ──
                // [K] [key field] [→] [V] [value field] [✕]
                float fieldWidth = (rect.width - LabelWidth * 2 - ArrowWidth - RemoveButtonWidth - Spacing * 5) / 2f;

                // K 라벨
                var kLabelRect = new Rect(x, rect.y, LabelWidth, lineHeight);
                GUI.Label(kLabelRect, "K", isDuplicate ? WarningKeyLabelStyle() : _keyLabelStyle);
                x = kLabelRect.xMax + Spacing;

                // Key 필드
                var keyRect = new Rect(x, rect.y, fieldWidth, lineHeight);
                EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
                x = keyRect.xMax + Spacing;

                // → 화살표
                var arrowRect = new Rect(x, rect.y, ArrowWidth, lineHeight);
                GUI.Label(arrowRect, "\u2192", _arrowStyle);
                x = arrowRect.xMax + Spacing;

                // V 라벨
                var vLabelRect = new Rect(x, rect.y, LabelWidth, lineHeight);
                GUI.Label(vLabelRect, "V", _valueLabelStyle);
                x = vLabelRect.xMax + Spacing;

                // Value 필드
                var valueRect = new Rect(x, rect.y, fieldWidth, lineHeight);
                EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
                x = valueRect.xMax + Spacing;
            }
            else
            {
                // ── 복합 값: 키 한 줄 + 값 펼침 ──
                float keyFieldWidth = rect.width - LabelWidth - RemoveButtonWidth - Spacing * 3;

                // 첫 줄: K 라벨 + Key 필드
                var kLabelRect = new Rect(x, rect.y, LabelWidth, lineHeight);
                GUI.Label(kLabelRect, "K", isDuplicate ? WarningKeyLabelStyle() : _keyLabelStyle);

                var keyRect = new Rect(kLabelRect.xMax + Spacing, rect.y, keyFieldWidth, lineHeight);
                EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);

                // 값 영역: 들여쓰기 후 PropertyField
                float indent = 20f;
                float valueY = rect.y + lineHeight + Spacing;
                float valueHeight = EditorGUI.GetPropertyHeight(valueProp, true) ;
                var valueRect = new Rect(rect.x + indent, valueY, rect.width - indent - RemoveButtonWidth - Spacing, valueHeight);

                // V 라벨
                var vHeaderRect = new Rect(rect.x + indent, valueY, LabelWidth, lineHeight);
                GUI.Label(vHeaderRect, "V", _valueLabelStyle);

                var valuePropRect = new Rect(vHeaderRect.xMax + Spacing, valueY, valueRect.width - LabelWidth - Spacing, valueHeight);
                EditorGUI.PropertyField(valuePropRect, valueProp, new GUIContent(valueProp.displayName), true);

                x = rect.x + rect.width - RemoveButtonWidth;
            }

            // ✕ 삭제 버튼
            var removeRect = new Rect(rect.x + rect.width - RemoveButtonWidth, rect.y, RemoveButtonWidth, lineHeight);
            bool remove = GUI.Button(removeRect, "\u2715", _removeButtonStyle);

            return remove;
        }

        /// <summary>엔트리 하나의 높이를 계산한다.</summary>
        private float GetEntryHeight(SerializedProperty keys, SerializedProperty values, int index)
        {
            if (index >= values.arraySize) return EditorGUIUtility.singleLineHeight;

            var valueProp = values.GetArrayElementAtIndex(index);

            if (!IsComplexProperty(valueProp))
                return EditorGUIUtility.singleLineHeight;

            // 복합 값: 키 한 줄 + 값 높이
            float valueHeight = EditorGUI.GetPropertyHeight(valueProp, true);
            return EditorGUIUtility.singleLineHeight + Spacing + valueHeight;
        }

        /// <summary>Value가 복합 타입(자식 프로퍼티가 있는)인지 판별한다.</summary>
        private static bool IsComplexProperty(SerializedProperty prop)
        {
            return prop.hasVisibleChildren
                   && prop.propertyType != SerializedPropertyType.String
                   && prop.propertyType != SerializedPropertyType.ObjectReference;
        }

        /// <summary>keys 배열에서 중복 키의 인덱스를 찾는다.</summary>
        private static HashSet<int> FindDuplicateIndices(SerializedProperty keys)
        {
            var result = new HashSet<int>();
            var seen = new Dictionary<string, int>();

            for (int i = 0; i < keys.arraySize; i++)
            {
                string keyStr = GetKeyString(keys.GetArrayElementAtIndex(i));
                if (seen.TryGetValue(keyStr, out int prevIndex))
                {
                    result.Add(prevIndex);
                    result.Add(i);
                }
                else
                {
                    seen[keyStr] = i;
                }
            }

            return result;
        }

        /// <summary>중복 키가 있는지 빠르게 판별한다.</summary>
        private static bool HasDuplicateKeys(SerializedProperty keys)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < keys.arraySize; i++)
            {
                string keyStr = GetKeyString(keys.GetArrayElementAtIndex(i));
                if (!seen.Add(keyStr)) return true;
            }
            return false;
        }

        /// <summary>SerializedProperty를 문자열로 변환한다 (중복 비교용).</summary>
        private static string GetKeyString(SerializedProperty prop)
        {
            return prop.propertyType switch
            {
                SerializedPropertyType.String => prop.stringValue ?? "",
                SerializedPropertyType.Integer => prop.intValue.ToString(),
                SerializedPropertyType.Float => prop.floatValue.ToString("R"),
                SerializedPropertyType.Enum => prop.enumValueIndex.ToString(),
                SerializedPropertyType.Boolean => prop.boolValue.ToString(),
                SerializedPropertyType.ObjectReference => prop.objectReferenceInstanceIDValue.ToString(),
                _ => prop.propertyPath + "_" + prop.type
            };
        }

        /// <summary>keys/values 배열 크기를 동기화한다.</summary>
        private static void SyncArraySizes(SerializedProperty keys, SerializedProperty values)
        {
            while (values.arraySize < keys.arraySize)
                values.InsertArrayElementAtIndex(values.arraySize);
            while (keys.arraySize < values.arraySize)
                keys.InsertArrayElementAtIndex(keys.arraySize);
        }

        /// <summary>중복 키일 때 사용하는 주황색 K 라벨 스타일.</summary>
        private static GUIStyle WarningKeyLabelStyle()
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.98f, 0.75f, 0.15f) }, // #fbbf24
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };
        }
    }
}
#endif
