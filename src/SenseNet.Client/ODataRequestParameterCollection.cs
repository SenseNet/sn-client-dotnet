using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Client
{
    public class ODataRequestParameterCollection : IList<KeyValuePair<string, string>>
    {
        private readonly List<KeyValuePair<string, string>> _list = new List<KeyValuePair<string, string>>();
        private IDictionary<string, string> _builtinParameters;

        public ODataRequestParameterCollection(IDictionary<string, string> builtinParameters)
        {
            _builtinParameters = builtinParameters;
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
            CheckItem(item);
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
            return _list.Remove(item);
        }

        public int Count => _list.Count;
        public bool IsReadOnly => false;
        public int IndexOf(KeyValuePair<string, string> item)
        {
            return _list.IndexOf(item);
        }
        public void Insert(int index, KeyValuePair<string, string> item)
        {
            CheckItem(item);
            _list.Insert(index, item);
        }
        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }
        public KeyValuePair<string, string> this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        private void CheckItem(KeyValuePair<string, string> item)
        {
            //UNDONE ? set the matching request property instead of throwing an error
            if(_builtinParameters.Keys.Contains(item.Key))
                throw new InvalidOperationException($"Invalid dynamic parameter usage. Use the \"{_builtinParameters[item.Key]}\" property of the ODataRequest.");
        }

    }
}
