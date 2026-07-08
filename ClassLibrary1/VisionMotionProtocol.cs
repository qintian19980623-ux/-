using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    /// <summary>
    /// 视觉运动控制通讯协议类
    /// 用于与视觉系统进行TCP Socket通讯，支持T1-T8八种协议命令
    /// </summary>
    public class VisionMotionProtocol : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected;
        private readonly object _lockObject = new object();

        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        public bool IsConnected => _isConnected && _client?.Connected == true;

        /// <summary>
        /// 日志事件，用于接收操作日志信息
        /// </summary>
        public event Action<string> OnLog;

        /// <summary>
        /// 错误事件，用于接收错误信息
        /// </summary>
        public event Action<string> OnError;

        #region 连接管理

        /// <summary>
        /// 异步连接到视觉服务器
        /// </summary>
        /// <param name="ip">服务器IP地址，默认127.0.0.1</param>
        /// <param name="port">服务器端口，默认7950</param>
        /// <param name="timeout">连接超时时间(毫秒)，默认5000</param>
        /// <returns>连接成功返回true，失败返回false</returns>
        public async Task<bool> ConnectAsync(string ip = "127.0.0.1", int port = 7950, int timeout = 5000)
        {
            try
            {
                Disconnect();
                _client = new TcpClient();
                var connectTask = _client.ConnectAsync(ip, port);

                if (await Task.WhenAny(connectTask, Task.Delay(timeout)) == connectTask)
                {
                    if (_client.Connected)
                    {
                        _stream = _client.GetStream();
                        _isConnected = true;
                        Log($"成功连接到 {ip}:{port}");
                        return true;
                    }
                }
                else
                {
                    Error($"连接超时: {ip}:{port}");
                }
            }
            catch (Exception ex)
            {
                Error($"连接失败: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// 断开与视觉服务器的连接
        /// </summary>
        public void Disconnect()
        {
            lock (_lockObject)
            {
                try
                {
                    _stream?.Close();
                    _client?.Close();
                    _isConnected = false;
                    Log("连接已断开");
                }
                catch (Exception ex)
                {
                    Error($"断开连接时出错: {ex.Message}");
                }
            }
        }

        #endregion

        #region T1 上下相机映射

        /// <summary>
        /// 发送T1协议命令：上下相机映射
        /// 用于建立上下相机之间的坐标映射关系
        /// </summary>
        /// <param name="currentPosition">当前位置坐标，格式："X,Y,R"</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        public async Task<bool> SendT1Async(string currentPosition)
        {
            string command = $"T1;{currentPosition}";
            return await SendCommandAsync(command);
        }

        /// <summary>
        /// 接收T1协议响应
        /// </summary>
        /// <param name="timeout">接收超时时间(毫秒)，默认10000</param>
        /// <returns>T1响应对象，包含映射后的坐标</returns>
        public async Task<T1Response> ReceiveT1Async(int timeout = 10000)
        {
            string response = await ReceiveResponseAsync(timeout);
            return ParseT1Response(response);
        }

        private T1Response ParseT1Response(string response)
        {
            var result = new T1Response { RawData = response };
            try
            {
                if (string.IsNullOrEmpty(response))
                {
                    result.Success = false;
                    Error("T1响应数据为空");
                    return result;
                }

                var parts = response.Split(';');
                Log($"T1解析: 收到 {parts.Length} 个字段");
                
                if (parts.Length >= 4 && parts[0].Trim().ToUpper() == "T1")
                {
                    string okng = parts[1].Trim().ToUpper();
                    int resultCode = int.Parse(parts[2].Trim());
                    result.ResultCode = resultCode;
                    result.Success = (okng == "OK" && resultCode == 0);
                    
                    Log($"T1解析: OK/NG='{okng}', ResultCode={resultCode}, Success={result.Success}");

                    var coords = parts[3].Split(',');
                    if (coords.Length == 3)
                    {
                        result.X = double.Parse(coords[0].Trim());
                        result.Y = double.Parse(coords[1].Trim());
                        result.R = double.Parse(coords[2].Trim());
                    }
                }
                else
                {
                    result.Success = false;
                    Error($"T1响应格式错误: 期望 'T1;OK;0;X,Y,R'，实际收到 '{response}'");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                Error($"T1解析失败: {ex.Message}, 原始数据: '{response}'");
            }
            return result;
        }

        #endregion

        #region T2 九点标定

        /// <summary>
        /// 发送T2协议命令：九点标定
        /// 用于相机坐标系与机械坐标系的九点标定
        /// </summary>
        /// <param name="cameraNo">相机编号</param>
        /// <param name="moduleNo">模组编号</param>
        /// <param name="gripperNo">吸嘴/夹爪编号</param>
        /// <param name="pointNo">标定点编号(1-9)</param>
        /// <param name="controlFlag">控制标志</param>
        /// <param name="moveStep">移动步长</param>
        /// <param name="currentPosition">当前位置坐标，格式："X,Y,R"</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        public async Task<bool> SendT2Async(int cameraNo, int moduleNo, int gripperNo, int pointNo,
            int controlFlag, double moveStep, string currentPosition)
        {
            string command = $"T2;{cameraNo};{moduleNo};{gripperNo};{pointNo};{controlFlag};{moveStep};{currentPosition}";
            return await SendCommandAsync(command);
        }

        /// <summary>
        /// 接收T2协议响应
        /// </summary>
        /// <param name="timeout">接收超时时间(毫秒)，默认10000</param>
        /// <returns>T2响应对象，包含标定结果</returns>
        public async Task<T2Response> ReceiveT2Async(int timeout = 10000)
        {
            string response = await ReceiveResponseAsync(timeout);
            return ParseT2Response(response);
        }

        private T2Response ParseT2Response(string response)
        {
            var result = new T2Response { RawData = response };
            try
            {
                if (string.IsNullOrEmpty(response))
                {
                    result.Success = false;
                    Error("T2响应数据为空");
                    return result;
                }

                var parts = response.Split(';');
                if (parts.Length >= 3 && parts[0].Trim().ToUpper() == "T2")
                {
                    string okng = parts[1].Trim().ToUpper();
                    int resultCode = int.Parse(parts[2].Trim());
                    result.ResultCode = resultCode;
                    result.Success = (okng == "OK" && resultCode == 0);
                    Log($"T2解析: OK/NG='{okng}', ResultCode={resultCode}, Success={result.Success}");
                }
                else
                {
                    result.Success = false;
                    Error($"T2响应格式错误: '{response}'");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                Error($"T2解析失败: {ex.Message}, 原始数据: '{response}'");
            }
            return result;
        }

        #endregion

        #region T3 吸嘴夹爪标定

        /// <summary>
        /// 发送T3协议命令：吸嘴夹爪标定
        /// 用于标定吸嘴或夹爪与相机之间的位置关系
        /// </summary>
        /// <param name="cameraNo">相机编号</param>
        /// <param name="gripperNo">吸嘴/夹爪编号</param>
        /// <param name="currentPosition">当前位置坐标，格式："X,Y,R"</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        public async Task<bool> SendT3Async(int cameraNo, int gripperNo, string currentPosition)
        {
            string command = $"T3;{cameraNo};{gripperNo};{currentPosition}";
            return await SendCommandAsync(command);
        }

        /// <summary>
        /// 接收T3协议响应
        /// </summary>
        /// <param name="timeout">接收超时时间(毫秒)，默认10000</param>
        /// <returns>T3响应对象，包含标定后的坐标</returns>
        public async Task<T3Response> ReceiveT3Async(int timeout = 10000)
        {
            string response = await ReceiveResponseAsync(timeout);
            return ParseT3Response(response);
        }

        private T3Response ParseT3Response(string response)
        {
            var result = new T3Response { RawData = response };
            try
            {
                if (string.IsNullOrEmpty(response))
                {
                    result.Success = false;
                    Error("T3响应数据为空");
                    return result;
                }

                var parts = response.Split(';');
                if (parts.Length >= 4 && parts[0].Trim().ToUpper() == "T3")
                {
                    string okng = parts[1].Trim().ToUpper();
                    int resultCode = int.Parse(parts[2].Trim());
                    result.ResultCode = resultCode;
                    result.Success = (okng == "OK" && resultCode == 0);
                    Log($"T3解析: OK/NG='{okng}', ResultCode={resultCode}, Success={result.Success}");

                    var coords = parts[3].Split(',');
                    if (coords.Length == 3)
                    {
                        result.X = double.Parse(coords[0].Trim());
                        result.Y = double.Parse(coords[1].Trim());
                        result.R = double.Parse(coords[2].Trim());
                    }
                }
                else
                {
                    result.Success = false;
                    Error($"T3响应格式错误: '{response}'");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                Error($"T3解析失败: {ex.Message}, 原始数据: '{response}'");
            }
            return result;
        }

        #endregion

        #region T4 管壳模组上下相机对齐

        /// <summary>
        /// 发送T4协议命令：管壳模组上下相机对齐
        /// 用于管壳检测时上下相机的坐标对齐
        /// </summary>
        /// <param name="cameraNo">相机编号</param>
        /// <param name="currentPosition">当前位置坐标，格式："X,Y,R"</param>
        /// <param name="nozzlePickPosition">吸嘴取料位置坐标，格式："X,Y,R"</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        public async Task<bool> SendT4Async(int cameraNo, string currentPosition, string nozzlePickPosition)
        {
            string command = $"T4;{cameraNo};{currentPosition};{nozzlePickPosition}";
            return await SendCommandAsync(command);
        }

        /// <summary>
        /// 接收T4协议响应
        /// </summary>
        /// <param name="timeout">接收超时时间(毫秒)，默认10000</param>
        /// <returns>T4响应对象，包含对齐后的坐标</returns>
        public async Task<T4Response> ReceiveT4Async(int timeout = 10000)
        {
            string response = await ReceiveResponseAsync(timeout);
            return ParseT4Response(response);
        }

        private T4Response ParseT4Response(string response)
        {
            var result = new T4Response { RawData = response };
            try
            {
                if (string.IsNullOrEmpty(response))
                {
                    result.Success = false;
                    Error("T4响应数据为空");
                    return result;
                }

                var parts = response.Split(';');
                if (parts.Length >= 4 && parts[0].Trim().ToUpper() == "T4")
                {
                    string okng = parts[1].Trim().ToUpper();
                    int resultCode = int.Parse(parts[2].Trim());
                    result.ResultCode = resultCode;
                    result.Success = (okng == "OK" && resultCode == 0);
                    Log($"T4解析: OK/NG='{okng}', ResultCode={resultCode}, Success={result.Success}");

                    var coords = parts[3].Split(',');
                    if (coords.Length == 3)
                    {
                        result.X = double.Parse(coords[0].Trim());
                        result.Y = double.Parse(coords[1].Trim());
                        result.R = double.Parse(coords[2].Trim());
                    }
                }
                else
                {
                    result.Success = false;
                    Error($"T4响应格式错误: '{response}'");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                Error($"T4解析失败: {ex.Message}, 原始数据: '{response}'");
            }
            return result;
        }

        #endregion
        #region T5 锗窗检测

        /// <summary>
        /// 发送T5协议命令：管壳检测模组自动引导
        /// 用于管壳检测时的自动引导定位
        /// </summary>
        /// <param name="cameraNo">相机编号</param>
        /// <param name="nozzleNo">吸嘴编号</param>
        /// <param name="pickPlaceFlag">取放料标志：1-取料，2-放料</param>
        /// <param name="photoPosition1">第一次拍照位置坐标，格式："X,Y,R"</param>
        /// <param name="photoPosition2">第二次拍照位置坐标，格式："X,Y,R"，如果只拍一次则发送"999,999,999"</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        public async Task<bool> SendT5Async(int cameraNo, int nozzleNo, int pickPlaceFlag, string photoPosition1, string photoPosition2)
        {
            string command = $"T5;{cameraNo};{nozzleNo};{pickPlaceFlag};{photoPosition1};{photoPosition2}";
            return await SendCommandAsync(command);
        }

        /// <summary>
        /// 接收T5协议响应
        /// </summary>
        /// <param name="timeout">接收超时时间(毫秒)，默认10000</param>
        /// <returns>T5响应对象，包含引导后的目标坐标</returns>
        public async Task<T5Response> ReceiveT5Async(int timeout = 10000)
        {
            string response = await ReceiveResponseAsync(timeout);
            return ParseT5Response(response);
        }

        private T5Response ParseT5Response(string response)
        {
            var result = new T5Response { RawData = response };
            try
            {
                if (string.IsNullOrEmpty(response))
                {
                    result.Success = false;
                    Error("T5响应数据为空");
                    return result;
                }

                var parts = response.Split(';');
                if (parts.Length >= 4 && parts[0].Trim().ToUpper() == "T5")
                {
                    string okng = parts[1].Trim().ToUpper();
                    int resultCode = int.Parse(parts[2].Trim());
                    result.ResultCode = resultCode;
                    result.Success = (okng == "OK" && resultCode == 0);
                    Log($"T5解析: OK/NG='{okng}', ResultCode={resultCode}, Success={result.Success}");

                    var coords = parts[3].Split(',');
                    if (coords.Length == 3)
                    {
                        result.X = double.Parse(coords[0].Trim());
                        result.Y = double.Parse(coords[1].Trim());
                        result.R = double.Parse(coords[2].Trim());
                    }
                }
                else
                {
                    result.Success = false;
                    Error($"T5响应格式错误: '{response}'");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                Error($"T5解析失败: {ex.Message}, 原始数据: '{response}'");
            }
            return result;
        }

        #endregion

        #region T6 取料对位

        /// <summary>
        /// 发送T6协议命令：锗窗检测模组自动引导
        /// 用于锗窗检测时的取料和放料自动引导
        /// </summary>
        /// <param name="cameraNo">相机编号</param>
        /// <param name="nozzleNo">吸嘴编号(1=18mm, 2=23mm, 3=配重块, 4=夹爪)</param>
        /// <param name="pickPlaceFlag">取放料标志(1=取料, 2=放料)</param>
        /// <param name="photoPosition1">第一次拍照位置坐标，格式："X,Y,R"</param>
        /// <param name="photoPosition2">第二次拍照位置坐标，格式："X,Y,R"，如果只拍一次则发送"999,999,999"</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        public async Task<bool> SendT6Async(int cameraNo, int nozzleNo, int pickPlaceFlag, string photoPosition1, string photoPosition2)
        {
            string command = $"T6;{cameraNo};{nozzleNo};{pickPlaceFlag};{photoPosition1};{photoPosition2}";
            return await SendCommandAsync(command);
        }

        /// <summary>
        /// 接收T6协议响应
        /// </summary>
        /// <param name="timeout">接收超时时间(毫秒)，默认10000</param>
        /// <returns>T6响应对象，包含取放料后的目标坐标</returns>
        public async Task<T6Response> ReceiveT6Async(int timeout = 10000)
        {
            string response = await ReceiveResponseAsync(timeout);
            return ParseT6Response(response);
        }

        private T6Response ParseT6Response(string response)
        {
            var result = new T6Response { RawData = response };
            try
            {
                if (string.IsNullOrEmpty(response))
                {
                    result.Success = false;
                    Error("T6响应数据为空");
                    return result;
                }

                var parts = response.Split(';');
                if (parts.Length >= 4 && parts[0].Trim().ToUpper() == "T6")
                {
                    string okng = parts[1].Trim().ToUpper();
                    int resultCode = int.Parse(parts[2].Trim());
                    result.ResultCode = resultCode;
                    result.Success = (okng == "OK" && resultCode == 0);
                    Log($"T6解析: OK/NG='{okng}', ResultCode={resultCode}, Success={result.Success}");

                    var coords = parts[3].Split(',');
                    if (coords.Length == 3)
                    {
                        result.X = double.Parse(coords[0].Trim());
                        result.Y = double.Parse(coords[1].Trim());
                        result.R = double.Parse(coords[2].Trim());
                    }
                }
                else
                {
                    result.Success = false;
                    Error($"T6响应格式错误: '{response}'");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                Error($"T6解析失败: {ex.Message}, 原始数据: '{response}'");
            }
            return result;
        }

        #endregion
        
        #region T7 出料载具

        /// <summary>
        /// 发送T7协议命令：出料载具定位和示教
        /// 用于出料载具的定位功能和示教功能
        /// </summary>
        /// <param name="carrierNo">载具编号</param>
        /// <param name="functionNo">功能编号(1=定位, 2=示教)</param>
        /// <param name="photoPosition1">第一次拍照位置坐标，格式："X,Y,R"</param>
        /// <param name="photoPosition2">第二次拍照位置坐标，格式："X,Y,R"</param>
        /// <param name="pickPosition1">取料位置坐标，格式："X,Y,R"，定位功能时发送"999,999,999"</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        public async Task<bool> SendT7Async(int carrierNo, int functionNo, string photoPosition1, string photoPosition2, string pickPosition1)
        {
            string command = $"T7;{carrierNo};{functionNo};{photoPosition1};{photoPosition2};{pickPosition1}";
            return await SendCommandAsync(command);
        }

        /// <summary>
        /// 接收T7协议响应
        /// </summary>
        /// <param name="timeout">接收超时时间(毫秒)，默认10000</param>
        /// <returns>T7响应对象，定位功能返回载具所有穴位坐标，示教功能返回999</returns>
        public async Task<T7Response> ReceiveT7Async(int timeout = 10000)
        {
            string response = await ReceiveResponseAsync(timeout);
            return ParseT7Response(response);
        }

        private T7Response ParseT7Response(string response)
        {
            var result = new T7Response { RawData = response };
            try
            {
                if (string.IsNullOrEmpty(response))
                {
                    result.Success = false;
                    Error("T7响应数据为空");
                    return result;
                }

                var parts = response.Split(';');
                Log($"T7解析: 收到 {parts.Length} 个字段");

                if (parts.Length >= 6 && parts[0].Trim().ToUpper() == "T7")
                {
                    string okng = parts[1].Trim().ToUpper();
                    int resultCode = int.Parse(parts[2].Trim());
                    result.ResultCode = resultCode;
                    result.Success = (okng == "OK" && resultCode == 0);
                    Log($"T7解析: OK/NG='{okng}', ResultCode={resultCode}, Success={result.Success}");

                    // 第4个字段：X坐标数组（逗号分隔）
                    var xCoords = parts[3].Split(',');
                    var xList = new List<double>();
                    foreach (var x in xCoords)
                    {
                        if (double.TryParse(x.Trim(), out double xVal))
                        {
                            xList.Add(xVal);
                        }
                    }
                    result.XCoordinates = xList.ToArray();
                    Log($"T7解析: X坐标数量={result.XCoordinates.Length}");

                    // 第5个字段：Y坐标数组（逗号分隔）
                    var yCoords = parts[4].Split(',');
                    var yList = new List<double>();
                    foreach (var y in yCoords)
                    {
                        if (double.TryParse(y.Trim(), out double yVal))
                        {
                            yList.Add(yVal);
                        }
                    }
                    result.YCoordinates = yList.ToArray();
                    Log($"T7解析: Y坐标数量={result.YCoordinates.Length}");

                    // 第6个字段：R角度（可能是999表示无效值）
                    if (double.TryParse(parts[5].Trim(), out double rVal))
                    {
                        result.R = rVal;
                    }
                    else
                    {
                        result.R = 0;
                    }

                    Log($"T7解析完成: 穴位数量={result.XCoordinates?.Length ?? 0}, R={result.R}");
                }
                else
                {
                    result.Success = false;
                    Error($"T7响应格式错误: 期望至少6个字段，实际收到 {parts.Length} 个字段");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                Error($"T7解析失败: {ex.Message}, 原始数据: '{response}'");
            }
            return result;
        }

        #endregion

        #region T8 出料检测

        /// <summary>
        /// 发送T8协议命令：出料载具穴位对齐图心
        /// 用于出料载具的三个Mark点对齐图心标定
        /// </summary>
        /// <param name="carrierNo">载具编号</param>
        /// <param name="photoPosition1">第一个Mark点拍照位置坐标，格式："X,Y,R"</param>
        /// <param name="photoPosition2">第二个Mark点拍照位置坐标，格式："X,Y,R"</param>
        /// <param name="photoPosition3">第三个Mark点拍照位置坐标，格式："X,Y,R"</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        public async Task<bool> SendT8Async(int carrierNo, string photoPosition1, string photoPosition2, string photoPosition3)
        {
            string command = $"T8;{carrierNo};{photoPosition1};{photoPosition2};{photoPosition3}";
            return await SendCommandAsync(command);
        }

        /// <summary>
        /// 接收T8协议响应
        /// </summary>
        /// <param name="timeout">接收超时时间(毫秒)，默认10000</param>
        /// <returns>T8响应对象，包含三个Mark点的对齐结果坐标</returns>
        public async Task<T8Response> ReceiveT8Async(int timeout = 10000)
        {
            string response = await ReceiveResponseAsync(timeout);
            return ParseT8Response(response);
        }

        private T8Response ParseT8Response(string response)
        {
            var result = new T8Response { RawData = response };
            try
            {
                if (string.IsNullOrEmpty(response))
                {
                    result.Success = false;
                    Error("T8响应数据为空");
                    return result;
                }

                var parts = response.Split(';');
                if (parts.Length >= 4 && parts[0].Trim().ToUpper() == "T8")
                {
                    string okng = parts[1].Trim().ToUpper();
                    int resultCode = int.Parse(parts[2].Trim());
                    result.ResultCode = resultCode;
                    result.Success = (okng == "OK" && resultCode == 0);
                    Log($"T8解析: OK/NG='{okng}', ResultCode={resultCode}, Success={result.Success}");

                    // T8返回三个Mark点的坐标: X1,Y1,X2,Y2,X3,Y3
                    var coords = parts[3].Split(',');
                    if (coords.Length == 6)
                    {
                        result.X1 = double.Parse(coords[0].Trim());
                        result.Y1 = double.Parse(coords[1].Trim());
                        result.X2 = double.Parse(coords[2].Trim());
                        result.Y2 = double.Parse(coords[3].Trim());
                        result.X3 = double.Parse(coords[4].Trim());
                        result.Y3 = double.Parse(coords[5].Trim());
                    }
                }
                else
                {
                    result.Success = false;
                    Error($"T8响应格式错误: '{response}'");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                Error($"T8解析失败: {ex.Message}, 原始数据: '{response}'");
            }
            return result;
        }

        #endregion

        #region 底层通信方法

        /// <summary>
        /// 发送命令到服务器
        /// </summary>
        /// <param name="command">要发送的命令字符串</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        private async Task<bool> SendCommandAsync(string command)
        {
            if (!IsConnected)
            {
                Error("未连接到服务器");
                return false;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(command);
                await _stream.WriteAsync(data, 0, data.Length);
                Log($"发送命令: {command}");
                return true;
            }
            catch (Exception ex)
            {
                Error($"发送命令失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从服务器接收响应数据
        /// </summary>
        /// <param name="timeout">接收超时时间(毫秒)，默认10000</param>
        /// <returns>接收到的响应字符串，失败返回空字符串</returns>
        private async Task<string> ReceiveResponseAsync(int timeout = 10000)
        {
            if (!IsConnected)
            {
                Error("未连接到服务器");
                return string.Empty;
            }

            try
            {
                byte[] buffer = new byte[1024];
                var readTask = _stream.ReadAsync(buffer, 0, buffer.Length);
                var timeoutTask = Task.Delay(timeout);

                if (await Task.WhenAny(readTask, timeoutTask) == readTask)
                {
                    int bytesRead = await readTask;
                    if (bytesRead > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Log($"接收响应: {response}");
                        return response;
                    }
                }
                else
                {
                    Error("接收响应超时");
                }
            }
            catch (Exception ex)
            {
                Error($"接收响应失败: {ex.Message}");
            }
            return string.Empty;
        }

        #endregion

        #region 日志方法

        /// <summary>
        /// 记录日志信息
        /// </summary>
        /// <param name="message">日志消息</param>
        private void Log(string message)
        {
            OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        /// <summary>
        /// 记录错误信息
        /// </summary>
        /// <param name="message">错误消息</param>
        private void Error(string message)
        {
            OnError?.Invoke($"[{DateTime.Now:HH:mm:ss}] 错误: {message}");
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 释放资源，断开连接
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }

        #endregion
    }

    #region 响应类定义

    /// <summary>
    /// T1协议响应类：上下相机映射结果
    /// </summary>
    public class T1Response
    {
        /// <summary>操作是否成功(OK且ResultCode=0时为true)</summary>
        public bool Success { get; set; }
        /// <summary>结果码(0=成功, -1=失败, 2=需要重拍)</summary>
        public int ResultCode { get; set; }
        /// <summary>X坐标</summary>
        public double X { get; set; }
        /// <summary>Y坐标</summary>
        public double Y { get; set; }
        /// <summary>R角度</summary>
        public double R { get; set; }
        /// <summary>原始响应数据</summary>
        public string RawData { get; set; }
    }

    /// <summary>
    /// T2协议响应类：九点标定结果
    /// </summary>
    public class T2Response
    {
        /// <summary>操作是否成功(OK且ResultCode=0时为true)</summary>
        public bool Success { get; set; }
        /// <summary>结果码(0=成功, -1=失败, 2=需要重拍)</summary>
        public int ResultCode { get; set; }
        /// <summary>原始响应数据</summary>
        public string RawData { get; set; }
    }

    /// <summary>
    /// T3协议响应类：吸嘴夹爪标定结果
    /// </summary>
    public class T3Response
    {
        /// <summary>操作是否成功(OK且ResultCode=0时为true)</summary>
        public bool Success { get; set; }
        /// <summary>结果码(0=成功, -1=失败, 2=需要重拍)</summary>
        public int ResultCode { get; set; }
        /// <summary>X坐标</summary>
        public double X { get; set; }
        /// <summary>Y坐标</summary>
        public double Y { get; set; }
        /// <summary>R角度</summary>
        public double R { get; set; }
        /// <summary>原始响应数据</summary>
        public string RawData { get; set; }
    }

    /// <summary>
    /// T4协议响应类：管壳模组上下相机对齐结果
    /// </summary>
    public class T4Response
    {
        /// <summary>操作是否成功(OK且ResultCode=0时为true)</summary>
        public bool Success { get; set; }
        /// <summary>结果码(0=成功, -1=失败, 2=需要重拍)</summary>
        public int ResultCode { get; set; }
        /// <summary>X坐标</summary>
        public double X { get; set; }
        /// <summary>Y坐标</summary>
        public double Y { get; set; }
        /// <summary>R角度</summary>
        public double R { get; set; }
        /// <summary>原始响应数据</summary>
        public string RawData { get; set; }
    }

    /// <summary>
    /// T5协议响应类：管壳检测模组自动引导结果
    /// </summary>
    /// <summary>
    /// T5协议响应类：管壳检测模组自动引导结果
    /// </summary>
    public class T5Response
    {
        /// <summary>操作是否成功(OK且ResultCode=0时为true)</summary>
        public bool Success { get; set; }
        /// <summary>结果码(0=成功, -1=失败, 2=需要重拍)</summary>
        public int ResultCode { get; set; }
        /// <summary>X坐标</summary>
        public double X { get; set; }
        /// <summary>Y坐标</summary>
        public double Y { get; set; }
        /// <summary>R角度</summary>
        public double R { get; set; }
        /// <summary>原始响应数据</summary>
        public string RawData { get; set; }
    }

    /// <summary>
    /// T6协议响应类：锗窗检测模组自动引导结果
    /// </summary>
    public class T6Response
    {
        /// <summary>操作是否成功(OK且ResultCode=0时为true)</summary>
        public bool Success { get; set; }
        /// <summary>结果码(0=成功, -1=失败, 2=需要重拍)</summary>
        public int ResultCode { get; set; }
        /// <summary>X坐标</summary>
        public double X { get; set; }
        /// <summary>Y坐标</summary>
        public double Y { get; set; }
        /// <summary>R角度</summary>
        public double R { get; set; }
        /// <summary>原始响应数据</summary>
        public string RawData { get; set; }
    }

    /// <summary>
    /// T7协议响应类：出料载具定位和示教结果
    /// </summary>
    public class T7Response
    {
        /// <summary>操作是否成功(OK且ResultCode=0时为true)</summary>
        public bool Success { get; set; }
        /// <summary>结果码(0=成功, -1=失败, 2=需要重拍)</summary>
        public int ResultCode { get; set; }
        /// <summary>X坐标</summary>
        public double X { get; set; }
        /// <summary>Y坐标</summary>
        public double Y { get; set; }
        /// <summary>R角度(载具旋转角度)</summary>
        public double R { get; set; }
        /// <summary>所有穴位的X坐标数组(定位功能时返回)</summary>
        public double[]? XCoordinates { get; set; }
        /// <summary>所有穴位的Y坐标数组(定位功能时返回)</summary>
        public double[]? YCoordinates { get; set; }
        /// <summary>原始响应数据</summary>
        public string RawData { get; set; }
    }

    /// <summary>
    /// T8协议响应类：出料载具穴位对齐图心结果
    /// </summary>
    public class T8Response
    {
        /// <summary>操作是否成功(OK且ResultCode=0时为true)</summary>
        public bool Success { get; set; }
        /// <summary>结果码(0=成功, -1=失败, 2=需要重拍)</summary>
        public int ResultCode { get; set; }
        /// <summary>第一个Mark点X坐标</summary>
        public double X1 { get; set; }
        /// <summary>第一个Mark点Y坐标</summary>
        public double Y1 { get; set; }
        /// <summary>第二个Mark点X坐标</summary>
        public double X2 { get; set; }
        /// <summary>第二个Mark点Y坐标</summary>
        public double Y2 { get; set; }
        /// <summary>第三个Mark点X坐标</summary>
        public double X3 { get; set; }
        /// <summary>第三个Mark点Y坐标</summary>
        public double Y3 { get; set; }
        /// <summary>原始响应数据</summary>
        public string RawData { get; set; }
    }

    #endregion
}
