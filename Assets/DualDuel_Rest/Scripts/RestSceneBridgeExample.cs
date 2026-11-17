using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 테스트용 브리지: 가짜 체력과 카드 12장으로 휴식 씬 전체를 돌려볼 수 있다.
/// 실제 게임에 연결할 때는 이 파일을 참고해 RestSceneBridge 를 상속한 클래스를 만들고
/// (예: 플레이어 데이터에서 HP 를 읽고, CardInstance 로 실제 카드 UI 를 만들고) 씬의 브리지를 교체하세요.
/// </summary>
public class RestSceneBridgeExample : RestSceneBridge
{
    public int currentHP = 31;
    public int maxHP = 50;
    public Sprite cardSprite;           // UI_CardPlaceholder
    public TMP_FontAsset font;

    class FakeCard { public string name; public int value; public int level; }
    readonly List<object> _deck = new List<object>();

    void Awake()
    {
        string[] names = { "스트라이크", "브루트 포스", "헤비 스매시", "잽", "방어", "회복", "재생", "흡혈", "독", "출혈", "약화", "힘" };
        for (int i = 0; i < names.Length; i++) _deck.Add(new FakeCard { name = names[i], value = 3 + i % 5, level = i % 3 == 0 ? 1 : 0 });
    }

    public override int CurrentHP => currentHP;
    public override int MaxHP => maxHP;
    public override void ApplyHeal(int amount) { currentHP = Mathf.Min(maxHP, currentHP + amount); Debug.Log($"[Rest] 회복 +{amount} -> {currentHP}"); }

    public override IList<object> GetCards(bool forUpgrade)
    {
        if (!forUpgrade) return _deck;
        var list = new List<object>();
        foreach (FakeCard c in _deck) if (c.level < 2) list.Add(c);
        return list;
    }

    public override RectTransform CreateCardView(object card, Transform parent, bool upgradedPreview)
    {
        var c = (FakeCard)card;
        int level = c.level + (upgradedPreview ? 1 : 0);
        var go = new GameObject($"Card_{c.name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(160f, 240f);
        var img = go.GetComponent<Image>();
        img.sprite = cardSprite;
        Label(rt, c.name + new string('+', level), new Vector2(0f, 98f), 18f);
        Label(rt, (c.value + level * 2).ToString(), new Vector2(0f, -93f), 22f);
        return rt;
    }

    void Label(RectTransform parent, string text, Vector2 pos, float size)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(150f, 30f);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.75f, 1f, 0.8f);
        t.raycastTarget = false;
    }

    public override void ApplyUpgrade(object card) { var c = (FakeCard)card; c.level++; Debug.Log($"[Rest] 강화 {c.name} -> +{c.level}"); }
    public override void RemoveCard(object card) { _deck.Remove(card); Debug.Log($"[Rest] 제거 {((FakeCard)card).name}"); }
}
