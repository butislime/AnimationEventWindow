using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

public class AnimationEventWindow : EditorWindow
{
	AnimationClip AnimClip;
	GameObject RootObject;
	int EventIndex;

	List<MethodInfo> CallableMethods = new List<MethodInfo>();
	string FunctionSearchText;

	public void UpdateCallableMethodList()
	{
		CallableMethods.Clear();
		var scripts = RootObject.GetComponents<MonoBehaviour>();
		foreach(var script in scripts)
		{
			var methods = script.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance);
			foreach(var method in methods)
			{
				AddCallableMethod(CallableMethods, method);
			}
		}
		CallableMethods = CallableMethods.OrderBy(method => method.Name).ToList();
	}

	void OnGUI()
	{
		if(RootObject == null) return;

		// EditorGUILayout.BeginHorizontal();
		// {
			// FunctionSearchText = EditorGUILayout.TextField("検索", FunctionSearchText, (GUIStyle)"SearchTextField");
			// if(GUILayout.Button(string.Empty, (GUIStyle)"SearchCancelButton"))
			// {
				// FunctionSearchText = string.Empty;
			// }
		// }
		// EditorGUILayout.EndHorizontal();
		FunctionSearchText = EditorGUILayout.TextField("関数名検索", FunctionSearchText);
		var targetMethods = new List<MethodInfo>(CallableMethods);
		if(string.IsNullOrEmpty(FunctionSearchText) == false)
		{
			targetMethods = targetMethods.Where(method => method.Name.ToLower().Contains(FunctionSearchText.ToLower())).ToList();
		}

		var animEvents = AnimationUtility.GetAnimationEvents(AnimClip);
		var animEvent = animEvents[EventIndex];
		var funcIdx = 0;
		if(string.IsNullOrEmpty(animEvent.functionName) == false) funcIdx = targetMethods.FindIndex(method => method.Name == animEvent.functionName);
		var selectFuncIdx = EditorGUILayout.Popup(funcIdx, targetMethods.Select(method => FormatCallableMethodName(method)).ToArray());
		// 関数が変わったら、パラメータ初期化
		if(funcIdx != selectFuncIdx)
		{
			animEvent.intParameter = 0;
			animEvent.floatParameter = 0.0f;
			animEvent.stringParameter = string.Empty;
			animEvent.objectReferenceParameter = null;
			funcIdx = selectFuncIdx;
		}
		if(funcIdx >= 0 && funcIdx < targetMethods.Count)
		{
			animEvent.functionName = targetMethods[funcIdx].Name;
			ShowParameter(animEvent, targetMethods[funcIdx]);
		}

		AnimationUtility.SetAnimationEvents(AnimClip, animEvents);
	}

	void ShowParameter(AnimationEvent animEvent, MethodInfo methodInfo)
	{
		if(methodInfo.GetParameters().Length == 0) return;
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Parameters");
		var parameterType = methodInfo.GetParameters()[0].ParameterType;
		switch(parameterType.Name)
		{
			case "Int32":
				animEvent.intParameter = EditorGUILayout.IntField("整数", animEvent.intParameter);
				break;
			case "Single":
				animEvent.floatParameter = EditorGUILayout.FloatField("数値(小数点)", animEvent.floatParameter);
				break;
			case "String":
				animEvent.stringParameter = EditorGUILayout.TextField("文字列", animEvent.stringParameter);
				break;
			case "Object":
				animEvent.objectReferenceParameter = EditorGUILayout.ObjectField("Object", animEvent.objectReferenceParameter, typeof(UnityEngine.Object), true);
				break;
			default:
				var selectIdx = 0;
				var enumValues = Enum.GetValues(parameterType);
				for(int enumIdx = 0; enumIdx < enumValues.Length; ++enumIdx)
				{
					if((int)enumValues.GetValue(enumIdx) == animEvent.intParameter) selectIdx = enumIdx;
				}
				selectIdx = EditorGUILayout.Popup("Enum", selectIdx, Enum.GetNames(parameterType));
				animEvent.intParameter = (int)enumValues.GetValue(selectIdx);
				// var converter = new EnumConverter(MethodInfo.GetParameters()[0].ParameterType);
				// animEvent.intParameter = EditorGUILayout.EnumPopup("Enum", converter.ConvertFrom(animEvent.intParameter));
				break;
		}
	}

	void AddCallableMethod(List<MethodInfo> callableMethods, MethodInfo methodInfo)
	{
		if(methodInfo == null) return;
		if(methodInfo.IsSpecialName) return;
		var monoMethods = typeof(MonoBehaviour).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance);
		if(monoMethods.Any(method => method.Name == methodInfo.Name)) return;
		var ignoreFuncs = new string[] {
			"Awake",
			"Start",
			"Update",
		};
		if(ignoreFuncs.Any(func_name => func_name == methodInfo.Name)) return;
		var parameters = methodInfo.GetParameters();
		if(parameters.Length > 1) return;
		if(parameters.Length == 1)
		{
			if(parameters[0].ParameterType.IsEnum == false)
			{
				var availableParamNames = new string[] {
					"Single",
					"Int32",
					"String",
					"Object",
				};
				if(availableParamNames.Any(name => name == parameters[0].ParameterType.Name) == false) return;
			}
		}
		if(callableMethods.Any(method => method.Name == methodInfo.Name))
		{
			callableMethods.RemoveAll(method => method.Name == methodInfo.Name);
			return;
		}

		callableMethods.Add(methodInfo);
	}

	string FormatCallableMethodName(MethodInfo methodInfo)
	{
		if(methodInfo.GetParameters().Length == 0) return methodInfo.Name + "( )";
		return methodInfo.Name + "( " + ConvertTypeName(methodInfo.GetParameters()[0].ParameterType.ToString()) + " )";
	}

	string ConvertTypeName(string before_type_name)
	{
		var conv_table = new Dictionary<string, string>() {
			{"System.Single", "float"},
			{"System.Int32", "int"},
			{"System.String", "string"},
			{"UnityEngine.Object", "Object"},
		};
		string after_type_name;
		if(conv_table.TryGetValue(before_type_name, out after_type_name)) return after_type_name;
		return before_type_name;
	}
	
	[InitializeOnLoadMethod]
	static void Startup()
	{
		EditorApplication.update += Update;
	}
	static void Update()
	{
		var windows = Resources.FindObjectsOfTypeAll(typeof(EditorWindow)) as EditorWindow[];
		foreach(var w in windows)
		{
			var winType = w.GetType();
			if(winType.ToString().Contains("AnimationEventPopup") == false) continue;

			var animClipField = winType.GetField("m_Clip", BindingFlags.NonPublic | BindingFlags.Instance);
			var rootObjField = winType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
			var eventIdxField = winType.GetField("m_EventIndex", BindingFlags.NonPublic | BindingFlags.Instance);

			w.Close();

			var animEventWin = EditorWindow.GetWindow(typeof(AnimationEventWindow)) as AnimationEventWindow;
			animEventWin.AnimClip = animClipField.GetValue(w) as AnimationClip;
			animEventWin.RootObject = rootObjField.GetValue(w) as GameObject;
			animEventWin.EventIndex = (int)eventIdxField.GetValue(w);

			animEventWin.UpdateCallableMethodList();

			animEventWin.Show();
		}
	}
}
