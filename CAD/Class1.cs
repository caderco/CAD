using System;
using System.Windows.Forms;
using System.Drawing;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Runtime;
using ZwSoft.ZwCAD.DatabaseServices;
using TunnelDrawer;
using CoalLayerDrawer;
using ZwSoft.ZwCAD.Geometry;
using System.Globalization;

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

        //将宽度和高度放入数组,新建路径变量
        public string Filepath { get; private set; }
        public double[] param { get; private set; }

        public double RadiusValue { get; private set; }
        public double LenValue { get; private set; }
        // UI控件声明
        private GroupBox grpTunnelType;
        private RadioButton rbtnTypeThree_star, rbtnTypeHalf_circle, rbtnTypeOther;

        private GroupBox grpDimension;
        private Label lblWidth, lblLen, lblRadius, lblHeight;
        private TextBox txtWidth, txtLen, txtRadius, txtHeight;

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
            this.MinimizeBox = false;

            //初始化param
            param = new double[4];// 禁止最小化

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
                Font = new System.Drawing.Font("Microsoft YaHei", 9.5f)
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
                        Filepath = selectedFilePath;
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
            grpDimension.Controls.Clear();

            // 初始化公共控件并赋值给成员变量
            lblWidth = new Label { Location = new Point(20, 25), AutoSize = true };
            txtWidth = new TextBox { Location = new Point(80, 22), Width = 80, ImeMode = ImeMode.Disable };
            lblLen = new Label { Location = new Point(20, 60), AutoSize = true };
            txtLen = new TextBox { Location = new Point(80, 57), Width = 80, ImeMode = ImeMode.Disable };
            lblRadius = new Label { Text = "半径:", Location = new Point(20, 95), AutoSize = true };
            txtRadius = new TextBox { Location = new Point(80, 92), Width = 80, ImeMode = ImeMode.Disable };

            grpDimension.Controls.AddRange(new Control[] { lblWidth, txtWidth, lblLen, txtLen, lblRadius, txtRadius });

            switch (tunnelType)
            {
                case "三星形":
                    lblWidth.Text = "宽度:";
                    lblLen.Text = "高度:";

                    //隐藏半径可视化
                    lblRadius.Visible = false;
                    txtRadius.Visible = false;
                    break;
                case "半圆形":
                    lblRadius.Text = "半径:";
                    lblWidth.Visible = false;
                    txtWidth.Visible = false;
                    lblLen.Visible = false;
                    txtLen.Visible = false;
                    break;
                case "梯形":
                    lblWidth.Text = "上底长:";
                    lblLen.Text = "下底长:";

                    //隐藏半径可视化
                    lblRadius.Visible = false;
                    txtRadius.Visible = false;
                    lblHeight = new Label { Text = "高度:", Location = new Point(20, 95), AutoSize = true };
                    txtHeight = new TextBox { Location = new Point(80, 92), Width = 80, ImeMode = ImeMode.Disable };
                    grpDimension.Controls.AddRange(new Control[] { lblHeight, txtHeight });
                    break;
            }
        }
        // 确定按钮点击事件：验证输入有效性，赋值属性，关闭窗体返回OK
        private void BtnOK_Click(object sender, EventArgs e)
        {
            // 设置巷道类型
            if (rbtnTypeThree_star.Checked)
                TunnelType = "三星形";
            else if (rbtnTypeHalf_circle.Checked)
                TunnelType = "半圆形";
            else
                TunnelType = "梯形";

            try
            {
                double.TryParse(txtRadius.Text, out double radius);
                // 验证半径（所有类型都需要）
                //if (!double.TryParse(txtRadius.Text, out double radius) || radius <= 0)
                //{
                //    MessageBox.Show("请输入有效的半径值（正数）。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    txtRadius.Focus();
                //    return;
                //}
                RadiusValue = radius;

                // 根据类型验证其他字段
                if (TunnelType != "半圆形")
                {
                    if (!double.TryParse(txtWidth.Text, out double width) || width <= 0)
                    {
                        MessageBox.Show("请输入有效的宽度值（正数）。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtWidth.Focus();
                        return;
                    }
                    if (!double.TryParse(txtLen.Text, out double len) || len <= 0)
                    {
                        MessageBox.Show("请输入有效的长度值（正数）。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtLen.Focus();
                        return;
                    }
                    WidthValue = width;
                    LenValue = len;

                    //将宽度和高度放入数组
                    param[0] = width;
                    param[1] = len;
                }

                // 梯形需要额外验证高度
                if (TunnelType == "梯形" && txtHeight != null)
                {

                    if (!double.TryParse(txtHeight.Text, out double height) || height <= 0)
                    {
                        MessageBox.Show("请输入有效的高度值（正数）。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtHeight.Focus();
                        return;
                    }
                    HeightValue = height;

                    //梯形高度填入
                    param[2] = height;
                }
                if (TunnelType == "半圆形")
                {
                    //半径填入
                    param[0] = RadiusValue;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show($"控件未正确初始化: {ex.Message}", "严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    //用于绘制煤层参数（走向、倾角、厚度、长宽）
    public class Coal_Bed_MakeForm : Form
    {
        public TunnelUtil tunnelUtil1;
        private void InitializeControls()
        {
            lblUcs = new Label { Text = "UCS原点", AutoSize = true };
            txtUcs = new TextBox();

            lblUcsX = new Label { Text = "UCS X方向", AutoSize = true };
            txtUcsX = new TextBox();

            lblUcsY = new Label { Text = "UCS Y方向", AutoSize = true };
            txtUcsY = new TextBox();

            lblWidth = new Label { Text = "宽度", AutoSize = true };
            txtWidth = new TextBox();

            lblLength = new Label { Text = "长度", AutoSize = true };
            txtLength = new TextBox();

            lblThickness = new Label { Text = "厚度", AutoSize = true };
            txtThickness = new TextBox();

            lblAngleX = new Label { Text = "X倾角", AutoSize = true };
            txtAngleX = new TextBox();

            lblAngleY = new Label { Text = "Y倾角", AutoSize = true };
            txtAngleY = new TextBox();

            lblTowards = new Label { Text = "走向", AutoSize = true };
            txtTowards = new TextBox();

            lblGridLength = new Label { Text = "网格长度", AutoSize = true };
            txtGridLength = new TextBox();

            lblGridWidth = new Label { Text = "网格宽度", AutoSize = true };
            txtGridWidth = new TextBox();
        }
        // 坐标系参数
        public Point3d UcsOrigin { get; private set; }    // UCS原点坐标
        public Vector3d UcsX { get; private set; }         // X方向单位向量
        public Vector3d UcsY { get; private set; }         // Y方向单位向量

        // 煤层几何参数
        public double Length { get; private set; }        // 长度（沿走向）
        public double Width { get; private set; }         // 宽度（垂直走向）
        public double Thickness { get; private set; }      // 厚度

        // 地质参数
        public double AngleX { get; private set; }        // X向倾角（度）
        public double AngleY { get; private set; }        // Y向倾角（度）
        public double Towards { get; private set; }        // 走向（方位角，度）

        // 网格参数
        public double GridLength { get; private set; }     // 网格单元长度
        public double GridWidth { get; private set; }      // 网格单元宽度

        private GroupBox grpCoalBed;

        //更改煤层输入参数  宽度 长度 厚度 X倾角 Y倾角 前向偏移 网格长度 网格宽度 Ucs坐标 UcsX向量 UcsY向量
        private Label lblWidth, lblLength, lblThickness, lblAngleX, lblAngleY, lblTowards, lblGridLength, lblGridWidth, lblUcs, lblUcsX, lblUcsY;
        private TextBox txtWidth, txtLength, txtThickness, txtAngleX, txtAngleY, txtTowards, txtGridLength, txtGridWidth, txtUcs, txtUcsX, txtUcsY;
        private Button btnOK, btnCancel;

        public Coal_Bed_MakeForm()
        {
           
            this.Text = "绘制煤层参数输入";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(320, 450);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            InitializeControls();

            // 初始化参数组
            grpCoalBed = new GroupBox
            {
                Text = "煤层参数",
                Location = new Point(15, 10),
                Size = new Size(290, 380)
            };

            int yPos = 30; // 初始Y坐标
            int labelX = 20;
            int textBoxX = 150;
            int rowHeight = 30;

            // 第一行：UCS坐标
            lblUcs.Location = new Point(labelX, yPos);
            txtUcs.Location = new Point(textBoxX, yPos);

            txtUcs.Text = TunnelUtil.Origin;
            yPos += rowHeight;

            // 第二行：UCS X方向
            lblUcsX.Location = new Point(labelX, yPos);
            txtUcsX.Location = new Point(textBoxX, yPos);

            txtUcsX.Text = TunnelUtil.OriginX;

            yPos += rowHeight;

            // 第三行：UCS Y方向
            lblUcsY.Location = new Point(labelX, yPos);
            txtUcsY.Location = new Point(textBoxX, yPos);

            txtUcsY.Text = TunnelUtil.OriginY;

            yPos += rowHeight;

            // 第四行：前向偏移
            lblTowards.Location = new Point(labelX, yPos);
            txtTowards.Location = new Point(textBoxX, yPos);
            yPos += rowHeight;

            // 第五行：长度
            lblLength.Location = new Point(labelX, yPos);
            txtLength.Location = new Point(textBoxX, yPos);
            yPos += rowHeight;

            // 第六行：宽度
            lblWidth.Location = new Point(labelX, yPos);
            txtWidth.Location = new Point(textBoxX, yPos);
            yPos += rowHeight;


            // 第七行：厚度
            lblThickness.Location = new Point(labelX, yPos);
            txtThickness.Location = new Point(textBoxX, yPos);
            yPos += rowHeight;


            // 第八行：X方向倾角
            lblAngleX.Location = new Point(labelX, yPos);
            txtAngleX.Location = new Point(textBoxX, yPos);
            yPos += rowHeight;


            // 第九行：Y方向倾角
            lblAngleY.Location = new Point(labelX, yPos);
            txtAngleY.Location = new Point(textBoxX, yPos);
            yPos += rowHeight;

            // 第十行：网格长度
            lblGridLength.Location = new Point(labelX, yPos);
            txtGridLength.Location = new Point(textBoxX, yPos);
            yPos += rowHeight;

            // 第十一行：网格宽度
            lblGridWidth.Location = new Point(labelX, yPos);
            txtGridWidth.Location = new Point(textBoxX, yPos);

            // 调整GroupBox高度以容纳所有控件
            grpCoalBed.Size = new Size(290, yPos + 50);

            // 修正控件添加列表（移除重复项）
            grpCoalBed.Controls.AddRange(new Control[] {
            lblUcs, txtUcs,
            lblUcsX, txtUcsX,
            lblUcsY, txtUcsY,
            lblWidth, txtWidth,
            lblLength, txtLength,
            lblThickness, txtThickness,
            lblAngleX, txtAngleX,
            lblAngleY, txtAngleY,
            lblTowards, txtTowards,
            lblGridLength, txtGridLength,
            lblGridWidth, txtGridWidth
            });

            // 确定和取消按钮
            btnOK = new Button { Text = "确定", Location = new Point(80, 400), Width = 80 };
            btnCancel = new Button { Text = "取消", Location = new Point(180, 400), Width = 80 };

            btnOK.Click += BtnOK_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // 添加所有控件到窗体
            this.Controls.AddRange(new Control[] { grpCoalBed, btnOK, btnCancel });
        }


        private bool IsZeroVector(Vector3d vec, double tolerance = 1e-9)
        {
            return Math.Abs(vec.X) < tolerance &&
                   Math.Abs(vec.Y) < tolerance &&
                   Math.Abs(vec.Z) < tolerance;
        }

        // 新增 Vector3d 解析方法
        private bool TryParseVector3d(string input, out Vector3d vector)
        {
            // 初始化 vector 为零向量，确保所有代码路径都有赋值
            vector = new Vector3d(0, 0, 0);

            if (string.IsNullOrWhiteSpace(input))
                return false;

            // 分割输入字符串
            char[] separators = { ',', ';', ' ' };
            string[] parts = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            // 必须包含三个分量
            if (parts.Length != 3)
                return false;

            // 显式初始化变量
            double x = 0, y = 0, z = 0;

            // 解析 X 分量
            bool xParsed = double.TryParse(
                parts[0],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out x
            );

            // 解析 Y 分量
            bool yParsed = double.TryParse(
                parts[1],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out y
            );

            // 解析 Z 分量
            bool zParsed = double.TryParse(
                parts[2],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out z
            );

            // 所有分量必须解析成功
            if (!xParsed || !yParsed || !zParsed)
                return false;

            // 构造有效向量
            vector = new Vector3d(x, y, z);
            return true;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {


            try
            {


                // 验证并获取所有参数（保持原顺序）
                // UCS坐标验证（格式：X,Y,Z）
                if (string.IsNullOrEmpty(txtUcs.Text) ||
                    !TryParsePoint3d(txtUcs.Text, out Point3d ucsOrigin))
                {
                    MessageBox.Show("UCS坐标格式错误，请使用逗号分隔的X,Y,Z格式", "错误",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUcs.Focus();
                    return;
                }

                // UCS X方向向量验证（格式：X,Y,Z）
                if (string.IsNullOrEmpty(txtUcsX.Text) ||
      !TryParseVector3d(txtUcsX.Text, out Vector3d ucsX) ||
      IsZeroVector(ucsX)) // 使用自定义零向量检查
                {
                    MessageBox.Show("X方向向量格式错误或为零向量",
                                   "输入错误",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Error);
                    txtUcsX.Focus();
                    return;
                }

                // UCS Y方向向量验证（格式：X,Y,Z）
                if (string.IsNullOrEmpty(txtUcsY.Text) ||
       !TryParseVector3d(txtUcsY.Text, out Vector3d ucsY) ||
       IsZeroVector(ucsY)) // 使用自定义零向量检查
                {
                    MessageBox.Show("X方向向量格式错误或为零向量",
                                   "输入错误",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Error);
                    txtUcsY.Focus();
                    return;
                }

                //// 验证X和Y方向向量正交性
                //double dotProduct = ucsX.GetNormal().DotProduct(ucsY.GetNormal());
                //if (Math.Abs(dotProduct) > 1e-3)
                //{
                //    MessageBox.Show("X/Y方向向量必须正交", "错误",
                //                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    txtUcsX.Focus();
                //    return;
                //}

                // 以下保持原有验证顺序
                if (!double.TryParse(txtWidth.Text, out double width) || width <= 0)
                {
                    MessageBox.Show("请输入有效的宽度值（正数）。", "错误",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtWidth.Focus();
                    return;
                }

                if (!double.TryParse(txtLength.Text, out double length) || length <= 0)
                {
                    MessageBox.Show("请输入有效的长度值（正数）。", "错误",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLength.Focus();
                    return;
                }

                if (!double.TryParse(txtThickness.Text, out double thickness) || thickness <= 0)
                {
                    MessageBox.Show("请输入有效的厚度值（正数）。", "错误",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtThickness.Focus();
                    return;
                }

                // 允许负角度（-90~90）
                if (!double.TryParse(txtAngleX.Text, out double angleX) ||
                    angleX < -90 || angleX > 90)
                {
                    MessageBox.Show("X向倾角范围需在-90到90度之间", "错误",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtAngleX.Focus();
                    return;
                }

                if (!double.TryParse(txtAngleY.Text, out double angleY) ||
                    angleY < -90 || angleY > 90)
                {
                    MessageBox.Show("Y向倾角范围需在-90到90度之间", "错误",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtAngleY.Focus();
                    return;
                }

                // 允许负走向（表示反向）
                if (!double.TryParse(txtTowards.Text, out double towards))
                {
                    MessageBox.Show("请输入有效的走向值。", "错误",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtTowards.Focus();
                    return;
                }

                if (!double.TryParse(txtGridLength.Text, out double gridLength) || gridLength <= 0)
                {
                    MessageBox.Show("请输入有效的网格长度值（正数）。", "错误",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtGridLength.Focus();
                    return;
                }

                if (!double.TryParse(txtGridWidth.Text, out double gridWidth) || gridWidth <= 0)
                {
                    MessageBox.Show("请输入有效的网格宽度值（正数）。", "错误",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtGridWidth.Focus();
                    return;
                }
                // 保存参数
                UcsOrigin = ucsOrigin;
                UcsX = ucsX;
                UcsY = ucsY;

                Width = width;
                Length = length;
                Thickness = thickness;

                AngleX = angleX;
                AngleY = angleY;

                Towards = towards;
                GridLength = gridLength;
                GridWidth = gridWidth;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (ZwSoft.ZwCAD.Runtime.Exception ex)
            {
                MessageBox.Show($"参数解析错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        // 添加坐标解析辅助方法
        private bool TryParsePoint3d(string input, out Point3d result)
        {
            result = new Point3d();
            string[] parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) return false;

            if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) ||
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y) ||
                !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                return false;

            result = new Point3d(x, y, z);
            return true;
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

            //实例化TunelUtil类
            TunnelUtil tunnelUtil = new TunnelUtil();


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
                ed.WriteMessage($"\n坐标X: {form.RadiusValue}");
                ed.WriteMessage($"\n坐标Y: {form.LenValue}");
                ed.WriteMessage("\n--- 结束绘图参数输出 ---");

                //调用绘图函数
                tunnelUtil.ReadPointsFromTxt(form.Filepath, ed);
                //TunnelUtil.GenerateTunnelSection(form.TunnelType, form.param, ed);
                tunnelUtil.DrawTunnelByPath(form.TunnelType, form.param, form.Filepath);

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

            // 提示用户选择巷道口中心点
            //PromptPointOptions ppo = new PromptPointOptions("\n请选择巷道口中心点: ");
            //var res = ed.GetPoint(ppo);

            //if (res.Status != PromptStatus.OK)
            //{
            //    ed.WriteMessage("\n用户取消了操作。");
            //    return;
            //}

            // 显示煤层参数输入窗体
            Coal_Bed_MakeForm form = new Coal_Bed_MakeForm();
            form.TopMost = true;
            var dr = form.ShowDialog();

            if (dr == DialogResult.OK)
            {
                // 调用煤层绘制函数
              //  Point3d tunnelCenter = res.Value;
                double offset = 0; // 煤层与巷道口的偏移量，可添加UI让用户输入

                CoalLayerUtil.DrawCoalLayerByUserUcs(
                    form.UcsOrigin, form.UcsX, form.UcsY,
                    form.Towards,
                    form.Length, form.Width, form.Thickness,
                    form.AngleX, form.AngleY,
                    form.GridLength, form.GridWidth
                );


            }
            else
            {
                ed.WriteMessage("\n用户取消了煤层绘制操作。");
            }
        }
    }
}