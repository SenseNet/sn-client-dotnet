using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
    }

    public class ContentSet<T> : IOrderedEnumerable<T>, IQueryProvider, ISnQueryable<T>
    {
        protected internal bool CountOnlyEnabled { get; protected set; }
        protected internal bool HeadersOnlyEnabled { get; protected set; }
        protected internal FilterStatus Autofilters { get; protected set; }
        protected internal FilterStatus LifespanFilter { get; protected set; }
        protected internal QueryExecutionMode QueryExecutionMode { get; protected set; }

        protected internal Type TypeFilter { get; protected set; }


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

        private readonly string[] _forbiddenMethodNames = {"SelectMany", "Average", "Min", "Max", "SkipWhile", "TakeWhile" };
        private ContentSet<Q> Clone<Q>(Expression expression)
        {
            if (typeof(Content) != typeof(Q))
                this.TypeFilter = typeof(Q);
            MethodCallExpression? callExpression = null;
            if (expression is MethodCallExpression callExpr)
            {
                callExpression = callExpr;
                if(_forbiddenMethodNames.Contains(callExpr.Method.Name))
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
                    TypeFilter = this.TypeFilter
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
                QueryExecutionMode = QueryExecutionMode
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

        public LinqQuery GetCompiledQuery()
        {
            var queryProperties = new QueryProperties
            {
                EnableAutofilters = Autofilters,
                EnableLifespanFilter = LifespanFilter,
                QueryExecutionMode = QueryExecutionMode
            };
            return SnExpression.BuildQuery(this.Expression, typeof(T), queryProperties, _repository);
        }

    }
}
