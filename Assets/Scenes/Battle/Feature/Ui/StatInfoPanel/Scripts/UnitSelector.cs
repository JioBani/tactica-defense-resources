using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Units;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Scenes.Battle.Feature.Ui.StatInfoPanel
{
    public class UnitSelector : MonoBehaviour
    {
        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            var screenPos = Mouse.current.position.ReadValue();
            var worldPos = _cam.ScreenToWorldPoint(screenPos);
            var hit = Physics2D.Raycast(worldPos, Vector2.zero);

            if (hit.collider != null && hit.collider.TryGetComponent<Units.Unit>(out var unit))
                GlobalEventBus.Publish(new OnUnitSelectedEvent(unit));
            else
                GlobalEventBus.Publish(new OnUnitSelectedEvent(null));
        }
    }
}
