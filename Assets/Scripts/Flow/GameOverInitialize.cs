using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverInitialize : MonoBehaviour
{
    public void GameOverLoadMain()
    {
        GameOverInit();                         // 초기화를 먼저 끝내고
        SceneTransition.Load("MainScene");      // 메인으로
    }

    public void GameOverInit()
    {
        // 카드 랭크 누적 삭제
        // (Initialize 는 처음 한 번만 동작해서 누적이 지워지지 않음 -> SetChapter(1) 이 1챕터 테이블로 바꾸며 누적도 초기화)
        if (RewardManager.Instance != null) RewardManager.Instance.SetChapter(1);
        ShopManager.ResetShopRoll();                                            // 상점 랭크 누적 삭제

        if (GameManager.Instance != null)
        {
            GameManager.Instance.DeckInit();                                    // 초기덱으로 되돌림
            GameManager.Instance.StatInit();                                    // 초기 캐릭터, 체력, 골드 초기화
        }
        if (StageManager.Instance != null) StageManager.Instance.ResetRunData(); // 스테이지 초기화

        // 진행 중이던 런 저장 삭제 (이어하기 불가). 다이얼로그 기록(dialogue.sav)은 영구라 지우지 않음
        SaveManager.DeleteSave();
    }
}
