using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.Scripts.SerializableDictionary
{
    /// <summary>커스텀 PropertyDrawer 타겟용 비제네릭 베이스 클래스.</summary>
    public abstract class SerializableDictionaryBase { }

    /// <summary>
    /// Unity Inspector에서 직렬화 가능한 Dictionary.
    /// ISerializationCallbackReceiver를 통해 키-값 리스트를 Dictionary로 변환한다.
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : SerializableDictionaryBase, ISerializationCallbackReceiver
    {
        [SerializeField] private TKey[] keys = Array.Empty<TKey>();
        [SerializeField] private TValue[] values = Array.Empty<TValue>();

        private Dictionary<TKey, TValue> _dict = new();

        /// <summary>키로 값을 조회하거나 설정한다.</summary>
        public TValue this[TKey key]
        {
            get => _dict[key];
            set => _dict[key] = value;
        }

        /// <summary>키로 값을 조회한다. 키가 없으면 false를 반환한다.</summary>
        public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

        /// <summary>키가 존재하는지 확인한다.</summary>
        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

        /// <summary>항목 수를 반환한다.</summary>
        public int Count => _dict.Count;

        /// <summary>모든 항목을 제거한다.</summary>
        public void Clear() => _dict.Clear();

        public void OnBeforeSerialize()
        {
            keys = new TKey[_dict.Count];
            values = new TValue[_dict.Count];
            int i = 0;
            foreach (var kvp in _dict)
            {
                keys[i] = kvp.Key;
                values[i] = kvp.Value;
                i++;
            }
        }

        public void OnAfterDeserialize()
        {
            _dict = new Dictionary<TKey, TValue>();
            int count = Mathf.Min(keys.Length, values.Length);
            for (int i = 0; i < count; i++)
            {
                if (keys[i] != null)
                    _dict[keys[i]] = values[i];
            }
        }
    }
}
