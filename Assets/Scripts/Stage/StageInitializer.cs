using UnityEngine;

public class StageInitializer : MonoBehaviour
{
    private void Start()
    {
        InitCurrentStage();
    }

    /// <summary>
    /// 로드된 씬에서 현재 진입한 스테이지 노드의 타입과 층수를 확인하고 전투 스펙을 보정합니다.
    /// </summary>
    public void InitCurrentStage()
    {
        if (StageManager.Instance == null || StageManager.Instance.currentNode == null)
        {
            Debug.LogWarning("테스트 실행: 기본 Normal 스테이지로 임시 시작합니다.");
            return;
        }

        StageNode current = StageManager.Instance.currentNode;
        int currentFloor = current.floor;
        StageType type = current.stageType;

        // 기획서 4-3 난이도 계수: Normal (HP +10%, ATK +8%), Elite (HP +15%, ATK +12%)
        float hpScale = 0.1f;
        float atkScale = 0.08f;

        if (type == StageType.Elite)
        {
            hpScale = 0.15f;
            atkScale = 0.12f;
        }
        else if (type == StageType.Boss)
        {
            hpScale = 0.25f;
            atkScale = 0.20f;
        }

        float totalHpMultiplier = 1f + (hpScale * currentFloor);
        float totalAtkMultiplier = 1f + (atkScale * currentFloor);

        Debug.Log($"<color=cyan>[스테이지 초기화]</color> 층수: {currentFloor}, 타입: {type} | HP 배율: {totalHpMultiplier:F2}, ATK 배율: {totalAtkMultiplier:F2}");

        // TODO: BattleManager.Instance.StartBattleWithModifiers(totalHpMultiplier, totalAtkMultiplier);
    }

    /// <summary>
    /// 전투 승리 버튼(테스트용) 또는 BattleManager의 승리 이벤트에서 호출
    /// </summary>
    public void OnVictoryTestButton()
    {
        StageManager.Instance.CompleteCurrentStage();
    }
}