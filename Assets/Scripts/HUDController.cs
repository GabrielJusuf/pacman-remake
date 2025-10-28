using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Header("Timer UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Score UI")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Lives UI")]
    [SerializeField] private GameObject[] lifeIcons;

    [Header("Ghost Timer UI")]
    [SerializeField] private GameObject ghostTimerContainer;
    [SerializeField] private TMP_Text ghostTimerText;
    [SerializeField] private string ghostTimerFormat = "{0:0.0}s";

    private void Start()
    {
        UpdateTimerDisplay(0f);
        UpdateScoreDisplay(0);
        SetGhostTimerActive(false);
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

    public void UpdateScoreDisplay(int score)
    {
        if (scoreText == null)
            return;

        scoreText.text = $"{score}";
    }

    public void UpdateLivesDisplay(int livesRemaining)
    {
        if (lifeIcons == null || lifeIcons.Length == 0)
            return;

        int clampedLives = Mathf.Clamp(livesRemaining, 0, lifeIcons.Length);

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            int indexFromRight = lifeIcons.Length - 1 - i;
            GameObject icon = lifeIcons[indexFromRight];
            if (icon == null)
                continue;

            bool shouldBeActive = i < clampedLives;
            icon.SetActive(shouldBeActive);
        }
    }

    public void SetGhostTimerActive(bool isActive)
    {
        if (ghostTimerContainer != null)
        {
            ghostTimerContainer.SetActive(isActive);
        }

        if (!isActive && ghostTimerText != null)
        {
            ghostTimerText.text = string.Empty;
        }
    }

    public void UpdateGhostTimerDisplay(float secondsRemaining)
    {
        if (ghostTimerText == null)
            return;

        float displaySeconds = Mathf.Max(0f, secondsRemaining);

        if (!string.IsNullOrEmpty(ghostTimerFormat) && ghostTimerFormat.Contains("{0"))
        {
            ghostTimerText.text = string.Format(ghostTimerFormat, displaySeconds);
        }
        else
        {
            ghostTimerText.text = $"{displaySeconds:0.0}s";
        }
    }
}
