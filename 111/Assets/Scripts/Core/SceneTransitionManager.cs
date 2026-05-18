// Assets/Scripts/Core/SceneTransitionManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("淡入淡出")]
    public CanvasGroup fadeCanvasGroup; // 全屏黑色遮罩的CanvasGroup
    public float fadeDuration = 0.3f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 带淡入淡出切换场景
    /// </summary>
    public void GoToScene(string sceneName)
    {
        StartCoroutine(TransitionCoroutine(sceneName));
    }

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        // 淡出（当前画面变黑）
        yield return StartCoroutine(Fade(0f, 1f));

        GameManager.Instance.CurrentScene = sceneName;
        SceneManager.LoadScene(sceneName);

        // 等一帧让新场景加载
        yield return null;

        // 淡入（新画面从黑变亮）
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsed = 0f;
        fadeCanvasGroup.alpha = from;
        fadeCanvasGroup.gameObject.SetActive(true);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
        if (to == 0f)
            fadeCanvasGroup.gameObject.SetActive(false);
    }
}