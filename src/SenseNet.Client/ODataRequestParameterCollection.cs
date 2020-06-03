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

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// Does not include the well-known items that have been set.
        /// </summary>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// Does not include the well-known items that have been set.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds an item to the collection.
        /// If the item is well known, sets the matching property of the current <see cref="ODataRequest"/>.
        /// </summary>
        /// <param name="name">Name of the parameter.</param>
        /// <param name="value">Value of the parameter.</param>
        public void Add(string name, string value)
        {
            var item = new KeyValuePair<string, string>(name, value);
            Add(item);
        }
        /// <summary>
        /// Adds an item to the collection.
        /// If the item is well known, sets the matching property of the current <see cref="ODataRequest"/>.
        /// </summary>
        /// <param name="item">The object to add to the collection.</param>
        public void Add(KeyValuePair<string, string> item)
        {
            if (!_addWellKnownItem(item))
                _list.Add(item);
        }
        public void Clear()
        {
            _list.Clear();
        }
        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// Does not affect the well-known items that have been set.
        /// </summary>
        /// <param name="item">The object to locate in the collection.</param>
        /// <returns>true if <paramref name="item">item</paramref> is found in the collection; otherwise, false.</returns>
        public bool Contains(KeyValuePair<string, string> item)
        {
            return _list.Contains(item);
        }
        /// <summary>
        /// Copies the elements of the collection to an Array, starting at a particular Array index.
        /// Does not copy the well-known items that have been set.
        /// </summary>
        /// <param name="array">The Array that is the destination of the elements copied from collection.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Removes all occurrences of a specific object from the collection.
        /// If the item is well known, resets the matching property of the current <see cref="ODataRequest"/>.
        /// </summary>
        /// <param name="name">Items with this name should be deleted.</param>
        /// <returns>true if the items were successfully removed from the collection; otherwise, false.</returns>
        public bool Remove(string name)
        {
            if (_removeWellKnownItem(new KeyValuePair<string, string>(name, null)))
                return true;

            var items = _list.Where(x => x.Key == name).ToArray();
            foreach (var item in items)
                _list.Remove(item);
            return items.Length > 0;
        }
        /// <summary>
        /// Removes the first occurrence of a specific object from the collection.
        /// If the item is well known, resets the matching property of the current <see cref="ODataRequest"/>
        /// by item's Key regardless of the item's Value.
        /// </summary>
        /// <param name="item">The object to remove from the collection.</param>
        /// <returns>true if <paramref name="item">item</paramref> was successfully removed from the collection; otherwise, false. This method also returns false if <paramref name="item">item</paramref> is not found in the original collection.</returns>
        public bool Remove(KeyValuePair<string, string> item)
        {
            return _removeWellKnownItem(item) || _list.Remove(item);
        }
        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// Does not include the number of well-known items that have been set.
        /// </summary>
        public int Count => _list.Count;
        /// <summary>
        /// Always false.
        /// </summary>
        public bool IsReadOnly => false;
    }
}
