using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TableSlot
{
    Bottom = 0,
    Left = 1,
    Top = 2,
    Right = 3
}
public class UIManager : MonoBehaviour
{
    [SerializeField] public RectTransform[] avatarSlots;
    [SerializeField] public GameObject[] cardsOnTableShots;
    [SerializeField] public Sprite[] avatars;

    [SerializeField] public TextMeshProUGUI[] playerNames;
    [SerializeField] public Button[] abilityButtons;

    [SerializeField] public TextMeshProUGUI WinText;
    [SerializeField] public GameObject GameUI;
    [SerializeField] public TurnIndicator[] turnIndicators;
    // uiSlotIndex -> clientId
    public Dictionary<int, ulong> uiSlotToClientId = new();

    private RectTransform[] originalAvatarSlots;
    private GameObject[] originalCardSlots;
    private TurnIndicator[] originalTurnIndicators;
    private TextMeshProUGUI[] originalPlayerNames;

    private void Awake()
    {
        CacheOriginalLayout();
    }

    private void CacheOriginalLayout()
    {
        originalAvatarSlots = (RectTransform[])avatarSlots.Clone();
        originalCardSlots = (GameObject[])cardsOnTableShots.Clone();
        originalTurnIndicators = (TurnIndicator[])turnIndicators.Clone();
        originalPlayerNames = (TextMeshProUGUI[])playerNames.Clone();
    }
    private void Swap<T>(T[] array, int a, int b)
    {
        T temp = array[a];
        array[a] = array[b];
        array[b] = temp;
    }

    public void ConfigureTableLayout(int playerCount)
    {
        // Always restore first to prevent stacked swaps
        RestoreOriginalLayout();

        // Enable everything first
        for (int i = 0; i < 4; i++)
        {
            avatarSlots[i].gameObject.SetActive(true);
            cardsOnTableShots[i].SetActive(true);
            turnIndicators[i].gameObject.SetActive(true);
            playerNames[i].gameObject.SetActive(true);
        }

        if (playerCount == 2)
        {
            // Swap first
            SwapAll(1, 2);

            // Then disable 2 and 3
            DisableIndex(2);
            DisableIndex(3);
        }
        else if (playerCount == 3)
        {
            // Swap first
            SwapAll(2, 3);

            // Then disable index 2
            DisableIndex(3);
        }
        // 4 players = original layout
    }
    private void DisableIndex(int index)
    {
        avatarSlots[index].gameObject.SetActive(false);
        cardsOnTableShots[index].SetActive(false);
        turnIndicators[index].gameObject.SetActive(false);
        playerNames[index].gameObject.SetActive(false);
    }
    private void SwapAll(int a, int b)
    {
        Swap(avatarSlots, a, b);
        Swap(cardsOnTableShots, a, b);
        Swap(turnIndicators, a, b);
        Swap(playerNames, a, b);
    }
    public void RestoreOriginalLayout()
    {
        for (int i = 0; i < 4; i++)
        {
            avatarSlots[i] = originalAvatarSlots[i];
            cardsOnTableShots[i] = originalCardSlots[i];
            turnIndicators[i] = originalTurnIndicators[i];
            playerNames[i] = originalPlayerNames[i];
        }
    }
}


