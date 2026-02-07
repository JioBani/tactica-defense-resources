using Cysharp.Threading.Tasks;

namespace Common.Scripts.TaskQueue
{
    /// <summary>
    /// 큐에 추가할 태스크 인터페이스
    /// </summary>
    public interface IQueuedTask
    {
        /// <summary>
        /// 태스크 실행. UniTask가 완료되면 다음 태스크로 진행
        /// </summary>
        UniTask ExecuteAsync();
    }
}
