#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 열려 있는 씬을 상점 씬으로 구성한다. 여러 번 실행해도 같은 결과 (기존 구성은 지우고 다시 만듦).
/// Tools > DUAL DUEL > 상점 씬 구성
///
///   ShopBGCanvas (sorting 0)    Shop_BG (빈 카드 슬롯 8개가 그려진 배경)
///   ShopUICanvas (sorting 100)  카드 8장(UI_ViewerCard), 가격, 제목, 보유 골드, 나가기, 덱 보기
///   ShopScene                   ShopManager + ShopPurchaseFX
///
/// 캔버스 기준 해상도 1778 x 1000, 높이 맞춤 (카메라 Orthographic Size 5, PPU 100 에서 배경 픽셀 = 카드 픽셀)
/// </summary>
public static class ShopSceneSetup
{
    static readonly string[] Generated = { "ShopBGCanvas", "ShopUICanvas", "ShopScene" };
    static readonly Color TextLight = new Color(0.75f, 1f, 0.8f);
    static readonly Color TextDark = new Color(0.02f, 0.12f, 0.05f);
    static readonly Color TextDim = new Color(0.45f, 0.75f, 0.5f);

    // 배경(Shop_BG)에 그려진 슬롯 위치 (캔버스 좌표, 화면 중앙 기준)
    static readonly float[] CardX = { -324f, -108f, 108f, 324f };
    static readonly float[] CardY = { 178f, -126f };
    static readonly float[] TagY = { 34f, -270f };
    static readonly Vector2 SlotSize = new Vector2(160f, 240f);

    static TMP_FontAsset _font;

    [MenuItem("Tools/DUAL DUEL/상점 씬 구성")]
    public static void Build()
    {
        Camera cam = Camera.main;
        if (cam == null) { EditorUtility.DisplayDialog("상점 씬 구성", "MainCamera 태그가 붙은 카메라가 필요합니다.", "확인"); return; }
        if (!cam.orthographic || Mathf.Abs(cam.orthographicSize - 5f) > 0.01f)
            Debug.LogWarning($"[상점 씬] 카메라가 Orthographic Size 5 가 아닙니다 (현재 {cam.orthographicSize}). 배경과 카드 슬롯 위치가 어긋날 수 있습니다.");

        GameObject cardPrefab = FindViewerCardPrefab();
        if (cardPrefab == null)
        {
            EditorUtility.DisplayDialog("상점 씬 구성", "UIViewerCard 가 붙은 카드 프리팹(UI_ViewerCard)을 찾지 못했습니다.", "확인");
            return;
        }

        _font = FindFont();
        foreach (string n in Generated) { GameObject old = GameObject.Find(n); if (old != null) Undo.DestroyObjectImmediate(old); }

        // ---------- background ----------
        RectTransform bgCanvas = MakeCanvas("ShopBGCanvas", cam, 0, false);
        Img("Background", bgCanvas, LoadSprite("Shop_BG"), Vector2.zero, true);

        // ---------- UI ----------
        RectTransform ui = MakeCanvas("ShopUICanvas", cam, 100, true);
        Text("Title", ui, "카드 상점", 44, new Vector2(0f, 390f), new Vector2(400f, 80f), TextLight);

        // 보유 골드 (우상단)
        Image goldPanel = Img("Gold", ui, LoadSprite("Shop_GoldPanel"), new Vector2(682f, 440f), false);
        goldPanel.type = Image.Type.Sliced;
        goldPanel.rectTransform.sizeDelta = new Vector2(344f, 68f);
        Img("Coin", goldPanel.rectTransform, LoadSprite("Shop_Coin"), new Vector2(-120f, 0f), true).rectTransform.localScale = Vector3.one * 1.5f;
        TMP_Text goldText = Text("Amount", goldPanel.rectTransform, "0", 40, new Vector2(30f, 0f), new Vector2(220f, 60f), TextLight);
        goldText.alignment = TextAlignmentOptions.Right;
        GoldCounter goldCounter = goldText.gameObject.AddComponent<GoldCounter>();

        // 카드 8장 + 가격
        RectTransform cards = Stretch(NewUI("Cards", ui));
        var viewers = new UIViewerCard[ShopManager.SlotCount];
        var prices = new TMP_Text[ShopManager.SlotCount];
        var coins = new Graphic[ShopManager.SlotCount];
        var prefabRt = (RectTransform)cardPrefab.transform;
        Vector2 prefabSize = prefabRt.rect.size;
        float fit = prefabSize.x > 1f && prefabSize.y > 1f ? Mathf.Min(SlotSize.x / prefabSize.x, SlotSize.y / prefabSize.y) : 1f;
        for (int i = 0; i < ShopManager.SlotCount; i++)
        {
            int col = i % 4, row = i / 4;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab, cards);
            Undo.RegisterCreatedObjectUndo(go, "Shop Card");
            go.name = $"ShopCard_{i}";
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(CardX[col], CardY[row]);
            rt.localScale = Vector3.one * fit;   // 배경 슬롯(160 x 240)에 맞춤 = 구매 연출의 originalScale
            go.AddComponent<ShopCardSlot>();
            viewers[i] = go.GetComponent<UIViewerCard>();

            RectTransform tag = NewUI($"Price_{i}", ui);
            tag.anchoredPosition = new Vector2(CardX[col], TagY[row]);
            tag.sizeDelta = new Vector2(128f, 32f);
            Image coin = Img("Coin", tag, LoadSprite("Shop_Coin"), new Vector2(-38f, 0f), true);
            coins[i] = coin;
            TMP_Text price = Text("Amount", tag, "0", 26, new Vector2(12f, 0f), new Vector2(84f, 32f), TextLight);
            prices[i] = price;
        }

        // 버튼: 나가기 (우하단), 덱 보기 (좌하단, 구매한 카드가 날아가 들어가는 곳)
        Button exit = Btn(ui, "Exit", "나가기", true, new Vector2(682f, -436f));
        Button deck = Btn(ui, "Deck", "덱 보기", false, new Vector2(-682f, -436f));

        // ---------- logic ----------
        var root = new GameObject("ShopScene");
        Undo.RegisterCreatedObjectUndo(root, "Shop Scene");
        var fx = root.AddComponent<ShopPurchaseFX>();
        fx.sparkleSprite = LoadSprite("FX_Shop_Sparkle");
        fx.glowSprite = LoadSprite("FX_Shop_Glow");
        var manager = root.AddComponent<ShopManager>();

        var so = new SerializedObject(manager);
        SetList(so.FindProperty("cardViewers"), viewers);
        SetList(so.FindProperty("priceTexts"), prices);
        SetList(so.FindProperty("priceIcons"), coins);
        so.FindProperty("goldCounter").objectReferenceValue = goldCounter;
        so.FindProperty("exitButton").objectReferenceValue = exit;
        so.FindProperty("deckTarget").objectReferenceValue = deck.transform;
        so.FindProperty("deckButton").objectReferenceValue = deck;
        so.FindProperty("purchaseFX").objectReferenceValue = fx;
        DeckViewerPopup viewer = Object.FindFirstObjectByType<DeckViewerPopup>(FindObjectsInactive.Include);
        if (viewer != null) so.FindProperty("deckViewer").objectReferenceValue = viewer;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "EventSystem");
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log($"[상점 씬] 구성 완료 (카드 프리팹 '{cardPrefab.name}', 슬롯 맞춤 배율 {fit:0.###}). " +
                  (viewer == null ? "덱 보기 팝업(DeckViewerPopup)이 씬에 없어 덱 보기 버튼은 연출 도착 지점으로만 쓰입니다. " : "") +
                  "씬을 저장하세요. CRT 연출이 필요하면 Tools > DUAL DUEL > 배틀씬 CRT 연출 세팅 도 실행하세요.");
    }

    // ------------------------------------------------------------------ builders

    static void SetList(SerializedProperty list, Object[] items)
    {
        list.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    static RectTransform MakeCanvas(string name, Camera cam, int order, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(go, name);
        go.layer = LayerMask.NameToLayer("UI");
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceCamera;
        c.worldCamera = cam;
        c.planeDistance = 100f;
        c.sortingOrder = order;
        var s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1778f, 1000f);
        s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        s.matchWidthOrHeight = 1f;
        s.referencePixelsPerUnit = 100f;
        if (raycast) go.AddComponent<GraphicRaycaster>();
        return (RectTransform)go.transform;
    }

    static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    static Image Img(string name, Transform parent, Sprite sprite, Vector2 pos, bool native)
    {
        RectTransform rt = NewUI(name, parent);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        rt.anchoredPosition = pos;
        if (native && sprite != null) img.SetNativeSize();
        return img;
    }

    static TMP_Text Text(string name, Transform parent, string text, float size, Vector2 pos, Vector2 box, Color color)
    {
        RectTransform rt = NewUI(name, parent);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    static Button Btn(Transform parent, string name, string label, bool bright, Vector2 pos)
    {
        Image img = Img(name, parent, LoadSprite(bright ? "Shop_Button_Bright" : "Shop_Button_Dark"), pos, false);
        img.type = Image.Type.Sliced;
        img.rectTransform.sizeDelta = new Vector2(344f, 72f);
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        var colors = b.colors;
        colors.normalColor = new Color(0.85f, 0.85f, 0.85f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        b.colors = colors;
        Text("Label", img.rectTransform, label, 34, Vector2.zero, new Vector2(320f, 64f), bright ? TextDark : TextLight);
        return b;
    }

    // ------------------------------------------------------------------ assets

    static Sprite LoadSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{name} t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        Debug.LogWarning($"[상점 씬] 스프라이트 '{name}' 를 찾지 못했습니다.");
        return null;
    }

    /// <summary>UIViewerCard 가 붙은 프리팹. 이름이 UI_ViewerCard 인 것을 먼저 찾음</summary>
    static GameObject FindViewerCardPrefab()
    {
        GameObject fallback = null;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null || go.GetComponent<UIViewerCard>() == null) continue;
            if (Path.GetFileNameWithoutExtension(path) == "UI_ViewerCard") return go;
            if (fallback == null) fallback = go;
        }
        return fallback;
    }

    static TMP_FontAsset FindFont()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string n = Path.GetFileNameWithoutExtension(path);
            if (n.Contains("Stardust") || n.Contains("스타더스트") || n.StartsWith("PF")) return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }
        return TMP_Settings.defaultFontAsset;
    }
}
#endif
