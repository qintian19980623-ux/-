using System.Drawing;
using System.Windows.Forms;

namespace 通讯协议测试
{
    /// <summary>
    /// UI辅助类 - 提供通用的UI控件创建方法
    /// </summary>
    public static class UIHelper
    {
        #region 标签创建

        /// <summary>
        /// 创建样式化标签
        /// </summary>
        public static Label CreateStyledLabel(string text, bool isBold = true)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("微软雅黑", UIConfig.Font.Normal, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = UIConfig.Colors.TextPrimary,
                BackColor = Color.Transparent
            };
        }

        #endregion

        #region 文本框创建

        /// <summary>
        /// 创建样式化文本框
        /// </summary>
        public static TextBox CreateStyledTextBox(bool multiline = false, bool readOnly = false, string defaultText = "")
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
                txt.Font = new Font("Consolas", UIConfig.Font.Console);
                txt.BackColor = UIConfig.Colors.LogBg;
                txt.ForeColor = UIConfig.Colors.LogText;
                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.Dock = DockStyle.Fill;
            }
            else
            {
                // 普通输入框样式
                txt.Font = new Font("微软雅黑", UIConfig.Font.Large);
                txt.BackColor = UIConfig.Colors.BgSecondary;
                txt.ForeColor = UIConfig.Colors.TextPrimary;
                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.Dock = DockStyle.Top;
            }

            return txt;
        }

        #endregion

        #region 按钮创建

        /// <summary>
        /// 创建样式化按钮
        /// </summary>
        public static Button CreateStyledButton(string text, int width, int height, bool isPrimary = false)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                Font = new Font("微软雅黑", UIConfig.Font.Large, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };

            if (isPrimary)
            {
                btn.BackColor = UIConfig.Colors.ButtonPrimary;
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderSize = 0;
            }
            else
            {
                btn.BackColor = UIConfig.Colors.BgPanel;
                btn.ForeColor = UIConfig.Colors.TextPrimary;
                btn.FlatAppearance.BorderColor = UIConfig.Colors.ButtonPrimary;
                btn.FlatAppearance.BorderSize = 1;
            }

            return btn;
        }

        #endregion

        #region 下拉框创建

        /// <summary>
        /// 创建样式化下拉框
        /// </summary>
        public static ComboBox CreateStyledComboBox(string[] items = null, int defaultIndex = 0)
        {
            var cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", UIConfig.Font.Normal),
                Dock = DockStyle.Top,
                BackColor = UIConfig.Colors.BgSecondary,
                ForeColor = UIConfig.Colors.TextPrimary,
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
        /// 创建相机选择下拉框
        /// </summary>
        public static ComboBox CreateCameraCombo(int defaultValue = 1)
        {
            var combo = CreateStyledComboBox();
            combo.Width = 180;
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

        /// <summary>
        /// 创建吸嘴选择下拉框
        /// </summary>
        public static ComboBox CreateNozzleCombo(int defaultValue = 1)
        {
            var combo = CreateStyledComboBox();
            combo.Width = 150;
            combo.Items.AddRange(new object[] {
                "1 - 吸嘴1",
                "2 - 吸嘴2",
                "3 - 吸嘴3",
                "4 - 夹爪"
            });
            combo.SelectedIndex = defaultValue - 1;
            return combo;
        }

        /// <summary>
        /// 创建模组选择下拉框
        /// </summary>
        public static ComboBox CreateModuleCombo(int defaultValue = 1)
        {
            var combo = CreateStyledComboBox();
            combo.Width = 150;
            combo.Items.AddRange(new object[] {
                "1 - 管壳检测",
                "2 - 锗窗检测",
                "3 - 出料模组"
            });
            combo.SelectedIndex = defaultValue - 1;
            return combo;
        }

        /// <summary>
        /// 获取下拉框选中的值（索引+1）
        /// </summary>
        public static int GetComboValue(ComboBox combo)
        {
            return combo.SelectedIndex + 1;
        }

        #endregion

        #region 端口选择器

        /// <summary>
        /// 创建端口选择下拉框
        /// </summary>
        public static ComboBox CreatePortSelector(int[] ports, int defaultIndex = 3)
        {
            var cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", UIConfig.Font.Normal),
                Width = UIConfig.PortSelector.Width,
                BackColor = UIConfig.Colors.BgSecondary,
                ForeColor = UIConfig.Colors.TextPrimary,
                FlatStyle = FlatStyle.Flat
            };

            foreach (var port in ports)
            {
                cmb.Items.Add(port.ToString());
            }

            if (defaultIndex >= 0 && defaultIndex < ports.Length)
            {
                cmb.SelectedIndex = defaultIndex;
            }

            return cmb;
        }

        /// <summary>
        /// 创建端口选择器面板
        /// </summary>
        public static Panel CreatePortSelectorPanel(ComboBox cmbPort)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = UIConfig.PortSelector.PanelHeight,
                Padding = new Padding(10, 5, 10, 5),
                BackColor = UIConfig.Colors.PortSelectorBg
            };

            var lblPort = new Label
            {
                Text = "选择端口:",
                Location = new Point(10, 8),
                Size = new Size(80, 20),
                Font = new Font("微软雅黑", UIConfig.Font.Normal, FontStyle.Bold),
                ForeColor = UIConfig.Colors.TextPrimary,
                BackColor = Color.Transparent
            };

            cmbPort.Location = new Point(95, 5);
            cmbPort.Width = UIConfig.PortSelector.Width;
            cmbPort.BackColor = UIConfig.Colors.BgSecondary;
            cmbPort.ForeColor = UIConfig.Colors.TextPrimary;
            cmbPort.FlatStyle = FlatStyle.Flat;

            panel.Controls.Add(lblPort);
            panel.Controls.Add(cmbPort);

            return panel;
        }

        #endregion
    }
}
