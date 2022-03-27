using System;
using UnityEngine;

namespace UnityMeshDecimation.Internal {
	public class VertexPair {
		public Vertex v0 { get; private set; }
		public Vertex v1 { get; private set; }

		public VertexPair(Vertex v0, Vertex v1) {
			this.v0 = v0;
			this.v1 = v1;
		}
	}

	public class Vertex : FlagBase {

		[Flags]
		public enum VertexFlags : int {
			Deleted		= 1 << 0,
			NotRead		= 1 << 1,
			NotWrite	= 1 << 2,
			Modified	= 1 << 3,
			Visited		= 1 << 4,
			Border		= 1 << 5,
			User		= 1 << 6,
		}

		public Vector3 pos;
		public Face vfParent;
		public int vfIndex;
		public int iMark = 0;

		public Vertex(Vector3 pos) {
			this.pos = pos;
		}

		public void InitIMark() {
			this.iMark = 0;
		}

		#region Flags
		public bool IsDeleted() => this.HasFlag((int)VertexFlags.Deleted);
		public void SetDeleted() => this.AddFlag((int)VertexFlags.Deleted);
		public void ClearDeleted() => this.RemoveFlag((int)VertexFlags.Deleted);

		public bool IsVisited() => this.HasFlag((int)VertexFlags.Visited);
		public void SetVisited() => this.AddFlag((int)VertexFlags.Visited);
		public void ClearVisited() => this.RemoveFlag((int)VertexFlags.Visited);

		public bool IsWritable() => !this.HasFlag((int)VertexFlags.NotWrite);
		public void SetWritable() => this.RemoveFlag((int)VertexFlags.NotWrite);
		public void ClearWritable() => this.AddFlag((int)VertexFlags.NotWrite);
		#endregion

		#region Operators
		public static bool operator <(Vertex x, Vertex y) {
			return (x.pos.z != y.pos.z) ? (x.pos.z < y.pos.z) : (x.pos.y != y.pos.y) ? (x.pos.y < y.pos.y) : (x.pos.x < y.pos.x);
		}

		public static bool operator >(Vertex x, Vertex y) {
			return (x.pos.z != y.pos.z) ? (x.pos.z > y.pos.z) : (x.pos.y != y.pos.y) ? (x.pos.y > y.pos.y) : (x.pos.x > y.pos.x);
		}

		public static bool operator <=(Vertex x, Vertex y) {
			return (x.pos.z != y.pos.z) ? (x.pos.z < y.pos.z) : (x.pos.y != y.pos.y) ? (x.pos.y < y.pos.y) : (x.pos.x <= y.pos.x);
		}

		public static bool operator >=(Vertex x, Vertex y) {
			return (x.pos.z != y.pos.z) ? (x.pos.z > y.pos.z) : (x.pos.y != y.pos.y) ? (x.pos.y > y.pos.y) : (x.pos.x >= y.pos.x);
		}
		#endregion
	}
}
