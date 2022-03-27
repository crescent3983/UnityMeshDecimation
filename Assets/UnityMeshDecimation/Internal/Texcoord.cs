using System;
using UnityEngine;

namespace UnityMeshDecimation.Internal {
	public struct Texcoord : IEquatable<Texcoord> {
		public int size;
		public float x, y, z, w;

		public Texcoord(float x, float y) {
			this.size = 2;
			this.x = x;
			this.y = y;
			this.z = 0;
			this.w = 0;
		}

		public Texcoord(float x, float y, float z) {
			this.size = 3;
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = 0;
		}

		public Texcoord(float x, float y, float z, float w) {
			this.size = 4;
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		public Texcoord(Vector2 uv) : this(uv.x, uv.y) { }
		public Texcoord(Vector3 uv) : this(uv.x, uv.y, uv.z) { }
		public Texcoord(Vector4 uv) : this(uv.x, uv.y, uv.z, uv.w) { }

		public static Texcoord operator +(Texcoord a, Texcoord b) {
			if(a.size == 2) {
				return new Texcoord(a.x + b.x, a.y + b.y);
			}
			else if (a.size == 3) {
				return new Texcoord(a.x + b.x, a.y + b.y, a.z + b.z);
			}
			else if (a.size == 4) {
				return new Texcoord(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
			}
			else {
				return new Texcoord();
			}
		}

		public static Texcoord operator *(Texcoord a, float scale) {
			if (a.size == 2) {
				return new Texcoord(a.x * scale, a.y * scale);
			}
			else if (a.size == 3) {
				return new Texcoord(a.x * scale, a.y * scale, a.z * scale);
			}
			else if (a.size == 4) {
				return new Texcoord(a.x * scale, a.y * scale, a.z * scale, a.w * scale);
			}
			else {
				return new Texcoord();
			}
		}

		public static bool operator ==(Texcoord a, Texcoord b) {
			if (a.size != b.size) {
				return false;
			}
			if (a.size == 2) {
				return a.x == b.x && a.y == b.y;
			}
			else if (a.size == 3) {
				return a.x == b.x && a.y == b.y && a.z == b.z;
			}
			else if (a.size == 4) {
				return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
			}
			return false;
		}

		public static bool operator !=(Texcoord x, Texcoord y) {
			return !(x == y);
		}

		public bool Equals(Texcoord obj) {
			return this == obj;
		}

		public override bool Equals(object obj) {
			return this.Equals((Texcoord)obj);
		}

		public override int GetHashCode() {
			var hashCode = -408799446;
			hashCode = hashCode * -1521134295 + this.size.GetHashCode();
			hashCode = hashCode * -1521134295 + this.x.GetHashCode();
			hashCode = hashCode * -1521134295 + this.y.GetHashCode();
			hashCode = hashCode * -1521134295 + this.z.GetHashCode();
			hashCode = hashCode * -1521134295 + this.w.GetHashCode();
			return hashCode;
		}

		public static implicit operator Vector2(Texcoord tc) => new Vector2(tc.x, tc.y);
		public static implicit operator Vector3(Texcoord tc) => new Vector3(tc.x, tc.y, tc.z);
		public static implicit operator Vector4(Texcoord tc) => new Vector4(tc.x, tc.y, tc.z, tc.w);
	}
}
