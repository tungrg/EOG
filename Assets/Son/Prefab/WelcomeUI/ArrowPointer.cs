using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [Header("References")]
    public Transform player;   // Player mà arrow sẽ ở trên đầu
    public Transform enemy;    // Enemy mà arrow sẽ chỉ tới

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2f, 0);   // vị trí arrow trên đầu player
    public float bobAmplitude = 0.25f;               // độ nhấp nhô
    public float bobFrequency = 2f;                  // tần số nhấp nhô
    public Vector3 rotationOffset = Vector3.zero;    // chỉnh nếu arrow bị lệch trục

    void Update()
    {
        if (player == null || enemy == null) return;

        // vị trí: luôn ở trên đầu player, có nhấp nhô
        Vector3 basePos = player.position + offset;
        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = basePos + Vector3.up * bob;

        // hướng: từ player -> enemy (bỏ y để không ngửa lên trời)
        Vector3 dir = enemy.position - player.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = lookRot * Quaternion.Euler(rotationOffset);
        }
    }

    // hàm gọi từ TutorialManager
    public void SetTargets(Transform playerTransform, Transform enemyTransform)
    {
        player = playerTransform;
        enemy = enemyTransform;
    }
}
