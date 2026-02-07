using System;
using Cysharp.Threading.Tasks;

namespace Common.Scripts.TaskQueue
{
    /// <summary>
    /// 람다를 IQueuedTask로 래핑하는 헬퍼 클래스
    /// </summary>
    public class QueuedTask : IQueuedTask
    {
        private readonly Func<UniTask> _task;

        public QueuedTask(Func<UniTask> task)
        {
            _task = task;
        }

        public async UniTask ExecuteAsync()
        {
            if (_task != null)
            {
                await _task();
            }
        }
    }
}
