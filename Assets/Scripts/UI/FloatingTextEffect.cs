using DG.Tweening;
using TMPro;
using UnityEngine;

public class FloatingTextEffect : MonoBehaviour
{
    [SerializeField] private TextMeshPro tmpText;

    /// <summary>
    /// 플로팅 텍스트 초기화 및 DOTween 연출 시작
    /// </summary>
    public void Play(string content, Color color, Vector3 spawnPosition, Vector3 moveDir, float distance = 0.5f, float duration = 0.6f)
    {
        if (tmpText == null) tmpText = GetComponent<TextMeshPro>();

        transform.position = spawnPosition;
        gameObject.SetActive(true);

        // 이미 작성해둔 DOTweenManager.FloatingText 연출 재사용
        Sequence seq = DOTweenManager.FloatingText(tmpText, content, color, moveDir, distance, duration);

        // 연출 완료 시 Destroy가 아닌 오브젝트 비활성화 (풀 반환)
        if (seq != null)
        {
            seq.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}