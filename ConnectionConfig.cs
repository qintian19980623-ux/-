using System.Collections.Generic;

namespace 通讯协议测试
{
    /// <summary>
    /// 连接配置类 - 用于序列化保存和加载配置
    /// </summary>
    public class ConnectionConfig
    {
        /// <summary>
        /// 定位协议服务器IP
        /// </summary>
        public string ServerIP { get; set; } = "127.0.0.1";

        /// <summary>
        /// 定位协议端口列表
        /// </summary>
        public int[] Ports { get; set; } = { 7920, 7930, 7940, 7950 };

        /// <summary>
        /// 各Tab页面选择的端口配置
        /// </summary>
        public Dictionary<string, int> TabPorts { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// AOI服务器IP
        /// </summary>
        public string AoiServerIP { get; set; } = "127.0.0.1";

        /// <summary>
        /// AOI服务器端口
        /// </summary>
        public int AoiServerPort { get; set; } = 8888;

        /// <summary>
        /// 配置文件默认路径
        /// </summary>
        public const string DefaultConfigPath = "PortConfig.json";
    }
}
