using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace 通讯协议测试
{
    /// <summary>
    /// AOI服务类 - 负责AOI服务器的连接和通信
    /// </summary>
    public class AOIService
    {
        private TcpClient client;
        private NetworkStream stream;
        private string serverIP = "127.0.0.1";
        private int serverPort = 8888;

        /// <summary>
        /// 日志事件
        /// </summary>
        public event Action<string, bool> OnLog;

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        public event Action<bool> OnConnectionChanged;

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => client != null && client.Connected;

        /// <summary>
        /// 获取当前服务器地址
        /// </summary>
        public string GetServerAddress() => $"{serverIP}:{serverPort}";

        /// <summary>
        /// 连接到AOI服务器
        /// </summary>
        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                serverIP = ip;
                serverPort = port;

                OnLog?.Invoke($"正在连接AOI服务器: {ip}:{port}", false);

                client = new TcpClient();
                await client.ConnectAsync(ip, port);
                stream = client.GetStream();

                OnLog?.Invoke($"AOI连接成功: {ip}:{port}", false);
                OnConnectionChanged?.Invoke(true);
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"AOI连接失败: {ex.Message}", true);
                client = null;
                stream = null;
                OnConnectionChanged?.Invoke(false);
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            try
            {
                client?.Close();
                client = null;
                stream = null;

                OnLog?.Invoke("AOI连接已断开", false);
                OnConnectionChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"AOI断开异常: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 发送Capture命令
        /// </summary>
        public async Task<AOIResponse> SendCaptureAsync(string station, string x, string y)
        {
            return await SendCommandAsync("Capture", station, x, y);
        }

        /// <summary>
        /// 发送Detect命令
        /// </summary>
        public async Task<AOIResponse> SendDetectAsync(string station, string x, string y)
        {
            return await SendCommandAsync("Detect", station, x, y);
        }

        /// <summary>
        /// 发送AOI命令的通用方法
        /// </summary>
        private async Task<AOIResponse> SendCommandAsync(string command, string station, string x, string y)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("未连接到AOI服务器");
            }

            try
            {
                // 构建请求命令: REQ,命令名称,目标工位,拍照坐标X,拍照坐标Y,触发参数1,触发参数2#
                string request = $"REQ,{command},{station},{x},{y}#";

                OnLog?.Invoke($"发送AOI命令: {request}", false);

                // 发送命令
                byte[] sendData = Encoding.UTF8.GetBytes(request);
                await stream.WriteAsync(sendData, 0, sendData.Length);

                // 接收回复
                byte[] buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                OnLog?.Invoke($"收到AOI回复: {response}", false);

                // 解析回复
                return ParseResponse(response);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"AOI命令执行异常: {ex.Message}", true);
                throw;
            }
        }

        /// <summary>
        /// 解析AOI回复
        /// </summary>
        private AOIResponse ParseResponse(string response)
        {
            try
            {
                // 移除结束符#
                response = response.TrimEnd('#');

                // 分割字段: RLY,命令名称,目标工位,OK/NG,ErrMsg,回复参数1,回复参数2
                string[] parts = response.Split(',');

                if (parts.Length < 4 || parts[0] != "RLY")
                {
                    return new AOIResponse
                    {
                        Success = false,
                        ErrorMessage = $"无效的回复格式: {response}"
                    };
                }

                return new AOIResponse
                {
                    Command = parts[1],
                    Station = parts[2],
                    Status = parts[3],
                    Success = parts[3] == "OK",
                    ErrorMessage = parts.Length > 4 ? parts[4] : "",
                    Parameter1 = parts.Length > 5 ? parts[5] : "",
                    Parameter2 = parts.Length > 6 ? parts[6] : ""
                };
            }
            catch (Exception ex)
            {
                return new AOIResponse
                {
                    Success = false,
                    ErrorMessage = $"解析回复失败: {ex.Message}\n原始数据: {response}"
                };
            }
        }
    }

    /// <summary>
    /// AOI响应数据类
    /// </summary>
    public class AOIResponse
    {
        public string Command { get; set; }
        public string Station { get; set; }
        public string Status { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Parameter1 { get; set; }  // 通常是图片路径
        public string Parameter2 { get; set; }

        public override string ToString()
        {
            var result = $"命令: {Command}\n";
            result += $"工位: {Station}\n";
            result += $"状态: {Status}\n";

            if (Success)
            {
                if (!string.IsNullOrEmpty(Parameter1))
                    result += $"图片路径: {Parameter1}\n";
                if (!string.IsNullOrEmpty(Parameter2))
                    result += $"附加信息: {Parameter2}\n";
            }
            else
            {
                result += $"错误信息: {ErrorMessage}\n";
            }

            return result;
        }
    }
}
