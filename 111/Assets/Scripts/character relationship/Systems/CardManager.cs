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

    [Header("�����λ��ֻ���������λ�����ڴ�����д�ã�")]
    [SerializeField] private CardSlot[] cardSlots;

    // λ�ú���������
    private static readonly string[] slotNames = {
        "�ӵ·�","������","����","÷����",
        "�¼���","�½���","����Ӣ","����ɽ","������",
        "������","�Ŵ���","���",
        "л��ͮ","л��Զ","������",
    };

    private static readonly Vector2[] slotPositions = {
    // ���� �����ˣ���һ�ţ�����������������������������������������������������������������
    new(-600,  400), new(-200,  400), new( 200,  400), new( 600,  400),

    // ���� ��һ���ܺ��ߣ��ڶ��ţ�����������������������������������������������������������
    new(-700,  140),  // �¼��֣���Ҫ������ƫ��
    new(-850, -130), new(-500, -130), new(-700, -390), new(-350, -390),

    // ���� �ڶ����ܺ��ߣ������ţ�����������������������������������������������������������
    new(   0,  140),  // ����������Ҫ�����У�
    new(-150, -130), new( 150, -130),

    // ���� �������ܺ��ߣ������ţ�����������������������������������������������������������
    new( 600,  140),  // л��ͮ����Ҫ������ƫ�ң�
    new( 450, -130), new( 750, -130),
};

    // �༭���е�� Reset ���״ι���ʱ�Զ���ʼ����λ
    void Reset()
    {
        InitSlots();
    }

    [ContextMenu("���³�ʼ����λ")]
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

            // 若 GameManager 已有该角色的存档进度，跳过 ResetRuntimeData()，
            // CharacterCard.Start() 会从 GameManager 恢复数据。
            // 若无存档（首次游戏），才重置为初始状态。
            bool hasSavedProgress = GameManager.Instance != null
                && GameManager.Instance.CharacterProgressMap
                               .ContainsKey(slot.character.characterId);
            if (!hasSavedProgress)
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