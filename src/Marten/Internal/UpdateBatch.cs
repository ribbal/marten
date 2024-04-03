using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JasperFx.Core.Exceptions;
using JasperFx.Core.Reflection;
using Marten.Exceptions;
using Marten.Internal.Operations;
using Marten.Internal.Sessions;

namespace Marten.Internal;

public class UpdateBatch: IUpdateBatch
{
    private readonly IList<Exception> _exceptions = new List<Exception>();
    private readonly IReadOnlyList<IStorageOperation> _operations;

    public UpdateBatch(IReadOnlyList<IStorageOperation> operations)
    {
        _operations = operations;
    }

    private IEnumerable<OperationPage> buildPages(IMartenSession session)
    {
        if (!_operations.Any()) yield break;

        if (_operations.Count < session.Options.UpdateBatchSize)
        {
            yield return new OperationPage(session, _operations);
        }
        else
        {
            var count = 0;

            while (count < _operations.Count)
            {
                var operations = _operations
                    .Skip(count)
                    .Take(session.Options.UpdateBatchSize)
                    .ToArray();

                var page = new OperationPage(session, operations);
                yield return page;

                count += session.Options.UpdateBatchSize;
            }
        }
    }

    public IReadOnlyList<Type> DocumentTypes()
    {
        return _operations.Select(x => x.DocumentType).Where(x => x != null).Distinct().ToList();
    }

    public Task PostUpdateAsync(IMartenSession session)
    {
        return Task.CompletedTask;
    }

    public Task PreUpdateAsync(IMartenSession session)
    {
        return Task.CompletedTask;
    }

    public IReadOnlyList<OperationPage> BuildPages(IMartenSession session)
    {
        return buildPages(session).ToList();
    }


}
