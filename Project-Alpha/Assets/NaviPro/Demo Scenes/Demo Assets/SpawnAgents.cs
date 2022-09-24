using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpawnAgents : MonoBehaviour
{
	[System.Serializable]
	public class Area
	{
		public Rect Rect;
		public float Height;
	};
	public Area SpawnArea;
	public Area GoalArea;

	public NaviProAgent AgentPrefab;
	public int NumberOfAgents;

	public bool HideAgentsInHierarchy = false;
	public bool MoveToMatchingPointInGoalArea = false;
	public float SpawnStartTime = 0.0f;


	bool spawned = false;
	private void FixedUpdate()
	{
		if (!spawned && Time.time > SpawnStartTime)
		{
			spawn();
			spawned = true;
		}
	}

	void spawn()
	{
		for (int i = 0; i < NumberOfAgents; ++i)
		{
			Vector3 randomStart = new Vector3(Random.Range(SpawnArea.Rect.min.x, SpawnArea.Rect.max.x), SpawnArea.Height, Random.Range(SpawnArea.Rect.min.y, SpawnArea.Rect.max.y));
			Vector3 randomGoal;
			if (MoveToMatchingPointInGoalArea)
				randomGoal = Vector3.Scale(randomStart - new Vector3(SpawnArea.Rect.min.x, SpawnArea.Height, SpawnArea.Rect.min.y), new Vector3(GoalArea.Rect.width / SpawnArea.Rect.width, 1.0f, GoalArea.Rect.height / SpawnArea.Rect.height)) + new Vector3(GoalArea.Rect.min.x, GoalArea.Height, GoalArea.Rect.min.y);
			else
				randomGoal = new Vector3(Random.Range(GoalArea.Rect.min.x, GoalArea.Rect.max.x), GoalArea.Height, Random.Range(GoalArea.Rect.min.y, GoalArea.Rect.max.y));

			randomStart = transform.localToWorldMatrix.MultiplyPoint3x4(randomStart);
			randomGoal = transform.localToWorldMatrix.MultiplyPoint3x4(randomGoal);

			var agent = Instantiate(AgentPrefab, randomStart, Quaternion.identity);
			agent.transform.SetParent(transform.parent, true);
			agent.SetGoal(randomGoal);
			if (HideAgentsInHierarchy)
				agent.gameObject.hideFlags = HideFlags.HideInHierarchy;
		}
	}
}


#if UNITY_EDITOR
[CustomEditor(typeof(SpawnAgents)), CanEditMultipleObjects]
public class SpawnAgentsEditor : Editor
{
	bool modifyingRectangles = false;
	Tool lastTool;

	void beginModifyingRectangles()
	{
		lastTool = Tools.current;
		Tools.current = Tool.None;
	}
	void endModifyingRectangles()
	{
		Tools.current = lastTool;
	}


	public void OnDisable()
	{
		if (modifyingRectangles)
			endModifyingRectangles();
	}


	void modifyArea(Object obj, SpawnAgents.Area area, Color drawColor, Transform transform)
	{
		Vector3[] verts = new Vector3[]
		{
			transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(area.Rect.min.x, area.Height, area.Rect.min.y)),
			transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(area.Rect.min.x, area.Height, area.Rect.max.y)),
			transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(area.Rect.max.x, area.Height, area.Rect.max.y)),
			transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(area.Rect.max.x, area.Height, area.Rect.min.y))
		};

		EditorGUI.BeginChangeCheck();
		Vector3 newPosition = Handles.PositionHandle(transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(area.Rect.center.x, area.Height, area.Rect.center.y)), transform.rotation);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(obj, "Moved Area");
			newPosition = transform.worldToLocalMatrix.MultiplyPoint3x4(newPosition);
			area.Rect.center = new Vector2(newPosition.x, newPosition.z);
			area.Height = newPosition.y;
		}

		for (int i = 0; i < 4; ++i)
		{
			EditorGUI.BeginChangeCheck();
			Vector3 newPos = Handles.PositionHandle(verts[i], transform.rotation);
			if (EditorGUI.EndChangeCheck())
			{
				newPos = transform.worldToLocalMatrix.MultiplyPoint3x4(newPos);
				Undo.RecordObject(obj, "Resized Area");
				if (i == 0)
				{
					area.Rect.min = new Vector2(newPos.x, newPos.z);
					area.Rect.size = new Vector2(area.Rect.max.x - newPos.x, area.Rect.max.y - newPos.z);
				}
				else if (i == 1)
				{
					area.Rect.min = new Vector2(newPos.x, area.Rect.min.y);
					area.Rect.size = new Vector2(area.Rect.max.x - newPos.x, newPos.z - area.Rect.min.y);
				}
				else if (i == 2)
				{
					area.Rect.size = new Vector2(newPos.x - area.Rect.min.x, newPos.z - area.Rect.min.y);
				}
				else if (i == 3)
				{
					area.Rect.min = new Vector2(area.Rect.min.x, newPos.z);
					area.Rect.size = new Vector2(newPos.x - area.Rect.min.x, area.Rect.max.y - newPos.z);
				}
			}
		}
	}

	void drawArea(SpawnAgents.Area area, Color drawColor, Transform transform)
	{
		Vector3[] verts = new Vector3[]
		{
			transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(area.Rect.min.x, area.Height, area.Rect.min.y)),
			transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(area.Rect.min.x, area.Height, area.Rect.max.y)),
			transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(area.Rect.max.x, area.Height, area.Rect.max.y)),
			transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(area.Rect.max.x, area.Height, area.Rect.min.y))
		};

		Handles.DrawSolidRectangleWithOutline(verts, drawColor, Color.black);
	}

	public void OnSceneGUI()
	{
		SpawnAgents obj = (SpawnAgents)target;

		drawArea(obj.SpawnArea, Color.blue, obj.transform);
		drawArea(obj.GoalArea, Color.red, obj.transform);

		if (modifyingRectangles)
		{
			modifyArea(obj, obj.SpawnArea, Color.blue, obj.transform);
			modifyArea(obj, obj.GoalArea, Color.red, obj.transform);
		}
	}

	public override void OnInspectorGUI()
	{
		SpawnAgents obj = (SpawnAgents)target;

		DrawDefaultInspector();

		EditorGUI.BeginChangeCheck();
		modifyingRectangles = GUILayout.Toggle(modifyingRectangles, "Edit Areas", GUI.skin.button);
		if (EditorGUI.EndChangeCheck())
		{
			if (modifyingRectangles)
				beginModifyingRectangles();
			else
				endModifyingRectangles();
		}
	}
}
#endif