using System.Collections;
using System.Collections.Generic;
using _Scripts;
using UnityEngine;

public class MusicManager : GenericSingleton<MusicManager>
{
    private AudioSource _audioSource;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;
    void Start()
    {
        DontDestroyOnLoad(this);
        _audioSource = GetComponent<AudioSource>();
        PlayMenuMusic();
    }

    public void PlayMenuMusic()
    {
        _audioSource.clip = menuMusic;
        _audioSource.Play();
    }

    public void PlayGameMusic()
    {
        _audioSource.clip = gameMusic;
        _audioSource.Play();
    }
    
}
