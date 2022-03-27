using DataStructures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityMeshDecimation.Internal;
using Debug = UnityEngine.Debug;
using Mesh = UnityMeshDecimation.Internal.Mesh;
using UnityMesh = UnityEngine.Mesh;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityMeshDecimation {

	[Serializable]
	public sealed class TargetConditions {
		/// <summary>
		/// Target count of triangles
		/// </summary>
		public int faceCount;

		/// <summary>
		/// Target count of vertices
		/// </summary>
		public int vertexCount;

		/// <summary>
		/// Max operations of collapsions
		/// </summary>
		public int maxOperations;

		/// <summary>
		/// Max error of collapsion
		/// </summary>
		public float maxMetrix;

		/// <summary>
		/// Max time of optimzation
		/// </summary>
		public float maxTime;

		public TargetConditions() {
			this.faceCount = 0;
			this.vertexCount = 0;
			this.maxOperations = 0;
			this.maxMetrix = 0;
			this.maxTime = 0;
		}
	}

	public sealed class UnityMeshDecimation {

		private const int PRINT_FREQUENCY = 200;
		private const int HEAP_RATIO = 4;

		private Mesh _mesh;
		private BinaryHeap<float, EdgeCollapse> _heap;
		private BVH<Face> _bvh;
		private TargetConditions _targetConditions;

		private int _initVertexCount;
		private int _initFaceCount;
		private float _currMetric;
		private int _currOperations;

		private bool _showProgress;
		

		private Stopwatch _stopWatch;
		private Stopwatch stopWatch {
			get {
				if (this._stopWatch == null) {
					this._stopWatch = new Stopwatch();
				}
				return this._stopWatch;
			}
		}

		#region API
		public void Execute(UnityMesh mesh, EdgeCollapseParameter collapseParam, TargetConditions targetConditions, bool showProgress = false) {
			this._showProgress = showProgress;
			this.InitializeMesh(mesh, collapseParam, targetConditions);
			this.OptimizeMesh();
		}

		public void Execute(UnityMesh mesh, EdgeCollapseParameter collapseParam, int targetTriangles, float targetMetric, bool showProgress = false) {
			var targetOptions = new TargetConditions() {
				faceCount = targetTriangles,
				maxMetrix = targetMetric,
			};
			this.Execute(mesh, collapseParam, targetOptions, showProgress);
		}

		public void ToMesh(UnityMesh m) {
			this._mesh?.ToMesh(m);
		}

		public UnityMesh ToMesh() {
			return this._mesh?.ToMesh();
		}

		public Face GetSelectedFace(Vector3 start, Vector3 end) {
			var hit = this._bvh.Traverse((b) => {
				return MeshUtil.IsLineInBox(start, end, b);
			});

			Face selected = null;
			float minD = float.MaxValue;
			for (int h = 0; h < hit.Count; h++) {
				var faces = hit[h].GObjects;
				if (faces == null) {
					continue;
				}
				for (int i = 0; i < faces.Count; i++) {
					var face = faces[i];
					if (MeshUtil.IsLineIntersectTriangle(start, end, face.P(0), face.P(1), face.P(2), out Vector3 result)) {
						Vector3 center = (face.P(0) + face.P(1) + face.P(2)) / 3;
						float d = (center - start).sqrMagnitude;
						if (d < minD) {
							minD = d;
							selected = face;
						}
					}
				}
			}
			return selected;
		}

		public EdgeCollapse[] GetFaceCollapse(Face face) {
			var collapse = new List<EdgeCollapse>();
			using (var tEnu = this._heap.GetEnumerator()) {
				while (tEnu.MoveNext()) {
					var edge = tEnu.Current;
					var v0 = edge.v0;
					var v1 = edge.v1;
					if ((v0 == face.V(0) && v1 == face.V(1)) || (v0 == face.V(1) && v1 == face.V(0)) ||
						(v0 == face.V(0) && v1 == face.V(2)) || (v0 == face.V(2) && v1 == face.V(0)) ||
						(v0 == face.V(1) && v1 == face.V(2)) || (v0 == face.V(2) && v1 == face.V(1))) {
						collapse.Add(edge);
					}
				}
			}
			return collapse.ToArray();
		}
		#endregion

		#region Internal Methods
		private void InitializeMesh(UnityMesh mesh, EdgeCollapseParameter collapseParam, TargetConditions targetOptions) {
#if UNITY_EDITOR
			if (this._showProgress) {
				EditorUtility.DisplayProgressBar("Initialization", "Building mesh topology...", 0.1f);
			}
#endif
			this.stopWatch.Restart();

			this._targetConditions = targetOptions;
			this._mesh = new Mesh(mesh);
			this._heap = new BinaryHeap<float, EdgeCollapse>(HEAP_RATIO * this._mesh.faceCount, float.MinValue, float.MaxValue);
			this._bvh = new BVH<Face>(new BVHFaceAdapter(), collapseParam.PreventIntersection ? this._mesh.faces : new List<Face>(0));

			this._mesh.InitIMark();
			EdgeCollapse.globalMark = 0;
			EdgeCollapse.Init(this._mesh, this._heap, this._bvh, collapseParam);

			this._initVertexCount = this._mesh.vertexCount;
			this._initFaceCount = this._mesh.faceCount;
			this._currMetric = this._heap.First.Priority();

			this._stopWatch.Stop();
			Debug.Log($"<color=cyan>Initialization time: {this._stopWatch.ElapsedMilliseconds / 1000f}</color>");

#if UNITY_EDITOR
			if (this._showProgress) {
				EditorUtility.ClearProgressBar();
			}
#endif
		}

		private void OptimizeMesh() {
			this.stopWatch.Restart();

			this._currOperations = 0;
			while (!this.IsGoalReached() && this._heap.Count > 0) {
				var locMod = this._heap.Dequeue();
				this._currMetric = locMod.Priority();

				if (locMod.IsUpToDate() && locMod.IsFeasible()) {
					try {
						if (locMod.Execute()) {
							locMod.UpdateHeap();
							this._currOperations++;
						}
					}
					catch (Exception e) {
						Debug.LogException(e);
						break;
					}
					if (this._currOperations % PRINT_FREQUENCY == 0) {
						var status = this.GetCurrentStatus();
						Debug.Log(status);
#if UNITY_EDITOR
						if (this._showProgress) {
							EditorUtility.DisplayProgressBar("Optimization", status, this.GetProgress());
						}
#endif
					}
				}
			}
			Debug.Log(this.GetCurrentStatus());

			this.stopWatch.Stop();
			Debug.Log($"<color=cyan>Optimization time: {this.stopWatch.ElapsedMilliseconds / 1000f}</color>");
			Debug.Log($"<color=lime>Original Face: {this._initFaceCount}, Final Face: {this._mesh.faceCount}, Ratio: {this._mesh.faceCount * 100 / this._initFaceCount}%</color>");

#if UNITY_EDITOR
			if (this._showProgress) {
				EditorUtility.ClearProgressBar();
			}
#endif
		}

		private bool IsGoalReached() {
			if (this._targetConditions.faceCount > 0 && this._mesh.faceCount <= this._targetConditions.faceCount) return true;
			if (this._targetConditions.vertexCount > 0 && this._mesh.vertexCount <= this._targetConditions.vertexCount) return true;
			if (this._targetConditions.maxOperations > 0 && this._currOperations > this._targetConditions.maxOperations) return true;
			if (this._targetConditions.maxMetrix > 0 && this._currMetric > this._targetConditions.maxMetrix) return true;
			if (this._targetConditions.maxTime > 0 && this._stopWatch.ElapsedMilliseconds > this._targetConditions.maxTime * 1000) return true;
			return false;
		}

		private float GetProgress() {
			float tmp, progress = 0;
			if (this._targetConditions.faceCount > 0) {
				tmp = (this._initFaceCount - this._mesh.faceCount) / (this._initFaceCount - this._targetConditions.faceCount);
				if (tmp > progress) progress = tmp;
			}
			if (this._targetConditions.vertexCount > 0) {
				tmp = (this._initVertexCount - this._mesh.vertexCount) / (this._initVertexCount - this._targetConditions.vertexCount);
				if (tmp > progress) progress = tmp;
			}
			if (this._targetConditions.maxOperations > 0) {
				tmp = this._currOperations / this._targetConditions.maxOperations;
				if (tmp > progress) progress = tmp;
			}
			if (this._targetConditions.maxMetrix > 0) {
				tmp = this._currMetric / this._targetConditions.maxMetrix;
				if (tmp > progress) progress = tmp;
			}
			if (this._targetConditions.maxTime > 0) {
				tmp = this._stopWatch.ElapsedMilliseconds / (this._targetConditions.maxTime * 1000);
				if (tmp > progress) progress = tmp;
			}
			return progress;
		}

		private string GetCurrentStatus() {
			return $"vert: {this._mesh.vertexCount} face: {this._mesh.faceCount} bvh size: {this._bvh.nodeCount} heap size: {this._heap.Count} error: {this._currMetric}";
		}
		#endregion
	}
}