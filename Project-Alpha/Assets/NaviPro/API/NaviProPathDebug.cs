using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("NaviPro/NaviPro Path Debug")]
public class NaviProPathDebug : MonoBehaviour
{
	public Material LineMaterial;

	[Tooltip("The starting point of the path to display")]
	public Transform StartPoint;
	[Tooltip("The end point of the path to display")]
	public Transform EndPoint;
	[Tooltip("The simulated Agent Radius of the path")]
	public float AgentRadius = 0.35f;
	[Tooltip("The simulated Safe Distance of the path")]
	public float SafeDistance = 2.0f;
	[Tooltip("The height the path is drawn at, in meters")]
	public float PathDrawHeight = 0.0f;
	[Tooltip("The drawn width of the lines, in meters")]
	public float LineWidth = 0.1f;

	NaviProSector sector;

	LineRenderer lineRenderer;

	bool computed = false;
	Vector3 start;
	Vector3 end;
	float radius;
	float safeRadius;

	private void OnEnable()
	{
		var lineRendererObject = new GameObject("Line");
		lineRenderer = lineRendererObject.AddComponent<LineRenderer>();
		lineRenderer.gameObject.hideFlags = HideFlags.HideInHierarchy;
		lineRenderer.widthMultiplier = 0.5f;
		lineRenderer.enabled = false;

		sector = gameObject.GetComponentInParent<NaviProSector>();
	}

	private void OnDisable()
	{
		if (lineRenderer)
			Destroy(lineRenderer.gameObject);
		computed = false;
	}

	void Update()
	{
		if (!enabled)
			return;

		if (!StartPoint || !EndPoint)
			return;

		if (computed && start == StartPoint.position && end == EndPoint.position && radius == AgentRadius && safeRadius == SafeDistance)
			return;

		start = StartPoint.position;
		end = EndPoint.position;
		radius = AgentRadius;
		safeRadius = SafeDistance;

		Vector3 startLocal = getSector().transform.worldToLocalMatrix.MultiplyPoint3x4(start);
		Vector3 endLocal = getSector().transform.worldToLocalMatrix.MultiplyPoint3x4(end);

		var points = NaviProCore.System.GetPath(getSector().sector, startLocal, endLocal, radius, safeRadius);

		Vector3[] linePoints = new Vector3[points.Count];
		for (int i = 0; i < points.Count; ++i)
		{
			// Account for Z-up orientation given below
			linePoints[i] = new Vector3(points[i].x, points[i].y, -PathDrawHeight - 0.001f);
		}

		if (points.Count == 0)
			lineRenderer.enabled = false;
		else
		{
			lineRenderer.enabled = true;
			lineRenderer.gameObject.transform.SetParent(getSector().transform, false);
			lineRenderer.gameObject.hideFlags = HideFlags.HideInHierarchy;
			lineRenderer.positionCount = points.Count;
			lineRenderer.SetPositions(linePoints);
			lineRenderer.useWorldSpace = false;
			lineRenderer.material = LineMaterial;
			lineRenderer.widthMultiplier = 0.1f;
			// Rotate object and positions so that -Z is up
			lineRenderer.alignment = LineAlignment.TransformZ;
			lineRenderer.transform.rotation = Quaternion.FromToRotation(Vector3.forward, -Vector3.up);
		}
		computed = true;
	}

	internal NaviProSector getSector()
	{
		if (sector)
			return sector;
		else
			return NaviProCore.GetBaseSector();
	}
}


#if UNITY_EDITOR
[CustomEditor(typeof(NaviProPathDebug))]
public class NaviProPathDebugEditor : Editor
{
	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		var property = serializedObject.GetIterator();
		bool first = true;
		while (property.NextVisible(first))
		{
			if (property.displayName != "Script" && property.displayName != "LineMaterial")
				EditorGUILayout.PropertyField(property);
			first = false;
		}

		serializedObject.ApplyModifiedProperties();
	}
}
#endif
