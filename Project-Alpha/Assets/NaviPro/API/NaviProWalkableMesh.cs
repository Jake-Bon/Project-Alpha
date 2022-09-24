using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[AddComponentMenu("NaviPro/NaviPro Walkable Mesh")]
public class NaviProWalkableMesh : MonoBehaviour
{
	public Mesh GetMesh()
	{
		return GetComponent<MeshFilter>().sharedMesh;
	}
}


#if UNITY_EDITOR
[CustomEditor(typeof(NaviProWalkableMesh))]
public class NaviProWalkableMeshEditor : Editor
{
	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.Space();
		var property = serializedObject.GetIterator();
		bool first = true;
		while (property.NextVisible(first))
		{
			if (property.displayName != "Script")
				EditorGUILayout.PropertyField(property);
			first = false;
		}

		serializedObject.ApplyModifiedProperties();
	}
}
#endif
