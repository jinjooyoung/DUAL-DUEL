#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 열려 있는 씬을 휴식 씬으로 구성한다. 여러 번 실행해도 같은 결과 (기존 구성은 지우고 다시 만듦).
/// Tools > DUAL DUEL > 휴식 씬 구성
///
///   RestBGCanvas       (sorting 0)    배경
///   RestUICanvas       (sorting 100)  선택 화면, 팝업, 카드 목록
///   RestCutsceneCanvas (sorting 180)  컷씬 (CRT 스캔라인 캔버스 200 아래)
///   RestScene          컨트롤러 + 컷씬 + 브리지(없으면 테스트용 예제)
///
/// 캔버스는 기준 해상도 1778 x 1000, 높이 맞춤: 캔버스 1단위 = 내보낸 이미지 1px = 0.01 유닛
/// (카메라 Orthographic Size 5, PPU 100 에서 배경과 카드 픽셀 크기가 같아짐)
/// </summary>
public static class RestSceneSetup
{
    const string Root = "RestScene";
    static readonly string[] Generated = { "RestBGCanvas", "RestUICanvas", "RestCutsceneCanvas", Root };
    static readonly Color TextLight = new Color(0.75f, 1f, 0.8f);
    static readonly Color TextDark = new Color(0.02f, 0.12f, 0.05f);
    static readonly Color TextDim = new Color(0.45f, 0.75f, 0.5f);
    static TMP_FontAsset _font;

    // 기계팔 고정점 (Weld_BG 와 같은 위치, 캔버스 좌표)
    static readonly Vector2[] Mounts = { new Vector2(-630f, 120f), new Vector2(0f, 456f), new Vector2(630f, 120f) };
    static readonly float[] Bends = { -1f, 1f, 1f };

    [MenuItem("Tools/DUAL DUEL/휴식 씬 구성")]
    public static void Build()
    {
        Camera cam = Camera.main;
        if (cam == null) { EditorUtility.DisplayDialog("휴식 씬 구성", "MainCamera 태그가 붙은 카메라가 필요합니다.", "확인"); return; }
        if (!cam.orthographic || Mathf.Abs(cam.orthographicSize - 5f) > 0.01f)
            Debug.LogWarning($"[휴식 씬] 카메라가 Orthographic Size 5 가 아닙니다 (현재 {cam.orthographicSize}). 배경 픽셀 크기가 카드와 달라질 수 있습니다.");

        _font = FindFont();
        foreach (string n in Generated) { GameObject old = GameObject.Find(n); if (old != null) Undo.DestroyObjectImmediate(old); }

        // ---------- background ----------
        RectTransform bgCanvas = MakeCanvas("RestBGCanvas", cam, 0, false);
        RectTransform bgZoom = Stretch(NewUI("Zoom", bgCanvas));
        Img("Background", bgZoom, LoadSprite("Rest_BG"), Vector2.zero, true);

        // ---------- UI ----------
        RectTransform ui = MakeCanvas("RestUICanvas", cam, 100, true);
        RectTransform uiZoom = Stretch(NewUI("Zoom", ui));

        // selection
        RectTransform sel = Stretch(NewUI("Selection", uiZoom));
        Text("Title", sel, "휴식", 56, new Vector2(0f, 400f), new Vector2(600f, 80f), TextLight);
        Text("Subtitle", sel, "한 가지를 선택하세요", 26, new Vector2(0f, 345f), new Vector2(600f, 40f), TextDim);
        Button heal = Option(sel, "Heal", "회복", "최대 체력의 20% 회복", "UI_Icon_Heal", -300f);
        Button upg = Option(sel, "Upgrade", "강화", "카드 1장 강화", "UI_Icon_Upgrade", 0f);
        Button rem = Option(sel, "Remove", "제거", "카드 1장 제거", "UI_Icon_Remove", 300f);

        // heal popup
        RectTransform healPop = Stretch(NewUI("HealPopup", uiZoom));
        Dim(healPop, 0.6f);
        Image panel = Img("Panel", healPop, LoadSprite("UI_Popup"), Vector2.zero, false);
        panel.type = Image.Type.Sliced; panel.rectTransform.sizeDelta = new Vector2(720f, 400f);
        Text("Title", panel.rectTransform, "회복", 36, new Vector2(0f, 140f), new Vector2(600f, 50f), TextLight);
        TMP_Text healText = Text("Body", panel.rectTransform, "최대 체력의 20%를 회복합니다.\n31  ->  41", 28, new Vector2(0f, 30f), new Vector2(640f, 120f), TextLight);
        Button healOk = Btn(panel.rectTransform, "Confirm", "회복하기", true, new Vector2(-150f, -120f));
        Button healNo = Btn(panel.rectTransform, "Cancel", "돌아가기", false, new Vector2(150f, -120f));

        // card list
        RectTransform list = Stretch(NewUI("CardList", uiZoom));
        Dim(list, 0.85f);
        TMP_Text listTitle = Text("Title", list, "강화할 카드를 선택하세요", 32, new Vector2(0f, 440f), new Vector2(900f, 50f), TextLight);
        ScrollRect scroll = ScrollView(list, out RectTransform content);
        Button listClose = Btn(list, "Close", "돌아가기", false, new Vector2(0f, -455f));

        // upgrade compare
        RectTransform cmp = Stretch(NewUI("ComparePopup", uiZoom));
        Dim(cmp, 0.6f);
        RectTransform before = Slot(cmp, "BeforeSlot", new Vector2(-230f, 40f));
        RectTransform after = Slot(cmp, "AfterSlot", new Vector2(230f, 40f));
        Image arrow = Img("Arrow", cmp, LoadSprite("UI_Arrow"), new Vector2(0f, 40f), true);
        arrow.rectTransform.localScale = Vector3.one * 1.5f;
        Text("BeforeLabel", cmp, "강화 전", 24, new Vector2(-230f, -160f), new Vector2(300f, 40f), TextDim);
        Text("AfterLabel", cmp, "강화 후", 24, new Vector2(230f, -160f), new Vector2(300f, 40f), TextLight);
        Button upOk = Btn(cmp, "Confirm", "강화하기", true, new Vector2(-150f, -300f));
        Button upNo = Btn(cmp, "Cancel", "돌아가기", false, new Vector2(150f, -300f));

        // remove confirm
        RectTransform rmv = Stretch(NewUI("RemovePopup", uiZoom));
        Dim(rmv, 0.6f);
        RectTransform rmSlot = Slot(rmv, "CardSlot", new Vector2(0f, 40f));
        Text("Label", rmv, "이 카드를 덱에서 제거합니다", 24, new Vector2(0f, -170f), new Vector2(600f, 40f), TextLight);
        Button rmOk = Btn(rmv, "Confirm", "제거하기", true, new Vector2(-150f, -300f));
        Button rmNo = Btn(rmv, "Cancel", "돌아가기", false, new Vector2(150f, -300f));

        // ---------- cutscene ----------
        RectTransform cs = MakeCanvas("RestCutsceneCanvas", cam, 180, true);
        CanvasGroup group = cs.gameObject.AddComponent<CanvasGroup>();
        Image focusDim = Dim(cs, 0f);
        focusDim.name = "FocusDim";
        RectTransform weld = Stretch(NewUI("WeldStage", cs));
        Img("WeldBG", weld, LoadSprite("Weld_BG"), Vector2.zero, true);
        GameObject cracks = Img("Cracks", weld, LoadSprite("Weld_Cracks"), Vector2.zero, true).gameObject;
        var arms = new RobotArm[3];
        for (int i = 0; i < 3; i++) arms[i] = Arm(weld, i);
        RectTransform particles = Stretch(NewUI("Particles", cs));
        RectTransform focus = NewUI("FocusRoot", cs);
        focus.sizeDelta = Vector2.zero;
        Image burst = Img("Burst", cs, LoadSprite("FX_Burst"), Vector2.zero, true);
        burst.raycastTarget = false;
        burst.gameObject.SetActive(false);
        Image white = Dim(cs, 0f);
        white.name = "White";
        white.color = new Color(1f, 1f, 1f, 0f);
        white.rectTransform.SetAsLastSibling();   // 흰 화면은 항상 맨 위

        // ---------- logic ----------
        var rootGo = new GameObject(Root);
        Undo.RegisterCreatedObjectUndo(rootGo, "Rest Scene");
        RestSceneBridge bridge = Object.FindFirstObjectByType<RestSceneBridge>();
        if (bridge == null)
        {
            var ex = rootGo.AddComponent<RestSceneBridgeExample>();
            ex.cardSprite = LoadSprite("UI_CardPlaceholder");
            ex.font = _font;
            bridge = ex;
            Debug.Log("[휴식 씬] 테스트용 RestSceneBridgeExample 을 붙였습니다. 실제 게임 데이터용 브리지로 교체하세요.");
        }

        var cut = rootGo.AddComponent<RestCutscenes>();
        cut.bridge = bridge;
        cut.group = group;
        cut.white = white;
        cut.focusDim = focusDim;
        cut.focusRoot = focus;
        cut.zoomTargets = new[] { bgZoom, uiZoom };
        cut.weldStage = weld.gameObject;
        cut.cracks = cracks;
        cut.arms = arms;
        cut.particleLayer = particles;
        cut.burst = burst;
        cut.shineSprite = LoadSprite("FX_Shine");

        var ctl = rootGo.AddComponent<RestSceneController>();
        ctl.bridge = bridge;
        ctl.cutscenes = cut;
        ctl.selection = sel.gameObject;
        ctl.healOption = heal; ctl.upgradeOption = upg; ctl.removeOption = rem;
        ctl.healPopup = healPop.gameObject; ctl.healText = healText; ctl.healConfirm = healOk; ctl.healCancel = healNo;
        ctl.listOverlay = list.gameObject; ctl.listTitle = listTitle; ctl.listContent = content; ctl.listScroll = scroll; ctl.listClose = listClose;
        ctl.comparePopup = cmp.gameObject; ctl.compareBefore = before; ctl.compareAfter = after; ctl.upgradeConfirm = upOk; ctl.upgradeCancel = upNo;
        ctl.removePopup = rmv.gameObject; ctl.removeSlot = rmSlot; ctl.removeConfirm = rmOk; ctl.removeCancel = rmNo;

        weld.gameObject.SetActive(false);
        healPop.gameObject.SetActive(false);
        list.gameObject.SetActive(false);
        cmp.gameObject.SetActive(false);
        rmv.gameObject.SetActive(false);

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "EventSystem");
        }

        EditorSceneManager.MarkSceneDirty(rootGo.scene);
        Selection.activeGameObject = rootGo;
        Debug.Log("[휴식 씬] 구성 완료. 씬을 저장하세요. CRT 연출이 필요하면 Tools > DUAL DUEL > 배틀씬 CRT 연출 세팅 도 실행하세요.");
    }

    // ------------------------------------------------------------------ builders

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

    static Image Dim(Transform parent, float alpha)
    {
        RectTransform rt = Stretch(NewUI("Dim", parent));
        rt.offsetMin = new Vector2(-80f, -80f); rt.offsetMax = new Vector2(80f, 80f);   // 줌/왜곡에도 가장자리가 비지 않게
        rt.gameObject.AddComponent<CanvasRenderer>();
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, alpha);
        img.raycastTarget = true;    // 뒤의 UI 클릭 막기
        rt.SetAsFirstSibling();
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
        Image img = Img(name, parent, LoadSprite(bright ? "UI_Button_Bright" : "UI_Button_Dark"), pos, false);
        img.type = Image.Type.Sliced;
        img.rectTransform.sizeDelta = new Vector2(260f, 64f);
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        var colors = b.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f);
        colors.normalColor = new Color(0.85f, 0.85f, 0.85f);
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        b.colors = colors;
        Text("Label", img.rectTransform, label, 28, Vector2.zero, new Vector2(240f, 56f), bright ? TextDark : TextLight);
        return b;
    }

    static Button Option(Transform parent, string name, string title, string desc, string icon, float x)
    {
        Image panel = Img(name, parent, LoadSprite("UI_OptionPanel"), new Vector2(x, -20f), true);
        panel.raycastTarget = true;
        var b = panel.gameObject.AddComponent<Button>();
        var colors = b.colors;
        colors.normalColor = new Color(0.8f, 0.8f, 0.8f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        b.colors = colors;
        Image ic = Img("Icon", panel.rectTransform, LoadSprite(icon), new Vector2(0f, 54f), true);
        ic.rectTransform.localScale = Vector3.one * 2f;
        Text("Label", panel.rectTransform, title, 32, new Vector2(0f, -82f), new Vector2(200f, 50f), TextLight);
        Text("Desc", panel.rectTransform, desc, 22, new Vector2(0f, -200f), new Vector2(280f, 40f), TextDim);
        return b;
    }

    static RectTransform Slot(Transform parent, string name, Vector2 pos)
    {
        RectTransform rt = NewUI(name, parent);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(160f, 240f);
        rt.localScale = Vector3.one * 1.4f;
        return rt;
    }

    static ScrollRect ScrollView(Transform parent, out RectTransform content)
    {
        Image frame = Img("ScrollView", parent, LoadSprite("UI_ScrollFrame"), new Vector2(0f, -10f), false);
        frame.type = Image.Type.Sliced;
        frame.rectTransform.sizeDelta = new Vector2(1440f, 780f);
        frame.raycastTarget = true;

        RectTransform viewport = Stretch(NewUI("Viewport", frame.rectTransform));
        viewport.offsetMin = new Vector2(16f, 16f); viewport.offsetMax = new Vector2(-44f, -16f);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<CanvasRenderer>();
        var vimg = viewport.gameObject.AddComponent<Image>();
        vimg.color = new Color(0f, 0f, 0f, 0f);   // 빈 곳도 드래그 스크롤 되도록

        content = NewUI("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0f, 0f);
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160f, 240f);
        grid.spacing = new Vector2(20f, 20f);
        grid.padding = new RectOffset(24, 24, 24, 24);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 7;
        var fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image track = Img("Scrollbar", frame.rectTransform, LoadSprite("UI_ScrollTrack"), Vector2.zero, false);
        track.type = Image.Type.Sliced;
        RectTransform trt = track.rectTransform;
        trt.anchorMin = new Vector2(1f, 0f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(1f, 0.5f);
        trt.sizeDelta = new Vector2(20f, -40f); trt.anchoredPosition = new Vector2(-16f, 0f);
        track.raycastTarget = true;
        RectTransform area = Stretch(NewUI("Sliding Area", trt));
        Image handle = Img("Handle", area, LoadSprite("UI_ScrollHandle"), Vector2.zero, false);
        handle.type = Image.Type.Sliced;
        Stretch(handle.rectTransform);
        handle.raycastTarget = true;
        var bar = track.gameObject.AddComponent<Scrollbar>();
        bar.handleRect = handle.rectTransform;
        bar.targetGraphic = handle;
        bar.direction = Scrollbar.Direction.BottomToTop;

        var sr = frame.gameObject.AddComponent<ScrollRect>();
        sr.content = content;
        sr.viewport = viewport;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40f;
        sr.verticalScrollbar = bar;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        return sr;
    }

    static RobotArm Arm(Transform stage, int i)
    {
        RectTransform root = Stretch(NewUI($"Arm{i}", stage));
        // 그리는 순서: 윗팔, 아랫팔, 용접기, 고정판 (고정판이 어깨 관절을 덮음)
        RectTransform upper = Seg(root, "Upper", "FX_Arm_Upper", 9f * 2f);
        RectTransform fore = Seg(root, "Fore", "FX_Arm_Fore", 8f * 2f);
        RectTransform welder = Seg(root, "Welder", "FX_Arm_Welder", 7f * 2f);
        Image b = Img("Base", root, LoadSprite("FX_Arm_Base"), Mounts[i], true);
        var arm = root.gameObject.AddComponent<RobotArm>();
        arm.shoulder = b.rectTransform;
        arm.upper = upper; arm.fore = fore; arm.welder = welder;
        arm.upperLength = 300f; arm.foreLength = 260f; arm.tipLength = 52f;
        arm.bend = Bends[i];
        arm.target = Mounts[i] + new Vector2(0f, -200f);
        return arm;
    }

    static RectTransform Seg(Transform parent, string name, string sprite, float pivotFromTop)
    {
        Image img = Img(name, parent, LoadSprite(sprite), Vector2.zero, true);
        RectTransform rt = img.rectTransform;
        rt.pivot = new Vector2(0.5f, 1f - pivotFromTop / rt.sizeDelta.y);
        return rt;
    }

    // ------------------------------------------------------------------ assets

    static Sprite LoadSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{name} t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        Debug.LogWarning($"[휴식 씬] 스프라이트 '{name}' 를 찾지 못했습니다.");
        return null;
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
