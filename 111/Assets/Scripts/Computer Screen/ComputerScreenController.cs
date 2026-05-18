using UnityEngine;

public class ComputerScreenController : MonoBehaviour
{
    public static ComputerScreenController Instance;
    [SerializeField] private GameObject navigationBar;

    void Awake()
    {
        Instance = this;
       // gameObject.SetActive(false);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        navigationBar.SetActive(false);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        navigationBar.SetActive(true);
    }
}