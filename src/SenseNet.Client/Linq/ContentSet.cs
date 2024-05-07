using AngleSharp.Dom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

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
        private ContentSet<Q> Clone<Q>(Expression expression)
        {
            if (typeof(Content) != typeof(Q))
                this.TypeFilter = typeof(Q);
            if (typeof(Q) == typeof(Content) || typeof(Node).IsAssignableFrom(typeof(Q)))
                return new ContentSet<Q>(_repository, expression)
                {
                    CountOnlyEnabled = this.CountOnlyEnabled,
                    HeadersOnlyEnabled = this.HeadersOnlyEnabled,
                    TypeFilter = this.TypeFilter
                };
            if (expression is MethodCallExpression callExpr)
            {
                var lastMethodName = callExpr.Method.Name;
                throw SnExpression.CallingAsEnunerableExpectedError(lastMethodName);
            }
            throw SnExpression.CallingAsEnunerableExpectedError(expression);
        }

        public object? Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotImplementedException();
        }

        public Type ElementType => throw new SnNotSupportedException("SnLinq: ContentSet.ElementType");
        public Expression Expression => _expression ??= Expression.Constant(this);
        public IQueryProvider Provider => this;
        public ISnQueryable<T> CountOnly()
        {
            throw new NotImplementedException();
        }

        public ISnQueryable<T> HeadersOnly()
        {
            throw new NotImplementedException();
        }

        public ISnQueryable<T> EnableAutofilters()
        {
            throw new NotImplementedException();
        }

        public ISnQueryable<T> DisableAutofilters()
        {
            throw new NotImplementedException();
        }

        public ISnQueryable<T> EnableLifespan()
        {
            throw new NotImplementedException();
        }

        public ISnQueryable<T> DisableLifespan()
        {
            throw new NotImplementedException();
        }

        public LinqQuery GetCompiledQuery()
        {
            return SnExpression.BuildQuery(this.Expression, typeof(T), _repository);
        }

    }
}
