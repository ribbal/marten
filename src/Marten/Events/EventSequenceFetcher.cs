using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Marten.Internal;
using Marten.Linq.QueryHandlers;
using Weasel.Postgresql;

namespace Marten.Events;

/// <summary>
///     Used to fetch the next N values of the event store sequence numbers
/// </summary>
internal class EventSequenceFetcher: IQueryHandler<Queue<long>>
{
    private readonly string _sql;

    public EventSequenceFetcher(EventGraph graph, int number)
    {
        _sql = $"select nextval('{graph.DatabaseSchemaName}.mt_events_sequence') from generate_series(1,{number})";
    }

    public void ConfigureCommand(ICommandBuilder builder, IMartenSession session)
    {
        builder.Append(_sql);
    }

    public Queue<long> Handle(DbDataReader reader, IMartenSession session)
    {
        var queue = new Queue<long>();

        while (reader.Read())
        {
            queue.Enqueue(reader.GetFieldValue<long>(0));
        }

        return queue;
    }

    public async Task<Queue<long>> HandleAsync(DbDataReader reader, IMartenSession session, CancellationToken token)
    {
        var queue = new Queue<long>();

        while (await reader.ReadAsync(token).ConfigureAwait(false))
        {
            queue.Enqueue(await reader.GetFieldValueAsync<long>(0, token).ConfigureAwait(false));
        }

        return queue;
    }

    public Task<int> StreamJson(Stream stream, DbDataReader reader, CancellationToken token)
    {
        throw new NotSupportedException();
    }
}
