using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class Playlist : List<AudioClip>
{
    public int current = -1;

    /// <inheritdoc />
    public Playlist(AudioClip[] tracks)
    {
        this.AddRange(tracks);
    }

    /// <summary>
    /// Iteratively returns items from the list and will loop when reaching the end
    /// Starts with returning the first item
    /// </summary>
    /// <returns></returns>
    public AudioClip Next()
    {
        while (true)
        {
            AudioClip next;
            if (current < this.Count - 1)
            {
                next = this[++current];
            }
            else
            {
                // if at the last song, loop
                current = -1;
                next = Next();
            }

            if (next != null)
                return next;
        }
    }

    public void Shuffle()
    {
        for (var i = 0; i < this.Count; i++)
            Swap(i, UnityEngine.Random.Range(0, this.Count - 1));

        Debug.Log("New shuffled list:\n" + string.Join("\n", this.Select(x => x.name).ToArray()));
    }

    /// <summary>
    /// Swaps the two items in the list with given indexes
    /// </summary>
    /// <param name="a">index of first element</param>
    /// <param name="b">index of second element</param>
    private void Swap(int a, int b)
    {
        if (a == b) return;
        if (a < 0 || a >= this.Count) throw new IndexOutOfRangeException(string.Format("The index {0} is out of not valid", a));
        if (b < 0 || b >= this.Count) throw new IndexOutOfRangeException(string.Format("The index {0} is out of not valid", b));

        var temp = this[a];
        this[a] = this[b];
        this[b] = temp;
        temp = null;
    }

}