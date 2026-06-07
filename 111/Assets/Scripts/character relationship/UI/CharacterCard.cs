using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterCard : MonoBehaviour
{
    [Header("需要在Prefab上分配（需要填写对应字段不为空）")]
    public Image photoImage;
    public GameObject photoBorderGray;
    public Button nameButton;
    public Button roleButton;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI roleText;
    public Transform badgeGroup;
    public GameObject badgePrefab;

    [Header("卡片边框")]
    public Image cardBorder;
    public GameObject lockIcon;

    [Header("由 CardManager 赋值")]
    public CharacterData data;

    static readonly Color normalBorderColor = new Color(0.85f, 0.85f, 0.85f);
    static readonly Color lockedBorderColor = new Color(1f, 0.84f, 0f);

    // 运行时照片（不修改 ScriptableObject，跨场景由 GameManager 恢复）
    private Sprite _runtimePhoto;

    // ════════════════════════════════════════════════════════════════════
    //  Unity 生命周期
    // ════════════════════════════════════════════════════════════════════

    void Start()
    {
        // 1. 优先从 GameManager 恢复跨场景持久化的状态（覆盖 ScriptableObject 默认值）
        RestoreFromGameManager();

        // 2. 若 currentName 仍为空，用 prefilledName 填入
        if (data != null
            && string.IsNullOrEmpty(data.currentName)
            && !string.IsNullOrEmpty(data.prefilledName))
        {
            data.currentName = data.prefilledName;
            SaveToGameManager();
        }

        // 3. 恢复照片
        TryLoadPhotoFromGameManager();

        // 4. 字体 + 显示
        ApplyChineseFont();
        RefreshDisplay();

        // 5. 事件绑定
        nameButton?.onClick.AddListener(OnClickNameField);
        roleButton?.onClick.AddListener(OnClickRoleField);
        EventBus.On("OnPhotoUnlocked",    OnPhotoUnlocked);
        EventBus.On("OnCharacterLocked",  OnCharacterLocked);
    }

    void OnDestroy()
    {
        EventBus.Off("OnPhotoUnlocked",   OnPhotoUnlocked);
        EventBus.Off("OnCharacterLocked", OnCharacterLocked);
    }

    // ════════════════════════════════════════════════════════════════════
    //  跨场景持久化：存 / 取
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 从 GameManager.CharacterProgressMap 读取并恢复人物卡片状态。
    /// 在 Start() 最开始调用，保证切换场景后数据不丢失。
    /// </summary>
    void RestoreFromGameManager()
    {
        if (GameManager.Instance == null || data == null) return;
        if (!GameManager.Instance.CharacterProgressMap
                .TryGetValue(data.characterId, out var progress)) return;

        // 恢复姓名
        if (!string.IsNullOrEmpty(progress.FilledName))
            data.currentName = progress.FilledName;

        // 恢复身份/角色
        if (!string.IsNullOrEmpty(progress.FilledIdentity))
            data.currentRole = progress.FilledIdentity;

        // 恢复锁定状态
        if (progress.IsLocked && !data.isLocked)
        {
            data.isLocked = true;
            // 直接应用锁定外观（无需协程动画，直接到达最终状态）
            if (cardBorder != null) cardBorder.color = lockedBorderColor;
            if (lockIcon   != null) lockIcon.SetActive(true);
            if (nameButton != null) nameButton.interactable = false;
            if (roleButton != null) roleButton.interactable = false;
        }
    }

    /// <summary>
    /// 将当前状态写入 GameManager.CharacterProgressMap（DontDestroyOnLoad）。
    /// 在每次状态变化时调用，确保数据即时持久化。
    /// </summary>
    void SaveToGameManager()
    {
        if (GameManager.Instance == null || data == null) return;
        GameManager.Instance.SaveCharacterProgress(
            data.characterId,
            data.currentName,
            data.currentRole,
            data.isLocked);
    }

    // ════════════════════════════════════════════════════════════════════
    //  照片
    // ════════════════════════════════════════════════════════════════════

    void TryLoadPhotoFromGameManager()
    {
        if (data == null || GameManager.Instance == null) return;
        if (GameManager.Instance.UnlockedPhotos.TryGetValue(data.characterId, out var photo)
            && photo != null)
        {
            _runtimePhoto      = photo;
            data.photoUnlocked = true;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  字体
    // ════════════════════════════════════════════════════════════════════

    void ApplyChineseFont()
    {
        var font = GameManager.GetChineseFont();
        if (font == null) return;
        if (nameText != null) nameText.font = font;
        if (roleText != null) roleText.font = font;
        if (badgeGroup != null)
            foreach (var tmp in badgeGroup.GetComponentsInChildren<TextMeshProUGUI>(true))
                tmp.font = font;
    }

    // ════════════════════════════════════════════════════════════════════
    //  显示刷新
    // ════════════════════════════════════════════════════════════════════

    public void RefreshDisplay()
    {
        if (nameText != null)
        {
            string displayName = !string.IsNullOrEmpty(data.currentName)
                               ? data.currentName
                               : (!string.IsNullOrEmpty(data.prefilledName)
                                  ? data.prefilledName
                                  : "???");
            nameText.text = displayName;
        }

        if (roleText != null)
        {
            roleText.text = string.IsNullOrEmpty(data.currentRole)
                          ? "???"
                          : data.currentRole;
        }

        if (photoImage != null)
        {
            var  displayPhoto = _runtimePhoto != null ? _runtimePhoto : data.photo;
            // 次要人物直接显示配置好的头像；主要人物需要解锁后才显示
            bool hasPhoto = displayPhoto != null &&
                            (data.photoUnlocked || data.characterType == CharacterType.Minor);
            photoImage.gameObject.SetActive(hasPhoto);
            if (photoBorderGray != null) photoBorderGray.SetActive(!hasPhoto);
            if (hasPhoto) photoImage.sprite = displayPhoto;
        }

        // Badge 动态创建
        if (badgeGroup != null && badgePrefab != null)
        {
            foreach (Transform child in badgeGroup) Destroy(child.gameObject);
            var font = GameManager.GetChineseFont();
            foreach (var label in data.badgeLabels)
            {
                var badge = Instantiate(badgePrefab, badgeGroup);
                var tmp   = badge.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = label;
                    if (font != null) tmp.font = font;
                }
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  玩家交互：填写姓名 / 角色
    // ════════════════════════════════════════════════════════════════════

    void OnClickNameField()
    {
        if (data.isLocked) return;
        SelectionPanel.Instance.Show(
            data.nameOptions,
            (selected) =>
            {
                data.currentName = selected;
                SaveToGameManager();   // ← 立即持久化
                RefreshDisplay();
            },
            nameButton.GetComponent<RectTransform>());
    }

    void OnClickRoleField()
    {
        if (data.isLocked) return;
        SelectionPanel.Instance.Show(
            data.roleOptions,
            (selected) =>
            {
                data.currentRole = selected;
                SaveToGameManager();   // ← 立即持久化
                RefreshDisplay();
            },
            roleButton.GetComponent<RectTransform>());
    }

    // ════════════════════════════════════════════════════════════════════
    //  事件处理
    // ════════════════════════════════════════════════════════════════════

    void OnPhotoUnlocked(object obj)
    {
        if ((string)obj != data.characterId) return;
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
            if (cardBorder != null)
                cardBorder.color = Color.Lerp(normalBorderColor, lockedBorderColor, t / duration);
            yield return null;
        }
        if (cardBorder != null) cardBorder.color = lockedBorderColor;
        if (nameButton  != null) nameButton.interactable = false;
        if (roleButton  != null) roleButton.interactable = false;

        // 锁定完成后持久化（isLocked 由 LockSystem 设置，这里同步到 GameManager）
        SaveToGameManager();
    }
}
