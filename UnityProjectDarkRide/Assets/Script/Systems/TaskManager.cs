using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// TaskManager: Pengelola Tugas & Checklist Notebook (TAB).
/// Struktur TaskData:
/// - Id, Title, ParentId (Hubungan Sub-Task), IsCompleted, IsSubTask, IsRevealed, CurrentProgress.
/// Production-Ready: Clean & 0% Spam Log & 0 Warning.
/// </summary>
public class TaskManager : MonoBehaviour
{
    private static TaskManager _instance;
    public static TaskManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TaskManager>(true);
            }
            return _instance;
        }
    }

    [System.Serializable]
    public class TaskData
    {
        public string id;
        public string title;
        public string parentId;       // ID Induk Utama jika tugas ini adalah Sub-Task
        public bool isCompleted;
        public bool isSubTask;        // True jika tugas ini adalah sub-task dinamis
        public bool isRevealed;       // True jika tugas sudah terbuka & terlihat di notebook
        public int currentProgress;   // Progress pengerjaan saat ini (misal: 1, 2, 3)

        public TaskData() 
        {
            isRevealed = true;
        }

        public TaskData(string id, string title, string parentId = "", bool isSub = false, bool isRev = true)
        {
            this.id = id;
            this.title = title;
            this.parentId = parentId;
            this.isSubTask = isSub;
            this.isRevealed = isRev;
            this.isCompleted = false;
            this.currentProgress = 0;
        }
    }

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI taskChecklistText;

    [Header("Task List")]
    [SerializeField] private List<TaskData> taskList = new List<TaskData>();

    private void Awake()
    {
        if (_instance == null) _instance = this;
        UpdateNotebookUI();
    }

    /// <summary>
    /// Selesaikan tugas berdasarkan ID
    /// </summary>
    public void CompleteTask(string taskId)
    {
        if (taskList == null) return;

        TaskData task = taskList.Find(t => t != null && !string.IsNullOrEmpty(t.id) && t.id.Equals(taskId, System.StringComparison.OrdinalIgnoreCase));
        if (task != null)
        {
            task.currentProgress++;
            task.isCompleted = true;
            UpdateNotebookUI();
            IsAllTasksCompleted();
        }
    }

    /// <summary>
    /// Buka sub-task dinamis (misal: kabel rusak/korslet/lukisan miring)
    /// </summary>
    public void RevealSubTask(string taskId)
    {
        if (taskList == null) return;

        TaskData task = taskList.Find(t => t != null && !string.IsNullOrEmpty(t.id) && t.id.Equals(taskId, System.StringComparison.OrdinalIgnoreCase));
        if (task != null)
        {
            task.isSubTask = true;
            task.isRevealed = true;
            UpdateNotebookUI();
        }
    }

    /// <summary>
    /// Reset semua tugas kembali ke kondisi awal saat Player Respawn/Retry
    /// </summary>
    public void ResetAllTasks()
    {
        if (taskList == null) return;

        foreach (var task in taskList)
        {
            if (task == null) continue;
            task.isCompleted = false;
            task.currentProgress = 0;
            if (task.isSubTask)
            {
                task.isRevealed = false;
            }
        }
        UpdateNotebookUI();
    }

    public void UpdateNotebookUI()
    {
        if (taskChecklistText != null && taskList != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b>CHECKLIST TUGAS SHIFT:</b>\n");

            foreach (var task in taskList)
            {
                if (task == null) continue;
                if (task.isSubTask && !task.isRevealed) continue;

                string titleDisplay = !string.IsNullOrEmpty(task.title) ? task.title : task.id;
                if (string.IsNullOrEmpty(titleDisplay)) continue;

                string checkMark = task.isCompleted ? "[X]" : "[ ]";

                if (task.isCompleted)
                {
                    sb.AppendLine($"<s>{checkMark} {titleDisplay}</s>");
                }
                else
                {
                    sb.AppendLine($"{checkMark} {titleDisplay}");
                }
            }

            taskChecklistText.text = sb.ToString();
        }
    }

    public bool IsAllTasksCompleted()
    {
        if (taskList == null || taskList.Count == 0) return false;

        foreach (var task in taskList)
        {
            if (task == null) continue;
            if (task.isSubTask && !task.isRevealed) continue;

            if (!task.isCompleted)
            {
                return false;
            }
        }
        return true;
    }

    public List<TaskData> TaskList => taskList;
}
