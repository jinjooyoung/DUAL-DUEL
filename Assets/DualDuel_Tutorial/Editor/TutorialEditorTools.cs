#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 에디터 메뉴
///   Tools > DUAL DUEL > 대사 CSV 가져오기              엑셀 Dialogue 시트를 'CSV UTF-8' 로 저장한 파일 -> DialogueTable 에셋
///   Tools > DUAL DUEL > 튜토리얼 매니저 프리팹 만들기    대사창/스탠딩/TMP/스킵 버튼이 들어 있는 TutorialManager 프리팹 생성 (+ 열린 씬에 배치)
///   Tools > DUAL DUEL > 튜토리얼 최초 실행 상태로 초기화  PlayerPrefs 를 0 으로 (테스트용)
/// </summary>
public static class TutorialEditorTools
{
    const string DefaultTablePath = "Assets/Data/DialogueTable.asset";
    const string PrefabPath = "Assets/Prefabs/Tutorial/TutorialManager.prefab";

    [MenuItem("Tools/DUAL DUEL/대사 CSV 가져오기")]
    public static void ImportCsv()
    {
        string path = EditorUtility.OpenFilePanel("Dialogue 시트 CSV (UTF-8) 선택", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(path)) return;

        List<string[]> rows = ParseCsv(File.ReadAllText(path, Encoding.UTF8));
        if (rows.Count < 2) { EditorUtility.DisplayDialog("대사 CSV 가져오기", "행이 없습니다.", "확인"); return; }

        // 헤더 이름으로 열 찾기 (열 순서가 바뀌어도 동작)
        string[] header = rows[0];
        int Col(string name) { for (int i = 0; i < header.Length; i++) if (header[i].Trim().Equals(name, System.StringComparison.OrdinalIgnoreCase)) return i; return -1; }
        int cId = Col("id"), cName = Col("characterName"), cText = Col("text"), cNext = Col("nextId"), cPath = Col("portraitPath"), cExpr = Col("expression");
        if (cId < 0 || cText < 0 || cNext < 0)
        {
            EditorUtility.DisplayDialog("대사 CSV 가져오기", "id, text, nextId 열이 필요합니다.", "확인");
            return;
        }

        var lines = new List<DialogueData>();
        for (int r = 1; r < rows.Count; r++)
        {
            string[] row = rows[r];
            string Get(int c) => c >= 0 && c < row.Length ? row[c].Trim() : string.Empty;
            if (!int.TryParse(Get(cId), out int id)) continue;   // 빈 줄, 메모 줄은 건너뜀
            lines.Add(new DialogueData
            {
                id = id,
                characterName = Get(cName),
                text = Get(cText),
                nextId = int.TryParse(Get(cNext), out int next) ? next : DialogueData.END,   // 비어 있으면 끝(-1)
                portraitPath = Get(cPath),
                expression = Get(cExpr),
            });
        }

        DialogueTableSO table = Selection.activeObject as DialogueTableSO;
        if (table == null) table = AssetDatabase.LoadAssetAtPath<DialogueTableSO>(DefaultTablePath);
        if (table == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DefaultTablePath));
            table = ScriptableObject.CreateInstance<DialogueTableSO>();
            AssetDatabase.CreateAsset(table, DefaultTablePath);
        }
        Undo.RecordObject(table, "Import Dialogue CSV");
        table.SetLines(lines);
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        Selection.activeObject = table;
        Debug.Log($"[튜토리얼] 대사 {lines.Count}줄을 {AssetDatabase.GetAssetPath(table)} 에 가져왔습니다.");
    }

    [MenuItem("Tools/DUAL DUEL/튜토리얼 매니저 프리팹 만들기")]
    public static void CreatePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
            !EditorUtility.DisplayDialog("튜토리얼 매니저 프리팹", $"{PrefabPath} 가 이미 있습니다.\n덮어쓰면 프리팹에서 직접 바꾼 위치/크기가 초기화됩니다.", "덮어쓰기", "취소"))
            return;

        Sprite frame = FindAsset<Sprite>("UI_ScrollFrame", "t:Sprite");
        TMP_FontAsset font = FindFont();
        Color nameColor = new Color(0.55f, 1f, 0.65f), textColor = new Color(0.8f, 1f, 0.85f);

        // 루트: 캔버스 (카메라는 실행 중에 매니저가 붙임)
        var root = new GameObject("TutorialManager", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = LayerMask.NameToLayer("UI");
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 195;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1778f, 1000f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        RectTransform dialogue = Stretch(Child("Dialogue", root.transform));

        // 화면 전체 입력 막음 (대사창 이외 클릭 차단, 살짝 어둡게)
        Image blocker = AddImage(Stretch(Child("Blocker", dialogue)), null, new Color(0f, 0f, 0f, 0.25f));
        blocker.raycastTarget = true;

        // 스탠딩: 대사창 뒤, 화면 좌측 하단 (비율 유지, 스프라이트가 없으면 매니저가 끔)
        Image portrait = AddImage(Child("Portrait", dialogue), null, Color.white);
        RectTransform prt = portrait.rectTransform;
        prt.anchorMin = prt.anchorMax = Vector2.zero; prt.pivot = Vector2.zero;
        prt.sizeDelta = new Vector2(480f, 640f); prt.anchoredPosition = new Vector2(120f, 0f);
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        portrait.gameObject.SetActive(false);

        // 대사창: 하단 중앙 (UI_ScrollFrame 9-슬라이스, 없으면 어두운 단색)
        Image box = AddImage(Child("Box", dialogue), frame, frame != null ? Color.white : new Color(0.02f, 0.08f, 0.04f, 0.95f));
        if (frame != null) box.type = Image.Type.Sliced;
        box.raycastTarget = false;
        RectTransform brt = box.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f); brt.pivot = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(1500f, 240f); brt.anchoredPosition = new Vector2(0f, 30f);

        TMP_Text name = AddText(Child("Name", brt), font, 30f, nameColor, TextAlignmentOptions.TopLeft, "도우미");
        RectTransform nrt = name.rectTransform;
        nrt.anchorMin = new Vector2(0f, 1f); nrt.anchorMax = new Vector2(1f, 1f); nrt.pivot = new Vector2(0f, 1f);
        nrt.offsetMin = new Vector2(44f, -64f); nrt.offsetMax = new Vector2(-44f, -22f);

        TMP_Text body = AddText(Child("Text", brt), font, 30f, textColor, TextAlignmentOptions.TopLeft, "대사가 여기에 타이핑됩니다.");
        RectTransform trt = body.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(44f, 34f); trt.offsetMax = new Vector2(-44f, -70f);

        // 다음 표시: 폰트에 기호가 없어도 보이도록 글자 대신 작은 마름모
        Image next = AddImage(Child("Next", brt), null, nameColor);
        next.raycastTarget = false;
        RectTransform mrt = next.rectTransform;
        mrt.anchorMin = mrt.anchorMax = new Vector2(1f, 0f);
        mrt.sizeDelta = new Vector2(14f, 14f); mrt.anchoredPosition = new Vector2(-40f, 34f);
        mrt.localEulerAngles = new Vector3(0f, 0f, 45f);
        next.gameObject.SetActive(false);

        // 좌상단 스킵 버튼 (대사창의 입력 막는 화면보다 위에 그려지도록 Dialogue 다음 순서)
        Sprite btnSprite = FindAsset<Sprite>("UI_Button_Dark", "t:Sprite");
        Image skipImg = AddImage(Child("SkipButton", root.transform), btnSprite, btnSprite != null ? Color.white : new Color(0.04f, 0.16f, 0.08f, 0.95f));
        if (btnSprite != null) skipImg.type = Image.Type.Sliced;
        skipImg.raycastTarget = true;
        RectTransform srt = skipImg.rectTransform;
        srt.anchorMin = srt.anchorMax = new Vector2(0f, 1f); srt.pivot = new Vector2(0f, 1f);
        srt.sizeDelta = new Vector2(260f, 56f); srt.anchoredPosition = new Vector2(30f, -30f);
        Button skip = skipImg.gameObject.AddComponent<Button>();
        TMP_Text skipLabel = AddText(Child("Label", srt), font, 22f, textColor, TextAlignmentOptions.Center, "튜토리얼 건너뛰기");
        Stretch(skipLabel.rectTransform);
        skip.gameObject.SetActive(false);   // 튜토리얼 진행 중에만 매니저가 켬

        // 매니저 + 연결
        var tm = root.AddComponent<TutorialManager>();
        var so = new SerializedObject(tm);
        so.FindProperty("dialogueTable").objectReferenceValue = FindFirst<DialogueTableSO>("t:DialogueTableSO");
        so.FindProperty("canvas").objectReferenceValue = canvas;
        so.FindProperty("dialogueRoot").objectReferenceValue = dialogue.gameObject;
        so.FindProperty("portrait").objectReferenceValue = portrait;
        so.FindProperty("nameText").objectReferenceValue = name;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("nextMark").objectReferenceValue = next;
        so.FindProperty("skipButton").objectReferenceValue = skip;
        so.ApplyModifiedPropertiesWithoutUndo();
        dialogue.gameObject.SetActive(false);   // 평소엔 꺼져 있음

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[튜토리얼] 프리팹을 만들었습니다: {PrefabPath}" +
                  (frame == null ? " (UI_ScrollFrame 스프라이트를 찾지 못해 단색 대사창으로 만들었습니다)" : ""));

        if (Object.FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include) == null &&
            EditorUtility.DisplayDialog("튜토리얼 매니저 프리팹", "열린 씬에 배치할까요? (게임의 첫 씬, 메인 씬에 하나만 두면 됩니다)", "배치", "나중에"))
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(inst, "TutorialManager");
            EditorSceneManager.MarkSceneDirty(inst.scene);
            Selection.activeGameObject = inst;
        }
    }

    [MenuItem("Tools/DUAL DUEL/튜토리얼 최초 실행 상태로 초기화")]
    public static void ResetFirstRun()
    {
        TutorialManager.ResetFirstRun();
        Debug.Log("[튜토리얼] 튜토리얼 미완료 상태(0)로 초기화했습니다. 다음 게임 시작 때 튜토리얼이 나옵니다.");
    }

    // ------------------------------------------------------------------ helpers

    // 따옴표 안의 쉼표/줄바꿈/"" 를 처리하는 CSV 파서
    static List<string[]> ParseCsv(string text)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var cell = new StringBuilder();
        bool quoted = false;
        if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);   // BOM
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                else if (ch == '"') quoted = false;
                else cell.Append(ch);
                continue;
            }
            if (ch == '"') quoted = true;
            else if (ch == ',') { row.Add(cell.ToString()); cell.Clear(); }
            else if (ch == '\n' || ch == '\r')
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString()); cell.Clear();
                rows.Add(row.ToArray()); row.Clear();
            }
            else cell.Append(ch);
        }
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row.ToArray()); }
        return rows;
    }

    static RectTransform Child(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    static Image AddImage(RectTransform rt, Sprite sprite, Color color)
    {
        rt.gameObject.AddComponent<CanvasRenderer>();
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        return img;
    }

    static TMP_Text AddText(RectTransform rt, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align, string sample)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.text = sample;   // 프리팹에서 위치를 보기 위한 예시 글자 (실행 중엔 대사로 바뀜)
        return t;
    }

    static T FindAsset<T>(string name, string filter) where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets($"{name} {filter}"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<T>(p);
        }
        return null;
    }

    static T FindFirst<T>(string filter) where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets(filter))
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        return null;
    }

    static TMP_FontAsset FindFont()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            string n = Path.GetFileNameWithoutExtension(p);
            if (n.Contains("Stardust") || n.Contains("스타더스트") || n.StartsWith("PF")) return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p);
        }
        return TMP_Settings.defaultFontAsset;
    }
}
#endif
