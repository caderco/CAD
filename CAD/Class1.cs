using System;
// 正确的using语句示例
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.Runtime;


namespace MyZWCADExtension
{
    public class MyCommands1
    {//在这里面写入执行按钮的代码
        [CommandMethod("BUTTON_COMMAND1")]
        public void MyButtonCommand1()
        {
            //实现绘制巷道HD
            Document zcDoc = Application.DocumentManager.MdiActiveDocument;
            Database zcDB = zcDoc.Database;
            Transaction ZcTran = zcDoc.TransactionManager.StartTransaction();
            using (ZcTran)
            {
                BlockTable zcBLT = (BlockTable)ZcTran.GetObject(zcDB.BlockTableId, OpenMode.ForRead);
                BlockTableRecord zcBLTR = (BlockTableRecord)ZcTran.GetObject(zcBLT[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                Circle zcCircle = new Circle();
                zcCircle.Center = new Point3d(2, 3, 0);
                zcCircle.Radius = 10;
                zcCircle.ColorIndex = 1;
                zcBLTR.AppendEntity(zcCircle);
                ZcTran.AddNewlyCreatedDBObject(zcCircle, true);
                ZcTran.Commit();
            }
            zcDoc.SendStringToExecute("_ZOOM E ", false, false, false);

        }
    }
    public class MyCommands2
    {
        [CommandMethod("BUTTON_COMMAND2")]
        public void MyButtonCommand2()
        {
            //实现绘制煤层QD

        }
    }
    public class MyCommands3
    {
        [CommandMethod("BUTTON_COMMAND3")]
        public void MyButtonCommand3()
        {
            //实现绘制砖孔起点QD

        }
    }
    public class MyCommands4
    {
        [CommandMethod("BUTTON_COMMAND4")]
        public void MyButtonCommand4()
        {
            //实现绘制砖孔ZK

        }
    }
    public class MyCommands5
    {
        [CommandMethod("BUTTON_COMMAND5")]
        public void MyButtonCommand5()
        {
            //实现砖号编号BH

        }
    }
    public class MyCommands6
    {
        [CommandMethod("BUTTON_COMMAND6")]
        public void MyButtonCommand6()
        {
            //实现设计表格SJ

        }
    }
    public class MyCommands7
    {
        [CommandMethod("BUTTON_COMMAND7")]
        public void MyButtonCommand7()
        {
            //实现竣工表格JB

        }
    }
    public class MyCommands8
    {
        [CommandMethod("BUTTON_COMMAND8")]
        public void MyButtonCommand8()
        {
            //实现竣工3D图JG

        }
    }
    public class MyCommands9
    {
        [CommandMethod("BUTTON_COMMAND9")]
        public void MyButtonCommand9()
        {
            //实现绘制面图PM

        }
    }
    public class MyCommands10
    {
        [CommandMethod("BUTTON_COMMAND10")]
        public void MyButtonCommand10()
        {
            //实现参数对比DB

        }
    }

    public class MyExtension : IExtensionApplication
    {
        public void Initialize()
        {
            // 添加菜单和按钮
            AddMyMenu();

            Document doc = Application.DocumentManager.MdiActiveDocument;

        }

        public void Terminate()
        {
            // 清理代码
        }

        private void AddMyMenu()
        {

            // 获取ZWCAD应用程序对象
            dynamic zwcadApp = Application.AcadApplication;

            // 获取菜单组
            dynamic menuGroup = zwcadApp.MenuGroups.Item(0);

            // 创建新菜单
            dynamic newMenu = menuGroup.Menus.Add("石门揭煤砖空工具");

            // 添加菜单项（按钮）
            newMenu.AddMenuItem(0, "绘制巷道HD", "BUTTON_COMMAND1 ");
            newMenu.AddMenuItem(1, "绘制煤层MC", "BUTTON_COMMAND2 ");
            newMenu.AddMenuItem(2, "砖孔起点QD", "BUTTON_COMMAND3 ");
            newMenu.AddMenuItem(3, "绘制砖孔ZK", "BUTTON_COMMAND4 ");
            newMenu.AddMenuItem(4, "砖号编号BH", "BUTTON_COMMAND5 ");
            newMenu.AddMenuItem(5, "设计表格SJ", "BUTTON_COMMAND6 ");
            newMenu.AddMenuItem(6, "竣工表格JB", "BUTTON_COMMAND7 ");
            newMenu.AddMenuItem(7, "竣工3D图JG", "BUTTON_COMMAND8 ");
            newMenu.AddMenuItem(8, "绘制面图PM", "BUTTON_COMMAND9 ");
            newMenu.AddMenuItem(9, "参数对比DB", "BUTTON_COMMAND10 ");

            int insertPosition = 3; // 从 0 开始计数
            newMenu.InsertInMenuBar(insertPosition);


        }
    }
}
