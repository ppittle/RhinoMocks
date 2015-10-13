using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Rhino.Mocks.Impl;

namespace Rhino.Mocks.PartialFromInstance.Tests
{
    public static class MockExtensions
    {
        public static T StubFromInstance<T>(this T mock, T instance)
            where T : class
        {
            if (!typeof (T).IsInterface)
                throw new ArgumentException("This only works with interfaces at the moment.");


            foreach (var interfaceMember in typeof (T).GetMembers())
            {
                MockMember(instance, mock, interfaceMember);
            }

            return mock;
        }

        private static void MockMember<T>(T instance, T mock, MemberInfo member)
            where T : class
        {
            if (member.MemberType != MemberTypes.Property)
                return;

            var propInfo = member as PropertyInfo;

            var getMethod = propInfo.GetGetMethod();

            Type returnType = getMethod.ReturnType;

            var param = Expression.Parameter(typeof (T), "x");

            var setupExpression =
                Expression.Lambda(
                    typeof (Rhino.Mocks.Function<,>).MakeGenericType(
                        typeof (T),
                        returnType),
                    Expression.Property(
                        param,
                        getMethod),
                    new List<ParameterExpression>
                    {
                        param
                    });

            var setupMethod =
                typeof (RhinoMocksExtensions)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == "Stub")
                    .ElementAt(1)
                    .MakeGenericMethod(
                        typeof (T), returnType);

            var response =
                setupMethod
                    .Invoke(
                        null,
                        BindingFlags.NonPublic | BindingFlags.Static,
                        null,
                        new object[] {mock, setupExpression.Compile()},
                        CultureInfo.CurrentCulture);

            var returnsMethod =
                typeof (MethodOptions<>)
                    .MakeGenericType(returnType)
                    //backing store for Do method
                    .GetMethod("Do");

            var returnFunction =
                Expression.Lambda(
                    typeof (Func<>)
                        .MakeGenericType(returnType),
                    Expression.Convert(
                        Expression.Call(
                            Expression.Constant(
                                instance),
                            getMethod),
                        returnType),
                    new ParameterExpression[] {})
                    .Compile();

            returnsMethod
                .Invoke(
                    response,
                    new object[]
                    {
                        DelegateWrapper.Create(returnFunction)
                    });
        }
    }

    /// <summary>
    /// <see cref="AbstractExpectation.AssertDelegateArgumentsMatchMethod"/>
    /// expects that the<see cref="Delegate"/> passed into <see cref="MethodOptions{T}.Do"/> 
    /// has the same number of <see cref="MethodBase.GetParameters"/> as the actual
    /// method / property we are mocking.  
    /// 
    /// However, a <see cref="LambdaExpression.Compile()"/> returns a <see cref="Delegate"/>
    /// that has a hidden Parameter, which will throw off this check: 
    /// http://stackoverflow.com/questions/7935306/compiling-a-lambda-expression-results-in-delegate-with-closure-argument 
    /// 
    /// This class creates a wrapper around <see cref="LambdaExpression.Compile()"/> that
    /// <see cref="AbstractExpectation.AssertDelegateArgumentsMatchMethod"/> is happy with.
    /// </summary>
    internal abstract class DelegateWrapper
    {
        internal abstract Delegate InvokeDelegate { get; }

        public static Delegate Create(Delegate originalDelegate)
        {
            Type returnType = originalDelegate.Method.ReturnType;
            var wrapperType = typeof(DelegateWrapper<>).MakeGenericType(returnType);
            var wrapper = Activator.CreateInstance(wrapperType, originalDelegate);
            return ((DelegateWrapper)wrapper).InvokeDelegate;
        }
    }

    internal sealed class DelegateWrapper<T> : DelegateWrapper
    {
        private readonly Delegate _originalDelegate;

        public DelegateWrapper(Delegate originalDelegate)
        {
            _originalDelegate = originalDelegate;
        }

        private T Invoke()
        {
            return (T)_originalDelegate.DynamicInvoke();
        }

        internal override Delegate InvokeDelegate {
            get
            {
                return new Func<T>(Invoke);
            }
        }
    }

}
