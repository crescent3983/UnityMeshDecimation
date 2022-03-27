using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityMeshDecimation.Internal {
	public class Face : FlagBase {

		public const int VERTEX_COUNT = 3;

		[Flags]
		public enum FaceFlags : int {
			Deleted		= 1 << 0,
			NotRead		= 1 << 1,
			NotWrite	= 1 << 2,
			Visited		= 1 << 3,
			Border0		= 1 << 4,
			Border1		= 1 << 5,
			Border2		= 1 << 6,
		}

		public Vertex[] vertices { get; private set; } = new Vertex[VERTEX_COUNT];
		public Vector3[] normals { get; private set; } = new Vector3[VERTEX_COUNT];
		public Vector4[] tangents { get; private set; } = new Vector4[VERTEX_COUNT];
		public Color[] colors { get; private set; } = new Color[VERTEX_COUNT];
		public Texcoord[][] uvs { get; private set; } = new Texcoord[Mesh.UV_COUNT][] {
			new Texcoord[VERTEX_COUNT],
			new Texcoord[VERTEX_COUNT],
			new Texcoord[VERTEX_COUNT],
			new Texcoord[VERTEX_COUNT],
			new Texcoord[VERTEX_COUNT],
			new Texcoord[VERTEX_COUNT],
			new Texcoord[VERTEX_COUNT],
			new Texcoord[VERTEX_COUNT],
		};
		public BoneWeight[] boneWeights { get; private set; } = new BoneWeight[VERTEX_COUNT];

		public int subMeshIndex { get; set; }
		public Vector3 faceNormal { get; private set; }

		public Face[] vfParent { get; private set; } = new Face[VERTEX_COUNT];
		public int[] vfIndex { get; private set; } = new int[VERTEX_COUNT];

		#region Vertex
		public ref Vertex V(int index) => ref this.vertices[index];
		public Vector3 P(int index) => this.vertices[index].pos;

		public Vertex V0(int index) => this.V(index);
		public Vertex V1(int index) => this.V((index + 1) % VERTEX_COUNT);
		public Vertex V2(int index) => this.V((index + 2) % VERTEX_COUNT);

		public Vector3 P0(int index) => this.V(index).pos;
		public Vector3 P1(int index) => this.V((index + 1) % VERTEX_COUNT).pos;
		public Vector3 P2(int index) => this.V((index + 2) % VERTEX_COUNT).pos;
		#endregion

		#region Property
		public static int GetPropertySize(VertexProperty property, Mesh mesh) {
			int count = 0;
			if (property.HasFlag(VertexProperty.Position)) {
				count += 3;
			}
			if (property.HasFlag(VertexProperty.Normal)) {
				count += 3;
			}
			if (property.HasFlag(VertexProperty.Tangent)) {
				count += 4;
			}
			if (property.HasFlag(VertexProperty.Color)) {
				count += 4;
			}
			for (int i = 0; i < Mesh.UV_COUNT; i++) {
				if (property.HasFlag((VertexProperty)((int)VertexProperty.UV0 << i))) {
					count += mesh.uvSizes[i];
				}
			}
			if (property.HasFlag(VertexProperty.BoneWeight)) {
				count += 8;
			}
			return count;
		}

		public Vector<float> GetPropertyS(VertexProperty property, int id) {
			List<float> result = new List<float>();
			this.InternalGetProperty(property, id, result);
			return Vector<float>.Build.Dense(result.ToArray());
		}

		public Vector<double> GetPropertyD(VertexProperty property, int id) {
			List<double> result = new List<double>();
			this.InternalGetProperty(property, id, result);
			return Vector<double>.Build.Dense(result.ToArray());
		}

		public void SetPropertyS(VertexProperty property, int id, Vector<float> value) {
			this.InternalSetProperty(property, id, value);
		}

		public void SetPropertyD(VertexProperty property, int id, Vector<double> value) {
			this.InternalSetProperty(property, id, value);
		}

		private void InternalGetProperty(VertexProperty property, int id, dynamic result) {
			if (property.HasFlag(VertexProperty.Position)) {
				var pos = this.P(id);
				result.Add(pos.x);
				result.Add(pos.y);
				result.Add(pos.z);
			}
			if (property.HasFlag(VertexProperty.Normal)) {
				var normal = this.normals[id];
				result.Add(normal.x);
				result.Add(normal.y);
				result.Add(normal.z);
			}
			if (property.HasFlag(VertexProperty.Tangent)) {
				var tangnet = this.tangents[id];
				result.Add(tangnet.x);
				result.Add(tangnet.y);
				result.Add(tangnet.z);
				result.Add(tangnet.w);
			}
			if (property.HasFlag(VertexProperty.Color)) {
				var color = this.colors[id];
				result.Add(color.r);
				result.Add(color.g);
				result.Add(color.b);
				result.Add(color.a);
			}
			for (int i = 0; i < Mesh.UV_COUNT; i++) {
				if (property.HasFlag((VertexProperty)((int)VertexProperty.UV0 << i))) {
					var uv = this.uvs[i][id];
					if (uv.size >= 2) {
						result.Add(uv.x);
						result.Add(uv.y);
					}
					if (uv.size >= 3) {
						result.Add(uv.z);
					}
					if (uv.size >= 4) {
						result.Add(uv.w);
					}
				}
			}
			if (property.HasFlag(VertexProperty.BoneWeight)) {
				var boneWeight = this.boneWeights[id];
				result.Add(boneWeight.boneIndex0);
				result.Add(boneWeight.boneIndex1);
				result.Add(boneWeight.boneIndex2);
				result.Add(boneWeight.boneIndex3);
				result.Add(boneWeight.weight0);
				result.Add(boneWeight.weight1);
				result.Add(boneWeight.weight2);
				result.Add(boneWeight.weight3);
			}
		}

		private void InternalSetProperty(VertexProperty property, int id, dynamic value) {
			int index = 0;
			if (property.HasFlag(VertexProperty.Position)) {
				this.V(id).pos = new Vector3((float)value[index++], (float)value[index++], (float)value[index++]);
			}
			if (property.HasFlag(VertexProperty.Normal)) {
				this.normals[id] = new Vector3((float)value[index++], (float)value[index++], (float)value[index++]);
			}
			if (property.HasFlag(VertexProperty.Tangent)) {
				this.tangents[id] = new Vector4((float)value[index++], (float)value[index++], (float)value[index++], (float)value[index++]);
			}
			if (property.HasFlag(VertexProperty.Color)) {
				this.colors[id] = new Color((float)value[index++], (float)value[index++], (float)value[index++], (float)value[index++]);
			}
			for (int i = 0; i < Mesh.UV_COUNT; i++) {
				if (property.HasFlag((VertexProperty)((int)VertexProperty.UV0 << i))) {
					var uv = this.uvs[i][id];
					if (uv.size == 2) {
						this.uvs[i][id] = new Texcoord((float)value[index++], (float)value[index++]);
					}
					if (uv.size == 3) {
						this.uvs[i][id] = new Texcoord((float)value[index++], (float)value[index++], (float)value[index++]);
					}
					if (uv.size == 4) {
						this.uvs[i][id] = new Texcoord((float)value[index++], (float)value[index++], (float)value[index++], (float)value[index++]);
					}
				}
			}
			if (property.HasFlag(VertexProperty.BoneWeight)) {
				var boneWeight = this.boneWeights[id];
				boneWeight.boneIndex0 = (int)value[index++];
				boneWeight.boneIndex1 = (int)value[index++];
				boneWeight.boneIndex2 = (int)value[index++];
				boneWeight.boneIndex3 = (int)value[index++];
				boneWeight.weight0 = (float)value[index++];
				boneWeight.weight1 = (float)value[index++];
				boneWeight.weight2 = (float)value[index++];
				boneWeight.weight3 = (float)value[index++];
			}
		}
		#endregion

		#region Interploation
		public Vector3 InterpolateNormal(Vector3 barycentric) {
			return Vector3.Normalize(this.normals[0] * barycentric.x + this.normals[1] * barycentric.y + this.normals[2] * barycentric.z);
		}

		public Vector3 InterpolateTangent(Vector3 barycentric) {
			return MeshUtil.NormalizeTangent(this.tangents[0] * barycentric.x + this.tangents[1] * barycentric.y + this.tangents[2] * barycentric.z);
		}

		public Color InterpolateColor(Vector3 barycentric) {
			return this.colors[0] * barycentric.x + this.colors[1] * barycentric.y + this.colors[2] * barycentric.z;
		}

		public Texcoord InterpolateUV(int index, Vector3 barycentric) {
			return this.uvs[index][0] * barycentric.x + this.uvs[index][1] * barycentric.y + this.uvs[index][2] * barycentric.z;
		}

		public BoneWeight InterpolateBoneWeight(Vector3 barycentric) {
			var bones = new Dictionary<int, float>();

			void TryDicAdd(Dictionary<int, float> dic, int key, float value) {
				if (value == 0) return;
				if (!dic.ContainsKey(key)) dic[key] = value;
				else dic[key] += value;
			}

			BoneWeight bone;
			bone = this.boneWeights[0];
			TryDicAdd(bones, bone.boneIndex0, bone.weight0 * barycentric.x);
			TryDicAdd(bones, bone.boneIndex1, bone.weight1 * barycentric.x);
			TryDicAdd(bones, bone.boneIndex2, bone.weight2 * barycentric.x);
			TryDicAdd(bones, bone.boneIndex3, bone.weight3 * barycentric.x);

			bone = this.boneWeights[1];
			TryDicAdd(bones, bone.boneIndex0, bone.weight0 * barycentric.y);
			TryDicAdd(bones, bone.boneIndex1, bone.weight1 * barycentric.y);
			TryDicAdd(bones, bone.boneIndex2, bone.weight2 * barycentric.y);
			TryDicAdd(bones, bone.boneIndex3, bone.weight3 * barycentric.y);

			bone = this.boneWeights[2];
			TryDicAdd(bones, bone.boneIndex0, bone.weight0 * barycentric.z);
			TryDicAdd(bones, bone.boneIndex1, bone.weight1 * barycentric.z);
			TryDicAdd(bones, bone.boneIndex2, bone.weight2 * barycentric.z);
			TryDicAdd(bones, bone.boneIndex3, bone.weight3 * barycentric.z);

			var list = bones.ToList();
			list.Sort((x, y) => y.Value.CompareTo(x.Value));

			float ratio = 0f;
			for(int i = 0; i < list.Count && i < 4; i++) {
				ratio += list[i].Value;
			}
			ratio = 1f / ratio;

			var result = new BoneWeight();
			result.boneIndex0 = list.Count > 0 ? list[0].Key : 0;
			result.weight0 = list.Count > 0 ? list[0].Value * ratio : 0;
			result.boneIndex1 = list.Count > 1 ? list[1].Key : 0;
			result.weight1 = list.Count > 1 ? list[1].Value * ratio : 0;
			result.boneIndex2 = list.Count > 2 ? list[2].Key : 0;
			result.weight2 = list.Count > 2 ? list[2].Value * ratio : 0;
			result.boneIndex3 = list.Count > 3 ? list[3].Key : 0;
			result.weight3 = list.Count > 3 ? list[3].Value * ratio : 0;

			return result;
		}
		#endregion

		#region Flags
		public bool IsDeleted() { return this.HasFlag((int)FaceFlags.Deleted); }
		public void SetDeleted() { this.AddFlag((int)FaceFlags.Deleted); }
		public void ClearDeleted() { this.RemoveFlag((int)FaceFlags.Deleted); }

		public bool IsVisited() { return this.HasFlag((int)FaceFlags.Visited); }
		public void SetVisited() { this.AddFlag((int)FaceFlags.Visited); }
		public void ClearVisited() { this.RemoveFlag((int)FaceFlags.Deleted); }

		public bool IsWritable() => !this.HasFlag((int)FaceFlags.NotWrite);
		public void SetWritable() => this.RemoveFlag((int)FaceFlags.NotWrite);
		public void ClearWritable() => this.AddFlag((int)FaceFlags.NotWrite);

		public bool IsBorder(int index) => this.HasFlag((int)FaceFlags.Border0 << index);
		public void SetBorder(int index) => this.AddFlag((int)FaceFlags.Border0 << index);
		public void ClearBorder(int index) => this.RemoveFlag((int)FaceFlags.Border0 << index);

		public void ClearBorderFlags() {
			this.RemoveFlag((int)(FaceFlags.Border0 | FaceFlags.Border1 | FaceFlags.Border2));
		}
		#endregion

		public void BuildFaceNormal() {
			this.faceNormal = MeshUtil.FaceNormal(this);
		}

		public float GetQuality() {
			var p0 = this.P(0);
			var p1 = this.P(1);
			var p2 = this.P(2);
			Vector3 d10 = p1 - p0;
			Vector3 d20 = p2 - p0;
			Vector3 d12 = p1 - p2;
			Vector3 x = Vector3.Cross(d10, d20);

			float a = x.magnitude;
			if (a == 0) return 0;
			float b = d10.sqrMagnitude;
			if (b == 0) return 0;
			float t;
			t = d20.sqrMagnitude;
			if (b < t) b = t;
			t = d12.sqrMagnitude;
			if (b < t) b = t;
			return a / b;
		}

		public static void VFDetach(Face f, int z) {
			if (f.V(z).vfParent == f) {
				int fz = f.V(z).vfIndex;
				f.V(z).vfParent = f.vfParent[fz];
				f.V(z).vfIndex = f.vfIndex[fz];
			}
			else {
				VFIterator vfi = new VFIterator(f.V(z));
				Face tf;
				int tz;
				vfi.MoveNext();
				while (true) {
					tf = vfi.f;
					tz = vfi.z;
					if (!vfi.MoveNext()) {
						break;
					}
					if (vfi.f == f) {
						tf.vfParent[tz] = f.vfParent[z];
						tf.vfIndex[tz] = f.vfIndex[z];
						break;
					}
				}
			}
		}
	}
}
