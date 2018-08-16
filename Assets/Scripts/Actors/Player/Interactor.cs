using System;
using System.Collections;
using System.Collections.Generic;
using InControl;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(Collider2D))]
public class Interactor : MonoBehaviour
{
    [SerializeField] private Text promptText;
    [SerializeField] private LayerMask interactionMask;
    
    private IEnumerator m_disableTextCoroutine;

    /// <summary>
    /// Can we interacting with something at the moment. (the prompt would be visible)
    /// </summary>
    private Interactable m_currentInteractable;

    private void Awake()
    {
        if (!promptText) promptText = GameObject.Find("Interaction prompt text").GetComponent<Text>();
    }

    void Update()
    {
        if (m_currentInteractable != null)
        {
            if (CrossPlatformInputManager.GetButtonDown("Fire1") || InputManager.ActiveDevice.Action3.IsPressed)
            {
                if (m_currentInteractable.enabled)
                {
                    m_currentInteractable.OnInteract();
                    DisableText();
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        Interactable interactable = col.gameObject.GetComponent<Interactable>();
        if (interactable == null)
        {
            return;
        }
        m_currentInteractable = interactable;
        
        promptText.enabled = true;
        promptText.text = m_currentInteractable.GetPrompt();

    }
    private void OnTriggerStay2D(Collider2D col)
    {
        Interactable interactable = col.gameObject.GetComponent<Interactable>();
        if (interactable == null)
        {
            return;
        }
        m_currentInteractable = interactable;
        
        promptText.enabled = true;
        promptText.text = m_currentInteractable.GetPrompt();

        // disable the previouse coroutine, this check prevents flickering text
        if (m_disableTextCoroutine != null) StopCoroutine(m_disableTextCoroutine);
        m_disableTextCoroutine = WaitAndDisableText(3);
        StartCoroutine(m_disableTextCoroutine);
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        promptText.enabled = true;
        Interactable interactable = col.gameObject.GetComponent<Interactable>();
        if (interactable == null)
        {
            return;
        }
        m_currentInteractable = null;
        DisableText();
    }

    private IEnumerator WaitAndDisableText(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        DisableText();
    }

    private void DisableText()
    {
        promptText.text = "";
        promptText.enabled = false;
    }
}