using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityMesh = UnityEngine.Mesh;

namespace UnityMeshDecimation.Internal {
	[Flags]
	public enum VertexProperty : int {
		Position = 1 << 1,
		Normal = 1 << 2,
		Tangent = 1 << 3,
		Color = 1 << 4,
		UV0 = 1 << 5,
		UV1 = 1 << 6,
		UV2 = 1 << 7,
		UV3 = 1 << 8,
		UV4 = 1 << 9,
		UV5 = 1 << 10,
		UV6 = 1 << 11,
		UV7 = 1 << 12,
		BoneWeight = 1 << 13,
	}

	public class Mesh : FlagBase {

		public const int UV_COUNT = 8;

		public List<Vertex> verts { get; private set; }
		public List<Face> faces { get; private set; }

		public int vertexCount { get; private set; }
		public int faceCount { get; private set; }
		public int[] uvSizes { get; private set; }

		public VertexProperty properties => (VertexProperty)this.flags;

		private int subMeshCount;
		private Matrix4x4[] bindposes;

		public Mesh(UnityMesh mesh) {
			var vertices = mesh.vertices;
			var normals = mesh.normals;
			var tangents = mesh.tangents;
			var colors = mesh.colors;
			var boneWeights = mesh.boneWeights;
			var uvs = new List<Vector4>[UV_COUNT];
			uvSizes = new int[UV_COUNT];
			for(int i = 0; i < uvs.Length; i++) {
				uvs[i] = new List<Vector4>();
				mesh.GetUVs(i, uvs[i]);
				uvSizes[i] = MeshUtil.GetUsedUVComponents(uvs[i]);
			}

			this.AddFlag((int)VertexProperty.Position);
			if (normals.Length > 0) this.AddFlag((int)VertexProperty.Normal);
			if (tangents.Length > 0) this.AddFlag((int)VertexProperty.Tangent);
			if (colors.Length > 0) this.AddFlag((int)VertexProperty.Color);
			if (boneWeights.Length > 0) this.AddFlag((int)VertexProperty.BoneWeight);
			for (int i = 0; i < UV_COUNT; i++) {
				if (uvs[i].Count > 0) {
					this.AddFlag((int)VertexProperty.UV0 << i);
				}
			}

			var vertDic = new Dictionary<Vector3, Vertex>();
			this.faces = new List<Face>();
			for (int i = 0; i < mesh.subMeshCount; i++) {
				var topology = mesh.GetTopology(i);
				var indices = mesh.GetIndices(i);
				if (topology == MeshTopology.Triangles) {
					for (int j = 0; j < indices.Length; j += 3) {
						var face = new Face();
						Vertex vert;
						for (int k = 0; k < Face.VERTEX_COUNT; k++) {
							var index = indices[j + k];
							var v = vertices[index];
							if (!vertDic.TryGetValue(v, out vert)) {
								vert = new Vertex(v);
								vertDic[v] = vert;
							}
							face.V(k) = vert;

							if (this.HasFlag((int)VertexProperty.Normal)) face.normals[k] = normals[index];
							if (this.HasFlag((int)VertexProperty.Tangent)) face.tangents[k] = tangents[index];
							if (this.HasFlag((int)VertexProperty.Color)) face.colors[k] = colors[index];
							if (this.HasFlag((int)VertexProperty.BoneWeight)) face.boneWeights[k] = boneWeights[index];
							for (int m = 0; m < UV_COUNT; m++) {
								if (!this.HasFlag((int)VertexProperty.UV0 << m)) {
									continue;
								}
								var uv = uvs[m][index];
								face.uvs[m][k] = (uvSizes[m] == 2) ? new Texcoord(uv.x, uv.y) :
												(uvSizes[m] == 3) ? new Texcoord(uv.x, uv.y, uv.z) :
												new Texcoord(uv);
							}
						}
						face.BuildFaceNormal();
						face.subMeshIndex = i;
						this.faces.Add(face);
					}
				}
			}
			this.verts = new List<Vertex>(vertDic.Values);
			this.faceCount = this.faces.Count;
			this.vertexCount = this.verts.Count;
			this.subMeshCount = mesh.subMeshCount;
			this.bindposes = mesh.bindposes;
			if(mesh.blendShapeCount != 0) {
				Debug.LogWarning("Blending shape is not supported yet");
			}
		}

		public UnityMesh ToMesh() {
			UnityMesh mesh = new UnityMesh();
			this.ToMesh(mesh);
			return mesh;
		}

		public void ToMesh(UnityMesh mesh) {
			mesh.Clear();

			mesh.subMeshCount = this.subMeshCount;
			var dic = new Dictionary<Vector<float>, int>();
			var vertices = new List<Vector3>();
			var normals = new List<Vector3>();
			var tangents = new List<Vector4>();
			var colors = new List<Color>();
			var boneWeights = new List<BoneWeight>();
			var uvs = new List<Vector4>[UV_COUNT];
			for (int i = 0; i < uvs.Length; i++) uvs[i] = new List<Vector4>();

			List<int>[] subMeshes = new List<int>[this.subMeshCount];
			for(int i = 0; i < subMeshes.Length; i++) {
				subMeshes[i] = new List<int>();
			}

			for(int i = 0; i < this.faces.Count; i++) {
				var face = this.faces[i];
				if (!face.IsDeleted()) {
					for(int j = 0; j < Face.VERTEX_COUNT; j++) {
						var key = face.GetPropertyS((VertexProperty)this.flags, j);
						int idx;
						if(!dic.TryGetValue(key, out idx)) {
							vertices.Add(face.V(j).pos);
							if (this.HasFlag((int)VertexProperty.Normal)) normals.Add(face.normals[j]);
							if (this.HasFlag((int)VertexProperty.Tangent)) tangents.Add(face.tangents[j]);
							if (this.HasFlag((int)VertexProperty.Color)) colors.Add(face.colors[j]);
							if (this.HasFlag((int)VertexProperty.BoneWeight)) boneWeights.Add(face.boneWeights[j]);
							for (int k = 0; k < UV_COUNT; k++) {
								if (!this.HasFlag((int)VertexProperty.UV0 << k)) {
									continue;
								}
								uvs[k].Add(face.uvs[k][j]);
							}
							idx = vertices.Count - 1;
							dic.Add(key, idx);
						}
						subMeshes[face.subMeshIndex].Add(idx);
					}
				}
			}

			mesh.SetVertices(vertices);
			if (this.HasFlag((int)VertexProperty.Normal)) mesh.SetNormals(normals);
			if (this.HasFlag((int)VertexProperty.Tangent)) mesh.SetTangents(tangents);
			if (this.HasFlag((int)VertexProperty.Color)) mesh.SetColors(colors);
			if (this.HasFlag((int)VertexProperty.BoneWeight)) mesh.boneWeights = boneWeights.ToArray();
			for (int i = 0; i < UV_COUNT; i++) {
				if (!this.HasFlag((int)VertexProperty.UV0 << i)) {
					continue;
				}
				if(uvSizes[i] == 2) mesh.SetUVs(i, uvs[i].ConvertAll(v => new Vector2(v.x, v.y)));
				else if (uvSizes[i] == 3) mesh.SetUVs(i, uvs[i].ConvertAll(v => new Vector3(v.x, v.y, v.z)));
				else if(uvSizes[i] == 4) mesh.SetUVs(i, uvs[i]);
			}
			for (int i = 0; i < subMeshes.Length; i++) {
				mesh.SetTriangles(subMeshes[i], i);
			}
			mesh.bindposes = this.bindposes;

#if UNITY_EDITOR
			UnityEditor.MeshUtility.Optimize(mesh);
#endif
		}

		public void InitIMark() {
			for (int i = 0; i < this.verts.Count; i++) {
				if (!this.verts[i].IsDeleted()) {
					this.verts[i].InitIMark();
				}
			}
		}

		public void BuildVertexFace() {
			for(int i = 0; i < this.verts.Count; i++) {
				this.verts[i].vfParent = null;
				this.verts[i].vfIndex = 0;
			}
			for(int i = 0; i < this.faces.Count; i++) {
				var face = this.faces[i];
				if (!face.IsDeleted()) {
					for(int j = 0; j < Face.VERTEX_COUNT; j++) {
						face.vfParent[j] = face.V(j).vfParent;
						face.vfIndex[j] = face.V(j).vfIndex;
						face.V(j).vfParent = face;
						face.V(j).vfIndex = j;
					}
				}
			}
		}

		public void BuildFaceBorder() {
			for(int i = 0; i < this.faces.Count; i++) {
				this.faces[i].ClearBorderFlags();
			}
			int[] borderFlags = new int[] { (int)Face.FaceFlags.Border0, (int)Face.FaceFlags.Border1, (int)Face.FaceFlags.Border2 };
			for(int i = 0; i < this.verts.Count; i++) {
				var vertex = this.verts[i];
				if (!vertex.IsDeleted()) {
					var vfi = new VFIterator(vertex);
					while (vfi.MoveNext()) {
						vfi.f.V1(vfi.z).ClearVisited();
						vfi.f.V2(vfi.z).ClearVisited();
					}
					vfi.Reset();
					while (vfi.MoveNext()) {
						if (vfi.f.V1(vfi.z).IsVisited()) vfi.f.V1(vfi.z).ClearVisited();
						else vfi.f.V1(vfi.z).SetVisited();
						if (vfi.f.V2(vfi.z).IsVisited()) vfi.f.V2(vfi.z).ClearVisited();
						else vfi.f.V2(vfi.z).SetVisited();
					}
					vfi.Reset();
					while (vfi.MoveNext()) {
						if (vfi.f.V(vfi.z) < vfi.f.V1(vfi.z) && vfi.f.V1(vfi.z).IsVisited()) {
							vfi.f.AddFlag(borderFlags[vfi.z]);
						}
						if (vfi.f.V(vfi.z) < vfi.f.V2(vfi.z) && vfi.f.V2(vfi.z).IsVisited()) {
							vfi.f.AddFlag(borderFlags[(vfi.z + 2) % 3]);
						}
					}
				}
			}
		}

		public static void DeleteFace(Mesh m, Face f) {
			f.SetDeleted();
			m.faceCount--;
		}

		public static void DeleteVertex(Mesh m, Vertex v) {
			v.SetDeleted();
			m.vertexCount--;
		}
	}
}
