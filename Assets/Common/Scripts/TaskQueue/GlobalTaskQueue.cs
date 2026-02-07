using System.Collections.Generic;

namespace Common.Scripts.TaskQueue
{
    /// <summary>
    /// 채널별 전역 태스크 큐를 관리하는 Static 클래스
    /// </summary>
    public static class GlobalTaskQueue
    {
        private static readonly Dictionary<TaskQueueChannel, TaskQueue> _queues = new();

        /// <summary>
        /// 태스크를 채널의 큐에 추가
        /// </summary>
        public static void Enqueue(TaskQueueChannel channel, IQueuedTask task)
        {
            GetOrCreate(channel).Enqueue(task);
        }

        /// <summary>
        /// 채널의 모든 대기 중인 태스크 제거
        /// </summary>
        public static void Clear(TaskQueueChannel channel)
        {
            if (_queues.TryGetValue(channel, out var queue))
            {
                queue.Clear();
            }
        }

        /// <summary>
        /// 모든 채널 초기화
        /// </summary>
        public static void ClearAll()
        {
            foreach (var queue in _queues.Values)
            {
                queue.Clear();
            }
            _queues.Clear();
        }

        private static TaskQueue GetOrCreate(TaskQueueChannel channel)
        {
            if (!_queues.TryGetValue(channel, out var queue))
            {
                queue = new TaskQueue();
                _queues[channel] = queue;
            }
            return queue;
        }
    }
}
