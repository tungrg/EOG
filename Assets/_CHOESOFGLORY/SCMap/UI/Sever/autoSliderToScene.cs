using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class autoSliderToScene : MonoBehaviour
{
    [SerializeField] private Slider slider; // Gán UI Slider trong Inspector
    [SerializeField] private float speed = 1f; // Tốc độ trượt (giây để hoàn thành từ 0 đến 100)

    private void Start()
    {
        if (slider != null)
        {
            slider.value = 0; // Đặt giá trị ban đầu là 0
            StartCoroutine(AutoSlide());
        }
    }

    private System.Collections.IEnumerator AutoSlide()
    {
        float elapsedTime = 0f;
        float startValue = 0f;
        float targetValue = 100f;

        while (elapsedTime < speed)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / speed;
            slider.value = Mathf.Lerp(startValue, targetValue, t);
            yield return null;
        }

        // Đảm bảo slider đạt chính xác 100
        slider.value = 100f;

        // Chuyển sang scene CreateCharate khi slider đạt 100
          SceneManager.LoadScene("Map");
    }
}