using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class Music : MonoBehaviour
{
    [SerializeField] private AudioClip[] tracks;
    [SerializeField] private bool shufflePlaylist = false;
    private AudioSource audioSource;
    private Playlist _playlist;

    private void Awake() {
        audioSource = GetComponent<AudioSource>();
        _playlist = new Playlist(tracks);
        if (audioSource.clip) _playlist.Add(audioSource.clip);
        audioSource.playOnAwake = false;
        audioSource.clip = null;
        audioSource.loop = false;
    }

    // Use this for initialization
    void Start() {
        if (shufflePlaylist)
            _playlist.Shuffle();
        // play the first sound track
        var audioClip = _playlist.Next();
        audioSource.PlayOneShot(audioClip);
        StartCoroutine(PlayAfterTrackIsOver(audioClip));
    }

    IEnumerator PlayAfterTrackIsOver(AudioClip clip) {
        yield return new WaitForSeconds(clip.length);

        var nextAudioClip = _playlist.Next();
        audioSource.PlayOneShot(nextAudioClip);
        StartCoroutine(PlayAfterTrackIsOver(nextAudioClip));
    }

}

class Playlist : List<AudioClip>
{
    public int current = -1;

    /// <inheritdoc />
    public Playlist(AudioClip[] tracks) {
        this.AddRange(tracks);
    }
    /// <summary>
    /// Iteratively returns items from the list and will loop when reaching the end
    /// Starts with returning the first item
    /// </summary>
    /// <returns></returns>
    public AudioClip Next() {
        AudioClip next;
        if (current < this.Count - 1) {
            next = this[++current];
        } else { // if at the last song, loop
            current = -1;
            next = Next();
        }
        return next ?? Next();
    }

    public void Shuffle() {
        for (var i = 0; i < this.Count; i++)
            Swap(i, UnityEngine.Random.Range(0, this.Count - 1));

        Debug.Log("New shuffled list:\n" + string.Join("\n", this.Select(x => x.name).ToArray()));
    }

    /// <summary>
    /// Swaps the two items in the list with given indexes
    /// </summary>
    /// <param name="a">index of first element</param>
    /// <param name="b">index of second element</param>
    private void Swap(int a, int b) {
        if (a == b) return;
        if (a < 0 || a >= this.Count) throw new IndexOutOfRangeException(string.Format("The index {0} is out of not valid", a));
        if (b < 0 || b >= this.Count) throw new IndexOutOfRangeException(string.Format("The index {0} is out of not valid", b));

        var temp = this[a];
        this[a] = this[b];
        this[b] = temp;
        temp = null;
    }

}
