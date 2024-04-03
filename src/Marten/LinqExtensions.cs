#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JasperFx.Core.Reflection;
using Marten.Schema;

namespace Marten;

public static class LinqExtensions
{
    /// <summary>
    ///     Used for Linq queries to match an element to one of a list of values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="variable"></param>
    /// <param name="matches"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">when called for collection</exception>
    public static bool IsOneOf<T>(this T variable, params T[] matches)
    {
        if (typeof(T).IsArray || typeof(T).IsGenericEnumerable())
        {
            throw new NotSupportedException(
                "IsOneOf operator should not be used for collections. Use IsSubsetOf instead.");
        }

        return matches.Contains(variable);
    }

    /// <summary>
    ///     Used for Linq queries to match an element to one of a list of values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="variable"></param>
    /// <param name="matches"></param>
    /// <returns></returns>
    public static bool IsOneOf<T>(this T variable, IList<T> matches)
    {
        return matches.Contains(variable);
    }

    /// <summary>
    ///     Used for Linq queries to match an element to one of a list of values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="variable"></param>
    /// <param name="matches"></param>
    /// <returns></returns>
    public static bool In<T>(this T variable, params T[] matches)
    {
        if (typeof(T).IsArray || typeof(T).IsGenericEnumerable())
        {
            throw new NotSupportedException("In operator should not be used for collections. Use IsSubsetOf instead.");
        }

        return matches.Contains(variable);
    }

    /// <summary>
    ///     Used for Linq queries to match an element to one of a list of values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="variable"></param>
    /// <param name="matches"></param>
    /// <returns></returns>
    public static bool In<T>(this T variable, IList<T> matches)
    {
        return matches.Contains(variable);
    }

    /// <summary>
    ///     Used for Linq queries to determines whether an element is a superset of the specified collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <param name="items"></param>
    /// <returns></returns>
    public static bool IsSupersetOf<T>(this IEnumerable<T> enumerable, params T[] items)
    {
        var hashSet = new HashSet<T>(enumerable);
        return hashSet.IsSupersetOf(items);
    }

    /// <summary>
    ///     Used for Linq queries to determines whether an element is a subset of the specified collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <param name="items"></param>
    /// <returns></returns>
    public static bool IsSubsetOf<T>(this IEnumerable<T> enumerable, IEnumerable<T> items)
    {
        var hashSet = new HashSet<T>(enumerable);
        return hashSet.IsSubsetOf(items);
    }

    /// <summary>
    ///     Used for Linq queries to determines whether an element is a subset of the specified collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <param name="items"></param>
    /// <returns></returns>
    public static bool IsSubsetOf<T>(this IEnumerable<T> enumerable, params T[] items)
    {
        var hashSet = new HashSet<T>(enumerable);
        return hashSet.IsSubsetOf(items);
    }

    /// <summary>
    ///     Used for Linq queries to determines whether an element is a superset of the specified collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <param name="items"></param>
    /// <returns></returns>
    public static bool IsSupersetOf<T>(this IEnumerable<T> enumerable, IEnumerable<T> items)
    {
        var hashSet = new HashSet<T>(enumerable);
        return hashSet.IsSupersetOf(items);
    }

    /// <summary>
    ///     Used for Linq queries to match on empty child collections
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <returns></returns>
    public static bool IsEmpty<T>(this IEnumerable<T>? enumerable)
    {
        if (enumerable == null)
        {
            return true;
        }

        if (enumerable is string)
        {
            return string.IsNullOrEmpty(enumerable.As<string>());
        }

        return !enumerable.Any();
    }

    /// <summary>
    ///     Query across any and all tenants
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="variable"></param>
    /// <returns></returns>
    public static bool AnyTenant<T>(this T variable)
    {
        throw new NotSupportedException(
            $"{nameof(AnyTenant)} extension method can only be used in Marten Linq queries.");
    }

    /// <summary>
    ///     Query for the range of supplied tenants
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="variable"></param>
    /// <param name="tenantIds"></param>
    /// <returns></returns>
    public static bool TenantIsOneOf<T>(this T variable, params string[] tenantIds)
    {
        throw new NotSupportedException(
            $"{nameof(TenantIsOneOf)} extension method can only be used in Marten Linq queries.");
    }

    /// <summary>
    ///     Performs a full text search against <typeparamref name="TDoc" />
    /// </summary>
    /// <param name="searchTerm">
    ///     The text to search for.  May contain lexeme patterns used by PostgreSQL for full text
    ///     searching
    /// </param>
    /// <remarks>
    ///     See: https://www.postgresql.org/docs/10/static/textsearch-controls.html#TEXTSEARCH-PARSING-QUERIES
    /// </remarks>
    public static bool Search<T>(this T variable, string searchTerm)
    {
        throw new NotSupportedException($"{nameof(Search)} extension method can only be used in Marten Linq queries.");
    }

    /// <summary>
    ///     Performs a full text search against <typeparamref name="TDoc" />
    /// </summary>
    /// <param name="searchTerm">
    ///     The text to search for.  May contain lexeme patterns used by PostgreSQL for full text
    ///     searching
    /// </param>
    /// <param name="regConfig">
    ///     The dictionary config passed to the 'to_tsquery' function, must match the config parameter used
    ///     by <seealso cref="DocumentMapping.AddFullTextIndex(string)" />
    /// </param>
    /// <remarks>
    ///     See: https://www.postgresql.org/docs/10/static/textsearch-controls.html#TEXTSEARCH-PARSING-QUERIES
    /// </remarks>
    public static bool Search<T>(this T variable, string searchTerm, string regConfig)
    {
        throw new NotSupportedException($"{nameof(Search)} extension method can only be used in Marten Linq queries.");
    }

    /// <summary>
    ///     Performs a full text search against <typeparamref name="TDoc" /> using the 'plainto_tsquery' search function
    /// </summary>
    /// <param name="queryText">The text to search for.  May contain lexeme patterns used by PostgreSQL for full text searching</param>
    /// <remarks>
    ///     See: https://www.postgresql.org/docs/10/static/textsearch-controls.html#TEXTSEARCH-PARSING-QUERIES
    /// </remarks>
    public static bool PlainTextSearch<T>(this T variable, string searchTerm)
    {
        throw new NotSupportedException(
            $"{nameof(PlainTextSearch)} extension method can only be used in Marten Linq queries.");
    }

    /// <summary>
    ///     Performs a full text search against <typeparamref name="TDoc" /> using the 'plainto_tsquery' search function
    /// </summary>
    /// <param name="queryText">The text to search for.  May contain lexeme patterns used by PostgreSQL for full text searching</param>
    /// <param name="regConfig">
    ///     The dictionary config passed to the 'to_tsquery' function, must match the config parameter used
    ///     by <seealso cref="DocumentMapping.AddFullTextIndex(string)" />
    /// </param>
    /// <remarks>
    ///     See: https://www.postgresql.org/docs/10/static/textsearch-controls.html#TEXTSEARCH-PARSING-QUERIES
    /// </remarks>
    public static bool PlainTextSearch<T>(this T variable, string searchTerm, string regConfig)
    {
        throw new NotSupportedException(
            $"{nameof(PlainTextSearch)} extension method can only be used in Marten Linq queries.");
    }

    /// <summary>
    ///     Performs a full text search against <typeparamref name="TDoc" /> using the 'phraseto_tsquery' search function
    /// </summary>
    /// <param name="queryText">The text to search for.  May contain lexeme patterns used by PostgreSQL for full text searching</param>
    /// <remarks>
    ///     See: https://www.postgresql.org/docs/10/static/textsearch-controls.html#TEXTSEARCH-PARSING-QUERIES
    /// </remarks>
    public static bool PhraseSearch<T>(this T variable, string searchTerm)
    {
        throw new NotSupportedException(
            $"{nameof(PhraseSearch)} extension method can only be used in Marten Linq queries.");
    }

    /// <summary>
    ///     Performs a full text search against <typeparamref name="TDoc" /> using the 'phraseto_tsquery' search function
    /// </summary>
    /// <param name="queryText">The text to search for.  May contain lexeme patterns used by PostgreSQL for full text searching</param>
    /// <param name="regConfig">
    ///     The dictionary config passed to the 'to_tsquery' function, must match the config parameter used
    ///     by <seealso cref="DocumentMapping.AddFullTextIndex(string)" />
    /// </param>
    /// <remarks>
    ///     See: https://www.postgresql.org/docs/10/static/textsearch-controls.html#TEXTSEARCH-PARSING-QUERIES
    /// </remarks>
    public static bool PhraseSearch<T>(this T variable, string searchTerm, string regConfig)
    {
        throw new NotSupportedException(
            $"{nameof(PhraseSearch)} extension method can only be used in Marten Linq queries.");
    }

    /// <summary>
    ///     Performs a full text search against <typeparamref name="T" /> using the 'websearch_to_tsquery' search function
    /// </summary>
    /// <param name="searchTerm">
    ///     The text to search for.  Uses an alternative syntax to the other search functions, similar to
    ///     the one used by web search engines
    /// </param>
    /// <remarks>
    ///     Supported from Postgres 11
    ///     See: https://www.postgresql.org/docs/11/textsearch-controls.html#TEXTSEARCH-PARSING-QUERIES
    /// </remarks>
    public static bool WebStyleSearch<T>(this T variable, string searchTerm)
    {
        throw new NotSupportedException(
            $"{nameof(WebStyleSearch)} extension method can only be used in Marten Linq queries.");
    }

    /// <summary>
    ///     Performs a full text search against <typeparamref name="T" /> using the 'websearch_to_tsquery' search function
    /// </summary>
    /// <param name="searchTerm">
    ///     The text to search for.  Uses an alternative syntax to the other search functions, similar to
    ///     the one used by web search engines
    /// </param>
    /// <param name="regConfig">
    ///     The dictionary config passed to the 'websearch_to_tsquery' function, must match the config
    ///     parameter used by <seealso cref="DocumentMapping.AddFullTextIndex(string)" />
    /// </param>
    /// <remarks>
    ///     Supported from Postgres 11
    ///     See: https://www.postgresql.org/docs/11/textsearch-controls.html#TEXTSEARCH-PARSING-QUERIES
    /// </remarks>
    public static bool WebStyleSearch<T>(this T variable, string searchTerm, string regConfig)
    {
        throw new NotSupportedException(
            $"{nameof(WebStyleSearch)} extension method can only be used in Marten Linq queries.");
    }

    /// <summary>
    ///     Performs a ngram search against <typeparamref name="T" /> using a custom ngram search function
    /// </summary>
    /// <param name="searchTerm">The text to search for.</param>
    public static bool NgramSearch<T>(this T variable, string searchTerm)
    {
        throw new NotSupportedException(
            $"{nameof(NgramSearch)} extension method can only be used in Marten Linq queries.");
    }
}
