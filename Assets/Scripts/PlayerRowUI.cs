using TMPro;
using UnityEngine;

public class PlayerRowUI : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI diamondsText;

    public void SetData(PlayerListData data)
    {
        rankText.text = data.rank.ToString();
        nameText.text = data.name.ToString();   // ✅ FixedString → string
        coinsText.text = data.coins.ToString();
        diamondsText.text = data.diamonds.ToString();
    }
}
