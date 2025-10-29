using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{

    [Header("Audio")]
    public AudioClip clickSound;
    private AudioSource audioSource;

    [Header("Start Scene High Score UI")]
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private string highScoreFormat = "High Score\n{0}\n\nTime\n{1}";

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        UpdateHighScoreDisplay();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadLevelOne()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene("Level1");
    }

    public void ExitLevel()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.LoadScene("StartScene");
    }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int Level1Index = SceneManager.GetSceneByName("Level1").buildIndex;

        if (scene.buildIndex == Level1Index)
        {
            // Quit Button
            GameObject exitButtonObject = GameObject.FindGameObjectWithTag("ExitButton");
            Button exitButton = exitButtonObject.GetComponent<Button>();
            exitButton.onClick.AddListener(ExitLevel);
            exitButton.onClick.AddListener(PlayClickSound);
            
        }
    }


    public void PlayClickSound()
    {
        audioSource.PlayOneShot(clickSound);
    }

    private void UpdateHighScoreDisplay()
    {
        if (highScoreText == null)
            return;

        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        float bestTime = PlayerPrefs.GetFloat("HighScoreTime", float.MaxValue);

        string timeString = (bestTime < float.MaxValue)
            ? FormatTime(bestTime)
            : "--:--:--";

        highScoreText.text = $"High Score\n{highScore}\n\nTime\n{timeString}";
    }

    private string FormatTime(float seconds)
    {
        int clampedMilliseconds = Mathf.Max(0, Mathf.FloorToInt(seconds * 1000f));
        int minutes = clampedMilliseconds / 60000;
        int secs = (clampedMilliseconds % 60000) / 1000;
        int millis = (clampedMilliseconds % 1000) / 10;
        return $"{minutes:00}:{secs:00}:{millis:00}";
    }
}
