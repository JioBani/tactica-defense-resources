using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Common.Scripts.Extensions
{
    public static class DOTweenExtensions
    {
        /// <summary>
        /// DOTween Tween을 UniTask로 변환
        /// </summary>
        public static async UniTask ToUniTask(this Tween tween)
        {
            if (tween == null || !tween.IsActive())
            {
                return;
            }

            var source = new UniTaskCompletionSource();

            tween.OnComplete(() => source.TrySetResult());
            tween.OnKill(() => source.TrySetCanceled());

            await source.Task;
        }
    }
}
