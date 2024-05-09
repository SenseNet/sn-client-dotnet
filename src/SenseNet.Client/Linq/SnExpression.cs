using System;
using System.Linq.Expressions;
using SenseNet.Client.Linq.Predicates;

namespace SenseNet.Client.Linq;

public class QueryProperties
{
    public FilterStatus EnableAutofilters { get; set; }
    public FilterStatus EnableLifespanFilter { get; set; }
    public QueryExecutionMode QueryExecutionMode { get; set; }
    public string[] ExpandedFieldNames { get; set; }
    public string[] SelectedFieldNames { get; set; }
}

public class SnExpression
{
    public static LinqQuery BuildQuery(Expression expression, Type sourceCollectionItemType, QueryProperties queryProperties, IRepository repository)
    {
        return BuildSnQuery(expression, sourceCollectionItemType, queryProperties, repository, out var elementSelection);
    }
    public static LinqQuery BuildQuery(Expression expression, Type sourceCollectionItemType, QueryProperties queryProperties, IRepository repository, out string elementSelection)
    {
        return BuildSnQuery(expression, sourceCollectionItemType, queryProperties, repository, out elementSelection);
    }
    private static LinqQuery BuildSnQuery(Expression expression, Type sourceCollectionItemType, QueryProperties queryProperties, IRepository repository, out string elementSelection)
    {
        SnQueryPredicate q0 = null;
        elementSelection = null;

        SnLinqVisitor v = null;
        // #1 compiling linq expression
        if (expression != null)
        {
            var v1 = new SetExecVisitor();
            var expr1 = v1.Visit(expression);
            var expr2 = expr1;
            if (v1.HasParameter)
            {
                var v2 = new ExecutorVisitor(v1.GetExpressions());
                expr2 = v2.Visit(expr1);
            }
            v = new SnLinqVisitor(repository);
            v.Visit(expr2);
            q0 = v.GetPredicate(sourceCollectionItemType);
            elementSelection = v.ElementSelection;
        }

        // #4 empty query substitution
        if (q0 == null)
            q0 = LinqQuery.FullSetPredicate;

        var q1 = OptimizeBooleans(q0);

        // #5 configuring query by linq expression (the smallest priority)
        var query = LinqQuery.Create(q1);
        if (v != null)
        {
            query.Skip = v.Skip;
            query.Top = v.Top;
            query.CountOnly = v.CountOnly;
            query.Sort = v.Sort.ToArray();
            query.ThrowIfEmpty = v.ThrowIfEmpty;
            query.ExistenceOnly = v.ExistenceOnly;
            query.EnableAutofilters = queryProperties.EnableAutofilters;
            query.EnableLifespanFilter = queryProperties.EnableLifespanFilter;
            query.QueryExecutionMode = queryProperties.QueryExecutionMode;
            //UNDONE:LINQ: set projection from the SnLinqVisitor
            //queryProperties.ExpandedFieldNames = v.ExpandedFields;
            //queryProperties.SelectedFieldNames = v.SelectedFields;
        }

        return query;
    }

    internal static SnQueryPredicate OptimizeBooleans(SnQueryPredicate predicate)
    {
        var v = new OptimizeBooleansVisitor();
        var optimizedPredicate = v.Visit(predicate);
        if (!(optimizedPredicate is LogicalPredicate logicalPredicate))
            return optimizedPredicate;
        var clauses = logicalPredicate.Clauses;
        if (clauses.Count != 1)
            return logicalPredicate;
        if (clauses[0].Occur != Occurence.MustNot)
            return logicalPredicate;
        logicalPredicate.Clauses.Add(new LogicalClause(LinqQuery.FullSetPredicate, Occurence.Must));
        return logicalPredicate;
    }

    internal static Exception CallingAsEnumerableExpectedError(string methodName, Exception innerException = null)
    {
        var message = $"Cannot resolve an expression. Use 'AsEnumerable()' method before calling the '{methodName}' method. {innerException?.Message ?? string.Empty}";
        return new NotSupportedException(message);
    }
    internal static Exception CallingAsEnumerableExpectedError(Expression expression)
    {
        return new NotSupportedException($"Cannot resolve an expression: Use 'AsEnumerable()' method before using the '{expression}' expression.");
    }
}