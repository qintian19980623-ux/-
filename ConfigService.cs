using System;
using System.IO;
using System.Text.Json;

namespace 通讯协议测试
{
    /// <summary>
    /// 配置服务类 - 负责配置的保存、加载和管理
    /// </summary>
    public class ConfigService
    {
        private readonly string configFilePath;
        private ConnectionConfig currentConfig;

        /// <summary>
        /// 配置加载事件
        /// </summary>
        public event Action<string, bool> OnLog;

        public ConfigService(string configPath = null)
        {
            configFilePath = configPath ?? ConnectionConfig.DefaultConfigPath;
            currentConfig = new ConnectionConfig();
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public ConnectionConfig GetConfig()
        {
            return currentConfig;
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public bool SaveConfig(ConnectionConfig config)
        {
            try
            {
                currentConfig = config;
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                string fullPath = Path.GetFullPath(configFilePath);
                File.WriteAllText(fullPath, json);
                OnLog?.Invoke($"配置保存成功: {fullPath}", false);
                OnLog?.Invoke($"AOI服务器: {config.AoiServerIP}:{config.AoiServerPort}", false);
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"保存配置失败: {ex.Message}", true);
                return false;
            }
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public ConnectionConfig LoadConfig()
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    OnLog?.Invoke("配置文件不存在，使用默认配置", false);
                    return currentConfig;
                }

                string json = File.ReadAllText(configFilePath);
                var config = JsonSerializer.Deserialize<ConnectionConfig>(json);

                if (config != null)
                {
                    currentConfig = config;
                    OnLog?.Invoke("配置加载成功", false);
                    return config;
                }
                else
                {
                    OnLog?.Invoke("配置文件解析失败，使用默认配置", true);
                    return currentConfig;
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"加载配置失败: {ex.Message}", true);
                return currentConfig;
            }
        }

        /// <summary>
        /// 更新Tab端口配置
        /// </summary>
        public void UpdateTabPort(string tabName, int port)
        {
            currentConfig.TabPorts[tabName] = port;
        }

        /// <summary>
        /// 更新AOI服务器配置
        /// </summary>
        public void UpdateAOIServer(string ip, int port)
        {
            currentConfig.AoiServerIP = ip;
            currentConfig.AoiServerPort = port;
        }

        /// <summary>
        /// 获取Tab端口配置
        /// </summary>
        public int GetTabPort(string tabName, int defaultPort = 7950)
        {
            return currentConfig.TabPorts.ContainsKey(tabName)
                ? currentConfig.TabPorts[tabName]
                : defaultPort;
        }
    }
}
