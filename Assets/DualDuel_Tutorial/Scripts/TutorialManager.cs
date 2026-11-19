using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 대사창 (프리팹). 씬이 바뀌어도 유지된다.
///   메인 씬 시작 버튼 -> StartTutorialIfFirstRun()  (아직 저장 안 함)
///   마지막 대사까지 읽으면 EndTutorial() -> PlayerPrefs 1 저장 / 좌상단 스킵 버튼 -> 즉시 1 저장
///   각 씬의 TutorialSceneSteps 가 정해진 순서대로 Play(대사 id) 를 부른다.
///   게임 코드는 행동이 일어난 곳에서 TutorialManager.Notify("행동이름") 만 부르면 된다.
/// 입력: 클릭 / Enter / Space -> 타이핑 중이면 전부 표시, 다 표시됐으면 다음 대사 (nextId -1 이면 닫힘)
/// </summary>
public class TutorialManager : MonoBehaviour
{
    /// <summary>
    /// 씬이 바뀌어도 유지되는 매니저 (여러 씬에 프리팹을 둬도 처음 것 하나만 남음)
    /// </summary>
    public static TutorialManager Instance { get; private set; }

    /// <summary>
    /// 게임 코드에서 Notify 한 행동 이름이 전달되는 이벤트 (TutorialSceneSteps 가 구독)
    /// </summary>
    public static event Action<string> Notified;

    private const string FirstRunKey = "TutorialStarted";   // 0 = 튜토리얼 미완료, 1 = 끝까지 봤거나 스킵함

    [Header("데이터")]
    [SerializeField] private DialogueTableSO dialogueTable;

    [Header("UI (프리팹에 연결됨)")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Graphic nextMark;
    [Tooltip("좌상단 튜토리얼 건너뛰기 버튼 (튜토리얼 진행 중에만 보임)")]
    [SerializeField] private Button skipButton;

    [Header("연출")]
    [SerializeField] private float charsPerSecond = 30f;
    [Tooltip("HUD/팝업(100) 위, CRT 스캔라인(200) 아래")]
    [SerializeField] private int sortingOrder = 195;

    [Header("테스트")]
    [Tooltip("켜면 PlayerPrefs 와 상관없이 매번 튜토리얼을 진행 (빌드 전에 꺼주세요)")]
    [SerializeField] private bool alwaysRunForTest;

    [Header("스탠딩 캐시 (처음 한 번 Resources 에서 채움, 읽기 전용)")]
    [SerializeField] private List<Sprite> portraitSprites = new List<Sprite>();

    /// <summary>
    /// 이번 실행에서 튜토리얼이 진행 중인지
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 대사창이 떠 있는 동안 true. OnMouseDown 처럼 UI 가 아닌 입력 코드에서 확인해 무시하세요.
    /// </summary>
    public static bool IsBlockingInput => Instance != null && Instance.isOpen;

    private readonly Dictionary<string, Sprite> portraitByKey = new Dictionary<string, Sprite>();
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
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipTutorial);
            skipButton.gameObject.SetActive(false);
        }
        if (dialogueTable == null) Debug.LogError("[Tutorial] 대사 테이블(DialogueTable)이 연결되지 않았습니다.");
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
    /// 메인 씬의 게임 시작 버튼에서 호출. 튜토리얼을 끝까지 본 적이 없으면(0) 튜토리얼을 시작합니다.
    /// 여기서는 저장하지 않으므로 중간에 게임을 끄면 다음에 다시 나옵니다.
    /// </summary>
    public void StartTutorialIfFirstRun()
    {
        bool done = PlayerPrefs.GetInt(FirstRunKey, 0) == 1;
        if (done && !alwaysRunForTest)
        {
            Debug.Log("[Tutorial] 이미 튜토리얼을 마쳐서 건너뜁니다. (다시 보려면 Tools > DUAL DUEL > 튜토리얼 최초 실행 상태로 초기화)");
            return;
        }
        IsRunning = true;
        if (skipButton != null) skipButton.gameObject.SetActive(true);
        Debug.Log("[Tutorial] 튜토리얼 시작");
    }

    /// <summary>
    /// 튜토리얼을 끝까지 봤을 때 (마지막 씬의 마지막 단계가 끝나면 TutorialSceneSteps 가 호출). PlayerPrefs 를 1 로 저장
    /// </summary>
    public void EndTutorial()
    {
        Finish();
        Debug.Log("[Tutorial] 튜토리얼 완료");
    }

    /// <summary>
    /// 튜토리얼 건너뛰기 (좌상단 스킵 버튼). 즉시 PlayerPrefs 를 1 로 저장하고 대사창을 닫음
    /// </summary>
    public void SkipTutorial()
    {
        Finish();
        Debug.Log("[Tutorial] 튜토리얼 건너뜀");
    }

    /// <summary>
    /// startId 대사부터 nextId 를 따라 재생. nextId 가 -1 인 대사를 읽으면 닫히고 finished 호출
    /// </summary>
    public void Play(int startId, Action finished = null)
    {
        if (dialogueTable == null || !dialogueTable.TryGet(startId, out DialogueData line))
        {
            Debug.LogWarning($"[Tutorial] 대사 id {startId} 를 찾을 수 없습니다. (대사 CSV 를 가져왔는지, 매니저에 테이블이 연결됐는지 확인)");
            finished?.Invoke();
            return;
        }
        onFinished = finished;
        isOpen = true;
        openedFrame = Time.frameCount;
        AttachCamera();
        if (dialogueRoot != null) dialogueRoot.SetActive(true);
        Show(line);
    }

    /// <summary>
    /// 게임 코드에서 플레이어 행동이 일어났을 때 호출 (예: Notify("StageClicked")). 튜토리얼 중이 아니면 무시됨
    /// </summary>
    public static void Notify(string action)
    {
        if (Instance == null || !Instance.IsRunning || string.IsNullOrEmpty(action)) return;
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

    /// <summary>
    /// PlayerPrefs 를 최초 실행 상태(0)로 되돌림 (테스트용)
    /// </summary>
    public static void ResetFirstRun()
    {
        PlayerPrefs.DeleteKey(FirstRunKey);
        PlayerPrefs.Save();
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
        done?.Invoke();
    }

    // 대사 한 줄 표시: 이름, 스탠딩(없으면 끔), 타이핑 시작
    private void Show(DialogueData line)
    {
        current = line;
        bool hasName = !string.IsNullOrWhiteSpace(line.characterName);
        if (nameText != null) { nameText.gameObject.SetActive(hasName); nameText.text = line.characterName; }

        Sprite sprite = GetPortrait(line.portraitPath, line.expression);
        if (portrait != null) { portrait.gameObject.SetActive(sprite != null); portrait.sprite = sprite; }

        if (nextMark != null) nextMark.gameObject.SetActive(false);
        if (bodyText != null) { bodyText.text = (line.text ?? "").Replace("\\n", "\n"); bodyText.maxVisibleCharacters = 0; }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Type());
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

    // 끝(완료/스킵) 공통: 1 저장, 진행 중 해제, 대사창/스킵 버튼 닫기
    private void Finish()
    {
        PlayerPrefs.SetInt(FirstRunKey, 1);
        PlayerPrefs.Save();
        IsRunning = false;
        Close();
        if (skipButton != null) skipButton.gameObject.SetActive(false);
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

    // CRT 후처리를 받도록 메인 카메라 기준 캔버스로 (카메라가 없으면 오버레이)
    private void AttachCamera()
    {
        if (canvas == null) return;
        Camera cam = Camera.main;
        canvas.renderMode = cam != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = cam;
        canvas.planeDistance = 50f;
        canvas.sortingOrder = sortingOrder;
    }
}
