using UnityEngine;
using UnityEngine.Tilemaps;

namespace AmberMesa.Height
{
    /// <summary>
    /// 各高度带的地板支撑查询（专用 Support Tilemap 的 HasTile）。
    /// 与 Floor 渲染层、Collision 墙体层分离；未注册某层时视为该层处处有支撑。
    /// </summary>
    public static class HeightSupport
    {
        static readonly Tilemap[] Maps = new Tilemap[3];

        public static void Register(HeightLevel level, Tilemap tilemap)
        {
            if (tilemap == null)
                return;
            Maps[(int)level] = tilemap;
        }

        public static void Unregister(HeightLevel level, Tilemap tilemap)
        {
            int i = (int)level;
            if (Maps[i] == tilemap)
                Maps[i] = null;
        }

        public static bool IsRegistered(HeightLevel level) => Maps[(int)level] != null;

        /// <summary>
        /// 脚底世界坐标所在格子是否有该高度带的地板。
        /// </summary>
        public static bool HasSupport(HeightLevel level, Vector3 worldPosition)
        {
            Tilemap map = Maps[(int)level];
            if (map == null)
                return true;

            Vector3Int cell = map.WorldToCell(worldPosition);
            return map.HasTile(cell);
        }

        /// <summary>
        /// 从当前高度往下找最近有支撑的落点高度；若已在 Deep 则返回 false。
        /// </summary>
        public static bool TryFindLandingHeight(float fromHeight, Vector3 worldPosition, out float landingHeight)
        {
            landingHeight = fromHeight;
            HeightLevel fromLevel = HeightPhysics.ToLevel(fromHeight);
            if (fromLevel == HeightLevel.Deep)
                return false;

            for (int i = (int)fromLevel - 1; i >= (int)HeightLevel.Deep; i--)
            {
                var level = (HeightLevel)i;
                if (!IsRegistered(level) || HasSupport(level, worldPosition))
                {
                    landingHeight = HeightPhysics.ToHeight(level);
                    return true;
                }
            }

            // 下层都没有格子时仍落到 Deep，避免悬空卡死。
            landingHeight = HeightPhysics.DeepHeight;
            return true;
        }
    }
}
