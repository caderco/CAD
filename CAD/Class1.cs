using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.Runtime;
using System.Windows.Forms;
using System.Drawing;


namespace MyZWCADExtension
{
    public class TunnelForm
    {
        //存储输入参数
        public class Parametwes
        {
            //类型：三星，半圆，梯形
            public string Type { get; set; }
            //宽度
            public string Width { get; set; }
            //长度
            public string Height { get; set; }
        }

        [CommandMethod("Tunnel_Make")]
        public void ShowForm()
        {
            Document doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // 创建主表单
                Form mainForm = new Form()
                {
                    Text = "巷道参数输入",
                    Width = 400,
                    Height = 500,
                    FormBorderStyle = FormBorderStyle.FixedDialog, //设置窗体的边框样式为固定对话框
                    StartPosition = FormStartPosition.CenterScreen //设置窗体首次显示时的位置为屏幕中央。
                };

                //巷道类型选择,创建巷道类型表单
                GroupBox typeGroup = new GroupBox() { Text = "巷道类型", Location = new System.Drawing.Point(20, 20), Size = new System.Drawing.Size(350, 80) };

                //三星形按钮
                RadioButton ThreeStar = new RadioButton() { Text = "三星形", Location = new System.Drawing.Point(20, 20), Checked = true };
                //半圆形按钮
                RadioButton Semicircle = new RadioButton() { Text = "半圆形", Location = new System.Drawing.Point(120, 20) };
                //梯形按钮
                RadioButton Trapezoid = new RadioButton() { Text = "梯形", Location = new System.Drawing.Point(220, 20) };

                typeGroup.Controls.Add(ThreeStar);
                typeGroup.Controls.Add(Semicircle);
                typeGroup.Controls.Add(Trapezoid);


                //创建尺寸菜单，接收输入尺寸
                GroupBox sizeGroup = new GroupBox() { Text = "截面尺寸", Location = new System.Drawing.Point(20, 120), Size = new System.Drawing.Size(350, 100) };

                Label lbWidth = new Label() { Text = "宽度", Location = new System.Drawing.Point(20, 30) };
                Label lbHeight = new Label() { Text = "高度", Location = new System.Drawing.Point(20, 60) };
                TextBox txtWidth = new TextBox() { Location = new System.Drawing.Point(80, 30), Width = 100 };
                TextBox txtHeight = new TextBox() { Location = new System.Drawing.Point(80, 60), Width = 100 };

                sizeGroup.Controls.Add(lbWidth);
                sizeGroup.Controls.Add(txtWidth);
                sizeGroup.Controls.Add(lbHeight);
                sizeGroup.Controls.Add(txtHeight);

                //路径坐标表单，接收坐标 路径点分组
                GroupBox pathGroup = new GroupBox() { Text = "路径坐标输入 (X,Y,Z)", Location = new Point(20, 240), Size = new Size(350, 150) };

                // 坐标输入标签
                Label lblX = new Label() { Text = "X:", Location = new Point(20, 30) };
                Label lblY = new Label() { Text = "Y:", Location = new Point(20, 60) };
                Label lblZ = new Label() { Text = "Z:", Location = new Point(20, 90) };

                // 坐标输入文本框
                TextBox txtX = new TextBox() { Location = new Point(80, 30), Width = 100, Tag = "X" };
                TextBox txtY = new TextBox() { Location = new Point(80, 60), Width = 100, Tag = "Y" };
                TextBox txtZ = new TextBox() { Location = new Point(80, 90), Width = 100, Tag = "Z" };

                pathGroup.Controls.Add(lblX);
                pathGroup.Controls.Add(txtX);
                pathGroup.Controls.Add(lblY);
                pathGroup.Controls.Add(txtY);
                pathGroup.Controls.Add(lblZ);
                pathGroup.Controls.Add(txtZ);

                //确定按钮
                Button buttonOK = new Button() { Text = "确定", Location = new System.Drawing.Point(100, 410), Width = 80 };
                //取消按钮
                Button buttonCancel = new Button() { Text = "取消", Location = new System.Drawing.Point(210, 410), Width = 80 };

                //添加控件到主表单
                mainForm.Controls.Add(typeGroup);
                mainForm.Controls.Add(sizeGroup);
                mainForm.Controls.Add(pathGroup);
                mainForm.Controls.Add(buttonOK);
                mainForm.Controls.Add(buttonCancel);
                mainForm.AcceptButton = buttonOK;
                mainForm.CancelButton = buttonCancel;
                mainForm.ShowDialog();  // 显示窗体


            }
            catch (System.Exception)
            {

                throw;

            }

        }

        public class MyCommands2
    {
        [CommandMethod("Tunnel_Make")]
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

            Document doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

        }

        public void Terminate()
        {
            // 清理代码
        }

        private void AddMyMenu()
        {

            // 获取ZWCAD应用程序对象
            dynamic zwcadApp = ZwSoft.ZwCAD.ApplicationServices.Application.AcadApplication;

            // 获取菜单组
            dynamic menuGroup = zwcadApp.MenuGroups.Item(0);

            // 创建新菜单
            dynamic newMenu = menuGroup.Menus.Add("石门揭煤砖空工具");

            // 添加菜单项（按钮）
            newMenu.AddMenuItem(0, "绘制巷道HD", "Tunnel_Make ");
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
    }
