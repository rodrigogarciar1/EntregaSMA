using UnityEngine;
using TMPro;
public class InventoryUI : MonoBehaviour
{
    private TextMeshProUGUI relic_text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        relic_text = GetComponent<TextMeshProUGUI>();
    }
    public void UpdateRelicText(PlayerInventory playerInventory)
    {
        relic_text.text = playerInventory.NumberOfRelics.ToString();
    }


}
