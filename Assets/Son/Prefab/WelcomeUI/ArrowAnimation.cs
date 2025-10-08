using UnityEngine;

public class ArrowAnimation : MonoBehaviour
{
    private Vector3 basePosition;  // lưu vị trí ban đầu

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 0.0f, 0);   // vị trí arrow trên đầu player
    public float bobAmplitude = 0.25f;               // độ nhấp nhô
    public float bobFrequency = 2f;                  // tần số nhấp nhô

    void Start()
    {
        // lưu vị trí ban đầu khi bắt đầu
        basePosition = transform.position + offset;
    }

    void Update()
    {
        // vị trí: nhấp nhô từ vị trí ban đầu
        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = basePosition + Vector3.up * bob;


    }
}
    // hàm gọi từ TutorialManager
