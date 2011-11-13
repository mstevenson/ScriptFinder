using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using System.IO;


public enum ScriptType
{
	CS,
	JS,
	Boo
}


public class ScriptReference
{
	public readonly MonoScript script;
	public readonly ScriptType scriptType;
	
	/// <summary>
	/// Prefabs containing the script.
	/// </summary>
	public List<UnityEngine.Object> prefabs = new List<UnityEngine.Object> ();
	/// <summary>
	/// Scene files containing the script.
	/// </summary>
	public List<UnityEngine.Object> scenes = new List<UnityEngine.Object> ();
	/// <summary>
	/// Game objects in the current scene containing the script.
	/// </summary>
	public List<UnityEngine.Object> gameObjects = new List<UnityEngine.Object> ();
	/// <summary>
	/// Other scripts that reference this script.
	/// </summary>
	public List<UnityEngine.Object> otherScripts = new List<UnityEngine.Object> ();
	
	
	public ScriptReference (MonoScript script)
	{
		this.script = script;
//		string ext = Path.GetExtension (script.name
//		if (this.script.name
	}
	
	
	/// <summary>
	/// Is the script attached to anything within any scene or prefab?
	/// </summary>
	public bool IsAttached {
		get { return prefabs.Count != 0 || scenes.Count != 0; }
	}

	/// <summary>
	/// Is the script referenced by another script?
	/// </summary>
	public bool IsReferenced {
		get { return otherScripts.Count != 0; }
	}
}


/// <summary>
/// Finds MonoBehaviour that are not attached to any objects in any scene.
/// </summary>
/// <remarks>
/// This tool does not take into account prefabs or components that are instantiated at runtime. Use with caution.
/// </remarks>
public sealed class ScriptFinder : EditorWindow
{
	#region Window Setup

	private static ScriptFinder window;

	[MenuItem("Custom/Find Unused Scripts")]
	static void Init ()
	{
		ScriptFinder window = (ScriptFinder)EditorWindow.GetWindow (typeof(ScriptFinder), false, "Script Finder");
	}

	#endregion

	
	public enum AssetType {
		Prefab,
		Scene,
		Script
	}
	
	
	/// <summary>
	/// A Scene, Prefab, or MonoScript which relies a particular MonoBehaviour.
	/// </summary>
	private class ScriptDepender {
		public UnityEngine.Object obj;
		public string path;
		AssetType assetType;
	}
	
	
	
	/// <summary>
	/// Get MonoScripts which are used in the given scene or prefab.
	/// </summary>
	private List<MonoScript> GetScriptsInScenesOrPrefabs (UnityEngine.Object[] scenesOrPrefabs)
	{
		List<MonoScript> scripts = new List<MonoScript> ();
		foreach (var s in EditorUtility.CollectDependencies (scenesOrPrefabs)) {
			if (s as MonoScript) {
				scripts.Add ((MonoScript)s);
			}
		}
		return scripts;
	}
	
	
	
	/// <summary>
	/// Get all scene files contained in the project.
	/// </summary>
	private List<UnityEngine.Object> GetAllSceneAssets ()
	{
		return GetAllAssetsOfTypeWithExtension<UnityEngine.Object> (".unity");
	}
	
	
	/// <summary>
	/// Get all prefabs contained in the project.
	/// </summary>
	private List<UnityEngine.Object> GetAllPrefabAssets ()
	{
		return GetAllAssetsOfTypeWithExtension<UnityEngine.Object> (".prefab");
	}
	
	
	/// <summary>
	/// Get all asset files in the project which match the given extension (including the dot)
	/// </summary>
	private List<T> GetAllAssetsOfTypeWithExtension<T> (string extension)
		where T : UnityEngine.Object
	{
		List<T> objs = new List<T> ();
		foreach (string path in AssetDatabase.GetAllAssetPaths ()) {
			if (Path.GetExtension (path) != extension)
				continue;
			T asset = AssetDatabase.LoadAssetAtPath (path, typeof(T)) as T;
			if (asset != null)
				objs.Add (asset);
		}
		return objs;
	}
	
	
	/// <summary>
	/// Find all MonoBehaviour files contained in the project.
	/// </summary>
	private static List<MonoScript> FindAllMonoBehaviourScriptsInProject ()
	{
		List<MonoScript> scripts = new List<MonoScript> ();
		foreach (var obj in FindObjectsOfTypeIncludingAssets (typeof(MonoScript))) {
			if (obj as MonoScript) {
				MonoScript script = (MonoScript)obj;
				if (script.GetClass ().IsSubclassOf (typeof(MonoBehaviour))) {
					scripts.Add (script);
				}
			}
		}
		return scripts;
	}
	
	
	
	
	
	
	// When displaying scenes that use a particular script, click to highlight the scene file.
	// Double click to open and highlight the objects that use the script. Also expands
	// The view 
	
	// Auto refresh when deleting or changing files. Is there a callback for this? May have to use my Watcher
	
	
	#region GUI
	
	private ScriptList list = new ScriptList ();
	
	void OnGUI ()
	{
		// Toolbar
		GUILayout.BeginHorizontal ("Toolbar");
		{
			if (GUILayout.Button ("Clear", EditorStyles.toolbarButton, GUILayout.Width (45)))
				Clear ();
			if (GUILayout.Button ("Refresh", EditorStyles.toolbarButton, GUILayout.Width (45)))
				Refresh ();
			GUILayout.Space (6);
			ScriptList.unusedOnly = GUILayout.Toggle (ScriptList.unusedOnly, "Only unused", EditorStyles.toolbarButton, GUILayout.Width (70));
			EditorGUILayout.Space ();
		}
		GUILayout.EndHorizontal ();
		
		// Script list
		list.scrollPos = GUILayout.BeginScrollView (list.scrollPos);
		{
			for (int i = 0; i < list.elements.Count; i++) {
				list.elements[i].row = i;
				list.elements[i].Draw ();
			}
		}
		GUILayout.EndScrollView ();
	}
	
	
	private void Clear ()
	{
		list.elements.Clear ();
	}
	
	
	private void Refresh ()
	{
		Debug.Log ("Refreshing");
		
		var allScripts = FindAllMonoBehaviourScriptsInProject ();
		var allSceneAssets = GetAllSceneAssets ();
		var allPrefabAssets = GetAllPrefabAssets ();
		
		List<ScriptReference> references = new List<ScriptReference> ();
		foreach (MonoScript script in allScripts) {
			ScriptReference s = new ScriptReference (script);
			references.Add (s);
		}
		
		list.elements.Clear ();
		foreach (var r in references) {
			ScriptListElement element = new ScriptListElement ();
			element.scriptRef = r;
			list.elements.Add (element);
		}
	}
	
	#endregion
}
