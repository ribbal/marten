using System.Linq.Expressions;
using Marten.Linq.Members;
using Weasel.Postgresql.SqlGeneration;

namespace Marten.Linq.Parsing.Methods.Strings;

internal class StringIsNullOrWhiteSpace: IMethodCallParser
{
    public bool Matches(MethodCallExpression expression)
    {
        return expression.Method.Name == nameof(string.IsNullOrWhiteSpace)
               && expression.Method.DeclaringType == typeof(string);
    }

    public ISqlFragment Parse(IQueryableMemberCollection memberCollection, IReadOnlyStoreOptions options,
        MethodCallExpression expression)
    {
        var locator = memberCollection.MemberFor(expression.Arguments[0]).RawLocator;

        return new WhereFragment($"({locator} IS NULL OR trim({locator}) = '')");
    }
}
