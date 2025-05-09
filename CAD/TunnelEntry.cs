using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using MyZWCADExtension;
using System.Windows.Forms;

namespace TunnelDrawer
{
    
    public static class TunnelUtil
    {

        /// <summary>
        /// 根据路径文件和截面参数绘制三维巷道
        /// </summary>
        /// <param name="tunnelType">截面类型（如“三星型”、“半圆型”、“梯型”）</param>
        /// <param name="parameters">截面参数</param>
        /// <param name="filePath">路径坐标文件（txt）</param>
        public static void DrawTunnelByPath(string tunnelType, double[] parameters, string filePath)
        {
            // 获取当前文档对象
            Document doc =ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 从 TXT 文件读取路径点
            List<Point3d> pathPoints = ReadPointsFromTxt(filePath, ed);
            if (pathPoints.Count < 2)
            {
                ed.WriteMessage("\n路径坐标不足，无法绘制。");
                return;
            }

            // 启动事务
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // 打开块表和模型空间记录
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 创建 Polyline3d 实体用于路径
                Polyline3d path = new Polyline3d();
                btr.AppendEntity(path);
                tr.AddNewlyCreatedDBObject(path, true);

                // 添加路径点为 Polyline3d 的顶点
                foreach (Point3d pt in pathPoints)
                {
                    PolylineVertex3d vertex = new PolylineVertex3d(pt);
                    path.AppendVertex(vertex);
                    tr.AddNewlyCreatedDBObject(vertex, true);
                }

                ed.WriteMessage("\n三维路径已添加");

                // 生成截面区域（Region）
                Region section = GenerateTunnelSection(tunnelType, parameters, ed);
                if (section == null)
                {
                    ed.WriteMessage("\n截面区域生成失败。");
                    return;
                }

                // 将截面移动到路径起点位置
                Matrix3d moveToPathStart = Matrix3d.Displacement(pathPoints[0] - Point3d.Origin);
                section.TransformBy(moveToPathStart);
                ed.WriteMessage($"\n截面已移动到路径起点：{pathPoints[0]}");

                // 使用截面和路径扫掠生成实体
                Solid3d tunnel = new Solid3d();
                tunnel.SetDatabaseDefaults();

                SweepOptionsBuilder sb = new SweepOptionsBuilder();
                sb.Align = SweepOptionsAlignOption.AlignSweepEntityToPath;
                SweepOptions so = sb.ToSweepOptions();

                try
                {
                    tunnel.CreateSweptSolid(section, path, so);
                    btr.AppendEntity(tunnel);
                    tr.AddNewlyCreatedDBObject(tunnel, true);
                    ed.WriteMessage("\n巷道实体生成成功。");
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n扫掠失败: {ex.Message}");
                }

                // 提交事务
                tr.Commit();
            }
        }

        /// <summary>
        /// 从 TXT 文件读取三维坐标点
        /// </summary>
        public static List<Point3d> ReadPointsFromTxt(string path, Editor ed)
        {
            List<Point3d> points = new List<Point3d>();
            try
            {
                // 按行读取
                foreach (string line in File.ReadAllLines(path))
                {
                    // 分隔符可为空格、逗号或 Tab
                    string[] parts = line.Trim().Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
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
                        if (p.Length < 2)
                        {
                            ed.WriteMessage("\n参数不足，需提供 [宽, 高]");
                            return null;
                        }
                        //MessageBox.Show(p[0].ToString());
                        double w = p[0];  // 总宽度
                        
                        double h = p[1];  // 总高度
                        double wall = w * 0.05; // 默认墙厚

                        double rectHeight = h - w / 2; // 去除半圆后剩余的矩形高度
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

                        ///
                    
                        
                        double irh = rectHeight - wall;
                        ///
    
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

                       // MessageBox.Show(p[0].ToString());
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
                        if (outerRegions.Count == 0 || innerRegions.Count == 0)
                            return null;

                        Region outerR = outerRegions[0] as Region;
                        Region innerR = innerRegions[0] as Region;

                        outerR.BooleanOperation(BooleanOperationType.BoolSubtract, innerR);
                        return outerR;
                    }

                default:
                    ed.WriteMessage("\n未知的巷道类型");
                    return null;
            }
        }
    }
}
