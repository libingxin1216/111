using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [SerializeField] private GameObject mainCardPrefab;
    [SerializeField] private GameObject minorCardPrefab;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private LineManager lineManager;

    [System.Serializable]
    public class CardSlot
    {
        [HideInInspector] public string slotName;
        public CharacterData character;
        [HideInInspector] public Vector2 position;
    }

    [Header("人物槽位（只需拖入人物，位置已在代码中写好）")]
    [SerializeField] private CardSlot[] cardSlots;

    // 位置和名称数据
    private static readonly string[] slotNames = {
        "钟德发","高亚珍","老六","梅仁义",
        "陈嘉乐","陈建军","刘桂英","陈守山","王秀莲",
        "李雨欣","张翠兰","李建国",
        "谢雨彤","谢明远","林婉清",
    };

    private static readonly Vector2[] slotPositions = {
    // ── 嫌疑人（第一排，顶部）─────────────────────────────
    new(-600,  400), new(-200,  400), new( 200,  400), new( 600,  400),

    // ── 第一起受害者（第二排）─────────────────────────────
    new(-700,  140),  // 陈嘉乐（主要，居中偏左）
    new(-850, -130), new(-500, -130), new(-700, -390), new(-350, -390),

    // ── 第二起受害者（第三排）─────────────────────────────
    new(   0,  140),  // 李雨欣（主要，居中）
    new(-150, -130), new( 150, -130),

    // ── 第三起受害者（第四排）─────────────────────────────
    new( 600,  140),  // 谢雨彤（主要，居中偏右）
    new( 450, -130), new( 750, -130),
};

    // 编辑器中点击 Reset 或首次挂载时自动初始化槽位
    void Reset()
    {
        InitSlots();
    }

    [ContextMenu("重新初始化槽位")]
    void InitSlots()
    {
        cardSlots = new CardSlot[slotNames.Length];
        for (int i = 0; i < slotNames.Length; i++)
        {
            cardSlots[i] = new CardSlot
            {
                slotName = slotNames[i],
                position = slotPositions[i],
            };
        }
    }

    void Start()
    {
        var map = new Dictionary<string, CharacterCard>();

        for (int i = 0; i < cardSlots.Length; i++)
        {
            var slot = cardSlots[i];
            if (slot.character == null) continue;

            slot.character.ResetRuntimeData();

            var prefab = slot.character.characterType == CharacterType.Main
                       ? mainCardPrefab : minorCardPrefab;

            var go = Instantiate(prefab, contentRoot);
            go.name = slotNames[i];
            go.GetComponent<RectTransform>().anchoredPosition = slotPositions[i];

            var card = go.GetComponent<CharacterCard>();
            card.data = slot.character;
            card.RefreshDisplay();

            map[slot.character.characterId] = card;

            LockSystem.Instance.RegisterCharacter(slot.character);
            lineManager.RegisterCard(
                slotNames[i],
                go.GetComponent<RectTransform>());
        }

        EventBus.Emit("OnCardsReady", null);
    }
}