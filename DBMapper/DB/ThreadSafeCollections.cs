using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.ObjectModel;

namespace DBMapper {

	public class ThreadSafeDictionary<TKey, TValue> : IDictionary<TKey, TValue> {

		private IDictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

		#region IDictionary<TKey,TValue> Members

		public void Add(TKey key, TValue value) {
			lock (((IDictionary)dictionary).SyncRoot) {
				dictionary.Add(key, value);
			}
		}

		public bool ContainsKey(TKey key) {
			lock (((IDictionary)dictionary).SyncRoot) {
				return dictionary.ContainsKey(key);
			}
		}

		public ICollection<TKey> Keys {
			get {
				lock (((IDictionary)dictionary).SyncRoot) {
					return dictionary.Keys;
				}
			}
		}

		public bool Remove(TKey key) {
			lock (((IDictionary)dictionary).SyncRoot) {
				return dictionary.Remove(key);
			}
		}

		public bool TryGetValue(TKey key, out TValue value) {
			lock (((IDictionary)dictionary).SyncRoot) {
				return dictionary.TryGetValue(key, out value);
			}
		}

		public ICollection<TValue> Values {
			get {
				lock (((IDictionary)dictionary).SyncRoot) {
					return dictionary.Values;
				}
			}
		}

		public TValue this[TKey key] {
			get {
				lock (((IDictionary)dictionary).SyncRoot) {
					return dictionary[key];
				}
			}
			set {
				lock (((IDictionary)dictionary).SyncRoot) {
					dictionary[key] = value;
				}
			}
		}

		public object SyncRoot {
			get {
				return ((IDictionary)dictionary).SyncRoot;
			}
		}

		#endregion

		#region ICollection<KeyValuePair<TKey,TValue>> Members

		public void Add(KeyValuePair<TKey, TValue> item) {
			lock (((IDictionary)dictionary).SyncRoot) {
				dictionary.Add(item.Key, item.Value);
			}
		}

		public void Clear() {
			lock (((IDictionary)dictionary).SyncRoot) {
				dictionary.Clear();
			}
		}

		public bool Contains(KeyValuePair<TKey, TValue> item) {
			lock (((IDictionary)dictionary).SyncRoot) {
				return dictionary.Contains(item);
			}
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
			lock (((IDictionary)dictionary).SyncRoot) {
				dictionary.CopyTo(array, arrayIndex);
			}
		}

		public int Count {
			get {
				lock (((IDictionary)dictionary).SyncRoot) {
					return dictionary.Count;
				}
			}
		}

		public bool IsReadOnly {
			get {
				lock (((IDictionary)dictionary).SyncRoot) {
					return dictionary.IsReadOnly;
				}
			}
		}

		public bool Remove(KeyValuePair<TKey, TValue> item) {
			lock (((IDictionary)dictionary).SyncRoot) {
				return dictionary.Remove(item);
			}
		}

		#endregion

		#region IEnumerable<KeyValuePair<TKey,TValue>> Members

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
			lock (((IDictionary)dictionary).SyncRoot) {
				return dictionary.GetEnumerator();
			}
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			lock (((IDictionary)dictionary).SyncRoot) {
				return dictionary.GetEnumerator();
			}
		}

		#endregion
	}

	public class ThreadSafeList<T> : IList<T> {

		private List<T> list = new List<T>();

		#region IList<T> Members

		public int IndexOf(T item) {
			lock (((IList)list).SyncRoot) {
				return list.IndexOf(item);
			}
		}

		public void Insert(int index, T item) {
			lock (((IList)list).SyncRoot) {
				list.Insert(index, item);
			}
		}

		public void RemoveAt(int index) {
			lock (((IList)list).SyncRoot) {
				list.RemoveAt(index);
			}
		}

		public T this[int index] {
			get {
				lock (((IList)list).SyncRoot) {
					return list[index];
				}
			}
			set {
				lock (((IList)list).SyncRoot) {
					list[index] = value;
				}
			}
		}

		#endregion

		#region ICollection<T> Members

		public void Add(T item) {
			lock (((IList)list).SyncRoot) {
				list.Add(item);
			}
		}

		public void Clear() {
			lock (((IList)list).SyncRoot) {
				list.Clear();
			}
		}

		public bool Contains(T item) {
			lock (((IList)list).SyncRoot) {
				return list.Contains(item);
			}
		}

		public void CopyTo(T[] array, int arrayIndex) {
			lock (((IList)list).SyncRoot) {
				list.CopyTo(array, arrayIndex);
			}
		}

		public int Count {
			get {
				lock (((IList)list).SyncRoot) {
					return list.Count;
				}
			}
		}

		public bool IsReadOnly {
			get {
				lock (((IList)list).SyncRoot) {
					return ((IList)list).IsReadOnly;
				}
			}
		}

		public bool Remove(T item) {
			lock (((IList)list).SyncRoot) {
				return list.Remove(item);
			}
		}

		#endregion

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator() {
			lock (((IList)list).SyncRoot) {
				return list.GetEnumerator();
			}
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator() {
			lock (((IList)list).SyncRoot) {
				return list.GetEnumerator();
			}
		}

		#endregion

		public ReadOnlyCollection<T> AsReadOnly() {
			ReadOnlyCollection<T> readOnlyCollection = null;
			lock (((IList)list).SyncRoot) {
				readOnlyCollection = ((List<T>)list).AsReadOnly();
			}

			return readOnlyCollection;
		}

		public void AddRange(IEnumerable<T> collection) {
			lock (((IList)list).SyncRoot) {
				list.AddRange(collection);
			}
		}

		public object SyncRoot {
			get {
				return ((IList)list).SyncRoot;
			}
		}

	}

	public static class Extensions {

		public static ThreadSafeList<T> ToThreadSafeList<T>(this IEnumerable<T> source) {
			ThreadSafeList<T> threadSafeList = new ThreadSafeList<T>();
			lock (threadSafeList.SyncRoot) {
				foreach (T item in source)
					threadSafeList.Add(item);
			}

			return threadSafeList;
		}

	}
}
