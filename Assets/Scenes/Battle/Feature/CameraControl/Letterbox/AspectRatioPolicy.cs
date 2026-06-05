namespace Scenes.Battle.Feature.CameraControl.Letterbox
{
    /// <summary>
    /// 역할: 게임의 기준 표시 종횡비(레터박스 기준) 정책을 한 곳에서 관리하는 상수 모음.
    /// 게임 콘텐츠가 아니라 단일 표시 정책이므로 ScriptableObject 대신 상수로 둔다.
    /// 종횡비를 바꿀 일이 생기면 이 한 곳만 수정한다(매직넘버 분산 방지).
    /// </summary>
    public static class AspectRatioPolicy
    {
        // 기준 픽셀 도면값(2400x1080 = 20:9). 기존 800x360의 정확히 3배라 좌표 호환이 유지된다.
        public const int ReferenceWidth = 2400;
        public const int ReferenceHeight = 1080;

        // 기준 종횡비(width / height ≈ 2.2222). 레터박스 계산·CanvasScaler 기준으로 공유한다.
        public const float TargetAspect = (float)ReferenceWidth / ReferenceHeight;
    }
}
