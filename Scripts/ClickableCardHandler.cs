using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ClickableCardHandler : MonoBehaviour, IPointerClickHandler
{
    public enum CardStates
    {
        unplayable, playable, disabled, selected
    };
    [Header("Card Visuals")]
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color playableColor = Color.white;
    [SerializeField] private Color unplayableColor = Color.black;
    [SerializeField] private Color[] cardColors;
    [SerializeField] public UnityEngine.UI.Image AbilityFilter;



    Color lastColor = Color.white;
    public CardStates myCardState;
    private UnityEngine.UI.Image cardImage;
    private static ClickableCardHandler currentlySelected;
    public int cardId;
    public int cardSuit;
    public bool cardIsUnplayable = false;
    bool cardIsDisabled = false;
    private PlayerBehavior ownerPlayer;

    public static int CurrentSelectedCardId { get; private set; } = -1;
    public static System.Action<int, bool> OnCardSelected;



    private void Awake()
    {
        cardImage = GetComponent<UnityEngine.UI.Image>();
        Deselect();
    }

    /// <summary>
    /// Called when card is spawned.
    /// </summary>
    /// <param name="id">Card ID</param>
    /// <param name="player">Owning player</param>
    public void Initialize(int id, PlayerBehavior player)
    {
        cardId = id;
        cardSuit = MyUtilities.GetSuitIndex(id);
        ownerPlayer = player;
    }
    public void SetAbility(string ability)
    {
        Sprite sprite = MyUtilities.GetAbilitySprite(ability);

        AbilityFilter.sprite = sprite;
        AbilityFilter.rectTransform.sizeDelta = new Vector2(200f, 200f);

        // ✅ Opacity control
        Color c = AbilityFilter.color;
        c.a = sprite != null ? 1f : 0f;   // 100% or 0%
        AbilityFilter.color = c;
    }
    public void MakeCardUnplayable()
    {
        myCardState = CardStates.unplayable;
        cardImage.color = unplayableColor;
        cardIsUnplayable = true;
    }
    public void MakeCardPlayable()
    {
        myCardState = CardStates.playable;
        cardImage.color = playableColor;
        cardIsUnplayable = false;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        {
            if (currentlySelected == this)
            {
                Deselect();
                currentlySelected = null;
                CurrentSelectedCardId = -1;

                OnCardSelected?.Invoke(-1, false);

                if (ownerPlayer != null) ownerPlayer.SetSelectedCardId(-1);
            }
            else
            {
                if (currentlySelected != null)
                    currentlySelected.Deselect();

                Select();
                currentlySelected = this;
                CurrentSelectedCardId = cardId;

                OnCardSelected?.Invoke(cardId, cardIsUnplayable);

                if (ownerPlayer != null) ownerPlayer.SetSelectedCardId(cardId);
            }
        }

    }
    public void ChangeState(int state)
    {
        myCardState = (CardStates)state;
        ChangeCardColor();
    }
    public void ChangeCardColor()
    {
        Color currentColor;
        switch ((int)myCardState)
        {
            case 0:
                currentColor = cardColors[0];
                break;
            case 1:
                currentColor = cardColors[1];
                currentlySelected = null;
                break;
            case 2:
                currentColor = cardColors[2];
                break;
            default:
                currentColor = cardColors[3];
                currentlySelected = this;
                break;

        }
        cardImage.color = currentColor;
    }
    void Select()
    {
        lastColor=cardImage.color;
        cardImage.color=selectedColor;
    }

    private void Deselect() => cardImage.color = lastColor;
}
