using AmberMesa.Height;
using UnityEngine;

/// <summary>
/// 相机平滑跟随玩家脚底，并加上 <see cref="ActorHeight"/> 的视觉抬升，
/// 避免角色在高层时看起来偏画面上方。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    [Tooltip("玩家根物体（脚底 / 碰撞体所在 Transform）。")]
    [SerializeField] private Transform target;

    [Tooltip("可选。不拖则从 target 上自动取 ActorHeight。")]
    [SerializeField] private ActorHeight targetHeight;

    [Header("跟随参数")]
    [Tooltip("平滑时间（秒），越小跟得越紧。0 表示瞬间到位。")]
    [SerializeField] private float smoothTime = 0.15f;

    [Tooltip("相机在 Z 轴上的固定位置（保持负值，让相机位于场景后方）。")]
    [SerializeField] private float zOffset = -10f;

    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        ResolveHeight();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (targetHeight == null)
            ResolveHeight();

        // 跟「看起来站着的位置」：脚底 + 高度视觉抬升。
        Vector3 targetPosition = targetHeight != null
            ? targetHeight.FollowPoint
            : target.position;
        targetPosition.z = zOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime);
    }

    private void ResolveHeight()
    {
        if (targetHeight != null || target == null)
            return;
        targetHeight = target.GetComponent<ActorHeight>();
        if (targetHeight == null)
            targetHeight = target.GetComponentInChildren<ActorHeight>();
    }
}
