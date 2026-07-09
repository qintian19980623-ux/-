using System.Drawing;

namespace 通讯协议测试
{
    /// <summary>
    /// UI配置类 - 集中管理所有UI相关的常量配置
    /// </summary>
    public static class UIConfig
    {
        #region 窗体尺寸配置

        /// <summary>
        /// 窗体尺寸配置
        /// </summary>
        public static class Form
        {
            public const int Width = 1400;
            public const int Height = 900;
            public const int MinWidth = 1200;
            public const int MinHeight = 800;
        }

        /// <summary>
        /// 布局区域高度配置
        /// </summary>
        public static class Layout
        {
            public const float ConnectionPanelHeight = 90F;
            public const float LogPanelHeight = 220F;
            public const int TabPaddingTop = 45;
            public const int TabPaddingSide = 20;
        }

        #endregion

        #region 控件尺寸配置

        /// <summary>
        /// 连接区域尺寸配置
        /// </summary>
        public static class Connection
        {
            public const int GroupWidth = 500;
            public const int GroupHeight = 75;
            public const int PortButtonWidth = 80;
            public const int PortButtonHeight = 40;
            public const int PortButtonSpacing = 90;
        }

        /// <summary>
        /// 端口选择器尺寸配置
        /// </summary>
        public static class PortSelector
        {
            public const int PanelHeight = 35;
            public const int Width = 100;
        }

        /// <summary>
        /// 列宽度配置
        /// </summary>
        public static class Column
        {
            public const float LabelWidth = 150F;
            public const float LabelWidthNarrow = 120F;
            public const float LabelWidthWide = 140F;
            public const float InputWidth = 220F;
            public const float InputWidthNarrow = 200F;
            public const float InputWidthWide = 250F;
            public const int InputBoxWidth = 300;
        }

        /// <summary>
        /// 行高配置
        /// </summary>
        public static class Row
        {
            public const float HeightSmall = 35F;
            public const float HeightNormal = 40F;
            public const float HeightLarge = 45F;
            public const float HeightButton = 50F;
            public const float HeightSpacing = 10F;
        }

        /// <summary>
        /// 按钮尺寸配置
        /// </summary>
        public static class Button
        {
            public const int Width = 150;
            public const int Height = 35;
            public const int WidthWide = 140;
            public const int WidthExtraWide = 160;
        }

        #endregion

        #region 字体配置

        /// <summary>
        /// 字体大小配置
        /// </summary>
        public static class Font
        {
            public const float Small = 8.5F;
            public const float Normal = 9F;
            public const float Large = 10F;
            public const float Console = 9F;
        }

        #endregion

        #region 颜色主题配置

        /// <summary>
        /// 颜色主题配置 - 科技风深色主题
        /// </summary>
        public static class Colors
        {
            // 背景色
            public static readonly Color BgMain = Color.FromArgb(18, 22, 30);           // 主背景：深邃太空灰
            public static readonly Color BgSecondary = Color.FromArgb(25, 30, 40);      // 次级背景：深灰蓝
            public static readonly Color BgPanel = Color.FromArgb(30, 36, 48);          // 面板背景

            // 按钮色
            public static readonly Color ButtonPrimary = Color.FromArgb(0, 174, 255);   // 科技蓝：主按钮
            public static readonly Color ButtonSecondary = Color.FromArgb(0, 229, 255); // 科技青：次要元素
            public static readonly Color ButtonSuccess = Color.FromArgb(0, 255, 159);   // 霓虹绿：已连接状态
            public static readonly Color ButtonError = Color.FromArgb(255, 68, 119);    // 霓虹红：未连接/错误
            public static readonly Color ButtonDefault = Color.FromArgb(50, 55, 65);    // 深灰：未连接按钮

            // 文字色
            public static readonly Color TextPrimary = Color.FromArgb(240, 245, 255);   // 主文字
            public static readonly Color TextSecondary = Color.FromArgb(160, 170, 190); // 次要文字

            // 日志区域
            public static readonly Color LogBg = Color.FromArgb(10, 15, 20);            // 日志背景：极深背景
            public static readonly Color LogText = Color.FromArgb(0, 255, 180);         // 日志文字：终端风格绿

            // 面板色（快捷方式）
            public static readonly Color PanelBg = BgSecondary;
            public static readonly Color PortSelectorBg = BgPanel;
        }

        #endregion
    }
}
