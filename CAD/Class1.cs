using System;
using System.Windows.Forms;
using System.Drawing;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Runtime;

namespace MyZWCADExtension
{
    // 自定义输入窗体，用于输入巷道参数（类型、尺寸、坐标）

    public class TunnelInputForm : Form
    {
        // 公开属性，供外部获取用户输入的巷道参数
        //这些属性允许外部代码获取用户输入的值，但只能在类内部设置（private set）。
        public string TunnelType { get; private set; }
        public double WidthValue { get; private set; }
        public double HeightValue { get; private set; }
        public double CoordX { get; private set; }
        public double CoordY { get; private set; }
        public double CoordZ { get; private set; }

        // UI控件声明
        private GroupBox grpTunnelType;
        private RadioButton rbtnTypeThree_star, rbtnTypeHalf_circle, rbtnTypeOther;

        private GroupBox grpDimension;
        private Label lblWidth, lblHeight;
        private TextBox txtWidth, txtHeight;

        private GroupBox grpCoordinate;
        
        private Button btnOK, btnCancel;

        // 构造函数，初始化窗体及控件位置大小和事件
        public TunnelInputForm()
        {
            this.Text = "巷道参数输入";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;  // 固定大小对话框
            this.StartPosition = FormStartPosition.CenterScreen; // 居中弹出
            this.ClientSize = new Size(320, 380);                 // 窗体大小
            this.MaximizeBox = false;                             // 禁止最大化
            this.MinimizeBox = false;                             // 禁止最小化

            // 巷道类型分组框及三个单选按钮（默认选中“三星形”）
            grpTunnelType = new GroupBox
            {
                Text = "巷道类型",
                Location = new Point(15, 10),
                Size = new Size(290, 70)
            };
            rbtnTypeThree_star = new RadioButton { Text = "三星形", Location = new Point(20, 25), AutoSize = true, Checked = true };
            rbtnTypeHalf_circle = new RadioButton { Text = "半圆形", Location = new Point(110, 25), AutoSize = true };
            rbtnTypeOther = new RadioButton { Text = "梯形", Location = new Point(200, 25), AutoSize = true };
            grpTunnelType.Controls.AddRange(new Control[] { rbtnTypeThree_star, rbtnTypeHalf_circle, rbtnTypeOther });

            // 截面尺寸分组框及宽度、高度标签和输入框
            grpDimension = new GroupBox()
            {
                Text = "截面尺寸",
                Location = new Point(15, 90),
                Size = new Size(290, 120)
            };
           
            // 路径坐标分组框（仅包含文件选择按钮）
            grpCoordinate = new GroupBox()
            {
                Text = "导入路径坐标",
                Location = new Point(15, 220),
                Size = new Size(290, 60)
            };

            // 创建文件选择按钮
            var btnSelectFile = new Button()
            {
                Text = "选择坐标文件",
                Location = new Point(50, 20),
                Size = new Size(200, 30),
                Font = new Font("Microsoft YaHei", 9.5f)
            };

            // 点击事件
            btnSelectFile.Click += (sender, e) =>
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "文本文件|*.txt";
                    openFileDialog.Title = "选择坐标文件";

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string selectedFilePath = openFileDialog.FileName;
                        MessageBox.Show($"已选择文件:\n{selectedFilePath}",
                                       "文件选择成功",
                                       MessageBoxButtons.OK,
                                       MessageBoxIcon.Information);
                    }
                }
            };

            grpCoordinate.Controls.Add(btnSelectFile);

            // 确定和取消按钮，绑定事件，取消直接关闭窗体
            btnOK = new Button() { Text = "确定", Location = new Point(80, 300), Width = 80 };
            btnCancel = new Button() { Text = "取消", Location = new Point(180, 300), Width = 80 };
            btnOK.Click += BtnOK_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            //为类型按钮添加事件处理程序
            rbtnTypeThree_star.CheckedChanged += RadioButton_CheckedChanged;
            rbtnTypeHalf_circle.CheckedChanged += RadioButton_CheckedChanged;
            rbtnTypeOther.CheckedChanged += RadioButton_CheckedChanged;
            // 将控件添加到窗体
            this.Controls.AddRange(new Control[] { grpTunnelType, grpDimension, grpCoordinate, btnOK, btnCancel });

            // 初始化截面尺寸框
            UpdateDimensionControls("三星形");
        }

         // 单选按钮状态改变事件处理程序
        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton != null && radioButton.Checked)
            {
                string tunnelType = radioButton.Text;
                UpdateDimensionControls(tunnelType);
            }
        }

        // 根据巷道类型更新截面尺寸框
        private void UpdateDimensionControls(string tunnelType)
        {
            grpDimension.Controls.Clear(); // 清空原有控件

            Label lbl1 = new Label {  Location = new Point(20, 25), AutoSize = true };
            TextBox txt1 = new TextBox { Location = new Point(80, 22), Width = 80, ImeMode = ImeMode.Disable };
            Label lbl2 = new Label { Location = new Point(20, 60), AutoSize = true };
            TextBox txt2 = new TextBox { Location = new Point(80, 57), Width = 80, ImeMode = ImeMode.Disable };
            grpDimension.Controls.AddRange(new Control[] { lbl1, txt1, lbl2, txt2 });
           
            // 根据巷道类型调整控件
            switch (tunnelType)
            {
                case "三星形":
                    // 三星形可能需要特定的尺寸参数
                    Label lblradius = new Label { Text = "半径:", Location = new Point(20, 95), AutoSize = true };
                    TextBox txtradius = new TextBox { Location = new Point(80, 92), Width = 80, ImeMode = ImeMode.Disable };
                    grpDimension.Controls.AddRange(new Control[] { lblradius, txtradius });
                    lbl1.Text = "宽度:";
                    lbl2.Text = "长度:";
                    break;
                case "半圆形":
                    // 半圆形只要半径
                    lbl1.Text = "半径:";
                    lbl2.Visible = false;
                    txt2.Visible = false;
                    break;
                case "梯形":
                    // 梯形可能需要额外的参数
                    lbl1.Text = "上底长:";
                    lbl2.Text = "下底长:";
                    Label lblHeight = new Label { Text = "高度:", Location = new Point(20, 95), AutoSize = true };
                    TextBox txtHeight = new TextBox { Location = new Point(80, 92), Width = 80, ImeMode = ImeMode.Disable };
                    grpDimension.Controls.AddRange(new Control[] { lblHeight,txtHeight });                   
                    break;
            }

           
        }
        // 确定按钮点击事件：验证输入有效性，赋值属性，关闭窗体返回OK
        private void BtnOK_Click(object sender, EventArgs e)
        {
            // 判断哪个单选按钮被选中，设置巷道类型字符串
            if (rbtnTypeThree_star.Checked)
                TunnelType = "三星形";
            else if (rbtnTypeHalf_circle.Checked)
                TunnelType = "半圆形";
            else
                TunnelType = "梯形";

            // 宽度输入验证
            if (!double.TryParse(txtWidth.Text, out double width))
            {
                MessageBox.Show("宽度请输入有效数字。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtWidth.Focus();
                return;
            }
            // 高度输入验证
            if (!double.TryParse(txtHeight.Text, out double height))
            {
                MessageBox.Show("高度请输入有效数字。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtHeight.Focus();
                return;
            }
           

            // 验证通过后，赋值给公开属性供外部访问
            WidthValue = width;
            HeightValue = height;
           
            // 关闭窗体，返回OK结果
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    //用于绘制煤层参数（走向、倾角、厚度、长宽）
    public class Coal_Bed_MakeForm:Form
    {
        public string Length { get; private set; }
        public string Width_Coal_Bed { get; private set; }
        public string Towards { get; private set; }//走向
        public string Angle { get; private set; }
        public string Thickness { get; private set; }

        private GroupBox grpCoal_Bed;
        private Label lbWidth_Coal_Bed, lbLength, lbThickness, lbAngle, lbTowards;
        private TextBox txtWidth_Coal_Bed, txtLength;


        public Coal_Bed_MakeForm()
        {
            this.Text = "绘制煤层参数输入";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;  // 固定大小对话框
            this.StartPosition = FormStartPosition.CenterScreen; // 居中弹出
            this.ClientSize = new Size(320, 370);                 
            this.MaximizeBox = false;                             
            this.MinimizeBox = false;

            grpCoal_Bed = new GroupBox
            {
                Location = new Point(15, 10),
                Size = new Size(290, 70)
            };
            lbWidth_Coal_Bed = new Label() { Text = "宽度:", Location = new Point(20, 25), AutoSize = true };
            txtWidth_Coal_Bed = new TextBox() { Location = new Point(80, 22), Width = 180, ImeMode = ImeMode.Disable };
            lbLength = new Label() { Text = "长度:", Location = new Point(20, 60), AutoSize = true };

        }
    }

    // ZwCAD 插件主类，继承 IExtensionApplication 接口，这是 ZwCAD 插件的基本接口
    public class TunnelCommands : IExtensionApplication
    {
        // 插件初始化时调用，添加自定义菜单
        public void Initialize()
        {
            AddCustomMenu();
        }

        // 插件卸载时调用，可做资源清理（这里留空）
        public void Terminate()
        {
        }

        // 添加自定义菜单项到ZwCAD菜单栏
        private void AddCustomMenu()
        {
            try
            {
                // 通过动态方式获取ZwCAD应用及菜单组
                dynamic zwcadApp = ZwSoft.ZwCAD.ApplicationServices.Application.AcadApplication;
                dynamic menuGroup = zwcadApp.MenuGroups.Item(0);

                // 防止重复添加菜单，遍历已有菜单检查
                foreach (dynamic m in menuGroup.Menus)
                {
                    if (m.Name == "石门揭煤砖空工具")
                    {
                        return; // 菜单已存在直接返回
                    }
                }

                // 创建新菜单并添加两个菜单项绑定命令名
                dynamic newMenu = menuGroup.Menus.Add("石门揭煤砖空工具");

                newMenu.AddMenuItem(0, "绘制巷道HD", "Tunnel_Make");  // 触发 Tunnel_Make 命令
                newMenu.AddMenuItem(1, "绘制煤层MC", "BUTTON_COMMAND2"); // 触发 BUTTON_COMMAND2 命令

                // 将菜单插入菜单栏第4个位置（索引3）
                newMenu.InsertInMenuBar(3);
            }
            catch (System.Exception ex)
            {
                // 异常捕获并输出到ZwCAD命令行，方便调试
                var doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage($"\n添加菜单失败: {ex.Message}");
                }
            }
        }

        // 自定义命令 “Tunnel_Make”，弹出窗体获取参数，打印输出参数，TODO: 可接入绘图逻辑
        [CommandMethod("Tunnel_Make")]
        public void TunnelMakeCmd()
        {
            var doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            // 创建参数输入窗体，置顶显示
            TunnelInputForm form = new TunnelInputForm();
            form.TopMost = true;
            var dr = form.ShowDialog();  // 显示模态窗体，等待用户输入

            if (dr == DialogResult.OK)
            {
                // 用户点击确认，打印参数到命令行确认参数已正确传递
                ed.WriteMessage("\n--- 开始绘图参数输出 ---");
                ed.WriteMessage($"\n巷道类型: {form.TunnelType}");
                ed.WriteMessage($"\n宽度: {form.WidthValue}");
                ed.WriteMessage($"\n高度: {form.HeightValue}");
                ed.WriteMessage($"\n坐标X: {form.CoordX}");
                ed.WriteMessage($"\n坐标Y: {form.CoordY}");
                ed.WriteMessage($"\n坐标Z: {form.CoordZ}");
                ed.WriteMessage("\n--- 结束绘图参数输出 ---");

                // TODO: 这里写绘图逻辑，使用这些参数
            }
            else
            {
                // 用户取消操作，提示取消信息
                ed.WriteMessage("\n用户取消了输入。");
            }
        }

        // 另一个示范命令
        [CommandMethod("BUTTON_COMMAND2")]
        public void ButtonCommand2()
        {
            var doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            ed.WriteMessage("\n【提示】你的第二个命令触发了。");
            // TODO: 第二个命令绘制煤层的具体逻辑写这里
        }
    }
}
