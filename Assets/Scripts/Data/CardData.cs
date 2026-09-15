using System;
using UnityEngine;

[Serializable]
public class CardData
{
    public int cardId;
    public string cardType;     // 카드의 타입 (공격, 방어, 버프 등)
    public int rank;            // 1~6까지의 등급을 나타냄. 높을수록 희귀(좋은) 등급

    public string nameKey;      // 추후 로컬라이징 적용할 때 사용할 카드 이름 키
    public string descKey;      // 설명 키

    public int baseValue;       // 기본 수치
    public int upgrade_1;       // 업그레이드 1회 수치
    public int upgrade_2;       // 업그레이드 2회 수치
    public int upgrade_3;       // 업그레이드 3회 수치
    public int upgrade_4;       // 업그레이드 4회 수치
    public int upgrade_5;       // 업그레이드 5회 수치
}
