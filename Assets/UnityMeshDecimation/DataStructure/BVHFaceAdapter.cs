using DataStructures;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMeshDecimation.Internal {
	public class BVHFaceAdapter : IBVHNodeAdapter<Face> {

		private BVH<Face> _bvh;
		private Dictionary<Face, BVHNode<Face>> _gameObjectToLeafMap = new Dictionary<Face, BVHNode<Face>>();
		private Dictionary<Face, (Vector3 pos, float radius)> _boundingSphere = new Dictionary<Face, (Vector3 pos, float radius)>();

		BVH<Face> IBVHNodeAdapter<Face>.BVH {
			get => this._bvh;
			set => this._bvh = value;
		}

		//TODO: this is not used?
		public void CheckMap(Face obj) {
			if (!this._gameObjectToLeafMap.ContainsKey(obj)) {
				throw new Exception("missing map for shuffled child");
			}
		}

		public BVHNode<Face> GetLeaf(Face obj) {
			return this._gameObjectToLeafMap[obj];
		}

		public Vector3 GetObjectPos(Face obj) {
			return this.GetBoundingSphere(obj).pos;
		}

		public float GetRadius(Face obj) {
			return this.GetBoundingSphere(obj).radius;
		}

		public void MapObjectToBVHLeaf(Face obj, BVHNode<Face> leaf) {
			this._gameObjectToLeafMap[obj] = leaf;
		}

		// this allows us to be notified when an object moves, so we can adjust the BVH
		public void OnPositionOrSizeChanged(Face changed) {
			var sphere = this.MakeMinimumBoundingSphere(changed.P(0), changed.P(1), changed.P(2));
			this._boundingSphere[changed] = sphere;

			// the SSObject has changed, so notify the BVH leaf to refit for the object
			this._gameObjectToLeafMap[changed].RefitObjectChanged(this, changed);
		}

		public void UnmapObject(Face obj) {
			this._gameObjectToLeafMap.Remove(obj);
		}

		private (Vector3 pos, float radius) GetBoundingSphere(Face obj) {
			if (this._boundingSphere.TryGetValue(obj, out (Vector3 pos, float radius) sphere)) {
				return sphere;
			}
			sphere = this.MakeMinimumBoundingSphere(obj.P(0), obj.P(1), obj.P(2));
			this._boundingSphere[obj] = sphere;
			return sphere;
		}

		private (Vector3, float) MakeMinimumBoundingSphere(Vector3 p1, Vector3 p2, Vector3 p3) {
			// Calculate relative distances
			float A = (p1 - p2).magnitude;
			float B = (p2 - p3).magnitude;
			float C = (p3 - p1).magnitude;

			// Re-orient triangle (make A longest side)
			Vector3 a = p3, b = p1, c = p2;
			if (B < C) {
				Swap(ref B, ref C);
				Swap(ref b, ref c);
			}
			if (A < B) {
				Swap(ref A, ref B);
				Swap(ref a, ref b);
			}

			Vector3 pos = Vector3.zero;
			float radius = 0;
			// If obtuse, just use longest diameter, otherwise circumscribe
			if ((B * B) + (C * C) <= (A * A)) {
				radius = A / 2;
				pos = (b + c) / 2;
			} else {
				// http://en.wikipedia.org/wiki/Circumscribed_circle
				Vector3 alpha = a - c, beta = b - c;
				Vector3 alphaCrossbeta = Vector3.Cross(alpha, beta);

				float sinC =  (alphaCrossbeta.magnitude) / (A * B);
				radius = (a - b).magnitude / (2 * sinC);

				pos = c + Vector3.Cross(beta * alpha.sqrMagnitude - alpha * beta.sqrMagnitude, alphaCrossbeta) / (2 * alphaCrossbeta.sqrMagnitude);
			}
			return (pos, radius);
		}

		private static void Swap<T>(ref T lhs, ref T rhs) {
			T temp;
			temp = lhs;
			lhs = rhs;
			rhs = temp;
		}
	}
}
