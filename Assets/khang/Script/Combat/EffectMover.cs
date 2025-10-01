using UnityEngine;

public class EffectMover : MonoBehaviour
{
    private Vector3 targetPosition;
    private float speed = 10f;
    private bool isMoving;
    private ObjectPool objectPool; // Thêm tham chiếu đến ObjectPool

    void Awake()
    {
        objectPool = FindFirstObjectByType<CombatManager>().GetComponent<ObjectPool>(); // Thay đổi từ FindObjectOfType
    }

    public void Initialize(Vector3 target, float moveSpeed)
    {
        targetPosition = target;
        speed = moveSpeed;
        isMoving = true;
    }

    void Update()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            isMoving = false;
            ParticleSystem particleSystem = GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                StartCoroutine(ReturnToPoolAfterDuration(particleSystem.main.duration));
            }
            else
            {
                StartCoroutine(ReturnToPoolAfterDuration(1f));
            }
        }
    }

    private System.Collections.IEnumerator ReturnToPoolAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        objectPool.ReturnObject(gameObject); // Trả về pool thay vì Destroy
    }
}