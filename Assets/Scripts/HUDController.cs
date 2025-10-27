using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Header("Timer UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Score UI")]
    [SerializeField] private TMP_Text scoreText;

    private void Start()
    {
        UpdateTimerDisplay(0f);
    }

    public void UpdateTimerDisplay(float elapsedSeconds)
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(elapsedSeconds / 60f);
        int seconds = Mathf.FloorToInt(elapsedSeconds % 60f);
        int centiseconds = Mathf.FloorToInt((elapsedSeconds - Mathf.Floor(elapsedSeconds)) * 100f);

        timerText.text = $"{minutes:00}:{seconds:00}.{centiseconds:00}";
    }
}
