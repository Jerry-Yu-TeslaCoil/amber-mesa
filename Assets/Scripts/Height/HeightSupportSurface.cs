using UnityEngine;
using UnityEngine.Tilemaps;

namespace AmberMesa.Height
{
    /// <summary>
    /// 挂在专用 Support Tilemap 上（与 Floor 渲染层、Collision 墙体层分开）。
    /// 只提供 <see cref="Tilemap.HasTile"/> 支撑查询；无墙体碰撞、不参与 Sorting。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap))]
    public class HeightSupportSurface : MonoBehaviour
    {
        [SerializeField] private HeightLevel level = HeightLevel.Ground;

        [Tooltip("默认用本物体 Tilemap。")]
        [SerializeField] private Tilemap tilemap;

        [Tooltip("Play 模式隐藏绘制，编辑时仍可见以便涂支撑格。")]
        [SerializeField] private bool hideRendererInPlayMode = true;

        public HeightLevel Level => level;

        private void Reset()
        {
            tilemap = GetComponent<Tilemap>();
        }

        private void Awake()
        {
            if (tilemap == null)
                tilemap = GetComponent<Tilemap>();

            if (hideRendererInPlayMode && Application.isPlaying)
            {
                var renderer = GetComponent<TilemapRenderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (tilemap == null)
                tilemap = GetComponent<Tilemap>();
            HeightSupport.Register(level, tilemap);
        }

        private void OnDisable()
        {
            HeightSupport.Unregister(level, tilemap);
        }
    }
}
