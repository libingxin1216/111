using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterCard : MonoBehaviour
{
    [Header("主要人物组件（次要人物对应字段留空）")]
    public Image photoImage;
    public GameObject photoBorderGray;
    public Button nameButton;
    public Button roleButton;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI roleText;
    public Transform badgeGroup;
    public GameObject badgePrefab;

    [Header("卡片外观")]
    public Image cardBorder;
    public GameObject lockIcon;

    [Header("由 CardManager 赋值")]
    public CharacterData data;

    static readonly Color normalBorderColor = new Color(0.85f, 0.85f, 0.85f);
    static readonly Color lockedBorderColor = new Color(1f, 0.84f, 0f);

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
            nameText.text = string.IsNullOrEmpty(data.currentName)
                          ? "？？？" : data.currentName;

        if (roleText != null)
            roleText.text = string.IsNullOrEmpty(data.currentRole)
                          ? "？？？" : data.currentRole;

        if (photoImage != null)
        {
            bool hasPhoto = data.photoUnlocked && data.photo != null;
            photoImage.gameObject.SetActive(hasPhoto);
            if (photoBorderGray != null) photoBorderGray.SetActive(!hasPhoto);
            if (hasPhoto) photoImage.sprite = data.photo;
        }

        // Badge 动态填充
        if (badgeGroup != null && badgePrefab != null)
        {
            foreach (Transform child in badgeGroup)
                Destroy(child.gameObject);
            foreach (var label in data.badgeLabels)
            {
                var badge = Instantiate(badgePrefab, badgeGroup);
                var tmp = badge.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = label;
            }
        }
    }

    // 填写后只更新显示，不触发自动判定
    void OnClickNameField()
    {
        if (data.isLocked) return;
        SelectionPanel.Instance.Show(
            data.nameOptions,
            (selected) => { data.currentName = selected; RefreshDisplay(); },
            nameButton.GetComponent<RectTransform>());
    }

    void OnClickRoleField()
    {
        if (data.isLocked) return;
        SelectionPanel.Instance.Show(
            data.roleOptions,
            (selected) => { data.currentRole = selected; RefreshDisplay(); },
            roleButton.GetComponent<RectTransform>());
    }

    void OnPhotoUnlocked(object obj)
    {
        if ((string)obj != data.characterId) return;
        data.photoUnlocked = true;
        RefreshDisplay();
    }

    void OnCharacterLocked(object obj)
    {
        if ((string)obj == data.characterId)
            StartCoroutine(PlayLockAnimation());
    }

    IEnumerator PlayLockAnimation()
    {
        if (lockIcon != null) lockIcon.SetActive(true);
        float t = 0f, duration = 0.6f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cardBorder.color = Color.Lerp(
                normalBorderColor, lockedBorderColor, t / duration);
            yield return null;
        }
        cardBorder.color = lockedBorderColor;
        if (nameButton != null) nameButton.interactable = false;
        if (roleButton != null) roleButton.interactable = false;
    }
}