using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 DB: ID 검색(BaseDatabaseSO) + 스테이지 타입(Normal / Elite / Boss)별 리스트 캐싱(BaseRankDatabaseSO)
/// 타입별 리스트는 appearStartStage 오름차순으로 정렬되어 있어 이진 탐색으로 등장 가능 범위를 구함
/// </summary>
[CreateAssetMenu(fileName = "MonsterDatabaseSO", menuName = "SO/Database/MonsterDatabaseSO")]
public class MonsterDatabaseSO : BaseRankDatabaseSO<int, StageType, MonsterSO>
{
    protected override int GetKey(MonsterSO item) => item.monsterID;
    protected override StageType GetRank(MonsterSO item) => item.monsterType;

    public MonsterSO GetMonsterById(int id) => GetByKey(id);

    /// <summary>
    /// 등장 스테이지 오름차순 정렬 비교 (같으면 ID 순). 에디터 변환 시에도 같은 기준 사용
    /// </summary>
    public static int CompareByAppearStage(MonsterSO a, MonsterSO b)
    {
        int compare = a.appearStartStage.CompareTo(b.appearStartStage);
        return compare != 0 ? compare : a.monsterID.CompareTo(b.monsterID);
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();

        // 에디터 변환 시 이미 정렬해서 저장하지만, 인스펙터에서 수동으로 추가/수정한 경우에도
        // 이진 탐색이 깨지지 않도록 타입별 리스트를 한 번 더 정렬 (Initialize 시 1회)
        SortListByAppearStage(StageType.Normal);
        SortListByAppearStage(StageType.Elite);
        SortListByAppearStage(StageType.Boss);
    }

    private void SortListByAppearStage(StageType type)
    {
        List<MonsterSO> list = GetListByRank(type);
        list?.Sort(CompareByAppearStage);
    }

    /// <summary>
    /// 스테이지 타입 + 현재 챕터/층 조건으로 등장 가능한 몬스터 중 하나를 랜덤으로 뽑음
    /// </summary>
    public MonsterSO GetRandomMonster(StageType stageType, int chapter, int floor)
    {
        // 1차: 스테이지 타입으로 거름
        if (!MonsterSO.IsBattleStageType(stageType))
        {
            Debug.LogWarning($"[MonsterDatabase] {stageType} 스테이지는 몬스터가 등장하지 않습니다.");
            return null;
        }

        List<MonsterSO> list = GetListByRank(stageType);
        if (list == null || list.Count == 0)
        {
            Debug.LogError($"[MonsterDatabase] {stageType} 타입 몬스터가 한 마리도 없습니다.");
            return null;
        }

        // 2차: 현재 스테이지 코드 이하에서 등장하는 몬스터 개수 (정렬된 리스트의 앞쪽 count개)
        int stageCode = MonsterSO.ToStageCode(chapter, floor);
        int count = CountAppearable(list, stageCode);

        if (count == 0)
        {
            // 데이터 누락으로 진행이 막히지 않도록 가장 먼저 등장하는 몬스터로 대체
            Debug.LogWarning($"[MonsterDatabase] {stageType} / {stageCode} 에 등장 가능한 몬스터가 없어 {list[0].monsterID}번으로 대체합니다.");
            return list[0];
        }
        
        // 3차: 등장 가능 범위 (0, count) 에서 랜덤 인덱스
        int randomIndex = Random.Range(0, count);
        MonsterSO picked = list[randomIndex];

        Debug.Log($"[MonsterDatabase] {stageType} / 스테이지 {stageCode} -> 후보 {count}마리 중 {picked.monsterID}({picked.monsterName}) 선택");
        return picked;
    }

    /// <summary>
    /// appearStartStage 오름차순 리스트에서 appearStartStage <= stageCode 인 원소 개수 (upper bound 이진 탐색)
    /// </summary>
    public static int CountAppearable(List<MonsterSO> sortedList, int stageCode)
    {
        int low = 0;
        int high = sortedList.Count;    // 탐색 범위 [low, high)

        while (low < high)
        {
            int mid = low + (high - low) / 2;

            if (sortedList[mid].appearStartStage <= stageCode)
                low = mid + 1;          // mid까지는 등장 가능 -> 오른쪽 탐색
            else
                high = mid;             // mid부터는 등장 불가 -> 왼쪽 탐색
        }

        return low;
    }
}
