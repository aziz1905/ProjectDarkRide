using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabReplacer : EditorWindow
{
    private string targetNameFilter = "wall";
    private GameObject replacementPrefab = null;
    private bool keepScale = true;
    private bool keepRotation = true;
    private bool caseSensitive = false;

    [MenuItem("Tools/Prefab Replacer")]
    public static void ShowWindow()
    {
        GetWindow<PrefabReplacer>("Prefab Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace Graybox Objects with Prefab", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        targetNameFilter = EditorGUILayout.TextField("Target Name Filter", targetNameFilter);
        caseSensitive = EditorGUILayout.Toggle("Case Sensitive", caseSensitive);
        
        GUILayout.Space(5);
        replacementPrefab = (GameObject)EditorGUILayout.ObjectField("Replacement Prefab", replacementPrefab, typeof(GameObject), false);
        
        GUILayout.Space(5);
        keepRotation = EditorGUILayout.Toggle("Keep Original Rotation", keepRotation);
        keepScale = EditorGUILayout.Toggle("Keep Original Scale", keepScale);

        GUILayout.Space(15);
        if (GUILayout.Button("Replace All Matching Objects"))
        {
            ReplaceObjects();
        }
    }

    private void ReplaceObjects()
    {
        if (replacementPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a replacement Prefab first!", "OK");
            return;
        }

        if (string.IsNullOrEmpty(targetNameFilter))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a target name filter!", "OK");
            return;
        }

        // Find all root game objects in active scene to search hierarchically
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        List<GameObject> objectsToReplace = new List<GameObject>();

        string filter = caseSensitive ? targetNameFilter : targetNameFilter.ToLower();

        foreach (GameObject go in allObjects)
        {
            // Skip assets, prefab mode isolated views, etc.
            if (go.hideFlags != HideFlags.None) continue;
            
            string goName = caseSensitive ? go.name : go.name.ToLower();

            if (goName.Contains(filter))
            {
                // Ensure we don't try to replace the prefab itself if it's in the scene
                if (go == replacementPrefab) continue;
                objectsToReplace.Add(go);
            }
        }

        if (objectsToReplace.Count == 0)
        {
            EditorUtility.DisplayDialog("Result", $"No objects found with name containing '{targetNameFilter}'", "OK");
            return;
        }

        string confirmMsg = $"Found {objectsToReplace.Count} objects matching '{targetNameFilter}'.\nDo you want to replace all of them with '{replacementPrefab.name}'?\n\nThis action can be undone (Ctrl+Z).";
        if (!EditorUtility.DisplayDialog("Confirm Replacement", confirmMsg, "Yes, Replace All", "Cancel"))
        {
            return;
        }

        int replacedCount = 0;
        
        // Register undo group for the whole operation
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Replace Objects with Prefab");
        int groupIndex = Undo.GetCurrentGroup();

        foreach (GameObject oldObj in objectsToReplace)
        {
            if (oldObj == null) continue;

            // Instantiate prefab as an actual prefab instance in Unity
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(replacementPrefab);
            if (newObj == null) continue;

            // Record creation for Undo
            Undo.RegisterCreatedObjectUndo(newObj, "Create Replacement Prefab");

            // Copy transform data
            newObj.transform.position = oldObj.transform.position;
            
            if (keepRotation)
                newObj.transform.rotation = oldObj.transform.rotation;
            else
                newObj.transform.rotation = replacementPrefab.transform.rotation;

            if (keepScale)
                newObj.transform.localScale = oldObj.transform.localScale;
            else
                newObj.transform.localScale = replacementPrefab.transform.localScale;

            // Maintain hierarchy parent
            newObj.transform.SetParent(oldObj.transform.parent, true);
            
            // Match active state
            newObj.SetActive(oldObj.activeSelf);

            // Destroy old object with undo support
            Undo.DestroyObjectImmediate(oldObj);
            
            replacedCount++;
        }

        Undo.CollapseUndoOperations(groupIndex);

        EditorUtility.DisplayDialog("Success", $"Successfully replaced {replacedCount} objects!", "OK");
    }
}
