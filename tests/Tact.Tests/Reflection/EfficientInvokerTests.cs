using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Tact.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Tact.Tests.Reflection
{
    public class EfficientInvokerTests
    {
        public const int AsyncIterations = 1000;
        public const int Iterations = 1000000;

        private static readonly object[] Args = { 1, true };

        private readonly TestClass[] _obj = {
                                                new TestClass(),
                                                new TestClass1(),
                                                new TestClass2(),
                                                new TestClass3(),
                                                new TestClass4()
                                            };
        private readonly ITestOutputHelper _output;
        private static readonly long TicksPerMillisecond = Stopwatch.Frequency / 1000;

        public EfficientInvokerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DelegateComparison()
        {
            var count = 0;
            var count1 = 0;
            var count2 = 0;
            var count3 = 0;
            var count4 = 0;
            Func<int, int, bool> func = (a, b) =>
            {
                Interlocked.Increment(ref count);
                return a == b;
            };
            Func<int, int, bool> func1 = (a, b) =>
            {
                Interlocked.Increment(ref count1);
                return a == b;
            };
            Func<int, int, bool> func2 = (a, b) =>
            {
                Interlocked.Increment(ref count2);
                return a == b;
            };
            Func<int, int, bool> func3 = (a, b) =>
            {
                Interlocked.Increment(ref count3);
                return a == b;
            };
            Func<int, int, bool> func4 = (a, b) =>
            {
                Interlocked.Increment(ref count4);
                return a == b;
            };

            var ticks0 = DelegateDynamicInvoke(func);
            Assert.Equal(Iterations, count);

            var ticks1 = DelegateEfficientInvoke(func);
            Assert.Equal(Iterations * 2, count);

            var ticks2 = DelegateEfficientInvoke2(func);
            Assert.Equal(Iterations * 3, count);

            var ticks3 = MultiDelegatesEfficientInvoke(func, func1, func2, func3, func4);
            Assert.Equal(Iterations * 4, count);
            Assert.Equal(Iterations, count1);
            Assert.Equal(Iterations, count2);
            Assert.Equal(Iterations, count3);
            Assert.Equal(Iterations, count4);

            var ticks4 = MultiDelegatesEfficientInvoke2(func, func1, func2, func3, func4);
            Assert.Equal(Iterations * 5, count);
            Assert.Equal(Iterations * 2, count1);
            Assert.Equal(Iterations * 2, count2);
            Assert.Equal(Iterations * 2, count3);
            Assert.Equal(Iterations * 2, count4);

            ////Assert.True(ticks1 * 10 < ticks0);
        }

        private long DelegateDynamicInvoke(Delegate d)
        {
            var sw0 = Stopwatch.StartNew();
            d.DynamicInvoke(1, 1);
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                d.DynamicInvoke(iteration, iteration);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long DelegateEfficientInvoke(Delegate d)
        {
            var sw0 = Stopwatch.StartNew();
            var x = d.GetInvoker();
            x.Invoke(d, 1, 1);
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                x.Invoke(d, iteration, iteration);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long DelegateEfficientInvoke2(Delegate d)
        {
            var sw0 = Stopwatch.StartNew();
            var x = d.GetInvoker2();
            x.Invoke(d, 1, 1);
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                x.Invoke(d, iteration, iteration);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long MultiDelegatesEfficientInvoke(Delegate d, Delegate d1, Delegate d2, Delegate d3, Delegate d4)
        {
            var sw0 = Stopwatch.StartNew();
            var x = d.GetInvoker();
            var x1 = d1.GetInvoker();
            var x2 = d2.GetInvoker();
            var x3 = d3.GetInvoker();
            var x4 = d4.GetInvoker();
            x.Invoke(d, 1, 1);
            x1.Invoke(d1, 1, 1);
            x2.Invoke(d2, 1, 1);
            x3.Invoke(d3, 1, 1);
            x4.Invoke(d4, 1, 1);
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                x.Invoke(d, iteration, iteration);
                x1.Invoke(d1, iteration, iteration);
                x2.Invoke(d2, iteration, iteration);
                x3.Invoke(d3, iteration, iteration);
                x4.Invoke(d4, iteration, iteration);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long MultiDelegatesEfficientInvoke2(Delegate d, Delegate d1, Delegate d2, Delegate d3, Delegate d4)
        {
            var sw0 = Stopwatch.StartNew();
            var x = d.GetInvoker2();
            var x1 = d1.GetInvoker2();
            var x2 = d2.GetInvoker2();
            var x3 = d3.GetInvoker2();
            var x4 = d4.GetInvoker2();
            x.Invoke(d, 1, 1);
            x1.Invoke(d1, 1, 1);
            x2.Invoke(d2, 1, 1);
            x3.Invoke(d3, 1, 1);
            x4.Invoke(d4, 1, 1);
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                x.Invoke(d, iteration, iteration);
                x.Invoke(d1, iteration, iteration);
                x.Invoke(d2, iteration, iteration);
                x.Invoke(d3, iteration, iteration);
                x.Invoke(d4, iteration, iteration);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        [Fact]
        public void MethodComparison()
        {
            var ticks0 = MethodInfoInvoke();
            Assert.Equal(Iterations, _obj[0].Count0);

            var ticks1 = CachedMethodInfoInvoke();
            Assert.Equal(Iterations * 2, _obj[0].Count0);

            var ticks2 = MethodEfficientInvoker();
            Assert.Equal(Iterations * 3, _obj[0].Count0);

            var ticks3 = MethodEfficientInvoker2();
            Assert.Equal(Iterations * 4, _obj[0].Count0);

            var ticks4 = MultiMethodEfficientInvoker();
            Assert.Equal(Iterations * 5, _obj[0].Count0);
            Assert.Equal(Iterations, _obj[1].Count0);
            Assert.Equal(Iterations, _obj[2].Count0);
            Assert.Equal(Iterations, _obj[3].Count0);
            Assert.Equal(Iterations, _obj[4].Count0);

            var ticks5 = MultiMethodEfficientInvoker2();
            Assert.Equal(Iterations * 6, _obj[0].Count0);
            Assert.Equal(Iterations * 2, _obj[1].Count0);
            Assert.Equal(Iterations * 2, _obj[2].Count0);
            Assert.Equal(Iterations * 2, _obj[3].Count0);
            Assert.Equal(Iterations * 2, _obj[4].Count0);

            ////Assert.True(ticks1 < ticks0);
            ////Assert.True(ticks2 * 8 < ticks0);
        }

        private long MethodInfoInvoke()
        {
            var sw0 = Stopwatch.StartNew();
            _obj[0].GetType().GetTypeInfo().GetMethod("TestMethod0").Invoke(_obj[0], Args);
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                _obj[0].GetType().GetTypeInfo().GetMethod("TestMethod0").Invoke(_obj[0], Args);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long CachedMethodInfoInvoke()
        {
            var map = new ConcurrentDictionary<Type, MethodInfo>();

            var sw0 = Stopwatch.StartNew();
            map.GetOrAdd(_obj[0].GetType(), type => type.GetTypeInfo().GetMethod("TestMethod0")).Invoke(_obj[0], Args);
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                map.GetOrAdd(_obj[0].GetType(), type => type.GetTypeInfo().GetMethod("TestMethod0")).Invoke(_obj[0], Args);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long MethodEfficientInvoker()
        {
            var sw0 = Stopwatch.StartNew();
            var x = _obj[0].GetType().GetMethodInvoker("TestMethod0");
            x.Invoke(_obj[0], Args);
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                x.Invoke(_obj[0], Args);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long MethodEfficientInvoker2()
        {
            var sw0 = Stopwatch.StartNew();
            var x = _obj[0].GetType().GetMethodInvoker2("TestMethod0");
            x.Invoke(_obj[0], Args);
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                x.Invoke(_obj[0], Args);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long MultiMethodEfficientInvoker()
        {
            var initTasks = new Task[5][];
            var invokeTasks = new Task[5][];
            for (var i = 0; i < 5; i++)
            {
                initTasks[i] = new Task[5];
                invokeTasks[i] = new Task[5];
            }
            var x = new EfficientInvoker[5][];

            var sw0 = Stopwatch.StartNew();
            for (var i = 0; i < 5; i++)
            {
                var efficientInvokers = new EfficientInvoker[5];
                x[i] = efficientInvokers;
                for (var j = 0; j < 5; j++)
                {
                    var target = _obj[i];
                    var methodName = $"TestMethod{j}";
                    var j1 = j;
                    ////initTasks[i][j] = Task.Run(() =>
                    ////                           {
                                                   var efficientInvoker = target.GetType().GetMethodInvoker(methodName);
                                                   efficientInvokers[j1] = efficientInvoker;
                                                   efficientInvoker.Invoke(target, Args);
                    ////                           });
                }
            }
            ////Task.WhenAll(initTasks.SelectMany(tasks => tasks)).Wait();
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                for (var i = 0; i < 5; i++)
                {
                    for (var j = 0; j < 5; j++)
                    {
                        var efficientInvoker = x[i][j];
                        var target = _obj[i];
                        ////invokeTasks[i][j] = Task.Run(() =>
                        ////                             {
                                                         efficientInvoker.Invoke(target, Args);
                        ////                             });
                    }
                }
                ////Task.WhenAll(invokeTasks.SelectMany(tasks => tasks)).Wait();
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long MultiMethodEfficientInvoker2()
        {
            var initTasks = new Task[5][];
            var invokeTasks = new Task[5][];
            for (var i = 0; i < 5; i++)
            {
                initTasks[i] = new Task[5];
                invokeTasks[i] = new Task[5];
            }
            var x = new EfficientInvoker2[5][];

            var sw0 = Stopwatch.StartNew();
            for (var i = 0; i < 5; i++)
            {
                var efficientInvokers = new EfficientInvoker2[5];
                x[i] = efficientInvokers;
                for (var j = 0; j < 5; j++)
                {
                    var target = _obj[i];
                    var methodName = $"TestMethod{j}";
                    var j1 = j;
                    ////initTasks[i][j] = Task.Run(() =>
                    ////                           {
                                                   var efficientInvoker = target.GetType().GetMethodInvoker2(methodName);
                                                   efficientInvokers[j1] = efficientInvoker;
                                                   efficientInvoker.Invoke(target, Args);
                                               ////});
                }
            }
            ////Task.WhenAll(initTasks.SelectMany(tasks => tasks)).Wait();
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                for (var i = 0; i < 5; i++)
                {
                    for (var j = 0; j < 5; j++)
                    {
                        var efficientInvoker = x[i][j];
                        var target = _obj[i];
                        ////invokeTasks[i][j] = Task.Run(() =>
                        ////{
                            efficientInvoker.Invoke(target, Args);
                        ////});
                    }
                }
                ////Task.WhenAll(invokeTasks.SelectMany(tasks => tasks)).Wait();
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        [Fact]
        public void AsyncMethodComparison()
        {
            var ticks0 = MethodAsyncEfficientInvoker();
            Assert.Equal(AsyncIterations, _obj[0].Count0);

            var ticks1 = MethodAsyncEfficientInvoker2();
            Assert.Equal(AsyncIterations * 2, _obj[0].Count0);

            var ticks2 = MultiMethodAsyncEfficientInvoker();
            Assert.Equal(AsyncIterations * 3, _obj[0].Count0);
            Assert.Equal(AsyncIterations, _obj[1].Count0);
            Assert.Equal(AsyncIterations, _obj[2].Count0);
            Assert.Equal(AsyncIterations, _obj[3].Count0);
            Assert.Equal(AsyncIterations, _obj[4].Count0);

            var ticks3 = MultiMethodAsyncEfficientInvoker2();
            Assert.Equal(AsyncIterations * 4, _obj[0].Count0);
            Assert.Equal(AsyncIterations * 2, _obj[1].Count0);
            Assert.Equal(AsyncIterations * 2, _obj[2].Count0);
            Assert.Equal(AsyncIterations * 2, _obj[3].Count0);
            Assert.Equal(AsyncIterations * 2, _obj[4].Count0);

            ////Assert.True(ticks1 < ticks0);
            ////Assert.True(ticks2 * 8 < ticks0);
        }

        private long MethodAsyncEfficientInvoker()
        {
            var sw0 = Stopwatch.StartNew();
            var x = _obj[0].GetType().GetMethodInvoker("TestMethodAsync0");
            x.InvokeAsync(_obj[0], Args).Wait();
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < AsyncIterations - 1; iteration++)
            {
                x.InvokeAsync(_obj[0], Args).Wait();
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long MethodAsyncEfficientInvoker2()
        {
            var sw0 = Stopwatch.StartNew();
            var x = _obj[0].GetType().GetMethodInvoker2("TestMethodAsync0");
            x.InvokeAsync(_obj[0], Args).Wait();
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < AsyncIterations - 1; iteration++)
            {
                x.InvokeAsync(_obj[0], Args).Wait();
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long MultiMethodAsyncEfficientInvoker()
        {
            var initTasks = new Task[5][];
            var invokeTasks = new Task[5][];
            for (var i = 0; i < 5; i++)
            {
                initTasks[i] = new Task[5];
                invokeTasks[i] = new Task[5];
            }
            var x = new EfficientInvoker[5][];

            var sw0 = Stopwatch.StartNew();
            for (var i = 0; i < 5; i++)
            {
                x[i] = new EfficientInvoker[5];
                for (var j = 0; j < 5; j++)
                {
                    x[i][j] = _obj[i].GetType().GetMethodInvoker($"TestMethodAsync{j}");
                    initTasks[i][j] = x[i][j].InvokeAsync(_obj[i], Args);
                }
            }
            Task.WhenAll(initTasks.SelectMany(tasks => tasks)).Wait();
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < AsyncIterations - 1; iteration++)
            {
                for (var i = 0; i < 5; i++)
                {
                    for (var j = 0; j < 5; j++)
                    {
                        invokeTasks[i][j] = x[i][j].InvokeAsync(_obj[i], Args);
                    }
                }
                Task.WhenAll(invokeTasks.SelectMany(tasks => tasks)).Wait();
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long MultiMethodAsyncEfficientInvoker2()
        {
            var initTasks = new Task[5][];
            var invokeTasks = new Task[5][];
            for (var i = 0; i < 5; i++)
            {
                initTasks[i] = new Task[5];
                invokeTasks[i] = new Task[5];
            }
            var x = new EfficientInvoker2[5][];

            var sw0 = Stopwatch.StartNew();
            for (var i = 0; i < 5; i++)
            {
                x[i] = new EfficientInvoker2[5];
                for (var j = 0; j < 5; j++)
                {
                    x[i][j] = _obj[i].GetType().GetMethodInvoker2($"TestMethodAsync{j}");
                    initTasks[i][j] = x[i][j].InvokeAsync(_obj[i], Args);
                }
            }
            Task.WhenAll(initTasks.SelectMany(tasks => tasks)).Wait();
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < AsyncIterations - 1; iteration++)
            {
                for (var i = 0; i < 5; i++)
                {
                    for (var j = 0; j < 5; j++)
                    {
                        invokeTasks[i][j] = x[i][j].InvokeAsync(_obj[i], Args);
                    }
                }
                Task.WhenAll(invokeTasks.SelectMany(tasks => tasks)).Wait();
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        [Fact]
        public void PropertyComparison()
        {
            var ticks0 = PropertyInfoInvoke();
            Assert.Equal(Iterations, _obj[0].Count0);

            var ticks1 = CachedPropertyInfoInvoke();
            Assert.Equal(Iterations * 2, _obj[0].Count0);

            var ticks2 = PropertyEfficientInvoker();
            Assert.Equal(Iterations * 3, _obj[0].Count0);

            var ticks3 = PropertyEfficientInvoker2();
            Assert.Equal(Iterations * 4, _obj[0].Count0);

            ////Assert.True(ticks1 < ticks0);
            ////Assert.True(ticks2 * 6 < ticks0);
        }

        private long PropertyInfoInvoke()
        {
            var sw0 = Stopwatch.StartNew();
            var a = _obj[0].GetType().GetRuntimeProperty("TestProperty0").GetValue(_obj[0]);
            sw0.Stop();

            Assert.Equal('a', a);

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                _obj[0].GetType().GetRuntimeProperty("TestProperty0").GetValue(_obj[0]);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long CachedPropertyInfoInvoke()
        {
            var map = new ConcurrentDictionary<Type, PropertyInfo>();

            var sw0 = Stopwatch.StartNew();
            var a = map.GetOrAdd(_obj[0].GetType(), type => type.GetRuntimeProperty("TestProperty0")).GetValue(_obj[0]);
            sw0.Stop();

            Assert.Equal('a', a);

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                map.GetOrAdd(_obj[0].GetType(), type => type.GetRuntimeProperty("TestProperty0")).GetValue(_obj[0]);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long PropertyEfficientInvoker()
        {
            var sw0 = Stopwatch.StartNew();
            var x = _obj[0].GetType().GetPropertyInvoker("TestProperty0");
            x.Invoke(_obj[0]);
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                x.Invoke(_obj[0]);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        private long PropertyEfficientInvoker2()
        {
            var sw0 = Stopwatch.StartNew();
            var x = _obj[0].GetType().GetPropertyInvoker2("TestProperty0");
            x.Invoke(_obj[0]);
            sw0.Stop();

            int iteration;
            var sw1 = Stopwatch.StartNew();
            for (iteration = 0; iteration < Iterations - 1; iteration++)
            {
                x.Invoke(_obj[0]);
            }
            sw1.Stop();

            OutputBothTimings(iteration, sw0.ElapsedTicks, sw1.ElapsedTicks);
            return sw1.ElapsedTicks;
        }

        [Fact]
        public async Task InvokeAsync()
        {
            var invoker = _obj[0].GetType().GetMethodInvoker("TestMethodAsync0");
            var result1 = await invoker.InvokeAsync(_obj[0], 1, false).ConfigureAwait(false);
            var result2 = await invoker.InvokeAsync(_obj[0], 2, true).ConfigureAwait(false);

            Assert.Equal(false, result1);
            Assert.Equal(false, result2);
        }

        private void OutputBothTimings(int iterations, long firstElapsedTicks, long restElapsedTicks, [CallerMemberName] string callerMemberName = null)
        {
            _output.WriteLine($"{callerMemberName,-35} - First: {(double)firstElapsedTicks / TicksPerMillisecond,7:N0}ms - Next {iterations,11:N0}: {(double)restElapsedTicks / TicksPerMillisecond,7:N0}ms - {(double)restElapsedTicks / TicksPerMillisecond / iterations,7:N7}ms/call");
        }

        private class TestClass
        {
            private const int AsyncMillisecondsDelay = 0;

            public int Count0;
            public int Count1;
            public int Count2;
            public int Count3;
            public int Count4;

            // ReSharper disable once UnusedMember.Local
            public int TestMethod0(int i, bool b)
            {
                Interlocked.Increment(ref Count0);
                return i + (b ? 1 : 2);
            }

            // ReSharper disable once UnusedMember.Local
            public int TestMethod1(int i, bool b)
            {
                Interlocked.Increment(ref Count1);
                return i + (b ? 1 : 2);
            }

            // ReSharper disable once UnusedMember.Local
            public int TestMethod2(int i, bool b)
            {
                Interlocked.Increment(ref Count2);
                return i + (b ? 1 : 2);
            }

            // ReSharper disable once UnusedMember.Local
            public int TestMethod3(int i, bool b)
            {
                Interlocked.Increment(ref Count3);
                return i + (b ? 1 : 2);
            }

            // ReSharper disable once UnusedMember.Local
            public int TestMethod4(int i, bool b)
            {
                Interlocked.Increment(ref Count4);
                return i + (b ? 1 : 2);
            }

            // ReSharper disable once UnusedMember.Local
            public async Task<bool> TestMethodAsync0(int i, bool b)
            {
                await Task.Delay(AsyncMillisecondsDelay).ConfigureAwait(false);
                Interlocked.Increment(ref Count0);
                return i % 2 == 0 ^ b;
            }

            // ReSharper disable once UnusedMember.Local
            public async Task<bool> TestMethodAsync1(int i, bool b)
            {
                await Task.Delay(AsyncMillisecondsDelay).ConfigureAwait(false);
                Interlocked.Increment(ref Count1);
                return i % 2 == 0 ^ b;
            }

            // ReSharper disable once UnusedMember.Local
            public async Task<bool> TestMethodAsync2(int i, bool b)
            {
                await Task.Delay(AsyncMillisecondsDelay).ConfigureAwait(false);
                Interlocked.Increment(ref Count2);
                return i % 2 == 0 ^ b;
            }

            // ReSharper disable once UnusedMember.Local
            public async Task<bool> TestMethodAsync3(int i, bool b)
            {
                await Task.Delay(AsyncMillisecondsDelay).ConfigureAwait(false);
                Interlocked.Increment(ref Count3);
                return i % 2 == 0 ^ b;
            }

            // ReSharper disable once UnusedMember.Local
            public async Task<bool> TestMethodAsync4(int i, bool b)
            {
                await Task.Delay(AsyncMillisecondsDelay).ConfigureAwait(false);
                Interlocked.Increment(ref Count4);
                return i % 2 == 0 ^ b;
            }

            // ReSharper disable once UnusedMember.Local
            public char TestProperty0
            {
                get
                {
                    Interlocked.Increment(ref Count0);
                    return 'a';
                }
            }

            // ReSharper disable once UnusedMember.Local
            public char TestProperty1
            {
                get
                {
                    Interlocked.Increment(ref Count1);
                    return 'a';
                }
            }

            // ReSharper disable once UnusedMember.Local
            public char TestProperty2
            {
                get
                {
                    Interlocked.Increment(ref Count2);
                    return 'a';
                }
            }

            // ReSharper disable once UnusedMember.Local
            public char TestProperty3
            {
                get
                {
                    Interlocked.Increment(ref Count3);
                    return 'a';
                }
            }

            // ReSharper disable once UnusedMember.Local
            public char TestProperty4
            {
                get
                {
                    Interlocked.Increment(ref Count4);
                    return 'a';
                }
            }
        }

        private class TestClass1 : TestClass
        {
        }

        private class TestClass2 : TestClass
        {
        }

        private class TestClass3 : TestClass
        {
        }

        private class TestClass4 : TestClass
        {
        }
    }
}
