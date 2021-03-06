using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tact.Practices.LifetimeManagers
{
    public interface ILifetimeManager
    {
        string Description { get; }

        ILifetimeManager Clone(IContainer scope);

        object Resolve(Stack<Type> stack);

        Task DisposeAsync(IContainer scope, CancellationToken cancelToken = default(CancellationToken));
    }
}
