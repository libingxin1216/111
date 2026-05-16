using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterCard : MonoBehaviour
{
    [Header("主要人物组件（次要人物可为空）")]
    public Image photoImage;
    public GameObject photoBorderGray;  // 灰色虚线框（未填入时显示）
    public Button nameButton;
    public Button roleButton;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI roleText;
    public Transform badgeGroup;
    public GameObject badgePrefab;

    [Header("卡片边框")]
    public Image cardBorder;
    public GameObject lockIcon;

    [Header("数据")]
    public CharacterData data;

    private static readonly Color normalBorderColor = new Color(0.85f, 0.85f, 0.85f);
    private static readonly Color lockedBorderColor = new Color(1f, 0.84f, 0f); // 金色

    void Start()
    {
        RefreshDisplay();
        nameButton?.onClick.AddListener(OnClickNameField);
        roleButton?.onClick.AddListener(OnClickRoleField);

        EventBus.On("OnPhotoUnlocked", OnPhotoUnlocked);
        EventBus.On("OnCharacterLocked", OnCharacterLocked);
    }

    void OnDestroy()
    {
        EventBus.Off("OnPhotoUnlocked", OnPhotoUnlocked);
        EventBus.Off("OnCharacterLocked", OnCharacterLocked);
    }

    public void RefreshDisplay()
    {
        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(data.currentName) ? "？？？" : data.currentName;

        if (roleText != null)
            roleText.text = string.IsNullOrEmpty(data.currentRole) ? "？？？" : data.currentRole;

        if (photoImage != null)
        {
            bool hasPhoto = data.photoUnlocked && data.photo != null;
            photoImage.gameObject.SetActive(hasPhoto);
            if (photoBorderGray != null)
                photoBorderGray.SetActive(!hasPhoto);
            if (hasPhoto) photoImage.sprite = data.photo;
        }
    }

    void OnClickNameField()
    {
        if (data.isLocked) return;
        SelectionPanel.Instance.Show(
            data.nameOptions,
            (selected) => {
                data.currentName = selected;
                RefreshDisplay();
                LockSystem.Instance.TryCheckAlignment(data.characterId);
            },
            nameButton.GetComponent<RectTransform>()
        );
    }

    void OnClickRoleField()
    {
        if (data.isLocked) return;
        SelectionPanel.Instance.Show(
            data.roleOptions,
            (selected) => {
                data.currentRole = selected;
                RefreshDisplay();
                LockSystem.Instance.TryCheckAlignment(data.characterId);
            },
            roleButton.GetComponent<RectTransform>()
        );
    }

    void OnPhotoUnlocked(object obj)
    {
        string id = obj as string;
        if (id == data.characterId)
        {
            data.photoUnlocked = true;
            RefreshDisplay();
            LockSystem.Instance.TryCheckAlignment(data.characterId);
        }
    }

    void OnCharacterLocked(object obj)
    {
        string id = obj as string;
        if (id == data.characterId)
            StartCoroutine(PlayLockAnimation());
    }

    IEnumerator PlayLockAnimation()
    {
        if (lockIcon != null) lockIcon.SetActive(true);
        float t = 0f;
        float duration = 0.6f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cardBorder.color = Color.Lerp(normalBorderColor, lockedBorderColor, t / duration);
            yield return null;
        }
        cardBorder.color = lockedBorderColor;

        // 禁用按钮
        if (nameButton != null) nameButton.interactable = false;
        if (roleButton != null) roleButton.interactable = false;
    }
}