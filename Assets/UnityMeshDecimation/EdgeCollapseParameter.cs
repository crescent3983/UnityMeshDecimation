using System;
using UnityEngine;
using UnityMeshDecimation.Internal;

namespace UnityMeshDecimation {
	[Serializable]
	public sealed class EdgeCollapseParameter : ICloneable {

		[Serializable]
		public sealed class PropertySetting {

			/// <summary>
			/// Additional weight for property
			/// </summary>
			public float ExtraWeight;

			/// <summary>
			/// Use the adjacent face when position is outside of triangle
			/// </summary>
			public bool InterpolateWithAdjacentFace;

			/// <summary>
			/// Clamp the interpolation when position is outside of triangle
			/// </summary>
			public bool InterpolateClamped;

			/// <summary>
			/// Function used to calculate value (default is property value)
			/// </summary>
			public Func<Vector4, Vector4> SampleFunc;

			/// <summary>
			/// Max squared distance to seem as same value
			/// </summary>
			public float SqrDistanceThreshold;

			public PropertySetting() {
				this.ExtraWeight = 0;
				this.InterpolateWithAdjacentFace = true;
				this.InterpolateClamped = true;
				this.SampleFunc = null;
				this.SqrDistanceThreshold = 0.003f;
			}
		}

		[Serializable]
		public class VertexPropertySetting : SerializableDictionary<VertexProperty, PropertySetting> {}

		/// <summary>
		/// Used properties for quadric
		/// </summary>
		public VertexProperty UsedProperty;

		/// <summary>
		/// Weight when edge is a boundary
		/// </summary>
		public double BoundaryWeight;

		/// <summary>
		/// Enable check for normal change
		/// </summary>
		public bool NormalCheck;

		/// <summary>
		/// The threshold for normal change
		/// </summary>
		public double NormalCosineThr;

		/// <summary>
		/// Enable finding the best new position
		/// </summary>
		public bool OptimalPlacement;

		/// <summary>
		/// The sample count when can't find a optimal position
		/// </summary>
		public int OptimalSampleCount;

		/// <summary>
		/// Enable fixed position for boundary vertex
		/// </summary>
		public bool PreserveBoundary;

		/// <summary>
		/// The minimum quadric error
		/// </summary>
		public double QuadricEpsilon;

		/// <summary>
		/// The threshold for triangle quality (larger than this will cost no penalty)
		/// </summary>
		public double QualityThr;

		/// <summary>
		/// Addition quality quadric
		/// </summary>
		public bool QualityQuadric;

		/// <summary>
		/// Prevent intersection between faces
		/// </summary>
		public bool PreventIntersection;

		/// <summary>
		/// Settings for all properties
		/// </summary>
		[SerializeField]
		private VertexPropertySetting PropertySettings;

		public EdgeCollapseParameter() {
			this.SetDefaultParams();
		}

		public void SetDefaultParams() {
			this.UsedProperty = VertexProperty.UV0;
			this.BoundaryWeight = 0.5;
			this.NormalCheck = false;
			this.NormalCosineThr = Math.Cos(Math.PI / 2);
			this.OptimalPlacement = true;
			this.OptimalSampleCount = 1;
			this.PreserveBoundary = false;
			this.QuadricEpsilon = 1e-15;
			this.QualityThr = 0.1;
			this.QualityQuadric = false;
			this.PreventIntersection = false;
			this.PropertySettings = new VertexPropertySetting();
		}

		public PropertySetting GetPropertySetting(VertexProperty property) {
			PropertySetting setting = null;
			if (!this.PropertySettings.TryGetValue(property, out setting)) {
				setting = new PropertySetting();
				this.PropertySettings[property] = setting;
			}
			return setting;
		}

		public object Clone() {
			return this.MemberwiseClone();
		}
	}
}
