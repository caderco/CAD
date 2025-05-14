using System;
using System.Collections.Generic;
using System.IO;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace CoalLayerDrawer
{
    public static class CoalLayerUtil
    {

        public static void SavePointsToFile(Dictionary<int, Point3d> points, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Index,X,Y,Z");
                foreach (var kvp in points)
                {
                    Point3d pt = kvp.Value;
                    writer.WriteLine($"{kvp.Key},{pt.X:F4},{pt.Y:F4},{pt.Z:F4}");
                }
            }
        }

        /// <summary>
        /// 根据用户定义的 UCS 坐标系，在模型空间中绘制一个倾斜煤层实体及其表面网格。
        /// </summary>
        /// <param name="ucsOrigin">用户坐标系原点</param>
        /// <param name="ucsX">用户坐标系的 X 轴方向向量</param>
        /// <param name="ucsY">用户坐标系的 Y 轴方向向量</param>
        /// <param name="offset">煤层距 UCS 原点沿 Z 方向的偏移距离</param>
        /// <param name="length">煤层长（沿 X 轴）</param>
        /// <param name="width">煤层宽（沿 Y 轴）</param>
        /// <param name="height">煤层厚度（沿 Z 轴）</param>
        /// <param name="angleXDeg">绕 X 轴的倾斜角度（单位：度）</param>
        /// <param name="angleYDeg">绕 Y 轴的倾斜角度（单位：度）</param>
        /// <param name="gridLength">网格线沿 X 方向的间距</param>
        /// <param name="gridWidth">网格线沿 Y 方向的间距</param>
        public static void DrawCoalLayerByUserUcs(
            Point3d ucsOrigin,
            Vector3d ucsX, Vector3d ucsY,
            double offset,
            double length, double width, double height,
            double angleXDeg, double angleYDeg,
            double gridLength, double gridWidth)
        {
            // 验证 UCS X 和 Y 轴是否正交（点积应为 0）
            double dotXY = ucsX.DotProduct(ucsY);
            if (Math.Abs(dotXY) > 1e-4)
            {
                Application.ShowAlertDialog(
                    $"输入的 UCS X 轴和 Y 轴不正交！（DotProduct = {dotXY:F6}）\n请检查坐标输入。");
                return;
            }

            // 单位化 UCS 坐标轴，确保变换计算稳定
            ucsX = ucsX.GetNormal();
            ucsY = ucsY.GetNormal();
            Vector3d ucsZ = ucsX.CrossProduct(ucsY).GetNormal(); // 通过叉乘计算出正交 Z 轴

            // 构造局部坐标系到世界坐标系的变换矩阵
            Matrix3d ucsToWorld = Matrix3d.AlignCoordinateSystem(
                Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis, // 源坐标系
                ucsOrigin, ucsX, ucsY, ucsZ                                     // 目标 UCS 坐标系
            );

            // 创建煤层实体（长方体）
            Solid3d solid = new Solid3d();
            solid.CreateBox(length, width, height);

            // 把实体从底面对齐到局部原点（便于旋转）
            Vector3d centerOffset = new Vector3d(0, 0, height / 2);
            solid.TransformBy(Matrix3d.Displacement(centerOffset));

            // 向上偏移，使煤层整体位于 UCS Z 轴偏移处
            solid.TransformBy(Matrix3d.Displacement(Vector3d.ZAxis * offset));

            // 设置旋转中心（局部坐标系中位于底面中心、偏移高度的位置）
            Point3d rotationCenter = new Point3d(0, 0, offset);

            // 角度转换为弧度并应用旋转（先绕 X 再绕 Y）
            double radX = angleXDeg * Math.PI / 180.0;
            double radY = angleYDeg * Math.PI / 180.0;
            solid.TransformBy(Matrix3d.Rotation(radX, Vector3d.XAxis, rotationCenter));
            solid.TransformBy(Matrix3d.Rotation(radY, Vector3d.YAxis, rotationCenter));

            // 将实体从局部坐标系转换到世界坐标系
            solid.TransformBy(ucsToWorld);

            // 获取当前数据库对象
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // 获取模型空间（ModelSpace）
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 将煤层实体添加到模型空间
                btr.AppendEntity(solid);
                trans.AddNewlyCreatedDBObject(solid, true);

                // 计算网格线数量（对称分布）
                int xCount = (int)(length / gridLength / 2);
                int yCount = (int)(width / gridWidth / 2);

                // 沿 X 方向绘制网格线（固定 Y 轴，X 变化）
                for (int i = -xCount; i <= xCount; i++)
                {
                    double x = i * gridLength;
                    Point3d p1 = new Point3d(x, -width / 2, 0);
                    Point3d p2 = new Point3d(x, width / 2, 0);

                    Line line = new Line(p1, p2);
                    line.TransformBy(Matrix3d.Displacement(Vector3d.ZAxis * offset));         // 向上偏移
                    line.TransformBy(Matrix3d.Rotation(radX, Vector3d.XAxis, rotationCenter)); // 倾斜
                    line.TransformBy(Matrix3d.Rotation(radY, Vector3d.YAxis, rotationCenter));
                    line.TransformBy(ucsToWorld);                                              // 转到世界坐标系

                    btr.AppendEntity(line);
                    trans.AddNewlyCreatedDBObject(line, true);
                }

                // 沿 Y 方向绘制网格线（固定 X 轴，Y 变化）
                for (int i = -yCount; i <= yCount; i++)
                {
                    double y = i * gridWidth;
                    Point3d p1 = new Point3d(-length / 2, y, 0);
                    Point3d p2 = new Point3d(length / 2, y, 0);

                    Line line = new Line(p1, p2);
                    line.TransformBy(Matrix3d.Displacement(Vector3d.ZAxis * offset));
                    line.TransformBy(Matrix3d.Rotation(radX, Vector3d.XAxis, rotationCenter));
                    line.TransformBy(Matrix3d.Rotation(radY, Vector3d.YAxis, rotationCenter));
                    line.TransformBy(ucsToWorld);

                    btr.AppendEntity(line);
                    trans.AddNewlyCreatedDBObject(line, true);
                }

            //#######################################################################################################

                // 创建交点字典
                Dictionary<int, Point3d> intersectionPoints = new Dictionary<int, Point3d>();
                int pointIndex = 1;

                // 遍历所有交点（在局部坐标系下先求出，然后统一变换）
                for (int i = -xCount; i <= xCount; i++)
                {
                    double x = i * gridLength;
                    for (int j = -yCount; j <= yCount; j++)
                    {
                        double y = j * gridWidth;

                        // 在局部坐标系中的点
                        Point3d localPt = new Point3d(x, y, 0);

                        // 向上偏移
                        localPt = localPt.Add(Vector3d.ZAxis * offset);

                        // 应用倾斜旋转
                        localPt = localPt.TransformBy(Matrix3d.Rotation(radX, Vector3d.XAxis, rotationCenter));
                        localPt = localPt.TransformBy(Matrix3d.Rotation(radY, Vector3d.YAxis, rotationCenter));

                        // 转换到世界坐标系
                        Point3d worldPt = localPt.TransformBy(ucsToWorld);

                        // 添加到字典
                        intersectionPoints.Add(pointIndex++, worldPt);
                    }
                }


                //#####################改为用户自己选保存位置
                SavePointsToFile(intersectionPoints, "C:\\Users\\24257\\Desktop\\CoalLayerGridPoints.csv");

                // 提交事务，保存所有更改
                trans.Commit();
            }
        }
    }
}
