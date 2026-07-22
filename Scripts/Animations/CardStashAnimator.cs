using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardStashAnimator : MonoBehaviour
{
    [Header("References")]
    public RectTransform cardStash;          // Target
    [Header("Animation Settings")]
    [SerializeField] public float animationDuration = .25f;
    [SerializeField] public float delayBetweenCards = 0.03f;

    UIManager uiManager;
    void Start()
    {
        uiManager = FindAnyObjectByType<UIManager>();
    }
    public void AnimateCardsToStash(List<GameObject> cards,bool moveToAvatar = false,ulong targetClientId = ulong.MaxValue)
    {
        List<GameObject> cardsCopy = new List<GameObject>(cards);
        StartCoroutine(AnimateSequentially(cardsCopy, moveToAvatar, targetClientId));
    }


    private IEnumerator AnimateSequentially(
     List<GameObject> cards,
     bool moveToAvatar,
     ulong targetClientId = ulong.MaxValue
 )
    {
        foreach (GameObject cardObj in cards)
        {   
            RectTransform cardRT = cardObj.GetComponent<RectTransform>();
            if (cardRT != null)
                yield return StartCoroutine(
                    AnimateCard(cardRT, moveToAvatar, targetClientId)
                );

            yield return new WaitForSeconds(delayBetweenCards);
        }
    }


    private IEnumerator AnimateCard(
     RectTransform card,
     bool moveToAvatar,
     ulong? targetClientId
 )
    {
        PlaySFX playSFX=FindAnyObjectByType<PlaySFX>();
        playSFX.Discard();
        card.SetAsLastSibling();

        float elapsed = 0f;

        Vector3 startPos = card.position;
        Quaternion startRot = card.rotation;
        Vector3 startScale = card.localScale;

        // 🔥 DEFAULT TARGET (stash)
        Vector3 targetPos = cardStash.position;
        Quaternion targetRot = cardStash.rotation;
        Vector3 targetScale = Vector3.zero;

        // 🔁 OVERRIDE TARGET → avatar slot
        if (moveToAvatar)
        {
            int avatarSlotIndex = -1;

            foreach (var kvp in uiManager.uiSlotToClientId)
            {
                if (kvp.Value == targetClientId.Value)
                {
                    avatarSlotIndex = kvp.Key;
                    break;
                }
            }

            if (avatarSlotIndex != -1)
            {
                RectTransform avatarRT =
                    uiManager.avatarSlots[avatarSlotIndex].GetComponent<RectTransform>();

                targetPos = avatarRT.position;
                targetRot = avatarRT.rotation;
                targetScale = Vector3.zero; // same vanish effect
            }
            // else → silently falls back to stash
        }

        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            card.position = Vector3.Lerp(startPos, targetPos, smoothT);
            card.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            card.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

            yield return null;
        }

        card.position = targetPos;
        card.rotation = targetRot;
        card.localScale = targetScale;

        Destroy(card.gameObject);
    }

}
