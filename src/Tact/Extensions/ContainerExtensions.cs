﻿using System;
using System.Linq;
using System.Reflection;
using Tact.Configuration;
using Tact.Diagnostics;
using Tact.Practices;
using Tact.Practices.LifetimeManagers;
using Tact.Practices.LifetimeManagers.Implementation;

namespace Tact
{
    public static class ContainerExtensions
    {
        #region Initialize By Attribute

        public static void InitializeByAttribute(this IContainer container, params Assembly[] assemblies)
        {
            container.InitializeByAttribute<IInitializeAttribute>(assemblies);
        }

        public static void InitializeByAttribute(this IContainer container, params Type[] types)
        {
            container.InitializeByAttribute<IInitializeAttribute>(types);
        }

        public static void InitializeByAttribute<T>(this IContainer container, params Assembly[] assemblies)
            where T : IInitializeAttribute
        {
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                container.InitializeByAttribute<T>(types);
            }
        }

        public static void InitializeByAttribute<T>(this IContainer container, params Type[] types)
            where T : IInitializeAttribute
        {
            ILog logger;
            container.TryResolve(out logger);

            foreach (var type in types)
            {
                var attribute = type
                    .GetTypeInfo()
                    .GetCustomAttributes()
                    .OfType<T>()
                    .SingleOrDefault();

                if (attribute == null)
                    continue;
                
                logger?.Debug("Type: {0} - Attribute: {1}", type.Name, attribute.GetType().Name);
                attribute.Initialize(container);
            }
        }

        #endregion

        #region Register By Attribute

        public static void RegisterByAttribute(this IContainer container, params Assembly[] assemblies)
        {
            container.RegisterByAttribute<IRegisterAttribute, IRegisterConditionAttribute>(assemblies);
        }

        public static void RegisterByAttribute(this IContainer container, params Type[] types)
        {
            container.RegisterByAttribute<IRegisterAttribute, IRegisterConditionAttribute>(types);
        }

        public static void RegisterByAttribute<T>(this IContainer container, params Assembly[] assemblies)
            where T : IRegisterAttribute
        {
            container.RegisterByAttribute<T, IRegisterConditionAttribute>(assemblies);
        }

        public static void RegisterByAttribute<T>(this IContainer container, params Type[] types)
            where T : IRegisterAttribute
        {
            container.RegisterByAttribute<T, IRegisterConditionAttribute>(types);
        }

        public static void RegisterByAttribute<TRegister, TCondition>(this IContainer container, params Assembly[] assemblies)
            where TRegister : IRegisterAttribute
            where TCondition : IRegisterConditionAttribute
        {
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                container.RegisterByAttribute<TRegister, TCondition>(types);
            }
        }

        public static void RegisterByAttribute<TRegister, TCondition>(this IContainer container, params Type[] types)
            where TRegister : IRegisterAttribute
            where TCondition : IRegisterConditionAttribute
        {
            ILog logger;
            container.TryResolve(out logger);

            foreach (var type in types)
            {
                var attribute = type
                    .GetTypeInfo()
                    .GetCustomAttributes()
                    .OfType<TRegister>()
                    .SingleOrDefault();

                if (attribute == null)
                    continue;

                var conditions = type
                    .GetTypeInfo()
                    .GetCustomAttributes()
                    .OfType<TCondition>()
                    .ToArray();

                var shouldRegister = conditions.All(condition => condition.ShouldRegister(container, type));

                logger?.Debug("Type: {0} - Attribute: {1} - Conditions: {2} - ShouldRegister: {3}", type.Name,
                    attribute.GetType().Name, conditions.Length, shouldRegister);

                if (!shouldRegister)
                    continue;

                attribute.Register(container, type);
            }
        }

        #endregion

        #region Register Per Resolve
        
        public static void RegisterPerResolve<T>(this IContainer container, Func<IResolver, T> factory = null)
            where T : class
        {
            var type = typeof(T);
            container.RegisterPerResolve(type, factory);
        }

        public static void RegisterPerResolve(this IContainer container, Type type, Func<IResolver, object> factory = null)
        {
            if (factory == null)
            {
                container.RegisterPerResolve(type, type);
                return;
            }

            var lifetimeManager = new PerResolveLifetimeManager(type, container, factory);
            container.Register(type, lifetimeManager);
        }
        
        public static void RegisterPerResolve<T>(this IContainer container, string key, Func<IResolver, T> factory = null)
            where T : class
        {
            var type = typeof(T);
            container.RegisterPerResolve(type, key, factory);
        }

        public static void RegisterPerResolve(this IContainer container, Type type, string key, Func<IResolver, object> factory = null)
        {
            if (factory == null)
            {
                container.RegisterPerResolve(type, type, key);
                return;
            }

            var lifetimeManager = new PerResolveLifetimeManager(type, container, factory);
            container.Register(type, key, lifetimeManager);
        }
        
        public static void RegisterPerResolve<TFrom, TTo>(this IContainer container, string key = null)
            where TTo : class, TFrom
        {
            var fromType = typeof(TFrom);
            var toType = typeof(TTo);
            container.RegisterPerResolve(fromType, toType, key);
        }

        public static void RegisterPerResolve(this IContainer container, Type fromType, Type toType, string key = null)
        {
            toType.EnsureSingleCostructor();
            var lifetimeManager = new PerResolveLifetimeManager(toType, container);

            if (string.IsNullOrWhiteSpace(key))
                container.Register(fromType, lifetimeManager);
            else
                container.Register(fromType, key, lifetimeManager);
        }

        #endregion

        #region Register Per Scope
        
        public static void RegisterPerScope<T>(this IContainer container, Func<IResolver, T> factory = null)
            where T : class
        {
            var type = typeof(T);
            container.RegisterPerScope(type, factory);
        }

        public static void RegisterPerScope(this IContainer container, Type type, Func<IResolver, object> factory = null)
        {
            if (factory == null)
            {
                container.RegisterPerScope(type, type);
                return;
            }

            var lifetimeManager = new PerScopeLifetimeManager(type, container, factory);
            container.Register(type, lifetimeManager);
        }
        
        public static void RegisterPerScope<T>(this IContainer container, string key, Func<IResolver, T> factory = null)
            where T : class
        {
            var type = typeof(T);
            container.RegisterPerScope(type, key, factory);
        }

        public static void RegisterPerScope(this IContainer container, Type type, string key, Func<IResolver, object> factory = null)
        {
            if (factory == null)
            {
                container.RegisterPerScope(type, type, key);
                return;
            }

            var lifetimeManager = new PerScopeLifetimeManager(type, container, factory);
            container.Register(type, key, lifetimeManager);
        }
        
        public static void RegisterPerScope<TFrom, TTo>(this IContainer container, string key = null)
            where TTo : class, TFrom
        {
            var fromType = typeof(TFrom);
            var toType = typeof(TTo);
            container.RegisterPerScope(fromType, toType, key);
        }

        public static void RegisterPerScope(this IContainer container, Type fromType, Type toType, string key = null)
        {
            toType.EnsureSingleCostructor();
            var lifetimeManager = new PerScopeLifetimeManager(toType, container);

            if (string.IsNullOrWhiteSpace(key))
                container.Register(fromType, lifetimeManager);
            else
                container.Register(fromType, key, lifetimeManager);
        }

        #endregion

        #region Register Instance
        
        public static void RegisterInstance<T>(this IContainer container, T value, string key = null)
        {
            var type = typeof(T);

            if (string.IsNullOrWhiteSpace(key))
                container.RegisterInstance(type, value);
            else
                container.RegisterInstance(type, value, key);
        }

        public static void RegisterInstance(this IContainer container, Type type, object value, string key = null)
        {
            var lifetimeManager = new InstanceLifetimeManager(value, container);

            if (string.IsNullOrWhiteSpace(key))
                container.Register(type, lifetimeManager);
            else
                container.Register(type, key, lifetimeManager);
        }

        #endregion

        #region Register Singleton
        
        public static void RegisterSingleton<T>(this IContainer container, Func<IResolver, T> factory = null)
            where T : class
        {
            var type = typeof(T);
            container.RegisterSingleton(type, factory);
        }

        public static void RegisterSingleton(this IContainer container, Type type, Func<IResolver, object> factory = null)
        {
            if (factory == null)
            {
                container.RegisterSingleton(type, type);
                return;
            }

            var lifetimeManager = new SingletonLifetimeManager(type, container, factory);
            container.Register(type, lifetimeManager);
        }
        
        public static void RegisterSingleton<T>(this IContainer container, string key, Func<IResolver, T> factory = null)
            where T : class
        {
            var type = typeof(T);
            container.RegisterSingleton(type, key, factory);
        }

        public static void RegisterSingleton(this IContainer container, Type type, string key, Func<IResolver, object> factory = null)
        {
            if (factory == null)
            {
                container.RegisterSingleton(type, type, key);
                return;
            }

            var lifetimeManager = new SingletonLifetimeManager(type, container, factory);
            container.Register(type, key, lifetimeManager);
        }
        
        public static void RegisterSingleton<TFrom, TTo>(this IContainer container, string key = null)
            where TTo : class, TFrom
        {
            var fromType = typeof(TFrom);
            var toType = typeof(TTo);
            container.RegisterSingleton(fromType, toType, key);
        }

        public static void RegisterSingleton(this IContainer container, Type fromType, Type toType, string key = null)
        {
            toType.EnsureSingleCostructor();
            var lifetimeManager = new SingletonLifetimeManager(toType, container);

            if (string.IsNullOrWhiteSpace(key))
                container.Register(fromType, lifetimeManager);
            else
                container.Register(fromType, key, lifetimeManager);
        }

        #endregion

        #region Register Transient
        
        public static void RegisterTransient<T>(this IContainer container, Func<IResolver, T> factory = null)
            where T : class
        {
            var type = typeof(T);
            container.RegisterTransient(type, factory);
        }

        public static void RegisterTransient(this IContainer container, Type type, Func<IResolver, object> factory = null)
        {
            if (factory == null)
            {
                container.RegisterTransient(type, type);
                return;
            }

            var lifetimeManager = new TransientLifetimeManager(type, container, factory);
            container.Register(type, lifetimeManager);
        }
        
        public static void RegisterTransient<T>(this IContainer container, string key, Func<IResolver, T> factory = null)
            where T : class
        {
            var type = typeof(T);
            container.RegisterTransient(type, key, factory);
        }

        public static void RegisterTransient(this IContainer container, Type type, string key, Func<IResolver, object> factory = null)
        {
            if (factory == null)
            {
                container.RegisterTransient(type, type, key);
                return;
            }

            var lifetimeManager = new TransientLifetimeManager(type, container, factory);
            container.Register(type, key, lifetimeManager);
        }
        
        public static void RegisterTransient<TFrom, TTo>(this IContainer container, string key = null)
            where TTo : class, TFrom
        {
            var fromType = typeof(TFrom);
            var toType = typeof(TTo);
            container.RegisterTransient(fromType, toType, key);
        }

        public static void RegisterTransient(this IContainer container, Type fromType, Type toType, string key = null)
        {
            toType.EnsureSingleCostructor();
            var lifetimeManager = new TransientLifetimeManager(toType, container);

            if (string.IsNullOrWhiteSpace(key))
                container.Register(fromType, lifetimeManager);
            else
                container.Register(fromType, key, lifetimeManager);
        }

        #endregion

        #region Register Proxy

        public static void RegisterProxy<TFrom, TTo>(this IContainer container, string fromKey = null, string toKey = null)
        {
            var fromType = typeof(TFrom);
            var toType = typeof(TTo);
            container.RegisterProxy(fromType, toType, fromKey, toKey);
        }

        public static void RegisterProxy(this IContainer container, Type fromType, Type toType, string fromKey = null, string toKey = null)
        {
            var lifetimeManager = new ProxyLifetimeManager(toType, fromKey, container);

            if (string.IsNullOrWhiteSpace(toKey))
                container.Register(fromType, lifetimeManager);
            else
                container.Register(fromType, toKey, lifetimeManager);
        }

        #endregion
    }
}