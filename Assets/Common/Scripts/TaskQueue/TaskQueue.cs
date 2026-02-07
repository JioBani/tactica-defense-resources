using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Common.Scripts.TaskQueue
{
    /// <summary>
    /// 인스턴스 기반 태스크 큐
    /// 태스크들이 순차적으로 실행됨
    /// </summary>
    public class TaskQueue
    {
        private readonly Queue<IQueuedTask> _queue = new();
        private bool _isRunning;

        /// <summary>
        /// 태스크를 큐에 추가하고, 실행 중이 아니면 즉시 실행
        /// </summary>
        public void Enqueue(IQueuedTask task)
        {
            _queue.Enqueue(task);

            if (!_isRunning)
            {
                ExecuteNextAsync().Forget();
            }
        }

        /// <summary>
        /// 모든 대기 중인 태스크 제거
        /// </summary>
        public void Clear()
        {
            _queue.Clear();
            _isRunning = false;
        }

        private async UniTaskVoid ExecuteNextAsync()
        {
            while (_queue.Count > 0)
            {
                _isRunning = true;
                var task = _queue.Dequeue();
                await task.ExecuteAsync();
            }

            _isRunning = false;
        }
    }
}
