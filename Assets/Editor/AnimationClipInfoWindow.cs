using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

public class AnimationClipInfoWindow : EditorWindow
{
	Vector2 ScrollPos;
	AnimationEvent EventInfoInCopy;

	[MenuItem("Window/AnimationClip Window")]
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

		var framePerSec = 1 / clip.frameRate;

		ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
		EditorGUILayout.LabelField("イベントリスト");
		EditorGUILayout.BeginHorizontal();
		GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
		GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
		EditorGUILayout.LabelField("関数名", GUILayout.MaxWidth(200));
		EditorGUILayout.LabelField("frame("+clip.frameRate+")", GUILayout.MaxWidth(100));
		EditorGUILayout.LabelField("整数値", GUILayout.MaxWidth(100));
		EditorGUILayout.LabelField("数値(小数点)", GUILayout.MaxWidth(100));
		EditorGUILayout.LabelField("文字列");
		EditorGUILayout.EndHorizontal();
		var eventList = AnimationUtility.GetAnimationEvents(clip);
		for(var eventIdx = 0; eventIdx < eventList.Length; ++eventIdx)
		{
			var eventInfo = eventList[eventIdx];
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			if(GUILayout.Button("P", GUILayout.Width(20)))
			{
				if(EventInfoInCopy != null)
				{
					eventInfo.functionName = EventInfoInCopy.functionName;
					eventInfo.intParameter = EventInfoInCopy.intParameter;
					eventInfo.floatParameter = EventInfoInCopy.floatParameter;
					eventInfo.stringParameter = EventInfoInCopy.stringParameter;
					AnimationUtility.SetAnimationEvents(clip, eventList);
				}
			}
			if(GUILayout.Button("C", GUILayout.Width(20)))
			{
				EventInfoInCopy = eventInfo;
			}
			EditorGUILayout.LabelField(string.IsNullOrEmpty(eventInfo.functionName) ? "(指定なし)" : eventInfo.functionName, GUILayout.MaxWidth(200));
			EditorGUILayout.LabelField((eventInfo.time / framePerSec).ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(eventInfo.intParameter.ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(eventInfo.floatParameter.ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(eventInfo.stringParameter.ToString());
			EditorGUILayout.EndHorizontal();
		}
		if(EventInfoInCopy != null)
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("コピー中イベント(関数名、整数値、数値(小数点)、文字列置き換え)");
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
			GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
			EditorGUILayout.LabelField(string.IsNullOrEmpty(EventInfoInCopy.functionName) ? "(指定なし)" : EventInfoInCopy.functionName, GUILayout.MaxWidth(200));
			EditorGUILayout.LabelField((EventInfoInCopy.time / framePerSec).ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(EventInfoInCopy.intParameter.ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(EventInfoInCopy.floatParameter.ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(EventInfoInCopy.stringParameter.ToString());
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();
	}
	void OnSelectionChange()
	{
		Repaint();
	}
}
