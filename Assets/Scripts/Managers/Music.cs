using System;
using System.Collections;
using System.Timers;
using UnityEngine;
using Random = System.Random;

public class Music : MonoBehaviour
{
    [SerializeField] private AudioClip[] tracks;
    [SerializeField] private bool shufflePlaylist = false;

    [NonSerialized] public AudioSource audioSource;
    private Playlist _playlist;
    private IEnumerator currentCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        _playlist = new Playlist(tracks);
        if (audioSource.clip) _playlist.Add(audioSource.clip);
        audioSource.playOnAwake = false;
        audioSource.clip = null;
        audioSource.loop = false;
    }

    // Use this for initialization
    private void Start()
    {
        if (shufflePlaylist)
            _playlist.Shuffle();
        // play the first sound track
        AudioClip audioClip = _playlist.Next();
        audioSource.PlayOneShot(audioClip);
        QueueClip(audioClip);
    }

    public void PlayPrevious()
    {
        PlayNext();
    }

    public void PlayNext()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        audioSource.Stop();
        AudioClip nextAudioClip = _playlist.Next();
        audioSource.PlayOneShot(nextAudioClip);
        QueueClip(nextAudioClip);
    }

    private IEnumerator PlayAfterTrackIsOver(AudioClip clip)
    {
        yield return new WaitForSecondsRealtime(clip.length);
        PlayNext();
        yield return null;
    }

    /// <summary>
    /// sets coroutine and calls PlayAfterTrackIsOver
    /// </summary>
    /// <param name="audioClip"></param>
    private void QueueClip(AudioClip audioClip)
    {
        currentCoroutine = PlayAfterTrackIsOver(audioClip);
        StartCoroutine(currentCoroutine);
    }
}