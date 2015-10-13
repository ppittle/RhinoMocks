using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Rhino.Mocks.PartialFromInstance.Tests
{
    [TestFixture]
    public class PartialFromInstanceTest
    {
        [Test]
        public void PartialMockTest()
        {
            // ARRANGE
            var actual = new Foo
            {
                StringProp = "Actual",
                IntProp = 42
            };

            

            //ACT
            var mock2 = MockRepository.GenerateMock<IFoo>();

            //stub from actual
            mock2.StubFromInstance(actual);

            //stub one method
            //mock2.Stub(x => x.IntProp).Do(new Func<int>(() => 25));
            
            #region Ignore
            var mock = MockRepository.GenerateMock<IFoo>();
            var stub = mock.Stub(x => x.StringProp);

            stub.Return("Mocked");

            mock
                .Stub(x => x.IntProp)
                .Do(new Func<int>(() => actual.IntProp));
            #endregion

            //ASSERT

            //auto-stubbed method
            Assert.AreEqual(
                "Actual",
                mock2.StringProp);

            //override stubbed method
            Assert.AreEqual(
                42,
                mock2.IntProp);

            /*Assert.AreEqual(
                42,
                mock.IntProp);

            Assert.AreEqual(
                "Mocked",
                mock.StringProp);
            */

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