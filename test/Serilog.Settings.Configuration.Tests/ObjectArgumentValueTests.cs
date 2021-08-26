using System;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Serilog.Settings.Configuration.Tests
{
    public class ObjectArgumentValueTests
    {
        readonly IConfigurationRoot _config;

        public ObjectArgumentValueTests()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("ObjectArgumentValueTests.json")
                .Build();
        }

        [Theory]
        [InlineData("case_1", typeof(A), "new A(1, 23:59:59, http://dot.com/, \"d\")")]
        [InlineData("case_2", typeof(B), "new B(2, new A(3, new D()), null)")]
        [InlineData("case_3", typeof(E), "new E(\"1\", \"2\", \"3\")")]
        [InlineData("case_4", typeof(F), "new F(\"paramType\", new E(1, 2, 3, 4))")]
        [InlineData("case_5", typeof(G), "new G()")]
        [InlineData("case_6", typeof(G), "new G(3, 4)")]
        public void ShouldBindToConstructorArguments(string caseSection, Type targetType, string expectedExpression)
        {
            var testSection = _config.GetSection(caseSection);

            Assert.True(ObjectArgumentValue.TryBuildCtorExpression(testSection, targetType, new(), out var ctorExpression));
            Assert.Equal(expectedExpression, ctorExpression.ToString());
        }

        class A
        {
            public A(int a, TimeSpan b, Uri c, string d = "d") { }
            public A(int a, C c) { }
        }

        class B
        {
            public B(int b, A a, long? c = null) { }
        }

        interface C { }

        class D : C { }

        class E
        {
            public E(int a, int b, int c, int d = 4) { }
            public E(int a, string b, string c) { }
            public E(string a, string b, string c) { }
        }

        class F
        {
            public F(string type, E e) { }
        }

        class G
        {
            public G() { }
            public G(int a = 1, int b = 2) { }
        }
    }
}
