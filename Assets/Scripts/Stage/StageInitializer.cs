using UnityEngine;

public class StageInitializer : MonoBehaviour
{
    // Start()의 자체 실행은 주석 처리 또는 제거하여 StageManager의 호출 순서에 맞춥니다.

    /// <summary>
    /// StageManager가 현재 진입한 노드 데이터를 넘겨주며 호출하는 초기화 함수
    /// </summary>
    /// <param name="current">진입한 스테이지 노드 정보</param>
    public void InitCurrentStage(StageNode current)
    {
        if (current == null)
        {
            Debug.LogWarning("[StageInitializer] 진입 노드 정보가 없습니다. 기본 Normal 0층으로 대체합니다.");
            return;
        }

        int currentFloor = current.floor;
        StageType type = current.stageType;

        // 기획서 4-3 난이도 계수: Normal (HP +10%, ATK +8%), Elite (HP +15%, ATK +12%), Boss (HP +25%, ATK +20%)
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

        Debug.Log($"<color=cyan>[전투 스테이지 초기화]</color> {currentFloor}층 {type} | HP 배율: {totalHpMultiplier:F2}, ATK 배율: {totalAtkMultiplier:F2}");

        // TODO: BattleManager에 보정값 전달
        // BattleManager.Instance?.StartBattleWithModifiers(totalHpMultiplier, totalAtkMultiplier);
    }

    /// <summary>
    /// 단독 테스트 시 매개변수 없이 호출할 수 있도록 지원하는 오버로딩 함수
    /// </summary>
    public void InitCurrentStage()
    {
        StageNode node = (StageManager.Instance != null) ? StageManager.Instance.currentNode : null;
        InitCurrentStage(node);
    }

    /// <summary>
    /// 전투 승리 테스트 버튼 등에서 호출
    /// </summary>
    public void OnVictoryTestButton()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.CompleteCurrentStage();
        }
    }
}