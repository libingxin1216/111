using System.Collections;
using UnityEngine;

public class ContentFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    public void FadeIn()
    {
        StopAllCoroutines();
        StartCoroutine(DoFade(0f, 1f));
    }

    IEnumerator DoFade(float from, float to)
    {
        float t = 0f;
        canvasGroup.alpha = from;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / 0.2f);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}