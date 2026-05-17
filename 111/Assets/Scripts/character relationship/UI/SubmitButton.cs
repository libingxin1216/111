using UnityEngine;
using UnityEngine.UI;

public class SubmitButton : MonoBehaviour
{
    [SerializeField] private Button button;

    void Start()
    {
        button?.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        LockSystem.Instance.OnSubmitClicked();
    }
}