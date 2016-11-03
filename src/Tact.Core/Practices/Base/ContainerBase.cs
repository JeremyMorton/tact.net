﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tact.Diagnostics;
using Tact.Practices.LifetimeManagers;
using Tact.Practices.ResolutionHandlers;

namespace Tact.Practices.Base
{
    public abstract class ContainerBase : IContainer
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly Dictionary<Type, ILifetimeManager> _lifetimeManagerMap = new Dictionary<Type, ILifetimeManager>();
        private readonly Dictionary<Type, Dictionary<string, ILifetimeManager>> _multiRegistrationMap = new Dictionary<Type, Dictionary<string, ILifetimeManager>>();
        protected readonly ILog Log;

        private bool _isDisposed;

        protected abstract IList<IResolutionHandler> ResolutionHandlers { get; }

        protected ContainerBase(ILog log)
        {
            Log = log;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            using (EnterReadLock())
            {
                foreach (var lifetimeManager in _lifetimeManagerMap.Values)
                    lifetimeManager.Dispose(this);

                foreach (var registrations in _multiRegistrationMap.Values)
                    foreach (var lifetimeManager in registrations.Values)
                        lifetimeManager.Dispose(this);
            }
        }

        public object Resolve(Type type)
        {
            var stack = new Stack<Type>();
            return Resolve(type, stack);
        }

        public object Resolve(Type type, Stack<Type> stack)
        {
            using (EnterPush(type, stack))
            using (EnterReadLock())
            {
                if (_lifetimeManagerMap.ContainsKey(type))
                    return _lifetimeManagerMap[type].Resolve(stack);

                foreach (var handler in ResolutionHandlers)
                {
                    object result;
                    if (handler.TryGetService(this, type, stack, out result))
                        return result;
                }

                return null;
            }
        }

        public object Resolve(Type type, string key)
        {
            var stack = new Stack<Type>();
            return Resolve(type, key, stack);
        }

        public object Resolve(Type type, string key, Stack<Type> stack)
        {
            using (EnterPush(type, stack))
            using (EnterReadLock())
            {
                if (_multiRegistrationMap.ContainsKey(type))
                {
                    var registrations = _multiRegistrationMap[type];
                    foreach (var lifetimeManager in registrations)
                        if (lifetimeManager.Key == key)
                            return lifetimeManager.Value.Resolve(stack);
                }

                foreach (var resolutionHandler in ResolutionHandlers)
                {
                    object instance;
                    if (resolutionHandler.TryGetService(this, type, stack, out instance))
                        return instance;
                }
            }

            return null;
        }

        public IEnumerable<object> ResolveAll(Type type)
        {
            var stack = new Stack<Type>();
            return ResolveAll(type, stack);
        }

        public IEnumerable<object> ResolveAll(Type type, Stack<Type> stack)
        {
            var instances = new List<object>();

            using (EnterPush(type, stack))
            using (EnterReadLock())
            {
                if (_multiRegistrationMap.ContainsKey(type))
                {
                    var registrations = _multiRegistrationMap[type];
                    foreach (var lifetimeManager in registrations.Values)
                    {
                        var instance = lifetimeManager.Resolve(stack);
                        instances.Add(instance);
                    }
                }
                else 
                {
                    foreach (var resolutionHandler in ResolutionHandlers)
                    {
                        object instance;
                        if (resolutionHandler.TryGetService(this, type, stack, out instance))
                            instances.Add(instance);
                    }
                }
            }

            return instances;
        }

        public IResolver BeginScope()
        {
            var scope = CreateScope();
            InitializeScope(this, scope);
            return scope;
        }

        public void Register(Type fromType, ILifetimeManager lifetimeManager)
        {
            using (EnterWriteLock())
            {
                if (_lifetimeManagerMap.ContainsKey(fromType))
                {
                    var previous = _lifetimeManagerMap[fromType];
                    Log.Debug("Type: {0} - {1} - Replaced {2}", fromType.Name, lifetimeManager.Description,
                        previous.Description);
                }
                else
                    Log.Debug("Type: {0} - {1}", fromType.Name, lifetimeManager.Description);

                _lifetimeManagerMap[fromType] = lifetimeManager;
            }
        }

        public void Register(Type fromType, string key, ILifetimeManager lifetimeManager)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Required", nameof(key));

            using (EnterWriteLock())
            {
                if (_multiRegistrationMap.ContainsKey(fromType))
                {
                    var previous = _lifetimeManagerMap[fromType];
                    Log.Debug("Type: {0} - Key: {1} - {2} - Replaced {3}", fromType.Name, key, lifetimeManager.Description,
                        previous.Description);
                    _multiRegistrationMap[fromType][key] = lifetimeManager;
                }
                else
                {
                    Log.Debug("Type: {0} - Key: {1} - {2}", fromType.Name, key, lifetimeManager.Description);
                    _multiRegistrationMap[fromType] = new Dictionary<string, ILifetimeManager> {{key, lifetimeManager}};
                }
            }
        }

        public object CreateInstance(Type type, Stack<Type> stack)
        {
            var constructor = type.EnsureSingleCostructor();
            var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
            var arguments = new object[parameterTypes.Length];
            for (var i = 0; i < parameterTypes.Length; i++)
            {
                var parameterType = parameterTypes[i];
                arguments[i] = Resolve(parameterType, stack);
            }

            return Activator.CreateInstance(type, arguments);
        }

        protected abstract ContainerBase CreateScope();
        
        private static void InitializeScope(ContainerBase source, ContainerBase target)
        {
            using (source.EnterReadLock())
            using (target.EnterWriteLock())
            {
                foreach (var pair in source._lifetimeManagerMap)
                {
                    var clone = pair.Value.Clone(target);
                    target._lifetimeManagerMap[pair.Key] = clone;
                }

                foreach (var pair in source._multiRegistrationMap)
                {
                    var clones = new Dictionary<string, ILifetimeManager>();

                    foreach (var lifetimeManager in pair.Value)
                    {
                        var clone = lifetimeManager.Value.Clone(target);
                        clones[lifetimeManager.Key] = clone;
                    }

                    target._multiRegistrationMap[pair.Key] = clones;
                }
            }
        }
        
        private static IDisposable EnterPush(Type type, Stack<Type> stack)
        {
            if (stack.Contains(type))
                throw new InvalidOperationException("Recursive resolution detected");

            return new DisposablePush(type, stack);
        }
        
        private IDisposable EnterReadLock()
        {
            _lock.EnterReadLock();
            return new DisposableLock(_lock, true);
        }

        private IDisposable EnterWriteLock()
        {
            _lock.EnterWriteLock();
            return new DisposableLock(_lock, false);
        }

        private class DisposableLock : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;
            private readonly bool _isRead;

            public DisposableLock(ReaderWriterLockSlim lockSlim, bool isRead)
            {
                _lock = lockSlim;
                _isRead = isRead;
            }

            public void Dispose()
            {
                if (_isRead)
                    _lock.ExitReadLock();
                else
                    _lock.ExitWriteLock();
            }
        }

        private class DisposablePush : IDisposable
        {
            private readonly Type _type;
            private readonly Stack<Type> _stack;

            public DisposablePush(Type type, Stack<Type> stack)
            {
                _type = type;
                _stack = stack;

                stack.Push(type);
            }

            public void Dispose()
            {
                var type = _stack.Pop();

                if (type != _type)
                    throw new InvalidOperationException("Stack became unsynchronized");
            }
        }
    }
}