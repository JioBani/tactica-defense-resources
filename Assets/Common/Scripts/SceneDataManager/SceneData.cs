using System;
using UnityEngine;

namespace Common.Scripts.SceneDataManager
{
    /// <summary>
    /// 씬 간 전달할 데이터를 담는 제네릭 컨테이너
    /// Inspector에서 확인 가능
    /// </summary>
    [Serializable]
    public class SceneData<T>
    {
        [SerializeField] private T _data;
        [SerializeField] private bool _hasData;

        /// <summary>
        /// 데이터 존재 여부
        /// </summary>
        public bool HasData => _hasData;

        /// <summary>
        /// 데이터 설정
        /// </summary>
        public void Set(T data)
        {
            _data = data;
            _hasData = true;
        }

        /// <summary>
        /// 데이터 가져오기
        /// </summary>
        public T Get()
        {
            return _data;
        }

        /// <summary>
        /// 데이터 가져오기 (성공 여부 반환)
        /// </summary>
        public bool TryGet(out T data)
        {
            data = _data;
            return _hasData;
        }

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Clear()
        {
            _data = default;
            _hasData = false;
        }
    }
}
