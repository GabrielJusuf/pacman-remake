using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Header("Timer UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private string emptyTimerPlaceholder = "--:--";

    [Header("Score UI")]
    [SerializeField] private TMP_Text scoreText;
    
    private void Start()
    {
        UpdateTimerDisplay(0f);
        UpdateScoreDisplay(0);
    }

    public void UpdateTimerDisplay(float elapsedSeconds)
    {
        if (timerText == null)
            return;

        if (elapsedSeconds < 0f)
        {
            timerText.text = emptyTimerPlaceholder;
            return;
        }

        int minutes = Mathf.FloorToInt(elapsedSeconds / 60f);
        int seconds = Mathf.FloorToInt(elapsedSeconds % 60f);
        int centiseconds = Mathf.FloorToInt((elapsedSeconds - Mathf.Floor(elapsedSeconds)) * 100f);

        timerText.text = $"{minutes:00}:{seconds:00}.{centiseconds:00}";
    }

    public void UpdateScoreDisplay(int score)
    {
        if (scoreText == null)
            return;

        scoreText.text = $"{score}";
    }
}
