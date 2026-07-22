using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TurnIndicator : MonoBehaviour
{
    [SerializeField] Image indicatorImage;
    [SerializeField] float duration = 5f;

    Coroutine timerRoutine;

    void Awake() 
    {
        indicatorImage.fillAmount = 0f;
        gameObject.SetActive(false);
    }

    public void StartTimer()
    {
        gameObject.SetActive(true);

        // Reset instantly
        indicatorImage.fillAmount = 1f;

        if (timerRoutine != null)
            StopCoroutine(timerRoutine);

        timerRoutine = StartCoroutine(Timer());
    }

    IEnumerator Timer()
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            indicatorImage.fillAmount = 1f - (time / duration);
            yield return null;
        }

        indicatorImage.fillAmount = 0f;
        gameObject.SetActive(false);
    }
}