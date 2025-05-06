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
        public string TunnelType { get; private set; }
        public double WidthValue { get; private set; }
        public double HeightValue { get; private set; }
        public double CoordX { get; private set; }
        public double CoordY { get; private set; }
        public double CoordZ { get; private set; }

        // UI控件声明
        private GroupBox grpTunnelType;
        private RadioButton rbtnTypeSingle, rbtnTypeDouble, rbtnTypeOther;

        private GroupBox grpDimension;
        private Label lblWidth, lblHeight;
        private TextBox txtWidth, txtHeight;

        private GroupBox grpCoordinate;
        private Label lblX, lblY, lblZ;
        private TextBox txtX, txtY, txtZ;

        private Button btnOK, btnCancel;

        // 构造函数，初始化窗体及控件位置大小和事件
        public TunnelInputForm()
        {
            this.Text = "巷道参数输入";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;  // 固定大小对话框
            this.StartPosition = FormStartPosition.CenterScreen; // 居中弹出
            this.ClientSize = new Size(320, 370);                 // 窗体大小
            this.MaximizeBox = false;                             // 禁止最大化
            this.MinimizeBox = false;                             // 禁止最小化

            // 巷道类型分组框及三个单选按钮（默认选中“三星形”）
            grpTunnelType = new GroupBox
            {
                Text = "巷道类型",
                Location = new Point(15, 10),
                Size = new Size(290, 70)
            };
            rbtnTypeSingle = new RadioButton { Text = "三星形", Location = new Point(20, 25), AutoSize = true, Checked = true };
            rbtnTypeDouble = new RadioButton { Text = "半圆形", Location = new Point(110, 25), AutoSize = true };
            rbtnTypeOther = new RadioButton { Text = "梯形", Location = new Point(200, 25), AutoSize = true };
            grpTunnelType.Controls.AddRange(new Control[] { rbtnTypeSingle, rbtnTypeDouble, rbtnTypeOther });

            // 截面尺寸分组框及宽度、高度标签和输入框
            grpDimension = new GroupBox()
            {
                Text = "截面尺寸",
                Location = new Point(15, 90),
                Size = new Size(290, 90)
            };
            lblWidth = new Label { Text = "宽度:", Location = new Point(20, 25), AutoSize = true };
            txtWidth = new TextBox { Location = new Point(80, 22), Width = 180, ImeMode = ImeMode.Disable };
            lblHeight = new Label { Text = "高度:", Location = new Point(20, 60), AutoSize = true };
            txtHeight = new TextBox { Location = new Point(80, 57), Width = 180, ImeMode = ImeMode.Disable };
            grpDimension.Controls.AddRange(new Control[] { lblWidth, txtWidth, lblHeight, txtHeight });

            // 路径坐标输入分组及X、Y、Z坐标输入框
            grpCoordinate = new GroupBox()
            {
                Text = "路径坐标输入",
                Location = new Point(15, 190),
                Size = new Size(290, 120)
            };
            lblX = new Label() { Text = "X:", Location = new Point(20, 35), AutoSize = true };
            txtX = new TextBox() { Location = new Point(50, 32), Width = 80, ImeMode = ImeMode.Disable };
            lblY = new Label() { Text = "Y:", Location = new Point(150, 35), AutoSize = true };
            txtY = new TextBox() { Location = new Point(180, 32), Width = 80, ImeMode = ImeMode.Disable };
            lblZ = new Label() { Text = "Z:", Location = new Point(20, 75), AutoSize = true };
            txtZ = new TextBox() { Location = new Point(50, 72), Width = 80, ImeMode = ImeMode.Disable };
            grpCoordinate.Controls.AddRange(new Control[] { lblX, txtX, lblY, txtY, lblZ, txtZ });

            // 确定和取消按钮，绑定事件，取消直接关闭窗体
            btnOK = new Button() { Text = "确定", Location = new Point(80, 320), Width = 80 };
            btnCancel = new Button() { Text = "取消", Location = new Point(180, 320), Width = 80 };
            btnOK.Click += BtnOK_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // 将控件添加到窗体
            this.Controls.AddRange(new Control[] { grpTunnelType, grpDimension, grpCoordinate, btnOK, btnCancel });
        }

        // 确定按钮点击事件：验证输入有效性，赋值属性，关闭窗体返回OK
        private void BtnOK_Click(object sender, EventArgs e)
        {
            // 判断哪个单选按钮被选中，设置巷道类型字符串
            if (rbtnTypeSingle.Checked)
                TunnelType = "三星形";
            else if (rbtnTypeDouble.Checked)
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
            // 坐标X输入验证
            if (!double.TryParse(txtX.Text, out double x))
            {
                MessageBox.Show("坐标X请输入有效数字。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtX.Focus();
                return;
            }
            // 坐标Y输入验证
            if (!double.TryParse(txtY.Text, out double y))
            {
                MessageBox.Show("坐标Y请输入有效数字。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtY.Focus();
                return;
            }
            // 坐标Z输入验证
            if (!double.TryParse(txtZ.Text, out double z))
            {
                MessageBox.Show("坐标Z请输入有效数字。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtZ.Focus();
                return;
            }

            // 验证通过后，赋值给公开属性供外部访问
            WidthValue = width;
            HeightValue = height;
            CoordX = x;
            CoordY = y;
            CoordZ = z;

            // 关闭窗体，返回OK结果
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    // ZwCAD 插件主类，实现 IExtensionApplication 接口以完成初始化和清理工作
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
