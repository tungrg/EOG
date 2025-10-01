using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    // Gọi từ Button (bằng cách chọn hàm và gõ tên scene trong Inspector)
    public void ChangeScene(string sceneName)
    {
        Debug.Log("ChangeScene called -> " + sceneName);
        SceneManager.LoadScene(sceneName);
    }

    // Tùy chọn: hàm không cần tham số (dễ gán nếu chỉ 1 scene cụ thể)
    public void ChangeToMap()
    {
        SceneManager.LoadScene("Map");
    }
}
