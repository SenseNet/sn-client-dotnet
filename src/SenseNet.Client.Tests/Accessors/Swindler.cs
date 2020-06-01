using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace SenseNet.Tests.Accessors
{
    public class Swindler<T> : IDisposable
    {
        private readonly T _original;
        private readonly Action<T> _setter;
        public Swindler(T hack, Func<T> getter, Action<T> setter)
        {
            _original = getter();
            _setter = setter;
            setter(hack);
        }

        public void Dispose()
        {
            _setter(_original);
        }
    }
}
