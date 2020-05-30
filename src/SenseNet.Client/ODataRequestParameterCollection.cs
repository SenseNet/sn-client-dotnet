using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Client
{
    public class ODataRequestParameterCollection : ICollection<KeyValuePair<string, string>>
    {
        private readonly List<KeyValuePair<string, string>> _list = new List<KeyValuePair<string, string>>();
        private readonly Func<KeyValuePair<string, string>, bool> _addWellKnownItem;
        private readonly Func<KeyValuePair<string, string>, bool> _removeWellKnownItem;

        internal ODataRequestParameterCollection(Func<KeyValuePair<string, string>, bool> addWellKnownItem,
            Func<KeyValuePair<string, string>, bool> removeWellKnownItem)
        {
            _addWellKnownItem = addWellKnownItem;
            _removeWellKnownItem = removeWellKnownItem;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(string name, string value)
        {
            var item = new KeyValuePair<string, string>(name, value);
            Add(item);
        }
        public void Add(KeyValuePair<string, string> item)
        {
            if (!_addWellKnownItem(item))
                _list.Add(item);
        }
        public void Clear()
        {
            _list.Clear();
        }
        public bool Contains(KeyValuePair<string, string> item)
        {
            return _list.Contains(item);
        }
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }
        public bool Remove(string name)
        {
            var items = _list.Where(x => x.Key == name).ToArray();
            foreach (var item in items)
                _list.Remove(item);
            return items.Length > 0;
        }
        public bool Remove(KeyValuePair<string, string> item)
        {
            return _removeWellKnownItem(item) || _list.Remove(item);
        }
        public int Count => _list.Count;
        public bool IsReadOnly => false;
    }
}
