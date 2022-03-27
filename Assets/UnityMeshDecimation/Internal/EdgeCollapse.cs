using DataStructures;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using PropertySetting = UnityMeshDecimation.EdgeCollapseParameter.PropertySetting;
using QH = UnityMeshDecimation.Internal.QuadricHelper;

namespace UnityMeshDecimation.Internal {
	public class EdgeCollapse {

		public static int globalMark;
		private static int propertySize;
		private static int quadricSize;

		private Mesh _mesh;
		private BinaryHeap<float, EdgeCollapse> _heap;
		private BVH<Face> _bvh;
		private EdgeCollapseParameter _param;
		private VertexPair _pair;
		private float _priority;
		private int _locakMark;
		public static void Init(Mesh mesh, BinaryHeap<float, EdgeCollapse> heap, BVH<Face> bvh, EdgeCollapseParameter param) {
			param = (EdgeCollapseParameter)param.Clone();
			param.UsedProperty &= mesh.properties;
			if (param.OptimalSampleCount <= 0) param.OptimalSampleCount = 1;

			propertySize = Face.GetPropertySize(param.UsedProperty, mesh);
			quadricSize = 3 + propertySize; // position + other properties

			mesh.BuildVertexFace();
			mesh.BuildFaceBorder();

			if (param.PreserveBoundary) {
				for (int i = 0; i < mesh.faces.Count; i++) {
					var face = mesh.faces[i];
					if (!face.IsDeleted() && face.IsWritable()) {
						for (int j = 0; j < Face.VERTEX_COUNT; j++) {
							if (face.IsBorder(j)) {
								if (face.V(j).IsWritable()) {
									face.V(j).ClearWritable();
								}
								if (face.V1(j).IsWritable()) {
									face.V1(j).ClearWritable();
								}
							}
						}
					}
				}
			}

			InitQuadric(mesh, param);
			InitCollapses(mesh, heap, bvh, param);
		}

		private static void InitQuadric(Mesh mesh, EdgeCollapseParameter param) {
			QH.Init(mesh.verts);

			for (int i = 0; i < mesh.faces.Count; i++) {
				var face = mesh.faces[i];
				if (face.IsDeleted()) {
					continue;
				}
				Quadric q = new Quadric(quadricSize);
				q.ByFace(face, QH.Qd3(face.V(0)), QH.Qd3(face.V(1)), QH.Qd3(face.V(2)), param.QualityQuadric, param.BoundaryWeight, param.UsedProperty);

				for (int j = 0; j < Face.VERTEX_COUNT; j++) {
					var vert = face.V(j);
					var props = face.GetPropertyS(param.UsedProperty, j);
					if (vert.IsWritable()) {
						if (!QH.Contains(vert, props)) {
							QH.Alloc(vert, props);
						}
						QH.SumAll(vert, props, q);
					}
				}
			}
		}

		private static void InitCollapses(Mesh mesh, BinaryHeap<float, EdgeCollapse> heap, BVH<Face> bvh, EdgeCollapseParameter param) {
			heap.Clear();

			// exclude face with different sub mesh ?
			for (int i = 0; i < mesh.verts.Count; i++) {
				var vertex = mesh.verts[i];
				if (vertex.IsWritable()) {
					var vfi = new VFIterator(vertex);
					while (vfi.MoveNext()) {
						vfi.V1().ClearVisited();
						vfi.V2().ClearVisited();
					}
					vfi.Reset();
					while (vfi.MoveNext()) {
						if (vfi.V0() < vfi.V1() && vfi.V1().IsWritable() && !vfi.V1().IsVisited()) {
							vfi.V1().SetVisited();
							var collapse = new EdgeCollapse(new VertexPair(vfi.V0(), vfi.V1()), globalMark, mesh, heap, bvh, param);
							heap.Enqueue(collapse, collapse.Priority());
						}
						if (vfi.V0() < vfi.V2() && vfi.V2().IsWritable() && !vfi.V2().IsVisited()) {
							vfi.V2().SetVisited();
							var collapse = new EdgeCollapse(new VertexPair(vfi.V0(), vfi.V2()), globalMark, mesh, heap, bvh, param);
							heap.Enqueue(collapse, collapse.Priority());
						}
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int matchVertexID(Face f, Vertex v) {
			if (f.V(0) == v) return 0;
			if (f.V(1) == v) return 1;
			if (f.V(2) == v) return 2;
			return -1;
		}

		public EdgeCollapse(VertexPair pair, int mark, Mesh mesh, BinaryHeap<float, EdgeCollapse> heap, BVH<Face> bvh, EdgeCollapseParameter param) {
			this._pair = pair;
			this._locakMark = mark;
			this._mesh = mesh;
			this._heap = heap;
			this._bvh = bvh;
			this._param = param;
			this._priority = this.ComputePriority();
		}

		public Vertex v0 => this._pair.v0;
		public Vertex v1 => this._pair.v1;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsUpToDate() {
			var v0 = this._pair.v0;
			var v1 = this._pair.v1;
			if (v0.IsDeleted() || v1.IsDeleted() || this._locakMark < v0.iMark || this._locakMark < v1.iMark) {
				return false;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsFeasible() {
			return MeshUtil.LinkConditions(this._pair);
		}

		private int CountDifference(List<(Vector<float>, Quadric)>[] lst, PropertySetting setting, Func<Vector<float>, Vector4> getV) {
			int size = 0;
			dynamic set;
			if (setting.SampleFunc != null) {
				set = new Dictionary<Vector4, List<Vector4>>();
			}
			else {
				set = new HashSet<Vector4>();
			}
			for (int m = 0; m < lst.Length; m++) {
				set.Clear();
				for (int j = 0; j < lst[m].Count; j++) {
					var value = getV(lst[m][j].Item1);
					if (setting.SampleFunc != null) {
						var sampleV = setting.SampleFunc(value);
						List<Vector4> values = null;
						if (set.TryGetValue(sampleV, out values)) {
							int k;
							for (k = 0; k < values.Count; k++) {
								if (MeshUtil.SqrDistance(value, values[k]) < setting.SqrDistanceThreshold) {
									break;
								}
							}
							if (k == values.Count) {
								values.Add(value);
							}
						}
						else {
							set[sampleV] = new List<Vector4>() { value };
						}
					}
					else {
						set.Add(value);
					}
				}
				if (setting.SampleFunc != null) {
					foreach (var values in set) {
						size += values.Value.Count;
					}
				}
				else {
					size += set.Count;
				}
			}
			return size;
		}

		public float GetExtraWeight() {
			var weight = 0f;
			var lst = new List<(Vector<float>, Quadric)>[2] { QH.Vd(this._pair.v0), QH.Vd(this._pair.v1) };

			int index = 0;
			if (this._param.UsedProperty.HasFlag(VertexProperty.Normal)) {
				var setting = this._param.GetPropertySetting(VertexProperty.Normal);
				int size = this.CountDifference(lst, setting, (value) => {
					return new Vector4(value[index], value[index + 1], value[index + 2]);
				});
				weight += setting.ExtraWeight * (size - 2);
				index += 3;
			}
			if (this._param.UsedProperty.HasFlag(VertexProperty.Tangent)) {
				var setting = this._param.GetPropertySetting(VertexProperty.Tangent);
				int size = this.CountDifference(lst, setting, (value) => {
					return new Vector4(value[index], value[index + 1], value[index + 2], value[index + 3]);
				});
				weight += setting.ExtraWeight * (size - 2);
				index += 4;
			}
			if (this._param.UsedProperty.HasFlag(VertexProperty.Color)) {
				var setting = this._param.GetPropertySetting(VertexProperty.Color);
				int size = this.CountDifference(lst, setting, (value) => {
					return new Vector4(value[index], value[index + 1], value[index + 2], value[index + 3]);
				});
				weight += setting.ExtraWeight * (size - 2);
				index += 4;
			}
			for (int i = 0; i < Mesh.UV_COUNT; i++) {
				var pp = (VertexProperty)((int)VertexProperty.UV0 << i);
				var setting = this._param.GetPropertySetting(pp);
				if (this._param.UsedProperty.HasFlag(pp)) {
					int size = this.CountDifference(lst, setting, (value) => {
						return this._mesh.uvSizes[i] == 2 ? new Vector4(value[index], value[index + 1]) :
								this._mesh.uvSizes[i] == 3 ? new Vector4(value[index], value[index + 1], value[index + 2]) :
								new Vector4(value[index], value[index + 1], value[index + 2], value[index + 3]);
					});
					weight += setting.ExtraWeight * (size - 2);
					index += this._mesh.uvSizes[i];
				}
			}
			if (this._param.UsedProperty.HasFlag(VertexProperty.BoneWeight)) {
				int size = 0;
				var set = new HashSet<(Vector4, Vector4)>();
				for (int m = 0; m < lst.Length; m++) {
					set.Clear();
					for (int i = 0; i < lst[m].Count; i++) {
						var value = lst[m][i].Item1;
						set.Add((
							new Vector4(value[index], value[index + 1], value[index + 2], value[index + 3]),
							new Vector4(value[index + 4], value[index + 5], value[index + 6], value[index + 7])
						));
					}
					size += set.Count;
				}
				weight += this._param.GetPropertySetting(VertexProperty.BoneWeight).ExtraWeight * (size - 2);
				index += 8;
			}

			return weight;
		}

		public float ComputePriority() {
			Quadric qsum1 = new Quadric(quadricSize);
			Quadric qsum2 = new Quadric(quadricSize);
			Vector<double> min1 = Vector<double>.Build.Dense(quadricSize);
			Vector<double> min2 = Vector<double>.Build.Dense(quadricSize);
			Vector<float> property0_1 = Vector<float>.Build.Dense(propertySize);
			Vector<float> property1_1 = Vector<float>.Build.Dense(propertySize);
			Vector<float> property0_2 = Vector<float>.Build.Dense(propertySize);
			Vector<float> property1_2 = Vector<float>.Build.Dense(propertySize);
			int nProperties = this.GetProperties(property0_1, property1_1, property0_2, property1_2);

			return this.ComputeMinimalsAndPriority(min1, min2, qsum1, qsum2, property0_1, property1_1, property0_2, property1_2, nProperties);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Priority() {
			return this._priority;
		}

		private void InterpolateProperties(Face face, int id, Face mface, int mid, Vector3 mcoord, Vector3 newPos, VertexProperty property) {
			Vector3 coord, closestCoord = Vector3.zero;
			Vector3 p0 = face.P(0), p1 = face.P(1), p2 = face.P(2);
			coord = MeshUtil.BarycentricCoords(newPos, p0, p1, p2);
			bool outside = (coord.x < 0 || coord.y < 0 || coord.z < 0);
			if (outside) closestCoord = MeshUtil.PointToTriangle(p0, p1, p2, newPos).barycentric;

			if (property.HasFlag(VertexProperty.Normal)) {
				var setting = this._param.GetPropertySetting(VertexProperty.Normal);
				face.normals[id] =
					(outside && setting.InterpolateWithAdjacentFace && MeshUtil.SqrDistance(face.normals[id], mface.normals[mid]) < setting.SqrDistanceThreshold) ? mface.InterpolateNormal(mcoord) :
					(outside && setting.InterpolateClamped) ? face.InterpolateNormal(closestCoord) :
					face.InterpolateNormal(coord);
			}
			if (property.HasFlag(VertexProperty.Tangent)) {
				var setting = this._param.GetPropertySetting(VertexProperty.Tangent);
				face.tangents[id] =
					(outside && setting.InterpolateWithAdjacentFace && MeshUtil.SqrDistance(face.tangents[id], mface.tangents[mid]) < setting.SqrDistanceThreshold) ? mface.InterpolateTangent(mcoord) :
					(outside && setting.InterpolateClamped) ? face.InterpolateTangent(closestCoord) :
					face.InterpolateTangent(coord);
			}
			if (property.HasFlag(VertexProperty.Color)) {
				var setting = this._param.GetPropertySetting(VertexProperty.Color);
				face.colors[id] =
					(outside && setting.InterpolateWithAdjacentFace && MeshUtil.SqrDistance(face.colors[id], mface.colors[mid]) < setting.SqrDistanceThreshold) ? mface.InterpolateColor(mcoord) :
					(outside && setting.InterpolateClamped) ? face.InterpolateColor(closestCoord) :
					face.InterpolateColor(coord);
			}
			for (int i = 0; i < Mesh.UV_COUNT; i++) {
				var pp = (VertexProperty)((int)VertexProperty.UV0 << i);
				if (property.HasFlag(pp)) {
					var setting = this._param.GetPropertySetting(pp);
					face.uvs[i][id] =
						(outside && setting.InterpolateWithAdjacentFace && MeshUtil.SqrDistance(face.uvs[i][id], mface.uvs[i][mid]) < setting.SqrDistanceThreshold) ? mface.InterpolateUV(i, mcoord) :
						(outside && setting.InterpolateClamped) ? face.InterpolateUV(i, closestCoord) :
						face.InterpolateUV(i, coord);
				}
			}
			if (property.HasFlag(VertexProperty.BoneWeight)) {
				var setting = this._param.GetPropertySetting(VertexProperty.BoneWeight);
				face.boneWeights[id] =
					(outside && setting.InterpolateWithAdjacentFace && face.boneWeights[id] == mface.boneWeights[mid]) ? mface.InterpolateBoneWeight(mcoord) :
					(outside && setting.InterpolateClamped) ? face.InterpolateBoneWeight(closestCoord) :
					face.InterpolateBoneWeight(coord);
			}
		}

		private void InterpolateProperties(Vector3 newPos, VertexProperty property) {
			Vertex v0 = this._pair.v0;
			Vertex v1 = this._pair.v1;

			Face minf = null;
			int z0 = 0, z1 = 0;
			float minDistance = float.MaxValue;
			var vfi = new VFIterator(v0);
			while (vfi.MoveNext()) {
				if (vfi.V1() == v1 || vfi.V2() == v1) {
					var result = MeshUtil.PointToTriangle(vfi.f.P(0), vfi.f.P(1), vfi.f.P(2), newPos);
					if (result.sqrDistance < minDistance) {
						minDistance = result.sqrDistance;
						minf = vfi.f;
						z0 = vfi.z;
						z1 = (vfi.V1() == v1) ? (vfi.z + 1) % 3 : (vfi.z + 2) % 3;
					}
				}
			}

			Vector3 mcoord = MeshUtil.BarycentricCoords(newPos, minf.P(0), minf.P(1), minf.P(2));
			if (mcoord.x < 0 || mcoord.y < 0 || mcoord.z < 0) {
				mcoord = MeshUtil.PointToTriangle(minf.P(0), minf.P(1), minf.P(2), newPos).barycentric;
			}

			vfi = new VFIterator(v0);
			while (vfi.MoveNext()) {
				if (vfi.V1() == v1 || vfi.V2() == v1) {
					continue;
				}
				this.InterpolateProperties(vfi.f, vfi.z, minf, z0, mcoord, newPos, property);
			}
			vfi = new VFIterator(v1);
			while (vfi.MoveNext()) {
				if (vfi.V1() == v0 || vfi.V2() == v0) {
					continue;
				}
				this.InterpolateProperties(vfi.f, vfi.z, minf, z1, mcoord, newPos, property);
			}
		}

		private int GetIntersectionCount(Vector3 newPos) {
			int count = 0;
			Vertex v0 = this._pair.v0;
			Vertex v1 = this._pair.v1;

			var set = new HashSet<Vector3>();
			var exclude = new HashSet<Face>();
			var t = new VFIterator(v0);
			while (t.MoveNext()) {
				if (v0.pos != newPos) {
					if (t.V1() != v1) {
						set.Add(t.V1().pos);
					}
					if (t.V2() != v1) {
						set.Add(t.V2().pos);
					}
				}
				exclude.Add(t.f);
			}
			t = new VFIterator(v1);
			while (t.MoveNext()) {
				if (v1.pos != newPos) {
					if (t.V1() != v0) {
						set.Add(t.V1().pos);
					}
					if (t.V2() != v0) {
						set.Add(t.V2().pos);
					}
				}
				exclude.Add(t.f);
			}
			var hit = this._bvh.Traverse((b) => {
				foreach (var p in set) {
					if (MeshUtil.IsLineInBox(newPos, p, b)) {
						return true;
					}
				}
				return false;
			});
			for (int h = 0; h < hit.Count; h++) {
				var faces = hit[h].GObjects;
				if (faces == null) {
					continue;
				}
				for (int i = 0; i < faces.Count; i++) {
					var face = faces[i];
					if (!exclude.Contains(face)) {
						foreach (var p in set) {
							if (MeshUtil.IsLineIntersectTriangle(newPos, p, face.P(0), face.P(1), face.P(2), out Vector3 result)) {
								if (result != p && result != newPos) {
									count++;
								}
							}
						}
					}
				}
			}
			return count;
		}

		public bool Execute() {
			Quadric qsum1 = new Quadric(quadricSize);
			Quadric qsum2 = new Quadric(quadricSize);
			Quadric qsum3 = null;
			Vector<double> min1 = Vector<double>.Build.Dense(quadricSize);
			Vector<double> min2 = Vector<double>.Build.Dense(quadricSize);
			Vector<float> property0_1 = Vector<float>.Build.Dense(propertySize);
			Vector<float> property1_1 = Vector<float>.Build.Dense(propertySize);
			Vector<float> property0_2 = Vector<float>.Build.Dense(propertySize);
			Vector<float> property1_2 = Vector<float>.Build.Dense(propertySize);
			Vector<float> newProperty = Vector<float>.Build.Dense(propertySize);
			Vector<float> newProperty1 = null;
			Vector<float> newProperty2 = null;
			var qv = new List<(Vector<float>, Quadric)>();
			int nProperties;
			Vertex v0 = this._pair.v0;
			Vertex v1 = this._pair.v1;

			qsum3 = new Quadric(QH.Qd3(v0));
			qsum3.Add(QH.Qd3(v1));

			nProperties = this.GetProperties(property0_1, property1_1, property0_2, property1_2);

			this.ComputeMinimalsAndPriority(min1, min2, qsum1, qsum2, property0_1, property1_1, property0_2, property1_2, nProperties);

			Vector3 newPos = new Vector3((float)min1[0], (float)min1[1], (float)min1[2]);

			if (this._param.PreventIntersection) {
				int intersects = this.GetIntersectionCount(newPos);
				if (intersects > 0) {
					return false;
				}
			}

			VertexProperty prop = this._mesh.properties & ~VertexProperty.Position & ~this._param.UsedProperty;
			this.InterpolateProperties(newPos, prop);

			var deleted = MeshUtil.DoCollapse(this._mesh, this._pair, newPos);
			if (this._param.PreventIntersection) {
				for (int i = 0; i < deleted.Count; i++) {
					this._bvh.Remove(deleted[i]);
				}
			}

			for (int i = 0; i < propertySize; i++) {
				newProperty[i] = (float)min1[3 + i];
			}
			newProperty1 = newProperty.Clone();
			qv.Add((newProperty1, new Quadric(qsum1)));

			if (nProperties > 1) {
				for (int i = 0; i < propertySize; i++) {
					newProperty[i] = (float)min2[3 + i];
				}
				newProperty2 = newProperty.Clone();
				qv.Add((newProperty2, new Quadric(qsum2)));
			}

			var vfi = new VFIterator(v1);
			while (vfi.MoveNext()) {
				var property = vfi.f.GetPropertyS(this._param.UsedProperty, vfi.z);
				if (property.Equals(property0_1) || property.Equals(property1_1)) {
					vfi.f.SetPropertyS(this._param.UsedProperty, vfi.z, newProperty1);
				}
				else if (nProperties > 1 && (property.Equals(property0_2) || property.Equals(property1_2))) {
					vfi.f.SetPropertyS(this._param.UsedProperty, vfi.z, newProperty2);
				}
				else {
					// not in the edge, should do interploation ?
					bool exist = false;
					for (int i = 0; i < qv.Count; i++) {
						if (qv[i].Item1.Equals(property)) {
							exist = true;
							break;
						}
					}
					if (!exist) {
						Quadric newq = null;
						if (QH.Contains(v0, property)) {
							newq = new Quadric(QH.Qd(v0, property));
							newq.Sum3(QH.Qd3(v1), property);
						}
						else if (QH.Contains(v1, property)) {
							newq = new Quadric(QH.Qd(v1, property));
							newq.Sum3(QH.Qd3(v0), property);
						}
						qv.Add((property.Clone(), newq));
					}
				}
				if (this._param.PreventIntersection) {
					this._bvh.MarkForUpdate(vfi.f);
				}
			}
			if (this._param.PreventIntersection) {
				this._bvh.Optimize();
			}
			QH.Qd3(v1, qsum3);
			QH.Vd(v1, qv);
			return true;
		}

		public void UpdateHeap() {
			EdgeCollapse.globalMark++;
			var v0 = this._pair.v0;
			var v1 = this._pair.v1;
			v1.iMark = EdgeCollapse.globalMark;

			VFIterator vfi = new VFIterator(v1);
			while (vfi.MoveNext()) {
				vfi.V1().ClearVisited();
				vfi.V2().ClearVisited();
			}

			vfi.Reset();
			while (vfi.MoveNext()) {
				if (!vfi.V1().IsVisited() && vfi.V1().IsWritable()) {
					vfi.V1().SetVisited();
					var collapse = new EdgeCollapse(new VertexPair(vfi.V0(), vfi.V1()), globalMark, this._mesh, this._heap, this._bvh, this._param);
					this._heap.Enqueue(collapse, collapse.Priority());
				}

				if (!vfi.V2().IsVisited() && vfi.V2().IsWritable()) {
					vfi.V2().SetVisited();
					var collapse = new EdgeCollapse(new VertexPair(vfi.V0(), vfi.V2()), globalMark, this._mesh, this._heap, this._bvh, this._param);
					this._heap.Enqueue(collapse, collapse.Priority());
				}
			}
		}

		private float ComputeMinimalsAndPriority(Vector<double> min1, Vector<double> min2, Quadric qsum1, Quadric qsum2,
			Vector<float> property0_1, Vector<float> property1_1, Vector<float> property0_2, Vector<float> property1_2, int nProperties) {

			var tmp1 = Vector<double>.Build.Dense(quadricSize);
			var tmp2 = Vector<double>.Build.Dense(quadricSize);
			float priority1, priority2;

			tmp1[0] = this._pair.v0.pos.x;
			tmp1[1] = this._pair.v0.pos.y;
			tmp1[2] = this._pair.v0.pos.z;
			for(int i = 0; i < property0_1.Count; i++) {
				tmp1[i + 3] = property0_1[i];
			}

			tmp2[0] = this._pair.v1.pos.x;
			tmp2[1] = this._pair.v1.pos.y;
			tmp2[2] = this._pair.v1.pos.z;
			for (int i = 0; i < property1_1.Count; i++) {
				tmp2[i + 3] = property1_1[i];
			}

			QH.Qd(this._pair.v0, property0_1).CopyTo(qsum1);
			qsum1.Add(QH.Qd(this._pair.v1, property1_1));

			this.ComputeMinimal(min1, tmp1, tmp2, qsum1);
			priority1 = this.ComputePropertyPriority(min1, qsum1);

			if (nProperties < 2) {
				return priority1 * (1 + this.GetExtraWeight());
			}

			for (int i = 0; i < property0_2.Count; i++) {
				tmp1[i + 3] = property0_2[i];
			}
			for (int i = 0; i < property1_2.Count; i++) {
				tmp2[i + 3] = property1_2[i];
			}

			QH.Qd(this._pair.v0, property0_2).CopyTo(qsum2);
			qsum2.Add(QH.Qd(this._pair.v1, property1_2));

			this.ComputeMinimal(min2, tmp1, tmp2, qsum2);
			priority2 = this.ComputePropertyPriority(min2, qsum2);

			if (priority1 > priority2) {
				this.ComputeMinimalWithGeoContraints(min2, tmp1, tmp2, qsum2, min1);
				priority2 = this.ComputePropertyPriority(min2, qsum2);
			}
			else {
				this.ComputeMinimalWithGeoContraints(min1, tmp1, tmp2, qsum1, min2);
				priority1 = this.ComputePropertyPriority(min1, qsum1);
			}

			this._priority = Mathf.Max(priority1, priority2) * (1 + this.GetExtraWeight());

			return this._priority;
		}

		private int GetProperties(Vector<float> property0_1, Vector<float> property1_1, Vector<float> property0_2, Vector<float> property1_2) {
			int npropertys = 0;

			var vfi = new VFIterator(this._pair.v0);
			while (vfi.MoveNext()) {
				if (vfi.f.V(0) == this._pair.v1 || vfi.f.V(1) == this._pair.v1 || vfi.f.V(2) == this._pair.v1) {
					if (npropertys == 0) {
						vfi.f.GetPropertyS(this._param.UsedProperty, matchVertexID(vfi.f, this._pair.v0)).CopyTo(property0_1);
						vfi.f.GetPropertyS(this._param.UsedProperty, matchVertexID(vfi.f, this._pair.v1)).CopyTo(property1_1);
					}
					else {
						vfi.f.GetPropertyS(this._param.UsedProperty, matchVertexID(vfi.f, this._pair.v0)).CopyTo(property0_2);
						vfi.f.GetPropertyS(this._param.UsedProperty, matchVertexID(vfi.f, this._pair.v1)).CopyTo(property1_2);

						if (property0_1.Equals(property0_2) && property1_1.Equals(property1_2)) {
							return 1;
						}
						else {
							return 2;
						}
					}
					npropertys++;
				}
			}
			return npropertys;
		}

		private void ComputeMinimal(Vector<double> vv, Vector<double> v0, Vector<double> v1, Quadric qsum) {
			double min = double.MaxValue;

			if (this._param.OptimalPlacement) {
				bool rt = qsum.Minimum(vv);
				if (rt) {
					return;
				}
			}

			double step = this._param.OptimalPlacement ? (double)1 / (this._param.OptimalSampleCount + 1) : 1;

			for (double t = 0; t <= 1; t += step) {
				var v = t * v1 + (1 - t) * v0;

				double q = qsum.Apply(v);
				if (q < min) {
					min = q;
					v.CopyTo(vv);
				}
			}
		}

		private float ComputePropertyPriority(Vector<double> vv, Quadric qsum) {

			Vertex v0 = this._pair.v0;
			Vertex v1 = this._pair.v1;

			Vector3 oldPos0 = v0.pos;
			Vector3 oldPos1 = v1.pos;

			v0.pos = new Vector3((float)vv[0], (float)vv[1], (float)vv[2]);
			v1.pos = v0.pos;

			double quadErr = qsum.Apply(vv);

			double qt, minQual = double.MaxValue;
			double ndiff, minCos = double.MaxValue;

			VFIterator vfi = new VFIterator(this._pair.v0);
			while (vfi.MoveNext()) {
				if (vfi.f.V(0) != v1 && vfi.f.V(1) != v1 && vfi.f.V(2) != v1) {
					qt = vfi.f.GetQuality();
					if (qt < minQual) {
						minQual = qt;
					}
					if (this._param.NormalCheck) {
						ndiff = Vector3.Dot(MeshUtil.FaceNormal(vfi.f), vfi.f.faceNormal);
						if (ndiff < minCos) {
							minCos = ndiff;
						}
					}
				}
			}
			vfi = new VFIterator(this._pair.v1);
			while (vfi.MoveNext()) {
				if (vfi.f.V(0) != v0 && vfi.f.V(1) != v0 && vfi.f.V(2) != v0) {
					qt = vfi.f.GetQuality();
					if (qt < minQual) {
						minQual = qt;
					}
					if (this._param.NormalCheck) {
						ndiff = Vector3.Dot(MeshUtil.FaceNormal(vfi.f), vfi.f.faceNormal);
						if (ndiff < minCos) {
							minCos = ndiff;
						}
					}
				}
			}

			if (minQual > this._param.QualityThr) minQual = this._param.QualityThr;
			if (quadErr < this._param.QuadricEpsilon) quadErr = this._param.QuadricEpsilon;

			this._priority = (float)(quadErr / minQual);

			if (this._param.NormalCheck) {
				if (minCos < this._param.NormalCosineThr) {
					this._priority *= 1000;
				}
			}

			v0.pos = oldPos0;
			v1.pos = oldPos1;
			return this._priority;
		}

		private void ComputeMinimalWithGeoContraints(Vector<double> vv, Vector<double> v0, Vector<double> v1, Quadric qsum, Vector<double> geo) {
			double min = double.MaxValue;

			if (this._param.OptimalPlacement) {
				bool rt = qsum.MinimumWithGeoContraints(vv, geo);
				if (rt) {
					return;
				}
			}

			var v = Vector<double>.Build.Dense(vv.Count);
			v[0] = geo[0]; v[1] = geo[1]; v[2] = geo[2];

			double step = this._param.OptimalPlacement ? (double)1 / (this._param.OptimalSampleCount + 1) : 1;

			for (double t = 0; t <= 1; t += step) {
				for (int i = 3; i < v.Count; i++) {
					v[i] = t * v1[i] + (1 - t) * v0[i];
				}

				double q = qsum.Apply(v);
				if (q < min) {
					min = q;
					v.CopyTo(vv);
				}
			}
		}
	}
}
