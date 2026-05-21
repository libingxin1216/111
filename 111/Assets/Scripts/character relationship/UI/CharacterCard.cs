using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterCard : MonoBehaviour
{
    [Header("��Ҫ�����������Ҫ�����Ӧ�ֶ����գ�")]
    public Image photoImage;
    public GameObject photoBorderGray;
    public Button nameButton;
    public Button roleButton;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI roleText;
    public Transform badgeGroup;
    public GameObject badgePrefab;

    [Header("��Ƭ���")]
    public Image cardBorder;
    public GameObject lockIcon;

    [Header("�� CardManager ��ֵ")]
    public CharacterData data;

    static readonly Color normalBorderColor = new Color(0.85f, 0.85f, 0.85f);
    static readonly Color lockedBorderColor = new Color(1f, 0.84f, 0f);

    // ── 运行时解锁的照片（不修改 ScriptableObject，跨场景由 GameManager 恢复）
    private Sprite _runtimePhoto;

    void Start()
    {
        // 场景每次加载时，从 GameManager 恢复跨场景解锁的照片
        TryLoadPhotoFromGameManager();
        RefreshDisplay();
        nameButton?.onClick.AddListener(OnClickNameField);
        roleButton?.onClick.AddListener(OnClickRoleField);
        EventBus.On("OnPhotoUnlocked", OnPhotoUnlocked);
        EventBus.On("OnCharacterLocked", OnCharacterLocked);
    }

    /// <summary>
    /// 从 GameManager.UnlockedPhotos 读取照片并存入运行时变量。
    /// 跨场景时 GameManager 持久，CharacterCard 每次 Start 时调用此方法，
    /// 确保即使事件在其他场景发出也不会丢失。
    /// </summary>
    void TryLoadPhotoFromGameManager()
    {
        if (data == null || GameManager.Instance == null) return;
        if (GameManager.Instance.UnlockedPhotos.TryGetValue(data.characterId, out var photo)
            && photo != null)
        {
            _runtimePhoto      = photo;
            data.photoUnlocked = true;   // 标记已解锁，供 RefreshDisplay 判断
        }
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
                          ? "������" : data.currentName;

        if (roleText != null)
            roleText.text = string.IsNullOrEmpty(data.currentRole)
                          ? "������" : data.currentRole;

        if (photoImage != null)
        {
            // 优先使用运行时动态解锁的照片（来自点击人脸热区），
            // 其次使用 ScriptableObject 中预先配置的照片
            var displayPhoto = _runtimePhoto != null ? _runtimePhoto : data.photo;
            bool hasPhoto    = data.photoUnlocked && displayPhoto != null;

            photoImage.gameObject.SetActive(hasPhoto);
            if (photoBorderGray != null) photoBorderGray.SetActive(!hasPhoto);
            if (hasPhoto) photoImage.sprite = displayPhoto;
        }

        // Badge ��̬���
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

    // ��д��ֻ������ʾ���������Զ��ж�
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
        // 同时从 GameManager 取回实际的 Sprite，不依赖 ScriptableObject 预设
        TryLoadPhotoFromGameManager();
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