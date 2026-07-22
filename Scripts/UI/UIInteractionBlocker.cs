using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIInteractionBlocker : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    public void Block()
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;
    }

    public void Unblock()
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = true;
    }
}
