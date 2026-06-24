using System;
using Loqui;
using NUnit.Framework;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationEventTests
    {
        [Test]
        public void Raise_InvokesEveryListener()
        {
            var evt = new LocalizationEvent();
            var a = 0;
            var b = 0;
            evt.Add(() => a++);
            evt.Add(() => b++);

            evt.Raise();

            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
        }

        [Test]
        public void Add_IgnoresDuplicateListener()
        {
            var evt = new LocalizationEvent();
            var count = 0;
            Action listener = () => count++;
            evt.Add(listener);
            evt.Add(listener);

            Assert.AreEqual(1, evt.Count);
            evt.Raise();
            Assert.AreEqual(1, count);
        }

        [Test]
        public void Remove_StopsDelivery()
        {
            var evt = new LocalizationEvent();
            var count = 0;
            Action listener = () => count++;
            evt.Add(listener);
            evt.Remove(listener);

            evt.Raise();

            Assert.AreEqual(0, evt.Count);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void Raise_ThrowingListener_DoesNotBlockOthers()
        {
            var evt = new LocalizationEvent();
            var reached = false;
            evt.Add(() => throw new InvalidOperationException("boom"));
            evt.Add(() => reached = true);

            Assert.DoesNotThrow(() => evt.Raise());
            Assert.IsTrue(reached);
        }

        [Test]
        public void Raise_ListenerUnsubscribingDuringRaise_IsSafe()
        {
            var evt = new LocalizationEvent();
            var secondCalls = 0;
            Action second = () => secondCalls++;
            Action first = null;
            first = () => evt.Remove(first);
            evt.Add(first);
            evt.Add(second);

            Assert.DoesNotThrow(() => evt.Raise());
            Assert.AreEqual(1, secondCalls);

            evt.Raise();
            Assert.AreEqual(2, secondCalls);
            Assert.AreEqual(1, evt.Count);
        }
    }
}
