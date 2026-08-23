using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Task Manager Terpusat (Dark Ride Maintenance).
/// Production-Ready: Clean & 0% Spam Log.
/// </summary>
public class TaskManager : MonoBehaviour
{
    private static TaskManager _instance;
    public static TaskManager Instance => _instance;

    [System.Serializable]
    public class TaskData
    {
        public string taskId;           // ID Unik Tugas (misal: "INSPECT_PANEL_1", "FIX_CABLE_1", "CLEAN_TRASH")
        public string taskDescription;  // Deskripsi Tugas (misal: "Inspeksi Panel Listrik Zona 1")
        
        [Header("Sub-Task Dinamis Settings")]
        [Tooltip("CENTANG jika ini adalah Sub-Task anak yang menempel di bawah tugas utama")]
        public bool isSubTask = false;
        [Tooltip("ID Tugas Utama induknya (misal: INSPECT_PANEL_1)")]
        public string parentTaskId = "";
        [Tooltip("HILANGKAN CENTANG jika sub-task ini tersembunyi dan baru muncul jika ada kabel rusak")]
        public bool isRevealed = true;

        [Header("Status Progress")]
        public int requiredCount = 1;   // Jumlah yang harus diselesaikan
        public int currentCount = 0;    // Jumlah yang sudah selesai
        public bool isCompleted = false;
    }

    [Header("Day Title Settings")]
    [SerializeField] private string dayTitle = "CATATAN SHIFT HARI 1";
    [SerializeField] private List<TaskData> taskList = new List<TaskData>();

    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI notebookTitleText;
    [SerializeField] private TextMeshProUGUI taskChecklistText;

    private void Awake()
    {
        if (_instance == null) _instance = this;
    }

    private void Start()
    {
        UpdateNotebookUI();
    }

    public void RevealSubTask(string subTaskId)
    {
        foreach (var task in taskList)
        {
            if (task.taskId.Trim().Equals(subTaskId.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                if (!task.isRevealed)
                {
                    task.isRevealed = true;
                    UpdateNotebookUI();
                }
                return;
            }
        }
    }

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
                    }

                    UpdateNotebookUI();
                    CheckAllTasksCompleted();
                }
                return;
            }
        }
    }

    public void ResetAllTasks()
    {
        foreach (var task in taskList)
        {
            task.currentCount = 0;
            task.isCompleted = false;
            if (task.isSubTask)
            {
                task.isRevealed = false;
            }
        }

        UpdateNotebookUI();
    }

    [Header("Task Completion Mark")]
    [Tooltip("Simbol penyelesaian tugas di sebelah kanan (misal: [X] atau [✓])")]
    [SerializeField] private string completionMark = "[X]";
    [SerializeField] private string completionMarkColor = "#44FF44";

    [Header("List Bullet Styling")]
    [SerializeField] private string mainTaskBullet = "• ";
    [Tooltip("Gunakan indent bersih yang kompatibel dengan seluruh font TextMeshPro")]
    [SerializeField] private string subTaskBullet = "    > "; 
    
    [Header("Line Spacing Settings")]
    [Tooltip("CENTANG agar ada jeda/jarak spasi vertikal antar tugas agar tulisan tidak berdempetan")]
    [SerializeField] private bool addSpacingBetweenTasks = true;

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
                if (task.isSubTask && !task.isRevealed) continue;

                string prefix = task.isSubTask ? subTaskBullet : mainTaskBullet;

                if (task.isCompleted)
                {
                    sb.AppendLine($"{prefix}<s color=#888888>{task.taskDescription}</s>   <color={completionMarkColor}><b>{completionMark}</b></color>");
                }
                else
                {
                    sb.AppendLine($"{prefix}{task.taskDescription}");
                }

                if (addSpacingBetweenTasks)
                {
                    sb.AppendLine();
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
            if (task.isSubTask && !task.isRevealed) continue;

            if (!task.isCompleted)
            {
                allDone = false;
                break;
            }
        }
    }

    public List<TaskData> TaskList => taskList;
}
