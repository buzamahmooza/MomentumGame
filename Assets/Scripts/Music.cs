using System.Collections;
using System.Timers;
using UnityEngine;
using Random = System.Random;

public class Music : MonoBehaviour
{
    [SerializeField] private AudioClip[] tracks;
    [SerializeField] private bool shufflePlaylist = false;
    private AudioSource audioSource;
    private Playlist _playlist;

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
        var audioClip = _playlist.Next();
        audioSource.PlayOneShot(audioClip);
        StartCoroutine(PlayAfterTrackIsOver(audioClip));
    }

    private IEnumerator PlayAfterTrackIsOver(AudioClip clip)
    {
        yield return new WaitForSeconds(clip.length);
        var nextAudioClip = _playlist.Next();
        audioSource.PlayOneShot(nextAudioClip);
        StartCoroutine(PlayAfterTrackIsOver(nextAudioClip));
        yield return null;
    }
}