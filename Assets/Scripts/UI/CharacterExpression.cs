using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterExpression : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite hitSprite;
    [SerializeField] private Sprite attackSprite;
    [SerializeField] private float duration = 0.5f;

    private Coroutine current;

    public void PlayHit() => Play(hitSprite);
    public void PlayAttack() => Play(attackSprite);

    private void Play(Sprite sprite)
    {
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(Routine(sprite));
    }

    private IEnumerator Routine(Sprite sprite)
    {
        targetImage.sprite = sprite;
        yield return new WaitForSeconds(duration);
        targetImage.sprite = defaultSprite;
        current = null;
    }
}