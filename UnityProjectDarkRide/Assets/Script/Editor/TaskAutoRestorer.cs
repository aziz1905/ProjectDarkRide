using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tool Otomatis Pemulih TaskManager:
/// Meneliti seluruh objek pemicu/interaksi di Scene aktif (HoldInteraction, WireTask, Gurney, FuseBox, ZoneTaskTrigger)
/// dan otomatis mengisi kembali TaskManager dengan judul-judul tugas yang sesuai tanpa perlu mengetik ulang manual!
/// </summary>
public class TaskAutoRestorer : MonoBehaviour
{
    [MenuItem("Tools/Auto Restore All Tasks From Scene Triggers")]
    public static void AutoRestoreTasksInCurrentScene()
    {
        TaskManager taskMgr = FindObjectOfType<TaskManager>(true);
        if (taskMgr == null)
        {
            Debug.LogError("[TASK RESTORER] TaskManager tidak ditemukan di Scene ini!");
            return;
        }

        Undo.RecordObject(taskMgr, "Auto Restore Tasks");

        Dictionary<string, string> foundTasks = new Dictionary<string, string>();

        // 1. Scan ZoneTaskTrigger
        ZoneTaskTrigger[] zoneTriggers = FindObjectsOfType<ZoneTaskTrigger>(true);
        foreach (var z in zoneTriggers)
        {
            SerializedObject so = new SerializedObject(z);
            string inspectId = so.FindProperty("inspectionTaskId")?.stringValue;
            string wireId = so.FindProperty("wiringTaskId")?.stringValue;

            if (!string.IsNullOrEmpty(inspectId) && !foundTasks.ContainsKey(inspectId))
                foundTasks[inspectId] = GetReadableTitle(inspectId, z.gameObject.name);

            if (!string.IsNullOrEmpty(wireId) && !foundTasks.ContainsKey(wireId))
                foundTasks[wireId] = GetReadableTitle(wireId, z.gameObject.name);
        }

        // 2. Scan Gurney
        GurneyDirectPushPull[] gurneys = FindObjectsOfType<GurneyDirectPushPull>(true);
        foreach (var g in gurneys)
        {
            SerializedObject so = new SerializedObject(g);
            string gId = so.FindProperty("taskId")?.stringValue;
            if (!string.IsNullOrEmpty(gId) && !foundTasks.ContainsKey(gId))
                foundTasks[gId] = GetReadableTitle(gId, "Kembalikan Gurney Ke Posisi Parkir Stasiun");
        }

        // 3. Scan FuseBox
        FuseBoxController[] fuseBoxes = FindObjectsOfType<FuseBoxController>(true);
        foreach (var f in fuseBoxes)
        {
            SerializedObject so = new SerializedObject(f);
            string fId = so.FindProperty("taskId")?.stringValue;
            if (!string.IsNullOrEmpty(fId) && !foundTasks.ContainsKey(fId))
                foundTasks[fId] = GetReadableTitle(fId, "Perbaiki Sekring Kelistrikan Utama");
        }

        // 4. Scan LampBulbFixture
        LampBulbFixture[] lamps = FindObjectsOfType<LampBulbFixture>(true);
        foreach (var l in lamps)
        {
            SerializedObject so = new SerializedObject(l);
            string lId = so.FindProperty("taskId")?.stringValue;
            if (!string.IsNullOrEmpty(lId) && !foundTasks.ContainsKey(lId))
                foundTasks[lId] = GetReadableTitle(lId, "Ganti Bohlam Lampu Rusak");
        }

        // 5. Scan Interactables Lainnya (Wire, Screwdriver, Timing, Hold)
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var script in allScripts)
        {
            if (script == null) continue;
            SerializedObject so = new SerializedObject(script);
            SerializedProperty prop = so.FindProperty("taskId");
            if (prop != null && !string.IsNullOrEmpty(prop.stringValue))
            {
                string tId = prop.stringValue;
                if (!foundTasks.ContainsKey(tId))
                {
                    foundTasks[tId] = GetReadableTitle(tId, script.gameObject.name);
                }
            }
        }

        // Jika tidak ditemukan trigger sama sekali -> Pasang default dasar
        if (foundTasks.Count == 0)
        {
            foundTasks["INSPECT_STATION"] = "Periksa Area Stasiun & Wahana";
            foundTasks["CHECK_RAILS"] = "Periksa Sakelar & Jalur Rel Utama";
            foundTasks["FIX_WIRE"] = "Perbaiki Kabel Korslet & Kelistrikan";
        }

        // Isi kembali TaskManager dengan daftar yang ditemukan
        SerializedObject tmSo = new SerializedObject(taskMgr);
        SerializedProperty listProp = tmSo.FindProperty("taskList");
        listProp.ClearArray();

        int index = 0;
        foreach (var kvp in foundTasks)
        {
            listProp.InsertArrayElementAtIndex(index);
            SerializedProperty elem = listProp.GetArrayElementAtIndex(index);

            elem.FindPropertyRelative("id").stringValue = kvp.Key;
            elem.FindPropertyRelative("title").stringValue = kvp.Value;
            elem.FindPropertyRelative("isCompleted").boolValue = false;
            elem.FindPropertyRelative("isSubTask").boolValue = false;
            elem.FindPropertyRelative("isRevealed").boolValue = true;
            elem.FindPropertyRelative("currentProgress").intValue = 0;
            elem.FindPropertyRelative("maxProgress").intValue = 1;

            index++;
        }

        tmSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(taskMgr);

        Debug.Log($"<color=green>[TASK RESTORER] BERHASIL MEMULIHKAN {foundTasks.Count} TUGAS SECARA OTOMATIS DI SCENE INI!</color>");
    }

    private static string GetReadableTitle(string taskId, string objectName)
    {
        string upper = taskId.ToUpper();
        if (upper.Contains("WIRE") || upper.Contains("KABEL")) return "Perbaiki Kabel Korslet & Kelistrikan";
        if (upper.Contains("GURNEY") || upper.Contains("PARK")) return "Kembalikan Gurney Ke Posisi Parkir Stasiun";
        if (upper.Contains("PANEL") || upper.Contains("INSPECT")) return "Inspeksi Panel Kelistrikan Ruangan";
        if (upper.Contains("LAMP") || upper.Contains("BULB")) return "Ganti Bohlam Lampu Rusak Area";
        if (upper.Contains("FUSE") || upper.Contains("SEKRING")) return "Perbaiki Sekring Kelistrikan Utama";
        if (upper.Contains("CART") || upper.Contains("KERETA")) return "Periksa Kondisi Kereta Wahana";

        return $"Perawatan: {objectName} ({taskId})";
    }
}
