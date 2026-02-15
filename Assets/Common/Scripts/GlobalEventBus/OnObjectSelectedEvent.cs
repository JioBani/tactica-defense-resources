using UnityEngine;

namespace Common.Scripts.GlobalEventBus
{
    public struct OnObjectSelectedEvent : IGameEvent
    {
        public readonly GameObject SelectedObject;
        public readonly Vector2 ScreenPosition;

        public OnObjectSelectedEvent(GameObject selectedObject, Vector2 screenPosition)
        {
            SelectedObject = selectedObject;
            ScreenPosition = screenPosition;
        }
    }
}
