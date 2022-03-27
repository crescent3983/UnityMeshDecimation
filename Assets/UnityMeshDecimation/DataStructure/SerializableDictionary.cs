using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMeshDecimation.Internal {
	[Serializable]
	public class SerializableDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable {
		[SerializeField]
		List<TKey> _keys = new List<TKey>();
		[SerializeField]
		List<TValue> _values = new List<TValue>();

		public TValue this[TKey key] {
			get {
				int idx = this._keys.IndexOf(key);
				TValue v = this._values[idx];
				return v;
			}
			set {
				if (this._keys.Contains(key)) {
					int idx = this._keys.IndexOf(key);
					this._values[idx] = value;
				}
				else {
					this._keys.Add(key);
					this._values.Add(value);
				}
			}
		}

		public ICollection<TKey> Keys {
			get {
				return this._keys;
			}
		}

		public ICollection<TValue> Values {
			get {
				return this._values;
			}
		}

		public int Count {
			get {
				return Math.Min(this._keys.Count, this._values.Count);
			}
		}

		public bool ContainsKey(TKey key) {
			return this._keys.Contains(key);
		}

		public bool ContainsValue(TValue value) {
			return this._values.Contains(value);
		}

		public void Clear() {
			this._keys.Clear();
			this._values.Clear();
		}

		public void Add(TKey key, TValue value) {
			if (this._keys.Contains(key)) {
				throw new ArgumentException(string.Format("Have same key = {0}", key));
			}

			this._keys.Add(key);
			this._values.Add(value);
		}

		public bool Remove(TKey key) {
			if (!this._keys.Contains(key)) {
				return false;
			}

			int idx = this._keys.IndexOf(key);
			TValue value = this._values[idx];
			this._keys.Remove(key);
			this._values.Remove(value);
			return true;
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
			IEnumerator<TKey> it1 = this._keys.GetEnumerator();
			IEnumerator<TValue> it2 = this._values.GetEnumerator();
			while (it1.MoveNext() && it2.MoveNext()) {
				yield return new KeyValuePair<TKey, TValue>(it1.Current, it2.Current);
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public bool TryGetValue(TKey tkey, out TValue val) {
			val = default(TValue);
			int iIndex = this._keys.IndexOf(tkey);
			if (iIndex >= 0) {
				val = this._values[iIndex];
				return true;
			}
			return false;
		}
	}
}
