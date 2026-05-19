using UnityEngine;
using UnityEngine.SceneManagement;

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
        SceneManager.LoadScene("MainScene");
    }
}