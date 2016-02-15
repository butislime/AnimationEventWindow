using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

public class AnimationEventWindow : EditorWindow
{
	AnimationClip animClip;
	GameObject rootObject;
	int eventIndex;

	void OnGUI()
	{
		var callable_methods = new List<MethodInfo>();
		var scripts = rootObject.GetComponents<MonoBehaviour>();
		foreach(var script in scripts)
		{
			var methods = script.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance);
			foreach(var method in methods)
			{
				AddCallableMethod(callable_methods, method);
			}
		}

		var anim_events = AnimationUtility.GetAnimationEvents(animClip);
		var anim_event = anim_events[eventIndex];
		var func_idx = 0;
		if(string.IsNullOrEmpty(anim_event.functionName) == false) func_idx = callable_methods.FindIndex(method => method.Name == anim_event.functionName);
		func_idx = EditorGUILayout.Popup(func_idx, callable_methods.Select(method => FormatCallableMethodName(method)).ToArray());
		anim_event.functionName = callable_methods[func_idx].Name;

		ShowParameter(anim_event, callable_methods[func_idx]);

		AnimationUtility.SetAnimationEvents(animClip, anim_events);
	}

	void ShowParameter(AnimationEvent anim_event, MethodInfo method_info)
	{
		if(method_info.GetParameters().Length == 0) return;
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Parameters");
		switch(method_info.GetParameters()[0].ParameterType.ToString())
		{
			case "System.Int32":
				anim_event.intParameter = EditorGUILayout.IntField("Int", anim_event.intParameter);
				break;
			case "System.Single":
				anim_event.floatParameter = EditorGUILayout.FloatField("Float", anim_event.floatParameter);
				break;
			case "System.String":
				anim_event.stringParameter = EditorGUILayout.TextField("String", anim_event.stringParameter);
				break;
			case "UnityEngine.Object":
				anim_event.objectReferenceParameter = EditorGUILayout.ObjectField("Object", anim_event.objectReferenceParameter, typeof(UnityEngine.Object));
				break;
			default:
				var select_idx = 0;
				var enum_values = Enum.GetValues(method_info.GetParameters()[0].ParameterType);
				for(int enum_idx = 0; enum_idx < enum_values.Length; ++enum_idx)
				{
					if((int)enum_values.GetValue(enum_idx) == anim_event.intParameter) select_idx = enum_idx;
				}
				select_idx = EditorGUILayout.Popup("Enum", select_idx, Enum.GetNames(method_info.GetParameters()[0].ParameterType));
				anim_event.intParameter = (int)enum_values.GetValue(select_idx);
				break;
		}
	}

	void AddCallableMethod(List<MethodInfo> callable_methods, MethodInfo method_info)
	{
		if(method_info == null) return;
		var mono_methods = typeof(MonoBehaviour).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance);
		if(mono_methods.Any(method => method.Name == method_info.Name)) return;
		var ignore_funcs = new string[] {
			"Awake",
			"Start",
			"Update",
		};
		if(ignore_funcs.Any(func_name => func_name == method_info.Name)) return;
		if(method_info.GetParameters().Length > 1) return;
		// TODO : int, float, string, Object以外のパラメータは弾く
		if(callable_methods.Any(method => method.Name == method_info.Name))
		{
			callable_methods.RemoveAll(method => method.Name == method_info.Name);
			return;
		}

		callable_methods.Add(method_info);
	}

	string FormatCallableMethodName(MethodInfo method_info)
	{
		if(method_info.GetParameters().Length == 0) return method_info.Name + "( )";
		return method_info.Name + "( " + ConvertTypeName(method_info.GetParameters()[0].ParameterType.ToString()) + " )";
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
			var win_type = w.GetType();
			if(win_type.ToString().Contains("AnimationEventPopup") == false) continue;

			var anim_clip_field = win_type.GetField("m_Clip", BindingFlags.NonPublic | BindingFlags.Instance);
			var root_obj_field = win_type.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
			var event_idx_field = win_type.GetField("m_EventIndex", BindingFlags.NonPublic | BindingFlags.Instance);

			w.Close();

			var window = EditorWindow.GetWindow(typeof(AnimationEventWindow)) as AnimationEventWindow;
			window.animClip = anim_clip_field.GetValue(w) as AnimationClip;
			window.rootObject = root_obj_field.GetValue(w) as GameObject;
			window.eventIndex = (int)event_idx_field.GetValue(w);

			window.Show();
		}
	}
}
