using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

static public class AnimationExtensionData
{
	static public AnimationEvent EventInfoInCopy;
	static public List<AnimationEvent> EventCopyHistory = new List<AnimationEvent>();
	static public int HistoryMax = 10;

	/// <summary>
	/// イベント情報のコピー
	/// </summary>
	static public void CopyEventInfo(AnimationEvent fromEventInfo)
	{
		if(fromEventInfo == null) return;
		EventInfoInCopy = fromEventInfo;
		EventCopyHistory.Insert(0, EventInfoInCopy);
		if(EventCopyHistory.Count > HistoryMax)
		{
			EventCopyHistory.RemoveAt(EventCopyHistory.Count-1);
		}
	}
	/// <summary>
	/// コピー中のイベントを引数のイベントに適用する
	/// </summary>
	static public void PasteEventInfo(AnimationEvent toEventInfo)
	{
		if(toEventInfo == null) return;
		toEventInfo.functionName = EventInfoInCopy.functionName;
		toEventInfo.time = EventInfoInCopy.time;
		toEventInfo.intParameter = EventInfoInCopy.intParameter;
		toEventInfo.floatParameter = EventInfoInCopy.floatParameter;
		toEventInfo.stringParameter = EventInfoInCopy.stringParameter;
	}
}

public class AnimationClipInfoWindow : EditorWindow
{
	Vector2 ScrollPos;
	AnimationClip TargetClip;
	[SerializeField] AnimationEvent[] TargetEvents;
	bool IsDirty = false;

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
		if(TargetClip == null) return;
		if(TargetEvents == null) return;
		ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
		ShowAnimationClipEventList(TargetClip);
		ShowInCopyEventInfo(1/TargetClip.frameRate);
		ShowCopyHistory(1/TargetClip.frameRate);
		EditorGUILayout.EndScrollView();
	}
	/// <summary>
	/// AnimationClipに関連付いているイベント情報一覧表示
	/// </summary>
	void ShowAnimationClipEventList(AnimationClip clip)
	{
		var framePerSec = 1 / clip.frameRate;

		EditorGUILayout.LabelField("イベントリスト");
		EditorGUILayout.BeginHorizontal();
		// 新規イベント追加
		if(GUILayout.Button("+", GUILayout.Width(20)))
		{
			Undo.RecordObject(this, "add new event info");
			var eventList = TargetEvents.ToList();
			eventList.Add(new AnimationEvent());
			TargetEvents = eventList.OrderBy(event_info => event_info.time).ToArray();
			IsDirty = true;
		}
		GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
		GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
		EditorGUILayout.LabelField("関数名", GUILayout.MaxWidth(150));
		EditorGUILayout.LabelField("frame("+clip.frameRate+")", GUILayout.MaxWidth(100));
		EditorGUILayout.LabelField("整数値", GUILayout.MaxWidth(100));
		EditorGUILayout.LabelField("数値(小数点)", GUILayout.MaxWidth(100));
		EditorGUILayout.LabelField("文字列");
		EditorGUILayout.EndHorizontal();
		var deleteIdx = -1;
		for(var eventIdx = 0; eventIdx < TargetEvents.Length; ++eventIdx)
		{
			var eventInfo = TargetEvents[eventIdx];
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			GUI.color = Color.red;
			// イベント削除
			if(GUILayout.Button("―", GUILayout.Width(20)))
			{
				deleteIdx = eventIdx;
			}
			GUI.color = Color.white;
			// コピー中イベントを貼り付け
			if(GUILayout.Button("P", GUILayout.Width(20)))
			{
				if(AnimationExtensionData.EventInfoInCopy != null)
				{
					// Undoされない…
					// Undo.RecordObject(this, "paste event info");
					AnimationExtensionData.PasteEventInfo(toEventInfo: eventInfo);
					IsDirty = true;
				}
			}
			// コピー中イベントにする
			if(GUILayout.Button("C", GUILayout.Width(20)))
			{
				AnimationExtensionData.CopyEventInfo(fromEventInfo: eventInfo);
			}
			EditorGUILayout.LabelField(string.IsNullOrEmpty(eventInfo.functionName) ? "(指定なし)" : eventInfo.functionName, GUILayout.MaxWidth(150));
			var frame = eventInfo.time <= 0.0f ? 0 : (int)((eventInfo.time / framePerSec)+0.5f);
			var inputInt = EditorGUILayout.IntField(frame, GUILayout.MinWidth(100), GUILayout.MaxWidth(100));
			if(inputInt != frame)
			{
				eventInfo.time = inputInt * framePerSec;
				IsDirty = true;
			}
			inputInt = EditorGUILayout.IntField(eventInfo.intParameter, GUILayout.MinWidth(100), GUILayout.MaxWidth(100));
			if(inputInt != eventInfo.intParameter)
			{
				eventInfo.intParameter = inputInt;
				IsDirty = true;
			}
			var inputFloat = EditorGUILayout.FloatField(eventInfo.floatParameter, GUILayout.MinWidth(100), GUILayout.MaxWidth(100));
			if(inputFloat != eventInfo.floatParameter)
			{
				eventInfo.floatParameter = inputFloat;
				IsDirty = true;
			}
			var inputString = GUILayout.TextField(eventInfo.stringParameter);
			if(inputString != eventInfo.stringParameter)
			{
				eventInfo.stringParameter = inputString;
				IsDirty = true;
			}
			EditorGUILayout.EndHorizontal();
		}
		if(deleteIdx != -1)
		{
			Undo.RecordObject(this, "delete event info");
			var eventList = TargetEvents.ToList();
			eventList.RemoveAt(deleteIdx);
			TargetEvents = eventList.ToArray();
			deleteIdx = -1;
			IsDirty = true;
		}
		// 変更時のみボタンを有効に
		GUI.enabled = IsDirty;
		if(GUILayout.Button("適用", GUILayout.MaxWidth(50)))
		{
			Undo.RecordObject(clip, "apply event info");
			TargetEvents = TargetEvents.OrderBy(event_info => event_info.time).ToArray();
			AnimationUtility.SetAnimationEvents(clip, TargetEvents);
			IsDirty = false;
		}
		GUI.enabled = true;
	}
	/// <summary>
	/// コピー中イベント表示
	/// </summary>
	void ShowInCopyEventInfo(float framePerSec)
	{
		if(AnimationExtensionData.EventInfoInCopy != null)
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("コピー中イベント");
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
			GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
			GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
			EditorGUILayout.LabelField(string.IsNullOrEmpty(AnimationExtensionData.EventInfoInCopy.functionName) ? "(指定なし)" : AnimationExtensionData.EventInfoInCopy.functionName, GUILayout.MaxWidth(150));
			EditorGUILayout.LabelField(((int)((AnimationExtensionData.EventInfoInCopy.time / framePerSec)+0.5f)).ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(AnimationExtensionData.EventInfoInCopy.intParameter.ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(AnimationExtensionData.EventInfoInCopy.floatParameter.ToString(), GUILayout.MaxWidth(100));
			GUILayout.Label(AnimationExtensionData.EventInfoInCopy.stringParameter);
			EditorGUILayout.EndHorizontal();
		}
	}
	/// <summary>
	/// コピー履歴表示
	/// </summary>
	void ShowCopyHistory(float framePerSec)
	{
		if(AnimationExtensionData.EventCopyHistory.Count <= 0) return;
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("コピー履歴");
		for(var eventIdx = 0; eventIdx < AnimationExtensionData.EventCopyHistory.Count; ++eventIdx)
		{
			var eventInfo = AnimationExtensionData.EventCopyHistory[eventIdx];
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
			GUILayout.Button(string.Empty, GUI.skin.label, GUILayout.Width(20));
			// コピー中イベントにする
			if(GUILayout.Button("C", GUILayout.Width(20)))
			{
				AnimationExtensionData.CopyEventInfo(fromEventInfo: eventInfo);
			}
			EditorGUILayout.LabelField(string.IsNullOrEmpty(eventInfo.functionName) ? "(指定なし)" : eventInfo.functionName, GUILayout.MaxWidth(150));
			EditorGUILayout.LabelField(((int)((eventInfo.time / framePerSec)+0.5f)).ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(eventInfo.intParameter.ToString(), GUILayout.MaxWidth(100));
			EditorGUILayout.LabelField(eventInfo.floatParameter.ToString(), GUILayout.MaxWidth(100));
			GUILayout.Label(eventInfo.stringParameter);
			EditorGUILayout.EndHorizontal();
		}
	}
	void OnSelectionChange()
	{
		Clear();
		if(Selection.objects.Length == 0 || Selection.objects.Length >= 2) return;
		var clip = Selection.objects[0] as AnimationClip;
		if(clip == null) return;
		TargetClip = clip;
		TargetEvents = AnimationUtility.GetAnimationEvents(TargetClip);
		Repaint();
	}
	void OnUndoCallback()
	{
		Clear();
		if(Selection.objects.Length == 0 || Selection.objects.Length >= 2) return;
		var clip = Selection.objects[0] as AnimationClip;
		if(clip == null) return;
		TargetClip = clip;
		TargetEvents = AnimationUtility.GetAnimationEvents(TargetClip);
		Repaint();
	}
	/// <summary>
	/// 保持データ初期化
	/// </summary>
	void Clear()
	{
		TargetClip = null;
		TargetEvents = null;
		IsDirty = false;
	}
}
