using UnityEngine;
using UnityEngine.UI;

public class RewardItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;

    public void Setup(EquipmentData data)
    {
        if (icon != null)
            icon.sprite = data.ItemSprite;
    }
}