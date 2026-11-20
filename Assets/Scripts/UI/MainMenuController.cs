
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameOverInitialize GOI;

    public void StartNewGame()
    {
        if (GOI == null)
        {
            GOI = gameObject.GetComponent<GameOverInitialize>();
        }

        GOI.GameOverInit();

        SceneTransition.Load("StageSelectScene");
    }
}
