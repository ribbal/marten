using System;
using NpgsqlTypes;
using Weasel.Postgresql;
using Weasel.Postgresql.SqlGeneration;

namespace Marten.Linq.SqlGeneration.Filters;

public class ByIdFilter<T>: ISqlFragment
{
    private readonly CommandParameter _parameter;

    public ByIdFilter(T value, NpgsqlDbType dbType)
    {
        _parameter = new CommandParameter(value, dbType);
    }

    public void Apply(ICommandBuilder builder)
    {
        builder.Append("id = ");
        _parameter.Apply(builder);
    }

}

public class ByGuidFilter: ByIdFilter<Guid>
{
    public ByGuidFilter(Guid value): base(value, NpgsqlDbType.Uuid)
    {
    }
}

public class ByStringFilter: ByIdFilter<string>
{
    public ByStringFilter(string value): base(value, NpgsqlDbType.Varchar)
    {
    }
}

public class ByIntFilter: ByIdFilter<int>
{
    public ByIntFilter(int value): base(value, NpgsqlDbType.Integer)
    {
    }
}

public class ByLongFilter: ByIdFilter<long>
{
    public ByLongFilter(long value): base(value, NpgsqlDbType.Bigint)
    {
    }

    public ByLongFilter(int value): base(value, NpgsqlDbType.Bigint)
    {
    }
}
