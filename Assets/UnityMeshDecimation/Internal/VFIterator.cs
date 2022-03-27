using System.Collections;

namespace UnityMeshDecimation.Internal {
	public class VFIterator : IEnumerator {
		public Face f { get; private set; }
		public int z { get; private set; }

		private Vertex _v;
		private bool _init = false;

		public VFIterator(Vertex v) {
			this._v = v;
		}

		public void Reset() {
			this._init = false;
		}

		public bool MoveNext() {
			if (!this._init) {
				this.f = this._v.vfParent;
				this.z = this._v.vfIndex;
				this._init = true;
			}
			else if (this.f != null) {
				var t = this.f;
				this.f = t.vfParent[this.z];
				this.z = t.vfIndex[this.z];
			}
			return this.f != null;
		}

		public object Current => this;

		public Vertex V() => this.f.V(this.z);
		public Vertex V0() => this.f.V0(this.z);
		public Vertex V1() => this.f.V1(this.z);
		public Vertex V2() => this.f.V2(this.z);
	}
}
