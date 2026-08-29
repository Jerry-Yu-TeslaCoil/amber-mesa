using UnityEngine;
using UnityEngine.Rendering;

namespace AmberMesa.Height
{
    /// <summary>
    /// B 方案高度体：碰撞体留在地图平面；视觉抬升在子节点；
    /// SortingGroup 在脚底，与 Face 同 Sorting Layer，用 Y 排序与崖面遮挡（不与 Floor 同层）。
    /// 坡道内由 <see cref="RampVolume"/> 连续写 height，并同时碰撞 from/to 两层以便护栏生效。
    /// 脚下无 Support 遮罩时落入下一层（逻辑高度下降 + 坠落动画）。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SortingGroup))]
    public class ActorHeight : MonoBehaviour
    {
        [Header("高度")]
        [Tooltip("连续逻辑高度。Deep=0，Ground=1，High=2。")]
        [SerializeField] private float height = HeightPhysics.GroundHeight;

        [Tooltip("启动时用上面的 height 立刻应用碰撞过滤与视觉。")]
        [SerializeField] private bool applyOnStart = true;

        [Header("视觉（碰撞体不动）")]
        [Tooltip("只抬这个子节点；根物体保持脚底地图坐标。")]
        [SerializeField] private Transform visualRoot;

        [Tooltip("每一高度单位抬升多少世界单位（通常等于一层崖面的格子高度）。")]
        [SerializeField] private float visualUnitsPerHeight = 1f;

        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("挂在脚底根物体：Y 排序用脚底，而不是抬升后的 Visual。")]
        [SerializeField] private SortingGroup sortingGroup;

        [Header("物理")]
        [SerializeField] private Rigidbody2D body;

        [SerializeField] private Collider2D bodyCollider;

        [Header("坠落")]
        [Tooltip("检测脚下 Support Tilemap；无支撑则落入更低高度带。")]
        [SerializeField] private bool checkFloorSupport = true;

        [Tooltip("逻辑高度下落速度（高度单位/秒）。")]
        [SerializeField] private float fallSpeed = 4f;

        [Tooltip("可选；为空则在子物体里找 Animator。")]
        [SerializeField] private Animator animator;

        private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
        private static readonly int LandHash = Animator.StringToHash("Land");

        private bool onRamp;
        private HeightLevel rampFrom = HeightLevel.Ground;
        private HeightLevel rampTo = HeightLevel.High;

        private bool isFalling;
        private float fallTargetHeight;

        public float Height => height;

        public HeightLevel Level => HeightPhysics.ToLevel(height);

        public bool OnRamp => onRamp;

        public bool IsFalling => isFalling;

        /// <summary>
        /// 镜头 / 特效应对准的点：脚底世界坐标 + 视觉抬升（与 Visual 子节点一致）。
        /// </summary>
        public Vector3 FollowPoint
        {
            get
            {
                Vector3 p = transform.position;
                p.y += height * visualUnitsPerHeight;
                return p;
            }
        }

        private void Awake()
        {
            if (body == null)
                body = GetComponent<Rigidbody2D>();
            if (bodyCollider == null)
                bodyCollider = GetComponent<Collider2D>();
            if (sortingGroup == null)
                sortingGroup = GetComponent<SortingGroup>();
            if (spriteRenderer == null && visualRoot != null)
                spriteRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            if (applyOnStart)
                Apply();
        }

        private void FixedUpdate()
        {
            if (!checkFloorSupport || onRamp)
                return;

            if (isFalling)
            {
                TickFall(Time.fixedDeltaTime);
                return;
            }

            if (!HeightSupport.HasSupport(Level, transform.position))
                TryBeginFall();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;
            Apply();
        }
#endif

        public void SetHeight(float value)
        {
            CancelFall(applyLandAnim: false);
            height = value;
            onRamp = false;
            Apply();
        }

        public void SetLevel(HeightLevel level)
        {
            SetHeight(HeightPhysics.ToHeight(level));
        }

        /// <summary>坡道内调用：连续高度 + 同时启用 from/to 两层碰撞（护栏）。</summary>
        public void SetRampHeight(float value, HeightLevel from, HeightLevel to)
        {
            CancelFall(applyLandAnim: false);
            height = value;
            onRamp = true;
            rampFrom = from;
            rampTo = to;
            Apply();
        }

        /// <summary>离开坡道：高度钳到较近的一端，恢复单层碰撞。</summary>
        public void EndRamp(HeightLevel from, HeightLevel to)
        {
            float fromH = HeightPhysics.ToHeight(from);
            float toH = HeightPhysics.ToHeight(to);
            height = Mathf.Abs(height - fromH) <= Mathf.Abs(height - toH) ? fromH : toH;
            onRamp = false;
            Apply();
        }

        public void Apply()
        {
            ApplyCollisionFilter();
            ApplyVisual();
        }

        bool TryBeginFall()
        {
            if (!HeightSupport.TryFindLandingHeight(height, transform.position, out float landing))
                return false;

            isFalling = true;
            fallTargetHeight = landing;
            SetFallingAnim(true);
            return true;
        }

        void TickFall(float dt)
        {
            height = Mathf.MoveTowards(height, fallTargetHeight, fallSpeed * dt);
            Apply();

            if (height > fallTargetHeight + 0.0001f)
                return;

            height = fallTargetHeight;
            isFalling = false;
            SetFallingAnim(false);
            TriggerLandAnim();
            Apply();
        }

        void CancelFall(bool applyLandAnim)
        {
            if (!isFalling)
                return;

            isFalling = false;
            SetFallingAnim(false);
            if (applyLandAnim)
                TriggerLandAnim();
        }

        void SetFallingAnim(bool falling)
        {
            if (animator != null)
                animator.SetBool(IsFallingHash, falling);
        }

        void TriggerLandAnim()
        {
            if (animator != null)
                animator.SetTrigger(LandHash);
        }

        private void ApplyCollisionFilter()
        {
            LayerMask exclude;
            if (onRamp)
            {
                // 开口两侧护栏通常画在 from/to 的 Collision 上，坡上两层都要能撞到。
                exclude = HeightPhysics.ExcludeMaskForRamp(rampFrom, rampTo);
            }
            else
            {
                exclude = HeightPhysics.ExcludeMaskFor(Level);
            }

            if (body != null)
                body.excludeLayers = exclude;

            if (bodyCollider != null)
                bodyCollider.excludeLayers = exclude;
        }

        private void ApplyVisual()
        {
            if (visualRoot != null)
            {
                Vector3 local = visualRoot.localPosition;
                local.y = height * visualUnitsPerHeight;
                visualRoot.localPosition = local;
            }

            string layerName = HeightSorting.ActorLayerName(Level);
            int layerId = SortingLayer.NameToID(layerName);
            if (layerId == 0 && layerName != "Default")
            {
                Debug.LogError(
                    $"[{nameof(ActorHeight)}] Sorting Layer「{layerName}」不存在。角色会落在 Default 并被 Floor 挡住。",
                    this);
                return;
            }

            if (sortingGroup != null)
            {
                sortingGroup.sortingLayerID = layerId;
                sortingGroup.sortingOrder = 0;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingLayerID = layerId;
                spriteRenderer.sortingOrder = 0;
            }
        }
    }
}
