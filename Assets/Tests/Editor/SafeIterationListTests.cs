using System.Collections.Generic;
using Common.Scripts.SafeIterationList;
using NUnit.Framework;

namespace Tests.Editor
{
    public class SafeIterationListTests
    {
        // ── 기본 동작 ──

        [Test]
        public void Add_OutsideIteration_ImmediatelyAvailable()
        {
            var list = new SafeIterationList<int>();
            list.Add(1);
            list.Add(2);

            var result = new List<int>();
            foreach (var item in list)
                result.Add(item);

            Assert.AreEqual(new List<int> { 1, 2 }, result);
        }

        [Test]
        public void Remove_OutsideIteration_ImmediatelyRemoved()
        {
            var list = new SafeIterationList<int>();
            list.Add(1);
            list.Add(2);
            list.Remove(1);

            var result = new List<int>();
            foreach (var item in list)
                result.Add(item);

            Assert.AreEqual(new List<int> { 2 }, result);
        }

        [Test]
        public void Contains_ReturnsCorrectResult()
        {
            var list = new SafeIterationList<int>();
            list.Add(42);

            Assert.IsTrue(list.Contains(42));
            Assert.IsFalse(list.Contains(99));
        }

        // ── 순회 중 Add ──

        [Test]
        public void Add_DuringIteration_DeferredUntilIterationEnds()
        {
            var list = new SafeIterationList<int>();
            list.Add(1);
            list.Add(2);

            var result = new List<int>();
            foreach (var item in list)
            {
                result.Add(item);
                if (item == 1)
                    list.Add(3); // 순회 중 추가 — 이번 순회에는 안 나온다
            }

            Assert.AreEqual(new List<int> { 1, 2 }, result, "순회 중 추가된 항목은 현재 순회에 포함되지 않아야 한다");

            // 순회 종료 후 flush 되었으므로 다음 순회에서 나온다
            var afterFlush = new List<int>();
            foreach (var item in list)
                afterFlush.Add(item);

            Assert.AreEqual(new List<int> { 1, 2, 3 }, afterFlush);
        }

        // ── 순회 중 Remove ──

        [Test]
        public void Remove_DuringIteration_DeferredUntilIterationEnds()
        {
            var list = new SafeIterationList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            var result = new List<int>();
            foreach (var item in list)
            {
                result.Add(item);
                if (item == 1)
                    list.Remove(2); // 순회 중 제거 — 이번 순회에는 여전히 나온다
            }

            Assert.AreEqual(new List<int> { 1, 2, 3 }, result, "순회 중 제거된 항목은 현재 순회에서 여전히 보여야 한다");

            // 순회 종료 후 flush 되었으므로 다음 순회에서 사라진다
            var afterFlush = new List<int>();
            foreach (var item in list)
                afterFlush.Add(item);

            Assert.AreEqual(new List<int> { 1, 3 }, afterFlush);
        }

        // ── 중첩 순회 ──

        [Test]
        public void NestedIteration_FlushesOnlyAfterOutermostEnds()
        {
            var list = new SafeIterationList<int>();
            list.Add(1);
            list.Add(2);

            var outerResult = new List<int>();
            foreach (var outer in list)
            {
                outerResult.Add(outer);

                // 내부 순회
                foreach (var inner in list)
                {
                    if (inner == 1)
                        list.Add(99); // 중첩 순회 중 추가
                }
            }

            // 바깥 순회가 끝나기 전까지 flush 되지 않으므로, outer는 {1, 2}만 본다
            Assert.AreEqual(new List<int> { 1, 2 }, outerResult);

            // 바깥 순회 종료 후 flush
            var afterFlush = new List<int>();
            foreach (var item in list)
                afterFlush.Add(item);

            Assert.IsTrue(afterFlush.Contains(99), "중첩 순회 종료 후 추가된 항목이 반영되어야 한다");
        }

        // ── 예외 안전성 ──

        [Test]
        public void Iteration_DoesNotThrow_WhenAddAndRemoveDuringForeach()
        {
            var list = new SafeIterationList<string>();
            list.Add("a");
            list.Add("b");
            list.Add("c");

            Assert.DoesNotThrow(() =>
            {
                foreach (var item in list)
                {
                    list.Add("new");
                    list.Remove("b");
                }
            });
        }
    }
}
