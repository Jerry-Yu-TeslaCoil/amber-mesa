using UnityEngine;

/// <summary>
/// 相机平滑跟随目标（玩家）。
///
/// 原相机由 PlayerMovement 直接移动，现在改为独立玩家角色移动，
/// 本脚本挂在主相机上，让相机平滑跟随玩家，保持玩家在画面中心。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    [Tooltip("要跟随的玩家角色 Transform。")]
    [SerializeField] private Transform target;

    [Header("跟随参数")]
    [Tooltip("平滑时间（秒），越小跟得越紧。0 表示瞬间到位。")]
    [SerializeField] private float smoothTime = 0.15f;

    [Tooltip("相机在 Z 轴上的固定位置（保持负值，让相机位于场景后方）。")]
    [SerializeField] private float zOffset = -10f;

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position;
        targetPosition.z = zOffset;

        // 用 SmoothDamp 平滑跟随，避免相机抖动；LateUpdate 保证在角色移动后执行。
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime);
    }
}
