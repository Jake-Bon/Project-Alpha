using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;


[AddComponentMenu("NaviPro/NaviPro Sector")]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-3)]
public class NaviProSector : MonoBehaviour
{
	public enum CreationMethod
	{
		FromWalkableMesh,
		FromUnityNavMesh
	}

	[System.Serializable]
	public class CreationSettings
	{
		[Tooltip("Whether to create the Sector from a custom Walkable Mesh or to use Unity's NavMesh generation to construct one.")]
		public CreationMethod creationMethod = CreationMethod.FromWalkableMesh;

		[Tooltip("The maximum height at which Agents and goals are snapped to the Walkable Surface of the Sector. Larger differences in height are regarded as being off surface.")]
		[Min(0.0f)]
		public float MaximumSnappingHeight = 2.0f;
		[Tooltip("The minimal walking height to use for Unity's NavMesh generation.")]
		[Min(0.0f)]
		public float UnityNavMesh_MinHeight = 2.0f;
		[Tooltip("The maximum stepping height to use for Unity's NavMesh generation.")]
		[Min(0.0f)]
		public float UnityNavMesh_MaxStep = 1.0f;
		[Tooltip("The maximum slope angle in degrees allowed for walkable surfaces to use for Unity's NavMesh generation.")]
		[Min(0.0f)]
		public float UnityNavMesh_Slope = 20.0f;
		[Tooltip("The voxel size to use for Unity's NavMesh generation. Smaller sizes give more precision, but will build slower and will generate more complex meshes.")]
		[Min(0.0f)]
		public float UnityNavMesh_VoxelSize = 0.1f;
	}

	public CreationSettings Settings;

	float maximumSnappingHeight;


	// The reference to the created sector object
	internal NaviProNative.Sector sector;
	
	internal List<NaviProObstacle> obstacles = new List<NaviProObstacle>();
	internal List<NaviProPlatform> platforms = new List<NaviProPlatform>();

	bool created = false;

	// The bounding box containing all navigation meshes
	Bounds bounds;

	// Unity internal NavMesh data references
	NavMeshData navmeshData;
	NavMeshDataInstance navmeshDataInstance;
	bool createdUnityNavMesh = false;
	Vector3 navMeshOffsetCenter;

	// Internally, Unity NavMeshes are created side by side with some spacing between them
	// This is the next offset where a NavMesh instance should be placed
	static float nextNavMeshOffset = 0.0f;


	void OnEnable()
	{
		NaviProCore.Instance.insertSectors.Add(this);
	}
	void OnDisable()
	{
		if (NaviProCore.IsDestroyed())
			return;
		if (created)
			NaviProCore.Instance.removeSectors.Add(this);
		else
			NaviProCore.Instance.insertSectors.Remove(this);
	}


	internal Vector3 adjustHeight(Vector3 position, float maxDistance)
	{
		UnityEngine.AI.NavMeshHit hit;
		if (UnityEngine.AI.NavMesh.SamplePosition(position + navMeshOffsetCenter, out hit, maxDistance, UnityEngine.AI.NavMesh.AllAreas))
			position.y = hit.position.y;
		return position;
	}


	internal bool create(bool isBaseSector = false)
	{
		if (created)
			return false;

		if (Settings.creationMethod == CreationMethod.FromWalkableMesh)
		{
			if (!createFromWalkableMesh(isBaseSector))
				return false;
		}
		else if (Settings.creationMethod == CreationMethod.FromUnityNavMesh)
		{
			if (!createFromUnityNavMesh(isBaseSector))
				return false;
		}

		maximumSnappingHeight = Mathf.Abs(Settings.MaximumSnappingHeight);
		NaviProCore.System.SetSectorMaximumSnappingHeight(sector, maximumSnappingHeight);

		if (FindObjectOfType<NaviProDebug>())
			FindObjectOfType<NaviProDebug>().Redraw();

		created = true;
		return true;
	}

	internal void destroy(bool inEditor = false)
	{
		if (!created)
			return;

		NaviProCore.System.RemoveSector(sector);
		foreach (var obstacle in obstacles)
		{
			obstacle.mark_destroyed();
		}
		foreach (var platform in platforms)
		{
			platform.mark_destroyed();
		}

		if (createdUnityNavMesh)
		{
			navmeshDataInstance.Remove();
			if (inEditor)
				DestroyImmediate(navmeshData);
			else
				Destroy(navmeshData);
		}

		if (FindObjectOfType<NaviProDebug>())
			FindObjectOfType<NaviProDebug>().Redraw();

		created = false;
	}


	internal void checkProperties()
	{
		if (created && Mathf.Abs(Settings.MaximumSnappingHeight) != maximumSnappingHeight)
		{
			maximumSnappingHeight = Mathf.Abs(Settings.MaximumSnappingHeight);
			NaviProCore.System.SetSectorMaximumSnappingHeight(sector, maximumSnappingHeight);
		}
	}


	internal static bool canCreateBaseSector(CreationSettings baseSectorCreationSettings)
	{
		if (baseSectorCreationSettings.creationMethod == CreationMethod.FromWalkableMesh)
		{
			return GameObject.FindObjectsOfType<NaviProWalkableMesh>().Any(s => s.gameObject.GetComponentInParent<NaviProSector>() == null);
		}
		else if (baseSectorCreationSettings.creationMethod == CreationMethod.FromUnityNavMesh)
		{
			return GameObject.FindObjectsOfType<NaviProUnitySource>().Any(s => s.gameObject.GetComponentInParent<NaviProSector>() == null);
		}
		return false;
	}


	bool createFromWalkableMesh(bool isBaseSector)
	{
		IEnumerable<NaviProWalkableMesh> walkableMeshes;
		if (isBaseSector)
			walkableMeshes = GameObject.FindObjectsOfType<NaviProWalkableMesh>().Where(s => s.gameObject.GetComponentInParent<NaviProSector>() == null);
		else
			walkableMeshes = gameObject.GetComponentsInChildren<NaviProWalkableMesh>();

		int indexCount = 0;
		int vertexCount = 0;

		foreach (var walkableMesh in walkableMeshes)
		{
			Mesh mesh = walkableMesh.GetMesh();
			indexCount += mesh.triangles.Length;
			vertexCount += mesh.vertexCount;
		}

		int[] indices = new int[indexCount];
		indexCount = 0;
		float[] vertices = new float[vertexCount * 3];
		vertexCount = 0;

		foreach (var walkableMesh in walkableMeshes)
		{
			Mesh mesh = walkableMesh.GetMesh();
			int[] meshTriangles = mesh.triangles;
			Vector3[] meshVertices = mesh.vertices;
			Matrix4x4 localToWorld = walkableMesh.transform.localToWorldMatrix;
			Matrix4x4 worldToSector = transform.worldToLocalMatrix;
			Matrix4x4 localToSector = worldToSector * localToWorld;

			for (int i = 0; i < meshTriangles.Length; ++i)
			{
				indices[indexCount] = meshTriangles[i] + vertexCount;
				++indexCount;
			}

			Vector3 transformedVertex;
			for (int i = 0; i < meshVertices.Length; ++i)
			{
				transformedVertex = localToSector.MultiplyPoint3x4(meshVertices[i]);
				// Correct for Unity's Y-up system
				vertices[vertexCount * 3 + 0] = transformedVertex.x;
				vertices[vertexCount * 3 + 1] = transformedVertex.z;
				vertices[vertexCount * 3 + 2] = transformedVertex.y;
				++vertexCount;
			}
		}

		sector = NaviProCore.System.CreateSectorFromInputMesh(vertices, indices, true, 0.001f, true);
		if (sector == null)
			return false;

		return true;
	}


	// Uses the Unity NavMesh API to create a base NavMesh surface.
	// This NavMesh uses a 0 radius, so that it matches the walkable surface as closely as possible.
	// Unity uses a voxelization method internally, which allows the creation of a
	// continuous walkable surface from multiple meshes and can simplify geometry.
	bool createFromUnityNavMesh(bool isBaseSector)
	{
		NavMeshBuildSettings settings = getUnityBuildSettings();

		bounds = new Bounds();

		var sources = getBakeSources(isBaseSector, transform.worldToLocalMatrix).SelectMany(s => s.Value).ToList();
		// Ensure that the bounds do not clip any walkable surfaces
		bounds.min -= Vector3.one;
		bounds.max += Vector3.one;
		navMeshOffsetCenter = new Vector3(nextNavMeshOffset - bounds.min.x, 0.0f, 0.0f);
		for (int s = 0; s < sources.Count; ++s)
		{
			var source = sources[s];
			source.transform = Matrix4x4.Translate(navMeshOffsetCenter) * source.transform;
			sources[s] = source;
		}
		navmeshData = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, navMeshOffsetCenter, Quaternion.identity);
		navmeshDataInstance = NavMesh.AddNavMeshData(navmeshData);
		navmeshDataInstance.owner = this;

		List<int> verticesList = new List<int>();
		Dictionary<int, int> verticesSet = new Dictionary<int, int>();

		NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

		for (int i = 0; i < triangulation.vertices.Length; ++i)
		{
			if (triangulation.vertices[i].x > nextNavMeshOffset)
			{
				verticesSet.Add(i, verticesList.Count);
				verticesList.Add(i);
			}
		}

		float[] vertices = new float[verticesList.Count * 3];
		for (int i = 0; i < verticesList.Count; ++i)
		{
			// Correct for Unity's Y-up system
			vertices[i * 3] = triangulation.vertices[verticesList[i]].x - navMeshOffsetCenter.x;
			vertices[i * 3 + 1] = triangulation.vertices[verticesList[i]].z;
			vertices[i * 3 + 2] = triangulation.vertices[verticesList[i]].y;
		}

		int numIndices = 0;
		for (int i = 0; i < triangulation.indices.Length; ++i)
		{
			if (verticesSet.ContainsKey(triangulation.indices[i]))
				++numIndices;
		}
		int[] indices = new int[numIndices];
		int j = 0;
		for (int i = 0; i < triangulation.indices.Length; ++i)
		{
			if (verticesSet.ContainsKey(triangulation.indices[i]))
			{
				indices[j] = verticesSet[triangulation.indices[i]];
				++j;
			}
		}

		sector = NaviProCore.System.CreateSectorFromInputMesh(vertices, indices, true, settings.voxelSize * 0.5f, true);
		if (sector == null)
			return false;

		nextNavMeshOffset += bounds.size.x;
		createdUnityNavMesh = true;

		return true;
	}

	// Return a list of the NavMesh sources to be used in the bake
	Dictionary<NaviProUnitySource, List<NavMeshBuildSource>> getBakeSources(bool isBaseSector, Matrix4x4 worldToLocalMatrix)
	{
		Dictionary<NaviProUnitySource, List<NavMeshBuildSource>> bakeSources = new Dictionary<NaviProUnitySource, List<NavMeshBuildSource>>();

		// For a base sector, find all sources that are not under a NaviProSector component
		// For all others, find child NavMeshSource components
		List<NaviProUnitySource> sources;
		if (isBaseSector)
			sources = GameObject.FindObjectsOfType<NaviProUnitySource>().Where(s => s.gameObject.GetComponentInParent<NaviProSector>() == null).ToList();
		else
			sources = gameObject.GetComponentsInChildren<NaviProUnitySource>().ToList();

		foreach (var source in sources)
		{
			List<NavMeshBuildSource> sourceList = new List<NavMeshBuildSource>();
			foreach (var sourceObject in source.GetSourceObjects())
			{
				Bounds sourceBounds = new Bounds();
				var src = new NavMeshBuildSource();
				src.area = sourceObject.Walkable ? 0 : 1; // Fixed Walkable and Not Walkable layers
				src.transform = worldToLocalMatrix * sourceObject.Transform;

				if (sourceObject is NavMeshObjectMesh)
				{
					src.shape = NavMeshBuildSourceShape.Mesh;
					src.sourceObject = (sourceObject as NavMeshObjectMesh).Mesh;
					sourceBounds = (sourceObject as NavMeshObjectMesh).Mesh.bounds;
				}
				else if (sourceObject is NavMeshObjectBox)
				{
					if ((sourceObject as NavMeshObjectBox).Walkable)
						src.shape = NavMeshBuildSourceShape.Box;
					else
						src.shape = NavMeshBuildSourceShape.ModifierBox;
					src.size = (sourceObject as NavMeshObjectBox).Size;
					sourceBounds = new Bounds(Vector3.zero, src.size);
				}
				else if (sourceObject is NavMeshObjectTerrain)
				{
					src.shape = NavMeshBuildSourceShape.Terrain;
					src.sourceObject = (sourceObject as NavMeshObjectTerrain).Terrain.terrainData;
					sourceBounds = (sourceObject as NavMeshObjectTerrain).Terrain.terrainData.bounds;
				}

				sourceList.Add(src);

				// Grow the NavMesh bounds to encapsulate each corner point of the transformed local bounding box of this source
				if (sourceBounds.size.magnitude > 0.0f)
				{
					for (int i = 0; i < 8; ++i)
					{
						Vector3 corner = Vector3.one;
						if (i >= 4)
							corner.x *= -1;
						if (i % 4 >= 2)
							corner.y *= -1;
						if (i % 2 >= 1)
							corner.z *= -1;
						bounds.Encapsulate(src.transform.MultiplyPoint(sourceBounds.center + Vector3.Scale(sourceBounds.extents, corner)));
					}
				}
			}
			bakeSources.Add(source, sourceList);
		}
		return bakeSources;
	}

	// This will return an existing NavMeshBuildSettings if one is available
	// or create a new one. The setting mainly determines the used agent size (0) and voxel size.
	NavMeshBuildSettings getUnityBuildSettings()
	{
		NavMeshBuildSettings settings;
		if (NavMesh.GetSettingsCount() < 1)
			settings = NavMesh.CreateSettings();
		else
			settings = NavMesh.GetSettingsByIndex(0);

		settings.overrideVoxelSize = true;
		settings.voxelSize = Settings.UnityNavMesh_VoxelSize;
		settings.overrideTileSize = true;
		settings.tileSize = 1024 * 1024;

		settings.agentHeight = Settings.UnityNavMesh_MinHeight;
		settings.agentClimb = Settings.UnityNavMesh_MaxStep;
		settings.agentRadius = 0.0f;
		settings.agentSlope = Settings.UnityNavMesh_Slope;

		return settings;
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(NaviProSector))]
class NaviProSectorEditor : Editor
{
	SerializedProperty settings;

	public void OnEnable()
	{
		settings = serializedObject.FindProperty("Settings");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		NaviProSector sector = (NaviProSector)target;

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Sector Creation", new GUIStyle("BoldLabel"));
		DrawCreationSettings(settings);

		serializedObject.ApplyModifiedProperties();
	}


	static void showPropertyFieldWithMinAttribute(SerializedProperty property)
	{
		EditorGUILayout.PropertyField(property);
		if (property.propertyType == SerializedPropertyType.Float)
		{
			foreach (var attribute in typeof(NaviProSector.CreationSettings).GetField(property.name).GetCustomAttributes(false))
			{
				MinAttribute minAttribute = attribute as MinAttribute;
				if (minAttribute != null)
				{
					if (property.floatValue < minAttribute.min)
						property.floatValue = minAttribute.min;
				}
			}
		}
	}

	static public void DrawCreationSettings(SerializedProperty creationProperty)
	{
		var creationMethod = creationProperty.FindPropertyRelative("creationMethod");

		var MaximumSnappingHeight = creationProperty.FindPropertyRelative("MaximumSnappingHeight");
		var UnityNavMesh_MinHeight = creationProperty.FindPropertyRelative("UnityNavMesh_MinHeight");
		var UnityNavMesh_MaxStep = creationProperty.FindPropertyRelative("UnityNavMesh_MaxStep");
		var UnityNavMesh_Slope = creationProperty.FindPropertyRelative("UnityNavMesh_Slope");
		var UnityNavMesh_VoxelSize = creationProperty.FindPropertyRelative("UnityNavMesh_VoxelSize");


		EditorGUILayout.PropertyField(creationMethod);

		showPropertyFieldWithMinAttribute(MaximumSnappingHeight);
		if ((NaviProSector.CreationMethod)creationMethod.enumValueIndex == NaviProSector.CreationMethod.FromUnityNavMesh)
		{
			showPropertyFieldWithMinAttribute(UnityNavMesh_MinHeight);
			showPropertyFieldWithMinAttribute(UnityNavMesh_MaxStep);
			showPropertyFieldWithMinAttribute(UnityNavMesh_Slope);
			showPropertyFieldWithMinAttribute(UnityNavMesh_VoxelSize);
		}
	}
}
#endif
