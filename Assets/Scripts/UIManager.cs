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
        SceneManager.LoadScene("Level1");
    }

    public void PlayClickSound()
    {
        audioSource.PlayOneShot(clickSound);
    }
}

