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
    public StageNode currentNode; // 현재 선택/진입한 노드

    private StageNodeUI selectedNodeUI;

    [Header("연결 컴포넌트")]
    [SerializeField] private StageGenerator generator;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
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
        if (SceneManager.GetActiveScene().name == "StageSelectScene")
        {
            InitializeStageSelectScene();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "StageSelectScene")
        {
            InitializeStageSelectScene();
        }
        else if (scene.name == "BattleScene" || scene.name == "RestScene" || scene.name == "ShopScene")
        {
            InitializeGameplayScene();
        }
    }

    public void InitializeStageSelectScene()
    {
        if (floors == null || floors.Count == 0)
        {
            CreateNewRunData();
        }

        currentNode = null;
        selectedNodeUI = null;

        StageMapUI mapUI = FindFirstObjectByType<StageMapUI>();
        if (mapUI != null)
        {
            mapUI.RenderMap(floors);
        }

        StageInfoPanel panel = FindFirstObjectByType<StageInfoPanel>();
        if (panel != null)
        {
            panel.SetStartButtonInteractable(false);
        }
    }

    private void InitializeGameplayScene()
    {
        StageInitializer initializer = FindFirstObjectByType<StageInitializer>();
        if (initializer != null)
        {
            initializer.InitCurrentStage(currentNode);
        }
    }

    public void CreateNewRunData()
    {
        // 6개 타입 비율 설정
        Dictionary<StageType, float> weights = new Dictionary<StageType, float>
        {
            { StageType.Normal, 40f },
            { StageType.Elite, 20f },
            { StageType.Rest, 15f },
            { StageType.Shop, 10f },
            { StageType.Random, 15f }
        };

        floors = generator.GenerateMap(mapWidth, mapHeight, weights);
        currentNode = null;
    }

    /// <summary>
    /// 노드를 클릭했을 때 선택 상태 갱신 및 정보 패널 표시
    /// </summary>
    public void SelectNode(StageNodeUI nodeUI)
    {
        if (selectedNodeUI != null)
        {
            selectedNodeUI.SetSelected(false);
        }

        selectedNodeUI = nodeUI;
        currentNode = nodeUI.nodeData;
        selectedNodeUI.SetSelected(true);

        StageInfoPanel infoPanel = FindFirstObjectByType<StageInfoPanel>();
        if (infoPanel != null)
        {
            infoPanel.SetupPanel(currentNode);
        }
    }

    /// <summary>
    /// 정보 패널의 시작 버튼 클릭 시 실제 씬 진입
    /// </summary>
    public void StartCurrentSelectedStage()
    {
        if (currentNode == null || !currentNode.canGo) return;

        currentNode.isVisited = true;

        // 동일 층 노드 비활성화
        foreach (var node in floors[currentNode.floor])
        {
            node.canGo = false;
        }

        // Random 노드 처리: Normal, Elite, Rest, Shop 중 하나를 무작위 결정
        StageType executionType = currentNode.stageType;
        if (executionType == StageType.Random)
        {
            StageType[] pool = { StageType.Normal, StageType.Elite, StageType.Rest, StageType.Shop };
            executionType = pool[Random.Range(0, pool.Length)];
            Debug.Log($"<color=magenta>[StageManager] 랜덤 노드 결정: {executionType}</color>");
        }

        string targetScene = GetSceneNameForType(executionType);
        SceneManager.LoadScene(targetScene);
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
            case StageType.Rest:
                return "RestScene";
            case StageType.Shop:
                return "ShopScene";
            default:
                return "BattleScene";
        }
    }
}