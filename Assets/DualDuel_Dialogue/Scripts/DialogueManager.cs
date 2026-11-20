using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 다이얼로그 대사창 (프리팹). 씬이 바뀌어도 유지된다.
///   각 씬의 DialogueSceneSteps 가 정해진 순서대로 대사 묶음을 재생한다.
///   게임 코드는 행동이 일어난 곳에서 DialogueManager.Notify("행동이름") 만 부르면 된다.
///   대사의 action 열은 그 대사가 뜨는 순간 LineAction 이벤트로 게임 코드에 전달된다 (Glitch 는 기본 제공).
/// 본 기록은 DialogueProgress 가 영구 저장한다 (게임 진행 저장과 별개).
/// 입력: 클릭 / Enter / Space -> 타이핑 중이면 전부 표시, 다 표시됐으면 다음 대사 (nextId -1 이면 닫힘)
/// 대사창이 열려 있을 때 다른 묶음이 요청되면 덮어쓰지 않고 줄을 세워, 지금 묶음이 끝난 뒤 차례로 재생한다.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    /// <summary>
    /// 씬이 바뀌어도 유지되는 매니저 (여러 씬에 프리팹을 둬도 처음 것 하나만 남음)
    /// </summary>
    public static DialogueManager Instance { get; private set; }

    /// <summary>
    /// 게임 코드에서 Notify 한 행동 이름이 전달되는 이벤트 (DialogueSceneSteps 가 구독)
    /// </summary>
    public static event Action<string> Notified;

    /// <summary>
    /// action 열이 있는 대사가 뜨는 순간 (동작 이름, 그 대사). 보스의 카드 강탈 같은 게임 코드를 여기에 연결
    /// </summary>
    public static event Action<string, DialogueData> LineAction;

    [Header("데이터")]
    [SerializeField] private DialogueTableSO dialogueTable;

    [Header("UI (프리팹에 연결됨)")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Graphic nextMark;

    [Header("연출")]
    [SerializeField] private float charsPerSecond = 30f;
    [Tooltip("HUD/팝업(100) 위, CRT 스캔라인(200) 아래")]
    [SerializeField] private int sortingOrder = 195;
    [Tooltip("켜면 카메라와 상관없이 화면 맨 위에 그림 (대사창이 안 보일 때. 대신 CRT 스캔라인이 위에 안 덮임)")]
    [SerializeField] private bool forceOverlay;

    [Header("스탠딩 캐시 (처음 한 번 Resources 에서 채움, 읽기 전용)")]
    [SerializeField] private List<Sprite> portraitSprites = new List<Sprite>();

    /// <summary>
    /// 대사창이 떠 있는 동안 true. OnMouseDown 처럼 UI 가 아닌 입력 코드에서 확인해 무시하세요.
    /// </summary>
    public static bool IsBlockingInput => Instance != null && Instance.isOpen;

    /// <summary>
    /// 대사창이 떠 있는지
    /// </summary>
    public bool IsOpen => isOpen;

    private readonly Dictionary<string, Sprite> portraitByKey = new Dictionary<string, Sprite>();

    // 대사창이 열려 있는 동안 요청된 묶음 (지금 묶음이 끝나면 차례로 재생)
    private struct PlayRequest { public int startId; public Action finished; }
    private readonly Queue<PlayRequest> queue = new Queue<PlayRequest>();
    private DialogueData current;
    private Action onFinished;
    private Coroutine routine;
    private bool isOpen, isTyping;
    private int openedFrame = -1;

    // ------------------------------------------------------------------ 수명

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if (dialogueRoot != null) dialogueRoot.SetActive(false);
        if (dialogueTable == null) Debug.LogError("[Dialogue] 대사 테이블(DialogueTable)이 연결되지 않았습니다.");
        CachePortraits();
        AttachCamera();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ------------------------------------------------------------------ 외부 호출

    /// <summary>
    /// startId 묶음을 재생. 시작하는 순간 '봤음' 으로 영구 저장됩니다.
    /// nextId 가 -1 인 대사를 읽으면 닫히고 finished 호출.
    /// 이미 다른 묶음이 떠 있으면 덮어쓰지 않고 줄을 세웠다가 그 묶음이 끝난 뒤 재생합니다.
    /// </summary>
    public void Play(int startId, Action finished = null)
    {
        if (isOpen)
        {
            queue.Enqueue(new PlayRequest { startId = startId, finished = finished });
            Debug.Log($"[Dialogue] 묶음 {startId} 대기 (지금 묶음이 끝나면 재생, 대기 {queue.Count}개)");
            return;
        }

        if (dialogueTable == null || !dialogueTable.TryGet(startId, out DialogueData line))
        {
            Debug.LogWarning($"[Dialogue] 대사 id {startId} 를 찾을 수 없습니다. (CSV 를 가져왔는지, 매니저에 테이블이 연결됐는지 확인)");
            finished?.Invoke();
            return;
        }
        DialogueProgress.MarkSeen(startId);   // 보는 도중 강제 종료해도 '봤음'
        onFinished = finished;
        isOpen = true;
        openedFrame = Time.frameCount;
        AttachCamera();
        if (dialogueRoot != null) dialogueRoot.SetActive(true);
        Debug.Log($"[Dialogue] 묶음 {startId} 재생");
        Show(line);
    }

    /// <summary>
    /// 처음 보면 startId, 이미 봤으면 seenStartId 를 재생 (seenStartId 가 -1 이면 이미 봤을 때 아무것도 안 함).
    /// 씬 단계 없이 코드에서 바로 재생할 때 사용. 재생할 게 없으면 finished 를 바로 호출
    /// </summary>
    public void PlayBlock(int startId, int seenStartId = DialogueData.END, Action finished = null)
    {
        int id = DialogueProgress.HasSeen(startId) ? seenStartId : startId;
        if (id < 0) { finished?.Invoke(); return; }
        Play(id, finished);
    }

    /// <summary>
    /// 게임 코드에서 플레이어 행동이 일어났을 때 호출 (예: Notify("StageClicked")). 기다리는 단계가 없으면 무시됨
    /// </summary>
    public static void Notify(string action)
    {
        if (string.IsNullOrEmpty(action)) return;
        Debug.Log($"[Dialogue] Notify({action})");
        Notified?.Invoke(action);
    }

    /// <summary>
    /// 캐싱된 스탠딩 꺼내기 (경로_표정 -> 경로). 없으면 null. Resources 에는 다시 접근하지 않음
    /// </summary>
    public Sprite GetPortrait(string path, string expression = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (!string.IsNullOrWhiteSpace(expression) && portraitByKey.TryGetValue($"{path}_{expression}", out Sprite s) && s != null) return s;
        return portraitByKey.TryGetValue(path, out Sprite p) ? p : null;
    }

    // ------------------------------------------------------------------ 진행

    private void Update()
    {
        if (!isOpen || Time.frameCount == openedFrame) return;   // 연 바로 그 입력으로 넘어가지 않게
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            Advance();
    }

    // 타이핑 중이면 전부 표시, 아니면 다음 대사 또는 닫기
    private void Advance()
    {
        if (isTyping) { ShowAll(); return; }
        if (current != null && current.nextId >= 0 && dialogueTable.TryGet(current.nextId, out DialogueData next)) { Show(next); return; }

        Action done = onFinished;
        Close();
        done?.Invoke();      // 이 안에서 다음 묶음을 바로 Play 할 수도 있음 (그러면 대기열은 그 뒤에)
        PlayNextQueued();
    }

    // 대기 중인 묶음이 있고 대사창이 비어 있으면 다음 묶음 재생
    private void PlayNextQueued()
    {
        if (isOpen || queue.Count == 0) return;
        PlayRequest next = queue.Dequeue();
        Play(next.startId, next.finished);
    }

    // 대사 한 줄 표시: 이름, 스탠딩(없으면 끔), action 실행, 타이핑 시작
    private void Show(DialogueData line)
    {
        current = line;
        bool hasName = !string.IsNullOrWhiteSpace(line.characterName);
        if (nameText != null) { nameText.gameObject.SetActive(hasName); nameText.text = line.characterName; }

        Sprite sprite = GetPortrait(line.portraitPath, line.expression);
        if (portrait != null) { portrait.gameObject.SetActive(sprite != null); portrait.sprite = sprite; }

        if (nextMark != null) nextMark.gameObject.SetActive(false);
        if (bodyText != null) { bodyText.text = (line.text ?? "").Replace("\\n", "\n"); bodyText.maxVisibleCharacters = 0; }

        RunAction(line);

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Type());
    }

    // action 열: Glitch 는 바로 글리치, 나머지는 게임 코드로 전달
    private void RunAction(DialogueData line)
    {
        if (string.IsNullOrWhiteSpace(line.action)) return;
        string action = line.action.Trim();
        Debug.Log($"[Dialogue] 대사 {line.id} action: {action}");
        if (action == "Glitch") { SceneTransition.Pulse(); return; }
        LineAction?.Invoke(action, line);
    }

    // 한 글자씩 표시 (timeScale 무시)
    private IEnumerator Type()
    {
        isTyping = true;
        if (bodyText != null)
        {
            bodyText.ForceMeshUpdate();
            int total = bodyText.textInfo.characterCount;
            for (float shown = 0f; shown < total; shown += Time.unscaledDeltaTime * charsPerSecond)
            {
                bodyText.maxVisibleCharacters = (int)shown;
                yield return null;
            }
        }
        ShowAll();
    }

    // 전부 표시하고 다음 표시를 깜빡임
    private void ShowAll()
    {
        if (routine != null) StopCoroutine(routine);
        isTyping = false;
        if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
        if (nextMark != null) routine = StartCoroutine(Blink());
    }

    // 다음 대사로 넘어갈 수 있다는 표시
    private IEnumerator Blink()
    {
        nextMark.gameObject.SetActive(true);
        while (true)
        {
            nextMark.canvasRenderer.SetAlpha((Time.unscaledTime * 2f % 1f) < 0.6f ? 1f : 0.2f);
            yield return null;
        }
    }

    // 대사창 닫기
    private void Close()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        isOpen = isTyping = false;
        current = null;
        onFinished = null;
        if (dialogueRoot != null) dialogueRoot.SetActive(false);
    }

    // ------------------------------------------------------------------ 준비

    // 대사 테이블에 나오는 스탠딩 경로를 Resources 에서 한 번만 읽어 둠 (없는 경로는 null 로 기록)
    private void CachePortraits()
    {
        if (dialogueTable == null) return;
        foreach (DialogueData line in dialogueTable.lines)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.portraitPath)) continue;
            CacheOnce(line.portraitPath);
            if (!string.IsNullOrWhiteSpace(line.expression)) CacheOnce($"{line.portraitPath}_{line.expression}");
        }
    }

    // 한 경로를 한 번만 읽음
    private void CacheOnce(string key)
    {
        if (portraitByKey.ContainsKey(key)) return;
        Sprite sprite = Resources.Load<Sprite>(key);
        portraitByKey[key] = sprite;
        if (sprite != null && !portraitSprites.Contains(sprite)) portraitSprites.Add(sprite);
    }

    // 씬이 바뀌면 새 카메라에 붙임
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachCamera();

    // CRT 후처리를 받도록 메인 카메라 기준 캔버스로. 그 카메라가 대사창을 그릴 수 없으면 오버레이로
    private void AttachCamera()
    {
        if (canvas == null) return;
        canvas.sortingOrder = sortingOrder;
        Camera cam = Camera.main;

        string reason = null;
        if (forceOverlay) reason = "Force Overlay 켜짐";
        else if (cam == null) reason = "MainCamera 태그가 붙은 카메라가 없음";
        else if ((cam.cullingMask & (1 << gameObject.layer)) == 0) reason = $"카메라 '{cam.name}' 의 Culling Mask 에 '{LayerMask.LayerToName(gameObject.layer)}' 레이어가 없음";

        if (reason != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (!forceOverlay) Debug.LogWarning($"[Dialogue] {reason} -> 대사창을 오버레이로 그립니다.");
            return;
        }
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = Mathf.Clamp(50f, cam.nearClipPlane + 0.1f, Mathf.Max(cam.nearClipPlane + 0.2f, cam.farClipPlane - 0.1f));
    }
}
