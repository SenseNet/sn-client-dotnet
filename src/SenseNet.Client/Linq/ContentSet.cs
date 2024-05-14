using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

/*
Name            QueryString             LINQ Method
--------------- ----------------------- ----------------------------------------------------
Top             $top                    Take
Skip            $skip                   Skip
Expand          $expand                 ?
Select          $select                 ?
Filter          $filter                 _not_supported_
OrderBy         $orderby                OrderBy, ThenBy, OrderByDescending, ThenByDescending
InlineCount     $inlinecount            ?
Format          $format                 ?

CountOnly       $count                  ?

Metadata        metadata                ?
AutoFilters     enableautofilters       EnableAutofilters, DisableAutofilters
LifespanFilter  enablelifespanfilter    EnableLifespan, DisableLifespan
Version         version                 _not_supported_
Scenario        scenario                _not_supported_

ContentQuery    query                   COMPILED QUERY
Permissions     permissions             _not_supported_
User            user                    _not_supported_
*/

namespace SenseNet.Client.Linq
{
    public interface ISnQueryable<T> : IOrderedQueryable<T>
    {
        ISnQueryable<T> CountOnly();
        ISnQueryable<T> HeadersOnly();
        ISnQueryable<T> EnableAutofilters();
        ISnQueryable<T> DisableAutofilters();
        ISnQueryable<T> EnableLifespan();
        ISnQueryable<T> DisableLifespan();
        ISnQueryable<T> ExpandFields(params string[] expandedFieldNames);
        ISnQueryable<T> SelectFields(params string[] selectedFieldNames);
        ISnQueryable<T> SetTracer(ILinqTracer tracer);
    }

    public class ContentSet<T> : IOrderedEnumerable<T>, IQueryProvider, ISnQueryable<T>
    {
        protected internal bool CountOnlyEnabled { get; protected set; }
        protected internal bool HeadersOnlyEnabled { get; protected set; }
        protected internal FilterStatus Autofilters { get; protected set; }
        protected internal FilterStatus LifespanFilter { get; protected set; }
        protected internal QueryExecutionMode QueryExecutionMode { get; protected set; }
        protected string[] ExpandedFieldNames { get; set; }
        protected string[] SelectedFieldNames { get; set; }

        protected internal Type TypeFilter { get; protected set; }

        private ILinqTracer? _tracer;
        private IRepository _repository;
        private Expression _expression;

        public ContentSet(IRepository repository)
        {
            _repository = repository;
        }
        internal ContentSet(IRepository repository, Expression expression)
        {
            _repository = repository;
            _expression = expression;
        }

        public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer, bool descending)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return CreateQuery<Content>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return Clone<TElement>(expression);
        }

        // ReSharper disable once StaticMemberInGenericType
        private static readonly string[] ForbiddenMethodNames =
        {
            "SelectMany", "Join", "Cast", "Concat", "Distinct", "Except", "Intersect", "Union", "SkipWhile", "TakeWhile",
            "DefaultIfEmpty", "Reverse", "Zip"
        };
        private ContentSet<Q> Clone<Q>(Expression expression)
        {
            if (typeof(Content) != typeof(Q))
                this.TypeFilter = typeof(Q);
            MethodCallExpression? callExpression = null;
            if (expression is MethodCallExpression callExpr)
            {
                callExpression = callExpr;
                if(ForbiddenMethodNames.Contains(callExpr.Method.Name))
                    throw SnExpression.CallingAsEnumerableExpectedError(callExpr.Method.Name);
            }
            if (typeof(Q) == typeof(Content) || typeof(Content).IsAssignableFrom(typeof(Q)))
                return new ContentSet<Q>(_repository, expression)
                {
                    CountOnlyEnabled = this.CountOnlyEnabled,
                    HeadersOnlyEnabled = this.HeadersOnlyEnabled,
                    Autofilters = this.Autofilters,
                    LifespanFilter = this.LifespanFilter,
                    QueryExecutionMode = this.QueryExecutionMode,
                    ExpandedFieldNames = this.ExpandedFieldNames,
                    SelectedFieldNames = this.SelectedFieldNames,
                    TypeFilter = this.TypeFilter,

                    _tracer = this._tracer
                };
            if (callExpression != null)
            {
                var lastMethodName = callExpression.Method.Name;
                throw SnExpression.CallingAsEnumerableExpectedError(lastMethodName);
            }
            throw SnExpression.CallingAsEnumerableExpectedError(expression);
        }

        public object? Execute(Expression expression)
        {
            throw new SnNotSupportedException("SnLinq: ContentSet.Execute");
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var count = 0;

            //// in case there is a predefined list of nodes, we do not execute a query (but we still need to build it)
            //if (!this.ExecuteQuery)
            //    count = ChildrenDefinition.BaseCollection.Count();

            var queryProperties = new QueryProperties
            {
                EnableAutofilters = Autofilters,
                EnableLifespanFilter = LifespanFilter,
                QueryExecutionMode = QueryExecutionMode,
                ExpandedFieldNames = ExpandedFieldNames,
                SelectedFieldNames = SelectedFieldNames
            };
            var query = SnExpression.BuildQuery(expression, typeof(T), queryProperties, _repository);

            throw new NotImplementedException();
        }

        public Type ElementType => throw new SnNotSupportedException("SnLinq: ContentSet.ElementType");
        public Expression Expression => _expression ??= Expression.Constant(this);
        public IQueryProvider Provider => this;

        // -------------------------------------------- ISnQueryable
        public ISnQueryable<T> CountOnly()
        {
            this.CountOnlyEnabled = true;
            return this;
        }
        public ISnQueryable<T> HeadersOnly()
        {
            this.HeadersOnlyEnabled = true;
            return this;
        }
        public ISnQueryable<T> EnableAutofilters()
        {
            this.Autofilters = FilterStatus.Enabled;
            return this;
        }
        public ISnQueryable<T> DisableAutofilters()
        {
            this.Autofilters = FilterStatus.Disabled;
            return this;
        }
        public ISnQueryable<T> EnableLifespan()
        {
            this.LifespanFilter = FilterStatus.Enabled;
            return this;
        }
        public ISnQueryable<T> DisableLifespan()
        {
            this.LifespanFilter = FilterStatus.Disabled;
            return this;
        }
        public ISnQueryable<T> SetExecutionMode(QueryExecutionMode executionMode)
        {
            this.QueryExecutionMode = executionMode;
            return this;
        }
        /// <summary>
        /// Gets or sets expanded field names. Use '/' separator for deeper expansions e.g. "CreatedBy/Manager"
        /// </summary>
        public ISnQueryable<T> ExpandFields(params string[] expandedFieldNames)
        {
            this.ExpandedFieldNames = expandedFieldNames;
            return this;
        }
        /// <summary>
        /// Gets or sets selected field names. Use '/' separator for deeper selection e.g. "CreatedBy/Manager/Name"
        /// </summary>
        public ISnQueryable<T> SelectFields(params string[] selectedFieldNames)
        {
            this.SelectedFieldNames = selectedFieldNames;
            return this;
        }

        public ISnQueryable<T> SetTracer(ILinqTracer tracer)
        {
            _tracer = tracer;
            return this;
        }

        public LinqQuery GetCompiledQuery()
        {
            var queryProperties = new QueryProperties
            {
                EnableAutofilters = Autofilters,
                EnableLifespanFilter = LifespanFilter,
                QueryExecutionMode = QueryExecutionMode
            };
            var query =  SnExpression.BuildQuery(this.Expression, typeof(T), queryProperties, _repository);
            _tracer?.AddTrace($"Compiled: {query}");
            return query;
        }

        public ODataRequest GetODataRequest()
        {
            var queryProperties = new QueryProperties
            {
                EnableAutofilters = Autofilters,
                EnableLifespanFilter = LifespanFilter,
                QueryExecutionMode = QueryExecutionMode
            };
            var query = SnExpression.BuildQuery(this.Expression, typeof(T), queryProperties, _repository);

            return new ODataRequest(_repository.Server)
            {
                ContentQuery = query.ToString(),
                AutoFilters = Autofilters,
                LifespanFilter = LifespanFilter,
                ContentId = Constants.Repository.RootId,
                Expand = ExpandedFieldNames,
                Select = SelectedFieldNames,
                CountOnly = CountOnlyEnabled,
                InlineCount = InlineCountOptions.AllPages // always active
            };
        }
    }
}
