using System;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;

namespace CoalLayerDrawer
{
    /// <summary>
    /// 煤层绘制工具类，封装核心逻辑以实现 Solid3D 创建与旋转
    /// </summary>
    public static class CoalLayerUtil
    {
        /// <summary>
        /// 根据用户输入参数绘制一个倾斜的煤层长方体
        /// </summary>
        /// <param name="tunnelCenter">巷道口中心点</param>
        /// <param name="offset">煤层中心点距巷道口沿 Z 轴正方向的偏移</param>
        /// <param name="length">煤层长度（前后方向）</param>
        /// <param name="width">煤层宽度（左右方向）</param>
        /// <param name="height">煤层厚度（上下方向）</param>
        /// <param name="angleXDeg">绕 UCS X 轴旋转角度（度）</param>
        /// <param name="angleYDeg">绕 UCS Y 轴旋转角度（度）</param>
        /// <param name="angleZDeg">绕 UCS Z 轴旋转角度（度）</param>
        public static void DrawCoalLayer3D(Point3d tunnelCenter, double offset, double length, double width, double height,
                                           double angleXDeg, double angleYDeg, double angleZDeg)
        {
            // 获取当前 CAD 数据库
            Database db = HostApplicationServices.WorkingDatabase;

            // 开启事务处理
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // 获取块表
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);

                // 获取模型空间块表记录（用于写入新图元）
                BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 获取当前文档对象和用户坐标系（UCS）
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                Matrix3d ucsMat = ed.CurrentUserCoordinateSystem; // 当前 UCS 转换矩阵
                CoordinateSystem3d ucs = ucsMat.CoordinateSystem3d; // 提取坐标轴信息

                // 根据偏移量计算煤层中心点（Zaxis 表示前方方向）
                Point3d coalCenter = tunnelCenter + ucs.Zaxis.MultiplyBy(offset);

                // 创建 Solid3d 长方体对象（从原点起始）
                Solid3d solid = new Solid3d();
                solid.CreateBox(length, width, height); // 以原点为角点创建立方体

                // 计算需要平移的向量（将立方体从原点移动到目标中心点）
                Vector3d moveToCenter = new Vector3d(
                    coalCenter.X - length / 2,
                    coalCenter.Y - width / 2,
                    coalCenter.Z - height / 2
                );

                // 执行平移（从原点移动至煤层中心）
                solid.TransformBy(Matrix3d.Displacement(moveToCenter - Point3d.Origin.GetAsVector()));

                // 将角度从度转换为弧度
                double radX = angleXDeg * Math.PI / 180.0;
                double radY = angleYDeg * Math.PI / 180.0;
                double radZ = angleZDeg * Math.PI / 180.0;

                // 执行绕煤层中心旋转：绕 X、Y、Z 分别倾斜
                solid.TransformBy(Matrix3d.Rotation(radX, ucs.Xaxis, coalCenter));
                solid.TransformBy(Matrix3d.Rotation(radY, ucs.Yaxis, coalCenter));
                solid.TransformBy(Matrix3d.Rotation(radZ, ucs.Zaxis, coalCenter));

                // 将实体添加到模型空间
                btr.AppendEntity(solid);
                trans.AddNewlyCreatedDBObject(solid, true);

                // 提交事务，完成操作
                trans.Commit();
            }
        }
    }
}
