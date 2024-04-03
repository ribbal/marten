#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Marten.Events;
using Marten.Events.Daemon;
using Marten.Events.Daemon.HighWater;
using Marten.Events.Daemon.Internals;
using Marten.Events.Daemon.Progress;
using Marten.Linq.QueryHandlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Weasel.Postgresql;

namespace Marten.Storage;

public partial class MartenDatabase
{
    /// <summary>
    ///     Fetch the current size of the event store tables, including the current value
    ///     of the event sequence number
    /// </summary>
    /// <param name="tenantId">
    ///     Specify the database containing this tenant id. If omitted, this method uses the default
    ///     database
    /// </param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<EventStoreStatistics> FetchEventStoreStatistics(
        CancellationToken token = default)
    {
        var sql = $@"
select count(*) from {Options.Events.DatabaseSchemaName}.mt_events;
select count(*) from {Options.Events.DatabaseSchemaName}.mt_streams;
select last_value from {Options.Events.DatabaseSchemaName}.mt_events_sequence;
";

        await EnsureStorageExistsAsync(typeof(IEvent), token).ConfigureAwait(false);

        var statistics = new EventStoreStatistics();

        await using var conn = CreateConnection();

        await conn.OpenAsync(token).ConfigureAwait(false);

        await using var reader = await conn.CreateCommand(sql).ExecuteReaderAsync(token).ConfigureAwait(false);

        if (await reader.ReadAsync(token).ConfigureAwait(false))
        {
            statistics.EventCount = await reader.GetFieldValueAsync<long>(0, token).ConfigureAwait(false);
        }

        await reader.NextResultAsync(token).ConfigureAwait(false);

        if (await reader.ReadAsync(token).ConfigureAwait(false))
        {
            statistics.StreamCount = await reader.GetFieldValueAsync<long>(0, token).ConfigureAwait(false);
        }

        await reader.NextResultAsync(token).ConfigureAwait(false);

        if (await reader.ReadAsync(token).ConfigureAwait(false))
        {
            statistics.EventSequenceNumber = await reader.GetFieldValueAsync<long>(0, token).ConfigureAwait(false);
        }

        return statistics;
    }


    /// <summary>
    ///     Check the current progress of all asynchronous projections
    /// </summary>
    /// <param name="token"></param>
    /// <param name="tenantId">
    ///     Specify the database containing this tenant id. If omitted, this method uses the default
    ///     database
    /// </param>
    /// <returns></returns>
    public async Task<IReadOnlyList<ShardState>> AllProjectionProgress(
        CancellationToken token = default)
    {
        await EnsureStorageExistsAsync(typeof(IEvent), token).ConfigureAwait(false);

        var handler = (IQueryHandler<IReadOnlyList<ShardState>>)new ListQueryHandler<ShardState>(
            new ProjectionProgressStatement(Options.EventGraph),
            new ShardStateSelector());

        await using var conn = CreateConnection();
        await conn.OpenAsync(token).ConfigureAwait(false);

        var builder = new CommandBuilder();
        handler.ConfigureCommand(builder, null);

        await using var reader = await conn.ExecuteReaderAsync(builder, token).ConfigureAwait(false);
        return await handler.HandleAsync(reader, null, token).ConfigureAwait(false);
    }

    /// <summary>
    ///     Check the current progress of a single projection or projection shard
    /// </summary>
    /// <param name="tenantId">
    ///     Specify the database containing this tenant id. If omitted, this method uses the default
    ///     database
    /// </param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<long> ProjectionProgressFor(ShardName name,
        CancellationToken token = default)
    {
        await EnsureStorageExistsAsync(typeof(IEvent), token).ConfigureAwait(false);

        var statement = new ProjectionProgressStatement(Options.EventGraph) { Name = name };

        var handler = new OneResultHandler<ShardState>(statement,
            new ShardStateSelector(), true, false);

        await using var conn = CreateConnection();
        await conn.OpenAsync(token).ConfigureAwait(false);

        var builder = new CommandBuilder();
        handler.ConfigureCommand(builder, null);

        await using var reader = await conn.ExecuteReaderAsync(builder, token).ConfigureAwait(false);
        var state = await handler.HandleAsync(reader, null, token).ConfigureAwait(false);

        return state?.Sequence ?? 0;
    }

    /// <summary>
    ///     *If* a projection daemon has been started for this database, this
    ///     is the ShardStateTracker for the running daemon. This is useful in testing
    ///     scenarios
    /// </summary>
    public ShardStateTracker Tracker { get; private set; }

    internal IProjectionDaemon StartProjectionDaemon(DocumentStore store, ILogger? logger = null)
    {
        logger ??= store.Options.LogFactory?.CreateLogger<ProjectionDaemon>() ??
                   store.Options.DotNetLogger ?? NullLogger.Instance;

        var detector = new HighWaterDetector(this, Options.EventGraph, logger);

        return new ProjectionDaemon(store, this, logger, detector, new AgentFactory(store));
    }
}
