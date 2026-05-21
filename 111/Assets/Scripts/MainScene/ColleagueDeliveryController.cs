using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// 同事送达线索 —— 完整动画流程控制器
///
/// ── 使用方式 ────────────────────────────────────────────────────────
/// 挂载到 MainScene 任意持久 GameObject（推荐和 MainSceneController 同节点）。
/// 调用 TriggerDelivery(clueId, dialogueLines, onComplete) 启动流程。
///
/// ── 流程顺序 ──────────────────────────────────────────────────────────
///  1. 门缝出现光效（doorCrackLight 显示 + 闪烁动画）
///  2. 播放敲门音效 3 次
///  3. 激活门按钮，等待玩家点击
///  4. 门开动画（doorAnimator → "DoorOpen" Trigger）
///  5. 同事角色从右侧滑入
///  6. 气泡显示台词（可多句，点击继续）
///  7. 最后一句后显示确认按钮
///  8. 玩家点击确认 → 同事滑出，门关
///  9. ClueSystem 解锁线索，GameManager 更新红点
/// 10. 显示绿色通知条，文件夹图标出现红点
///
/// ── Inspector 必填 ────────────────────────────────────────────────────
///   doorCrackLight       门缝透光 Image（建议用渐变光效 Sprite）
///   doorButton           门的 Button 组件
///   doorAnimator         门的 Animator（需含 Trigger：DoorOpen / DoorClose）
///   colleagueOverlay     全屏半透明叠加层 Panel
///   colleagueSprite      同事人物 Image
///   speechBubble         对话气泡 GameObject（含 dialogueText TMP）
///   dialogueText         对话文字 TMP_Text
///   confirmButton        "好的，谢谢" 确认 Button
///   folderBadge          桌面文件夹图标上的红点 GameObject
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class ColleagueDeliveryController : MonoBehaviour
{
    public static ColleagueDeliveryController Instance { get; private set; }

    // ── 门 ────────────────────────────────────────────────────────────
    [Header("门")]
    [Tooltip("门缝透光 Image，启用后自动播放闪烁")]
    public GameObject doorCrackLight;

    [Tooltip("门的 Button（平时不可交互，流程触发后激活）")]
    public Button doorButton;

    [Tooltip("门的 Animator，需含 Trigger：DoorOpen / DoorClose")]
    public Animator doorAnimator;

    // ── 音效 ──────────────────────────────────────────────────────────
    [Header("音效")]
    public AudioSource audioSource;
    public AudioClip   knockClip;

    // ── 同事场景叠加层 ────────────────────────────────────────────────
    [Header("同事叠加层")]
    [Tooltip("全屏半透明遮罩 Panel")]
    public GameObject colleagueOverlay;

    [Tooltip("同事人物图（Image 组件）")]
    public Image colleagueSprite;

    [Tooltip("对话气泡 GameObject（含 dialogueText 子节点）")]
    public GameObject speechBubble;

    [Tooltip("对话内容 TMP 文字")]
    public TMP_Text dialogueText;

    
    public Button confirmButton;

    [Tooltip("点击对话区域（非气泡）继续下一句的透明 Button，可选")]
    public Button overlayClickArea;

    // ── 桌面红点 ──────────────────────────────────────────────────────
    [Header("桌面文件夹红点")]
    public GameObject folderBadge;

    // ── 动画参数 ──────────────────────────────────────────────────────
    [Header("动画参数")]
    public float slideInDuration  = 0.45f;
    public float slideOutDuration = 0.35f;
    public Vector2 charHiddenPos  = new Vector2(700, -160);
    public Vector2 charShownPos   = new Vector2(160, -160);

    // ── 私有状态 ──────────────────────────────────────────────────────
    private string   _pendingClueId;
    private string[] _dialogueLines;
    private int      _lineIndex;
    private Action   _onComplete;
    private Coroutine _crackFlicker;

    // ════════════════════════════════════════════════════════════════════
    //  Unity 生命周期
    // ════════════════════════════════════════════════════════════════════
    void Awake()
    {
        Instance = this;
        if (doorCrackLight   != null) doorCrackLight.SetActive(false);
        if (colleagueOverlay != null) colleagueOverlay.SetActive(false);
        if (speechBubble     != null) speechBubble.SetActive(false);
        if (confirmButton    != null) confirmButton.gameObject.SetActive(false);
        if (overlayClickArea != null) overlayClickArea.gameObject.SetActive(false);
        if (folderBadge      != null) folderBadge.SetActive(false);

        // confirmButton 监听器只注册一次，不会因 SetActive 而丢失
        confirmButton?.onClick.AddListener(OnConfirmClicked);

        // overlayClickArea 的监听器在 ShowCurrentLine 内动态管理，
        // 这里只注册，激活/禁用由流程控制
        overlayClickArea?.onClick.AddListener(OnOverlayClicked);

        // 若没有配置 overlayClickArea，则让 speechBubble 本身可点击
        if (overlayClickArea == null && speechBubble != null)
        {
            var btn = speechBubble.GetComponent<Button>()
                   ?? speechBubble.AddComponent<Button>();
            // 移除旧监听，防止重复
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnOverlayClicked);
        }
    }

    /// <summary>点击对话区域/气泡时：推进台词或忽略（已到末尾时不处理）</summary>
    void OnOverlayClicked()
    {
        // 只在仍有台词未播完时才推进；确认按钮阶段不响应
        if (_lineIndex < (_dialogueLines != null ? _dialogueLines.Length : 0))
            ShowNextLine();
    }

    // ════════════════════════════════════════════════════════════════════
    //  公共触发接口
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 启动同事送达流程。
    /// </summary>
    /// <param name="clueId">要解锁的线索 ID</param>
    /// <param name="lines">同事台词数组（至少 1 句）</param>
    /// <param name="onComplete">流程结束后的回调（可为 null）</param>
    public void TriggerDelivery(string clueId, string[] lines = null, Action onComplete = null)
    {
        _pendingClueId = clueId;
        _dialogueLines = (lines != null && lines.Length > 0)
                       ? lines
                       : new[] { "有你的文件，请查收。" };
        _lineIndex    = 0;
        _onComplete   = onComplete;
        StartCoroutine(DeliverySequence());
    }

    // ════════════════════════════════════════════════════════════════════
    //  主流程协程
    // ════════════════════════════════════════════════════════════════════
    IEnumerator DeliverySequence()
    {
        // ── 步骤 1：门缝光效 ──────────────────────────────────────────
        if (doorCrackLight != null)
        {
            doorCrackLight.SetActive(true);
            _crackFlicker = StartCoroutine(FlickerCrackLight());
        }

        // ── 步骤 2：敲门音效 3 次 ─────────────────────────────────────
        yield return PlayKnocks(3, 0.38f);

        // ── 步骤 3：激活门按钮 ────────────────────────────────────────
        if (doorButton != null)
        {
            doorButton.interactable = true;
            doorButton.onClick.RemoveAllListeners();
            doorButton.onClick.AddListener(OnDoorClicked);
        }
    }

    void OnDoorClicked()
    {
        if (doorButton != null)
        {
            doorButton.interactable = false;
            doorButton.onClick.RemoveAllListeners();
        }
        // 关闭光效
        if (_crackFlicker != null) StopCoroutine(_crackFlicker);
        if (doorCrackLight != null) doorCrackLight.SetActive(false);

        StartCoroutine(EnterColleague());
    }

    IEnumerator EnterColleague()
    {
        // ── 步骤 4：门开动画 ──────────────────────────────────────────
        if (doorAnimator != null) doorAnimator.SetTrigger("DoorOpen");
        yield return new WaitForSeconds(0.5f);

        // ── 步骤 5：叠加层显示，同事滑入 ─────────────────────────────
        if (colleagueOverlay != null) colleagueOverlay.SetActive(true);
        if (speechBubble     != null) speechBubble.SetActive(false);
        if (confirmButton    != null) confirmButton.gameObject.SetActive(false);
        if (overlayClickArea != null) overlayClickArea.gameObject.SetActive(false);

        if (colleagueSprite != null)
        {
            colleagueSprite.rectTransform.anchoredPosition = charHiddenPos;
            yield return SlideChar(charHiddenPos, charShownPos, slideInDuration);
        }

        yield return new WaitForSeconds(0.2f);

        // ── 步骤 6：显示第一句台词 ────────────────────────────────────
        _lineIndex = 0;
        ShowCurrentLine();
    }

    // ════════════════════════════════════════════════════════════════════
    //  台词逻辑
    // ════════════════════════════════════════════════════════════════════
    void ShowCurrentLine()
    {
        int total = _dialogueLines != null ? _dialogueLines.Length : 0;

        if (_lineIndex >= total)
        {
            // ── 台词播完：切换到确认按钮 ─────────────────────────────
            // 1. 隐藏气泡和点击继续区域，防止它们拦截 confirmButton 的点击
            if (speechBubble     != null) speechBubble.SetActive(false);
            if (overlayClickArea != null) overlayClickArea.gameObject.SetActive(false);

            // 2. 显示确认按钮
            if (confirmButton != null) confirmButton.gameObject.SetActive(true);
            return;
        }

        // ── 正常台词行 ────────────────────────────────────────────────
        if (dialogueText != null)
            dialogueText.text = _dialogueLines[_lineIndex];

        // 气泡可见、点击区域可见、确认按钮隐藏
        if (speechBubble     != null) speechBubble.SetActive(true);
        if (overlayClickArea != null) overlayClickArea.gameObject.SetActive(true);
        if (confirmButton    != null) confirmButton.gameObject.SetActive(false);
    }

    void ShowNextLine()
    {
        // 防越界：只在还有台词时才推进
        int total = _dialogueLines != null ? _dialogueLines.Length : 0;
        if (_lineIndex < total)
        {
            _lineIndex++;
            ShowCurrentLine();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  确认 → 结束
    // ════════════════════════════════════════════════════════════════════
    void OnConfirmClicked()
    {
        StartCoroutine(FinishDelivery());
    }

    IEnumerator FinishDelivery()
    {
        // ── 隐藏所有对话 UI ───────────────────────────────────────────
        if (confirmButton    != null) confirmButton.gameObject.SetActive(false);
        if (speechBubble     != null) speechBubble.SetActive(false);
        if (overlayClickArea != null) overlayClickArea.gameObject.SetActive(false);

        // ── 同事滑出 ──────────────────────────────────────────────────
        if (colleagueSprite != null)
            yield return SlideChar(charShownPos, charHiddenPos, slideOutDuration);

        // 叠加层全部关闭
        if (colleagueOverlay != null) colleagueOverlay.SetActive(false);

        // ── 门关动画 ──────────────────────────────────────────────────
        if (doorAnimator != null) doorAnimator.SetTrigger("DoorClose");
        yield return new WaitForSeconds(0.3f);

        // ── 解锁线索 ──────────────────────────────────────────────────
        if (!string.IsNullOrEmpty(_pendingClueId))
        {
            ClueSystem.Instance?.UnlockClue(_pendingClueId);
            GameManager.Instance?.AddClue(_pendingClueId);
        }

        // ── 通知条 + 文件夹红点 ───────────────────────────────────────
        NewClueNotification.Instance?.ShowNotification("新线索已添加");
        if (folderBadge != null) folderBadge.SetActive(true);

        // ── 重置状态，允许下次再触发 ──────────────────────────────────
        _pendingClueId = null;
        _dialogueLines = null;
        _lineIndex     = 0;

        _onComplete?.Invoke();
    }

    // ════════════════════════════════════════════════════════════════════
    //  辅助协程
    // ════════════════════════════════════════════════════════════════════
    IEnumerator PlayKnocks(int count, float interval)
    {
        if (audioSource == null || knockClip == null) yield break;
        for (int i = 0; i < count; i++)
        {
            audioSource.PlayOneShot(knockClip);
            yield return new WaitForSeconds(interval);
        }
        yield return new WaitForSeconds(0.15f);
    }

    IEnumerator FlickerCrackLight()
    {
        if (doorCrackLight == null) yield break;
        var img = doorCrackLight.GetComponent<Image>();
        while (true)
        {
            float alpha = Mathf.Lerp(0.6f, 1f, Mathf.PingPong(Time.time * 3.5f, 1f));
            if (img != null)
            {
                var c = img.color;
                c.a = alpha;
                img.color = c;
            }
            yield return null;
        }
    }

    IEnumerator SlideChar(Vector2 from, Vector2 to, float duration)
    {
        if (colleagueSprite == null) yield break;
        var rt = colleagueSprite.rectTransform;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Clamp01(t + Time.deltaTime / duration);
            float eased = t * t * (3f - 2f * t);   // smoothstep
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            yield return null;
        }
        rt.anchoredPosition = to;
    }
}
