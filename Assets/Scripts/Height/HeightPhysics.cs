using UnityEngine;

namespace AmberMesa.Height
{
    /// <summary>
    /// 三层地形碰撞的 Physics Layer 约定，以及 height → 高度带的换算。
    /// Layer 名必须与 Project Settings → Tags and Layers 一致。
    /// </summary>
    public static class HeightPhysics
    {
        public const string DeepLayerName = "DeepCollision";
        public const string GroundLayerName = "GroundCollision";
        public const string HighLayerName = "HighCollision";

        /// <summary>Deep / Ground / High 的标准高度值（与视觉抬升单位对齐）。</summary>
        public const float DeepHeight = 0f;
        public const float GroundHeight = 1f;
        public const float HighHeight = 2f;

        public static int DeepLayer => LayerMask.NameToLayer(DeepLayerName);
        public static int GroundLayer => LayerMask.NameToLayer(GroundLayerName);
        public static int HighLayer => LayerMask.NameToLayer(HighLayerName);

        public static int GetLayer(HeightLevel level)
        {
            return level switch
            {
                HeightLevel.Deep => DeepLayer,
                HeightLevel.Ground => GroundLayer,
                HeightLevel.High => HighLayer,
                _ => GroundLayer,
            };
        }

        public static LayerMask AllCollisionMask
        {
            get
            {
                int mask = 0;
                TryAdd(ref mask, DeepLayer);
                TryAdd(ref mask, GroundLayer);
                TryAdd(ref mask, HighLayer);
                return mask;
            }
        }

        /// <summary>
        /// 应排除的碰撞层 = 三层地形里除当前带以外的层。
        /// 角色只与当前高度带的 Collision Tilemap 相撞。
        /// </summary>
        public static LayerMask ExcludeMaskFor(HeightLevel level)
        {
            return AllCollisionMask & ~(1 << GetLayer(level));
        }

        public static float ToHeight(HeightLevel level)
        {
            return level switch
            {
                HeightLevel.Deep => DeepHeight,
                HeightLevel.Ground => GroundHeight,
                HeightLevel.High => HighHeight,
                _ => GroundHeight,
            };
        }

        /// <summary>
        /// 将连续高度落到最近的高度带（暂无坡道滞后；过渡区以后再加）。
        /// </summary>
        public static HeightLevel ToLevel(float height)
        {
            if (height < (DeepHeight + GroundHeight) * 0.5f)
                return HeightLevel.Deep;
            if (height < (GroundHeight + HighHeight) * 0.5f)
                return HeightLevel.Ground;
            return HeightLevel.High;
        }

        static void TryAdd(ref int mask, int layer)
        {
            if (layer >= 0)
                mask |= 1 << layer;
        }
    }
}
