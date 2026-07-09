using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisionMotionProtocolLib;

namespace 通讯协议测试
{
    /// <summary>
    /// 协议服务类 - 负责管理定位协议的连接、断开和通信
    /// </summary>
    public class ProtocolService
    {
        private readonly Dictionary<int, VisionMotionProtocol> protocols = new Dictionary<int, VisionMotionProtocol>();
        private readonly Dictionary<int, System.Threading.Timer> reconnectTimers = new Dictionary<int, System.Threading.Timer>();
        private readonly Dictionary<int, bool> isReconnecting = new Dictionary<int, bool>();
        private const int RECONNECT_INTERVAL = 5000; // 5秒重连一次

        private string serverIP = "127.0.0.1";

        /// <summary>
        /// 日志事件
        /// </summary>
        public event Action<string, bool> OnLog;

        /// <summary>
        /// 连接状态变化事件 (端口, 是否已连接)
        /// </summary>
        public event Action<int, bool> OnConnectionChanged;

        /// <summary>
        /// 设置服务器IP
        /// </summary>
        public void SetServerIP(string ip)
        {
            serverIP = ip;
        }

        /// <summary>
        /// 获取协议实例
        /// </summary>
        public VisionMotionProtocol GetProtocol(int port)
        {
            return protocols.ContainsKey(port) && protocols[port].IsConnected
                ? protocols[port]
                : null;
        }

        /// <summary>
        /// 检查端口是否已连接
        /// </summary>
        public bool IsConnected(int port)
        {
            return protocols.ContainsKey(port) && protocols[port].IsConnected;
        }

        /// <summary>
        /// 获取所有已连接的端口
        /// </summary>
        public IEnumerable<int> GetConnectedPorts()
        {
            return protocols.Where(p => p.Value.IsConnected).Select(p => p.Key).OrderBy(p => p);
        }

        /// <summary>
        /// 连接到指定端口
        /// </summary>
        public async Task<bool> ConnectAsync(int port)
        {
            try
            {
                OnLog?.Invoke($"端口 {port} 连接中...", false);

                var protocol = new VisionMotionProtocol();

                // 订阅日志事件
                protocol.OnLog += (msg) => OnLog?.Invoke($"[{port}] {msg}", false);
                protocol.OnError += (msg) => OnLog?.Invoke($"[{port}] {msg}", true);

                bool connected = await protocol.ConnectAsync(serverIP, port, 5000);

                if (connected)
                {
                    protocols[port] = protocol;
                    OnLog?.Invoke($"端口 {port} 连接成功", false);
                    OnConnectionChanged?.Invoke(port, true);

                    // 停止该端口的重连定时器
                    StopReconnectTimer(port);
                    return true;
                }
                else
                {
                    OnLog?.Invoke($"端口 {port} 连接失败", true);
                    OnConnectionChanged?.Invoke(port, false);

                    // 启动自动重连
                    StartReconnectTimer(port);
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"端口 {port} 连接异常: {ex.Message}", true);
                OnConnectionChanged?.Invoke(port, false);

                // 启动自动重连
                StartReconnectTimer(port);
                return false;
            }
        }

        /// <summary>
        /// 断开指定端口的连接
        /// </summary>
        public async Task<bool> DisconnectAsync(int port)
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

                OnLog?.Invoke($"端口 {port} 已断开", false);
                OnConnectionChanged?.Invoke(port, false);
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"端口 {port} 断开异常: {ex.Message}", true);
                return false;
            }
        }

        /// <summary>
        /// 自动连接所有端口
        /// </summary>
        public async Task AutoConnectAllAsync(int[] ports)
        {
            OnLog?.Invoke("正在自动连接所有端口...", false);
            foreach (int port in ports)
            {
                await ConnectAsync(port);
                await Task.Delay(500); // 间隔500ms，避免同时连接
            }
        }

        /// <summary>
        /// 断开所有连接
        /// </summary>
        public void DisconnectAll()
        {
            var connectedPorts = protocols.Keys.ToArray();
            foreach (var port in connectedPorts)
            {
                StopReconnectTimer(port);
                if (protocols.ContainsKey(port))
                {
                    protocols[port]?.Dispose();
                    protocols.Remove(port);
                }
            }
        }

        #region 自动重连逻辑

        /// <summary>
        /// 启动自动重连定时器
        /// </summary>
        private void StartReconnectTimer(int port)
        {
            if (isReconnecting.ContainsKey(port) && isReconnecting[port])
                return; // 已经在重连中

            isReconnecting[port] = true;

            var timer = new System.Threading.Timer(async (state) =>
            {
                if (!protocols.ContainsKey(port) || !protocols[port].IsConnected)
                {
                    OnLog?.Invoke($"端口 {port} 尝试自动重连...", false);
                    await ConnectPortSilently(port);
                }
            }, null, RECONNECT_INTERVAL, RECONNECT_INTERVAL);

            reconnectTimers[port] = timer;
        }

        /// <summary>
        /// 停止自动重连定时器
        /// </summary>
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

        /// <summary>
        /// 静默连接（用于自动重连）
        /// </summary>
        private async Task ConnectPortSilently(int port)
        {
            try
            {
                var protocol = new VisionMotionProtocol();

                // 订阅日志事件
                protocol.OnLog += (msg) => OnLog?.Invoke($"[{port}] {msg}", false);
                protocol.OnError += (msg) => OnLog?.Invoke($"[{port}] {msg}", true);

                bool connected = await protocol.ConnectAsync(serverIP, port, 3000);

                if (connected)
                {
                    protocols[port] = protocol;
                    OnLog?.Invoke($"端口 {port} 重连成功", false);
                    OnConnectionChanged?.Invoke(port, true);

                    // 停止重连定时器
                    StopReconnectTimer(port);
                }
            }
            catch (Exception)
            {
                // 重连失败不记录详细错误，避免日志刷屏
            }
        }

        #endregion
    }
}
