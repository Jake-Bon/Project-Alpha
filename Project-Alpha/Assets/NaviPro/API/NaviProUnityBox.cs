using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[AddComponentMenu("NaviPro/NaviPro Unity Box")]
[DefaultExecutionOrder(-3)]
public class NaviProUnityBox : NaviProUnitySource
{
	public Bounds Box;
	[Tooltip("Whether the surface of the box should be walkable. The box volume itself is always treated as an obstacle.")]
	public bool Walkable = true;

	override public List<NavMeshObject> GetSourceObjects()
	{
		List<NavMeshObject> objects = new List<NavMeshObject>();
		objects.Add(new NavMeshObjectBox(Vector3.Scale(Box.size, transform.lossyScale), Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one) * Matrix4x4.TRS(Vector3.Scale(Box.center, transform.lossyScale), Quaternion.identity, Vector3.one), Walkable) as NavMeshObject);
		return objects;
	}

	void Start()
	{
		// Force the creation of NaviProCore if not yet created
		var core = NaviProCore.Instance;
	}

#if UNITY_EDITOR
	public void OnDrawGizmosSelected()
	{
	}
#endif
}


#if UNITY_EDITOR
[CustomEditor(typeof(NaviProUnityBox))]
public class NaviProUnityBoxEditor : Editor
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


	protected void OnSceneGUI()
	{
		NaviProUnityBox obj = (NaviProUnityBox)target;

		float lineWidth = EditorGUIUtility.pixelsPerPoint * 4.0f;
		Color baseColor = Color.green;
		if (!obj.Walkable)
			baseColor = Color.red;

		Handles.matrix = Matrix4x4.TRS(obj.transform.position, obj.transform.rotation, obj.transform.lossyScale);
		Handles.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.8f);
		Handles.DrawWireCube(obj.Box.center, obj.Box.size);
	}
}
#endif