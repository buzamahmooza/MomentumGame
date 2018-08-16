using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// the PortalEditor will be in charge of putting different sprites for each teleporter,
/// as well as assigning the OtherTeleporter for each script.
/// Note: Portals should be facing right
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [SerializeField] public Portal OtherPortal;

    /// <summary>
    /// the position where an object will apear if it arrives to this teleporter
    /// </summary>
    public Transform SpawnTransform;

    private AudioSource _audioSource;
    private Collider2D _collider2D;
    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;


    // Use this for initialization
    private void Awake()
    {
        _collider2D = GetComponent<Collider2D>();

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;


        if (SpawnTransform == null) SpawnTransform = transform.Find("Spawn Position");
        if (SpawnTransform == null) SpawnTransform = transform;

        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (!_spriteRenderer) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        _originalColor = _spriteRenderer.color;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        bool teleportCondition = !col.isTrigger &&
                                 col.attachedRigidbody && col.attachedRigidbody.bodyType != RigidbodyType2D.Static &&
                                 Utils.IsInLayerMask(LayerMask.GetMask("Object", "Player", "Enemy", "Default"),
                                     col.gameObject.layer);

        if (teleportCondition)
        {
            Debug.Log("Teleported " + col.name);
            OtherPortal.TeleportObject(col);
            this.DisableTemporarily();
        }
    }

    /// <summary>
    /// takes the object and makes it appear at this portal
    /// </summary>
    /// <param name="col"></param>
    private void TeleportObject(Collider2D col)
    {
        col.gameObject.transform.position = this.SpawnTransform.position;
        _audioSource.Play();
        DisableTemporarily();
    }

    /// <summary> disables the portal for 1 second (disables collider and changes color to dark grey) </summary>
    public void DisableTemporarily(int time = 2)
    {
        _collider2D.enabled = false;
        _spriteRenderer.color = new Color(0.2f, 0.2f, 0.2f, _originalColor.a);
        Invoke("ReEnable", time);
    }

    /// <summary> enables the portal (enables collider and changes color to original) </summary>
    void ReEnable()
    {
        _spriteRenderer.color = _originalColor;
        _collider2D.enabled = true;
    }
}