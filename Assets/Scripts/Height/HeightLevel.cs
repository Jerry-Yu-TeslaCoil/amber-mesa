namespace AmberMesa.Height
{
    /// <summary>
    /// 离散高度带。连续 <c>float height</c> 通过阈值落到其中一带，用于启用对应碰撞层。
    /// </summary>
    public enum HeightLevel
    {
        Deep = 0,
        Ground = 1,
        High = 2,
    }
}
