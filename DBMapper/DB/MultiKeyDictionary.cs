using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyCollections {

	public class KeyTuple<TKey1, TKey2> {
		public KeyTuple(TKey1 key1, TKey2 key2) {
			Key1 = key1;
			Key2 = key2;
		}

		public TKey1 Key1;
		public TKey2 Key2;
	}

	public class KeyTuple<TKey1, TKey2, TKey3> {
		public KeyTuple(TKey1 key1, TKey2 key2, TKey3 key3) {
			Key1 = key1;
			Key2 = key2;
			Key3 = key3;
		}

		public TKey1 Key1;
		public TKey2 Key2;
		public TKey3 Key3;
	}

	public class KeyTuple<TKey1, TKey2, TKey3, TKey4> {
		public KeyTuple(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4) {
			Key1 = key1;
			Key2 = key2;
			Key3 = key3;
			Key4 = key4;
		}

		public TKey1 Key1;
		public TKey2 Key2;
		public TKey3 Key3;
		public TKey4 Key4;
	}

	public class KeyTuple<TKey1, TKey2, TKey3, TKey4, TKey5> {
		public KeyTuple(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5) {
			Key1 = key1;
			Key2 = key2;
			Key3 = key3;
			Key4 = key4;
			Key5 = key5;
		}

		public TKey1 Key1;
		public TKey2 Key2;
		public TKey3 Key3;
		public TKey4 Key4;
		public TKey5 Key5;
	}

	public class MultiKeyDictionary<TKey1, TKey2, TValue> {
		private Dictionary<TKey1, Dictionary<TKey2, TValue>> dictionary = new Dictionary<TKey1, Dictionary<TKey2, TValue>>();

		public object SyncRoot {
			get {
				return ((IDictionary)dictionary).SyncRoot;
			}
		}

		public ICollection<KeyTuple<TKey1, TKey2>> Keys {
			get {
				List<KeyTuple<TKey1, TKey2>> keyTupleList = new List<KeyTuple<TKey1, TKey2>>();
				foreach (TKey1 key1 in dictionary.Keys)
					foreach (TKey2 key2 in dictionary[key1].Keys)
						keyTupleList.Add(new KeyTuple<TKey1, TKey2>(key1, key2));

				return keyTupleList;
			}
		}

		public ICollection<TValue> Values {
			get {
				List<TValue> valueList = new List<TValue>();
				foreach (TKey1 key1 in dictionary.Keys)
					foreach (TKey2 key2 in dictionary[key1].Keys)
						valueList.Add(dictionary[key1][key2]);

				return valueList;
			}
		}

		public TValue this[TKey1 key1, TKey2 key2] {
			get {
				return dictionary[key1][key2];
			}
			set {
				this.Add(key1, key2, value);
			}
		}

		public void Add(TKey1 key1, TKey2 key2, TValue value) {
			if (!dictionary.ContainsKey(key1))
				dictionary.Add(key1, new Dictionary<TKey2, TValue>());

			dictionary[key1][key2] = value;
		}

		public bool ContainsKey(TKey1 key1, TKey2 key2) {
			if (dictionary.ContainsKey(key1))
				return dictionary[key1].ContainsKey(key2);

			return false;
		}

		public bool Remove(TKey1 key1, TKey2 key2) {
			if (dictionary.ContainsKey(key1)) {
				bool b1 = true;
				bool b2 = true;
				b2 = dictionary[key1].Remove(key2);

				if (dictionary[key1].Count == 0)
					b1 = dictionary.Remove(key1);

				return b1 && b2;
			}

			return false;
		}

		public bool TryGetValue(TKey1 key1, TKey2 key2, out TValue value) {
			if (dictionary.ContainsKey(key1))
				return dictionary[key1].TryGetValue(key2, out value);
			else {
				value = default(TValue);
				return false;
			}
		}

	}

	public class MultiKeyDictionary<TKey1, TKey2, TKey3, TValue> {
		private Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, TValue>>> dictionary = new Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, TValue>>>();

		public object SyncRoot {
			get {
				return ((IDictionary)dictionary).SyncRoot;
			}
		}

		public ICollection<KeyTuple<TKey1, TKey2, TKey3>> Keys {
			get {
				List<KeyTuple<TKey1, TKey2, TKey3>> keyTupleList = new List<KeyTuple<TKey1, TKey2, TKey3>>();
				foreach (TKey1 key1 in dictionary.Keys)
					foreach (TKey2 key2 in dictionary[key1].Keys)
						foreach (TKey3 key3 in dictionary[key1][key2].Keys)
							keyTupleList.Add(new KeyTuple<TKey1, TKey2, TKey3>(key1, key2, key3));

				return keyTupleList;
			}
		}

		public ICollection<TValue> Values {
			get {
				List<TValue> valueList = new List<TValue>();
				foreach (TKey1 key1 in dictionary.Keys)
					foreach (TKey2 key2 in dictionary[key1].Keys)
						foreach (TKey3 key3 in dictionary[key1][key2].Keys)
							valueList.Add(dictionary[key1][key2][key3]);

				return valueList;
			}
		}

		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3] {
			get {
				return dictionary[key1][key2][key3];
			}
			set {
				this.Add(key1, key2, key3, value);
			}
		}

		public void Add(TKey1 key1, TKey2 key2, TKey3 key3, TValue value) {
			if (!dictionary.ContainsKey(key1))
				dictionary.Add(key1, new Dictionary<TKey2, Dictionary<TKey3, TValue>>());

			if (!dictionary[key1].ContainsKey(key2))
				dictionary[key1].Add(key2, new Dictionary<TKey3, TValue>());

			dictionary[key1][key2][key3] = value;
		}

		public bool ContainsKey(TKey1 key1, TKey2 key2, TKey3 key3) {
			if (dictionary.ContainsKey(key1))
				if (dictionary[key1].ContainsKey(key2))
					return dictionary[key1][key2].ContainsKey(key3);

			return false;
		}

		public bool Remove(TKey1 key1, TKey2 key2, TKey3 key3) {
			if (dictionary.ContainsKey(key1)) {
				if (dictionary[key1].ContainsKey(key2)) {
					bool b1 = true;
					bool b2 = true;
					bool b3 = true;
					b3 = dictionary[key1][key2].Remove(key3);

					if (dictionary[key1][key2].Count == 0)
						b2 = dictionary[key1].Remove(key2);

					if (dictionary[key1].Count == 0)
						b1 = dictionary.Remove(key1);

					return b1 && b2 && b3;
				}
			}

			return false;
		}

		public bool TryGetValue(TKey1 key1, TKey2 key2, TKey3 key3, out TValue value) {
			if (dictionary.ContainsKey(key1))
				if (dictionary[key1].ContainsKey(key2))
					if (dictionary[key1][key2].ContainsKey(key3))
						return dictionary[key1][key2].TryGetValue(key3, out value);

			value = default(TValue);
			return false;
		}

	}

	public class MultiKeyDictionary<TKey1, TKey2, TKey3, TKey4, TValue> {
		private Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, Dictionary<TKey4, TValue>>>> dictionary = new Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, Dictionary<TKey4, TValue>>>>();

		public object SyncRoot {
			get {
				return ((IDictionary)dictionary).SyncRoot;
			}
		}

		public ICollection<KeyTuple<TKey1, TKey2, TKey3, TKey4>> Keys {
			get {
				List<KeyTuple<TKey1, TKey2, TKey3, TKey4>> keyTupleList = new List<KeyTuple<TKey1, TKey2, TKey3, TKey4>>();
				foreach (TKey1 key1 in dictionary.Keys)
					foreach (TKey2 key2 in dictionary[key1].Keys)
						foreach (TKey3 key3 in dictionary[key1][key2].Keys)
							foreach (TKey4 key4 in dictionary[key1][key2][key3].Keys)
								keyTupleList.Add(new KeyTuple<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4));

				return keyTupleList;
			}
		}

		public ICollection<TValue> Values {
			get {
				List<TValue> valueList = new List<TValue>();
				foreach (TKey1 key1 in dictionary.Keys)
					foreach (TKey2 key2 in dictionary[key1].Keys)
						foreach (TKey3 key3 in dictionary[key1][key2].Keys)
							foreach (TKey4 key4 in dictionary[key1][key2][key3].Keys)
								valueList.Add(dictionary[key1][key2][key3][key4]);

				return valueList;
			}
		}

		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4] {
			get {
				return dictionary[key1][key2][key3][key4];
			}
			set {
				this.Add(key1, key2, key3, key4, value);
			}
		}

		public void Add(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TValue value) {
			if (!dictionary.ContainsKey(key1))
				dictionary.Add(key1, new Dictionary<TKey2, Dictionary<TKey3, Dictionary<TKey4, TValue>>>());

			if (!dictionary[key1].ContainsKey(key2))
				dictionary[key1].Add(key2, new Dictionary<TKey3, Dictionary<TKey4, TValue>>());

			if (!dictionary[key1][key2].ContainsKey(key3))
				dictionary[key1][key2].Add(key3, new Dictionary<TKey4, TValue>());

			dictionary[key1][key2][key3][key4] = value;
		}

		public bool ContainsKey(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4) {
			if (dictionary.ContainsKey(key1))
				if (dictionary[key1].ContainsKey(key2))
					if (dictionary[key1][key2].ContainsKey(key3))
						return dictionary[key1][key2][key3].ContainsKey(key4);

			return false;
		}

		public bool Remove(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4) {
			if (dictionary.ContainsKey(key1)) {
				if (dictionary[key1].ContainsKey(key2)) {
					if (dictionary[key1][key2].ContainsKey(key3)) {
						bool b1 = true;
						bool b2 = true;
						bool b3 = true;
						bool b4 = true;
						b4 = dictionary[key1][key2][key3].Remove(key4);

						if (dictionary[key1][key2][key3].Count == 0)
							b3 = dictionary[key1][key2].Remove(key3);

						if (dictionary[key1][key2].Count == 0)
							b2 = dictionary[key1].Remove(key2);

						if (dictionary[key1].Count == 0)
							b1 = dictionary.Remove(key1);

						return b1 && b2 && b3 && b4;
					}
				}
			}

			return false;
		}

		public bool TryGetValue(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, out TValue value) {
			if (dictionary.ContainsKey(key1))
				if (dictionary[key1].ContainsKey(key2))
					if (dictionary[key1][key2].ContainsKey(key3))
						if (dictionary[key1][key2][key3].ContainsKey(key4))
							return dictionary[key1][key2][key3].TryGetValue(key4, out value);

			value = default(TValue);
			return false;
		}

	}

	public class MultiKeyDictionary<TKey1, TKey2, TKey3, TKey4, TKey5, TValue> {
		private Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, Dictionary<TKey4, Dictionary<TKey5, TValue>>>>> dictionary = new Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, Dictionary<TKey4, Dictionary<TKey5, TValue>>>>>();

		public object SyncRoot {
			get {
				return ((IDictionary)dictionary).SyncRoot;
			}
		}

		public ICollection<KeyTuple<TKey1, TKey2, TKey3, TKey4, TKey5>> Keys {
			get {
				List<KeyTuple<TKey1, TKey2, TKey3, TKey4, TKey5>> keyTupleList = new List<KeyTuple<TKey1, TKey2, TKey3, TKey4, TKey5>>();
				foreach (TKey1 key1 in dictionary.Keys)
					foreach (TKey2 key2 in dictionary[key1].Keys)
						foreach (TKey3 key3 in dictionary[key1][key2].Keys)
							foreach (TKey4 key4 in dictionary[key1][key2][key3].Keys)
								foreach (TKey5 key5 in dictionary[key1][key2][key3][key4].Keys)
									keyTupleList.Add(new KeyTuple<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5));

				return keyTupleList;
			}
		}

		public ICollection<TValue> Values {
			get {
				List<TValue> valueList = new List<TValue>();
				foreach (TKey1 key1 in dictionary.Keys)
					foreach (TKey2 key2 in dictionary[key1].Keys)
						foreach (TKey3 key3 in dictionary[key1][key2].Keys)
							foreach (TKey4 key4 in dictionary[key1][key2][key3].Keys)
								foreach (TKey5 key5 in dictionary[key1][key2][key3][key4].Keys)
									valueList.Add(dictionary[key1][key2][key3][key4][key5]);

				return valueList;
			}
		}

		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5] {
			get {
				return dictionary[key1][key2][key3][key4][key5];
			}
			set {
				this.Add(key1, key2, key3, key4, key5, value);
			}
		}

		public void Add(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, TValue value) {
			if (!dictionary.ContainsKey(key1))
				dictionary.Add(key1, new Dictionary<TKey2, Dictionary<TKey3, Dictionary<TKey4, Dictionary<TKey5, TValue>>>>());

			if (!dictionary[key1].ContainsKey(key2))
				dictionary[key1].Add(key2, new Dictionary<TKey3, Dictionary<TKey4, Dictionary<TKey5, TValue>>>());

			if (!dictionary[key1][key2].ContainsKey(key3))
				dictionary[key1][key2].Add(key3, new Dictionary<TKey4, Dictionary<TKey5, TValue>>());

			if (!dictionary[key1][key2][key3].ContainsKey(key4))
				dictionary[key1][key2][key3].Add(key4, new Dictionary<TKey5, TValue>());

			dictionary[key1][key2][key3][key4][key5] = value;
		}

		public bool ContainsKey(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5) {
			if (dictionary.ContainsKey(key1))
				if (dictionary[key1].ContainsKey(key2))
					if (dictionary[key1][key2].ContainsKey(key3))
						if (dictionary[key1][key2][key3].ContainsKey(key4))
							return dictionary[key1][key2][key3][key4].ContainsKey(key5);

			return false;
		}

		public bool Remove(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5) {
			if (dictionary.ContainsKey(key1)) {
				if (dictionary[key1].ContainsKey(key2)) {
					if (dictionary[key1][key2].ContainsKey(key3)) {
						if (dictionary[key1][key2][key3].ContainsKey(key4)) {
							bool b1 = true;
							bool b2 = true;
							bool b3 = true;
							bool b4 = true;
							bool b5 = true;
							b5 = dictionary[key1][key2][key3][key4].Remove(key5);

							if (dictionary[key1][key2][key3][key4].Count == 0)
								b3 = dictionary[key1][key2][key3].Remove(key4);

							if (dictionary[key1][key2][key3].Count == 0)
								b3 = dictionary[key1][key2].Remove(key3);

							if (dictionary[key1][key2].Count == 0)
								b2 = dictionary[key1].Remove(key2);

							if (dictionary[key1].Count == 0)
								b1 = dictionary.Remove(key1);

							return b1 && b2 && b3 && b4 && b5;
						}
					}
				}
			}

			return false;
		}

		public bool TryGetValue(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, out TValue value) {
			if (dictionary.ContainsKey(key1))
				if (dictionary[key1].ContainsKey(key2))
					if (dictionary[key1][key2].ContainsKey(key3))
						if (dictionary[key1][key2][key3].ContainsKey(key4))
							if (dictionary[key1][key2][key3][key4].ContainsKey(key5))
								return dictionary[key1][key2][key3][key4].TryGetValue(key5, out value);

			value = default(TValue);
			return false;
		}

	}

}
