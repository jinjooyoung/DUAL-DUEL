using System;
using UnityEngine;

/// <summary>
/// 엑셀 Dialogue 시트 한 행을 그대로 받는 데이터 (열 순서 = 엑셀 Dialogue 시트 열 순서)
/// </summary>
[Serializable]
public class DialogueData
{
    public int id;
    public string characterName;    // 비우면 이름표를 끔
    public string text;             // 셀 안의 \n 은 줄바꿈. TMP 서식 태그 사용 가능
    public int nextId;              // 다음 대사 id. -1 이면 여기서 대사창이 닫히고 묶음이 끝남
    public string portraitPath;     // Resources 기준 스탠딩 경로. 비우면 스탠딩 없음
    public string expression;       // 표정 (portraitPath_표정 을 먼저 찾음)
    public string action;           // 이 대사가 뜨는 순간 실행할 동작 이름 (비우면 없음). Glitch 는 기본 제공

    public const int END = -1;
}
