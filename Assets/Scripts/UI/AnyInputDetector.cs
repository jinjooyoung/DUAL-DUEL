using UnityEngine;
using UnityEngine.SceneManagement;

public class AnyInputDetector : MonoBehaviour
{
    [Header("입력 지연 시간 (초)")]
    [Tooltip("씬 진입 직후 실수로 눌리는 것을 방지하기 위한 대기 시간")]
    [SerializeField] private float inputDelay = 0.5f;

    private float timer = 0f;

    private void Update()
    {
        // 씬 진입 직후 오작동 방지를 위한 쿨타임 체크
        if (timer < inputDelay)
        {
            timer += Time.deltaTime;
            return;
        }

        if (Input.anyKeyDown)
        {
            SceneManager.LoadScene("StageSelectScene");
        }
    }
}