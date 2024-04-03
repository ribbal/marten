using System.Linq.Expressions;
using Marten.Linq.Members;
using Weasel.Postgresql.SqlGeneration;

namespace Marten.Linq.Parsing;

#region sample_IMethodCallParser

/// <summary>
///     Models the Sql generation for a method call
///     in a Linq query. For example, map an expression like Where(x => x.Property.StartsWith("prefix"))
///     to part of a Sql WHERE clause
/// </summary>
public interface IMethodCallParser
{
    /// <summary>
    ///     Can this parser create a Sql where clause
    ///     from part of a Linq expression that calls
    ///     a method
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    bool Matches(MethodCallExpression expression);

    /// <summary>
    ///     Creates an ISqlFragment object that Marten
    ///     uses to help construct the underlying Sql
    ///     command
    /// </summary>
    /// <param name="memberCollection"></param>
    /// <param name="options"></param>
    /// <param name="expression"></param>
    /// <param name="serializer"></param>
    /// <returns></returns>
    ISqlFragment Parse(IQueryableMemberCollection memberCollection, IReadOnlyStoreOptions options,
        MethodCallExpression expression);
}

#endregion
