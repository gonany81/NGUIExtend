using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UIScrollViewRolling))]
public class UIScrollViewRollingInspector : Editor
{
	static bool _showEvents;
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		UIScrollViewRolling scrollView = (UIScrollViewRolling)target;
		
		EditorGUILayout.BeginHorizontal();
		scrollView._eventTarget = (GameObject)EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName("EventTarget"), scrollView._eventTarget, typeof(GameObject), true);
		
		if (scrollView._eventTarget == null)
			return;

		string[] methods = null;
		List<MethodInfo> eventInfos = new List<MethodInfo>();
		foreach (MonoBehaviour behaviour in scrollView._eventTarget.GetComponents<MonoBehaviour>())
		{
			MethodInfo[] methodInfos = behaviour.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			foreach (MethodInfo methodInfo in methodInfos)
			{
				ParameterInfo[] parameterInfos = methodInfo.GetParameters();
				if (parameterInfos.Length == 1 && parameterInfos[0].ParameterType == typeof(UIScrollViewRolling.EventArgs))
					eventInfos.Add(methodInfo);
			}
		}

		if (eventInfos.Count > 0)
		{
			methods = new string[eventInfos.Count];
			for (int j = 0; j < eventInfos.Count; ++j)
				methods[j] = eventInfos[j].Name;
		}

		if (methods == null)
		{
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.Popup(0, new string[] { "No Method" }, GUILayout.Width(Screen.width / 3));
			EditorGUI.EndDisabledGroup();
		}
		else
		{
			int method = System.Array.IndexOf(methods, scrollView._eventMethod);
			if (method < 0)
				method = 0;
			method = EditorGUILayout.Popup(method, methods, GUILayout.Width(Screen.width / 3));
			if (method >= 0)
				scrollView._eventMethod = methods[method];
		}

		EditorGUILayout.EndHorizontal();

	}
}
