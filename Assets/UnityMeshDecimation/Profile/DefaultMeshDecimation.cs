using UnityEngine;
using UnityMeshDecimation.Internal;

#if UNITY_EDITOR
using UnityEditor;
using UnityMeshDecimation.UI;
#endif

namespace UnityMeshDecimation {
	[CreateAssetMenu(menuName = "MeshDecimation/DefaultMeshDecimation")]
	public class DefaultMeshDecimation : MeshDecimationProfile {

		public DefaultMeshDecimation() {
			
		}

	}

#if UNITY_EDITOR
	[CustomEditor(typeof(DefaultMeshDecimation))]
	public class DefaultMeshDecimationInspector : Editor {

		private DefaultMeshDecimation _mTarget;
		private VertexProperty _selected = VertexProperty.UV0;

		void OnEnable() {
			this._mTarget = this.target as DefaultMeshDecimation;
		}

		public override void OnInspectorGUI() {
			var collapseParam = this._mTarget.parameter;

			EasyGUILayout.EnumMaskField("Used Property", ref collapseParam.UsedProperty, this._mTarget);
			EasyGUILayout.DoubleField("Boundary Weight", ref collapseParam.BoundaryWeight, this._mTarget);
			EasyGUILayout.BoolField("Check Normal", ref collapseParam.NormalCheck, this._mTarget);
			EasyGUILayout.DoubleField("Normal Cosine Threshold", ref collapseParam.NormalCosineThr, this._mTarget);
			EasyGUILayout.BoolField("Optimal Placement", ref collapseParam.OptimalPlacement, this._mTarget);
			EasyGUILayout.IntField("Optimal Sample Count", ref collapseParam.OptimalSampleCount, this._mTarget);
			EasyGUILayout.BoolField("Preserve Boundary", ref collapseParam.PreserveBoundary, this._mTarget);
			EasyGUILayout.DoubleField("Quadric Epsilon", ref collapseParam.QuadricEpsilon, this._mTarget);
			EasyGUILayout.DoubleField("Quality Threshold", ref collapseParam.QualityThr, this._mTarget);
			EasyGUILayout.BoolField("Quality Quadric", ref collapseParam.QualityQuadric, this._mTarget);
			EasyGUILayout.BoolField("Prevent Intersection", ref collapseParam.PreventIntersection, this._mTarget);

			EditorGUILayout.Space();
			this._selected = (VertexProperty)EditorGUILayout.EnumPopup(this._selected);
			this.DrawProperty(this._selected, collapseParam.GetPropertySetting(this._selected), !collapseParam.UsedProperty.HasFlag(this._selected));
		}

		private void DrawProperty(VertexProperty prop, EdgeCollapseParameter.PropertySetting setting, bool disabled) {
			EditorGUI.indentLevel++;
			EditorGUI.BeginDisabledGroup(disabled);
			EasyGUILayout.FloatField("Extra Weight", ref setting.ExtraWeight, this._mTarget);
			EditorGUI.EndDisabledGroup();
			EasyGUILayout.BoolField("Interpolate With Adjacent Face", ref setting.InterpolateWithAdjacentFace, this._mTarget);
			EasyGUILayout.BoolField("Interpolate Clamped", ref setting.InterpolateClamped, this._mTarget);
			EasyGUILayout.FloatField("Squared Distance Threshold", ref setting.SqrDistanceThreshold, this._mTarget);
			EditorGUI.indentLevel--;
		}
	}
#endif
}
