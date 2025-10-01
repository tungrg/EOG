using UnityEngine;
using System.Collections;

public class MovementManager : MonoBehaviour
{
    [SerializeField] private float meleeAttackDistance = 1.5f;
    [SerializeField] private float jumpHeight = 0.9f;
    [SerializeField] private float jumpDuration = 0.5f;
    [SerializeField] private float rotationSpeed = 12f;
    private CombatManager combatManager;

    public void SetCombatManager(CombatManager manager)
    {
        combatManager = manager;
    }

    public IEnumerator MoveForAction(Transform mover, Transform target, AttackType attackType, float moveDistance, bool returnToStart = false, Vector3? originalPosition = null, bool hasMovementTag = false, bool useRunAnimation = false)
    {
        if (mover == null || target == null)
        {
            DebugLogger.LogError($"Mover or target is null in MoveForAction for {mover?.name}");
            yield break;
        }

        Vector3 startPosition = mover.position;
        Vector3 moveTarget = startPosition;
        Vector3 directionToTarget = (target.position - mover.position).normalized;
        directionToTarget.y = 0;
        Quaternion targetRotation = directionToTarget != Vector3.zero ? Quaternion.LookRotation(directionToTarget) : mover.rotation;

        // Xác định vị trí mục tiêu
        if (attackType == AttackType.Melee && !hasMovementTag)
        {
            moveTarget = target.position - directionToTarget * meleeAttackDistance;
            moveTarget.y = startPosition.y;

        }
        else if (attackType == AttackType.Ranged && moveDistance <= 0f && !hasMovementTag)
        {
            moveTarget = startPosition;
            ;
        }
        else if (hasMovementTag && moveDistance > 0f)
        {
            moveTarget = startPosition + directionToTarget * moveDistance;
            moveTarget.y = startPosition.y;

        }

        // Di chuyển nếu cần
        if (Vector3.Distance(mover.position, moveTarget) > 0.1f)
        {

            if (useRunAnimation)
            {
                if (mover.TryGetComponent<Enemy>(out Enemy enemy))
                {
                    enemy.SetMoving(true);
                }
                else if (mover.TryGetComponent<Combatant>(out Combatant combatant))
                {
                    combatant.SetMoving(true);
                }
            }
            yield return StartCoroutine(MoveInParabolicArc(mover, startPosition, moveTarget, target));
        }
        else
        {
            // Chỉ xoay nếu không cần di chuyển
            yield return StartCoroutine(RotateToTarget(mover, target));

        }

        // Đảm bảo vị trí và hướng cuối cùng
        mover.position = new Vector3(moveTarget.x, startPosition.y, moveTarget.z);
        UpdateRotation(mover, target);

        // Trở về vị trí ban đầu nếu cần
        if (returnToStart && originalPosition.HasValue)
        {

            if (useRunAnimation)
            {
                if (mover.TryGetComponent<Enemy>(out Enemy enemyReturn))
                {
                    enemyReturn.SetMoving(true);
                }
                else if (mover.TryGetComponent<Combatant>(out Combatant combatantReturn))
                {
                    combatantReturn.SetMoving(true);
                }
            }
            yield return StartCoroutine(MoveInParabolicArc(mover, mover.position, originalPosition.Value, target));
            mover.position = new Vector3(originalPosition.Value.x, startPosition.y, originalPosition.Value.z);
            UpdateRotation(mover, target);

        }

        // Đặt lại trạng thái sau khi di chuyển
        if (mover.TryGetComponent<Enemy>(out Enemy enemyFinal))
        {
            enemyFinal.SetMoving(false);
        }
        else if (mover.TryGetComponent<Combatant>(out Combatant combatantFinal))
        {
            combatantFinal.SetMoving(false);
        }
    }

    private IEnumerator MoveInParabolicArc(Transform mover, Vector3 startPos, Vector3 endPos, Transform target)
    {
        if (jumpDuration <= 0f)
        {
            DebugLogger.LogError("jumpDuration is zero or negative, setting to default 0.6f");
            jumpDuration = 0.6f;
        }

        float elapsedTime = 0f;
        Vector3 groundStartPos = new Vector3(startPos.x, startPos.y, startPos.z);
        Vector3 groundEndPos = new Vector3(endPos.x, startPos.y, endPos.z);

        while (elapsedTime < jumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / jumpDuration;

            // Tính toán vị trí theo parabol
            Vector3 currentPos = Vector3.Lerp(groundStartPos, groundEndPos, t);
            float height = jumpHeight * 4f * t * (1f - t);
            currentPos.y = startPos.y + height;

            // Cập nhật vị trí
            mover.position = currentPos;

            // Liên tục xoay về hướng mục tiêu
            UpdateRotation(mover, target);


            yield return null;
        }

        // Đảm bảo vị trí và hướng cuối cùng
        mover.position = new Vector3(groundEndPos.x, startPos.y, groundEndPos.z);
        UpdateRotation(mover, target);

    }

    private IEnumerator RotateToTarget(Transform mover, Transform target)
    {
        float elapsedTime = 0f;
        float rotationDuration = 0.3f;

        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            UpdateRotation(mover, target);
            yield return null;
        }

        UpdateRotation(mover, target);

    }

    private void UpdateRotation(Transform mover, Transform target)
    {
        if (target == null || mover == null) return;
        Vector3 directionToTarget = (target.position - mover.position).normalized;
        directionToTarget.y = 0;
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            mover.rotation = Quaternion.Slerp(mover.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}