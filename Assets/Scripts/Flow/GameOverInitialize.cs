using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverInitialize : MonoBehaviour
{
    public void GameOverLoadMain()
    {
        SceneManager.LoadScene("MainScene");
        GameOverInit();
    }

    private void GameOverInit()
    {
        RewardManager.Instance.Initialize();    // 카드 랭크 누적 삭제
        GameManager.Instance.DeckInit();        // 초기덱으로 되돌림
        GameManager.Instance.CharacterInit();   // 초기 캐릭터, 체력 초기화
        StageManager.Instance.ResetRunData();   // 스테이지 초기화
    }
}
