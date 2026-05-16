using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [SerializeField] private GameObject mainCardPrefab;
    [SerializeField] private GameObject minorCardPrefab;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private CharacterData[] allCharacters;
    [SerializeField] private LineManager lineManager;

    private Dictionary<string, CharacterCard> cardMap = new();

    // 初始位置（可自行调整）
    private Vector2[] startPositions = {
        new(-300, 100), new(0, 100), new(300, 100), new(-150, -100),
        new(150, -100), new(-300, -100), new(300, -100)
    };

    void Start()
    {
        for (int i = 0; i < allCharacters.Length; i++)
        {
            var data = allCharacters[i];
            data.ResetRuntimeData();

            var prefab = data.characterType == CharacterType.Main ? mainCardPrefab : minorCardPrefab;
            var go = Instantiate(prefab, contentRoot);
            go.GetComponent<RectTransform>().anchoredPosition =
                i < startPositions.Length ? startPositions[i] : Vector2.zero;

            var card = go.GetComponent<CharacterCard>();
            card.data = data;
            card.RefreshDisplay();
            cardMap[data.characterId] = card;

            LockSystem.Instance.RegisterCharacter(data);
            lineManager.RegisterCard(data.characterId, go.GetComponent<RectTransform>());
        }

        EventBus.Emit("OnCardsReady", null);
    }
}