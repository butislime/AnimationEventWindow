using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

static public class AnimationExtensionData
{
	static public AnimationEvent EventInfoInCopy;

	/// <summary>
	/// イベント情報のコピー
	/// </summary>
	static public void CopyEventInfo(AnimationEvent fromEventInfo)
	{
		if(fromEventInfo == null) return;
		EventInfoInCopy = fromEventInfo;
	}
	/// <summary>
	/// コピー中のイベントを引数のイベントに適用する
	/// functionname,int,float,stringのみ
	/// </summary>
	static public void PasteEventInfo(AnimationEvent toEventInfo)
	{
		if(toEventInfo == null) return;
		toEventInfo.functionName = EventInfoInCopy.functionName;
		toEventInfo.intParameter = EventInfoInCopy.intParameter;
		toEventInfo.floatParameter = EventInfoInCopy.floatParameter;
		toEventInfo.stringParameter = EventInfoInCopy.stringParameter;
	}
}

public class AnimationClipInfoWindow : EditorWindow
{
	Vector2 ScrollPos;

	[MenuItem("Window/AnimationClip Window")]
	static void OpenAnimationClipInfoWindow()
	{
		EditorWindow.GetWindow<AnimationClipInfoWindow>();
	}

	AnimationClipInfoWindow()
	{
		Undo.undoRedoPerformed += OnUndoCallback;
	}

	void OnGUI()
	{
		if(Selection.objects.Length == 0) return;
		if(Selection.objects.Length >= 2) return;
		var clip = Selection.objects[0] as AnimationClip;
		if(clip == null) return;

		var framePerSec = 1 / clip.frameRate;
		var events = AnimationUtility.GetAnimationEvents(clip);

		ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
		EditorGUILayout.LabelField("イベントリスト");
		EditorGUILayout.BeginHorizontal();
		// 新規イベント追加
		if(GUILayout.Button("+", GUILayout.Width(20)))
		{
			Undo.RecordObject(clip, "add new event info");
			var eventList = events.ToList();
			eventList.Add(new AnimationEvent());
			events = eventList.OrderBy(event_info => event_info.time).ToArray();
			AnimationUtility.SetAnimationEvents(clip, events);
		}
		GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
		EditorGUILayout.LabelField("関数名", GUILayout.MaxWidth(200));
		EditorGUILayout.LabelField("frame("+clip.frameRate+")", GUILayout.MaxWidth(100));
		EditorGUILayout.LabelField("整数値", GUILayout.MaxWidth(100));
		EditorGUILayout.LabelField("数値(小数点)", GUILayout.MaxWidth(100));
		EditorGUILayout.LabelField("文字列");
		EditorGUILayout.EndHorizontal();
		for(var eventIdx = 0; eventIdx < events.Length; ++eventIdx)
		{
			var eventInfo = events[eventIdx];
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			if(GUILayout.Button("P", GUILayout.Width(20)))
			{
				if(AnimationExtensionData.EventInfoInCopy != null)
				{
					Undo.RecordObject(clip, "paste event info");
					AnimationExtensionData.PasteEventInfo(toEventInfo: eventInfo);
					AnimationUtility.SetAnimationEvents(clip, events);
				}
			}
			if(GUILayout.Button("C", GUILayout.Width(20)))
			{
				AnimationExtensionData.CopyEventInfo(fromEventInfo: eventInfo);
				AnimationExtensionData.EventInfoInCopy = eventInfo;
			}
			EditorGUILayout.LabelField(string.IsNullOrEmpty(eventInfo.functionName) ? "(指定なし)" : eventInfo.functionName, GUILayout.MaxWidth(200));
			EditorGUILayout.LabelField((eventInfo.time / framePerSec).ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(eventInfo.intParameter.ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(eventInfo.floatParameter.ToString(), GUILayout.MaxWidth(100));
			GUILayout.Label(eventInfo.stringParameter.ToString());
			EditorGUILayout.EndHorizontal();
		}
		if(AnimationExtensionData.EventInfoInCopy != null)
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("コピー中イベント(関数名、整数値、数値(小数点)、文字列置き換え)");
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
			GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
			EditorGUILayout.LabelField(string.IsNullOrEmpty(AnimationExtensionData.EventInfoInCopy.functionName) ? "(指定なし)" : AnimationExtensionData.EventInfoInCopy.functionName, GUILayout.MaxWidth(200));
			EditorGUILayout.LabelField((AnimationExtensionData.EventInfoInCopy.time / framePerSec).ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(AnimationExtensionData.EventInfoInCopy.intParameter.ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(AnimationExtensionData.EventInfoInCopy.floatParameter.ToString(), GUILayout.MaxWidth(100));
			GUILayout.Label(AnimationExtensionData.EventInfoInCopy.stringParameter.ToString());
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();
	}
	void OnSelectionChange()
	{
		Repaint();
	}
	void OnUndoCallback()
	{
		Repaint();
	}
}
