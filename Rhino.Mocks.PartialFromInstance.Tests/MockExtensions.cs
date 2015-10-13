using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Rhino.Mocks.Expectations;
using Rhino.Mocks.Impl;
using Rhino.Mocks.Impl.Invocation.Specifications;
using Rhino.Mocks.Interfaces;
using Rhino.Mocks.Utilities;

namespace Rhino.Mocks.PartialFromInstance.Tests
{
    public static class MockExtensions
    {
        public static T StubFromInstance<T>(this T mock, T instance)
            where T : class
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("This only works with interfaces at the moment.");


            foreach (var interfaceMember in typeof(T).GetMembers())
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
                    typeof(Func<>)
                        .MakeGenericType(returnType),
                    Expression.Convert(
                        Expression.Call(
                            Expression.Constant(
                                instance),
                            getMethod),
                        returnType),
                    new ParameterExpression[] {})
                .Compile();

            var example = new Func<string>(() => "Hello World");


            /*var simple =
                Expression.Lambda(
                    typeof(Func<>)
                        .MakeGenericType(returnType),
                    Expression.Constant("Hello World"))
                .Compile();*/


            Expression<Func<string>> exampleExpression = () => "Hello World";

            var returnFuncParams = returnFunction.Method.GetParameters();
            //var simpleParams = simple.Method.GetParameters();

            var exampleParams = example.Method.GetParameters();
            var exampleExpressionParams = exampleExpression.Compile().Method.GetParameters();

            var methodParams = getMethod.GetParameters();
            
            returnsMethod
                .Invoke(
                    response,
                    new object[]
                    {
                        Activator.CreateInstance(
                            typeof(Func<>).MakeGenericType(returnType),
                            new []
                            {
                                returnFunction})
                            });
        }

        /// <summary>
        /// Assign the <paramref name="returnDelegate"/> to
        /// <see cref="MethodOptions{T}.expectation"/>'s 
        /// <see cref="AbstractExpectation.actionToExecute"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="methodOption"></param>
        /// <param name="returnDelegate"></param>
        private static void UpdateMethodOption<T>(MethodOptions<T> methodOption, Delegate returnDelegate)
        {
            var expectationField =
                methodOption.GetType()
                    .GetField("expectation");

            var expectation = expectationField.GetValue(methodOption);

            var actionToExecuteField = 

        }
    }

    public static class Helper
    {
        public static IDelegateWrapper CreateFunc(Delegate func, Type t)
        {
            return (Activator.CreateInstance(
                typeof (SuperAwesomeDelegateWrapper<>)
                    .MakeGenericType(t))
                    as IDelegateWrapper);
            
        }
    }

    public interface IDelegateWrapper
    {
        Delegate Invoke();
    }

    public class SuperAwesomeDelegateWrapper<T>
    {
        public SuperAwesomeDelegateWrapper(Delegate originalFunc)
        {
            _originalFunc = originalFunc;
        } 
        
        public Delegate Delegate { get; };

        public T Invoke()
        {
            return (T)_originalFunc.DynamicInvoke();
        }
    }
}
