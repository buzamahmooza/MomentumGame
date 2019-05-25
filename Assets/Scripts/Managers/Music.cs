using System;
using System.Collections;
using System.Timers;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

[RequireComponent(typeof(AudioSource))]
public class Music : MonoBehaviour
{
    [FormerlySerializedAs("tracks")] [SerializeField] private AudioClip[] m_tracks;
    [FormerlySerializedAs("shufflePlaylist")] [SerializeField] private bool m_shufflePlaylist = false;

    [NonSerialized] public AudioSource AudioSource;
    private Playlist m_playlist;
    private IEnumerator m_currentCoroutine;

    private void Awake()
    {
        AudioSource = GetComponent<AudioSource>();
        m_playlist = new Playlist(m_tracks);
        if (AudioSource.clip) m_playlist.Add(AudioSource.clip);
        AudioSource.playOnAwake = false;
        AudioSource.clip = null;
        AudioSource.loop = false;
    }

    // Use this for initialization
    private void Start()
    {
        if (m_shufflePlaylist)
            m_playlist.Shuffle();
        // play the first sound track
        AudioClip audioClip = m_playlist.Next();
        AudioSource.PlayOneShot(audioClip);
        QueueClip(audioClip);
    }

    public void PlayPrevious()
    {
        PlayNext();
    }

    public void PlayNext()
    {
        if (m_currentCoroutine != null)
            StopCoroutine(m_currentCoroutine);
        AudioSource.Stop();
        AudioClip nextAudioClip = m_playlist.Next();
        AudioSource.PlayOneShot(nextAudioClip);
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
        m_currentCoroutine = PlayAfterTrackIsOver(audioClip);
        StartCoroutine(m_currentCoroutine);
    }
}