using System;
using System.Collections.Generic;
using UnityEngine;

// 몬스터 기믹 타입 (파라미터는 항상 정수)
// 정수값이 에셋에 저장되므로 순서를 바꾸거나 중간에 끼워넣지 말 것 (새 타입은 맨 뒤에 추가)
public enum GimmickType
{
    None = 0,
    BlindSlot = 1,
    ScaleSlotValue = 2,
    InjectCurseCard = 3,
    RestrictCardType = 4
}

/// <summary>
/// 엑셀 Monster 시트 한 행을 그대로 받는 중간 데이터
/// </summary>
[Serializable]
public class MonsterData
{
    public int monsterID;
    public string monsterName;      // 로컬라이징 키 (예: MONSTERNAME_2000)
    public string monsterType;      // Normal / Elite / Boss
    public int appearStartStage;    // 등장 시작 스테이지 (챕터 * 100 + 층. 예: 203 = 2챕터 3층)
    public int maxHp;

    public string gimmickType1;
    public int gimmickParam1;
    public string gimmickType2;
    public int gimmickParam2;
}

[CreateAssetMenu(fileName = "MonsterSO", menuName = "SO/DataSO/MonsterSO")]
public class MonsterSO : ScriptableObject
{
    [Header("기본 정보")]
    public int monsterID;
    public string monsterName;      // 로컬라이징 키
    public StageType monsterType;   // 등장하는 전투 스테이지 타입 (Normal / Elite / Boss만 사용)

    [Header("등장 조건")]
    [Tooltip("챕터 * 100 + 층 (예: 103 = 1챕터 3층부터 등장)")]
    public int appearStartStage;

    [Header("스탯")]
    public int maxHp;

    [Header("기믹")]
    public GimmickType gimmickType1;
    public int gimmickParam1;
    public GimmickType gimmickType2;
    public int gimmickParam2;

    public int AppearChapter => appearStartStage / 100;
    public int AppearFloor => appearStartStage % 100;

    /// <summary>
    /// 챕터, 층 -> 스테이지 코드 (2챕터 4층 -> 204). 층은 0~99 범위여야 함
    /// </summary>
    public static int ToStageCode(int chapter, int floor) => chapter * 100 + floor;

    /// <summary>
    /// 몬스터가 등장하는 전투 스테이지 타입인지
    /// </summary>
    public static bool IsBattleStageType(StageType type)
    {
        return type == StageType.Normal || type == StageType.Elite || type == StageType.Boss;
    }

    /// <summary>
    /// 엑셀 MonsterData -> SO 값 세팅
    /// </summary>
    public void SetupFromData(MonsterData data)
    {
        monsterID = data.monsterID;
        monsterName = data.monsterName;
        appearStartStage = data.appearStartStage;
        maxHp = data.maxHp;

        monsterType = ParseEnum(data.monsterType, StageType.Normal, "monsterType");
        if (!IsBattleStageType(monsterType))
        {
            Debug.LogWarning($"[MonsterSO] {monsterID}번 몬스터 monsterType '{data.monsterType}' 는 Normal / Elite / Boss 중 하나여야 해서 Normal로 설정합니다.");
            monsterType = StageType.Normal;
        }

        if (AppearChapter < 1 || AppearFloor < 1)
            Debug.LogWarning($"[MonsterSO] {monsterID}번 몬스터 appearStartStage {appearStartStage} 형식 확인 필요 (챕터 * 100 + 층)");

        gimmickType1 = ParseEnum(data.gimmickType1, GimmickType.None, "gimmickType1");
        gimmickParam1 = data.gimmickParam1;
        gimmickType2 = ParseEnum(data.gimmickType2, GimmickType.None, "gimmickType2");
        gimmickParam2 = data.gimmickParam2;
    }

    private T ParseEnum<T>(string raw, T fallback, string columnName) where T : struct
    {
        if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse(raw.Trim(), true, out T result) && Enum.IsDefined(typeof(T), result))
            return result;

        if (!string.IsNullOrWhiteSpace(raw))
            Debug.LogWarning($"[MonsterSO] {monsterID}번 몬스터 {columnName} '{raw}' 를 {typeof(T).Name}으로 변환할 수 없어 {fallback} 로 설정합니다.");
        return fallback;
    }
}
