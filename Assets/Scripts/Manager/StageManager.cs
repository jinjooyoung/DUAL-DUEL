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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (floors == null || floors.Count == 0)
        {
            InitializeNewRun();
        }
    }

    /// <summary>
    /// 새로운 런 시작 시 호출되어 맵 데이터를 초기화하고 생성합니다.
    /// </summary>
    public void InitializeNewRun()
    {
        // 기획서 4-2 기준 가중치: Normal: 45, Elite: 30, Upgrade: 15, DeleteHeal: 10
        Dictionary<StageType, float> weights = new Dictionary<StageType, float>
        {
            { StageType.Normal, 45f },
            { StageType.Elite, 30f },
            { StageType.Upgrade, 15f },
            { StageType.DeleteHeal, 10f }
        };

        if (generator == null) generator = gameObject.AddComponent<StageGenerator>();
        floors = generator.GenerateMap(mapWidth, mapHeight, weights);
        currentNode = null;
    }

    /// <summary>
    /// UI에서 스테이지 노드 클릭 시 호출되어 이동 가능 여부를 판단하고 진입 루틴을 실행합니다.
    /// </summary>
    public bool TryMoveToNode(StageNode targetNode)
    {
        if (!targetNode.canGo)
        {
            Debug.Log("<color=red>이동 불가 스테이지입니다!</color>");
            // TODO: SoundManager.PlayNegativeSfx();
            return false;
        }

        // 이동 성공
        // TODO: SoundManager.PlayPositiveSfx();
        StartCoroutine(EnterStageRoutine(targetNode));
        return true;
    }

    /// <summary>
    /// 스테이지 진입 프로세스: 선택 상태 확정 -> 페이드 아웃 -> 씬 로드 -> 완료 대기
    /// </summary>
    private IEnumerator EnterStageRoutine(StageNode targetNode)
    {
        currentNode = targetNode;
        currentNode.isVisited = true;

        // 같은 층의 모든 노드 진입 불가 처리
        foreach (var node in floors[targetNode.floor])
        {
            node.canGo = false;
        }

        // 로드할 씬 이름 결정
        string targetSceneName = GetSceneNameForType(targetNode.stageType);

        // TODO: FadeManager.FadeOut(0.5f);
        yield return new WaitForSeconds(0.5f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 새 씬 로드 완료 후 BGM 및 페이드인 처리는 새 씬의 StageInitializer가 담당
    }

    /// <summary>
    /// 전투 승리 또는 이벤트 종료 후 호출되어 다음 노드를 활성화하고 맵 씬으로 복귀합니다.
    /// </summary>
    public void CompleteCurrentStage()
    {
        if (currentNode != null)
        {
            // 다음으로 연결된 상위 노드들만 canGo = true로 개방
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
                return "BattleScene"; // 오늘 단계에서는 모든 전투 타입을 동일한 BattleScene으로 라우팅
            case StageType.Upgrade:
            case StageType.DeleteHeal:
                return "EventScene";  // 비전투 씬 (또는 임시 BattleScene)
            default:
                return "BattleScene";
        }
    }
}