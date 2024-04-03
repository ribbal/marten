using System;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Marten.Internal;
using Marten.Linq.QueryHandlers;
using Weasel.Postgresql;

namespace Marten.Linq.Includes;

internal interface IIncludeQueryHandler<T>
{
    IQueryHandler<T> Inner { get; }
}

/// <summary>
///     Used internally to process Include() operations
///     in the Linq support
/// </summary>
public class IncludeQueryHandler<T>: IQueryHandler<T>, IIncludeQueryHandler<T>
{
    private readonly IIncludeReader[] _readers;

    public IncludeQueryHandler(IQueryHandler<T> inner, IIncludeReader[] readers)
    {
        Inner = inner;
        _readers = readers;
    }

    public IQueryHandler<T> Inner { get; }

    public void ConfigureCommand(ICommandBuilder builder, IMartenSession session)
    {
        Inner.ConfigureCommand(builder, session);
    }

    public Task<int> StreamJson(Stream stream, DbDataReader reader, CancellationToken token)
    {
        throw new NotSupportedException("JSON streaming is not supported in combination with Include() operations");
    }

    public T Handle(DbDataReader reader, IMartenSession session)
    {
        foreach (var includeReader in _readers)
        {
            includeReader.Read(reader);
            reader.NextResult();
        }

        return Inner.Handle(reader, session);
    }

    public async Task<T> HandleAsync(DbDataReader reader, IMartenSession session, CancellationToken token)
    {
        foreach (var includeReader in _readers)
        {
            await includeReader.ReadAsync(reader, token).ConfigureAwait(false);
            await reader.NextResultAsync(token).ConfigureAwait(false);
        }

        return await Inner.HandleAsync(reader, session, token).ConfigureAwait(false);
    }
}
