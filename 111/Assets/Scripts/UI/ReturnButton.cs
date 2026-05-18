// Assets/Scripts/UI/ReturnButton.cs
using UnityEngine;
using UnityEngine.UI;

public class ReturnButton : MonoBehaviour
{
    public Button returnBtn;

    void Start()
    {
        returnBtn.onClick.AddListener(() =>
        {
            SceneTransitionManager.Instance.GoToScene("MainScene");
        });
    }
}