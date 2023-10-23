using System.Threading;
using System.Threading.Tasks;
using Marten.Services;
using Npgsql;
using Weasel.Core;

namespace Marten.Internal.Sessions;

internal class ReadOnlyMartenControlledConnectionTransaction: MartenControlledConnectionTransaction
{
    private const string SetTransactionReadOnly = "SET TRANSACTION READ ONLY;";

    public ReadOnlyMartenControlledConnectionTransaction(SessionOptions options, StoreOptions storeOptions): base(options, storeOptions)
    {
    }

    public override void Apply(NpgsqlCommand command)
    {
        EnsureConnected();

        command.Connection = Connection;
        command.Transaction = Transaction;
        command.CommandTimeout = CommandTimeout;
    }

    public override async Task ApplyAsync(NpgsqlCommand command, CancellationToken token)
    {
        await EnsureConnectedAsync(token).ConfigureAwait(false);

        command.Connection = Connection;
        command.Transaction = Transaction;
        command.CommandTimeout = CommandTimeout;
    }

    public override void BeginTransaction()
    {
        if (Transaction == null)
        {
            base.BeginTransaction();

            Transaction.CreateCommand(SetTransactionReadOnly)
                .ExecuteNonQuery();
        }
    }

    public override async ValueTask BeginTransactionAsync(CancellationToken token)
    {
        if (Transaction == null)
        {
            await base.BeginTransactionAsync(token).ConfigureAwait(false);

            await Transaction.CreateCommand(SetTransactionReadOnly)
                .ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }
    }
}
