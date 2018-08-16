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

    private AudioSource m_audioSource;
    private Collider2D m_collider2D;
    private SpriteRenderer m_spriteRenderer;
    private Color m_originalColor;


    // Use this for initialization
    private void Awake()
    {
        m_collider2D = GetComponent<Collider2D>();

        m_audioSource = GetComponent<AudioSource>();
        m_audioSource.playOnAwake = false;


        if (SpawnTransform == null) SpawnTransform = transform.Find("Spawn Position");
        if (SpawnTransform == null) SpawnTransform = transform;

        m_spriteRenderer = GetComponent<SpriteRenderer>();
        if (!m_spriteRenderer) m_spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        m_originalColor = m_spriteRenderer.color;
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
        m_audioSource.Play();
        DisableTemporarily();
    }

    /// <summary> disables the portal for 1 second (disables collider and changes color to dark grey) </summary>
    private void DisableTemporarily(int time = 2)
    {
        m_collider2D.enabled = false;
        m_spriteRenderer.color = new Color(0.2f, 0.2f, 0.2f, m_originalColor.a);
        Invoke("ReEnable", time);
    }

    /// <summary> enables the portal (enables collider and changes color to original) </summary>
    void ReEnable()
    {
        m_spriteRenderer.color = m_originalColor;
        m_collider2D.enabled = true;
    }
}