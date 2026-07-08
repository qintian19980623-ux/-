using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClassLibrary1;

namespace 通讯协议测试
{
    public partial class VisionProtocolTestForm : Form
    {
        #region UI尺寸配置常量
        // 主窗体尺寸
        private const int FORM_WIDTH = 1400;
        private const int FORM_HEIGHT = 900;
        private const int FORM_MIN_WIDTH = 1200;
        private const int FORM_MIN_HEIGHT = 800;

        // 主布局区域高度
        private const float CONNECTION_PANEL_HEIGHT = 90F;
        private const float LOG_PANEL_HEIGHT = 220F;

        // 连接区域
        private const int CONNECTION_GROUP_WIDTH = 500;
        private const int CONNECTION_GROUP_HEIGHT = 75;
        private const int PORT_BUTTON_WIDTH = 80;
        private const int PORT_BUTTON_HEIGHT = 40;
        private const int PORT_BUTTON_SPACING = 90;

        // 端口选择器Panel
        private const int PORT_SELECTOR_PANEL_HEIGHT = 35;
        private const int PORT_SELECTOR_WIDTH = 100;

        // Tab页布局内边距
        private const int TAB_PADDING_TOP = 45;  // 为端口选择器留出空间
        private const int TAB_PADDING_SIDE = 20;

        // 标签列宽度
        private const float LABEL_COLUMN_WIDTH = 150F;
        private const float LABEL_COLUMN_WIDTH_NARROW = 120F;
        private const float LABEL_COLUMN_WIDTH_WIDE = 140F;

        // 输入框列宽度
        private const float INPUT_COLUMN_WIDTH = 220F;
        private const float INPUT_COLUMN_WIDTH_NARROW = 200F;
        private const float INPUT_COLUMN_WIDTH_WIDE = 250F;
        private const int INPUT_BOX_WIDTH = 300;

        // 行高
        private const float ROW_HEIGHT_SMALL = 35F;
        private const float ROW_HEIGHT_NORMAL = 40F;
        private const float ROW_HEIGHT_LARGE = 45F;
        private const float ROW_HEIGHT_BUTTON = 50F;
        private const float ROW_HEIGHT_SPACING = 10F;

        // 按钮尺寸
        private const int BUTTON_WIDTH = 150;
        private const int BUTTON_HEIGHT = 35;
        private const int BUTTON_WIDTH_WIDE = 140;
        private const int BUTTON_WIDTH_EXTRA_WIDE = 160;

        // 字体大小
        private const float FONT_SIZE_SMALL = 8.5F;
        private const float FONT_SIZE_NORMAL = 9F;
        private const float FONT_SIZE_LARGE = 10F;
        private const float FONT_SIZE_CONSOLE = 9F;

        // 颜色 - 科技风深色主题
        private static readonly Color COLOR_BG_MAIN = Color.FromArgb(18, 22, 30);           // 主背景：深邃太空灰
        private static readonly Color COLOR_BG_SECONDARY = Color.FromArgb(25, 30, 40);      // 次级背景：深灰蓝
        private static readonly Color COLOR_BG_PANEL = Color.FromArgb(30, 36, 48);          // 面板背景
        private static readonly Color COLOR_BUTTON_PRIMARY = Color.FromArgb(0, 174, 255);   // 科技蓝：主按钮
        private static readonly Color COLOR_BUTTON_SECONDARY = Color.FromArgb(0, 229, 255); // 科技青：次要元素
        private static readonly Color COLOR_BUTTON_SUCCESS = Color.FromArgb(0, 255, 159);   // 霓虹绿：已连接状态
        private static readonly Color COLOR_BUTTON_ERROR = Color.FromArgb(255, 68, 119);    // 霓虹红：未连接/错误
        private static readonly Color COLOR_BUTTON_DEFAULT = Color.FromArgb(50, 55, 65);    // 深灰：未连接按钮
        private static readonly Color COLOR_TEXT_PRIMARY = Color.FromArgb(240, 245, 255);   // 主文字
        private static readonly Color COLOR_TEXT_SECONDARY = Color.FromArgb(160, 170, 190); // 次要文字
        private static readonly Color COLOR_LOG_BG = Color.FromArgb(10, 15, 20);            // 日志背景：极深背景
        private static readonly Color COLOR_LOG_TEXT = Color.FromArgb(0, 255, 180);         // 日志文字：终端风格绿
        private static readonly Color COLOR_PANEL_BG = COLOR_BG_SECONDARY;
        private static readonly Color COLOR_PORT_SELECTOR_BG = COLOR_BG_PANEL;
        #endregion

        // 4个端口的连接实例
        private Dictionary<int, VisionMotionProtocol> protocols = new Dictionary<int, VisionMotionProtocol>();
        private Dictionary<int, Button> portButtons = new Dictionary<int, Button>();
        private string serverIP = "127.0.0.1";
        private int[] ports = { 7920, 7930, 7940, 7950 };

        private TextBox txtLog;
        private TabControl tabControl;

        // 配置文件路径
        private string configFilePath = "PortConfig.json";

        // 每个Tab的端口选择器
        private Dictionary<string, ComboBox> tabPortSelectors = new Dictionary<string, ComboBox>();

        // 自动重连相关
        private Dictionary<int, System.Threading.Timer> reconnectTimers = new Dictionary<int, System.Threading.Timer>();
        private Dictionary<int, bool> isReconnecting = new Dictionary<int, bool>();
        private const int RECONNECT_INTERVAL = 5000; // 5秒重连一次

        // 配置类
        private class PortConfig
        {
            public Dictionary<string, int> TabPorts { get; set; } = new Dictionary<string, int>();
        }

        public VisionProtocolTestForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // 启用DPI自动缩放 - 解决不同分辨率下UI布局差异问题
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);

            this.Text = "视觉运控通讯协议测试工具";
            this.Size = new Size(FORM_WIDTH, FORM_HEIGHT);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(FORM_MIN_WIDTH, FORM_MIN_HEIGHT);
            this.BackColor = COLOR_BG_MAIN;

            // 主布局容器
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(5)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, CONNECTION_PANEL_HEIGHT));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, LOG_PANEL_HEIGHT));

            // 1. 连接区域
            Panel pnlConnection = CreateConnectionPanel();
            mainLayout.Controls.Add(pnlConnection, 0, 0);

            // 2. TabControl 区域
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", FONT_SIZE_LARGE, FontStyle.Bold),
                BackColor = COLOR_BG_SECONDARY,
                ForeColor = COLOR_TEXT_PRIMARY,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.FillToRight,
            };
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.Paint += TabControl_Paint;
            tabControl.TabPages.Add(CreateT1Tab());
            tabControl.TabPages.Add(CreateT2Tab());
            tabControl.TabPages.Add(CreateT3Tab());
            tabControl.TabPages.Add(CreateT4Tab());
            tabControl.TabPages.Add(CreateT5Tab());
            tabControl.TabPages.Add(CreateT6Tab());
            tabControl.TabPages.Add(CreateT7Tab());
            tabControl.TabPages.Add(CreateT8Tab());
            mainLayout.Controls.Add(tabControl, 0, 1);

            // 3. 日志区域
            Panel pnlLog = CreateLogPanel();
            mainLayout.Controls.Add(pnlLog, 0, 2);

            this.Controls.Add(mainLayout);

            // 加载端口配置
            LoadPortConfig();

            // 启动时自动连接所有端口
            this.Load += async (s, e) => await AutoConnectAllPorts();
        }

        private Panel CreateConnectionPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = COLOR_PANEL_BG,
                Padding = new Padding(10)
            };

            GroupBox grpConnection = new GroupBox
            {
                Text = "服务器连接 (127.0.0.1)",
                Location = new Point(10, 5),
                Size = new Size(CONNECTION_GROUP_WIDTH, CONNECTION_GROUP_HEIGHT),
                Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold),
                ForeColor = COLOR_BUTTON_SECONDARY,
                BackColor = COLOR_PANEL_BG
            };

            Label lblPorts = new Label
            {
                Text = "端口:",
                Location = new Point(15, 28),
                Size = new Size(50, 25),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("微软雅黑", FONT_SIZE_NORMAL),
                ForeColor = COLOR_TEXT_PRIMARY,
                BackColor = Color.Transparent
            };

            // 4个快捷端口按钮
            int startX = 70;
            for (int i = 0; i < ports.Length; i++)
            {
                var btn = CreateQuickPortButton(ports[i], startX + i * PORT_BUTTON_SPACING, 26);
                portButtons[ports[i]] = btn;
                grpConnection.Controls.Add(btn);
            }

            grpConnection.Controls.Add(lblPorts);

            Label lblStatus = new Label
            {
                Text = "未连接",
                Location = new Point(520, 30),
                Size = new Size(800, 25),
                ForeColor = COLOR_TEXT_SECONDARY,
                BackColor = Color.Transparent,
                Font = new Font("微软雅黑", FONT_SIZE_NORMAL)
            };
            lblStatus.Name = "lblConnectionStatus";

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
                Size = new Size(PORT_BUTTON_WIDTH, PORT_BUTTON_HEIGHT),
                Font = new Font("微软雅黑", FONT_SIZE_SMALL),
                BackColor = COLOR_BUTTON_DEFAULT,
                ForeColor = COLOR_TEXT_SECONDARY,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = port,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderColor = COLOR_BUTTON_PRIMARY;
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += QuickPortButton_Click;
            return btn;
        }

        private async void QuickPortButton_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn) return;
            int port = (int)(btn.Tag ?? 0);

            // 如果当前端口已连接，则断开
            if (protocols.ContainsKey(port) && protocols[port].IsConnected)
            {
                await DisconnectPort(port);
            }
            else
            {
                // 连接到该端口
                await ConnectPort(port);
            }
        }

        private async Task ConnectPort(int port)
        {
            try
            {
                var btn = portButtons[port];
                btn.Text = $"{port}\n连接中...";
                btn.Enabled = false;

                var protocol = new VisionMotionProtocol();

                // 订阅日志事件
                protocol.OnLog += (msg) => LogMessage($"[{port}] {msg}");
                protocol.OnError += (msg) => LogMessage($"[{port}] {msg}", true);

                bool connected = await protocol.ConnectAsync(serverIP, port, 5000);

                if (connected)
                {
                    protocols[port] = protocol;
                    btn.Text = $"{port}\n已连接";
                    btn.BackColor = COLOR_BUTTON_SUCCESS;
                    btn.ForeColor = Color.White;
                    LogMessage($"端口 {port} 连接成功");
                    UpdateConnectionStatus();

                    // 停止该端口的重连定时器
                    StopReconnectTimer(port);
                }
                else
                {
                    btn.Text = $"{port}\n未连接";
                    btn.BackColor = COLOR_BUTTON_DEFAULT;
                    btn.ForeColor = COLOR_TEXT_SECONDARY;
                    LogMessage($"端口 {port} 连接失败", true);

                    // 启动自动重连
                    StartReconnectTimer(port);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"端口 {port} 连接异常: {ex.Message}", true);
                var btn = portButtons[port];
                btn.Text = $"{port}\n未连接";
                btn.BackColor = COLOR_BUTTON_DEFAULT;
                btn.ForeColor = COLOR_TEXT_SECONDARY;

                // 启动自动重连
                StartReconnectTimer(port);
            }
            finally
            {
                portButtons[port].Enabled = true;
            }
        }

        private async Task DisconnectPort(int port)
        {
            try
            {
                // 停止自动重连
                StopReconnectTimer(port);

                if (protocols.ContainsKey(port))
                {
                    protocols[port].Disconnect();
                    protocols.Remove(port);
                }

                var btn = portButtons[port];
                btn.Text = $"{port}\n未连接";
                btn.BackColor = COLOR_BUTTON_DEFAULT;
                btn.ForeColor = COLOR_TEXT_SECONDARY;
                LogMessage($"端口 {port} 已断开");
                UpdateConnectionStatus();
            }
            catch (Exception ex)
            {
                LogMessage($"端口 {port} 断开异常: {ex.Message}", true);
            }
        }

        private void UpdateConnectionStatus()
        {
            var lblStatus = this.Controls.Find("lblConnectionStatus", true).FirstOrDefault() as Label;
            if (lblStatus != null)
            {
                var connectedPorts = protocols.Where(p => p.Value.IsConnected).Select(p => p.Key).OrderBy(p => p);
                if (connectedPorts.Any())
                {
                    lblStatus.Text = $"已连接端口: {string.Join(", ", connectedPorts)}";
                    lblStatus.ForeColor = COLOR_BUTTON_SUCCESS;
                }
                else
                {
                    lblStatus.Text = "未连接";
                    lblStatus.ForeColor = COLOR_TEXT_SECONDARY;
                }
            }
        }

        // 启动时自动连接所有端口
        private async Task AutoConnectAllPorts()
        {
            LogMessage("正在自动连接所有端口...");
            foreach (int port in ports)
            {
                await ConnectPort(port);
                await Task.Delay(500); // 间隔500ms，避免同时连接
            }
        }

        // 启动自动重连定时器
        private void StartReconnectTimer(int port)
        {
            if (isReconnecting.ContainsKey(port) && isReconnecting[port])
                return; // 已经在重连中

            isReconnecting[port] = true;

            var timer = new System.Threading.Timer(async (state) =>
            {
                if (!protocols.ContainsKey(port) || !protocols[port].IsConnected)
                {
                    LogMessage($"端口 {port} 尝试自动重连...");
                    await ConnectPortSilently(port);
                }
            }, null, RECONNECT_INTERVAL, RECONNECT_INTERVAL);

            reconnectTimers[port] = timer;
        }

        // 停止自动重连定时器
        private void StopReconnectTimer(int port)
        {
            if (reconnectTimers.ContainsKey(port))
            {
                reconnectTimers[port].Dispose();
                reconnectTimers.Remove(port);
            }

            if (isReconnecting.ContainsKey(port))
            {
                isReconnecting[port] = false;
            }
        }

        // 静默连接（不改变按钮状态，只记录日志）
        private async Task ConnectPortSilently(int port)
        {
            try
            {
                var protocol = new VisionMotionProtocol();

                // 订阅日志事件
                protocol.OnLog += (msg) => LogMessage($"[{port}] {msg}");
                protocol.OnError += (msg) => LogMessage($"[{port}] {msg}", true);

                bool connected = await protocol.ConnectAsync(serverIP, port, 3000);

                if (connected)
                {
                    protocols[port] = protocol;

                    // 更新按钮状态（需要在UI线程）
                    if (portButtons.ContainsKey(port))
                    {
                        var btn = portButtons[port];
                        if (btn.InvokeRequired)
                        {
                            btn.Invoke(new Action(() =>
                            {
                                btn.Text = $"{port}\n已连接";
                                btn.BackColor = COLOR_BUTTON_SUCCESS;
                                btn.ForeColor = Color.White;
                            }));
                        }
                        else
                        {
                            btn.Text = $"{port}\n已连接";
                            btn.BackColor = COLOR_BUTTON_SUCCESS;
                            btn.ForeColor = Color.White;
                        }
                    }

                    LogMessage($"端口 {port} 重连成功");
                    UpdateConnectionStatus();

                    // 停止重连定时器
                    StopReconnectTimer(port);
                }
            }
            catch (Exception ex)
            {
                // 重连失败不记录详细错误，避免日志刷屏
                // LogMessage($"端口 {port} 重连失败: {ex.Message}", true);
            }
        }

        private Panel CreateLogPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = COLOR_BG_PANEL,
                Padding = new Padding(5)
            };

            Label lblLog = new Label
            {
                Text = "通讯日志",
                Location = new Point(10, 8),
                AutoSize = true,
                Font = new Font("微软雅黑", FONT_SIZE_LARGE, FontStyle.Bold),
                ForeColor = COLOR_BUTTON_SECONDARY,
                BackColor = Color.Transparent
            };

            Button btnClearLog = new Button
            {
                Text = "清除",
                Location = new Point(panel.Width - 95, 5),
                Size = new Size(80, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("微软雅黑", FONT_SIZE_NORMAL),
                BackColor = COLOR_BUTTON_PRIMARY,
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
                BackColor = COLOR_LOG_BG,
                ForeColor = COLOR_LOG_TEXT,
                Font = new Font("Consolas", FONT_SIZE_CONSOLE),
                BorderStyle = BorderStyle.FixedSingle
            };

            panel.Controls.AddRange(new Control[] { lblLog, btnClearLog, txtLog });
            return panel;
        }

        private ComboBox CreateCameraCombo(int defaultValue = 1)
        {
            var combo = new ComboBox
            {
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", FONT_SIZE_NORMAL),
                BackColor = COLOR_BG_SECONDARY,
                ForeColor = COLOR_TEXT_PRIMARY,
                FlatStyle = FlatStyle.Flat
            };
            combo.Items.AddRange(new object[] {
                "1 - 管壳上相机",
                "2 - 管壳下相机",
                "3 - 锗窗上相机",
                "4 - 锗窗下相机",
                "5 - 出料相机"
            });
            combo.SelectedIndex = defaultValue - 1;
            return combo;
        }

        private ComboBox CreateNozzleCombo(int defaultValue = 1)
        {
            var combo = new ComboBox
            {
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", FONT_SIZE_NORMAL),
                BackColor = COLOR_BG_SECONDARY,
                ForeColor = COLOR_TEXT_PRIMARY,
                FlatStyle = FlatStyle.Flat
            };
            combo.Items.AddRange(new object[] {
                "1 - 吸嘴1",
                "2 - 吸嘴2",
                "3 - 吸嘴3",
                "4 - 夹爪"
            });
            combo.SelectedIndex = defaultValue - 1;
            return combo;
        }

        private ComboBox CreateModuleCombo(int defaultValue = 1)
        {
            var combo = new ComboBox
            {
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", FONT_SIZE_NORMAL),
                BackColor = COLOR_BG_SECONDARY,
                ForeColor = COLOR_TEXT_PRIMARY,
                FlatStyle = FlatStyle.Flat
            };
            combo.Items.AddRange(new object[] {
                "1 - 管壳检测",
                "2 - 锗窗检测",
                "3 - 出料模组"
            });
            combo.SelectedIndex = defaultValue - 1;
            return combo;
        }

        private int GetComboValue(ComboBox combo)
        {
            return combo.SelectedIndex + 1;
        }

        #region T1-上下相机映射

        private TabPage CreateT1Tab()
        {
            var tab = new TabPage("T1-上下相机映射") { BackColor = COLOR_BG_SECONDARY };
            var cmbPort = CreatePortSelector(); // 端口选择器

            // 使用 TableLayoutPanel 进行布局
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(TAB_PADDING_SIDE, TAB_PADDING_TOP, TAB_PADDING_SIDE, TAB_PADDING_SIDE),
                BackColor = COLOR_BG_SECONDARY
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LABEL_COLUMN_WIDTH));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ROW_HEIGHT_NORMAL));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ROW_HEIGHT_BUTTON));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ROW_HEIGHT_SPACING));

            // 使用工厂方法创建控件
            var lblPosition = CreateStyledLabel("当前拍照位 (X,Y,R):");
            var txtPosition = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "100.5,200.3,45.0");
            txtPosition.Width = INPUT_BOX_WIDTH;

            var btnSend = CreateStyledButton("发送 T1 命令", BUTTON_WIDTH, BUTTON_HEIGHT, isPrimary: true);
            var txtResult = CreateStyledTextBox(multiline: true, readOnly: true);

            // 使用通用事件处理方法
            btnSend.Click += async (s, e) =>
            {
                await ExecuteProtocolCommand(cmbPort, btnSend, "发送", async (protocol) =>
                {
                    await protocol.SendT1Async(txtPosition.Text);
                    var response = await protocol.ReceiveT1Async(10000);
                    return $"T1结果 - {(response.Success ? "成功" : "失败")};{response.ResultCode};{response.X},{response.Y},{response.R}";
                });
            };

            layout.Controls.Add(lblPosition, 0, 0);
            layout.Controls.Add(txtPosition, 1, 0);
            layout.Controls.Add(btnSend, 0, 1);
            layout.SetColumnSpan(btnSend, 2);
            layout.Controls.Add(txtResult, 0, 2);
            layout.SetColumnSpan(txtResult, 2);

            tab.Controls.Add(CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            // 注册端口选择器
            tabPortSelectors["T1"] = cmbPort;

            return tab;
        }

        #endregion

        #region T2-九点标定

        private TabPage CreateT2Tab()
        {
            var tab = new TabPage("T2-九点标定") { BackColor = COLOR_BG_SECONDARY };
            var cmbPort = CreatePortSelector(); // 端口选择器

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 10,
                Padding = new Padding(TAB_PADDING_SIDE, TAB_PADDING_TOP, TAB_PADDING_SIDE, TAB_PADDING_SIDE),
                AutoScroll = true,
                BackColor = COLOR_BG_SECONDARY
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LABEL_COLUMN_WIDTH_WIDE));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, INPUT_COLUMN_WIDTH));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LABEL_COLUMN_WIDTH_WIDE));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ROW_HEIGHT_LARGE));      // 第0行
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ROW_HEIGHT_LARGE));      // 第1行
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ROW_HEIGHT_SMALL));      // 第2行
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ROW_HEIGHT_LARGE));      // 第3行
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ROW_HEIGHT_BUTTON));     // 第4行：按钮
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));                    // 第5行：结果开始
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));                    // 第6行
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));                    // 第7行
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));                    // 第8行
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));                    // 第9行

            var lblCamera = new Label { Text = "相机:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), ForeColor = COLOR_TEXT_PRIMARY, BackColor = Color.Transparent };
            var cmbCamera = CreateCameraCombo(1);
            cmbCamera.Dock = DockStyle.Top;
            cmbCamera.BackColor = COLOR_BG_SECONDARY;
            cmbCamera.ForeColor = COLOR_TEXT_PRIMARY;
            cmbCamera.FlatStyle = FlatStyle.Flat;

            var lblModule = new Label { Text = "模组:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), ForeColor = COLOR_TEXT_PRIMARY, BackColor = Color.Transparent };
            var cmbModule = CreateModuleCombo(1);
            cmbModule.Dock = DockStyle.Top;
            cmbModule.BackColor = COLOR_BG_SECONDARY;
            cmbModule.ForeColor = COLOR_TEXT_PRIMARY;
            cmbModule.FlatStyle = FlatStyle.Flat;

            var lblGripper = new Label { Text = "吸嘴/夹爪:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), ForeColor = COLOR_TEXT_PRIMARY, BackColor = Color.Transparent };
            var cmbGripper = CreateNozzleCombo(1);
            cmbGripper.Dock = DockStyle.Top;
            cmbGripper.BackColor = COLOR_BG_SECONDARY;
            cmbGripper.ForeColor = COLOR_TEXT_PRIMARY;
            cmbGripper.FlatStyle = FlatStyle.Flat;

            var lblStep = new Label { Text = "移动步长(mm):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), ForeColor = COLOR_TEXT_PRIMARY, BackColor = Color.Transparent };
            var txtStep = new TextBox { Text = "10.0", Dock = DockStyle.Top, Font = new Font("微软雅黑", FONT_SIZE_LARGE), BackColor = COLOR_BG_SECONDARY, ForeColor = COLOR_TEXT_PRIMARY, BorderStyle = BorderStyle.FixedSingle };

            // 分隔线标签
            // var lblSep1 = new Label { Text = "━━━ 九点标定参数 ━━━", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), ForeColor = COLOR_BUTTON_PRIMARY };

            var lblCenter = new Label { Text = "中心点(X,Y,R):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), ForeColor = COLOR_TEXT_PRIMARY, BackColor = Color.Transparent };
            var txtCenter = new TextBox { Text = "100.0,200.0,0.0", Dock = DockStyle.Top, Font = new Font("微软雅黑", FONT_SIZE_LARGE), BackColor = COLOR_BG_SECONDARY, ForeColor = COLOR_TEXT_PRIMARY, BorderStyle = BorderStyle.FixedSingle };

            var lblXDir = new Label { Text = "X方向:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), ForeColor = COLOR_TEXT_PRIMARY, BackColor = Color.Transparent };
            var chkXDir = new CheckBox { Text = "反向", Dock = DockStyle.Fill, Font = new Font("微软雅黑", FONT_SIZE_NORMAL), Checked = false, TextAlign = ContentAlignment.MiddleLeft, ForeColor = COLOR_TEXT_PRIMARY, BackColor = Color.Transparent };

            var lblYDir = new Label { Text = "Y方向:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), ForeColor = COLOR_TEXT_PRIMARY, BackColor = Color.Transparent };
            var chkYDir = new CheckBox { Text = "反向", Dock = DockStyle.Fill, Font = new Font("微软雅黑", FONT_SIZE_NORMAL), Checked = false, TextAlign = ContentAlignment.MiddleLeft, ForeColor = COLOR_TEXT_PRIMARY, BackColor = Color.Transparent };

            var lblRotateAngle = new Label { Text = "旋转角度(度):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), ForeColor = COLOR_TEXT_PRIMARY, BackColor = Color.Transparent };
            var txtRotateAngle = new TextBox { Text = "45.0", Dock = DockStyle.Top, Font = new Font("微软雅黑", FONT_SIZE_LARGE), BackColor = COLOR_BG_SECONDARY, ForeColor = COLOR_TEXT_PRIMARY, BorderStyle = BorderStyle.FixedSingle };

            // 分隔线标签
            //var lblSep2 = new Label { Text = "━━━ 旋转标定参数(相机2/4，九点后追加) ━━━", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), ForeColor = COLOR_BUTTON_PRIMARY };

            Panel btnPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var btnClear = new Button { Text = "1-清除标定点", Location = new Point(0, 5), Size = new Size(BUTTON_WIDTH_WIDE, BUTTON_HEIGHT), Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), Cursor = Cursors.Hand, BackColor = COLOR_BG_PANEL, ForeColor = COLOR_TEXT_PRIMARY, FlatStyle = FlatStyle.Flat };
            btnClear.FlatAppearance.BorderColor = COLOR_BUTTON_PRIMARY;
            btnClear.FlatAppearance.BorderSize = 1;
            var btnCalib = new Button { Text = "2-九点标定", Location = new Point(150, 5), Size = new Size(BUTTON_WIDTH_WIDE, BUTTON_HEIGHT), Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), Cursor = Cursors.Hand, BackColor = COLOR_BUTTON_PRIMARY, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCalib.FlatAppearance.BorderSize = 0;
            var btnCalibRotate = new Button { Text = "3-九点+旋转(16次)", Location = new Point(300, 5), Size = new Size(BUTTON_WIDTH_EXTRA_WIDE, BUTTON_HEIGHT), Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), Cursor = Cursors.Hand, BackColor = COLOR_BUTTON_PRIMARY, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCalibRotate.FlatAppearance.BorderSize = 0;
            var btnEnd = new Button { Text = "4-结束标定", Location = new Point(470, 5), Size = new Size(BUTTON_WIDTH_WIDE, BUTTON_HEIGHT), Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold), Cursor = Cursors.Hand, BackColor = COLOR_BG_PANEL, ForeColor = COLOR_TEXT_PRIMARY, FlatStyle = FlatStyle.Flat };
            btnEnd.FlatAppearance.BorderColor = COLOR_BUTTON_PRIMARY;
            btnEnd.FlatAppearance.BorderSize = 1;
            btnPanel.Controls.AddRange(new Control[] { btnClear, btnCalib, btnCalibRotate, btnEnd });

            var txtResult = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Font = new Font("Consolas", FONT_SIZE_CONSOLE), BackColor = COLOR_LOG_BG, ForeColor = COLOR_LOG_TEXT, BorderStyle = BorderStyle.FixedSingle };

            btnClear.Click += async (s, e) =>
            {
                var protocol = GetActiveProtocol(cmbPort);
                if (protocol == null) return;
                try
                {
                    btnClear.Enabled = false;
                    await protocol.SendT2Async(GetComboValue(cmbCamera), GetComboValue(cmbModule), GetComboValue(cmbGripper), 1, 1, double.Parse(txtStep.Text), txtCenter.Text);
                    var response = await protocol.ReceiveT2Async();
                    ShowResult($"清除标定点 - {(response.Success ? "成功" : "失败")};{response.ResultCode}");
                }
                catch (Exception ex) { ShowResult($"清除标定点异常: {ex.Message}"); }
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

                    // 解析中心点
                    var centerParts = txtCenter.Text.Split(',');
                    double centerX = double.Parse(centerParts[0]);
                    double centerY = double.Parse(centerParts[1]);
                    double centerR = double.Parse(centerParts[2]);
                    double step = double.Parse(txtStep.Text);

                    // X和Y方向系数（正向为1，反向为-1）
                    int xDir = chkXDir.Checked ? -1 : 1;
                    int yDir = chkYDir.Checked ? -1 : 1;

                    // 九点蛇形路径：从左上角开始，横向蛇形
                    // 1(左上) 2(中上) 3(右上)
                    // 6(左中) 5(中中) 4(右中)
                    // 7(左下) 8(中下) 9(右下)
                    var points = new (double x, double y)[]
                    {
                        (centerX + xDir * (-step), centerY + yDir * step),     // 点1：左上
                        (centerX, centerY + yDir * step),                      // 点2：中上
                        (centerX + xDir * step, centerY + yDir * step),        // 点3：右上
                        (centerX + xDir * step, centerY),                      // 点4：右中
                        (centerX, centerY),                                    // 点5：中心
                        (centerX + xDir * (-step), centerY),                   // 点6：左中
                        (centerX + xDir * (-step), centerY + yDir * (-step)),  // 点7：左下
                        (centerX, centerY + yDir * (-step)),                   // 点8：中下
                        (centerX + xDir * step, centerY + yDir * (-step))      // 点9：右下
                    };

                    for (int i = 0; i < 9; i++)
                    {
                        string currentPos = $"{points[i].x},{points[i].y},{centerR}";
                        await protocol.SendT2Async(GetComboValue(cmbCamera), GetComboValue(cmbModule), GetComboValue(cmbGripper), i + 1, 2, step, currentPos);
                        var response = await protocol.ReceiveT2Async();
                        ShowResult($"标定第{i + 1}点 ({points[i].x:F1},{points[i].y:F1}) - {(response.Success ? "成功" : "失败")};{response.ResultCode}");
                        await Task.Delay(500);
                    }
                    ShowResult("九点标定完成！");
                }
                catch (Exception ex) { ShowResult($"执行标定异常: {ex.Message}"); }
                finally { btnCalib.Enabled = true; btnCalib.Text = "2-九点标定"; }
            };

            btnCalibRotate.Click += async (s, e) =>
            {
                var protocol = GetActiveProtocol(cmbPort);
                if (protocol == null) return;
                int cameraNo = GetComboValue(cmbCamera);
                if (cameraNo != 2 && cameraNo != 4)
                {
                    MessageBox.Show("九点+旋转标定仅适用于下相机（相机2：管壳下相机 或 相机4：锗窗下相机）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                try
                {
                    btnCalibRotate.Enabled = false;
                    btnCalibRotate.Text = "标定中...";

                    // 解析中心点
                    var centerParts = txtCenter.Text.Split(',');
                    double centerX = double.Parse(centerParts[0]);
                    double centerY = double.Parse(centerParts[1]);
                    double centerR = double.Parse(centerParts[2]);
                    double step = double.Parse(txtStep.Text);
                    double rotateAngle = double.Parse(txtRotateAngle.Text);

                    // X和Y方向系数（正向为1，反向为-1）
                    int xDir = chkXDir.Checked ? -1 : 1;
                    int yDir = chkYDir.Checked ? -1 : 1;

                    // 第一步：执行九点标定
                    ShowResult("========== 开始九点标定 ==========");
                    var points = new (double x, double y)[]
                    {
                        (centerX + xDir * (-step), centerY + yDir * step),     // 点1：左上
                        (centerX, centerY + yDir * step),                      // 点2：中上
                        (centerX + xDir * step, centerY + yDir * step),        // 点3：右上
                        (centerX + xDir * step, centerY),                      // 点4：右中
                        (centerX, centerY),                                    // 点5：中心
                        (centerX + xDir * (-step), centerY),                   // 点6：左中
                        (centerX + xDir * (-step), centerY + yDir * (-step)),  // 点7：左下
                        (centerX, centerY + yDir * (-step)),                   // 点8：中下
                        (centerX + xDir * step, centerY + yDir * (-step))      // 点9：右下
                    };

                    for (int i = 0; i < 9; i++)
                    {
                        string currentPos = $"{points[i].x},{points[i].y},{centerR}";
                        await protocol.SendT2Async(GetComboValue(cmbCamera), GetComboValue(cmbModule), GetComboValue(cmbGripper), i + 1, 2, step, currentPos);
                        var response = await protocol.ReceiveT2Async();
                        ShowResult($"九点第{i + 1}点 ({points[i].x:F1},{points[i].y:F1}) - {(response.Success ? "成功" : "失败")};{response.ResultCode}");
                        await Task.Delay(500);
                    }
                    ShowResult("九点标定完成！");

                    // 第二步：执行7次旋转标定（第1次不加旋转角度，从第2次开始加）
                    ShowResult("\n========== 开始旋转标定 ==========");
                    for (int i = 1; i <= 7; i++)
                    {
                        double currentR = centerR + ((i - 1) * rotateAngle);  // 第1次: i=1时角度为0，第2次: i=2时角度为1*rotateAngle
                        string currentPos = $"{centerX},{centerY},{currentR}";
                        await protocol.SendT2Async(GetComboValue(cmbCamera), GetComboValue(cmbModule), GetComboValue(cmbGripper), 9 + i, 2, step, currentPos);
                        var response = await protocol.ReceiveT2Async();
                        ShowResult($"旋转标定第{i}次 (R={currentR:F1}度) - {(response.Success ? "成功" : "失败")};{response.ResultCode}");
                        await Task.Delay(500);
                    }
                    ShowResult("旋转标定完成！\n========== 总计完成16次标定 ==========");
                }
                catch (Exception ex) { ShowResult($"九点+旋转标定异常: {ex.Message}"); }
                finally { btnCalibRotate.Enabled = true; btnCalibRotate.Text = "3-九点+旋转(16次)"; }
            };

            btnEnd.Click += async (s, e) =>
            {
                var protocol = GetActiveProtocol(cmbPort);
                if (protocol == null) return;
                try
                {
                    btnEnd.Enabled = false;
                    var centerParts = txtCenter.Text.Split(',');
                    await protocol.SendT2Async(GetComboValue(cmbCamera), GetComboValue(cmbModule), GetComboValue(cmbGripper), 9, 3, double.Parse(txtStep.Text), txtCenter.Text);
                    var response = await protocol.ReceiveT2Async();
                    ShowResult($"结束标定 - {(response.Success ? "成功" : "失败")};{response.ResultCode}");
                }
                catch (Exception ex) { ShowResult($"结束标定异常: {ex.Message}"); }
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

            //layout.Controls.Add(lblSep1, 0, 2);
            //layout.SetColumnSpan(lblSep1, 4);

            layout.Controls.Add(lblCenter, 0, 2);
            layout.Controls.Add(txtCenter, 1, 2);
            layout.Controls.Add(lblXDir, 2, 2);
            layout.Controls.Add(chkXDir, 3, 2);

            layout.Controls.Add(lblYDir, 0, 3);
            layout.Controls.Add(chkYDir, 1, 3);
            layout.Controls.Add(lblRotateAngle, 2, 3);
            layout.Controls.Add(txtRotateAngle, 3, 3);

            //layout.Controls.Add(lblSep2, 0, 6);
            //layout.SetColumnSpan(lblSep2, 4);

            layout.Controls.Add(btnPanel, 0, 4);
            layout.SetColumnSpan(btnPanel, 4);

            layout.Controls.Add(txtResult, 0, 5);
            layout.SetColumnSpan(txtResult, 4);
            layout.SetRowSpan(txtResult, 5);  // 跨越第5-9行，共5行

            tab.Controls.Add(CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            // 注册端口选择器
            tabPortSelectors["T2"] = cmbPort;

            return tab;
        }

        #endregion

        #region T3-吸嘴夹爪标定

        private TabPage CreateT3Tab()
        {
            var tab = new TabPage("T3-吸嘴夹爪标定") { BackColor = COLOR_BG_SECONDARY };
            var cmbPort = CreatePortSelector(); // 端口选择器

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(TAB_PADDING_SIDE, TAB_PADDING_TOP, TAB_PADDING_SIDE, TAB_PADDING_SIDE),
                BackColor = COLOR_BG_SECONDARY
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // 相机、吸嘴
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // 拍照位1
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // 拍照位2
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));  // 间隔
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // 按钮
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // 结果

            // 使用工厂方法创建控件
            var lblCamera = CreateStyledLabel("相机:");
            var cmbCamera = CreateCameraCombo(2);
            cmbCamera.Dock = DockStyle.Top;

            var lblGripper = CreateStyledLabel("吸嘴/夹爪:");
            var cmbGripper = CreateNozzleCombo(1);
            cmbGripper.Dock = DockStyle.Top;

            var lblPosition1 = CreateStyledLabel("拍照位1(X,Y,R):");
            var txtPosition1 = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "150.0,300.0,0.0");
            txtPosition1.Width = 300;

            var lblPosition2 = CreateStyledLabel("拍照位2(X,Y,R):");
            var txtPosition2 = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "150.0,250.0,90.0");
            txtPosition2.Width = 300;

            var btnSend = CreateStyledButton("发送 T3 命令", 150, 35, isPrimary: true);
            var txtResult = CreateStyledTextBox(multiline: true, readOnly: true);

            // T3需要发送两个拍照位（一次性发送）
            btnSend.Click += async (s, e) =>
            {
                var protocol = GetActiveProtocol(cmbPort);
                if (protocol == null) return;

                string originalText = btnSend.Text;
                try
                {
                    btnSend.Enabled = false;
                    btnSend.Text = "发送中...";

                    // 一次性发送两个拍照位
                    await protocol.SendT3Async(GetComboValue(cmbCamera), GetComboValue(cmbGripper), txtPosition1.Text, txtPosition2.Text);
                    var response = await protocol.ReceiveT3Async();
                    ShowResult($"T3结果 - {(response.Success ? "成功" : "失败")};{response.ResultCode};{response.X},{response.Y},{response.R}");
                }
                catch (Exception ex)
                {
                    ShowResult($"T3发送异常: {ex.Message}");
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

            tab.Controls.Add(CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            // 注册端口选择器
            tabPortSelectors["T3"] = cmbPort;

            return tab;
        }

        #endregion

        #region T4-管壳模组上下相机对齐

        private TabPage CreateT4Tab()
        {
            var tab = new TabPage("T4-管壳模组上下相机对齐") { BackColor = COLOR_BG_SECONDARY };
            var cmbPort = CreatePortSelector(); // 端口选择器

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
                Padding = new Padding(TAB_PADDING_SIDE, TAB_PADDING_TOP, TAB_PADDING_SIDE, TAB_PADDING_SIDE),
                BackColor = COLOR_BG_SECONDARY
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

            var lblCamera = CreateStyledLabel("相机:");
            var cmbCamera = CreateCameraCombo(1);
            cmbCamera.Dock = DockStyle.Top;

            var lblPosition = CreateStyledLabel("当前位置(X,Y,R):");
            var txtPosition = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "100.0,200.0,0.0");

            var lblNozzle = CreateStyledLabel("吸嘴取位(X,Y,R):");
            var txtNozzle = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "999,999,999");

            var lblHint = CreateStyledLabel("提示：上相机时填写 999,999,999", isBold: false);
            lblHint.Font = new Font("微软雅黑", 8F);
            lblHint.ForeColor = Color.Gray;

            var btnSend = CreateStyledButton("发送 T4 命令", 150, 35, isPrimary: true);
            var txtResult = CreateStyledTextBox(multiline: true, readOnly: true);

            btnSend.Click += async (s, e) =>
            {
                await ExecuteProtocolCommand(cmbPort, btnSend, "发送", async (protocol) =>
                {
                    await protocol.SendT4Async(GetComboValue(cmbCamera), txtPosition.Text, txtNozzle.Text);
                    var response = await protocol.ReceiveT4Async();
                    return $"T4结果 - {(response.Success ? "成功" : "失败")};{response.ResultCode};{response.X},{response.Y},{response.R}";
                });
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

            tab.Controls.Add(CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            // 注册端口选择器
            tabPortSelectors["T4"] = cmbPort;

            return tab;
        }

        #endregion

        #region T5-管壳检测模组自动引导

        private TabPage CreateT5Tab()
        {
            var tab = new TabPage("T5-管壳检测模组自动引导") { BackColor = COLOR_BG_SECONDARY };
            var cmbPort = CreatePortSelector();

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(TAB_PADDING_SIDE, TAB_PADDING_TOP, TAB_PADDING_SIDE, TAB_PADDING_SIDE),
                BackColor = COLOR_BG_SECONDARY
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

            // 使用工厂方法创建控件
            var lblCamera = CreateStyledLabel("相机:");
            var cmbCamera = CreateCameraCombo(1);
            cmbCamera.Dock = DockStyle.Top;

            var lblNozzle = CreateStyledLabel("吸嘴号:");
            var cmbNozzle = CreateNozzleCombo(1);
            cmbNozzle.Dock = DockStyle.Top;

            var lblFlag = CreateStyledLabel("取放料:");
            var cmbFlag = CreateStyledComboBox(new string[] { "1 - 取料", "2 - 放料" }, 0);

            var lblPos1 = CreateStyledLabel("拍照位1(X,Y,R):");
            var txtPos1 = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "100.0,200.0,0.0");

            var lblPos2 = CreateStyledLabel("拍照位2(X,Y,R):");
            var txtPos2 = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "999,999,999");

            var btnSend = CreateStyledButton("发送 T5 命令", BUTTON_WIDTH, BUTTON_HEIGHT, isPrimary: true);
            var txtResult = CreateStyledTextBox(multiline: true, readOnly: true);

            // 使用通用事件处理方法
            btnSend.Click += async (s, e) =>
            {
                await ExecuteProtocolCommand(cmbPort, btnSend, "发送", async (protocol) =>
                {
                    await protocol.SendT5Async(GetComboValue(cmbCamera), GetComboValue(cmbNozzle), cmbFlag.SelectedIndex + 1, txtPos1.Text, txtPos2.Text);
                    var response = await protocol.ReceiveT5Async();
                    return $"T5结果 - {(response.Success ? "成功" : "失败")};{response.ResultCode};{response.X},{response.Y},{response.R}";
                });
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

            tab.Controls.Add(CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            tabPortSelectors["T5"] = cmbPort;

            return tab;
        }

        #endregion

        #region T6-锗窗检测模组自动引导

        private TabPage CreateT6Tab()
        {
            var tab = new TabPage("T6-锗窗检测模组自动引导") { BackColor = COLOR_BG_SECONDARY };
            var cmbPort = CreatePortSelector();

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(TAB_PADDING_SIDE, TAB_PADDING_TOP, TAB_PADDING_SIDE, TAB_PADDING_SIDE),
                BackColor = COLOR_BG_SECONDARY
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

            // 使用工厂方法创建控件
            var lblCamera = CreateStyledLabel("相机:");
            var cmbCamera = CreateCameraCombo(3);
            cmbCamera.Dock = DockStyle.Top;

            var lblNozzle = CreateStyledLabel("吸嘴号:");
            var cmbNozzle = CreateNozzleCombo(1);
            cmbNozzle.Dock = DockStyle.Top;

            var lblFlag = CreateStyledLabel("取放料:");
            var cmbFlag = CreateStyledComboBox(new string[] { "1 - 取料", "2 - 放料" }, 0);

            var lblPos1 = CreateStyledLabel("拍照位1(X,Y,R):");
            var txtPos1 = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "200.0,300.0,0.0");

            var lblPos2 = CreateStyledLabel("拍照位2(X,Y,R):");
            var txtPos2 = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "999,999,999");

            var btnSend = CreateStyledButton("发送 T6 命令", 150, 35, isPrimary: true);
            var txtResult = CreateStyledTextBox(multiline: true, readOnly: true);

            // 使用通用事件处理方法
            btnSend.Click += async (s, e) =>
            {
                await ExecuteProtocolCommand(cmbPort, btnSend, "发送", async (protocol) =>
                {
                    await protocol.SendT6Async(GetComboValue(cmbCamera), GetComboValue(cmbNozzle), cmbFlag.SelectedIndex + 1, txtPos1.Text, txtPos2.Text);
                    var response = await protocol.ReceiveT6Async();
                    return $"T6结果 - {(response.Success ? "成功" : "失败")};{response.ResultCode};{response.X},{response.Y},{response.R}";
                });
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

            tab.Controls.Add(CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            tabPortSelectors["T6"] = cmbPort;

            return tab;
        }

        #endregion

        #region T7-锗窗取放料对位

        private TabPage CreateT7Tab()
        {
            var tab = new TabPage("T7-出料载具") { BackColor = COLOR_BG_SECONDARY };
            var cmbPort = CreatePortSelector();

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 7,
                Padding = new Padding(TAB_PADDING_SIDE, TAB_PADDING_TOP, TAB_PADDING_SIDE, TAB_PADDING_SIDE),
                BackColor = COLOR_BG_SECONDARY
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

            // 使用工厂方法创建控件
            var lblCarrier = CreateStyledLabel("载具号:");
            var txtCarrier = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "1");
            txtCarrier.Width = 100;

            var lblFunc = CreateStyledLabel("功能:");
            var cmbFunc = CreateStyledComboBox(new string[] { "1 - 定位", "2 - 示教" }, 0);

            var lblPos1 = CreateStyledLabel("拍照位1(X,Y,R):");
            var txtPos1 = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "300.0,400.0,0.0");

            var lblPos2 = CreateStyledLabel("拍照位2(X,Y,R):");
            var txtPos2 = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "305.0,405.0,0.0");

            var lblPick = CreateStyledLabel("取料位(X,Y,R):");
            var txtPick = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "999,999,999");

            var lblHint = CreateStyledLabel("提示：定位功能时填写 999,999,999", isBold: false);
            lblHint.Font = new Font("微软雅黑", 8F);
            lblHint.ForeColor = COLOR_TEXT_SECONDARY;

            var btnSend = CreateStyledButton("发送 T7 命令", 150, 35, isPrimary: true);
            var txtResult = CreateStyledTextBox(multiline: true, readOnly: true);

            // T7的事件处理逻辑比较复杂，保留原有方式
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
                        result += "R:" + response.R + "\n";
                    }
                    ShowResult(result);
                }
                catch (Exception ex) { ShowResult($"T7异常: {ex.Message}"); }
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

            tab.Controls.Add(CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            tabPortSelectors["T7"] = cmbPort;

            return tab;
        }

        #endregion

        #region T8-出料载具穴位对齐图心

        private TabPage CreateT8Tab()
        {
            var tab = new TabPage("T8-出料载具穴位对齐图心") { BackColor = COLOR_BG_SECONDARY };
            var cmbPort = CreatePortSelector();

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(TAB_PADDING_SIDE, TAB_PADDING_TOP, TAB_PADDING_SIDE, TAB_PADDING_SIDE),
                BackColor = COLOR_BG_SECONDARY
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

            // 使用工厂方法创建控件
            var lblCarrier = CreateStyledLabel("载具号:");
            var txtCarrier = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "4");
            txtCarrier.Width = 100;

            var lblPos1 = CreateStyledLabel("拍照位1(X,Y,R):");
            var txtPos1 = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "400.0,500.0,0.0");

            var lblPos2 = CreateStyledLabel("拍照位2(X,Y,R):");
            var txtPos2 = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "450.0,500.0,0.0");

            var lblPos3 = CreateStyledLabel("拍照位3(X,Y,R):");
            var txtPos3 = CreateStyledTextBox(multiline: false, readOnly: false, defaultText: "425.0,550.0,0.0");

            var btnSend = CreateStyledButton("发送 T8 命令", 150, 35, isPrimary: true);
            var txtResult = CreateStyledTextBox(multiline: true, readOnly: true);

            // 使用通用事件处理方法
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
                });
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

            tab.Controls.Add(CreatePortSelectorPanel(cmbPort));
            tab.Controls.Add(layout);
            tab.Tag = txtResult;

            tabPortSelectors["T8"] = cmbPort;

            return tab;
        }

        #endregion

        private VisionMotionProtocol? GetActiveProtocol(ComboBox cmbPort)
        {
            if (cmbPort.SelectedItem == null)
            {
                MessageBox.Show("请先选择端口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            int port = int.Parse(cmbPort.SelectedItem.ToString() ?? "0");
            if (protocols.ContainsKey(port) && protocols[port].IsConnected)
            {
                return protocols[port];
            }

            MessageBox.Show($"端口 {port} 未连接，请先连接该端口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        private ComboBox CreatePortSelector()
        {
            var cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", FONT_SIZE_NORMAL),
                Width = 80,
                BackColor = COLOR_BG_SECONDARY,
                ForeColor = COLOR_TEXT_PRIMARY,
                FlatStyle = FlatStyle.Flat
            };
            foreach (var port in ports)
            {
                cmb.Items.Add(port.ToString());
            }
            cmb.SelectedIndex = 3; // 默认选择7950

            // 当端口选择改变时保存配置
            cmb.SelectedIndexChanged += (s, e) => SavePortConfig();

            return cmb;
        }

        private void SavePortConfig()
        {
            try
            {
                var config = new PortConfig();
                foreach (var kvp in tabPortSelectors)
                {
                    if (kvp.Value.SelectedItem != null)
                    {
                        config.TabPorts[kvp.Key] = int.Parse(kvp.Value.SelectedItem.ToString() ?? "7950");
                    }
                }

                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFilePath, json);
            }
            catch (Exception ex)
            {
                LogMessage($"保存配置失败: {ex.Message}", true);
            }
        }

        private void LoadPortConfig()
        {
            try
            {
                if (!File.Exists(configFilePath))
                    return;

                string json = File.ReadAllText(configFilePath);
                var config = JsonSerializer.Deserialize<PortConfig>(json);

                if (config != null && config.TabPorts != null)
                {
                    foreach (var kvp in config.TabPorts)
                    {
                        if (tabPortSelectors.ContainsKey(kvp.Key))
                        {
                            var cmb = tabPortSelectors[kvp.Key];
                            var portStr = kvp.Value.ToString();
                            int index = cmb.Items.IndexOf(portStr);
                            if (index >= 0)
                            {
                                cmb.SelectedIndex = index;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"加载配置失败: {ex.Message}", true);
            }
        }

        private Panel CreatePortSelectorPanel(ComboBox cmbPort)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = PORT_SELECTOR_PANEL_HEIGHT,
                Padding = new Padding(10, 5, 10, 5),
                BackColor = COLOR_PORT_SELECTOR_BG
            };

            var lblPort = new Label
            {
                Text = "选择端口:",
                Location = new Point(10, 8),
                Size = new Size(80, 20),
                Font = new Font("微软雅黑", FONT_SIZE_NORMAL, FontStyle.Bold),
                ForeColor = COLOR_TEXT_PRIMARY,
                BackColor = Color.Transparent
            };

            cmbPort.Location = new Point(95, 5);
            cmbPort.Width = PORT_SELECTOR_WIDTH;
            cmbPort.BackColor = COLOR_BG_SECONDARY;
            cmbPort.ForeColor = COLOR_TEXT_PRIMARY;
            cmbPort.FlatStyle = FlatStyle.Flat;

            panel.Controls.Add(lblPort);
            panel.Controls.Add(cmbPort);

            return panel;
        }

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

        private void ShowResult(string result)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowResult(result)));
                return;
            }

            var currentTab = tabControl.SelectedTab;
            if (currentTab?.Tag is TextBox txtResult)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string separator = new string('=', 80);
                txtResult.AppendText($"{separator}\r\n");
                txtResult.AppendText($"[{timestamp}] {result}\r\n");
                txtResult.AppendText($"{separator}\r\n\r\n");
                txtResult.SelectionStart = txtResult.Text.Length;
                txtResult.ScrollToCaret();
            }

            LogMessage(result);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // 停止所有重连定时器
                foreach (var port in ports)
                {
                    StopReconnectTimer(port);
                }

                // 断开所有连接
                foreach (var kvp in protocols)
                {
                    if (kvp.Value != null && kvp.Value.IsConnected)
                    {
                        kvp.Value.Dispose();
                    }
                }
                protocols.Clear();
            }
            catch { }
            base.OnFormClosing(e);
        }

        #region 控件工厂方法

        /// <summary>
        /// 创建样式化按钮
        /// </summary>
        /// <param name="text">按钮文字</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="isPrimary">是否为主按钮（蓝色背景）</param>
        /// <returns>按钮控件</returns>
        private Button CreateStyledButton(string text, int width, int height, bool isPrimary = false)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                Font = new Font("微软雅黑", FONT_SIZE_LARGE, isPrimary ? FontStyle.Bold : FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };

            if (isPrimary)
            {
                btn.BackColor = COLOR_BUTTON_PRIMARY;
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderSize = 0;
            }
            else
            {
                btn.BackColor = COLOR_BG_PANEL;
                btn.ForeColor = COLOR_TEXT_PRIMARY;
                btn.FlatAppearance.BorderColor = COLOR_BUTTON_PRIMARY;
                btn.FlatAppearance.BorderSize = 1;
            }

            return btn;
        }

        /// <summary>
        /// 创建样式化文本框
        /// </summary>
        /// <param name="multiline">是否多行</param>
        /// <param name="readOnly">是否只读</param>
        /// <param name="defaultText">默认文本</param>
        /// <returns>文本框控件</returns>
        private TextBox CreateStyledTextBox(bool multiline = false, bool readOnly = false, string defaultText = "")
        {
            var txt = new TextBox
            {
                Multiline = multiline,
                ReadOnly = readOnly,
                Text = defaultText
            };

            if (multiline && readOnly)
            {
                // 结果显示框样式
                txt.ScrollBars = ScrollBars.Vertical;
                txt.Font = new Font("Consolas", FONT_SIZE_CONSOLE);
                txt.BackColor = COLOR_LOG_BG;
                txt.ForeColor = COLOR_LOG_TEXT;
                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.Dock = DockStyle.Fill;
            }
            else
            {
                // 普通输入框样式
                txt.Font = new Font("微软雅黑", FONT_SIZE_LARGE);
                txt.BackColor = COLOR_BG_SECONDARY;
                txt.ForeColor = COLOR_TEXT_PRIMARY;
                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.Dock = DockStyle.Top;
            }

            return txt;
        }

        /// <summary>
        /// 创建样式化标签
        /// </summary>
        /// <param name="text">标签文字</param>
        /// <param name="isBold">是否加粗</param>
        /// <returns>标签控件</returns>
        private Label CreateStyledLabel(string text, bool isBold = true)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("微软雅黑", FONT_SIZE_NORMAL, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = COLOR_TEXT_PRIMARY,
                BackColor = Color.Transparent
            };
        }

        /// <summary>
        /// 创建样式化下拉框（基础版）
        /// </summary>
        /// <param name="items">下拉项数组</param>
        /// <param name="defaultIndex">默认选中索引</param>
        /// <returns>下拉框控件</returns>
        private ComboBox CreateStyledComboBox(string[] items, int defaultIndex = 0)
        {
            var cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", FONT_SIZE_NORMAL),
                Dock = DockStyle.Top,
                BackColor = COLOR_BG_SECONDARY,
                ForeColor = COLOR_TEXT_PRIMARY,
                FlatStyle = FlatStyle.Flat
            };

            if (items != null && items.Length > 0)
            {
                cmb.Items.AddRange(items);
                if (defaultIndex >= 0 && defaultIndex < items.Length)
                {
                    cmb.SelectedIndex = defaultIndex;
                }
            }

            return cmb;
        }

        /// <summary>
        /// 执行协议命令的通用处理方法
        /// </summary>
        /// <param name="cmbPort">端口选择器</param>
        /// <param name="button">触发按钮</param>
        /// <param name="commandName">命令名称</param>
        /// <param name="executeAction">执行动作（发送和接收逻辑）</param>
        private async Task ExecuteProtocolCommand(ComboBox cmbPort, Button button, string commandName, Func<VisionMotionProtocol, Task<string>> executeAction)
        {
            var protocol = GetActiveProtocol(cmbPort);
            if (protocol == null) return;

            string originalText = button.Text;
            try
            {
                button.Enabled = false;
                button.Text = $"{commandName}中...";

                string result = await executeAction(protocol);
                ShowResult(result);
            }
            catch (Exception ex)
            {
                ShowResult($"{commandName}异常: {ex.Message}");
            }
            finally
            {
                button.Enabled = true;
                button.Text = originalText;
            }
        }

        /// <summary>
        /// TabControl自定义绘制事件 - 实现深色标签页
        /// </summary>
        private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            TabControl? tc = sender as TabControl;
            if (tc == null) return;

            Graphics g = e.Graphics;
            TabPage tp = tc.TabPages[e.Index];
            Rectangle rect = e.Bounds;

            // 判断是否为选中的标签页
            bool isSelected = (e.Index == tc.SelectedIndex);

            // 绘制背景
            using (SolidBrush bgBrush = new SolidBrush(isSelected ? COLOR_BG_SECONDARY : COLOR_BG_MAIN))
            {
                g.FillRectangle(bgBrush, rect);
            }

            // 绘制文字
            using (SolidBrush textBrush = new SolidBrush(COLOR_TEXT_PRIMARY))
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(tp.Text, tc.Font, textBrush, rect, sf);
            }
        }

        /// <summary>
        /// TabControl自定义绘制边框 - 移除白色边框
        /// </summary>
        private void TabControl_Paint(object? sender, PaintEventArgs e)
        {
            TabControl? tc = sender as TabControl;
            if (tc == null || tc.TabCount == 0) return;

            // 用深色填充整个TabControl区域，覆盖白色边框
            using (SolidBrush bgBrush = new SolidBrush(COLOR_BG_SECONDARY))
            {
                // 获取TabPage显示区域
                Rectangle displayRect = tc.DisplayRectangle;

                // 绘制左边框
                e.Graphics.FillRectangle(bgBrush, new Rectangle(0, displayRect.Top, displayRect.Left, displayRect.Height));

                // 绘制右边框
                e.Graphics.FillRectangle(bgBrush, new Rectangle(displayRect.Right, displayRect.Top, tc.Width - displayRect.Right, displayRect.Height));

                // 绘制底边框
                e.Graphics.FillRectangle(bgBrush, new Rectangle(0, displayRect.Bottom, tc.Width, tc.Height - displayRect.Bottom));
            }
        }

        #endregion
    }
}
