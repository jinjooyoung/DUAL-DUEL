using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대사 테이블. (ScriptableObject 는 클래스 이름과 같은 이름의 파일에 있어야 빌드에서 에셋이 연결됨)
/// 대사 테이블 내용: Tools > DUAL DUEL > 대사 CSV 가져오기 로 채움 (기존 엑셀 임포터라면 SetLines)
/// </summary>
[CreateAssetMenu(fileName = "DialogueTable", menuName = "SO/DataSO/DialogueTable")]
public class DialogueTableSO : ScriptableObject
{
    public List<DialogueData> lines = new List<DialogueData>();

    private Dictionary<int, DialogueData> map;

    /// <summary>
    /// 대사 목록을 통째로 교체 (외부 임포터용)
    /// </summary>
    public void SetLines(List<DialogueData> newLines)
    {
        lines = newLines ?? new List<DialogueData>();
        map = null;
    }

    /// <summary>
    /// id 로 대사 찾기
    /// </summary>
    public bool TryGet(int id, out DialogueData line)
    {
        if (map == null) BuildMap();
        return map.TryGetValue(id, out line);
    }

    // id -> 대사 사전 (중복 id 는 먼저 나온 행만 사용)
    private void BuildMap()
    {
        map = new Dictionary<int, DialogueData>();
        foreach (DialogueData line in lines)
        {
            if (line == null) continue;
            if (map.ContainsKey(line.id)) { Debug.LogWarning($"[Dialogue] 중복된 대사 id {line.id} 는 먼저 나온 행만 사용합니다."); continue; }
            map.Add(line.id, line);
        }
    }

    // 인스펙터에서 바꾸면 사전을 다시 만들도록
    private void OnValidate() => map = null;
}
