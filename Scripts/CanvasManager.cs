using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    [SerializeField] Button currentBtn;
    [SerializeField] Sprite activeTabSprite;
    [SerializeField] Sprite inactiveTabSprite;
    public void DisableCanvas(Canvas fromCanvas)
    {
        fromCanvas.gameObject.SetActive(false);
    }
    public void EnableCanvas(Canvas toCanvas)
    {
        toCanvas.gameObject.SetActive(true);
    }

    public void ActivateShopTabs(Button btn)
    {
        if (btn == currentBtn)
            return;
        currentBtn.image.sprite=inactiveTabSprite;
        currentBtn = btn;
        btn.image.sprite = activeTabSprite;
    }
}
