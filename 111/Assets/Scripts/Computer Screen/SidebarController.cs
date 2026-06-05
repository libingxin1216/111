using UnityEngine;
using UnityEngine.UI;

public class SidebarController : MonoBehaviour
{
    [SerializeField] private GameObject[] panels;
    [SerializeField] private GameObject[] highlights;
    [SerializeField] private Image[] icons;
    [SerializeField] private ContentFader fader;

   // void Start() => SwitchTo(0);

    public void SwitchTo(int index)
    {
        if (panels == null || panels.Length == 0) return;

        for (int i = 0; i < panels.Length; i++)
        {
            // 面板显隐（不依赖其他数组）
            if (panels[i] != null)
                panels[i].SetActive(i == index);

            // 高亮（数组长度可能与 panels 不同）
            if (highlights != null && i < highlights.Length && highlights[i] != null)
                highlights[i].SetActive(i == index);

            // 图标颜色（Inspector 中未赋值时为 null，跳过即可）
            // 注意：此处若不做 null 检查，NullReferenceException 会在 i=0 处中断循环，
            //       导致后续 panels[i].SetActive(true) 永远不被执行 → Population/Transport 面板不显示
            if (icons != null && i < icons.Length && icons[i] != null)
                icons[i].color = i == index
                    ? Color.white
                    : new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        if (fader != null)
            fader.FadeIn();
    }
}