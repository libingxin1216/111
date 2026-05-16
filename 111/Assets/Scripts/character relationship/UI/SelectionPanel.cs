using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectionPanel : MonoBehaviour
{
    public static SelectionPanel Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private Transform optionContainer;
    [SerializeField] private GameObject optionButtonPrefab;
    [SerializeField] private Button closeOverlay; // 全屏透明遮罩

    private Action<string> currentCallback;
    private List<GameObject> spawnedButtons = new();

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
        closeOverlay?.onClick.AddListener(Hide);
    }

    public void Show(string[] options, Action<string> callback, RectTransform anchor)
    {
        currentCallback = callback;

        // 清空旧按钮
        foreach (var b in spawnedButtons) Destroy(b);
        spawnedButtons.Clear();

        // 生成新按钮
        foreach (var opt in options)
        {
            var btnGo = Instantiate(optionButtonPrefab, optionContainer);
            btnGo.GetComponentInChildren<TextMeshProUGUI>().text = opt;
            string captured = opt;
            btnGo.GetComponent<Button>().onClick.AddListener(() => {
                currentCallback?.Invoke(captured);
                Hide();
            });
            spawnedButtons.Add(btnGo);
        }

        // 定位面板到锚点附近
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.position = anchor.position + new Vector3(0, -anchor.rect.height, 0);

        panel.SetActive(true);
        closeOverlay.gameObject.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
        closeOverlay.gameObject.SetActive(false);
    }
}