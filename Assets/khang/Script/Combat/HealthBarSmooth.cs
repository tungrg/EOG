using UnityEngine;
using UnityEngine.UI;

public class HealthBarSmooth : MonoBehaviour
{
    private Image healthBarFill;
    private float targetFillAmount;
    private float smoothSpeed = 2f;

    void Start()
    {
        healthBarFill = GetComponent<Image>();
        targetFillAmount = healthBarFill.fillAmount;
    }

    void Update()
    {
        if (Mathf.Abs(healthBarFill.fillAmount - targetFillAmount) > 0.01f)
        {
            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
        }
    }

    public void UpdateHealth(float healthRatio)
    {
        targetFillAmount = healthRatio;
    }
}