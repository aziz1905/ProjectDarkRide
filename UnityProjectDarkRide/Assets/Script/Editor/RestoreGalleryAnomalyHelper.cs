#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class RestoreGalleryAnomalyHelper : EditorWindow
{
    [MenuItem("Tools/DarkRide/Pulihkan Setup Anomali Galeri")]
    public static void RestoreSetup()
    {
        Debug.Log("<color=yellow>[Restorasi] Memulai pemulihan GalleryAnomalyController...</color>");

        // 1. Cari atau buat GalleryAnomalyController
        GameObject controllerObj = GameObject.Find("GalleryAnomalyController");
        if (controllerObj == null)
        {
            controllerObj = new GameObject("GalleryAnomalyController");
            Undo.RegisterCreatedObjectUndo(controllerObj, "Create GalleryAnomalyController");
            Debug.Log("<color=green>[Restorasi] Membuat GameObject GalleryAnomalyController baru.</color>");
        }

        MaskedStatueGalleryAnomaly anomaly = controllerObj.GetComponent<MaskedStatueGalleryAnomaly>();
        if (anomaly == null)
        {
            anomaly = Undo.AddComponent<MaskedStatueGalleryAnomaly>(controllerObj);
        }

        SerializedObject so = new SerializedObject(anomaly);
        so.Update();

        // 2. Cari FuseBoxController (Fuse)
        FuseBoxController fuse = Object.FindObjectOfType<FuseBoxController>();
        if (fuse != null)
        {
            so.FindProperty("panelFuseBox").objectReferenceValue = fuse;
            Debug.Log($"<color=cyan>[Restorasi] Menghubungkan Panel Fuse Box: {fuse.name}</color>");
        }

        // 3. Cari DarkRideCartController (Cart)
        DarkRideCartController cart = Object.FindObjectOfType<DarkRideCartController>();
        if (cart != null)
        {
            so.FindProperty("darkRideCart").objectReferenceValue = cart;
            Debug.Log($"<color=cyan>[Restorasi] Menghubungkan Kereta Wahana: {cart.name}</color>");
        }

        // 4. Cari Trigger Pintu Keluar
        GameObject triggerDoorObj = GameObject.Find("TriggerAnomalyGallery");
        if (triggerDoorObj == null)
        {
            Collider[] colliders = Object.FindObjectsOfType<Collider>();
            foreach (var col in colliders)
            {
                if (col.name.ToLower().Contains("gallery") && col.isTrigger)
                {
                    triggerDoorObj = col.gameObject;
                    break;
                }
            }
        }
        if (triggerDoorObj != null)
        {
            Collider col = triggerDoorObj.GetComponent<Collider>();
            so.FindProperty("exitDoorTrigger").objectReferenceValue = col;
            Debug.Log($"<color=cyan>[Restorasi] Menghubungkan Exit Door Trigger: {triggerDoorObj.name}</color>");
        }

        // 5. Cari point dan point (1)
        Transform pointObj = null;
        Transform point1Obj = null;

        Transform[] allTransforms = Object.FindObjectsOfType<Transform>();
        foreach (var t in allTransforms)
        {
            if (t.name == "point" && pointObj == null) pointObj = t;
            else if (t.name == "point (1)" && point1Obj == null) point1Obj = t;
        }

        if (pointObj != null)
        {
            so.FindProperty("cartWaitPoint").objectReferenceValue = pointObj;
            Debug.Log("<color=cyan>[Restorasi] Menghubungkan Cart Wait Point: point</color>");
        }

        if (point1Obj != null)
        {
            so.FindProperty("rearClusterPoint").objectReferenceValue = point1Obj;
            Debug.Log("<color=cyan>[Restorasi] Menghubungkan Rear Cluster Point: point (1)</color>");
        }

        // 6. Kumpulkan atau Buat Patung MaskMan (15 buah: 0..13 chasers, 14 corner)
        GameObject maskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3DAsset/Props/MaskMan1.fbx");
        if (maskPrefab == null)
        {
            string[] guids = AssetDatabase.FindAssets("MaskMan1 t:Model");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                maskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        List<GameObject> maskMans = new List<GameObject>();
        for (int i = 0; i <= 14; i++)
        {
            string expectedName = i == 0 ? "MaskMan" : $"MaskMan ({i})";
            GameObject found = GameObject.Find(expectedName);
            if (found != null)
            {
                found.transform.SetParent(controllerObj.transform, true);
                maskMans.Add(found);
            }
            else if (maskPrefab != null)
            {
                GameObject newMask = (GameObject)PrefabUtility.InstantiatePrefab(maskPrefab);
                newMask.name = expectedName;
                newMask.transform.SetParent(controllerObj.transform, false);
                Undo.RegisterCreatedObjectUndo(newMask, "Instantiate MaskMan");
                maskMans.Add(newMask);
            }
        }

        // 7. Pasang MaskMan (14) ke cornerMaskMan
        if (maskMans.Count > 14)
        {
            GameObject cornerMan = maskMans[14];
            so.FindProperty("cornerMaskMan").objectReferenceValue = cornerMan;
            cornerMan.SetActive(false);
            Debug.Log("<color=cyan>[Restorasi] Menghubungkan Corner Mask Man: MaskMan (14)</color>");
        }

        // 8. Pasang 14 chasers ke list
        SerializedProperty chasersProp = so.FindProperty("chasers");
        int chaserCount = Mathf.Min(14, maskMans.Count);
        chasersProp.arraySize = chaserCount;

        for (int i = 0; i < chaserCount; i++)
        {
            SerializedProperty elem = chasersProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("chaserObject").objectReferenceValue = maskMans[i];

            bool isImmediate = (i < 10);
            elem.FindPropertyRelative("isImmediateChaser").boolValue = isImmediate;
            elem.FindPropertyRelative("awakenDistance").floatValue = isImmediate ? 6f : 7f;
            elem.FindPropertyRelative("label").stringValue = isImmediate ? $"Pasukan Belakang {i + 1}" : $"Patung Depan {i - 9}";
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(controllerObj);
        EditorSceneManager.MarkSceneDirty(controllerObj.scene);

        Selection.activeGameObject = controllerObj;
        EditorGUIUtility.PingObject(controllerObj);

        Debug.Log("<color=green><b>[Restorasi Selesai]</b> Seluruh komponen GalleryAnomalyController berhasil dipulihkan! Jangan lupa tekan Ctrl + S untuk menyimpan Scene.</color>");
        EditorUtility.DisplayDialog("Restorasi Berhasil", "Seluruh setup GalleryAnomalyController dan 14 patung MaskMan berhasil dipulihkan secara otomatis!\n\nSilakan tekan Ctrl + S untuk menyimpan scene sekarang.", "OK");
    }
}
#endif
