using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


[AddComponentMenu("NaviPro/NaviPro Platform")]
[DefaultExecutionOrder(-3)]
public class NaviProPlatform : MonoBehaviour
{
	[Tooltip("A polygon describing the area of the Walkable Surface to be affected")]
	public Vector2[] Points = new Vector2[4] { new Vector2(-2.0f, -2.0f), new Vector2(-2.0f, 2.0f), new Vector2(2.0f, 2.0f), new Vector2(2.0f, -2.0f) };

	[Tooltip("The height offset above and below the given polygon where the Platform affects the Walkable Surface.\nThis should be set large enough to encompass the surface to be affected, but not so large it will hit other surfaces.")]
	[Min(0.0f)]
	public float Height = 2.0f;

	
	internal NaviProSector sector;
	internal NaviProNative.Platform platform;

	internal LinkedListNode<NaviProPlatform> coreref = null;
	bool created = false;
	bool disabledDuringCreate = false;


	void OnEnable()
	{
		NaviProCore.Instance.insertPlatforms.Add(this);
	}
	void OnDisable()
	{
		if (disabledDuringCreate)
		{
			disabledDuringCreate = false;
			return;
		}

		if (NaviProCore.IsDestroyed())
			return;
		if (created)
			NaviProCore.Instance.removePlatforms.Add(this);
		else
			NaviProCore.Instance.insertPlatforms.Remove(this);
	}


	public void Apply()
	{
		NaviProCore.Instance.insertPlatforms.Add(this);
		NaviProCore.Instance.removePlatforms.Add(this);
	}


	internal void mark_destroyed()
	{
		created = false;
		platform = null;
		sector = null;
		enabled = false;
	}

	internal bool create()
	{
		if (created)
			return false;
		if (!gameObject.GetComponentInParent<NaviProSector>() && !NaviProCore.HasBaseSector())
		{
			disabledDuringCreate = true;
			enabled = false;
			return false;
		}

		sector = gameObject.GetComponentInParent<NaviProSector>();
		platform = NaviProCore.System.CreatePlatform(getSector().sector, getTransformedPoints(), getPositionInSector(), getLocalUpVectorInSector(), Mathf.Abs(Height));
		if (platform == null)
			return false;

		getSector().platforms.Add(this);

		if (FindObjectOfType<NaviProDebug>())
			FindObjectOfType<NaviProDebug>().Redraw();

		created = true;
		return true;
	}

	internal void destroy()
	{
		if (!created)
			return;

		NaviProCore.System.RemovePlatform(getSector().sector, platform);
		getSector().platforms.Remove(this);
		platform = null;
		sector = null;

		if (FindObjectOfType<NaviProDebug>())
			FindObjectOfType<NaviProDebug>().Redraw();

		created = false;
	}

	internal void checkSector()
	{
		var newSector = gameObject.GetComponentInParent<NaviProSector>();
		if (newSector != sector)
		{
			destroy();
			sector = newSector;
			create();
		}
	}

	internal NaviProSector getSector()
	{
		if (sector)
			return sector;
		else
			return NaviProCore.GetBaseSector();
	}

	Vector3[] getTransformedPoints()
	{
		Matrix4x4 localToSector = getSector().transform.worldToLocalMatrix * transform.localToWorldMatrix;
		Vector3[] points = new Vector3[Points.Length];
		for (int i = 0; i < Points.Length; ++i)
			points[i] = localToSector.MultiplyPoint3x4(new Vector3(Points[i].x, 0.0f, Points[i].y));
		return points;
	}

	Vector3 getPositionInSector()
	{
		return getSector().transform.worldToLocalMatrix.MultiplyPoint3x4(transform.position);
	}

	Vector3 getLocalUpVectorInSector()
	{
		return getSector().transform.worldToLocalMatrix.MultiplyVector(transform.up);
	}
}


#if UNITY_EDITOR
[CustomEditor(typeof(NaviProPlatform))]
public class NaviProPlatformEditor : Editor
{
	SerializedProperty points;
	SerializedProperty height;

	bool editPoints;
	Tool previousTool;

	public void OnEnable()
	{
		points = serializedObject.FindProperty("Points");
		height = serializedObject.FindProperty("Height");
		editPoints = false;
	}

	public void OnDisable()
	{
		if (editPoints)
		{
			Tools.current = previousTool;
			editPoints = false;
		}
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		NaviProPlatform obj = (NaviProPlatform)target;

		int insertPoint = -1;
		int removePoint = -1;
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Points", new GUIStyle("BoldLabel"));

		if (polygonHasSelfIntersection(obj.Points))
		{
			GUIStyle labelStyle = new GUIStyle(GUI.skin.box);
			labelStyle.normal.textColor = Color.red;
			labelStyle.stretchWidth = true;
			GUILayout.Label("The given polygon has a self intersection. This is not allowed.", labelStyle);
			EditorGUILayout.Space();
		}

		for (int i = 0; i < points.arraySize; ++i)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(points.GetArrayElementAtIndex(i), new GUIContent(""));
			if (GUILayout.Button("Remove"))
				removePoint = i;
			EditorGUILayout.EndHorizontal();
			if (GUILayout.Button("Insert Point"))
				insertPoint = i + 1;
		}
		if (insertPoint > 0)
		{
			points.InsertArrayElementAtIndex(insertPoint);
			points.GetArrayElementAtIndex(insertPoint).vector2Value = (points.GetArrayElementAtIndex(insertPoint - 1).vector2Value + points.GetArrayElementAtIndex((insertPoint + 1) % points.arraySize).vector2Value) * 0.5f;
		}
		else if (removePoint > 0)
		{
			points.DeleteArrayElementAtIndex(removePoint);
		}

		EditorGUILayout.Space();

		bool newEditPoints = GUILayout.Toggle(editPoints, "Edit Points in Scene View", new GUIStyle("Button"));
		if (newEditPoints != editPoints)
		{
			editPoints = newEditPoints;
			if (editPoints)
			{
				previousTool = Tools.current;
				Tools.current = Tool.None;
			}
			else
			{
				Tools.current = previousTool;
			}
		}

		EditorGUILayout.Space();

		EditorGUILayout.PropertyField(height);
		if (height.floatValue < 0.0f)
			height.floatValue = 0.0f;

		serializedObject.ApplyModifiedProperties();
	}

	protected void OnSceneGUI()
	{
		NaviProPlatform obj = (NaviProPlatform)target;

		if (editPoints)
		{
			for (int i = 0; i < obj.Points.Length; ++i)
			{
				EditorGUI.BeginChangeCheck();
				Vector3 position = obj.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(obj.Points[i].x, 0.0f, obj.Points[i].y));
				Vector3 newPosition = obj.transform.worldToLocalMatrix.MultiplyPoint3x4(Handles.PositionHandle(position, obj.transform.rotation));
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(obj, "Move Obstacle Point");
					obj.Points[i] = new Vector2(newPosition.x, newPosition.z);
				}
			}
		}

		Vector3[] polygonPoints = new Vector3[obj.Points.Length];
		Vector3[] polygonLinePoints = new Vector3[obj.Points.Length + 1];
		Vector3[] polygonLinePointsUpper = new Vector3[obj.Points.Length + 1];
		Vector3[] polygonLinePointsLower = new Vector3[obj.Points.Length + 1];
		for (int i = 0; i < obj.Points.Length; ++i)
		{
			polygonPoints[i] = obj.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(obj.Points[i].x, 0.0f, obj.Points[i].y));
			polygonLinePoints[i] = polygonPoints[i];
			polygonLinePointsUpper[i] = polygonPoints[i] + Vector3.up * obj.Height;
			polygonLinePointsLower[i] = polygonPoints[i] - Vector3.up * obj.Height;
		}
		polygonLinePoints[obj.Points.Length] = polygonLinePoints[0];
		polygonLinePointsUpper[obj.Points.Length] = polygonLinePointsUpper[0];
		polygonLinePointsLower[obj.Points.Length] = polygonLinePointsLower[0];

		float lineWidth = EditorGUIUtility.pixelsPerPoint * 4.0f;
		float lineWidthThin = EditorGUIUtility.pixelsPerPoint * 2.0f;
		Color baseColor = Color.green;
		Color offColor = Color.Lerp(Color.white, baseColor, 0.5f);

		Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

		Handles.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);
		drawAAPolygon(polygonPoints, obj.Points);
		Handles.color = baseColor;
		Handles.DrawAAPolyLine(lineWidth, polygonLinePoints);

		Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

		Handles.color = offColor;
		Handles.DrawAAPolyLine(lineWidthThin, polygonLinePointsUpper);
		Handles.DrawAAPolyLine(lineWidthThin, polygonLinePointsLower);
	}



	bool linesIntersect(Vector2 line1Point1, Vector2 line1Point2, Vector2 line2Point1, Vector2 line2Point2)
	{
		Vector2 line1Dir = line1Point2 - line1Point1;
		Vector2 line2Dir = line2Point2 - line2Point1;

		float dirCross = line1Dir.x * line2Dir.y - line1Dir.y * line2Dir.x;
		if (dirCross == 0.0f)
			return false;

		Vector2 line1To2 = (line2Point1 - line1Point1);
		float t1 = (line1To2.x * line2Dir.y - line1To2.y * line2Dir.x) / dirCross;
		float t2 = (line1To2.x * line1Dir.y - line1To2.y * line1Dir.x) / dirCross;

		return (t1 >= 0.0f && t1 <= 1.0f && t2 >= 0.0f && t2 <= 1.0f);
	}

	bool polygonHasSelfIntersection(Vector2[] points)
	{
		for (int i = 0; i < points.Length; ++i)
		{
			for (int j = 0; j < points.Length; ++j)
			{
				// Ignore adjacent edges
				if (j == i || j == ((i + 1) % points.Length) || j == ((i - 1 + points.Length) % points.Length))
					continue;

				if (linesIntersect(points[i], points[(i + 1) % points.Length], points[j], points[(j + 1) % points.Length]))
					return true;
			}
		}

		return false;
	}

	bool pointIsInsideTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p, bool ccw)
	{
		// sign for p1 to p2
		float sign1 = (p2.x - p1.x) * (p.y - p1.y) - (p2.y - p1.y) * (p.x - p1.x);
		// sign for p2 to p3
		float sign2 = (p3.x - p2.x) * (p.y - p2.y) - (p3.y - p2.y) * (p.x - p2.x);
		// sign for p3 to p1
		float sign3 = (p1.x - p3.x) * (p.y - p3.y) - (p1.y - p3.y) * (p.x - p3.x);
		if (ccw)
		{
			return (sign1 > 0.0f && sign2 > 0.0f && sign3 > 0.0f);
		}
		else
		{
			return (sign1 < 0.0f && sign2 < 0.0f && sign3 < 0.0f);
		}
	}

	void drawAAPolygon(Vector3[] points, Vector2[] points2D)
	{
		// Ear clipping algorithm
		List<Vector3> remainingPoints = new List<Vector3>(points);
		List<Vector2> remainingPoints2D = new List<Vector2>(points2D);

		// First get the winding order of the 2D polygon
		float area = 0.0f;
		for (int i = 0; i < remainingPoints2D.Count; ++i)
		{
			Vector2 p1 = remainingPoints2D[i];
			Vector2 p2 = remainingPoints2D[(i + 1) % remainingPoints2D.Count];
			area += p1.x * p2.y - p1.y * p2.x;
		}
		bool isCCW = (area > 0.0f);

		while (remainingPoints.Count > 0)
		{
			int earIndex = -1;

			if (remainingPoints.Count > 3)
			{
				// Find a point for a candidate ear
				for (int i = 0; i < remainingPoints2D.Count; ++i)
				{
					Vector2 p1 = remainingPoints2D[i];
					Vector2 p0 = remainingPoints2D[(i - 1 + remainingPoints2D.Count) % remainingPoints2D.Count];
					Vector2 p2 = remainingPoints2D[(i + 1) % remainingPoints2D.Count];

					// Check if the angle at the ear is convex
					float earArea = (p0 - p1).x * (p2 - p1).y - (p0 - p1).y * (p2 - p1).x;
					if ((isCCW && earArea > 0.0f) || (!isCCW && earArea < 0.0f))
						continue;

					// Check if the ear is completely contained in the shape
					// See if any point lies inside the ear, causing an intersection
					bool hasIntersection = false;
					for (int j = 0; j < remainingPoints2D.Count; ++j)
					{
						// Ignore the adjacent points
						if (j == i || j == ((i + 1) % remainingPoints2D.Count) || j == ((i - 1 + remainingPoints2D.Count) % remainingPoints2D.Count))
							continue;

						// Check against the edge
						if (pointIsInsideTriangle(p0, p1, p2, remainingPoints2D[j], isCCW))
						{
							hasIntersection = true;
							break;
						}
					}
					if (hasIntersection)
						continue;

					earIndex = i;
					break;
				}
			}
			else
			{
				earIndex = 1;
			}

			if (earIndex < 0)
				break;

			Vector3[] triangle = new Vector3[3];
			triangle[0] = remainingPoints[earIndex];
			triangle[1] = remainingPoints[(earIndex - 1 + remainingPoints.Count) % remainingPoints.Count];
			triangle[2] = remainingPoints[(earIndex + 1) % remainingPoints.Count];
			Handles.DrawAAConvexPolygon(triangle);

			if (remainingPoints.Count > 3)
			{
				remainingPoints.RemoveAt(earIndex);
				remainingPoints2D.RemoveAt(earIndex);
			}
			else
			{
				break;
			}
		}
	}
}
#endif