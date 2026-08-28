using UnityEngine;

/// <summary>
/// 玩家角色移动。
///
/// 原实现把脚本挂在主相机上直接改 <c>transform.position</c>，导致「移动的是相机」。
/// 现在改为挂在独立的玩家角色对象上，基于 Rigidbody2D 的物理移动（velocity 驱动），
/// 与 Terrain Tilemap 的 TilemapCollider2D / CompositeCollider2D 产生真实碰撞，
/// 同时把移动输入同步给 Animator 的四方向跑步动画。
///
/// 依赖：玩家对象需挂 Rigidbody2D（Dynamic）、Collider2D、Animator。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("移动")]
    [Tooltip("移动速度（世界单位/秒）。")]
    [SerializeField] private float moveSpeed = 4f;

    private Rigidbody2D rb;
    private Animator animator;

    // Animator 参数 hash，避免每帧字符串查找的开销。
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // 顶视角无重力：关闭重力、冻结旋转，并启用插值 + 连续碰撞检测，防止抖动和高速穿墙。
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Update()
    {
        // 读输入并归一化，防止斜向移动速度比正方向更快。
        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        // 用速度驱动物理移动，碰撞由 Rigidbody2D + Tilemap 碰撞器自动处理。
        rb.velocity = moveInput * moveSpeed;

        UpdateAnimation(moveInput);
    }

    /// <summary>把移动输入同步给 Animator，驱动 Idle / 四方向跑步动画。</summary>
    private void UpdateAnimation(Vector2 moveInput)
    {
        if (animator == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.001f;

        // 仅在移动时更新方向参数；停止时保持最后方向，让 Idle 停在最后朝向。
        if (isMoving)
        {
            animator.SetFloat(MoveXHash, moveInput.x);
            animator.SetFloat(MoveYHash, moveInput.y);
        }

        // Speed 用于在 Animator 里切换 Idle（0）/ Run（1）两个状态。
        animator.SetFloat(SpeedHash, isMoving ? 1f : 0f);
    }
}
