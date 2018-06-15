using BeatThat.ManagePrefabInstances;
using UnityEditor;
using UnityEngine;

namespace BeatThat.Placements
{
    [CustomEditor(typeof(PrefabPlacement), true)]
	[CanEditMultipleObjects]
	public class PrefabPlacementEditor : UnityEditor.Editor 
	{

		override public void OnInspectorGUI()
		{
			var bkgColorSave = GUI.backgroundColor;
			var fgColorSave = GUI.color;

			PrefabPlacement p = (target as PrefabPlacement);

			if (!p.isPrefabSet) {
				GUI.backgroundColor = Color.yellow;
				EditorGUILayout.HelpBox ("Required 'Prefab' property is not assigned", MessageType.Warning);
			}


			base.OnInspectorGUI();

			GUI.backgroundColor = bkgColorSave;

			if (!p.isPrefabSet) {
				return;
			}

			p.OnInspectorGUI_EditPrefabs ();

			GUI.backgroundColor = bkgColorSave;
		}
	}
}

