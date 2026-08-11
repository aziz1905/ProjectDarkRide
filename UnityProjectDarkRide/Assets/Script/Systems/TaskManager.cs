using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Task Manager Terpusat (Dark Ride Maintenance).
/// Mendukung Format Checklist Bersih: Simbol [✓] di Kanan & Coret Selesai.
/// Mendukung Dynamic Sub-Task (Tugas Tambahan Kabel Rusak Dinamis).
/// 0% Beban CPU & Zero Garbage Collection.
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

    /// <summary>
    /// Memunculkan Sub-Task kabel rusak secara dinamis tepat di bawah tugas inspeksi
    /// </summary>
    public void RevealSubTask(string subTaskId)
    {
        foreach (var task in taskList)
        {
            if (task.taskId.Trim().Equals(subTaskId.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                if (!task.isRevealed)
                {
                    task.isRevealed = true;
                    Debug.Log($"<color=yellow>[SUB-TASK REVEALED] Tugas Tambahan Ditemukan: '{task.taskDescription}'</color>");
                    UpdateNotebookUI();
                }
                return;
            }
        }
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
                        Debug.Log($"[TASK COMPLETED] Tugas '{task.taskDescription}' SELESAI [✓]!");
                    }
                    else
                    {
                        Debug.Log($"[TASK PROGRESS] Tugas '{task.taskDescription}' Progress: {task.currentCount}/{task.requiredCount}");
                    }

                    UpdateNotebookUI();
                    CheckAllTasksCompleted();
                }
                return;
            }
        }
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

    /// <summary>
    /// Meng-update Teks Checklist di Notebook [TAB] secara Real-Time dengan Format Minimalis Elegan
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
                // Jika tugas adalah Sub-Task tersembunyi, lewati jangan ditampilkan dulu
                if (task.isSubTask && !task.isRevealed) continue;

                string prefix = task.isSubTask ? subTaskBullet : mainTaskBullet;

                if (task.isCompleted)
                {
                    // Teks Dicoret Cokelat/Abu-Abu + Simbol [X] Hijau di Sebelah KANAN
                    sb.AppendLine($"{prefix}<s color=#888888>{task.taskDescription}</s>   <color={completionMarkColor}><b>{completionMark}</b></color>");
                }
                else
                {
                    // Teks Bersih dengan Bullet
                    sb.AppendLine($"{prefix}{task.taskDescription}");
                }

                // Beri jarak spasi antar tugas agar rapi dan tidak berdempetan
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
            // Abaikan sub-task yang memang tidak pernah terpicu
            if (task.isSubTask && !task.isRevealed) continue;

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

    public List<TaskData> TaskList => taskList;
}
