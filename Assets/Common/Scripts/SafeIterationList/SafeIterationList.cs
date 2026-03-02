// ─────────────────────────────────────────────
// SafeIterationList: foreach 순회 중 Add/Remove를 버퍼링하여
// 컬렉션 수정 예외를 방지하는 범용 리스트.
// 순회가 끝나면 대기 중인 변경 사항을 일괄 반영한다.
// ─────────────────────────────────────────────
using System.Collections;
using System.Collections.Generic;

namespace Common.Scripts.SafeIterationList
{
    /// <summary>
    /// foreach 순회 중 Add/Remove 호출을 버퍼링하여 컬렉션 수정 예외를 방지하는 리스트.
    /// 순회가 끝나면 대기 중인 추가/제거를 일괄 반영한다.
    /// 중첩 순회도 _iterationDepth로 안전하게 처리한다.
    /// </summary>
    public class SafeIterationList<T> : IEnumerable<T>
    {
        private readonly List<T> _items = new();
        private readonly List<T> _pendingAdds = new();
        private readonly List<T> _pendingRemoves = new();

        /// <summary>현재 진행 중인 순회 깊이. 0이면 순회 중이 아니다.</summary>
        private int _iterationDepth;

        /// <summary>
        /// 항목을 추가한다. 순회 중이면 순회 종료 후 반영된다.
        /// </summary>
        public void Add(T item)
        {
            if (_iterationDepth > 0)
                _pendingAdds.Add(item);
            else
                _items.Add(item);
        }

        /// <summary>
        /// 항목을 제거한다. 순회 중이면 순회 종료 후 반영된다.
        /// </summary>
        public bool Remove(T item)
        {
            if (_iterationDepth > 0)
            {
                _pendingRemoves.Add(item);
                return true;
            }

            return _items.Remove(item);
        }

        /// <summary>항목 포함 여부를 반환한다.</summary>
        public bool Contains(T item) => _items.Contains(item);

        /// <summary>
        /// 대기 중인 추가/제거를 일괄 반영하고 버퍼를 비운다.
        /// </summary>
        private void FlushPending()
        {
            foreach (var item in _pendingAdds)
                _items.Add(item);

            foreach (var item in _pendingRemoves)
                _items.Remove(item);

            _pendingAdds.Clear();
            _pendingRemoves.Clear();
        }

        public Enumerator GetEnumerator() => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 순회 시작 시 iterationDepth를 증가시키고, Dispose 시 감소 + flush하는 커스텀 열거자.
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly SafeIterationList<T> _list;
            private List<T>.Enumerator _inner;

            internal Enumerator(SafeIterationList<T> list)
            {
                _list = list;
                _list._iterationDepth++;
                _inner = list._items.GetEnumerator();
            }

            public T Current => _inner.Current;

            object IEnumerator.Current => Current;

            public bool MoveNext() => _inner.MoveNext();

            public void Dispose()
            {
                _inner.Dispose();
                _list._iterationDepth--;

                if (_list._iterationDepth == 0)
                    _list.FlushPending();
            }

            public void Reset()
            {
            }
        }
    }
}