using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;

namespace UnityMeshDecimation.Internal {
	public sealed class QuadricHelper {

		private static Dictionary<Vertex, List<(Vector<float>, Quadric)>> QuadricNTemp;
		private static Dictionary<Vertex, Quadric> QuadricTemp;

		public static void Init(IEnumerable<Vertex> verts) {
			QuadricNTemp = new Dictionary<Vertex, List<(Vector<float>, Quadric)>>();
			QuadricTemp = new Dictionary<Vertex, Quadric>();
			foreach (var vert in verts) {
				var q = new Quadric(3);
				q.Zero();
				QuadricTemp[vert] = q;
				QuadricNTemp[vert] = new List<(Vector<float>, Quadric)>();
			}
		}

		public static void Alloc(Vertex vert, Vector<float> props) {
			var qv = QuadricNTemp[vert];
			var newq = new Quadric(3 + props.Count);
			newq.Zero();
			newq.Sum3(Qd3(vert), props);
			qv.Add((props, newq));
		}

		public static void SumAll(Vertex vert, Vector<float> props, Quadric q) {
			var qv = QuadricNTemp[vert];
			for (int i = 0; i < qv.Count; i++) {
				Vector<float> p = qv[i].Item1;
				if (p.Equals(props)) {
					qv[i].Item2.Add(q);
				}
				else {
					qv[i].Item2.Sum3(Qd3(vert), p);
				}
			}
		}

		public static bool Contains(Vertex vert, Vector<float> props) {
			var qv = QuadricNTemp[vert];
			for (int i = 0; i < qv.Count; i++) {
				Vector<float> p = qv[i].Item1;
				if (p.Equals(props)) {
					return true;
				}
			}
			return false;
		}

		public static Quadric Qd(Vertex vert, Vector<float> props) {
			var qv = QuadricNTemp[vert];
			for (int i = 0; i < qv.Count; i++) {
				Vector<float> p = qv[i].Item1;
				if (p.Equals(props)) {
					return qv[i].Item2;
				}
			}
			return qv[0].Item2;
		}

		public static Quadric Qd3(Vertex vert) {
			return QuadricTemp[vert];
		}

		public static void Qd3(Vertex vert, Quadric value) {
			QuadricTemp[vert] = value;
		}

		public static List<(Vector<float>, Quadric)> Vd(Vertex vert) {
			return QuadricNTemp[vert];
		}

		public static void Vd(Vertex vert, List<(Vector<float>, Quadric)> value) {
			QuadricNTemp[vert] = value;
		}
	}
}
