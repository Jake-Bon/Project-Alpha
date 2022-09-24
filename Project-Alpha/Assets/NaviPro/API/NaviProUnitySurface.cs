using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[AddComponentMenu("NaviPro/NaviPro Unity Surface")]
[DefaultExecutionOrder(-3)]
public class NaviProUnitySurface : NaviProUnitySource
{
	[Tooltip("Whether the given surface should be walkable or not. Surfaces that are not walkable are treated as obstacles.")]
	public bool Walkable = true;

	override public List<NavMeshObject> GetSourceObjects()
	{
		var sources = GetComponentsInChildren<MeshFilter>().Select(filter => new NavMeshObjectMesh(filter.sharedMesh, filter.transform.localToWorldMatrix, Walkable) as NavMeshObject).ToList();
		sources.AddRange(GetComponentsInChildren<Terrain>().Select(terrain => new NavMeshObjectTerrain(terrain, terrain.transform.localToWorldMatrix, Walkable) as NavMeshObject).ToList());
		return sources;
	}

	void Start()
	{
		// Force the creation of NaviProCore if not yet created
		var core = NaviProCore.Instance;
	}
}


#if UNITY_EDITOR
[CustomEditor(typeof(NaviProUnitySurface))]
public class NaviProUnitySurfaceEditor : Editor
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
