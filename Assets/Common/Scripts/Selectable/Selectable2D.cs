using Common.Scripts.GlobalEventBus;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Common.Scripts.Selectable
{
    public class Selectable2D : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            // 드래그 후 릴리즈 시 클릭 이벤트 무시
            float threshold = EventSystem.current != null
                ? EventSystem.current.pixelDragThreshold
                : 10;
            if ((eventData.pressPosition - eventData.position).sqrMagnitude > threshold * threshold)
                return;

            GlobalEventBus.GlobalEventBus.Publish(new OnObjectSelectedEvent(gameObject, eventData.position));
        }
    }
}
