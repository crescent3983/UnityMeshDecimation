#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace UnityMeshDecimation.UI {
	public static class EasyGUILayout {
		public static void BeginRegion(string title) {
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField(title, EditorStyles.toolbarButton);
			EditorGUILayout.Space();
			EditorGUI.indentLevel++;
		}

		public static void EndRegion() {
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
		}

		public static bool ObjectField<T>(string title, ref T value, Object target = null, params GUILayoutOption[] options) where T : UnityEngine.Object {
			var newValue = EditorGUILayout.ObjectField(title, value, typeof(T), true, options);
			if (newValue != value) {
				value = (T)newValue;
				if (target) EditorUtility.SetDirty(target);
				return true;
			}
			return false;
		}

		public static bool IntField(string title, ref int value, Object target = null) {
			var newValue = EditorGUILayout.IntField(title, value);
			if (newValue != value) {
				value = newValue;
				if (target) EditorUtility.SetDirty(target);
				return true;
			}
			return false;
		}

		public static bool FloatField(string title, ref float value, Object target = null) {
			var newValue = EditorGUILayout.FloatField(title, value);
			if (newValue != value) {
				value = newValue;
				if (target) EditorUtility.SetDirty(target);
				return true;
			}
			return false;
		}

		public static bool DoubleField(string title, ref double value, Object target = null) {
			var newValue = EditorGUILayout.DoubleField(title, value);
			if (newValue != value) {
				value = newValue;
				if (target) EditorUtility.SetDirty(target);
				return true;
			}
			return false;
		}

		public static bool BoolField(string title, ref bool value, Object target = null) {
			var newValue = EditorGUILayout.Toggle(title, value);
			if (newValue != value) {
				value = newValue;
				if (target) EditorUtility.SetDirty(target);
				return true;
			}
			return false;
		}

		public static bool TextField(string title, ref string value, Object target = null) {
			var newValue = EditorGUILayout.TextField(title, value);
			if (newValue != value) {
				value = newValue;
				if (target) EditorUtility.SetDirty(target);
				return true;
			}
			return false;
		}

		public static bool EnumMaskField<T>(string title, ref T value, Object target = null) where T : System.Enum {
			var newValue = (T)EditorGUILayout.EnumFlagsField(title, value);
			if (!newValue.Equals(value)) {
				value = newValue;
				if (target) EditorUtility.SetDirty(target);
				return true;
			}
			return false;
		}

		public static bool FilePathField(string title, ref string value, string extension, string btnName = "Select", Object target = null) {
			EditorGUILayout.BeginHorizontal();
			var newValue = EditorGUILayout.TextField(title, value);
			if(GUILayout.Button(btnName, GUILayout.Width(50))){
				newValue = EditorUtility.SaveFilePanelInProject(title, "", extension, "");
			}
			EditorGUILayout.EndHorizontal();
			if (newValue != value) {
				value = newValue;
				if (target) EditorUtility.SetDirty(target);
				return true;
			}
			return false;
		}
	}
}
#endif