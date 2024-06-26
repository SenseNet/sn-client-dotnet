using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

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
        protected string[]? ExpandedFieldNames { get; set; }
        protected string[]? SelectedFieldNames { get; set; }

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
            var request = GetODataRequest();
            var result = _repository.QueryAsync(request, CancellationToken.None).GetAwaiter().GetResult();
            return result.Cast<T>().GetEnumerator();
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
            var request = GetODataRequest(expression, out var elementSelection, out var throwIfEmpty, out var countOnly, out var existenceOnly);
            var result = _repository.QueryAsync(request, CancellationToken.None).GetAwaiter().GetResult();
            //var resultArray = result.Cast<TResult>().ToArray();
            var count = result.TotalCount;

            if (count == 0)
            {
                if (throwIfEmpty)
                {
                    if (elementSelection == ElementSelection.ElementAt)
                        // ReSharper disable once NotResolvedInText
                        throw new ArgumentOutOfRangeException("Index was out of range.");
                    throw new InvalidOperationException("Sequence contains no elements.");
                }
                return default;
            }
            if (countOnly)
            {
                if (existenceOnly)
                    return (TResult)Convert.ChangeType(count > 0, typeof(TResult));
                return (TResult)Convert.ChangeType(count, typeof(TResult));
            }

            switch (elementSelection)
            {
                case ElementSelection.None:
                    break;
                case ElementSelection.First:
                    return (TResult) Convert.ChangeType(result.First(), typeof(TResult));
                case ElementSelection.Single:
                    return (TResult)Convert.ChangeType(result.Single(), typeof(TResult));
                case ElementSelection.Last:
                    return (TResult)Convert.ChangeType(result.Last(), typeof(TResult));
                case ElementSelection.ElementAt:
                    var any = result.Any();
                    if (!any)
                    {
                        if (throwIfEmpty)
                            // ReSharper disable once NotResolvedInText
                            throw new ArgumentOutOfRangeException("Index was out of range.");
                        return default;
                    }

                    return (TResult) Convert.ChangeType(result.First(), typeof(TResult));
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
            return Compile(Expression, out var _, out var _, out var _, out var _);
        }

        public QueryContentRequest GetODataRequest()
        {
            return GetODataRequest(this.Expression, out var _, out var _, out var _, out var _);
        }
        private QueryContentRequest GetODataRequest(Expression expression, out ElementSelection elementSelection, out bool throwIfEmpty, out bool countOnly, out bool existenceOnly)
        {
            var query = Compile(expression, out elementSelection, out throwIfEmpty, out countOnly, out existenceOnly);

            return new QueryContentRequest
            {
                ContentQuery = query.ToString(),
                AutoFilters = Autofilters,
                LifespanFilter = LifespanFilter,
                //ContentId = Constants.Repository.RootId,
                Expand = ExpandedFieldNames,
                Select = SelectedFieldNames,
                //CountOnly = CountOnlyEnabled,
                InlineCount = InlineCountOptions.AllPages // always active
            };
        }

        private readonly string[] NullArray = new[] { "[null]" };
        private LinqQuery Compile(Expression expression, out ElementSelection elementSelection, out bool throwIfEmpty, out bool countOnly, out bool existenceOnly)
        {
            var query = SnExpression.BuildQuery(expression, typeof(T), _repository, out elementSelection, out throwIfEmpty, out countOnly, out existenceOnly);
            query.EnableAutofilters = this.Autofilters;
            query.EnableLifespanFilter = this.LifespanFilter;
            query.QueryExecutionMode = this.QueryExecutionMode;
            if (ExpandedFieldNames == null)
                ExpandedFieldNames = query.ExpandedFieldNames;
            if (SelectedFieldNames == null)
                SelectedFieldNames = query.SelectedFieldNames;

            _tracer?.AddTrace($"Expression: {expression}");
            _tracer?.AddTrace($"Properties: AutoFilters: {query.EnableAutofilters}, " +
                              $"Lifespan: {query.EnableLifespanFilter}, " +
                              $"Mode: {query.QueryExecutionMode}, " +
                              $"Expand: {string.Join(",", ExpandedFieldNames ?? NullArray)}, " +
                              $"Select: {string.Join(",", SelectedFieldNames ?? NullArray)}");
            _tracer?.AddTrace($"Compiled: {query}");

            return query;
        }
    }
}
