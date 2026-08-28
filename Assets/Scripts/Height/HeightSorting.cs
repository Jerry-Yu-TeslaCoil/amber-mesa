namespace AmberMesa.Height
{
    /// <summary>
    /// 视觉 Sorting Layer（自下而上）。
    /// Floor 只铺地；角色与 Face（崖面/立面）同层，靠 Y 轴排序遮挡——站在地上的人绝不和 Floor 抢同一层。
    /// </summary>
    public static class HeightSorting
    {
        public const string DeepFloor = "DeepFloor";
        public const string DeepFace = "DeepFace";
        public const string GroundFloor = "GroundFloor";
        public const string GroundFace = "GroundFace";
        public const string HighFloor = "HighFloor";
        public const string High = "High";

        /// <summary>
        /// 角色对齐的层：Deep→Lower Face，Ground→Upper Face，High 置顶。
        /// </summary>
        public static string ActorLayerName(HeightLevel level) => level switch
        {
            HeightLevel.Deep => DeepFace,
            HeightLevel.Ground => GroundFace,
            HeightLevel.High => High,
            _ => GroundFace,
        };
    }
}
