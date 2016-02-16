using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

public class AnimationClipInfoWindow : EditorWindow
{
	Vector2 ScrollPos;

	[MenuItem("Window/AnimationClipInfoWindow")]
	static void OpenAnimationClipInfoWindow()
	{
		EditorWindow.GetWindow<AnimationClipInfoWindow>();
	}

	void OnGUI()
	{
		if(Selection.objects.Length == 0) return;
		if(Selection.objects.Length >= 2) return;
		var clip = Selection.objects[0] as AnimationClip;
		if(clip == null) return;

		var framePerMS = 1 / clip.frameRate;

		ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("関数名", GUILayout.MaxWidth(200));
		EditorGUILayout.LabelField("フレーム", GUILayout.MaxWidth(100));
		EditorGUILayout.LabelField("整数値", GUILayout.MaxWidth(100));
		EditorGUILayout.LabelField("数値(小数点)", GUILayout.MaxWidth(100));
		EditorGUILayout.LabelField("文字列", GUILayout.MaxWidth(200));
		EditorGUILayout.EndHorizontal();
		var eventList = AnimationUtility.GetAnimationEvents(clip);
		foreach(var eventInfo in eventList)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(string.IsNullOrEmpty(eventInfo.functionName) ? "(指定なし)" : eventInfo.functionName, GUILayout.MaxWidth(200));
			EditorGUILayout.LabelField((eventInfo.time / framePerMS).ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(eventInfo.intParameter.ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(eventInfo.floatParameter.ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(eventInfo.stringParameter.ToString(), GUILayout.MaxWidth(200));
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();
	}
	void OnSelectionChange()
	{
		Repaint();
	}
}
