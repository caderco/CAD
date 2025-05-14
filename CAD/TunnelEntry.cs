using System;
using System.Collections.Generic;
using System.IO;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;

namespace TunnelDrawer
{

    public class TunnelUtil
    {
        public static string Origin { get; private set; }
        public static string OriginX { get; private set; }
        public static string OriginY { get; private set; }



        /// <summary>
        /// 根据路径文件和截面参数绘制三维巷道
        /// </summary>
        /// <param name="tunnelType">截面类型（如“三星形”、“半圆形”、“梯形”）</param>
        /// <param name="parameters">截面参数</param>
        /// <param name="filePath">路径坐标文件（txt）</param>
        public void DrawTunnelByPath(string tunnelType, double[] parameters, string filePath)
        {
            // 获取当前活动文档
            Document doc = Application.DocumentManager.MdiActiveDocument;
            // 获取文档编辑器
            Editor ed = doc.Editor;
            // 获取文档数据库
            Database db = doc.Database;

            // 从文本文件中读取路径点
            List<Point3d> pathPoints = ReadPointsFromTxt(filePath, ed);
            // 如果路径点数量小于2，无法绘制
            if (pathPoints.Count < 2)
            {
                ed.WriteMessage("\n路径坐标不足，无法绘制。");
                return;
            }

            // 开始事务
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // 获取块表，以只读方式打开
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                // 获取模型空间块表记录，以写入方式打开
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 创建路径 Polyline3d
                Polyline3d path = new Polyline3d();
                // 将路径添加到模型空间块表记录中
                btr.AppendEntity(path);
                // 将新创建的对象添加到事务中
                tr.AddNewlyCreatedDBObject(path, true);

                // 遍历路径点，添加到路径 Polyline3d 中
                foreach (Point3d pt in pathPoints)
                {
                    PolylineVertex3d vertex = new PolylineVertex3d(pt);
                    path.AppendVertex(vertex);
                    tr.AddNewlyCreatedDBObject(vertex, true);
                }

                ed.WriteMessage("\n三维路径已添加");

                // 生成巷道截面
                Region section = GenerateTunnelSection(tunnelType, parameters, ed);
                // 如果截面生成失败，返回
                if (section == null)
                {
                    ed.WriteMessage("\n截面区域生成失败。");
                    return;
                }

                // 平移截面，使其底边中心对齐原点
                Extents3d secExt = section.GeometricExtents;
                double centerX = (secExt.MinPoint.X + secExt.MaxPoint.X) * 0.5;
                double minY = secExt.MinPoint.Y;
                Point3d basePoint = new Point3d(centerX, minY, 0);
                Matrix3d alignToOrigin = Matrix3d.Displacement(Point3d.Origin - basePoint);
                section.TransformBy(alignToOrigin);

                // 将截面平移到路径起点
                Point3d startPt = pathPoints[0];
                Matrix3d moveToPathStart = Matrix3d.Displacement(startPt - Point3d.Origin);
                section.TransformBy(moveToPathStart);
                ed.WriteMessage($"\n截面已移动到路径起点中心：{startPt}");

                // 执行扫掠操作
                Solid3d tunnel = new Solid3d();
                tunnel.SetDatabaseDefaults();

                SweepOptionsBuilder sb = new SweepOptionsBuilder();
                sb.Align = SweepOptionsAlignOption.AlignSweepEntityToPath;
                SweepOptions so = sb.ToSweepOptions();

                try
                {
                    // 使用截面和路径进行扫掠，创建巷道实体
                    tunnel.CreateSweptSolid(section, path, so);
                    // 将巷道实体添加到模型空间块表记录中
                    btr.AppendEntity(tunnel);
                    // 将新创建的对象添加到事务中
                    tr.AddNewlyCreatedDBObject(tunnel, true);
                    ed.WriteMessage("\n巷道实体生成成功。");
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n扫掠失败: {ex.Message}");
                    return;
                }

                // 计算终点并设置 UCS 坐标系
                Point3d endPt = pathPoints[pathPoints.Count - 1];
                Point3d secondLastPt = pathPoints[pathPoints.Count - 2];
                // 计算巷道方向（Z轴）
                Vector3d zAxis = (endPt - secondLastPt).GetNormal();
                // 计算X轴方向
                Vector3d xAxis = zAxis.CrossProduct(Vector3d.ZAxis).GetNormal().Negate();
                // 计算Y轴方向
                Vector3d yAxis = zAxis.CrossProduct(xAxis).GetNormal();

                // 创建 UCS
                CoordinateSystem3d cs = new CoordinateSystem3d(endPt, xAxis, yAxis);

                // 设置当前 UCS
                ed.CurrentUserCoordinateSystem = Matrix3d.AlignCoordinateSystem(
                    Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                    cs.Origin, cs.Xaxis, cs.Yaxis, cs.Zaxis
                );

                // 输出UCS详细信息
                CoordinateSystem3d ucs = ed.CurrentUserCoordinateSystem.CoordinateSystem3d;

                // 放入设置好的共用变量中
                Origin = $"({cs.Origin.X:F3}, {cs.Origin.Y:F3}, {cs.Origin.Z:F3})";
                OriginX = $"({cs.Xaxis.X:F3}, {cs.Xaxis.Y:F3}, {cs.Xaxis.Z:F3})";
                OriginY = $"({cs.Yaxis.X:F3}, {cs.Yaxis.Y:F3}, {cs.Yaxis.Z:F3})";
                //Vector3d finalZAxis = cs.Xaxis.CrossProduct(cs.Yaxis);
                //string zAxisStr = $"({finalZAxis.X:F3}, {finalZAxis.Y:F3}, {finalZAxis.Z:F3})";

                //ed.WriteMessage("\n当前用户坐标（UCS）设置：");
                //ed.WriteMessage($"\n原点： {originStr}");
                //ed.WriteMessage($"\nX轴方向： {xAxisStr}");
                //ed.WriteMessage($"\nY轴方向： {yAxisStr}");
                ////ed.WriteMessage($"\nZ轴方向： {zAxisStr}");
                ed.WriteMessage("\n请复制以上信息，粘贴到煤层命令作为坐标轴输入。");

                // 提交事务
                tr.Commit();
            }
        }

        /// <summary>
        /// 从 TXT 文件读取三维坐标点
        /// </summary>
        public  List<Point3d> ReadPointsFromTxt(string path, Editor ed)
        {
            List<Point3d> points = new List<Point3d>();
            try
            {
                // 按行读取文件内容
                foreach (string line in File.ReadAllLines(path))
                {
                    // 按空格、逗号或Tab分隔行内容
                    string[] parts = line.Trim().Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    // 如果分隔后的数组长度大于等于3，且能正确解析出X、Y、Z坐标
                    if (parts.Length >= 3 &&
                        double.TryParse(parts[0], out double x) &&
                        double.TryParse(parts[1], out double y) &&
                        double.TryParse(parts[2], out double z))
                    {
                        points.Add(new Point3d(x, y, z));
                    }
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n读取坐标文件错误: {ex.Message}");
            }
            return points;
        }

        /// <summary>
        /// 根据巷道类型和参数生成对应的 Region 作为截面
        /// </summary>
        public static Region GenerateTunnelSection(string type, double[] p, Editor ed)
        {
            switch (type.ToLower())
            {
                case "三星形":
                    {
                        // 如果参数数量小于2，参数不足
                        if (p.Length < 2)
                        {
                            ed.WriteMessage("\n参数不足，需提供 [宽, 高]");
                            return null;
                        }

                        double w = p[0];  // 总宽度
                        double h = p[1];  // 总高度
                        double wall = w * 0.05; // 默认墙厚

                        double rectHeight = h - w / 2; // 去除半圆后剩余的矩形高度
                        // 如果矩形高度小于墙厚，巷道太小，无法生成空心结构
                        if (rectHeight < wall)
                        {
                            ed.WriteMessage("\n巷道太小，无法生成空心结构");
                            return null;
                        }

                        // 外矩形轮廓
                        Polyline outer = new Polyline();
                        outer.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
                        outer.AddVertexAt(1, new Point2d(w, 0), 0, 0, 0);
                        outer.AddVertexAt(2, new Point2d(w, rectHeight), 1, 0, 0);
                        outer.AddVertexAt(3, new Point2d(0, rectHeight), 0, 0, 0);
                        outer.Closed = true;

                        // 内矩形轮廓（减去墙厚）
                        double iw = w - 2 * wall;
                        double irh = rectHeight - wall;
                        // 如果内部空间不足，检查墙厚或整体尺寸
                        if (iw <= 0 || irh <= 0)
                        {
                            ed.WriteMessage("\n内部空间不足，检查墙厚或整体尺寸");
                            return null;
                        }

                        Polyline inner = new Polyline();
                        inner.AddVertexAt(0, new Point2d(wall, wall), 0, 0, 0);
                        inner.AddVertexAt(1, new Point2d(w - wall, wall), 0, 0, 0);
                        inner.AddVertexAt(2, new Point2d(w - wall, irh), 1, 0, 0);
                        inner.AddVertexAt(3, new Point2d(wall, irh), 0, 0, 0);
                        inner.Closed = true;

                        // 构造区域并做差集运算
                        DBObjectCollection outerCurves = new DBObjectCollection(); outerCurves.Add(outer);
                        DBObjectCollection innerCurves = new DBObjectCollection(); innerCurves.Add(inner);

                        DBObjectCollection outerRegions = Region.CreateFromCurves(outerCurves);
                        DBObjectCollection innerRegions = Region.CreateFromCurves(innerCurves);
                        // 如果外区域或内区域创建失败，返回null
                        if (outerRegions.Count == 0 || innerRegions.Count == 0)
                        {
                            ed.WriteMessage("\n区域创建失败");
                            return null;
                        }

                        Region outerR = outerRegions[0] as Region;
                        Region innerR = innerRegions[0] as Region;

                        outerR.BooleanOperation(BooleanOperationType.BoolSubtract, innerR);
                        return outerR;
                    }

                case "半圆形":
                    {
                        double r = p[0];                // 外半径
                        double wall = r * 0.1;          // 默认墙厚

                        //if (r <= wall)
                        //{
                        //    ed.WriteMessage("\n半径过小，生成空心结构失败");
                        //    return null;
                        //}

                        // 设置半圆中心位置
                        Point3d center = new Point3d(r, 0, 0);

                        // 外部半圆及其底边（弦）
                        Arc arcOuter = new Arc(center, r, 0, Math.PI);
                        Line chordOuter = new Line(arcOuter.EndPoint, arcOuter.StartPoint);

                        // 内部半圆及其底边
                        double innerRadius = r - wall;
                        Arc arcInner = new Arc(center, innerRadius, 0, Math.PI);
                        Line chordInner = new Line(arcInner.StartPoint, arcInner.EndPoint); // 注意方向

                        // 构造区域并做差集
                        DBObjectCollection outerCurves = new DBObjectCollection();
                        outerCurves.Add(arcOuter); outerCurves.Add(chordOuter);
                        DBObjectCollection innerCurves = new DBObjectCollection();
                        innerCurves.Add(arcInner); innerCurves.Add(chordInner);

                        DBObjectCollection outerRegions = Region.CreateFromCurves(outerCurves);
                        DBObjectCollection innerRegions = Region.CreateFromCurves(innerCurves);
                        // 如果外区域或内区域创建失败，返回null
                        if (outerRegions.Count == 0 || innerRegions.Count == 0)
                            return null;

                        Region outerR = outerRegions[0] as Region;
                        Region innerR = innerRegions[0] as Region;
                        outerR.BooleanOperation(BooleanOperationType.BoolSubtract, innerR);

                        // 添加矩形底面
                        using (Polyline baseRect = new Polyline())
                        {
                            baseRect.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
                            baseRect.AddVertexAt(1, new Point2d(0, wall), 0, 0, 0);
                            baseRect.AddVertexAt(2, new Point2d(2 * r, wall), 0, 0, 0);
                            baseRect.AddVertexAt(3, new Point2d(2 * r, 0), 0, 0, 0);
                            baseRect.Closed = true;

                            DBObjectCollection baseRegionSet = Region.CreateFromCurves(new DBObjectCollection { baseRect });
                            // 如果底面区域创建成功，将其与外区域进行并集运算
                            if (baseRegionSet.Count > 0)
                            {
                                Region baseRegion = baseRegionSet[0] as Region;
                                outerR.BooleanOperation(BooleanOperationType.BoolUnite, baseRegion);
                            }
                        }

                        return outerR;
                    }

                case "梯形":
                    {
                        double top = p[0], bot = p[1], height = p[2];
                        double wall = top * 0.05; // 默认墙厚
                        double offset = (bot - top) / 2;

                        // 如果尺寸太小，生成空心结构失败
                        if (top <= 2 * wall || bot <= 2 * wall || height <= 2 * wall)
                        {
                            ed.WriteMessage("\n尺寸太小，生成空心结构失败");
                            return null;
                        }

                        // 外轮廓梯形
                        Polyline outer = new Polyline();
                        outer.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
                        outer.AddVertexAt(1, new Point2d(bot, 0), 0, 0, 0);
                        outer.AddVertexAt(2, new Point2d(offset + top, height), 0, 0, 0);
                        outer.AddVertexAt(3, new Point2d(offset, height), 0, 0, 0);
                        outer.Closed = true;

                        // 内轮廓梯形（减去墙厚）
                        double in_top = top - 2 * wall;
                        double in_bot = bot - 2 * wall;
                        double in_height = height - wall * 2;
                        double in_offset = (in_bot - in_top) / 2;

                        // 如果内部空间不足，返回null
                        if (in_top <= 0 || in_bot <= 0 || in_height <= 0)
                        {
                            ed.WriteMessage("\n内部空间不足");
                            return null;
                        }

                        Polyline inner = new Polyline();
                        inner.AddVertexAt(0, new Point2d(wall, wall), 0, 0, 0);
                        inner.AddVertexAt(1, new Point2d(wall + in_bot, wall), 0, 0, 0);
                        inner.AddVertexAt(2, new Point2d(wall + in_offset + in_top, wall + in_height), 0, 0, 0);
                        inner.AddVertexAt(3, new Point2d(wall + in_offset, wall + in_height), 0, 0, 0);
                        inner.Closed = true;

                        DBObjectCollection outerCurves = new DBObjectCollection(); outerCurves.Add(outer);
                        DBObjectCollection innerCurves = new DBObjectCollection(); innerCurves.Add(inner);
                        DBObjectCollection outerRegions = Region.CreateFromCurves(outerCurves);
                        DBObjectCollection innerRegions = Region.CreateFromCurves(innerCurves);
                        // 如果外区域或内区域创建失败，返回null
                        if (outerRegions.Count == 0 || innerRegions.Count == 0)
                            return null;

                        Region outerR = outerRegions[0] as Region;
                        Region innerR = innerRegions[0] as Region;

                        outerR.BooleanOperation(BooleanOperationType.BoolSubtract, innerR);
                        return outerR;
                    }
///##############################################################################################
                case "自定义":
                    {
                        // 获取当前数据库对象
                        Database db = ed.Document.Database;

                        ed.WriteMessage("\n请选择一个封闭的二维图形（如多段线、多边形、圆等）作为巷道截面。完成后按回车。");

                        // 创建选择图形的提示对象，只允许选择指定类型（封闭二维图形）
                        PromptEntityOptions peo = new PromptEntityOptions("\n选择封闭图形：");
                        peo.SetRejectMessage("\n请选择封闭二维图形。");
                        peo.AddAllowedClass(typeof(Polyline), true);
                        peo.AddAllowedClass(typeof(Circle), true);
                        peo.AddAllowedClass(typeof(Ellipse), true);

                        // 等待用户选择一个图形
                        PromptEntityResult per = ed.GetEntity(peo);
                        if (per.Status != PromptStatus.OK)
                        {
                            ed.WriteMessage("\n未选择有效图形，操作取消。");
                            return null;
                        }

                        // 默认墙厚设置为 20000（后续根据图形尺寸自动调整）
                        double wall = 20000;
                        Point3d basePoint = Point3d.Origin;
                        Entity outerEntity = null;
                        Entity innerEntity = null;

                        // 开始事务操作
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            // 读取用户选择的图形实体
                            outerEntity = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                            if (outerEntity == null)
                            {
                                ed.WriteMessage("\n选中的不是有效图形。");
                                return null;
                            }

                            // 检查如果是多段线，必须闭合
                            if (outerEntity is Polyline pl && !pl.Closed)
                            {
                                ed.WriteMessage("\n错误：所选多段线未闭合。");
                                return null;
                            }

                            // 克隆图形，用于后续几何操作
                            Entity outerClone = outerEntity.Clone() as Entity;
                            if (outerClone == null)
                            {
                                ed.WriteMessage("\n图形克隆失败。");
                                return null;
                            }

                            Extents3d ext = outerClone.GeometricExtents;
                            double width = ext.MaxPoint.X - ext.MinPoint.X;
                            double height = ext.MaxPoint.Y - ext.MinPoint.Y;
                            double minSize = Math.Min(width, height);

                            wall = Math.Max(50, Math.Min(wall, minSize * 0.5));
                            ed.WriteMessage($"\n墙厚自动设置为：{wall:f2}");

                            // 计算图形中心点
                            Point3d min = ext.MinPoint;
                            Point3d max = ext.MaxPoint;
                            basePoint = new Point3d((min.X + max.X) * 0.5, (min.Y + max.Y) * 0.5, 0);

                            // 缩放生成内轮廓
                            if (outerClone is Polyline pline)
                            {
                                Polyline innerPl = (Polyline)pline.Clone();
                                double scaleRatio = (pline.Length - 2 * wall) / pline.Length;
                                if (scaleRatio <= 0.01)
                                {
                                    ed.WriteMessage("\n图形太小，无法缩放生成内壁。");
                                }
                                else
                                {
                                    Matrix3d scaleMat = Matrix3d.Scaling(scaleRatio, basePoint);
                                    innerPl.TransformBy(scaleMat);
                                    innerEntity = innerPl;
                                }
                            }
                            else if (outerClone is Curve outerCurve)
                            {
                                try
                                {
                                    DBObjectCollection offsets = outerCurve.GetOffsetCurves(-wall);
                                    if (offsets.Count > 0)
                                    {
                                        innerEntity = offsets[0] as Entity;
                                    }
                                }
                                catch { }
                            }

                            if (innerEntity == null)
                            {
                                ed.WriteMessage("\n内轮廓生成失败。");
                                return null;
                            }
                        }
                        DBObjectCollection outerCurves = new DBObjectCollection();
                        outerCurves.Add(outerEntity); // 外轮廓集合
                        DBObjectCollection innerCurves = new DBObjectCollection();
                        innerCurves.Add(innerEntity); // 内轮廓集合

                        // 将多段线转为面域
                        DBObjectCollection outerRegions = Region.CreateFromCurves(outerCurves);
                        DBObjectCollection innerRegions = Region.CreateFromCurves(innerCurves);

                        if (outerRegions.Count == 0 || innerRegions.Count == 0)
                            return null; // 转换失败处理

                        // 执行布尔差集运算
                        Region outerR = outerRegions[0] as Region;
                        Region innerR = innerRegions[0] as Region;
                        outerR.BooleanOperation(BooleanOperationType.BoolSubtract, innerR);

                        return outerR; // 返回空心梯形面域

                    }
//############################################################################################################

                default:
                    // 处理未知巷道类型
                    ed.WriteMessage("\n未知的巷道类型");
                    return null;
            }
        }
    }
}
