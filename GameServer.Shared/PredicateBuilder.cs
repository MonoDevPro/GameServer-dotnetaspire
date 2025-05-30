using System.Linq.Expressions;

namespace GameServer.Shared;

/// <summary>
/// Helper for predicates building
/// </summary>
public static class PredicateBuilder
{
    /// <summary>
    /// Just a stub to start build predicate with True conditions
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static Expression<Func<T, bool>> True<T>() { return f => true; }

    /// <summary>
    /// Just a stub to start build predicate with False conditions
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static Expression<Func<T, bool>> False<T>() { return f => false; }

    /// <summary>
    /// Condition 'Or' for build predicate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
    }

    /// <summary>
    /// Condition 'And' for build predicate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
    }
}