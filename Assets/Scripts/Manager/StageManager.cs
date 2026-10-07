using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("맵 생성 옵션")]
    public int mapWidth = 5;
    public int mapHeight = 10;

    [Header("런타임 맵 데이터")]
    public List<List<StageNode>> floors = new List<List<StageNode>>();
    public StageNode currentNode;

    [Header("연결 컴포넌트")]
    [SerializeField] private StageGenerator generator;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); // 최상위 루트 보장
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (generator == null) generator = GetComponent<StageGenerator>();
        if (generator == null) generator = gameObject.AddComponent<StageGenerator>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Start()
    {
        // 첫 진입 씬이 StageSelectScene이면 무조건 생성 및 렌더링 강제 실행
        if (SceneManager.GetActiveScene().name == "StageSelectScene")
        {
            Debug.Log("<color=yellow>[StageManager] StageSelectScene 최초 실행 감지</color>");
            InitializeStageSelectScene();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"<color=white>[StageManager] 씬 로드 완료: {scene.name}</color>");

        if (scene.name == "StageSelectScene")
        {
            InitializeStageSelectScene();
        }
        else if (scene.name == "BattleScene" || scene.name == "EventScene")
        {
            InitializeGameplayScene();
        }
    }

    /// <summary>
    /// 스테이지 선택 씬 중앙 초기화: 맵 생성 보장 후 StageMapUI 직접 호출
    /// </summary>
    public void InitializeStageSelectScene()
    {
        // 데이터가 비어있거나 생성된 적이 없다면 즉시 생성
        if (floors == null || floors.Count == 0)
        {
            CreateNewRunData();
        }

        // 씬 내의 StageMapUI를 찾아 데이터 주입 및 렌더링 명령
        StageMapUI mapUI = FindFirstObjectByType<StageMapUI>();
        if (mapUI != null)
        {
            Debug.Log("<color=cyan>[StageManager] StageMapUI를 찾아 렌더링을 명령합니다.</color>");
            mapUI.RenderMap(floors);
        }
        else
        {
            Debug.LogError("<color=red>[StageManager] 씬에서 StageMapUI를 찾지 못했습니다! Content 오브젝트에 붙어있는지 확인하세요.</color>");
        }
    }

    /// <summary>
    /// 게임플레이(전투/이벤트) 씬 초기화
    /// </summary>
    private void InitializeGameplayScene()
    {
        StageInitializer initializer = FindFirstObjectByType<StageInitializer>();
        if (initializer != null)
        {
            initializer.InitCurrentStage(currentNode);
        }
    }

    /// <summary>
    /// 신규 런 데이터 생성 실행
    /// </summary>
    public void CreateNewRunData()
    {
        Debug.Log("<color=yellow>[StageManager] 새 맵 생성을 시작합니다...</color>");

        Dictionary<StageType, float> weights = new Dictionary<StageType, float>
        {
            { StageType.Normal, 45f },
            { StageType.Elite, 30f },
            { StageType.Upgrade, 15f },
            { StageType.DeleteHeal, 10f }
        };

        floors = generator.GenerateMap(mapWidth, mapHeight, weights);
        currentNode = null;

        int totalNodeCount = 0;
        for (int i = 0; i < floors.Count; i++) totalNodeCount += floors[i].Count;

        Debug.Log($"<color=green>[StageManager] 맵 생성 완료! 총 {floors.Count}개 층, {totalNodeCount}개 노드 생성됨.</color>");
    }

    public bool TryMoveToNode(StageNode targetNode)
    {
        if (!targetNode.canGo)
        {
            Debug.Log("<color=red>[StageManager] 진입 불가능한 노드입니다.</color>");
            return false;
        }

        StartCoroutine(EnterStageRoutine(targetNode));
        return true;
    }

    private IEnumerator EnterStageRoutine(StageNode targetNode)
    {
        currentNode = targetNode;
        currentNode.isVisited = true;

        foreach (var node in floors[targetNode.floor])
        {
            node.canGo = false;
        }

        string targetSceneName = GetSceneNameForType(targetNode.stageType);
        yield return new WaitForSeconds(0.2f);

        SceneManager.LoadScene(targetSceneName);
    }

    public void CompleteCurrentStage()
    {
        if (currentNode != null)
        {
            foreach (var next in currentNode.nextStages)
            {
                next.canGo = true;
            }
        }

        SceneManager.LoadScene("StageSelectScene");
    }

    private string GetSceneNameForType(StageType type)
    {
        switch (type)
        {
            case StageType.Normal:
            case StageType.Elite:
            case StageType.Boss:
                return "BattleScene";
            default:
                return "BattleScene";
        }
    }
}