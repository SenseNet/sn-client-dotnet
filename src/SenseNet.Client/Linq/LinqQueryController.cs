using System;
using System.Text;
using SenseNet.Client.Linq.Predicates;

namespace SenseNet.Client.Linq
{
    public partial class LinqQuery
    {
        internal bool FiltersPrepared { get; private set; }

        /// <summary>
        /// Defines a query predicate that represents all index documents.
        /// </summary>
        public static SnQueryPredicate FullSetPredicate { get; } = new RangePredicate("Id", new IndexValue(0), null, true, false);


        /// <summary>
        /// Replaces the top level predicate to a new LogicalPredicate that
        ///  contains the original top level predicate and a given predicate
        ///  encapsulated by two individual LogicalClause with "Must" occurence.
        /// </summary>
        public void AddAndClause(SnQueryPredicate clause)
        {
            AddClause(clause, Occurence.Must);
        }
        /// <summary>
        /// Replaces the top level predicate to a new LogicalPredicate that
        ///  contains the original top level predicate and a given predicate
        ///  encapsulated by two individual LogicalClause with "Should" occurence.
        /// </summary>
        public void AddOrClause(SnQueryPredicate clause)
        {
            AddClause(clause, Occurence.Should);
        }
        /// <summary>
        /// Replaces the top level predicate to a new LogicalPredicate that
        ///  contains the original top level predicate and a given predicate
        ///  encapsulated by two individual LogicalClause with the given occurence.
        /// </summary>
        public void AddClause(SnQueryPredicate clause, Occurence occurence)
        {
            QueryTree = new LogicalPredicate(new[]
            {
                new LogicalClause(QueryTree, occurence),
                new LogicalClause(clause, occurence),
            });
        }

        internal static LinqQuery ApplyVisitors(LinqQuery query)
        {
            var queryTree = query.QueryTree;

            var visitorTypes = SnQueryVisitor.VisitorExtensionTypes;
            if (visitorTypes == null || visitorTypes.Length == 0)
                return query;

            foreach (var visitorType in SnQueryVisitor.VisitorExtensionTypes)
            {
                var visitor = (SnQueryVisitor)Activator.CreateInstance(visitorType);
                queryTree = visitor.Visit(queryTree);
            }

            if (ReferenceEquals(queryTree, query.QueryTree))
                return query;

            var newQuery = Create(queryTree);

            newQuery.Querytext = query.Querytext;
            newQuery.Projection = query.Projection;
            newQuery.Top = query.Top;
            newQuery.Skip = query.Skip;
            newQuery.Sort = query.Sort;
            newQuery.EnableAutofilters = query.EnableAutofilters;
            newQuery.EnableLifespanFilter = query.EnableLifespanFilter;
            newQuery.CountOnly = query.CountOnly;
            newQuery.QueryExecutionMode = query.QueryExecutionMode;
            newQuery.AllVersions = query.AllVersions;
            newQuery.CountAllPages = query.CountAllPages;
            newQuery.ThrowIfEmpty = query.ThrowIfEmpty;
            newQuery.ExistenceOnly = query.ExistenceOnly;

            return newQuery;
        }

        private static bool IsAutofilterEnabled(FilterStatus value)
        {
            switch (value)
            {
                case FilterStatus.Default:
                    return EnableAutofiltersDefaultValue == FilterStatus.Enabled;
                case FilterStatus.Enabled:
                    return true;
                case FilterStatus.Disabled:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
        private static bool IsLifespanFilterEnabled(FilterStatus value)
        {
            switch (value)
            {
                case FilterStatus.Default:
                    return EnableLifespanFilterDefaultValue == FilterStatus.Enabled;
                case FilterStatus.Enabled:
                    return true;
                case FilterStatus.Disabled:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }


        //TODO: Part of 'CQL to SQL compiler' for future use.
        //private static void ValidateQuery<T>(IQueryResult<T> x, IQueryResult<T> y)
        //{
        //    executor = SearchProvider.GetExecutor(this);
        //    executor.Initialize(this, permissionChecker);
        //    result = Execute(executor);
        //    if (!(executor is LuceneQueryExecutor))
        //    {
        //        var fallbackExecutor = SearchProvider.GetFallbackExecutor(this);
        //        fallbackExecutor.Initialize(this, permissionChecker);
        //        var expectedResult = Execute(fallbackExecutor);
        //        AssertResultsAreEqual(expectedResult, result, fallbackExecutor.QueryString, executor.QueryString);
        //    }
        //}
        //protected void AssertResultsAreEqual(IEnumerable<LucObject> expected, IEnumerable<LucObject> actual, string cql, string sql)
        //{
        //    var exp = string.Join(",", expected.Select(x => x.NodeId).Distinct().OrderBy(y => y));
        //    var act = string.Join(",", actual.Select(x => x.NodeId).OrderBy(y => y));
        //    if (exp != act)
        //    {
        //        var msg = string.Format("VALIDATION: Results are different. Expected:{0}, actual:{1}, CQL:{2}, SQL:{3}", exp, act, cql, sql);
        //        SnTrace.Test.Write(msg);
        //        throw new Exception(msg);
        //    }
        //}


        /// <summary>
        /// Creates a new SnQuery instance by parsing the given CQL query
        ///  and context containing query settings.
        /// </summary>
        /// <param name="queryText"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static LinqQuery Parse(string queryText, IQueryContext context)
        {
            //return new CqlParser().Parse(queryText, context);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a LinqQuery instance from the given predicate.
        /// </summary>
        public static LinqQuery Create(SnQueryPredicate predicate)
        {
            return new LinqQuery { QueryTree = predicate };
        }

        /// <summary>
        /// Returns with string representation of the query.
        /// Contains the whole CQL query with predicates and extensions.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var visitor = new SnQueryToStringVisitor();
            visitor.Visit(QueryTree);
            var sb = new StringBuilder(visitor.Output);

            if (AllVersions)
                sb.Append(" ").Append(Cql.Keyword.AllVersions);
            if (CountOnly)
                sb.Append(" ").Append(Cql.Keyword.CountOnly);
            if (Top != 0 && Top != int.MaxValue)
                sb.Append(" ").Append(Cql.Keyword.Top).Append(":").Append(Top);
            if (Skip != 0)
                sb.Append(" ").Append(Cql.Keyword.Skip).Append(":").Append(Skip);
            if (HasSort)
            {
                foreach (var sortInfo in Sort)
                {
                    if (sortInfo.Reverse)
                        sb.Append(" ").Append(Cql.Keyword.ReverseSort).Append(":").Append(sortInfo.FieldName);
                    else
                        sb.Append(" ").Append(Cql.Keyword.Sort).Append(":").Append(sortInfo.FieldName);
                }
            }
            if (EnableAutofilters != FilterStatus.Default && EnableAutofilters != EnableAutofiltersDefaultValue)
                sb.Append(" ").Append(Cql.Keyword.Autofilters).Append(":").Append(EnableAutofiltersDefaultValue == FilterStatus.Enabled ? Cql.Keyword.Off : Cql.Keyword.On);
            if (EnableLifespanFilter != FilterStatus.Default && EnableLifespanFilter != EnableLifespanFilterDefaultValue)
                sb.Append(" ").Append(Cql.Keyword.Lifespan).Append(":").Append(EnableLifespanFilterDefaultValue == FilterStatus.Enabled ? Cql.Keyword.Off : Cql.Keyword.On);
            if (QueryExecutionMode == QueryExecutionMode.Quick)
                sb.Append(" ").Append(Cql.Keyword.Quick);

            return sb.ToString();
        }
    }
}
