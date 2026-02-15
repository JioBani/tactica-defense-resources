using UnityEngine;

namespace Common.Scripts.GlobalEventBus
{
    public struct OnObjectSelectedEvent : IGameEvent
    {
        public readonly GameObject SelectedObject;

        public OnObjectSelectedEvent(GameObject selectedObject)
        {
            SelectedObject = selectedObject;
        }
    }
}
