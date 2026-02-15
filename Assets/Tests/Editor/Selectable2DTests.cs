using Common.Scripts.GlobalEventBus;
using Common.Scripts.Selectable;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tests.Editor
{
    public class Selectable2DTests
    {
        private GameObject _go;
        private Selectable2D _selectable;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestSelectable");
            _selectable = _go.AddComponent<Selectable2D>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        // ── Left click ──

        [Test]
        public void LeftClick_PublishesEventWithCorrectGameObject()
        {
            OnObjectSelectedEvent? received = null;
            void Handler(OnObjectSelectedEvent evt) => received = evt;
            GlobalEventBus.Subscribe<OnObjectSelectedEvent>(Handler);

            try
            {
                var eventData = new PointerEventData(null)
                    { button = PointerEventData.InputButton.Left };
                _selectable.OnPointerClick(eventData);

                Assert.IsTrue(received.HasValue, "Event should be published");
                Assert.AreEqual(_go, received.Value.SelectedObject);
            }
            finally
            {
                GlobalEventBus.Unsubscribe<OnObjectSelectedEvent>(Handler);
            }
        }

        [Test]
        public void LeftClick_PublishesEventWithScreenPosition()
        {
            OnObjectSelectedEvent? received = null;
            void Handler(OnObjectSelectedEvent evt) => received = evt;
            GlobalEventBus.Subscribe<OnObjectSelectedEvent>(Handler);

            try
            {
                var clickPos = new Vector2(200f, 150f);
                var eventData = new PointerEventData(null)
                    { button = PointerEventData.InputButton.Left, pressPosition = clickPos, position = clickPos };
                _selectable.OnPointerClick(eventData);

                Assert.IsTrue(received.HasValue);
                Assert.AreEqual(clickPos, received.Value.ScreenPosition);
            }
            finally
            {
                GlobalEventBus.Unsubscribe<OnObjectSelectedEvent>(Handler);
            }
        }

        // ── Non-left clicks ──

        [Test]
        public void RightClick_DoesNotPublishEvent()
        {
            bool received = false;
            void Handler(OnObjectSelectedEvent evt) => received = true;
            GlobalEventBus.Subscribe<OnObjectSelectedEvent>(Handler);

            try
            {
                var eventData = new PointerEventData(null)
                    { button = PointerEventData.InputButton.Right };
                _selectable.OnPointerClick(eventData);

                Assert.IsFalse(received);
            }
            finally
            {
                GlobalEventBus.Unsubscribe<OnObjectSelectedEvent>(Handler);
            }
        }

        [Test]
        public void MiddleClick_DoesNotPublishEvent()
        {
            bool received = false;
            void Handler(OnObjectSelectedEvent evt) => received = true;
            GlobalEventBus.Subscribe<OnObjectSelectedEvent>(Handler);

            try
            {
                var eventData = new PointerEventData(null)
                    { button = PointerEventData.InputButton.Middle };
                _selectable.OnPointerClick(eventData);

                Assert.IsFalse(received);
            }
            finally
            {
                GlobalEventBus.Unsubscribe<OnObjectSelectedEvent>(Handler);
            }
        }

        // ── Drag guard ──

        [Test]
        public void DragThenRelease_DoesNotPublishEvent()
        {
            bool received = false;
            void Handler(OnObjectSelectedEvent evt) => received = true;
            GlobalEventBus.Subscribe<OnObjectSelectedEvent>(Handler);

            try
            {
                // pressPosition과 position 차이가 임계값(10px)을 초과
                var eventData = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left,
                    pressPosition = new Vector2(100f, 100f),
                    position = new Vector2(200f, 200f)
                };
                _selectable.OnPointerClick(eventData);

                Assert.IsFalse(received, "Event should not be published after drag");
            }
            finally
            {
                GlobalEventBus.Unsubscribe<OnObjectSelectedEvent>(Handler);
            }
        }

        [Test]
        public void SmallMovement_StillPublishesEvent()
        {
            OnObjectSelectedEvent? received = null;
            void Handler(OnObjectSelectedEvent evt) => received = evt;
            GlobalEventBus.Subscribe<OnObjectSelectedEvent>(Handler);

            try
            {
                // pressPosition과 position 차이가 임계값(10px) 이내
                var eventData = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left,
                    pressPosition = new Vector2(100f, 100f),
                    position = new Vector2(105f, 103f)
                };
                _selectable.OnPointerClick(eventData);

                Assert.IsTrue(received.HasValue, "Event should be published for small movement");
                Assert.AreEqual(_go, received.Value.SelectedObject);
            }
            finally
            {
                GlobalEventBus.Unsubscribe<OnObjectSelectedEvent>(Handler);
            }
        }

        // ── Multiple instances ──

        [Test]
        public void MultipleSelectables_EachPublishOwnGameObject()
        {
            var go2 = new GameObject("TestSelectable2");
            var selectable2 = go2.AddComponent<Selectable2D>();

            GameObject lastReceived = null;
            void Handler(OnObjectSelectedEvent evt) => lastReceived = evt.SelectedObject;
            GlobalEventBus.Subscribe<OnObjectSelectedEvent>(Handler);

            try
            {
                var eventData = new PointerEventData(null)
                    { button = PointerEventData.InputButton.Left };

                _selectable.OnPointerClick(eventData);
                Assert.AreEqual(_go, lastReceived);

                selectable2.OnPointerClick(eventData);
                Assert.AreEqual(go2, lastReceived);
            }
            finally
            {
                GlobalEventBus.Unsubscribe<OnObjectSelectedEvent>(Handler);
                Object.DestroyImmediate(go2);
            }
        }
    }
}
