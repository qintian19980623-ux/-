using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisionMotionProtocolLib;

namespace 通讯协议测试
{
    /// <summary>
    /// 协议Tab创建器 - 负责创建T1-T8协议测试Tab页面
    /// </summary>
    public class ProtocolTabCreator
    {
        private readonly ProtocolService protocolService;
        private readonly ConfigService configService;
        private readonly Action<string, bool> logCallback;
        private readonly Action<string, TextBox> showResultCallback;

        public ProtocolTabCreator(
            ProtocolService protocolService,
            ConfigService configService,
            Action<string, bool> logCallback,
            Action<string, TextBox> showResultCallback)
        {
            this.protocolService = protocolService;
            this.configService = configService;
            this.logCallback = logCallback;
            this.showResultCallback = showResultCallback;
        }

        /// <summary>
        /// 创建端口选择器
        /// </summary>
        private ComboBox CreatePortSelector(string tabName)
        {
            var config = configService.GetConfig();
            var cmb = UIHelper.CreatePortSelector(config.Ports, 3);

            // 当端口选择改变时保存配置
            cmb.SelectedIndexChanged += (s, e) =>
            {
                if (cmb.SelectedItem != null)
                {
                    int port = int.Parse(cmb.SelectedItem.ToString() ?? "7950");
                    configService.UpdateTabPort(tabName, port);
                    configService.SaveConfig(configService.GetConfig());
                }
            };

            return cmb;
        }

        /// <summary>
        /// 获取当前选中端口的协议实例
        /// </summary>
        private VisionMotionProtocol GetActiveProtocol(ComboBox cmbPort)
        {
            if (cmbPort.SelectedItem == null)
            {
                MessageBox.Show("请先选择端口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            int port = int.Parse(cmbPort.SelectedItem.ToString() ?? "0");
            var protocol = protocolService.GetProtocol(port);

            if (protocol == null)
            {
                MessageBox.Show($"端口 {port} 未连接，请先连接该端口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return protocol;
        }

        /// <summary>
        /// 执行协议命令的通用方法
        /// </summary>
        private async Task ExecuteProtocolCommand(
            ComboBox cmbPort,
            Button button,
            string commandName,
            Func<VisionMotionProtocol, Task<string>> executeAction,
            TextBox txtResult)
        {
            var protocol = GetActiveProtocol(cmbPort);
            if (protocol == null) return;

            string originalText = button.Text;
            try
            {
                button.Enabled = false;
                button.Text = $"{commandName}中...";

                string result = await executeAction(protocol);
                showResultCallback?.Invoke(result, txtResult);
            }
            catch (Exception ex)
            {
                showResultCallback?.Invoke($"{commandName}异常: {ex.Message}", txtResult);
            }
            finally
            {
                button.Enabled = true;
                button.Text = originalText;
            }
        }

        /// <summary>
        /// 创建T1 Tab
        /// </summary>
        public TabPage CreateT1Tab()
        {
            var tab = new TabPage("T1-上下相机映射") { BackColor = UIConfig.Colors.BgSecondary };
            var cmbPort = CreatePortSelector("T1");

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingTop,
                                     UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingSide),
                BackColor = UIConfig.Colors.BgSecondary
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, UIConfig.Column.LabelWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UIConfig.Row.HeightNormal));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UIConfig.Row.HeightButton));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UIConfig.Row.HeightSpacing));

            var lblPosition = UIHelper.CreateStyledLabel("当前拍照位 (X,Y,R):");
            var txtPosition = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "100.5,200.3,45.0");
            txtPosition.Width = UIConfig.Column.InputBoxWidth;

            var btnSend = UIHelper.CreateStyledButton("发送 T1 命令", UIConfig.Button.Width, UIConfig.Button.Height, isPrimary: true);
            var txtResult = UIHelper.CreateStyledTextBox(multiline: true, readOnly: true);

            btnSend.Click += async (s, e) =>
            {
                await ExecuteProtocolCommand(cmbPort, btnSend, "发送", async (protocol) =>
                {
                    await protocol.SendT1Async(txtPosition.Text);
                    var response = await protocol.ReceiveT1Async(10000);
                    return $"T1结果 - {(response.Success ? "成功" : "失败")};{response.ResultCode};{response.X},{response.Y},{response.R}";
                }, txtResult);
            };

            layout.Controls.Add(lblPosition, 0, 0);
            layout.Controls.Add(txtPosition, 1, 0);
            layout.Controls.Add(btnSend, 0, 1);
            layout.SetColumnSpan(btnSend, 2);
            layout.Controls.Add(txtResult, 0, 2);
            layout.SetColumnSpan(txtResult, 2);

            tab.Controls.Add(UIHelper.CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            return tab;
        }

        // T2-T8的创建方法将在后续添加...
        // 这里先创建占位符方法

        /// <summary>
        /// 创建T2 Tab - 九点标定
        /// </summary>
        public TabPage CreateT2Tab()
        {
            var tab = new TabPage("T2-九点标定") { BackColor = UIConfig.Colors.BgSecondary };
            var cmbPort = CreatePortSelector("T2");

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 10,
                Padding = new Padding(UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingTop,
                                     UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingSide),
                AutoScroll = true,
                BackColor = UIConfig.Colors.BgSecondary
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UIConfig.Row.HeightLarge));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UIConfig.Row.HeightLarge));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UIConfig.Row.HeightSmall));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UIConfig.Row.HeightLarge));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UIConfig.Row.HeightButton));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));

            var lblCamera = new Label { Text = "相机:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold), ForeColor = UIConfig.Colors.TextPrimary, BackColor = Color.Transparent };
            var cmbCamera = UIHelper.CreateCameraCombo(1);
            cmbCamera.Dock = DockStyle.Top;

            var lblModule = new Label { Text = "模组:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold), ForeColor = UIConfig.Colors.TextPrimary, BackColor = Color.Transparent };
            var cmbModule = UIHelper.CreateModuleCombo(1);
            cmbModule.Dock = DockStyle.Top;

            var lblGripper = new Label { Text = "吸嘴/夹爪:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold), ForeColor = UIConfig.Colors.TextPrimary, BackColor = Color.Transparent };
            var cmbGripper = UIHelper.CreateNozzleCombo(1);
            cmbGripper.Dock = DockStyle.Top;

            var lblStep = new Label { Text = "移动步长(mm):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold), ForeColor = UIConfig.Colors.TextPrimary, BackColor = Color.Transparent };
            var txtStep = new TextBox { Text = "10.0", Dock = DockStyle.Top, Font = new Font("微软雅黑", UIConfig.Font.Large), BackColor = UIConfig.Colors.BgSecondary, ForeColor = UIConfig.Colors.TextPrimary, BorderStyle = BorderStyle.FixedSingle };

            var lblCenter = new Label { Text = "中心点(X,Y,R):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold), ForeColor = UIConfig.Colors.TextPrimary, BackColor = Color.Transparent };
            var txtCenter = new TextBox { Text = "100.0,200.0,0.0", Dock = DockStyle.Top, Font = new Font("微软雅黑", UIConfig.Font.Large), BackColor = UIConfig.Colors.BgSecondary, ForeColor = UIConfig.Colors.TextPrimary, BorderStyle = BorderStyle.FixedSingle };

            var lblXDir = new Label { Text = "X方向:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold), ForeColor = UIConfig.Colors.TextPrimary, BackColor = Color.Transparent };
            var chkXDir = new CheckBox { Text = "反向", Dock = DockStyle.Fill, Font = new Font("微软雅黑", UIConfig.Font.Normal), Checked = false, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UIConfig.Colors.TextPrimary, BackColor = Color.Transparent };

            var lblYDir = new Label { Text = "Y方向:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold), ForeColor = UIConfig.Colors.TextPrimary, BackColor = Color.Transparent };
            var chkYDir = new CheckBox { Text = "反向", Dock = DockStyle.Fill, Font = new Font("微软雅黑", UIConfig.Font.Normal), Checked = false, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UIConfig.Colors.TextPrimary, BackColor = Color.Transparent };

            var lblRotateAngle = new Label { Text = "旋转角度(度):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold), ForeColor = UIConfig.Colors.TextPrimary, BackColor = Color.Transparent };
            var txtRotateAngle = new TextBox { Text = "45.0", Dock = DockStyle.Top, Font = new Font("微软雅黑", UIConfig.Font.Large), BackColor = UIConfig.Colors.BgSecondary, ForeColor = UIConfig.Colors.TextPrimary, BorderStyle = BorderStyle.FixedSingle };

            Panel btnPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var btnClear = new Button { Text = "1-清除标定点", Location = new Point(0, 5), Size = new Size(140, UIConfig.Button.Height), Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold), Cursor = Cursors.Hand, BackColor = UIConfig.Colors.BgPanel, ForeColor = UIConfig.Colors.TextPrimary, FlatStyle = FlatStyle.Flat };
            btnClear.FlatAppearance.BorderColor = UIConfig.Colors.ButtonPrimary;
            btnClear.FlatAppearance.BorderSize = 1;
            var btnCalib = new Button { Text = "2-九点标定", Location = new Point(150, 5), Size = new Size(140, UIConfig.Button.Height), Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold), Cursor = Cursors.Hand, BackColor = UIConfig.Colors.ButtonPrimary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCalib.FlatAppearance.BorderSize = 0;
            var btnCalibRotate = new Button { Text = "3-九点+旋转(16次)", Location = new Point(300, 5), Size = new Size(160, UIConfig.Button.Height), Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold), Cursor = Cursors.Hand, BackColor = UIConfig.Colors.ButtonPrimary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCalibRotate.FlatAppearance.BorderSize = 0;
            var btnEnd = new Button { Text = "4-结束标定", Location = new Point(470, 5), Size = new Size(140, UIConfig.Button.Height), Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold), Cursor = Cursors.Hand, BackColor = UIConfig.Colors.BgPanel, ForeColor = UIConfig.Colors.TextPrimary, FlatStyle = FlatStyle.Flat };
            btnEnd.FlatAppearance.BorderColor = UIConfig.Colors.ButtonPrimary;
            btnEnd.FlatAppearance.BorderSize = 1;
            btnPanel.Controls.AddRange(new Control[] { btnClear, btnCalib, btnCalibRotate, btnEnd });

            var txtResult = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Font = new Font("Consolas", UIConfig.Font.Console), BackColor = UIConfig.Colors.LogBg, ForeColor = UIConfig.Colors.LogText, BorderStyle = BorderStyle.FixedSingle };

            btnClear.Click += async (s, e) =>
            {
                var protocol = GetActiveProtocol(cmbPort);
                if (protocol == null) return;
                try
                {
                    btnClear.Enabled = false;
                    await protocol.SendT2Async(UIHelper.GetComboValue(cmbCamera), UIHelper.GetComboValue(cmbModule), UIHelper.GetComboValue(cmbGripper), 1, 1, double.Parse(txtStep.Text), txtCenter.Text);
                    var response = await protocol.ReceiveT2Async();
                    showResultCallback?.Invoke($"清除标定点 - {(response.Success ? "成功" : "失败")};{response.ResultCode}", txtResult);
                }
                catch (Exception ex) { showResultCallback?.Invoke($"清除标定点异常: {ex.Message}", txtResult); }
                finally { btnClear.Enabled = true; }
            };

            btnCalib.Click += async (s, e) =>
            {
                var protocol = GetActiveProtocol(cmbPort);
                if (protocol == null) return;
                try
                {
                    btnCalib.Enabled = false;
                    btnCalib.Text = "标定中...";

                    var centerParts = txtCenter.Text.Split(',');
                    double centerX = double.Parse(centerParts[0]);
                    double centerY = double.Parse(centerParts[1]);
                    double centerR = double.Parse(centerParts[2]);
                    double step = double.Parse(txtStep.Text);

                    int xDir = chkXDir.Checked ? -1 : 1;
                    int yDir = chkYDir.Checked ? -1 : 1;

                    var points = new (double x, double y)[]
                    {
                        (centerX + xDir * (-step), centerY + yDir * step),
                        (centerX, centerY + yDir * step),
                        (centerX + xDir * step, centerY + yDir * step),
                        (centerX + xDir * step, centerY),
                        (centerX, centerY),
                        (centerX + xDir * (-step), centerY),
                        (centerX + xDir * (-step), centerY + yDir * (-step)),
                        (centerX, centerY + yDir * (-step)),
                        (centerX + xDir * step, centerY + yDir * (-step))
                    };

                    for (int i = 0; i < 9; i++)
                    {
                        string currentPos = $"{points[i].x},{points[i].y},{centerR}";
                        await protocol.SendT2Async(UIHelper.GetComboValue(cmbCamera), UIHelper.GetComboValue(cmbModule), UIHelper.GetComboValue(cmbGripper), i + 1, 2, step, currentPos);
                        var response = await protocol.ReceiveT2Async();
                        showResultCallback?.Invoke($"标定第{i + 1}点 ({points[i].x:F1},{points[i].y:F1}) - {(response.Success ? "成功" : "失败")};{response.ResultCode}", txtResult);
                        await Task.Delay(500);
                    }
                    showResultCallback?.Invoke("九点标定完成！", txtResult);
                }
                catch (Exception ex) { showResultCallback?.Invoke($"执行标定异常: {ex.Message}", txtResult); }
                finally { btnCalib.Enabled = true; btnCalib.Text = "2-九点标定"; }
            };

            btnCalibRotate.Click += async (s, e) =>
            {
                var protocol = GetActiveProtocol(cmbPort);
                if (protocol == null) return;
                int cameraNo = UIHelper.GetComboValue(cmbCamera);
                if (cameraNo != 2 && cameraNo != 4)
                {
                    MessageBox.Show("九点+旋转标定仅适用于下相机（相机2：管壳下相机 或 相机4：锗窗下相机）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                try
                {
                    btnCalibRotate.Enabled = false;
                    btnCalibRotate.Text = "标定中...";

                    var centerParts = txtCenter.Text.Split(',');
                    double centerX = double.Parse(centerParts[0]);
                    double centerY = double.Parse(centerParts[1]);
                    double centerR = double.Parse(centerParts[2]);
                    double step = double.Parse(txtStep.Text);
                    double rotateAngle = double.Parse(txtRotateAngle.Text);

                    int xDir = chkXDir.Checked ? -1 : 1;
                    int yDir = chkYDir.Checked ? -1 : 1;

                    showResultCallback?.Invoke("========== 开始九点标定 ==========", txtResult);
                    var points = new (double x, double y)[]
                    {
                        (centerX + xDir * (-step), centerY + yDir * step),
                        (centerX, centerY + yDir * step),
                        (centerX + xDir * step, centerY + yDir * step),
                        (centerX + xDir * step, centerY),
                        (centerX, centerY),
                        (centerX + xDir * (-step), centerY),
                        (centerX + xDir * (-step), centerY + yDir * (-step)),
                        (centerX, centerY + yDir * (-step)),
                        (centerX + xDir * step, centerY + yDir * (-step))
                    };

                    for (int i = 0; i < 9; i++)
                    {
                        string currentPos = $"{points[i].x},{points[i].y},{centerR}";
                        await protocol.SendT2Async(UIHelper.GetComboValue(cmbCamera), UIHelper.GetComboValue(cmbModule), UIHelper.GetComboValue(cmbGripper), i + 1, 2, step, currentPos);
                        var response = await protocol.ReceiveT2Async();
                        showResultCallback?.Invoke($"九点第{i + 1}点 ({points[i].x:F1},{points[i].y:F1}) - {(response.Success ? "成功" : "失败")};{response.ResultCode}", txtResult);
                        await Task.Delay(500);
                    }
                    showResultCallback?.Invoke("九点标定完成！", txtResult);

                    showResultCallback?.Invoke("\n========== 开始旋转标定 ==========", txtResult);
                    for (int i = 1; i <= 7; i++)
                    {
                        double currentR = centerR + ((i - 1) * rotateAngle);
                        string currentPos = $"{centerX},{centerY},{currentR}";
                        await protocol.SendT2Async(UIHelper.GetComboValue(cmbCamera), UIHelper.GetComboValue(cmbModule), UIHelper.GetComboValue(cmbGripper), 9 + i, 2, step, currentPos);
                        var response = await protocol.ReceiveT2Async();
                        showResultCallback?.Invoke($"旋转标定第{i}次 (R={currentR:F1}度) - {(response.Success ? "成功" : "失败")};{response.ResultCode}", txtResult);
                        await Task.Delay(500);
                    }
                    showResultCallback?.Invoke("旋转标定完成！\n========== 总计完成16次标定 ==========", txtResult);
                }
                catch (Exception ex) { showResultCallback?.Invoke($"九点+旋转标定异常: {ex.Message}", txtResult); }
                finally { btnCalibRotate.Enabled = true; btnCalibRotate.Text = "3-九点+旋转(16次)"; }
            };

            btnEnd.Click += async (s, e) =>
            {
                var protocol = GetActiveProtocol(cmbPort);
                if (protocol == null) return;
                try
                {
                    btnEnd.Enabled = false;
                    await protocol.SendT2Async(UIHelper.GetComboValue(cmbCamera), UIHelper.GetComboValue(cmbModule), UIHelper.GetComboValue(cmbGripper), 9, 3, double.Parse(txtStep.Text), txtCenter.Text);
                    var response = await protocol.ReceiveT2Async();
                    showResultCallback?.Invoke($"结束标定 - {(response.Success ? "成功" : "失败")};{response.ResultCode}", txtResult);
                }
                catch (Exception ex) { showResultCallback?.Invoke($"结束标定异常: {ex.Message}", txtResult); }
                finally { btnEnd.Enabled = true; }
            };

            layout.Controls.Add(lblCamera, 0, 0);
            layout.Controls.Add(cmbCamera, 1, 0);
            layout.Controls.Add(lblModule, 2, 0);
            layout.Controls.Add(cmbModule, 3, 0);

            layout.Controls.Add(lblGripper, 0, 1);
            layout.Controls.Add(cmbGripper, 1, 1);
            layout.Controls.Add(lblStep, 2, 1);
            layout.Controls.Add(txtStep, 3, 1);

            layout.Controls.Add(lblCenter, 0, 2);
            layout.Controls.Add(txtCenter, 1, 2);
            layout.Controls.Add(lblXDir, 2, 2);
            layout.Controls.Add(chkXDir, 3, 2);

            layout.Controls.Add(lblYDir, 0, 3);
            layout.Controls.Add(chkYDir, 1, 3);
            layout.Controls.Add(lblRotateAngle, 2, 3);
            layout.Controls.Add(txtRotateAngle, 3, 3);

            layout.Controls.Add(btnPanel, 0, 4);
            layout.SetColumnSpan(btnPanel, 4);

            layout.Controls.Add(txtResult, 0, 5);
            layout.SetColumnSpan(txtResult, 4);
            layout.SetRowSpan(txtResult, 5);

            tab.Controls.Add(UIHelper.CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            return tab;
        }

        /// <summary>
        /// 创建T3 Tab - 吸嘴夹爪标定
        /// </summary>
        public TabPage CreateT3Tab()
        {
            var tab = new TabPage("T3-吸嘴夹爪标定") { BackColor = UIConfig.Colors.BgSecondary };
            var cmbPort = CreatePortSelector("T3");

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingTop,
                                     UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingSide),
                BackColor = UIConfig.Colors.BgSecondary
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblCamera = UIHelper.CreateStyledLabel("相机:");
            var cmbCamera = UIHelper.CreateCameraCombo(2);
            cmbCamera.Dock = DockStyle.Top;

            var lblGripper = UIHelper.CreateStyledLabel("吸嘴/夹爪:");
            var cmbGripper = UIHelper.CreateNozzleCombo(1);
            cmbGripper.Dock = DockStyle.Top;

            var lblPosition1 = UIHelper.CreateStyledLabel("拍照位1(X,Y,R):");
            var txtPosition1 = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "150.0,300.0,0.0");
            txtPosition1.Width = 300;

            var lblPosition2 = UIHelper.CreateStyledLabel("拍照位2(X,Y,R):");
            var txtPosition2 = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "150.0,250.0,90.0");
            txtPosition2.Width = 300;

            var btnSend = UIHelper.CreateStyledButton("发送 T3 命令", 150, 35, isPrimary: true);
            var txtResult = UIHelper.CreateStyledTextBox(multiline: true, readOnly: true);

            btnSend.Click += async (s, e) =>
            {
                var protocol = GetActiveProtocol(cmbPort);
                if (protocol == null) return;

                string originalText = btnSend.Text;
                try
                {
                    btnSend.Enabled = false;
                    btnSend.Text = "发送中...";

                    await protocol.SendT3Async(UIHelper.GetComboValue(cmbCamera), UIHelper.GetComboValue(cmbGripper), txtPosition1.Text, txtPosition2.Text);
                    var response = await protocol.ReceiveT3Async();
                    showResultCallback?.Invoke($"T3结果 - {(response.Success ? "成功" : "失败")};{response.ResultCode};{response.X},{response.Y},{response.R}", txtResult);
                }
                catch (Exception ex)
                {
                    showResultCallback?.Invoke($"T3发送异常: {ex.Message}", txtResult);
                }
                finally
                {
                    btnSend.Enabled = true;
                    btnSend.Text = originalText;
                }
            };

            layout.Controls.Add(lblCamera, 0, 0);
            layout.Controls.Add(cmbCamera, 1, 0);
            layout.Controls.Add(lblGripper, 2, 0);
            layout.Controls.Add(cmbGripper, 3, 0);
            layout.Controls.Add(lblPosition1, 0, 1);
            layout.Controls.Add(txtPosition1, 1, 1);
            layout.Controls.Add(lblPosition2, 0, 2);
            layout.Controls.Add(txtPosition2, 1, 2);
            layout.Controls.Add(btnSend, 0, 4);
            layout.SetColumnSpan(btnSend, 4);
            layout.Controls.Add(txtResult, 0, 5);
            layout.SetColumnSpan(txtResult, 4);

            tab.Controls.Add(UIHelper.CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            return tab;
        }

        /// <summary>
        /// 创建T4 Tab - 管壳模组上下相机对齐
        /// </summary>
        public TabPage CreateT4Tab()
        {
            var tab = new TabPage("T4-管壳模组上下相机对齐") { BackColor = UIConfig.Colors.BgSecondary };
            var cmbPort = CreatePortSelector("T4");

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
                Padding = new Padding(UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingTop,
                                     UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingSide),
                BackColor = UIConfig.Colors.BgSecondary
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblCamera = UIHelper.CreateStyledLabel("相机:");
            var cmbCamera = UIHelper.CreateCameraCombo(1);
            cmbCamera.Dock = DockStyle.Top;

            var lblPosition = UIHelper.CreateStyledLabel("当前位置(X,Y,R):");
            var txtPosition = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "100.0,200.0,0.0");

            var lblNozzle = UIHelper.CreateStyledLabel("吸嘴取位(X,Y,R):");
            var txtNozzle = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "999,999,999");

            var lblHint = UIHelper.CreateStyledLabel("提示：上相机时填写 999,999,999", isBold: false);
            lblHint.Font = new Font("微软雅黑", 8F);
            lblHint.ForeColor = Color.Gray;

            var btnSend = UIHelper.CreateStyledButton("发送 T4 命令", 150, 35, isPrimary: true);
            var txtResult = UIHelper.CreateStyledTextBox(multiline: true, readOnly: true);

            btnSend.Click += async (s, e) =>
            {
                await ExecuteProtocolCommand(cmbPort, btnSend, "发送", async (protocol) =>
                {
                    await protocol.SendT4Async(UIHelper.GetComboValue(cmbCamera), txtPosition.Text, txtNozzle.Text);
                    var response = await protocol.ReceiveT4Async();
                    return $"T4结果 - {(response.Success ? "成功" : "失败")};{response.ResultCode};{response.X},{response.Y},{response.R}";
                }, txtResult);
            };

            layout.Controls.Add(lblCamera, 0, 0);
            layout.Controls.Add(cmbCamera, 1, 0);
            layout.Controls.Add(lblPosition, 2, 0);
            layout.Controls.Add(txtPosition, 3, 0);
            layout.Controls.Add(lblNozzle, 0, 1);
            layout.Controls.Add(txtNozzle, 1, 1);
            layout.Controls.Add(lblHint, 2, 1);
            layout.SetColumnSpan(lblHint, 2);
            layout.Controls.Add(btnSend, 0, 3);
            layout.SetColumnSpan(btnSend, 4);
            layout.Controls.Add(txtResult, 0, 4);
            layout.SetColumnSpan(txtResult, 4);

            tab.Controls.Add(UIHelper.CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            return tab;
        }

        /// <summary>
        /// 创建T5 Tab - 管壳检测模组自动引导
        /// </summary>
        public TabPage CreateT5Tab()
        {
            var tab = new TabPage("T5-管壳检测模组自动引导") { BackColor = UIConfig.Colors.BgSecondary };
            var cmbPort = CreatePortSelector("T5");

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingTop,
                                     UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingSide),
                BackColor = UIConfig.Colors.BgSecondary
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblCamera = UIHelper.CreateStyledLabel("相机:");
            var cmbCamera = UIHelper.CreateCameraCombo(1);
            cmbCamera.Dock = DockStyle.Top;

            var lblNozzle = UIHelper.CreateStyledLabel("吸嘴号:");
            var cmbNozzle = UIHelper.CreateNozzleCombo(1);
            cmbNozzle.Dock = DockStyle.Top;

            var lblFlag = UIHelper.CreateStyledLabel("取放料:");
            var cmbFlag = UIHelper.CreateStyledComboBox(new string[] { "1 - 取料", "2 - 放料" }, 0);

            var lblPos1 = UIHelper.CreateStyledLabel("拍照位1(X,Y,R):");
            var txtPos1 = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "100.0,200.0,0.0");

            var lblPos2 = UIHelper.CreateStyledLabel("拍照位2(X,Y,R):");
            var txtPos2 = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "999,999,999");

            var btnSend = UIHelper.CreateStyledButton("发送 T5 命令", UIConfig.Button.Width, UIConfig.Button.Height, isPrimary: true);
            var txtResult = UIHelper.CreateStyledTextBox(multiline: true, readOnly: true);

            btnSend.Click += async (s, e) =>
            {
                await ExecuteProtocolCommand(cmbPort, btnSend, "发送", async (protocol) =>
                {
                    await protocol.SendT5Async(UIHelper.GetComboValue(cmbCamera), UIHelper.GetComboValue(cmbNozzle), cmbFlag.SelectedIndex + 1, txtPos1.Text, txtPos2.Text);
                    var response = await protocol.ReceiveT5Async();
                    return $"T5结果 - {(response.Success ? "成功" : "失败")};{response.ResultCode};{response.X},{response.Y},{response.R}";
                }, txtResult);
            };

            layout.Controls.Add(lblCamera, 0, 0);
            layout.Controls.Add(cmbCamera, 1, 0);
            layout.Controls.Add(lblNozzle, 2, 0);
            layout.Controls.Add(cmbNozzle, 3, 0);
            layout.Controls.Add(lblFlag, 0, 1);
            layout.Controls.Add(cmbFlag, 1, 1);
            layout.Controls.Add(lblPos1, 0, 2);
            layout.Controls.Add(txtPos1, 1, 2);
            layout.Controls.Add(lblPos2, 2, 2);
            layout.Controls.Add(txtPos2, 3, 2);
            layout.Controls.Add(btnSend, 0, 4);
            layout.SetColumnSpan(btnSend, 4);
            layout.Controls.Add(txtResult, 0, 5);
            layout.SetColumnSpan(txtResult, 4);

            tab.Controls.Add(UIHelper.CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            return tab;
        }

        /// <summary>
        /// 创建T6 Tab - 锗窗检测模组自动引导
        /// </summary>
        public TabPage CreateT6Tab()
        {
            var tab = new TabPage("T6-锗窗检测模组自动引导") { BackColor = UIConfig.Colors.BgSecondary };
            var cmbPort = CreatePortSelector("T6");

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingTop,
                                     UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingSide),
                BackColor = UIConfig.Colors.BgSecondary
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblCamera = UIHelper.CreateStyledLabel("相机:");
            var cmbCamera = UIHelper.CreateCameraCombo(3);
            cmbCamera.Dock = DockStyle.Top;

            var lblNozzle = UIHelper.CreateStyledLabel("吸嘴号:");
            var cmbNozzle = UIHelper.CreateNozzleCombo(1);
            cmbNozzle.Dock = DockStyle.Top;

            var lblFlag = UIHelper.CreateStyledLabel("取放料:");
            var cmbFlag = UIHelper.CreateStyledComboBox(new string[] { "1 - 取料", "2 - 放料" }, 0);

            var lblPos1 = UIHelper.CreateStyledLabel("拍照位1(X,Y,R):");
            var txtPos1 = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "200.0,300.0,0.0");

            var lblPos2 = UIHelper.CreateStyledLabel("拍照位2(X,Y,R):");
            var txtPos2 = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "999,999,999");

            var btnSend = UIHelper.CreateStyledButton("发送 T6 命令", 150, 35, isPrimary: true);
            var txtResult = UIHelper.CreateStyledTextBox(multiline: true, readOnly: true);

            btnSend.Click += async (s, e) =>
            {
                await ExecuteProtocolCommand(cmbPort, btnSend, "发送", async (protocol) =>
                {
                    await protocol.SendT6Async(UIHelper.GetComboValue(cmbCamera), UIHelper.GetComboValue(cmbNozzle), cmbFlag.SelectedIndex + 1, txtPos1.Text, txtPos2.Text);
                    var response = await protocol.ReceiveT6Async();
                    return $"T6结果 - {(response.Success ? "成功" : "失败")};{response.ResultCode};{response.X},{response.Y},{response.R}";
                }, txtResult);
            };

            layout.Controls.Add(lblCamera, 0, 0);
            layout.Controls.Add(cmbCamera, 1, 0);
            layout.Controls.Add(lblNozzle, 2, 0);
            layout.Controls.Add(cmbNozzle, 3, 0);
            layout.Controls.Add(lblFlag, 0, 1);
            layout.Controls.Add(cmbFlag, 1, 1);
            layout.Controls.Add(lblPos1, 0, 2);
            layout.Controls.Add(txtPos1, 1, 2);
            layout.Controls.Add(lblPos2, 2, 2);
            layout.Controls.Add(txtPos2, 3, 2);
            layout.Controls.Add(btnSend, 0, 4);
            layout.SetColumnSpan(btnSend, 4);
            layout.Controls.Add(txtResult, 0, 5);
            layout.SetColumnSpan(txtResult, 4);

            tab.Controls.Add(UIHelper.CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            return tab;
        }

        /// <summary>
        /// 创建T7 Tab - 出料载具
        /// </summary>
        public TabPage CreateT7Tab()
        {
            var tab = new TabPage("T7-出料载具") { BackColor = UIConfig.Colors.BgSecondary };
            var cmbPort = CreatePortSelector("T7");

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 7,
                Padding = new Padding(UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingTop,
                                     UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingSide),
                BackColor = UIConfig.Colors.BgSecondary
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblCarrier = UIHelper.CreateStyledLabel("载具号:");
            var txtCarrier = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "1");
            txtCarrier.Width = 100;

            var lblFunc = UIHelper.CreateStyledLabel("功能:");
            var cmbFunc = UIHelper.CreateStyledComboBox(new string[] { "1 - 定位", "2 - 示教" }, 0);

            var lblPos1 = UIHelper.CreateStyledLabel("拍照位1(X,Y,R):");
            var txtPos1 = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "300.0,400.0,0.0");

            var lblPos2 = UIHelper.CreateStyledLabel("拍照位2(X,Y,R):");
            var txtPos2 = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "305.0,405.0,0.0");

            var lblPick = UIHelper.CreateStyledLabel("取料位(X,Y,R):");
            var txtPick = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "999,999,999");

            var lblHint = UIHelper.CreateStyledLabel("提示：定位功能时填写 999,999,999", isBold: false);
            lblHint.Font = new Font("微软雅黑", 8F);
            lblHint.ForeColor = UIConfig.Colors.TextSecondary;

            var btnSend = UIHelper.CreateStyledButton("发送 T7 命令", 150, 35, isPrimary: true);
            var txtResult = UIHelper.CreateStyledTextBox(multiline: true, readOnly: true);

            btnSend.Click += async (s, e) =>
            {
                var protocol = GetActiveProtocol(cmbPort);
                if (protocol == null) return;
                try
                {
                    btnSend.Enabled = false;
                    btnSend.Text = "发送中...";
                    await protocol.SendT7Async(int.Parse(txtCarrier.Text), cmbFunc.SelectedIndex + 1, txtPos1.Text, txtPos2.Text, txtPick.Text);
                    var response = await protocol.ReceiveT7Async();

                    string xCoords = response.XCoordinates != null && response.XCoordinates.Length > 0
                        ? string.Join(", ", response.XCoordinates)
                        : "无";
                    string yCoords = response.YCoordinates != null && response.YCoordinates.Length > 0
                        ? string.Join(", ", response.YCoordinates)
                        : "无";

                    var result = $"T7结果 - {(response.Success ? "成功" : "失败")};{response.ResultCode};";

                    if (response.XCoordinates != null && response.YCoordinates != null && response.XCoordinates.Length > 0)
                    {
                        result += $"\n{response.XCoordinates.Length}\n;";
                        for (int i = 0; i < response.XCoordinates.Length; i++)
                        {
                            result += $"{response.XCoordinates[i]},{response.YCoordinates[i]}\n;";
                        }
                        result += response.R + "\n";
                    }
                    showResultCallback?.Invoke(result, txtResult);
                }
                catch (Exception ex) { showResultCallback?.Invoke($"T7异常: {ex.Message}", txtResult); }
                finally { btnSend.Enabled = true; btnSend.Text = "发送 T7 命令"; }
            };

            layout.Controls.Add(lblCarrier, 0, 0);
            layout.Controls.Add(txtCarrier, 1, 0);
            layout.Controls.Add(lblFunc, 2, 0);
            layout.Controls.Add(cmbFunc, 3, 0);
            layout.Controls.Add(lblPos1, 0, 1);
            layout.Controls.Add(txtPos1, 1, 1);
            layout.Controls.Add(lblPos2, 0, 2);
            layout.Controls.Add(txtPos2, 1, 2);
            layout.Controls.Add(lblPick, 0, 3);
            layout.Controls.Add(txtPick, 1, 3);
            layout.Controls.Add(lblHint, 2, 3);
            layout.SetColumnSpan(lblHint, 2);
            layout.Controls.Add(btnSend, 0, 5);
            layout.SetColumnSpan(btnSend, 4);
            layout.Controls.Add(txtResult, 0, 6);
            layout.SetColumnSpan(txtResult, 4);

            tab.Controls.Add(UIHelper.CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            return tab;
        }

        /// <summary>
        /// 创建T8 Tab - 出料载具穴位对齐图心
        /// </summary>
        public TabPage CreateT8Tab()
        {
            var tab = new TabPage("T8-出料载具穴位对齐图心") { BackColor = UIConfig.Colors.BgSecondary };
            var cmbPort = CreatePortSelector("T8");

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingTop,
                                     UIConfig.Layout.TabPaddingSide, UIConfig.Layout.TabPaddingSide),
                BackColor = UIConfig.Colors.BgSecondary
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblCarrier = UIHelper.CreateStyledLabel("载具号:");
            var txtCarrier = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "4");
            txtCarrier.Width = 100;

            var lblPos1 = UIHelper.CreateStyledLabel("拍照位1(X,Y,R):");
            var txtPos1 = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "400.0,500.0,0.0");

            var lblPos2 = UIHelper.CreateStyledLabel("拍照位2(X,Y,R):");
            var txtPos2 = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "450.0,500.0,0.0");

            var lblPos3 = UIHelper.CreateStyledLabel("拍照位3(X,Y,R):");
            var txtPos3 = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "425.0,550.0,0.0");

            var btnSend = UIHelper.CreateStyledButton("发送 T8 命令", 150, 35, isPrimary: true);
            var txtResult = UIHelper.CreateStyledTextBox(multiline: true, readOnly: true);

            btnSend.Click += async (s, e) =>
            {
                await ExecuteProtocolCommand(cmbPort, btnSend, "发送", async (protocol) =>
                {
                    await protocol.SendT8Async(int.Parse(txtCarrier.Text), txtPos1.Text, txtPos2.Text, txtPos3.Text);
                    var response = await protocol.ReceiveT8Async();
                    return $"T8结果 - {(response.Success ? "成功" : "失败")};{response.ResultCode};" +
                        $"{response.X1},{response.Y1}\n;" +
                        $"{response.X2},{response.Y2}\n;" +
                        $"{response.X3},{response.Y3}";
                }, txtResult);
            };

            layout.Controls.Add(lblCarrier, 0, 0);
            layout.Controls.Add(txtCarrier, 1, 0);
            layout.Controls.Add(lblPos1, 0, 1);
            layout.Controls.Add(txtPos1, 1, 1);
            layout.Controls.Add(lblPos2, 0, 2);
            layout.Controls.Add(txtPos2, 1, 2);
            layout.Controls.Add(lblPos3, 2, 2);
            layout.Controls.Add(txtPos3, 3, 2);
            layout.Controls.Add(btnSend, 0, 4);
            layout.SetColumnSpan(btnSend, 4);
            layout.Controls.Add(txtResult, 0, 5);
            layout.SetColumnSpan(txtResult, 4);

            tab.Controls.Add(UIHelper.CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            return tab;
        }

        private TabPage CreatePlaceholderTab(string title)
        {
            var tab = new TabPage(title) { BackColor = UIConfig.Colors.BgSecondary };
            var label = new Label
            {
                Text = $"{title} - 待实现\n完整功能将从原文件迁移",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = UIConfig.Colors.TextSecondary
            };
            tab.Controls.Add(label);
            return tab;
        }
    }
}
