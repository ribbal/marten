using Marten.Linq;
using Marten.Linq.SqlGeneration;
using Weasel.Postgresql;

namespace Marten.Events.Daemon.Progress;

internal class ProjectionProgressStatement: Statement
{
    private readonly EventGraph _events;

    public ProjectionProgressStatement(EventGraph events)
    {
        _events = events;
    }

    public ShardName Name { get; set; }

    protected override void configure(ICommandBuilder builder)
    {
        builder.Append($"select name, last_seq_id from {_events.DatabaseSchemaName}.mt_event_progression");
        if (Name != null)
        {
            builder.Append(" where name = ");
            builder.AppendParameter(Name.Identity);
        }
    }
}
