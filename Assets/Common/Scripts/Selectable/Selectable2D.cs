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
            GlobalEventBus.GlobalEventBus.Publish(new OnObjectSelectedEvent(gameObject, eventData.position));
        }
    }
}
