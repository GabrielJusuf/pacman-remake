using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    public AudioClip clickSound;
    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
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
}

