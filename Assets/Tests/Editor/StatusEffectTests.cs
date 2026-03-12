using System.Collections.Generic;
using Common.Scripts.StatusEffect;
using Common.Scripts.StatusEffect.HookProvider;
using NUnit.Framework;

namespace Tests.Editor
{
    public class StatusEffectTests
    {
        // ── 테스트용 구현체 ──

        private class TestStatusEffect : StatusEffect
        {
            public readonly List<string> CallLog = new();

            public override void OnApply(StatusEffectContext context)
            {
                CallLog.Add("OnApply");
            }

            public override void OnUpdate()
            {
                CallLog.Add("OnUpdate");
            }

            public override void OnRemove()
            {
                CallLog.Add("OnRemove");
            }

            public void ExposeRequestRemove() => RequestRemove();
        }

        private class TestHookProvider : IStatusEffectHookProvider
        {
            public readonly List<StatusEffect> Added = new();
            public readonly List<StatusEffect> Removed = new();
            public bool Disposed;

            public void OnStatusEffectAdded(StatusEffect statusEffect)
            {
                Added.Add(statusEffect);
            }

            public void OnStatusEffectRemoved(StatusEffect statusEffect)
            {
                Removed.Add(statusEffect);
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        // ── SE 생명주기 테스트 ──

        [Test]
        public void OnApply_CalledOnApply()
        {
            var effect = new TestStatusEffect();
            var context = new StatusEffectContext();

            effect.OnApply(context);

            Assert.AreEqual(new List<string> { "OnApply" }, effect.CallLog);
        }

        [Test]
        public void OnUpdate_CalledOnUpdate()
        {
            var effect = new TestStatusEffect();

            effect.OnUpdate();

            Assert.AreEqual(new List<string> { "OnUpdate" }, effect.CallLog);
        }

        [Test]
        public void OnRemove_CalledOnRemove()
        {
            var effect = new TestStatusEffect();

            effect.OnRemove();

            Assert.AreEqual(new List<string> { "OnRemove" }, effect.CallLog);
        }

        // ── RequestRemove 테스트 ──

        [Test]
        public void RequestRemove_SetsIsExpiredTrue()
        {
            var effect = new TestStatusEffect();

            Assert.IsFalse(effect.IsExpired);
            effect.ExposeRequestRemove();
            Assert.IsTrue(effect.IsExpired);
        }

        [Test]
        public void IsExpired_DefaultFalse()
        {
            var effect = new TestStatusEffect();

            Assert.IsFalse(effect.IsExpired);
        }

        // ── HookProvider 알림 테스트 ──

        [Test]
        public void HookProvider_OnStatusEffectAdded_CalledWithCorrectEffect()
        {
            var hookProvider = new TestHookProvider();
            var effect = new TestStatusEffect();

            hookProvider.OnStatusEffectAdded(effect);

            Assert.AreEqual(1, hookProvider.Added.Count);
            Assert.AreSame(effect, hookProvider.Added[0]);
        }

        [Test]
        public void HookProvider_OnStatusEffectRemoved_CalledWithCorrectEffect()
        {
            var hookProvider = new TestHookProvider();
            var effect = new TestStatusEffect();

            hookProvider.OnStatusEffectRemoved(effect);

            Assert.AreEqual(1, hookProvider.Removed.Count);
            Assert.AreSame(effect, hookProvider.Removed[0]);
        }

        [Test]
        public void HookProvider_Dispose_SetsDisposedTrue()
        {
            var hookProvider = new TestHookProvider();

            hookProvider.Dispose();

            Assert.IsTrue(hookProvider.Disposed);
        }

        // ── StatusEffectHookProvider<T> 캐싱 테스트 ──

        private interface ITestHook : IStatusEffectHook
        {
            void OnTestEvent();
        }

        private class HookImplementingEffect : StatusEffect, ITestHook
        {
            public int EventCount;
            public void OnTestEvent() => EventCount++;
        }

        private class NonHookEffect : StatusEffect { }

        private class TestGenericHookProvider : StatusEffectHookProvider<ITestHook>
        {
            public void FireTestEvent()
            {
                foreach (var se in StatusEffects)
                    se.OnTestEvent();
            }

            public int CachedCount => StatusEffects.Count;
        }

        [Test]
        public void GenericHookProvider_CachesEffectThatImplementsHook()
        {
            var provider = new TestGenericHookProvider();
            var effect = new HookImplementingEffect();

            provider.OnStatusEffectAdded(effect);

            Assert.AreEqual(1, provider.CachedCount);
        }

        [Test]
        public void GenericHookProvider_IgnoresEffectThatDoesNotImplementHook()
        {
            var provider = new TestGenericHookProvider();
            var effect = new NonHookEffect();

            provider.OnStatusEffectAdded(effect);

            Assert.AreEqual(0, provider.CachedCount);
        }

        [Test]
        public void GenericHookProvider_RemovesCachedEffect()
        {
            var provider = new TestGenericHookProvider();
            var effect = new HookImplementingEffect();

            provider.OnStatusEffectAdded(effect);
            provider.OnStatusEffectRemoved(effect);

            Assert.AreEqual(0, provider.CachedCount);
        }

        [Test]
        public void GenericHookProvider_DispatchesToCachedEffects()
        {
            var provider = new TestGenericHookProvider();
            var effect1 = new HookImplementingEffect();
            var effect2 = new HookImplementingEffect();
            var nonHookEffect = new NonHookEffect();

            provider.OnStatusEffectAdded(effect1);
            provider.OnStatusEffectAdded(effect2);
            provider.OnStatusEffectAdded(nonHookEffect);

            provider.FireTestEvent();

            Assert.AreEqual(1, effect1.EventCount);
            Assert.AreEqual(1, effect2.EventCount);
        }

    }
}
