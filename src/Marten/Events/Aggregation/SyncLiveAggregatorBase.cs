#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JasperFx.Core.Reflection;
using Marten.Events.Projections;

namespace Marten.Events.Aggregation;

/// <summary>
///     Internal base class for purely synchronous live aggregators
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class SyncLiveAggregatorBase<T>: ILiveAggregator<T> where T : class
{
    public abstract T Build(IReadOnlyList<IEvent> events, IQuerySession session, T? snapshot);

    public ValueTask<T> BuildAsync(IReadOnlyList<IEvent> events, IQuerySession session, T? snapshot,
        CancellationToken cancellation)
    {
        return new ValueTask<T>(Build(events, session, snapshot));
    }

    public T CreateDefault(IEvent @event)
    {
        try
        {
            return (T)Activator.CreateInstance(typeof(T), true);
        }
        catch (Exception e)
        {
            throw new System.InvalidOperationException($"There is no default constructor for {typeof(T).FullNameInCode()} or Create method for {@event.DotNetTypeName} event type.Check more about the create method convention in documentation: https://martendb.io/events/projections/event-projections.html#create-method-convention. If you're using Upcasting, check if {@event.DotNetTypeName} is an old event type. If it is, make sure to define transformation for it to new event type. Read more in Upcasting docs: https://martendb.io/events/versioning.html#upcasting-advanced-payload-transformations.");
        }
    }
}
