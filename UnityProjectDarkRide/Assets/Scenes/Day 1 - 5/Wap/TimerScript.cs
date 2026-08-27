using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimerScript : MonoBehaviour
{
    public float time;
    public TextMeshProUGUI timerText;
    
    // Lama waktu nyata (dalam detik) untuk menyelesaikan 6 jam game
    public float totalRealTimeInSeconds = 60f; 

    void Update()
    {
        if (time < totalRealTimeInSeconds)
        {
            time += Time.deltaTime;

            // Konversi proporsi waktu ke 6 jam (0 hingga 6)
            float gameHours = (time / totalRealTimeInSeconds) * 6f;

            int hours = Mathf.FloorToInt(gameHours);
            int minutes = Mathf.FloorToInt((gameHours - hours) * 60f);

            // Format menjadi "00:00"
            timerText.text = string.Format("{0:00}:{1:00}", hours, minutes);
        }
        else
        {
            // Kunci tampilan di jam 06:00 saat waktu habis
            timerText.text = "06:00"; 
        }
    }
}