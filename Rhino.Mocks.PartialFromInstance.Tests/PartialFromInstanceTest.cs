using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Rhino.Mocks.PartialFromInstance.Tests
{
    [TestFixture]
    public class PartialFromInstanceTest
    {
        private Foo _actual = new Foo
        {
            StringProp = "Actual",
            IntProp = 42
        };

        [Test]
        public void NormalBehavior()
        {
            // ARRANGE
            var mock = MockRepository.GenerateMock<IFoo>();

            // ACT
            mock
                .Stub(x => x.StringProp)
                .Return("Mocked");

            mock
                .Stub(x => x.IntProp)
                .Do(new Func<int>(() => 14));

            // ASSERT
            Assert.AreEqual(
                14,
                mock.IntProp);

            Assert.AreEqual(
                "Mocked",
                mock.StringProp);

            Console.Write("SUCCESS");
        }

        [Test]
        public void PartialMockTest()
        {
            // ARRANGE
            var mock = MockRepository.GenerateMock<IFoo>();

            //ACT

            //stub one method
            mock.Stub(x => x.IntProp).Do(new Func<int>(() => 25));

            //stub the rest from instance
            mock.StubFromInstance(_actual);
            
            //ASSERT
            //auto-stubbed method
            Assert.AreEqual(
                "Actual",
                mock.StringProp);

            //override stubbed method
            Assert.AreEqual(
                25,
                mock.IntProp);

            Console.Write("SUCCESS");
        }
    }

    public interface IFoo
    {
        string StringProp { get; }

        int IntProp { get; }
    }

    public class Foo : IFoo
    {
        public string StringProp { get; set; }
        public int IntProp { get; set; }
    }
}