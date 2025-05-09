using ZwSoft.ZwCAD.Geometry;

namespace CoalLayerDrawer
{
    /// <summary>
    /// 提供供前端调用的接口，接收传入参数并调用绘图核心函数
    /// </summary>
    public class CoalLayerEntry
    {
        /// <summary>
        /// 供前端按钮调用：绘制煤层
        /// </summary>
        /// <param name="tunnelX">巷道口中心 X 坐标</param>
        /// <param name="tunnelY">巷道口中心 Y 坐标</param>
        /// <param name="tunnelZ">巷道口中心 Z 坐标</param>
        /// <param name="offset">煤层中心与巷道口之间的偏移距离（前方方向）</param>
        /// <param name="length">煤层长度（前后方向）</param>
        /// <param name="width">煤层宽度（左右方向）</param>
        /// <param name="height">煤层厚度（上下方向）</param>
        /// <param name="angleX">绕 X 轴的倾角（度）</param>
        /// <param name="angleY">绕 Y 轴的倾角（度）</param>
        /// <param name="angleZ">绕 Z 轴的倾角（度）</param>
        public static void CallDrawCoalLayer(double tunnelX, double tunnelY, double tunnelZ,
                                             double offset, double length, double width, double height,
                                             double angleX, double angleY, double angleZ)
        {
            // 创建三维坐标点表示巷道口中心点
            Point3d tunnelCenter = new Point3d(tunnelX, tunnelY, tunnelZ);

            // 调用绘制煤层的方法
            CoalLayerUtil.DrawCoalLayer3D(tunnelCenter, offset, length, width, height,
                                          angleX, angleY, angleZ);
        }
    }
}
