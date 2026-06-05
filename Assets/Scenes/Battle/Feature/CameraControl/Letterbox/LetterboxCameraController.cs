using UnityEngine;

namespace Scenes.Battle.Feature.CameraControl.Letterbox
{
    /// <summary>
    /// 역할: 부착된 카메라의 뷰포트를 기준 종횡비(20:9)로 고정하는 얇은 어댑터.
    /// 종횡비 계산은 LetterboxViewport(순수 함수)에 위임하고, 이 클래스는
    /// 화면 크기 변동 감지와 Camera.rect 적용만 담당한다(단일 책임).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class LetterboxCameraController : MonoBehaviour
    {
        private Camera _camera;
        private int _appliedWidth;
        private int _appliedHeight;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            ApplyViewport();
        }

        private void Update()
        {
            // 화면 회전·창 크기 변경으로 해상도가 바뀌면 재계산한다. 변동이 없으면 적용을 건너뛴다.
            bool screenChanged = Screen.width != _appliedWidth || Screen.height != _appliedHeight;
            if (screenChanged)
            {
                ApplyViewport();
            }
        }

        private void ApplyViewport()
        {
            Rect viewport = LetterboxViewport.Calculate(AspectRatioPolicy.TargetAspect, Screen.width, Screen.height);
            _camera.rect = viewport;
            _appliedWidth = Screen.width;
            _appliedHeight = Screen.height;
        }
    }
}
