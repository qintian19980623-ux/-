using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisionMotionProtocolLib;

namespace 通讯协议测试
{
    /// <summary>
    /// 视觉协议测试主窗体 - 优化版本
    /// 使用服务层架构，分离UI和业务逻辑
    /// </summary>
    public partial class VisionProtocolTestForm_v2 : Form
    {
        #region 服务层

        private ProtocolService protocolService;
        private AOIService aoiService;
        private ConfigService configService;

        #endregion

        #region UI控件

        private TextBox txtLog;
        private TabControl mainTabControl;
        private TabControl protocolTabControl;
        private Dictionary<int, Button> portButtons = new Dictionary<int, Button>();
        private Dictionary<string, ComboBox> tabPortSelectors = new Dictionary<string, ComboBox>();
        private Button btnAoiConnect; // AOI连接按钮
        private TextBox txtAoiServer; // AOI服务器地址输入框

        #endregion

        #region 构造函数和初始化

        public VisionProtocolTestForm_v2()
        {
            InitializeServices();
            InitializeComponent();
            LoadConfiguration();
        }

        /// <summary>
        /// 初始化服务层
        /// </summary>
        private void InitializeServices()
        {
            // 创建服务实例
            protocolService = new ProtocolService();
            aoiService = new AOIService();
            configService = new ConfigService();

            // 订阅服务事件
            protocolService.OnLog += LogMessage;
            protocolService.OnConnectionChanged += OnProtocolConnectionChanged;

            aoiService.OnLog += LogMessage;
            aoiService.OnConnectionChanged += OnAOIConnectionChanged;

            configService.OnLog += LogMessage;
        }

        /// <summary>
        /// 初始化UI组件
        /// </summary>
        private void InitializeComponent()
        {
            // 启用DPI自动缩放
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);

            // 窗体基本配置
            this.Text = "视觉运控通讯协议测试工具";
            this.Size = new Size(UIConfig.Form.Width, UIConfig.Form.Height);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(UIConfig.Form.MinWidth, UIConfig.Form.MinHeight);
            this.BackColor = UIConfig.Colors.BgMain;

            // 主布局容器
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(5)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, UIConfig.Layout.ConnectionPanelHeight));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, UIConfig.Layout.LogPanelHeight));

            // 1. 连接区域
            Panel pnlConnection = CreateConnectionPanel();
            mainLayout.Controls.Add(pnlConnection, 0, 0);

            // 2. 主TabControl区域
            mainTabControl = CreateMainTabControl();
            mainLayout.Controls.Add(mainTabControl, 0, 1);

            // 3. 日志区域
            Panel pnlLog = CreateLogPanel();
            mainLayout.Controls.Add(pnlLog, 0, 2);

            this.Controls.Add(mainLayout);

            // 启动时自动连接所有端口
            this.Load += async (s, e) => await AutoConnectAllPorts();
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfiguration()
        {
            var config = configService.LoadConfig();

            // 设置协议服务的服务器IP
            protocolService.SetServerIP(config.ServerIP);

            // 恢复各Tab的端口选择
            foreach (var kvp in tabPortSelectors)
            {
                int port = configService.GetTabPort(kvp.Key, 7950);
                var portStr = port.ToString();
                int index = kvp.Value.Items.IndexOf(portStr);
                if (index >= 0)
                {
                    kvp.Value.SelectedIndex = index;
                }
            }
        }

        #endregion

        #region 连接区域创建

        private Panel CreateConnectionPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = UIConfig.Colors.PanelBg,
                Padding = new Padding(10)
            };

            GroupBox grpConnection = new GroupBox
            {
                Text = "服务器连接 (127.0.0.1)",
                Location = new Point(10, 5),
                Size = new Size(UIConfig.Connection.GroupWidth, UIConfig.Connection.GroupHeight),
                Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold),
                ForeColor = UIConfig.Colors.ButtonSecondary,
                BackColor = UIConfig.Colors.PanelBg
            };

            Label lblPorts = new Label
            {
                Text = "端口:",
                Location = new Point(15, 28),
                Size = new Size(50, 25),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("微软雅黑", UIConfig.Font.Normal),
                ForeColor = UIConfig.Colors.TextPrimary,
                BackColor = Color.Transparent
            };

            // 获取配置的端口列表
            var config = configService.GetConfig();
            int startX = 70;
            for (int i = 0; i < config.Ports.Length; i++)
            {
                var btn = CreateQuickPortButton(config.Ports[i], startX + i * UIConfig.Connection.PortButtonSpacing, 26);
                portButtons[config.Ports[i]] = btn;
                grpConnection.Controls.Add(btn);
            }

            grpConnection.Controls.Add(lblPorts);

            Label lblStatus = new Label
            {
                Text = "未连接",
                Location = new Point(520, 30),
                Size = new Size(800, 25),
                ForeColor = UIConfig.Colors.TextSecondary,
                BackColor = Color.Transparent,
                Font = new Font("微软雅黑", UIConfig.Font.Normal),
                Name = "lblConnectionStatus"
            };

            panel.Controls.Add(grpConnection);
            panel.Controls.Add(lblStatus);
            return panel;
        }

        private Button CreateQuickPortButton(int port, int x, int y)
        {
            var btn = new Button
            {
                Text = $"{port}\n未连接",
                Location = new Point(x, y),
                Size = new Size(UIConfig.Connection.PortButtonWidth, UIConfig.Connection.PortButtonHeight),
                Font = new Font("微软雅黑", UIConfig.Font.Small),
                BackColor = UIConfig.Colors.ButtonDefault,
                ForeColor = UIConfig.Colors.TextSecondary,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = port,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderColor = UIConfig.Colors.ButtonPrimary;
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += QuickPortButton_Click;
            return btn;
        }

        private async void QuickPortButton_Click(object sender, EventArgs e)
        {
            if (sender is not Button btn) return;
            int port = (int)(btn.Tag ?? 0);

            if (protocolService.IsConnected(port))
            {
                await protocolService.DisconnectAsync(port);
            }
            else
            {
                await protocolService.ConnectAsync(port);
            }
        }

        #endregion

        #region 主TabControl创建

        private TabControl CreateMainTabControl()
        {
            mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", UIConfig.Font.Large, FontStyle.Bold),
                BackColor = UIConfig.Colors.BgSecondary,
                ForeColor = UIConfig.Colors.TextPrimary,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.FillToRight,
            };
            mainTabControl.DrawItem += TabControl_DrawItem;
            mainTabControl.Paint += TabControl_Paint;

            // 创建"定位交互"页
            TabPage positionTab = new TabPage("定位交互") { BackColor = UIConfig.Colors.BgSecondary };
            protocolTabControl = CreateProtocolTabControl();
            positionTab.Controls.Add(protocolTabControl);

            // 创建"AOI交互"页
            TabPage aoiTab = CreateAOITab();

            mainTabControl.TabPages.Add(positionTab);
            mainTabControl.TabPages.Add(aoiTab);

            return mainTabControl;
        }

        private TabControl CreateProtocolTabControl()
        {
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", UIConfig.Font.Large, FontStyle.Bold),
                BackColor = UIConfig.Colors.BgSecondary,
                ForeColor = UIConfig.Colors.TextPrimary,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.FillToRight,
            };
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.Paint += TabControl_Paint;

            // 添加T1-T8协议Tab页
            tabControl.TabPages.Add(CreateT1Tab());
            tabControl.TabPages.Add(CreateT2Tab());
            tabControl.TabPages.Add(CreateT3Tab());
            tabControl.TabPages.Add(CreateT4Tab());
            tabControl.TabPages.Add(CreateT5Tab());
            tabControl.TabPages.Add(CreateT6Tab());
            tabControl.TabPages.Add(CreateT7Tab());
            tabControl.TabPages.Add(CreateT8Tab());

            return tabControl;
        }

        #endregion

        #region 日志区域创建

        private Panel CreateLogPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = UIConfig.Colors.BgPanel,
                Padding = new Padding(5)
            };

            Label lblLog = new Label
            {
                Text = "通讯日志",
                Location = new Point(10, 8),
                AutoSize = true,
                Font = new Font("微软雅黑", UIConfig.Font.Large, FontStyle.Bold),
                ForeColor = UIConfig.Colors.ButtonSecondary,
                BackColor = Color.Transparent
            };

            Button btnClearLog = new Button
            {
                Text = "清除",
                Location = new Point(panel.Width - 95, 5),
                Size = new Size(80, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("微软雅黑", UIConfig.Font.Normal),
                BackColor = UIConfig.Colors.ButtonPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClearLog.FlatAppearance.BorderSize = 0;
            btnClearLog.Click += (s, e) => txtLog.Clear();

            txtLog = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(panel.Width - 25, panel.Height - 45),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = UIConfig.Colors.LogBg,
                ForeColor = UIConfig.Colors.LogText,
                Font = new Font("Consolas", UIConfig.Font.Console),
                BorderStyle = BorderStyle.FixedSingle
            };

            panel.Controls.AddRange(new Control[] { lblLog, btnClearLog, txtLog });
            return panel;
        }

        #endregion

        #region T1-T8 Tab创建（使用ProtocolTabCreator）

        private TabPage CreateT1Tab()
        {
            var creator = new ProtocolTabCreator(protocolService, configService, LogMessage, ShowResult);
            var tab = creator.CreateT1Tab();
            tabPortSelectors["T1"] = tab.Controls.OfType<Panel>().FirstOrDefault()?.Controls.OfType<ComboBox>().FirstOrDefault();
            return tab;
        }

        private TabPage CreateT2Tab()
        {
            var creator = new ProtocolTabCreator(protocolService, configService, LogMessage, ShowResult);
            return creator.CreateT2Tab();
        }

        private TabPage CreateT3Tab()
        {
            var creator = new ProtocolTabCreator(protocolService, configService, LogMessage, ShowResult);
            return creator.CreateT3Tab();
        }

        private TabPage CreateT4Tab()
        {
            var creator = new ProtocolTabCreator(protocolService, configService, LogMessage, ShowResult);
            return creator.CreateT4Tab();
        }

        private TabPage CreateT5Tab()
        {
            var creator = new ProtocolTabCreator(protocolService, configService, LogMessage, ShowResult);
            return creator.CreateT5Tab();
        }

        private TabPage CreateT6Tab()
        {
            var creator = new ProtocolTabCreator(protocolService, configService, LogMessage, ShowResult);
            return creator.CreateT6Tab();
        }

        private TabPage CreateT7Tab()
        {
            var creator = new ProtocolTabCreator(protocolService, configService, LogMessage, ShowResult);
            return creator.CreateT7Tab();
        }

        private TabPage CreateT8Tab()
        {
            var creator = new ProtocolTabCreator(protocolService, configService, LogMessage, ShowResult);
            return creator.CreateT8Tab();
        }

        #endregion

        #region AOI Tab创建

        private TabPage CreateAOITab()
        {
            var tab = new TabPage("AOI交互") { BackColor = UIConfig.Colors.BgSecondary };

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 8,
                Padding = new Padding(20),
                BackColor = UIConfig.Colors.BgSecondary
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var config = configService.GetConfig();
            var lblAoiServer = UIHelper.CreateStyledLabel("AOI服务器:");
            txtAoiServer = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false,
                defaultText: $"{config.AoiServerIP}:{config.AoiServerPort}");
            txtAoiServer.Width = 200;

            btnAoiConnect = UIHelper.CreateStyledButton("连接AOI", 120, 35, isPrimary: true);

            var lblStation = UIHelper.CreateStyledLabel("工位:");
            var cmbStation = UIHelper.CreateStyledComboBox(new string[] {
                "GXFront - 管壳正面",
                "GXBack - 管壳背面",
                "ZCFront - 锗窗正面",
                "ZCBack - 锗窗背面",
                "Side - 侧面",
                "Out - 出料俯拍"
            }, 0);

            var lblX = UIHelper.CreateStyledLabel("拍照坐标X:");
            var txtX = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "0.123");
            txtX.Width = 200;

            var lblY = UIHelper.CreateStyledLabel("拍照坐标Y:");
            var txtY = UIHelper.CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "43.32");
            txtY.Width = 200;

            Panel btnPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var btnCapture = UIHelper.CreateStyledButton("Capture 普通拍照", 150, 35, isPrimary: true);
            btnCapture.Location = new Point(0, 5);
            var btnDetect = UIHelper.CreateStyledButton("Detect AOI检测", 150, 35, isPrimary: true);
            btnDetect.Location = new Point(160, 5);
            btnPanel.Controls.AddRange(new Control[] { btnCapture, btnDetect });

            var txtResult = UIHelper.CreateStyledTextBox(multiline: true, readOnly: true);

            // AOI连接按钮事件
            btnAoiConnect.Click += async (s, e) =>
            {
                if (aoiService.IsConnected)
                {
                    aoiService.Disconnect();
                }
                else
                {
                    var serverInfo = txtAoiServer.Text.Split(':');
                    string ip = serverInfo[0];
                    int port = int.Parse(serverInfo[1]);

                    btnAoiConnect.Enabled = false;
                    bool success = await aoiService.ConnectAsync(ip, port);
                    btnAoiConnect.Enabled = true;

                    if (success)
                    {
                        configService.UpdateAOIServer(ip, port);
                        configService.SaveConfig(configService.GetConfig());
                    }
                }
            };

            // Capture命令
            btnCapture.Click += async (s, e) =>
            {
                if (!aoiService.IsConnected)
                {
                    MessageBox.Show("请先连接AOI服务器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    btnCapture.Enabled = false;
                    string station = cmbStation.SelectedItem?.ToString()?.Split('-')[0].Trim() ?? "GXFront";
                    var response = await aoiService.SendCaptureAsync(station, txtX.Text, txtY.Text);
                    ShowResult(response.ToString(), txtResult);
                }
                catch (Exception ex)
                {
                    ShowResult($"Capture异常: {ex.Message}", txtResult);
                }
                finally
                {
                    btnCapture.Enabled = true;
                }
            };

            // Detect命令
            btnDetect.Click += async (s, e) =>
            {
                if (!aoiService.IsConnected)
                {
                    MessageBox.Show("请先连接AOI服务器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    btnDetect.Enabled = false;
                    string station = cmbStation.SelectedItem?.ToString()?.Split('-')[0].Trim() ?? "GXFront";
                    var response = await aoiService.SendDetectAsync(station, txtX.Text, txtY.Text);
                    ShowResult(response.ToString(), txtResult);
                }
                catch (Exception ex)
                {
                    ShowResult($"Detect异常: {ex.Message}", txtResult);
                }
                finally
                {
                    btnDetect.Enabled = true;
                }
            };

            layout.Controls.Add(lblAoiServer, 0, 0);
            layout.Controls.Add(txtAoiServer, 1, 0);
            layout.Controls.Add(btnAoiConnect, 2, 0);
            layout.Controls.Add(lblStation, 0, 2);
            layout.Controls.Add(cmbStation, 1, 2);
            layout.Controls.Add(lblX, 0, 3);
            layout.Controls.Add(txtX, 1, 3);
            layout.Controls.Add(lblY, 0, 4);
            layout.Controls.Add(txtY, 1, 4);
            layout.Controls.Add(btnPanel, 0, 6);
            layout.SetColumnSpan(btnPanel, 4);
            layout.Controls.Add(txtResult, 0, 7);
            layout.SetColumnSpan(txtResult, 4);

            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            return tab;
        }

        #endregion

        #region 事件处理

        private void OnProtocolConnectionChanged(int port, bool isConnected)
        {
            if (portButtons.ContainsKey(port))
            {
                var btn = portButtons[port];
                if (btn.InvokeRequired)
                {
                    btn.Invoke(new Action(() => UpdatePortButton(btn, port, isConnected)));
                }
                else
                {
                    UpdatePortButton(btn, port, isConnected);
                }
            }

            UpdateConnectionStatus();
        }

        private void UpdatePortButton(Button btn, int port, bool isConnected)
        {
            btn.Text = $"{port}\n{(isConnected ? "已连接" : "未连接")}";
            btn.BackColor = isConnected ? UIConfig.Colors.ButtonSuccess : UIConfig.Colors.ButtonDefault;
            btn.ForeColor = isConnected ? Color.White : UIConfig.Colors.TextSecondary;
        }

        private void OnAOIConnectionChanged(bool isConnected)
        {
            if (btnAoiConnect != null)
            {
                if (btnAoiConnect.InvokeRequired)
                {
                    btnAoiConnect.Invoke(new Action(() => UpdateAOIButton(isConnected)));
                }
                else
                {
                    UpdateAOIButton(isConnected);
                }
            }
        }

        private void UpdateAOIButton(bool isConnected)
        {
            if (btnAoiConnect != null)
            {
                btnAoiConnect.Text = isConnected ? "断开AOI" : "连接AOI";
                btnAoiConnect.BackColor = isConnected ? UIConfig.Colors.ButtonSuccess : UIConfig.Colors.ButtonPrimary;
                btnAoiConnect.ForeColor = Color.White;
            }
        }

        private void UpdateConnectionStatus()
        {
            var lblStatus = this.Controls.Find("lblConnectionStatus", true).FirstOrDefault() as Label;
            if (lblStatus != null)
            {
                var connectedPorts = protocolService.GetConnectedPorts();
                if (connectedPorts.Any())
                {
                    lblStatus.Text = $"已连接端口: {string.Join(", ", connectedPorts)}";
                    lblStatus.ForeColor = UIConfig.Colors.ButtonSuccess;
                }
                else
                {
                    lblStatus.Text = "未连接";
                    lblStatus.ForeColor = UIConfig.Colors.TextSecondary;
                }
            }
        }

        private async Task AutoConnectAllPorts()
        {
            var config = configService.GetConfig();
            await protocolService.AutoConnectAllAsync(config.Ports);
        }

        #endregion

        #region 日志和结果显示

        private void LogMessage(string message, bool isError = false)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => LogMessage(message, isError)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            txtLog.AppendText($"[{timestamp}] {message}\r\n");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void ShowResult(string result, TextBox txtResult)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowResult(result, txtResult)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string separator = new string('=', 80);
            txtResult.AppendText($"{separator}\r\n");
            txtResult.AppendText($"[{timestamp}] {result}\r\n");
            txtResult.AppendText($"{separator}\r\n\r\n");
            txtResult.SelectionStart = txtResult.Text.Length;
            txtResult.ScrollToCaret();

            LogMessage(result);
        }

        #endregion

        #region TabControl自定义绘制

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tc = sender as TabControl;
            if (tc == null) return;

            Graphics g = e.Graphics;
            TabPage tp = tc.TabPages[e.Index];
            Rectangle rect = e.Bounds;

            bool isSelected = (e.Index == tc.SelectedIndex);

            using (SolidBrush bgBrush = new SolidBrush(isSelected ? UIConfig.Colors.BgSecondary : UIConfig.Colors.BgMain))
            {
                g.FillRectangle(bgBrush, rect);
            }

            using (SolidBrush textBrush = new SolidBrush(UIConfig.Colors.TextPrimary))
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(tp.Text, tc.Font, textBrush, rect, sf);
            }
        }

        private void TabControl_Paint(object sender, PaintEventArgs e)
        {
            TabControl tc = sender as TabControl;
            if (tc == null || tc.TabCount == 0) return;

            using (SolidBrush bgBrush = new SolidBrush(UIConfig.Colors.BgSecondary))
            {
                Rectangle displayRect = tc.DisplayRectangle;
                e.Graphics.FillRectangle(bgBrush, new Rectangle(0, displayRect.Top, displayRect.Left, displayRect.Height));
                e.Graphics.FillRectangle(bgBrush, new Rectangle(displayRect.Right, displayRect.Top, tc.Width - displayRect.Right, displayRect.Height));
                e.Graphics.FillRectangle(bgBrush, new Rectangle(0, displayRect.Bottom, tc.Width, tc.Height - displayRect.Bottom));
            }
        }

        #endregion

        #region 窗体关闭

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // 保存AOI服务器配置
                if (txtAoiServer != null && !string.IsNullOrEmpty(txtAoiServer.Text))
                {
                    var serverInfo = txtAoiServer.Text.Split(':');
                    if (serverInfo.Length == 2)
                    {
                        string ip = serverInfo[0];
                        if (int.TryParse(serverInfo[1], out int port))
                        {
                            configService.UpdateAOIServer(ip, port);
                        }
                    }
                }

                // 保存配置到文件
                configService.SaveConfig(configService.GetConfig());

                // 断开所有连接
                protocolService.DisconnectAll();
                aoiService.Disconnect();
            }
            catch { }
            base.OnFormClosing(e);
        }

        #endregion
    }
}
