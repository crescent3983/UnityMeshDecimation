using UnityEngine;
using UnityMeshDecimation.Internal;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityMeshDecimation.UI;
#endif

namespace UnityMeshDecimation {
	[CreateAssetMenu(menuName = "MeshDecimation/GBVSMeshDecimation")]
	public class GBVSMeshDecimation : MeshDecimationProfile {

		[NonSerialized]
		public Texture2D baseTexture;
		[NonSerialized]
		public Texture2D ilmTexture;

		private bool _revertBase;
		private bool _revertILM;

		public GBVSMeshDecimation() {
			this.parameter.QualityThr = 0.3f;
			this.parameter.OptimalPlacement = true;
			this.parameter.OptimalSampleCount = 3;
			this.parameter.PreserveBoundary = true;
			this.parameter.BoundaryWeight = 1;
			this.parameter.QualityQuadric = false;
			this.parameter.NormalCheck = true;
			this.parameter.PreventIntersection = true;

			this.parameter.UsedProperty = VertexProperty.UV0;
			this.parameter.GetPropertySetting(VertexProperty.UV0).ExtraWeight = 1e+4f;
		}

		public override bool BeforeOptimize() {
			if (!this.baseTexture || !this.ilmTexture) {
				return false;
			}
			if (!this.baseTexture.isReadable) {
				if (SetTextureReadable(this.baseTexture)) {
					this._revertBase = true;
				}
				else {
					return false;
				}
			}
			if (!this.ilmTexture.isReadable) {
				if (SetTextureReadable(this.ilmTexture)) {
					this._revertILM = true;
				}
				else {
					return false;
				}
			}
			this.parameter.GetPropertySetting(VertexProperty.UV0).SampleFunc = this.GetUVValue;
			return true;
		}

		public override void AfterOptimize() {
			if (this._revertBase) {
				SetTextureUnReadable(this.baseTexture);
			}
			if (this._revertILM) {
				SetTextureUnReadable(this.ilmTexture);
			}
		}

		private Vector4 GetUVValue(Vector4 value) {
			var color = this.baseTexture.GetPixel((int)(this.baseTexture.width * value.x), (int)(this.baseTexture.height * value.y));
			var line = this.ilmTexture.GetPixel((int)(this.ilmTexture.width * value.x), (int)(this.ilmTexture.height * value.y)).a;
			return new Vector4(color.r * line, color.g * line, color.b * line, color.a * line);
		}

		private static bool SetTextureReadable(Texture2D tex) {
#if UNITY_EDITOR
			string path = AssetDatabase.GetAssetPath(tex);
			var importer = AssetImporter.GetAtPath(path);
			if (importer is TextureImporter texIMporter) {
				texIMporter.isReadable = true;
			}
			else if (importer is IHVImageFormatImporter ihvIMporter) {
				ihvIMporter.isReadable = true;
			}
			EditorUtility.SetDirty(importer);
			importer.SaveAndReimport();
			return true;
#else
			return false;
#endif
		}

		private static bool SetTextureUnReadable(Texture2D tex) {
#if UNITY_EDITOR
			string path = AssetDatabase.GetAssetPath(tex);
			var importer = AssetImporter.GetAtPath(path);
			if (importer is TextureImporter texIMporter) {
				texIMporter.isReadable = false;
			}
			else if (importer is IHVImageFormatImporter ihvIMporter) {
				ihvIMporter.isReadable = false;
			}
			EditorUtility.SetDirty(importer);
			importer.SaveAndReimport();
			return true;
#else
			return false;
#endif
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(GBVSMeshDecimation))]
	public class GBVSMeshDecimationInspector : Editor {

		private GBVSMeshDecimation _mTarget;

		void OnEnable() {
			this._mTarget = this.target as GBVSMeshDecimation;
		}

		public override void OnInspectorGUI() {
			EditorGUILayout.BeginHorizontal();
			EasyGUILayout.ObjectField("Base Texture", ref this._mTarget.baseTexture, this._mTarget);
			EasyGUILayout.ObjectField("ILM Texture", ref this._mTarget.ilmTexture, this._mTarget);
			EditorGUILayout.EndHorizontal();

			EasyGUILayout.FloatField("UV Weight", ref this._mTarget.parameter.GetPropertySetting(VertexProperty.UV0).ExtraWeight, this._mTarget);
		}
	}
#endif
}
