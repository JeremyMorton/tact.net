using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Tact.Reflection
{
    public sealed class EfficientInvoker2
    {
        private static readonly ConcurrentDictionary<Type, EfficientInvoker2> TypeToWrapperMap
            = new ConcurrentDictionary<Type, EfficientInvoker2>();

        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, EfficientInvoker2>> TypeToMethodToWrapperMap
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, EfficientInvoker2>>();

        private readonly Func<object, object[], object> _func;

        private EfficientInvoker2(Func<object, object[], object> func)
        {
            _func = func;
        }

        public static EfficientInvoker2 ForDelegate(Delegate del)
        {
            var type = del.GetType();
            return TypeToWrapperMap.GetOrAdd(type, t =>
            {
                var method = del.GetMethodInfo();
                var wrapper = CreateMethodWrapper(t, method, true);
                return new EfficientInvoker2(wrapper);
            });
        }

        public static EfficientInvoker2 ForMethod(Type type, string methodName)
        {
            ////var key = new MethodKey(type, methodName);
            var methodToWrapperMap = TypeToMethodToWrapperMap.GetOrAdd(
                type,
                k => new ConcurrentDictionary<string, EfficientInvoker2>());

            var typeInfo = type.GetTypeInfo();

            return methodToWrapperMap.GetOrAdd(methodName, k =>
            {
                var method = typeInfo.GetMethod(k);
                var wrapper = CreateMethodWrapper(type, method, false);
                return new EfficientInvoker2(wrapper);
            });
        }

        public static EfficientInvoker2 ForProperty(Type type, string propertyName)
        {
            ////var key = new MethodKey(type, propertyName);
            var methodToWrapperMap = TypeToMethodToWrapperMap.GetOrAdd(
                type,
                k => new ConcurrentDictionary<string, EfficientInvoker2>());

            return methodToWrapperMap.GetOrAdd(propertyName, k =>
            {
                var wrapper = CreatePropertyWrapper(type, propertyName);
                return new EfficientInvoker2(wrapper);
            });
        }

        public object Invoke(object target, params object[] args)
        {
            return _func(target, args);
        }

        public async Task<object> InvokeAsync(object target, params object[] args)
        {
            var result = _func(target, args);
            var task = result as Task;
            if (task == null) return result;

            await task.ConfigureAwait(false);
            return task.GetResult();
        }

        private static Func<object, object[], object> CreateMethodWrapper(Type type, MethodInfo method, bool isDelegate)
        {
            var parameters = method.GetParameters();
            var hasReturn = method.ReturnType != typeof(void);

            var targetExp = Expression.Parameter(typeof(object), "target");
            var argsExp = Expression.Parameter(typeof(object[]), "args");

            var paramsExps = new Expression[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var constExp = Expression.Constant(i, typeof(int));
                var argExp = Expression.ArrayIndex(argsExp, constExp);
                paramsExps[i] = Expression.Convert(argExp, parameters[i].ParameterType);
            }

            var castTargetExp = Expression.Convert(targetExp, type);
            var invokeExp = isDelegate
                ? (Expression)Expression.Invoke(castTargetExp, paramsExps)
                : Expression.Call(castTargetExp, method, paramsExps);

            var resultExp = hasReturn ? Expression.Convert(invokeExp, typeof(object)) : invokeExp;
            var lambdaExp = Expression.Lambda(resultExp, targetExp, argsExp);
            var lambda = lambdaExp.Compile();

            if (hasReturn)
                return (Func<object, object[], object>)lambda;

            var action = (Action<object, object[]>)lambda;
            return (target, args) =>
            {
                action(target, args);
                return null;
            };
        }

        private static Func<object, object[], object> CreatePropertyWrapper(Type type, string propertyName)
        {
            var property = type.GetRuntimeProperty(propertyName);
            var argExp = Expression.Parameter(typeof(object), "target");
            var castArgExp = Expression.Convert(argExp, type);
            var propExp = Expression.Property(castArgExp, property);
            var castPropExp = Expression.Convert(propExp, typeof(object));
            var lambdaExp = Expression.Lambda(castPropExp, argExp);
            var lambda = lambdaExp.Compile();
            var func = (Func<object, object>) lambda;
            return (target, args) => func(target);
        }

        private class MethodKeyComparer : IEqualityComparer<MethodKey>
        {
            public static readonly MethodKeyComparer Instance = new MethodKeyComparer();

            public bool Equals(MethodKey x, MethodKey y)
            {
                return x.Type == y.Type &&
                       StringComparer.Ordinal.Equals(x.Name, y.Name);
            }

            public int GetHashCode(MethodKey key)
            {
                var typeCode = key.Type.GetHashCode();
                var methodCode = key.Name.GetHashCode();
                return CombineHashCodes(typeCode, methodCode);
            }

            // From System.Web.Util.HashCodeCombiner
            private static int CombineHashCodes(int h1, int h2)
            {
                return ((h1 << 5) + h1) ^ h2;
            }
        }

        private struct MethodKey
        {
            public MethodKey(Type type, string name)
            {
                Type = type;
                Name = name;
            }

            public readonly Type Type;
            public readonly string Name;
        }
    }
}