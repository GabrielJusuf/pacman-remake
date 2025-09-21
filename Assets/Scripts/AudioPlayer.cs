using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip introBGM;
    [SerializeField] private AudioClip ghostNormalBGM;

    AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    void Start()
    {
        StartCoroutine(PlayIntroThenNormal());
    }

    IEnumerator PlayIntroThenNormal()
    {
        if (introBGM != null && source != null)
        {
            source.loop = false;
            source.clip = introBGM;
            source.Play();

            float elapsed = 0f;
            while (elapsed < 3f && source.isPlaying)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (ghostNormalBGM != null && source != null)
        {
            source.loop = true;
            source.clip = ghostNormalBGM;
            source.Play();
        }
    }
}
