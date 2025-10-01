using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterInfoPanel : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image manaBarFill; // Thêm thanh fill cho mana
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text manaText;
    private Combatant combatant;
    private Vector3 normalScale = new Vector3(1f, 1f, 1f);
    private Vector3 highlightedScale = new Vector3(1.2f, 1.2f, 1.2f);
    private float scaleSpeed = 5f;

    public Combatant Combatant => combatant;

    public void SetCombatant(Combatant combatant)
    {
        this.combatant = combatant;
        UpdatePanel();
        if (combatant.GetData().AvatarSprite != null)
        {
            avatarImage.sprite = combatant.GetData().AvatarSprite;
        }
        else
        {
            DebugLogger.LogWarning($"No AvatarSprite assigned for {combatant.Name}");
        }
    }

    public void UpdatePanel()
    {
        if (combatant == null || combatant.HP <= 0)
        {
            gameObject.SetActive(false); // Ẩn panel nếu nhân vật chết
            return;
        }

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)combatant.HP / combatant.GetData().HP;
        }
        if (healthText != null)
        {
            healthText.text = $"{combatant.HP}/{combatant.GetData().HP}";
        }
        if (manaBarFill != null)
        {
            manaBarFill.fillAmount = (float)combatant.Mana / combatant.GetData().Skill3ManaCost;
        }
        if (manaText != null)
        {
            manaText.text = $"Mana: {combatant.Mana}/{combatant.GetData().Skill3ManaCost}";
        }
    }

    public void SetHighlight(bool isHighlighted)
    {
        Vector3 targetScale = isHighlighted ? highlightedScale : normalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }
}