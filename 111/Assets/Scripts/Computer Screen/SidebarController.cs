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
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == index);
            highlights[i].SetActive(i == index);
            icons[i].color = i == index
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        fader.FadeIn();
    }
}