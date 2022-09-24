using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[AddComponentMenu("NaviPro/NaviPro Settings")]
public class NaviProSettings : MonoBehaviour
{
	public NaviProSector.CreationSettings BaseSectorCreationSettings;

	[Tooltip("This specifies whether to run the agent simulation in FixedUpdate. If not checked, the simulation will run in Update instead.")]
	public bool RunInFixedTimeStep = true;
	[Tooltip("When running the agent simulation in FixedUpdate, this specifies whether to interpolate agent position and rotation between frames.")]
	public bool InterpolateFixedSteps = true;

	static NaviProSettings settingsInstance;

	private void Awake()
	{
		// Should we warn the user not to use multiple NaviProSettings objects?
		// Probably not here, this is run-time code
		if (settingsInstance)
			return;

		settingsInstance = this;
	}

	public static NaviProSettings Get()
	{
		// Create Settings object if not available
		if (!settingsInstance)
		{
			var settingsObject = new GameObject("NaviProSettings");
			settingsObject.hideFlags = HideFlags.HideInHierarchy;
			settingsInstance = settingsObject.AddComponent<NaviProSettings>();
			settingsInstance.BaseSectorCreationSettings = new NaviProSector.CreationSettings();
		}

		return settingsInstance;
	}
}


#if UNITY_EDITOR
[CustomEditor(typeof(NaviProSettings))]
public class NaviProSettingsEditor : Editor
{
	SerializedProperty baseSectorCreationSettings;
	SerializedProperty runInFixedTimeStep;
	SerializedProperty interpolateFixedSteps;

	public void OnEnable()
	{
		baseSectorCreationSettings = serializedObject.FindProperty("BaseSectorCreationSettings");
		runInFixedTimeStep = serializedObject.FindProperty("RunInFixedTimeStep");
		interpolateFixedSteps = serializedObject.FindProperty("InterpolateFixedSteps");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		NaviProSettings settings = (NaviProSettings)target;

		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Base Sector Settings", new GUIStyle("BoldLabel"));
		NaviProSectorEditor.DrawCreationSettings(baseSectorCreationSettings);

		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Update Settings", new GUIStyle("BoldLabel"));
		EditorGUILayout.PropertyField(runInFixedTimeStep);
		if (runInFixedTimeStep.boolValue)
			EditorGUILayout.PropertyField(interpolateFixedSteps);

		serializedObject.ApplyModifiedProperties();
	}
}
#endif