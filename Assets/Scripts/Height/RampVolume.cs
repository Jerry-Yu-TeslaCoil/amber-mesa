using UnityEngine;

namespace AmberMesa.Height
{
    public enum RampAxis
    {
        /// <summary>沿世界 Y：通常南低北高或相反（用 Flip）。</summary>
        Vertical = 0,
        /// <summary>沿世界 X。</summary>
        Horizontal = 1,
    }

    /// <summary>
    /// 整段坡道体积：脚底在 Trigger 内时按位置连续写入 <see cref="ActorHeight"/>。
    /// Box 应对准 Collision 开口，Is Trigger = true。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class RampVolume : MonoBehaviour
    {
        [Header("高度两端")]
        [SerializeField] private HeightLevel fromLevel = HeightLevel.Ground;
        [SerializeField] private HeightLevel toLevel = HeightLevel.High;

        [Header("进度方向")]
        [SerializeField] private RampAxis axis = RampAxis.Vertical;

        [Tooltip("勾选后 to 在轴的负方向（例如北低南高时，轴仍是 Vertical 但要 Flip）。")]
        [SerializeField] private bool flipDirection;

        [Header("引用")]
        [SerializeField] private Collider2D volume;

        private void Reset()
        {
            volume = GetComponent<Collider2D>();
            if (volume != null)
                volume.isTrigger = true;
        }

        private void Awake()
        {
            if (volume == null)
                volume = GetComponent<Collider2D>();

            if (volume != null && !volume.isTrigger)
            {
                Debug.LogWarning(
                    $"[{nameof(RampVolume)}] {name} 的 Collider2D 不是 Trigger，已自动勾选 Is Trigger。",
                    this);
                volume.isTrigger = true;
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!TryGetActor(other, out ActorHeight actor))
                return;

            float t = EvaluateT(actor.transform.position);
            float fromH = HeightPhysics.ToHeight(fromLevel);
            float toH = HeightPhysics.ToHeight(toLevel);
            float height = Mathf.Lerp(fromH, toH, t);
            actor.SetRampHeight(height, fromLevel, toLevel);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!TryGetActor(other, out ActorHeight actor))
                return;

            actor.EndRamp(fromLevel, toLevel);
        }

        /// <summary>按脚底位置得到 0=from … 1=to。</summary>
        public float EvaluateT(Vector3 worldPosition)
        {
            Bounds b = volume.bounds;
            float t = axis == RampAxis.Vertical
                ? Mathf.InverseLerp(b.min.y, b.max.y, worldPosition.y)
                : Mathf.InverseLerp(b.min.x, b.max.x, worldPosition.x);

            if (flipDirection)
                t = 1f - t;

            return Mathf.Clamp01(t);
        }

        static bool TryGetActor(Collider2D other, out ActorHeight actor)
        {
            actor = other.GetComponent<ActorHeight>();
            if (actor == null)
                actor = other.GetComponentInParent<ActorHeight>();
            return actor != null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var col = volume != null ? volume : GetComponent<Collider2D>();
            if (col == null)
                return;

            Bounds b = col.bounds;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(b.center, b.size);

            // 指示 from → to 方向
            Vector3 fromPos;
            Vector3 toPos;
            if (axis == RampAxis.Vertical)
            {
                fromPos = new Vector3(b.center.x, flipDirection ? b.max.y : b.min.y, b.center.z);
                toPos = new Vector3(b.center.x, flipDirection ? b.min.y : b.max.y, b.center.z);
            }
            else
            {
                fromPos = new Vector3(flipDirection ? b.max.x : b.min.x, b.center.y, b.center.z);
                toPos = new Vector3(flipDirection ? b.min.x : b.max.x, b.center.y, b.center.z);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(fromPos, toPos);
            Gizmos.DrawSphere(fromPos, 0.08f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(toPos, 0.08f);
        }
#endif
    }
}
