using TMPro;
using UnityEngine;

public class CurrentGoldUI : MonoBehaviour
{
    public TextMeshProUGUI text;

    private void Start()
    {
        text.text = GameManager.Instance.currentGold.ToString("N0");
    }
}
