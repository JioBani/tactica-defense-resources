using Common.Scripts.GlobalEventBus;
using NUnit.Framework;

namespace Tests.Editor
{
    public class GlobalEventBusTests
    {
        private struct TestEvent : IGameEvent
        {
            public readonly int Value;
            public TestEvent(int value) => Value = value;
        }

        private struct OtherEvent : IGameEvent { }

        // ── Subscribe & Publish ──

        [Test]
        public void Publish_DeliversEventToSubscriber()
        {
            int received = 0;
            void Handler(TestEvent evt) => received = evt.Value;
            GlobalEventBus.Subscribe<TestEvent>(Handler);

            try
            {
                GlobalEventBus.Publish(new TestEvent(42));
                Assert.AreEqual(42, received);
            }
            finally
            {
                GlobalEventBus.Unsubscribe<TestEvent>(Handler);
            }
        }

        [Test]
        public void Publish_MultipleSubscribers_AllReceive()
        {
            int count1 = 0, count2 = 0;
            void Handler1(TestEvent evt) => count1++;
            void Handler2(TestEvent evt) => count2++;
            GlobalEventBus.Subscribe<TestEvent>(Handler1);
            GlobalEventBus.Subscribe<TestEvent>(Handler2);

            try
            {
                GlobalEventBus.Publish(new TestEvent(1));
                Assert.AreEqual(1, count1);
                Assert.AreEqual(1, count2);
            }
            finally
            {
                GlobalEventBus.Unsubscribe<TestEvent>(Handler1);
                GlobalEventBus.Unsubscribe<TestEvent>(Handler2);
            }
        }

        [Test]
        public void Publish_NoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => GlobalEventBus.Publish(new OtherEvent()));
        }

        // ── Unsubscribe ──

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int callCount = 0;
            void Handler(TestEvent evt) => callCount++;
            GlobalEventBus.Subscribe<TestEvent>(Handler);
            GlobalEventBus.Unsubscribe<TestEvent>(Handler);

            GlobalEventBus.Publish(new TestEvent(1));
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Unsubscribe_OnlyRemovesTargetHandler()
        {
            int count1 = 0, count2 = 0;
            void Handler1(TestEvent evt) => count1++;
            void Handler2(TestEvent evt) => count2++;
            GlobalEventBus.Subscribe<TestEvent>(Handler1);
            GlobalEventBus.Subscribe<TestEvent>(Handler2);
            GlobalEventBus.Unsubscribe<TestEvent>(Handler1);

            try
            {
                GlobalEventBus.Publish(new TestEvent(1));
                Assert.AreEqual(0, count1);
                Assert.AreEqual(1, count2);
            }
            finally
            {
                GlobalEventBus.Unsubscribe<TestEvent>(Handler2);
            }
        }

        [Test]
        public void Unsubscribe_NonExistentHandler_DoesNotThrow()
        {
            void Handler(TestEvent evt) { }
            Assert.DoesNotThrow(() => GlobalEventBus.Unsubscribe<TestEvent>(Handler));
        }
    }
}
