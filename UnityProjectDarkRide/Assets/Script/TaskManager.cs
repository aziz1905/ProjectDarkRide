using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TaskManager : MonoBehaviour
{
    [System.Serializable]
    public class TaskData
    {
        public string taskId;           // ID Unik Tugas (misal: "CLEAN_TRASH", "FIX_PANEL", "WIPE_MIRROR", "FIX_RAIL")
        public string taskDescription;  // Deskripsi Tugas (misal: "Bersihkan Puing Sampah")
        public int requiredCount = 1;   // Jumlah yang harus diselesaikan (misal: 3 sampah)
        public int currentCount = 0;    // Jumlah yang sudah diselesaikan
        public bool isCompleted = false;
    }

    [Header("Day Tasks Settings")]
    [SerializeField] private string dayTitle = "CATATAN SHIFT HARI 1";
    [SerializeField] private List<TaskData> taskList = new List<TaskData>();

    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI notebookTitleText;
    [SerializeField] private TextMeshProUGUI taskChecklistText;

    private void Start()
    {
        UpdateNotebookUI();
    }

    /// <summary>
    /// Memanggil fungsi ini setiap kali 1 tugas/interaksi selesai
    /// </summary>
    public void CompleteTask(string taskId)
    {
        foreach (var task in taskList)
        {
            if (task.taskId.Trim().Equals(taskId.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                if (!task.isCompleted)
                {
                    task.currentCount++;
                    if (task.currentCount >= task.requiredCount)
                    {
                        task.currentCount = task.requiredCount;
                        task.isCompleted = true;
                        Debug.Log($"[TASK COMPLETED] Tugas '{task.taskDescription}' SELESAI 100%!");
                    }
                    else
                    {
                        Debug.Log($"[TASK PROGRESS] Tugas '{task.taskDescription}' Progress: ({task.currentCount}/{task.requiredCount})");
                    }

                    UpdateNotebookUI();
                    CheckAllTasksCompleted();
                }
                return;
            }
        }
    }

    /// <summary>
    /// Meng-update Teks Checklist di Notebook [TAB] secara Real-Time
    /// </summary>
    public void UpdateNotebookUI()
    {
        if (notebookTitleText != null)
        {
            notebookTitleText.text = dayTitle;
        }

        if (taskChecklistText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (var task in taskList)
            {
                if (task.isCompleted)
                {
                    sb.AppendLine($"[X] <s color=#888888>{task.taskDescription}</s>"); // Centang & Coret jika selesai
                }
                else
                {
                    if (task.requiredCount > 1)
                    {
                        sb.AppendLine($"[ ] {task.taskDescription} ({task.currentCount}/{task.requiredCount})");
                    }
                    else
                    {
                        sb.AppendLine($"[ ] {task.taskDescription}");
                    }
                }
            }

            taskChecklistText.text = sb.ToString();
        }
    }

    private void CheckAllTasksCompleted()
    {
        bool allDone = true;
        foreach (var task in taskList)
        {
            if (!task.isCompleted)
            {
                allDone = false;
                break;
            }
        }

        if (allDone)
        {
            Debug.Log("<color=green>[PROBATION SHIFT COMPLETE] Seluruh tugas Hari ini telah selesai 100%! Silakan Absen di Walkie-Talkie.</color>");
        }
    }
}
