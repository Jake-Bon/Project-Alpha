using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif


[AddComponentMenu("NaviPro/NaviPro Debug")]
public class NaviProDebug : MonoBehaviour
{
	public Material LineMaterial;

	[Tooltip("Draw the outer boundary of the Walkable Area")]
	public bool DrawBoundary = true;
	[Tooltip("Draw the Medial Axis, the center of each corridor spanning the Walkable Area")]
	public bool DrawMedialAxis = false;
	[Tooltip("The drawn width of the lines, in meters")]
	public float LineWidth = 0.1f;


	bool requestedRedraw = true;
	bool drawnBoundary = false;
	bool drawnMedialAxis = false;

	[SerializeField]
	internal List<GameObject> debugObjects = new List<GameObject>();
	[SerializeField]
	internal bool created = false;
	[SerializeField]
	internal bool createdFromInspector = false;


	public void Redraw()
	{
		requestedRedraw = true;
	}

	private void Update()
	{
		if (!enabled)
			return;
		if (requestedRedraw || (DrawBoundary != drawnBoundary || DrawMedialAxis != drawnMedialAxis))
			redraw();
		requestedRedraw = false;
	}

	private void OnDisable()
	{
		destroy();
		requestedRedraw = true;
	}

	void redraw()
	{
		destroy();

		foreach (var sector in FindObjectsOfType<NaviProSector>().Where(z => z.sector != null))
		{
			var sectorDebugObject = drawSector(sector.sector, sector.transform);
			debugObjects.Add(sectorDebugObject);
		}
		if (NaviProCore.HasBaseSector())
		{
			var sectorDebugObject = drawSector(NaviProCore.GetBaseSector().sector, null);
			debugObjects.Add(sectorDebugObject);
		}

		created = true;
		createdFromInspector = false;

		drawnBoundary = DrawBoundary;
		drawnMedialAxis = DrawMedialAxis;
	}

	void destroy()
	{
		foreach (GameObject debugObject in debugObjects)
		{
			if (debugObject)
				Destroy(debugObject);
		}
		debugObjects.Clear();
		created = false;
	}

	internal GameObject drawSector(NaviProNative.Sector sector, Transform sectorTransform)
	{
		GameObject sectorDebugObject = new GameObject("Debug");

		if (sectorTransform)
			sectorDebugObject.transform.SetParent(sectorTransform, false);
		sectorDebugObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;

		if (DrawMedialAxis)
		{
			List<Vector3> medialAxis = NaviProCore.System.GetECMMedialAxis(sector);
			for (int i = 0; i < medialAxis.Count / 2; ++i)
			{
				GameObject lineSegment = new GameObject("Medial Axis Segment");
				lineSegment.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
				lineSegment.transform.SetParent(sectorDebugObject.transform, false);

				LineRenderer lineRenderer = lineSegment.AddComponent<LineRenderer>();
				lineRenderer.widthMultiplier = LineWidth;
				lineRenderer.useWorldSpace = false;
				lineRenderer.material = LineMaterial;
				// Rotate object and positions so that -Z is up
				lineRenderer.alignment = LineAlignment.TransformZ;
				lineRenderer.SetPositions(new Vector3[] { new Vector3(medialAxis[i * 2 + 1].x, medialAxis[i * 2 + 1].z, -medialAxis[i * 2 + 1].y - 0.001f), new Vector3(medialAxis[i * 2].x, medialAxis[i * 2].z, -medialAxis[i * 2].y - 0.001f) });
				lineRenderer.transform.rotation = Quaternion.FromToRotation(Vector3.forward, -Vector3.up);
			}
		}

		if (DrawBoundary)
		{
			List<Vector3> boundary = NaviProCore.System.GetBoundary(sector);
			for (int i = 0; i < boundary.Count / 2; ++i)
			{
				GameObject lineSegment = new GameObject("Boundary Segment");
				lineSegment.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
				lineSegment.transform.SetParent(sectorDebugObject.transform, false);

				LineRenderer lineRenderer = lineSegment.AddComponent<LineRenderer>();
				lineRenderer.widthMultiplier = LineWidth;
				lineRenderer.useWorldSpace = false;
				lineRenderer.material = LineMaterial;
				// Rotate object and positions so that -Z is up
				lineRenderer.alignment = LineAlignment.TransformZ;
				lineRenderer.SetPositions(new Vector3[] { new Vector3(boundary[i * 2 + 1].x, boundary[i * 2 + 1].z, -boundary[i * 2 + 1].y - 0.001f), new Vector3(boundary[i * 2].x, boundary[i * 2].z, -boundary[i * 2].y - 0.001f) });
				lineRenderer.transform.rotation = Quaternion.FromToRotation(Vector3.forward, -Vector3.up);
			}
		}

		return sectorDebugObject;
	}
}


#if UNITY_EDITOR
[CustomEditor(typeof(NaviProDebug))]
public class NaviProDebugEditor : Editor
{
	NaviProDebug debug;
	SerializedProperty drawBoundary;
	SerializedProperty drawMedialAxis;
	SerializedProperty lineWidth;

	public void OnEnable()
	{
		debug = (NaviProDebug)target;
		drawBoundary = serializedObject.FindProperty("DrawBoundary");
		drawMedialAxis = serializedObject.FindProperty("DrawMedialAxis");
		lineWidth = serializedObject.FindProperty("LineWidth");

		if (EditorApplication.isPlaying && debug.created && debug.createdFromInspector)
			destroy(true);
	}

	public void OnDisable()
	{
		destroy();
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(drawBoundary);
		EditorGUILayout.PropertyField(drawMedialAxis);
		EditorGUILayout.PropertyField(lineWidth);
		bool changes = GUI.changed;

		bool enableShowViewButton = !Application.isPlaying;
		if (!enableShowViewButton)
			GUI.enabled = false;
		if (GUILayout.Button((enableShowViewButton && debug.created) ? "Refresh Debug View" : "Show Debug View"))
			create();
		if (!enableShowViewButton)
			GUI.enabled = true;

		bool enableHideViewButton = (debug.created) && !Application.isPlaying;
		if (!enableHideViewButton)
			GUI.enabled = false;
		if (GUILayout.Button("Hide Debug View"))
			destroy();
		if (!enableHideViewButton)
			GUI.enabled = true;

		if (changes)
		{
			serializedObject.ApplyModifiedProperties();
			if (debug.created)
				create();
		}
	}

	void create()
	{
		if (EditorApplication.isPlaying)
			return;
		if (debug.created)
			destroy();

		debug.debugObjects = new List<GameObject>();

		foreach (var sector in FindObjectsOfType<NaviProSector>())
		{
			if (sector.create())
			{
				var sectorDebugObject = debug.drawSector(sector.sector, sector.transform);
				debug.debugObjects.Add(sectorDebugObject);
				sector.destroy(true);
			}
		}

		NaviProSector.CreationSettings baseSectorSettings;
		var settings = FindObjectOfType<NaviProSettings>();
		if (settings)
			baseSectorSettings = settings.BaseSectorCreationSettings;
		else
			baseSectorSettings = new NaviProSector.CreationSettings();

		if (NaviProSector.canCreateBaseSector(baseSectorSettings))
		{
			var baseSectorObject = new GameObject("Base Sector");
			var baseSector = baseSectorObject.AddComponent<NaviProSector>();
			baseSector.Settings = baseSectorSettings;
			if (baseSector.create(true))
			{
				var sectorDebugObject = debug.drawSector(baseSector.sector, null);
				debug.debugObjects.Add(sectorDebugObject);
				baseSector.destroy(true);
			}
			DestroyImmediate(baseSectorObject);
		}
		debug.created = true;
		debug.createdFromInspector = true;
	}

	void destroy(bool force = false)
	{
		if (!force)
		{
			if (EditorApplication.isPlaying)
				return;
			if (!debug.created)
				return;
		}

		foreach (GameObject debugObject in debug.debugObjects)
		{
			if (debugObject)
			{
				if (EditorApplication.isPlaying)
					Destroy(debugObject);
				else
					DestroyImmediate(debugObject);
			}
		}
		debug.debugObjects.Clear();
		debug.created = false;
	}
}
#endif