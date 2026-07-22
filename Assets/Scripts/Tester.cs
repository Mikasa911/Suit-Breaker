using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tester : MonoBehaviour
{
    [SerializeField]float cardWidth = 228f;
    [SerializeField]float cardHeight = 324f;
    [SerializeField]float cardSpacing = 60f;
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Transform myDeckPivotPoint;

    void Start()
    {
        Show();
    }

    // Update is called once per frame
    void Show()
    {
        float totalWidth = (20 - 1) * cardSpacing;
        float startX = -totalWidth / 2f;
        for (int i = 0; i < 20; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, myDeckPivotPoint);
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(startX + i * cardSpacing, 0);
            rt.sizeDelta = new Vector2(cardWidth, cardHeight);
            rt.SetSiblingIndex(i);
        }
    }
}
