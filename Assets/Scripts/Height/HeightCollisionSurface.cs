using UnityEngine;
using UnityEngine.Tilemaps;

namespace AmberMesa.Height
{
    /// <summary>
    /// 挂在 Deep / Ground / High Collision Tilemap 上：
    /// 把物体设到对应 Physics Layer，Play 模式下关闭 TilemapRenderer（碰撞仍在）。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap))]
    public class HeightCollisionSurface : MonoBehaviour
    {
        [SerializeField] private HeightLevel level = HeightLevel.Ground;

        [Tooltip("进入 Play 后隐藏碰撞层绘制，便于对照视觉层；编辑模式仍可见。")]
        [SerializeField] private bool hideRendererInPlayMode = true;

        public HeightLevel Level => level;

        private void Awake()
        {
            int layer = HeightPhysics.GetLayer(level);
            if (layer < 0)
            {
                Debug.LogError(
                    $"[{nameof(HeightCollisionSurface)}] 未找到 Layer「{LayerName(level)}」。" +
                    "请在 Project Settings → Tags and Layers 中添加 DeepCollision / GroundCollision / HighCollision。",
                    this);
                return;
            }

            gameObject.layer = layer;

            // CompositeCollider 有时挂在同一物体上；子物体若有碰撞体也一并设层。
            foreach (Transform child in transform)
                child.gameObject.layer = layer;

            if (hideRendererInPlayMode && Application.isPlaying)
            {
                var renderer = GetComponent<TilemapRenderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        static string LayerName(HeightLevel level) => level switch
        {
            HeightLevel.Deep => HeightPhysics.DeepLayerName,
            HeightLevel.Ground => HeightPhysics.GroundLayerName,
            HeightLevel.High => HeightPhysics.HighLayerName,
            _ => HeightPhysics.GroundLayerName,
        };
    }
}
