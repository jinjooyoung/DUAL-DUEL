
using UnityEngine;

public class SFXButton : MonoBehaviour
{
    public SFXType type;

    public void PlaySFX()
    {
        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlaySFX(type);
    }
}
