using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityMeshDecimation.Internal {
	public static class MeshUtil {

		public static int triangleCount(this UnityEngine.Mesh mesh) {
			int count = 0;
			for (int i = 0; i < mesh.subMeshCount; i++) {
				var topology = mesh.GetTopology(i);
				var indices = mesh.GetIndices(i);
				if (topology == MeshTopology.Triangles) {
					count += indices.Length / 3;
				}
			}
			return count;
		}

		public static int GetUsedUVComponents(List<Vector4> uvs) {
			if (uvs == null || uvs.Count == 0) {
				return 0;
			}
			int used = 2;
			foreach (var uv in uvs) {
				if (used < 3 && uv.z != 0f) {
					used = 3;
				}
				if (used < 4 && uv.w != 0f) {
					used = 4;
					break;
				}
			}
			return used;
		}

		public static List<Face> DoCollapse(Mesh m, VertexPair c, Vector3 p) {
			var deleted = new List<Face>();
			var av01 = new List<(Face, int)>();
			var av0 = new List<(Face, int)>();

			VFIterator vfi = new VFIterator(c.v0);
			while (vfi.MoveNext()) {
				bool foundV1 = false;
				for (int i = 0; i < Face.VERTEX_COUNT; i++) {
					if (vfi.f.V(i) == c.v1) {
						foundV1 = true;
						break;
					}
				}
				if (foundV1) av01.Add((vfi.f, vfi.z));
				else av0.Add((vfi.f, vfi.z));
			}

			for (int i = 0; i < av01.Count; i++) {
				var face = av01[i].Item1;
				Face.VFDetach(face, (av01[i].Item2 + 1) % Face.VERTEX_COUNT);
				Face.VFDetach(face, (av01[i].Item2 + 2) % Face.VERTEX_COUNT);
				Mesh.DeleteFace(m, face);
				deleted.Add(face);
			}

			for (int i = 0; i < av0.Count; i++) {
				var face = av0[i].Item1;
				var z = av0[i].Item2;

				face.V(z) = c.v1;
				face.vfParent[z] = c.v1.vfParent;
				face.vfIndex[z] = c.v1.vfIndex;
				c.v1.vfParent = face;
				c.v1.vfIndex = z;
			}

			Mesh.DeleteVertex(m, c.v0);
			c.v1.pos = p;
			return deleted;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 FaceNormal(Face face) {
			var e1 = face.P(1) - face.P(0);
			var e2 = face.P(2) - face.P(0);
			var n = Vector3.Cross(e1, e2);
			return n.normalized;
		}

		private static Vertex dummyVert = new Vertex(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
		private static Dictionary<Vertex, int> vertCnt = new Dictionary<Vertex, int>();
		private static Dictionary<(Vertex, Vertex), int> edgeCnt = new Dictionary<(Vertex, Vertex), int>();
		private static List<Vertex>[] boundaryVertexVec = new List<Vertex>[] {new List<Vertex>(), new List<Vertex>()};
		private static List<Vertex> LkEdge = new List<Vertex>();

		public static bool LinkConditions(VertexPair pair) {
			// at the end of the loop each vertex must be counted twice
			// except for boundary vertex.
			vertCnt.Clear();
			edgeCnt.Clear();

			// the list of the boundary vertexes for the two endpoints
			boundaryVertexVec[0].Clear();
			boundaryVertexVec[1].Clear();

			void TryDicAdd<T>(Dictionary<T, int> dic, T key, int value) {
				if (!dic.ContainsKey(key)) dic[key] = value;
				else dic[key] += value;
			}

			// Collect vertexes and edges of V0 and V1
			VFIterator vfi;
			(Vertex, Vertex) e;
			for (int i = 0; i < 2; i++) {
				vfi = new VFIterator(i == 0 ? pair.v0 : pair.v1);
				while (vfi.MoveNext()) {
					var v1 = vfi.V1();
					var v2 = vfi.V2();
					TryDicAdd(vertCnt, v1, 1);
					TryDicAdd(vertCnt, v2, 1);

					e = v1 < v2 ? (v1, v2) : (v2, v1);
					TryDicAdd(edgeCnt, e, 1);
				}
				// Now a loop to add dummy stuff: add the dummy vertex and two dummy edges
				// (and remember to increase the counters for the two boundary vertexes involved)
				foreach (var vcmit in vertCnt) {
					if (vcmit.Value == 1) { // boundary vertexes are counted only once
						boundaryVertexVec[i].Add(vcmit.Key);
					}
				}
				if (boundaryVertexVec[i].Count == 2) {
					// aha! one of the two vertex of the collapse is on the boundary
					// so add dummy vertex and two dummy edges
					TryDicAdd(vertCnt, dummyVert, 2);
					TryDicAdd(edgeCnt, (dummyVert, boundaryVertexVec[i][0]), 1);
					TryDicAdd(edgeCnt, (dummyVert, boundaryVertexVec[i][1]), 1);

					// remember to hide the boundaryness of the two boundary vertexes
					TryDicAdd(vertCnt, boundaryVertexVec[i][0], 1);
					TryDicAdd(vertCnt, boundaryVertexVec[i][1], 1);
				}
			}

			// Final loop to find cardinality of Lk( V0-V1 )
			// Note that Lk(edge) is only a set of vertices.
			LkEdge.Clear();

			vfi = new VFIterator(pair.v0);
			while (vfi.MoveNext()) {
				if (vfi.V1() == pair.v1) LkEdge.Add(vfi.V2());
				if (vfi.V2() == pair.v1) LkEdge.Add(vfi.V1());
			}

			// if the collapsing edge was a boundary edge, we must add the dummy vertex.
			// Note that this implies that Lk(edge) >=2;
			if (LkEdge.Count == 1) {
				LkEdge.Add(dummyVert);
			}

			// NOW COUNT!!!
			int sharedEdgeCnt = 0;
			foreach (var eci in edgeCnt) {
				if (eci.Value == 2) sharedEdgeCnt++;
			}
			if (sharedEdgeCnt > 0) return false;

			int sharedVertCnt = 0;
			foreach (var vci in vertCnt) {
				if (vci.Value == 4) sharedVertCnt++;
			}

			if (sharedVertCnt != LkEdge.Count) return false;

			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 BarycentricCoords(Vector3 point, Vector3 a, Vector3 b, Vector3 c) {
			Vector3 v0 = (b - a), v1 = (c - a), v2 = (point - a);
			float d00 = Vector3.Dot(v0, v0);
			float d01 = Vector3.Dot(v0, v1);
			float d11 = Vector3.Dot(v1, v1);
			float d20 = Vector3.Dot(v2, v0);
			float d21 = Vector3.Dot(v2, v1);
			float denom = d00 * d11 - d01 * d01;
			float v = (d11 * d20 - d01 * d21) / denom;
			float w = (d00 * d21 - d01 * d20) / denom;
			float u = 1f - v - w;
			return new Vector3(u, v, w);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector4 NormalizeTangent(Vector4 tangent) {
			var tangentVec = new Vector3(tangent.x, tangent.y, tangent.z).normalized;
			return new Vector4(tangentVec.x, tangentVec.y, tangentVec.z, tangent.w);
		}

		public static (Vector3 closest, Vector3 barycentric, float sqrDistance) PointToTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 p) {
            Vector3 diff = p - a;
            Vector3 edge0 = b - a;
            Vector3 edge1 = c - a;
            float a00 = Vector3.Dot(edge0, edge0);
            float a01 = Vector3.Dot(edge0, edge1);
            float a11 = Vector3.Dot(edge1, edge1);
            float b0 = -Vector3.Dot(diff, edge0);
            float b1 = -Vector3.Dot(diff, edge1);
            float det = a00 * a11 - a01 * a01;
            float t0 = a01 * b1 - a11 * b0;
            float t1 = a01 * b0 - a00 * b1;

			if (t0 + t1 <= det) {
				if (t0 < 0) {
					if (t1 < 0) {
						if (b0 < 0) {
							t1 = 0;
							if (-b0 >= a00) {
								t0 = 1;
							}
							else {
								t0 = -b0 / a00;
							}
						}
						else {
							t0 = 0;
							if (b1 >= 0) {
								t1 = 0;
							}
							else if (-b1 >= a11) {
								t1 = 1;
							}
							else {
								t1 = -b1 / a11;
							}
						}
					}
					else {
						t0 = 0;
						if (b1 >= 0) {
							t1 = 0;
						}
						else if (-b1 >= a11) {
							t1 = 1;
						}
						else {
							t1 = -b1 / a11;
						}
					}
				}
				else if (t1 < 0) {
					t1 = 0;
					if (b0 >= 0) {
						t0 = 0;
					}
					else if (-b0 >= a00) {
						t0 = 1;
					}
					else {
						t0 = -b0 / a00;
					}
				}
				else {
					float invDet = 1 / det;
					t0 *= invDet;
					t1 *= invDet;
				}
			}
			else {
				float tmp0, tmp1, numer, denom;

				if (t0 < 0) {
					tmp0 = a01 + b0;
					tmp1 = a11 + b1;
					if (tmp1 > tmp0) {
						numer = tmp1 - tmp0;
						denom = a00 - 2 * a01 + a11;
						if (numer >= denom) {
							t0 = 1;
							t1 = 0;
						}
						else {
							t0 = numer / denom;
							t1 = 1 - t0;
						}
					}
					else {
						t0 = 0;
						if (tmp1 <= 0) {
							t1 = 1;
						}
						else if (b1 >= 0) {
							t1 = 0;
						}
						else {
							t1 = -b1 / a11;
						}
					}
				}
				else if (t1 < 0) {
					tmp0 = a01 + b1;
					tmp1 = a00 + b0;
					if (tmp1 > tmp0) {
						numer = tmp1 - tmp0;
						denom = a00 - 2 * a01 + a11;
						if (numer >= denom) {
							t1 = 1;
							t0 = 0;
						}
						else {
							t1 = numer / denom;
							t0 = 1 - t1;
						}
					}
					else {
						t1 = 0;
						if (tmp1 <= 0) {
							t0 = 1;
						}
						else if (b0 >= 0) {
							t0 = 0;
						}
						else {
							t0 = -b0 / a00;
						}
					}
				}
				else {
					numer = a11 + b1 - a01 - b0;
					if (numer <= 0) {
						t0 = 0;
						t1 = 1;
					}
					else {
						denom = a00 - 2 * a01 + a11;
						if (numer >= denom) {
							t0 = 1;
							t1 = 0;
						}
						else {
							t0 = numer / denom;
							t1 = 1 - t0;
						}
					}
				}
			}
			var closest = a + t0 * edge0 + t1 * edge1;
			diff = p - closest;
			return (closest, new Vector3(1 - t0 - t1, t0, t1), diff.sqrMagnitude);
		}

		public static void RecalculateSmoothNormals(Mesh m) {
			for(int i = 0; i < m.verts.Count; i++) {
				if (m.verts[i].IsDeleted()) {
					continue;
				}
				var vfi = new VFIterator(m.verts[i]);
				Vector3 normal = Vector3.zero;
				while (vfi.MoveNext()) {
					normal += FaceNormal(vfi.f);
				}
				normal.Normalize();

				vfi.Reset();
				while (vfi.MoveNext()) {
					vfi.f.normals[vfi.z] = normal;
				}
			}
		}

		public static bool IsLineIntersectTriangle(Vector3 q1, Vector3 q2, Vector3 p1, Vector3 p2, Vector3 p3, out Vector3 result) {
			float SignedTetraVolume(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
				var v = Vector3.Dot(Vector3.Cross(b - a, c - a), d - a) / 6f;
				return v > 0 ? 1f : v < 0 ? -1f : 0f;
			}

			var s1 = SignedTetraVolume(q1, p1, p2, p3);
			var s2 = SignedTetraVolume(q2, p1, p2, p3);

			if(s1 != s2) {
				var s3 = SignedTetraVolume(q1, q2, p1, p2);
				var s4 = SignedTetraVolume(q1, q2, p2, p3);
				var s5 = SignedTetraVolume(q1, q2, p3, p1);

				if (s3 == s4 && s4 == s5) {
					var n = Vector3.Cross(p2 - p1, p3 - p1);
					var m = Vector3.Dot(q2 - q1, n);
					if(m != 0) {
						var t = Vector3.Dot(p1 - q1, n) / m;
						result = q1 + t * (q2 - q1);
						return true;
					}
				}
			}
			result = Vector3.zero;
			return false;
		}

		public static bool IsLineInBox(Vector3 a, Vector3 b, Bounds bounds) {
			a -= bounds.center;
			b -= bounds.center;

			// Get line midpoint and extent
			Vector3 mid = (a + b) * 0.5f;
			Vector3 l = (a - mid);
			Vector3 ext = new Vector3(Mathf.Abs(l.x), Mathf.Abs(l.y), Mathf.Abs(l.z));

			// Use Separating Axis Test
			// Separation vector from box center to line center is LMid, since the line is in box space
			if (Mathf.Abs(mid.x ) > bounds.extents.x + ext.x ) return false;
			if (Mathf.Abs(mid.y ) > bounds.extents.y + ext.y ) return false;
			if (Mathf.Abs(mid.z ) > bounds.extents.z + ext.z ) return false;
			// Crossproducts of line and each axis
			if (Mathf.Abs(mid.y* l.z - mid.z* l.y)  >  (bounds.extents.y* ext.z + bounds.extents.z* ext.y) ) return false;
			if (Mathf.Abs(mid.x* l.z - mid.z* l.x)  >  (bounds.extents.x* ext.z + bounds.extents.z* ext.x) ) return false;
			if (Mathf.Abs(mid.x* l.y - mid.y* l.x)  >  (bounds.extents.x* ext.y + bounds.extents.y* ext.x) ) return false;
			// No separating axis, the line intersects
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float SqrDistance(Vector3 a, Vector3 b) {
			float x = a.x - b.x;
			float y = a.y - b.y;
			float z = a.z - b.z;
			return x * x + y * y + z * z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float SqrDistance(Vector4 a, Vector4 b) {
			float x = a.x - b.x;
			float y = a.y - b.y;
			float z = a.z - b.z;
			float w = a.w - b.w;
			return x * x + y * y + z * z + w * w;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float SqrDistance(Texcoord a, Texcoord b) {
			float x = a.x - b.x;
			float y = a.y - b.y;
			float z = a.z - b.z;
			float w = a.w - b.w;
			return x * x + y * y + z * z + w * w;
		}
	}
}
