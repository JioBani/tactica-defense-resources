using System;
using UnityEngine;

namespace Scenes.Battle.Feature.CameraControl.Letterbox
{
    /// <summary>
    /// 역할: 화면 종횡비와 목표 종횡비로부터 카메라 뷰포트 Rect를 계산하는 순수 함수.
    /// Unity 씬/런타임 상태(MonoBehaviour·Transform·Time 등)에 의존하지 않으므로 EditMode 테스트로 검증 가능하다.
    /// 결과 Rect를 Camera.rect에 대입하면 목표 종횡비 영역만 중앙에 렌더되고 남는 곳은 검은 띠가 된다.
    /// </summary>
    public static class LetterboxViewport
    {
        /// <summary>
        /// 목표 종횡비 영역을 화면 중앙에 배치하는 카메라 뷰포트 Rect를 계산한다.
        /// 화면이 목표보다 덜 와이드하면 상하 띠(letterbox), 더 와이드하면 좌우 띠(pillarbox)가 된다.
        /// </summary>
        public static Rect Calculate(float targetAspect, int screenWidth, int screenHeight)
        {
            // 비정상 입력은 조용히 넘기지 않고 즉시 드러낸다(잘못된 Rect로 화면이 사라지는 디버깅 난항 방지).
            if (targetAspect <= 0f || screenWidth <= 0 || screenHeight <= 0)
            {
                throw new ArgumentException(
                    $"잘못된 입력: targetAspect={targetAspect}, screenWidth={screenWidth}, screenHeight={screenHeight}. 모두 양수여야 한다.");
            }

            float screenAspect = (float)screenWidth / screenHeight;
            // 목표 종횡비를 화면 종횡비로 나눈 비율. 1이면 정확히 일치(띠 없음).
            float heightScale = screenAspect / targetAspect;

            Rect viewport;
            if (heightScale < 1f)
            {
                // 화면이 목표보다 덜 와이드(예: 16:9) → 높이를 줄이고 상하에 띠.
                viewport = new Rect(0f, (1f - heightScale) / 2f, 1f, heightScale);
            }
            else
            {
                // 화면이 목표보다 더 와이드(예: 21:9) → 너비를 줄이고 좌우에 띠. heightScale==1 이면 전체 화면.
                float widthScale = 1f / heightScale;
                viewport = new Rect((1f - widthScale) / 2f, 0f, widthScale, 1f);
            }

            return viewport;
        }
    }
}
