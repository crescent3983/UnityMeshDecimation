using System.Collections.Generic;

namespace UnityMeshDecimation.Internal {
	public class BinaryHeap<K, V> where K : System.IComparable {
		private struct Element {
			public K key;
			public V value;
		}

		private readonly Element[] data;
		private readonly int capacity;
		private readonly K supremum;
		private int size;

		public BinaryHeap(int capacity, K infimum, K supremum) {
			data = new Element[capacity + 2];
			data[0].key = infimum;
			this.capacity = capacity;
			this.supremum = supremum;
			data[capacity + 1].key = supremum;
			Clear();
		}

		public void Clear() {
			size = 0;
			int cap = capacity;
			for (int i = 1; i <= cap; ++i) {
				data[i].key = supremum;
				data[i].value = default(V);
			}
		}

		public int Count => size;

		public V First => data[1].value;

		public void Enqueue(V value, K key) {
			++size;
			Element[] dat = data;
			int hole = size;
			int pred = hole >> 1;
			K predKey = dat[pred].key;
			while (predKey.CompareTo(key) > 0) {
				dat[hole].key = predKey;
				dat[hole].value = dat[pred].value;
				hole = pred;
				pred >>= 1;
				predKey = dat[pred].key;
			}

			dat[hole].key = key;
			dat[hole].value = value;
		}

		public V Dequeue() {
			V min = data[1].value;

			int hole = 1;
			int succ = 2;
			int sz = size;
			Element[] dat = data;

			while (succ < sz) {
				K key1 = dat[succ].key;
				K key2 = dat[succ + 1].key;
				if (key1.CompareTo(key2) > 0) {
					succ++;
					dat[hole].key = key2;
					dat[hole].value = dat[succ].value;
				}
				else {
					dat[hole].key = key1;
					dat[hole].value = dat[succ].value;
				}
				hole = succ;
				succ <<= 1;
			}

			K bubble = dat[sz].key;
			int pred = hole >> 1;
			while (dat[pred].key.CompareTo(bubble) > 0) {
				dat[hole] = dat[pred];
				hole = pred;
				pred >>= 1;
			}

			dat[hole].key = bubble;
			dat[hole].value = dat[sz].value;

			dat[size].key = supremum;
			size = sz - 1;

			return min;
		}

		public IEnumerator<V> GetEnumerator() {
			for(int i = 1; i <= size; i++) {
				yield return data[i].value;
			}
		}
	}
}