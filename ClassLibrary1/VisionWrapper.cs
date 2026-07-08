using System;
using System.Threading.Tasks;
using ClassLibrary1;

namespace ClassLibrary1
{
    /// <summary>
    /// 视觉系统简化封装类
    /// 提供更简单的接口供运控程序调用
    /// </summary>
    public class VisionWrapper : IDisposable
    {
        private VisionMotionProtocol _protocol;
        private bool _isConnected = false;

        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        public bool IsConnected => _isConnected && _protocol?.IsConnected == true;

        /// <summary>
        /// 日志事件
        /// </summary>
        public event Action<string> OnLog;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event Action<string> OnError;

        public VisionWrapper()
        {
            _protocol = new VisionMotionProtocol();
            _protocol.OnLog += (msg) => OnLog?.Invoke(msg);
            _protocol.OnError += (msg) => OnError?.Invoke(msg);
        }

        #region 连接管理

        /// <summary>
        /// 连接到视觉服务器
        /// </summary>
        /// <param name="ip">服务器IP，默认127.0.0.1</param>
        /// <param name="port">服务器端口，默认7950</param>
        /// <returns>连接成功返回true</returns>
        public async Task<bool> ConnectAsync(string ip = "127.0.0.1", int port = 7950)
        {
            _isConnected = await _protocol.ConnectAsync(ip, port);
            return _isConnected;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            _protocol.Disconnect();
            _isConnected = false;
        }

        #endregion

        #region T1 - 上下相机映射

        /// <summary>
        /// T1协议：上下相机映射
        /// </summary>
        /// <param name="currentPosition">当前位置，格式："X,Y,R"</param>
        /// <returns>定位结果</returns>
        public async Task<VisionResult> T1_上下相机映射(string currentPosition)
        {
            if (!IsConnected)
                return VisionResult.Failed("未连接到视觉服务器");

            try
            {
                await _protocol.SendT1Async(currentPosition);
                var response = await _protocol.ReceiveT1Async();

                return new VisionResult
                {
                    Success = response.Success,
                    ResultCode = response.ResultCode,
                    X = response.X,
                    Y = response.Y,
                    R = response.R,
                    Message = response.Success ? "成功" : $"失败，结果码:{response.ResultCode}"
                };
            }
            catch (Exception ex)
            {
                return VisionResult.Failed($"T1异常: {ex.Message}");
            }
        }

        #endregion

        #region T2 - 九点标定

        /// <summary>
        /// T2协议：清除标定点
        /// </summary>
        public async Task<VisionResult> T2_清除标定点(int cameraNo, int moduleNo, int gripperNo, double step, string centerPoint)
        {
            if (!IsConnected)
                return VisionResult.Failed("未连接到视觉服务器");

            try
            {
                await _protocol.SendT2Async(cameraNo, moduleNo, gripperNo, 1, 1, step, centerPoint);
                var response = await _protocol.ReceiveT2Async();

                return new VisionResult
                {
                    Success = response.Success,
                    ResultCode = response.ResultCode,
                    Message = response.Success ? "清除标定点成功" : $"失败，结果码:{response.ResultCode}"
                };
            }
            catch (Exception ex)
            {
                return VisionResult.Failed($"T2清除标定点异常: {ex.Message}");
            }
        }

        /// <summary>
        /// T2协议：九点标定
        /// </summary>
        public async Task<VisionResult> T2_九点标定(int cameraNo, int moduleNo, int gripperNo, double step, string centerPoint, bool xReverse = false, bool yReverse = false)
        {
            if (!IsConnected)
                return VisionResult.Failed("未连接到视觉服务器");

            try
            {
                await _protocol.SendT2Async(cameraNo, moduleNo, gripperNo, 1, 2, step, centerPoint);
                var response = await _protocol.ReceiveT2Async();

                return new VisionResult
                {
                    Success = response.Success,
                    ResultCode = response.ResultCode,
                    Message = response.Success ? "九点标定成功" : $"失败，结果码:{response.ResultCode}"
                };
            }
            catch (Exception ex)
            {
                return VisionResult.Failed($"T2九点标定异常: {ex.Message}");
            }
        }

        /// <summary>
        /// T2协议：九点+旋转标定（16次）
        /// </summary>
        public async Task<VisionResult> T2_九点旋转标定(int cameraNo, int moduleNo, int gripperNo, double step, string centerPoint, double rotateAngle)
        {
            if (!IsConnected)
                return VisionResult.Failed("未连接到视觉服务器");

            try
            {
                await _protocol.SendT2Async(cameraNo, moduleNo, gripperNo, 1, 3, step, centerPoint);
                var response = await _protocol.ReceiveT2Async();

                return new VisionResult
                {
                    Success = response.Success,
                    ResultCode = response.ResultCode,
                    Message = response.Success ? "九点旋转标定成功" : $"失败，结果码:{response.ResultCode}"
                };
            }
            catch (Exception ex)
            {
                return VisionResult.Failed($"T2九点旋转标定异常: {ex.Message}");
            }
        }

        /// <summary>
        /// T2协议：结束标定
        /// </summary>
        public async Task<VisionResult> T2_结束标定(int cameraNo, int moduleNo, int gripperNo)
        {
            if (!IsConnected)
                return VisionResult.Failed("未连接到视觉服务器");

            try
            {
                await _protocol.SendT2Async(cameraNo, moduleNo, gripperNo, 1, 4, 0, "0,0,0");
                var response = await _protocol.ReceiveT2Async();

                return new VisionResult
                {
                    Success = response.Success,
                    ResultCode = response.ResultCode,
                    Message = response.Success ? "结束标定成功" : $"失败，结果码:{response.ResultCode}"
                };
            }
            catch (Exception ex)
            {
                return VisionResult.Failed($"T2结束标定异常: {ex.Message}");
            }
        }

        #endregion

        #region T3 - 吸嘴夹爪标定

        /// <summary>
        /// T3协议：吸嘴夹爪标定
        /// </summary>
        /// <param name="cameraNo">相机编号</param>
        /// <param name="gripperNo">夹爪编号</param>
        /// <param name="position">位置，格式："X,Y,R"</param>
        /// <returns>对位结果</returns>
        public async Task<VisionResult> T3_吸嘴夹爪标定(int cameraNo, int gripperNo, string position)
        {
            if (!IsConnected)
                return VisionResult.Failed("未连接到视觉服务器");

            try
            {
                await _protocol.SendT3Async(cameraNo, gripperNo, position);
                var response = await _protocol.ReceiveT3Async();

                return new VisionResult
                {
                    Success = response.Success,
                    ResultCode = response.ResultCode,
                    X = response.X,
                    Y = response.Y,
                    R = response.R,
                    Message = response.Success ? "取料对位成功" : $"失败，结果码:{response.ResultCode}"
                };
            }
            catch (Exception ex)
            {
                return VisionResult.Failed($"T3异常: {ex.Message}");
            }
        }

        #endregion

        #region T4 - 管壳模组上下相机对齐

        /// <summary>
        /// T4协议：管壳模组上下相机对齐
        /// </summary>
        /// <param name="cameraNo">相机编号</param>
        /// <param name="position">位置，格式："X,Y,R"</param>
        /// <param name="nozzleHeight">吸嘴高度，格式："H1,H2,H3,H4"</param>
        /// <returns>对齐结果</returns>
        public async Task<VisionResult> T4_上下相机对齐(int cameraNo, string position, string nozzleHeight)
        {
            if (!IsConnected)
                return VisionResult.Failed("未连接到视觉服务器");

            try
            {
                await _protocol.SendT4Async(cameraNo, position, nozzleHeight);
                var response = await _protocol.ReceiveT4Async();

                return new VisionResult
                {
                    Success = response.Success,
                    ResultCode = response.ResultCode,
                    X = response.X,
                    Y = response.Y,
                    R = response.R,
                    Message = response.Success ? "上下相机对齐成功" : $"失败，结果码:{response.ResultCode}"
                };
            }
            catch (Exception ex)
            {
                return VisionResult.Failed($"T4异常: {ex.Message}");
            }
        }

        #endregion

        #region T5 - 管壳检测模组自动引导

        /// <summary>
        /// T5协议：管壳检测模组自动引导
        /// </summary>
        /// <param name="cameraNo">相机编号</param>
        /// <param name="nozzleNo">吸嘴编号</param>
        /// <param name="pickPlaceFlag">取放料标志：1-取料，2-放料</param>
        /// <param name="photoPosition1">第一次拍照位置，格式："X,Y,R"</param>
        /// <param name="photoPosition2">第二次拍照位置，格式："X,Y,R"，只拍一次时填"999,999,999"</param>
        /// <returns>引导结果</returns>
        public async Task<VisionResult> T5_管壳检测引导(int cameraNo, int nozzleNo, int pickPlaceFlag, string photoPosition1, string photoPosition2 = "999,999,999")
        {
            if (!IsConnected)
                return VisionResult.Failed("未连接到视觉服务器");

            try
            {
                await _protocol.SendT5Async(cameraNo, nozzleNo, pickPlaceFlag, photoPosition1, photoPosition2);
                var response = await _protocol.ReceiveT5Async();

                return new VisionResult
                {
                    Success = response.Success,
                    ResultCode = response.ResultCode,
                    X = response.X,
                    Y = response.Y,
                    R = response.R,
                    Message = response.Success ? "管壳检测引导成功" : $"失败，结果码:{response.ResultCode}"
                };
            }
            catch (Exception ex)
            {
                return VisionResult.Failed($"T5异常: {ex.Message}");
            }
        }

        #endregion

        #region T6 - 锗窗检测模组自动引导

        /// <summary>
        /// T6协议：锗窗检测模组自动引导
        /// </summary>
        /// <param name="cameraNo">相机编号</param>
        /// <param name="nozzleNo">吸嘴编号</param>
        /// <param name="pickPlaceFlag">取放料标志：1-取料，2-放料</param>
        /// <param name="photoPosition1">第一次拍照位置，格式："X,Y,R"</param>
        /// <param name="photoPosition2">第二次拍照位置，格式："X,Y,R"，只拍一次时填"999,999,999"</param>
        /// <returns>引导结果</returns>
        public async Task<VisionResult> T6_锗窗检测引导(int cameraNo, int nozzleNo, int pickPlaceFlag, string photoPosition1, string photoPosition2 = "999,999,999")
        {
            if (!IsConnected)
                return VisionResult.Failed("未连接到视觉服务器");

            try
            {
                await _protocol.SendT6Async(cameraNo, nozzleNo, pickPlaceFlag, photoPosition1, photoPosition2);
                var response = await _protocol.ReceiveT6Async();

                return new VisionResult
                {
                    Success = response.Success,
                    ResultCode = response.ResultCode,
                    X = response.X,
                    Y = response.Y,
                    R = response.R,
                    Message = response.Success ? "锗窗检测引导成功" : $"失败，结果码:{response.ResultCode}"
                };
            }
            catch (Exception ex)
            {
                return VisionResult.Failed($"T6异常: {ex.Message}");
            }
        }

        #endregion

        #region T7 - 出料载具

        /// <summary>
        /// T7协议：出料载具
        /// </summary>
        /// <param name="carrierNo">载具编号</param>
        /// <param name="functionNo">功能编号</param>
        /// <param name="photoPosition1">拍照位置1，格式："X,Y,R"</param>
        /// <param name="photoPosition2">拍照位置2，格式："X,Y,R"</param>
        /// <param name="pickPosition1">取料位置1，格式："X,Y,R"</param>
        /// <returns>定位结果</returns>
        public async Task<VisionResult> T7_出料载具(int carrierNo, int functionNo, string photoPosition1, string photoPosition2, string pickPosition1)
        {
            if (!IsConnected)
                return VisionResult.Failed("未连接到视觉服务器");

            try
            {
                await _protocol.SendT7Async(carrierNo, functionNo, photoPosition1, photoPosition2, pickPosition1);
                var response = await _protocol.ReceiveT7Async();

                return new VisionResult
                {
                    Success = response.Success,
                    ResultCode = response.ResultCode,
                    X = response.X,
                    Y = response.Y,
                    R = response.R,
                    Message = response.Success ? "出料载具定位成功" : $"失败，结果码:{response.ResultCode}"
                };
            }
            catch (Exception ex)
            {
                return VisionResult.Failed($"T7异常: {ex.Message}");
            }
        }

        #endregion

        #region T8 - 出料载具穴位对齐图心

        /// <summary>
        /// T8协议：出料载具穴位对齐图心
        /// </summary>
        /// <param name="carrierNo">载具编号</param>
        /// <param name="photoPosition1">拍照位置1，格式："X,Y,R"</param>
        /// <param name="photoPosition2">拍照位置2，格式："X,Y,R"</param>
        /// <param name="photoPosition3">拍照位置3，格式："X,Y,R"</param>
        /// <returns>定位结果（包含3个Mark点坐标）</returns>
        public async Task<VisionResult> T8_出料载具穴位对齐图心(int carrierNo, string photoPosition1, string photoPosition2, string photoPosition3)
        {
            if (!IsConnected)
                return VisionResult.Failed("未连接到视觉服务器");

            try
            {
                await _protocol.SendT8Async(carrierNo, photoPosition1, photoPosition2, photoPosition3);
                var response = await _protocol.ReceiveT8Async();

                return new VisionResult
                {
                    Success = response.Success,
                    ResultCode = response.ResultCode,
                    X = response.X1,  // 使用第一个Mark点的X坐标
                    Y = response.Y1,  // 使用第一个Mark点的Y坐标
                    R = 0,            // T8没有角度信息
                    Message = response.Success ? $"NG料盒定位成功，Mark1({response.X1},{response.Y1}), Mark2({response.X2},{response.Y2}), Mark3({response.X3},{response.Y3})" : $"失败，结果码:{response.ResultCode}",
                    // 额外信息可以通过Message传递
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return VisionResult.Failed($"T8异常: {ex.Message}");
            }
        }

        #endregion

        public void Dispose()
        {
            _protocol?.Dispose();
        }
    }

    /// <summary>
    /// 视觉操作结果
    /// </summary>
    public class VisionResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 结果码
        /// </summary>
        public int ResultCode { get; set; }

        /// <summary>
        /// X坐标
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y坐标
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// R角度
        /// </summary>
        public double R { get; set; }

        /// <summary>
        /// 结果消息
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// 原始响应对象（用于需要更多信息的场景，如T8的多个Mark点）
        /// </summary>
        public object? RawResponse { get; set; }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static VisionResult Failed(string message)
        {
            return new VisionResult
            {
                Success = false,
                ResultCode = -1,
                Message = message
            };
        }
    }
}
