using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeSpriteSwap : MonoBehaviour
{
    [SerializeField] public Image currentImage;
    [SerializeField] public Image nextImage;
    [SerializeField] public Sprite[] BGs;

    public float fadeDuration = 0.25f;
    bool isFading;

    public void ChangeSprite(int suit)
    {
        Sprite newSprite;
        if (suit == -1)
            newSprite = BGs[BGs.Length - 1];
        else
            newSprite = BGs[suit];
        if (isFading) return;
        StartCoroutine(FadeTransition(newSprite));
    }

    IEnumerator FadeTransition(Sprite newSprite)
    {
        isFading = true;

        nextImage.sprite = newSprite;
        nextImage.color = new Color(1, 1, 1, 0);
        nextImage.gameObject.SetActive(true);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;

            currentImage.color = new Color(1, 1, 1, 1 - t);
            nextImage.color = new Color(1, 1, 1, t);

            yield return null;
        }

        // Finalize
        currentImage.sprite = newSprite;
        currentImage.color = Color.white;

        nextImage.gameObject.SetActive(false);
        isFading = false;
    }
}
