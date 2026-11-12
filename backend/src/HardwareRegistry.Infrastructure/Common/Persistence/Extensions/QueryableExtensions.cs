using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using HardwareRegistry.Application.Common;

namespace HardwareRegistry.Infrastructure.Common.Persistence.Extensions
{
    public static class QueryableExtensions
    {
        /// <summary>
        /// Escape character used for ILIKE patterns so that user input cannot inject wildcards.
        /// </summary>
        private const string LikeEscapeCharacter = "\\";

        /// <summary>
        /// Cached reference to <c>EF.Functions.ILike(matchExpression, pattern, escapeCharacter)</c>.
        /// Resolved once, so a future overload change breaks here instead of at query build time.
        /// </summary>
        private static readonly MethodInfo ILikeMethod = typeof(NpgsqlDbFunctionsExtensions).GetMethod(
            nameof(NpgsqlDbFunctionsExtensions.ILike),
            BindingFlags.Public | BindingFlags.Static,
            [typeof(DbFunctions), typeof(string), typeof(string), typeof(string)]
        ) ?? throw new InvalidOperationException(
            "EF.Functions.ILike(matchExpression, pattern, escapeCharacter) was not found."
        );

        extension<TSource>(IQueryable<TSource> source) where TSource : class
        {
            /// <summary>
            /// Filters the query by a whitespace-separated search string using case-insensitive
            /// PostgreSQL <c>ILIKE</c> matching.
            /// </summary>
            /// <param name="likeTerms">
            /// Free-form search input, split on any whitespace. Wildcard characters are escaped,
            /// so the input is always matched literally.
            /// </param>
            /// <param name="matchExpressions">Columns to search in.</param>
            /// <returns>
            /// The source query when there is nothing to search for; otherwise a query where
            /// every term must match at least one column (terms are AND-ed, columns OR-ed).
            /// </returns>
            public IQueryable<TSource> Like(
                string? likeTerms,
                params Expression<Func<TSource, string?>>[] matchExpressions
            )
            {
                if (String.IsNullOrWhiteSpace(likeTerms) || matchExpressions is null or []) return source;

                var terms = likeTerms.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

                var query = source;

                foreach (var term in terms)
                {
                    var pattern = Expression.Property(
                        Expression.Constant(
                            new PatternHolder($"%{QueryableExtensions.EscapeLikePattern(term)}%")
                        ),
                        nameof(PatternHolder.Value)
                    );

                    var parameter = Expression.Parameter(typeof(TSource), "e");

                    Expression? orExpression = null;
                    foreach (var matchExpression in matchExpressions)
                    {
                        var replacedBody = new ReplaceParameterVisitor(matchExpression.Parameters[0], parameter)
                            .Visit(matchExpression.Body);

                        var likeCall = Expression.Call(
                            QueryableExtensions.ILikeMethod,
                            Expression.Property(null, typeof(EF), nameof(EF.Functions)),
                            replacedBody,
                            pattern,
                            Expression.Constant(QueryableExtensions.LikeEscapeCharacter)
                        );

                        orExpression = orExpression is null
                            ? likeCall
                            : Expression.OrElse(orExpression, likeCall);
                    }

                    var lambda = Expression.Lambda<Func<TSource, bool>>(orExpression!, parameter);

                    query = query.Where(lambda);
                }

                return query;
            }

            /// <summary>
            /// Orders the query by <paramref name="keySelector"/> in the requested direction.
            /// </summary>
            public IOrderedQueryable<TSource> OrderBy<TKey>(
                SortOrder order,
                Expression<Func<TSource, TKey>> keySelector
            ) => order is SortOrder.Descending
                ? source.OrderByDescending(keySelector)
                : source.OrderBy(keySelector);
        }

        extension<TSource>(IOrderedQueryable<TSource> source)
        {
            /// <summary>
            /// Adds a secondary ordering by <paramref name="keySelector"/> in the requested direction.
            /// </summary>
            public IOrderedQueryable<TSource> ThenBy<TKey>(
                SortOrder order,
                Expression<Func<TSource, TKey>> keySelector
            ) => order is SortOrder.Descending
                ? source.ThenByDescending(keySelector)
                : source.ThenBy(keySelector);
        }

        /// <summary>
        /// Escapes ILIKE wildcards so that a search term is matched literally.
        /// </summary>
        private static string EscapeLikePattern(string term)
            => term
                .Replace(
                    QueryableExtensions.LikeEscapeCharacter,
                    QueryableExtensions.LikeEscapeCharacter + QueryableExtensions.LikeEscapeCharacter
                )
                .Replace("%", QueryableExtensions.LikeEscapeCharacter + "%")
                .Replace("_", QueryableExtensions.LikeEscapeCharacter + "_");

        /// <summary>
        /// Wraps a search pattern so that it appears as a member access rather than a bare constant
        /// in the expression tree. EF Core lifts such nodes into SQL parameters instead of inlining
        /// them as literals, which keeps the PostgreSQL plan cache effective across different search terms.
        /// </summary>
        private sealed class PatternHolder(string value)
        {
            public string Value { get; } = value;
        }

        private sealed class ReplaceParameterVisitor(ParameterExpression from, Expression to) : ExpressionVisitor
        {
            private readonly ParameterExpression _from = from;
            private readonly Expression _to = to;

            protected override Expression VisitParameter(ParameterExpression node)
                => node == this._from ? this._to : base.VisitParameter(node);
        }
    }
}