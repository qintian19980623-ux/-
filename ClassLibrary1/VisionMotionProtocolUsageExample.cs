using System;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    /// <summary>
    /// VisionMotionProtocol 使用示例
    /// 演示如何使用视觉运控通讯协议类与视觉系统交互
    /// 包含T1-T8共8个流程的完整使用示例
    /// </summary>
    public class VisionMotionProtocolUsageExample
    {
        /// <summary>
        /// 主示例方法：演示完整的使用流程
        /// </summary>
        public async Task RunExampleAsync()
        {
            // ====================================
            // 1. 创建协议实例并订阅日志事件
            // ====================================
            using (var protocol = new VisionMotionProtocol())
            {
                // 订阅日志事件，将日志输出到控制台
                protocol.OnLog += (message) =>
                {
                    Console.WriteLine($"[日志] {message}");
                };

                // 订阅错误事件，将错误信息输出到控制台
                protocol.OnError += (message) =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[错误] {message}");
                    Console.ResetColor();
                };

                // ====================================
                // 2. 连接到视觉系统
                // ====================================
                // 连接到管壳检测模组上相机（端口7950）
                bool connected = await protocol.ConnectAsync("127.0.0.1", 7950, 5000);

                if (!connected)
                {
                    Console.WriteLine("连接失败，退出示例");
                    return;
                }

                // ====================================
                // 3. 使用各个协议流程
                // ====================================

                // T1 示例：上下相机映射
                await T1_CameraMappingExample(protocol);

                // T2 示例：九点标定
                await T2_NinePointCalibrationExample(protocol);

                // T3 示例：吸嘴夹爪标定
                await T3_NozzleGripperCalibrationExample(protocol);

                // T4 示例：管壳模组上下相机对齐
                await T4_CameraAlignmentExample(protocol);

                // T5 示例：管壳检测模组自动引导
                await T5_ShellModuleGuideExample(protocol);

                // T6 示例：锗窗检测模组自动引导
                await T6_GeWindowModuleGuideExample(protocol);

                // T7 示例：出料载具
                await T7_UnloadCarrierExample(protocol);

                // T8 示例：出料载具穴位对齐图心
                await T8_CarrierCavityAlignmentExample(protocol);

                // ====================================
                // 4. 断开连接
                // ====================================
                // 使用using语句会自动调用Dispose，断开连接
                // 也可以手动调用：protocol.Disconnect();
            }
        }

        /// <summary>
        /// T1 示例：上下相机映射
        /// 用于上下相机的坐标映射标定
        /// </summary>
        private async Task T1_CameraMappingExample(VisionMotionProtocol protocol)
        {
            Console.WriteLine("\n========== T1: 上下相机映射 ==========");

            // 构造当前拍照位坐标：X=100.5, Y=200.3, R=45.0
            string currentPosition = "100.5,200.3,45.0";

            // 发送T1命令
            bool sendSuccess = await protocol.SendT1Async(currentPosition);
            if (!sendSuccess)
            {
                Console.WriteLine("T1命令发送失败");
                return;
            }

            // 接收T1响应
            var response = await protocol.ReceiveT1Async(10000);

            // 处理响应结果
            if (response.Success)
            {
                Console.WriteLine($"T1执行成功");
                Console.WriteLine($"结果码: {response.ResultCode}");
                Console.WriteLine($"对齐坐标: X={response.X}, Y={response.Y}, R={response.R}");
            }
            else
            {
                Console.WriteLine($"T1执行失败，结果码: {response.ResultCode}");
                if (response.ResultCode == 2)
                {
                    Console.WriteLine("需要检查映射抓边模板并重新拍照");
                }
            }
        }

        /// <summary>
        /// T2 示例：九点标定
        /// 用于相机和机械坐标系的九点标定
        /// </summary>
        private async Task T2_NinePointCalibrationExample(VisionMotionProtocol protocol)
        {
            Console.WriteLine("\n========== T2: 九点标定 ==========");

            // 九点标定的参数
            int cameraNo = 1;           // 相机号：1=管壳上相机
            int moduleNo = 1;           // 模组号：1=管壳模组
            int gripperNo = 1;          // 爪号：1=吸嘴
            double moveStep = 10.0;     // 移动步长：10mm
            string currentPosition = "100.0,200.0,0.0";

            // 步骤1：清除标定点（controlFlag=1）
            Console.WriteLine("步骤1：清除标定点");
            await protocol.SendT2Async(cameraNo, moduleNo, gripperNo, 1, 1, moveStep, currentPosition);
            var clearResponse = await protocol.ReceiveT2Async();
            Console.WriteLine($"清除标定点结果: {(clearResponse.Success ? "成功" : "失败")}");

            // 步骤2：执行9次标定（controlFlag=2，pointNo从1到9）
            for (int pointNo = 1; pointNo <= 9; pointNo++)
            {
                Console.WriteLine($"步骤2-{pointNo}：标定第{pointNo}个点");

                // 根据点位号计算当前拍照位（这里只是示例，实际需要根据机械运动计算）
                double x = 100.0 + (pointNo % 3) * moveStep;
                double y = 200.0 + (pointNo / 3) * moveStep;
                currentPosition = $"{x},{y},0.0";

                await protocol.SendT2Async(cameraNo, moduleNo, gripperNo, pointNo, 2, moveStep, currentPosition);
                var calibResponse = await protocol.ReceiveT2Async();

                if (calibResponse.Success)
                {
                    Console.WriteLine($"第{pointNo}点标定成功");
                }
                else
                {
                    Console.WriteLine($"第{pointNo}点标定失败，结果码: {calibResponse.ResultCode}");
                    break;
                }
            }

            // 步骤3：结束标定（controlFlag=3）
            Console.WriteLine("步骤3：结束标定");
            await protocol.SendT2Async(cameraNo, moduleNo, gripperNo, 9, 3, moveStep, currentPosition);
            var endResponse = await protocol.ReceiveT2Async();
            Console.WriteLine($"九点标定最终结果: {(endResponse.Success ? "成功" : "失败")}");
        }

        /// <summary>
        /// T3 示例：吸嘴夹爪标定
        /// 用于标定吸嘴或夹爪中心与下相机图心的对齐
        /// </summary>
        private async Task T3_NozzleGripperCalibrationExample(VisionMotionProtocol protocol)
        {
            Console.WriteLine("\n========== T3: 吸嘴夹爪标定 ==========");

            int cameraNo = 2;       // 相机号：2=管壳下相机
            int gripperNo = 1;      // 爪号：1=吸嘴，4=夹爪
            string currentPosition = "150.0,250.0,0.0";

            // 发送T3命令
            await protocol.SendT3Async(cameraNo, gripperNo, currentPosition);

            // 接收T3响应
            var response = await protocol.ReceiveT3Async();

            if (response.Success)
            {
                if (response.ResultCode == 0)
                {
                    Console.WriteLine("T3标定成功");
                    Console.WriteLine($"吸嘴中心对齐图心的坐标: X={response.X}, Y={response.Y}, R={response.R}");
                }
                else if (response.ResultCode == 2)
                {
                    Console.WriteLine("Mark没有对齐图心，需要重新拍照");
                    Console.WriteLine($"移动到坐标: X={response.X}, Y={response.Y}, R={response.R}");
                    // 在实际应用中，这里需要控制轴移动到返回的坐标，然后重新发送T3命令
                }
            }
            else
            {
                Console.WriteLine($"T3标定失败，结果码: {response.ResultCode}");
            }
        }

        /// <summary>
        /// T4 示例：管壳模组上下相机对齐
        /// 用于管壳模组的上下相机对齐标定
        /// </summary>
        private async Task T4_CameraAlignmentExample(VisionMotionProtocol protocol)
        {
            Console.WriteLine("\n========== T4: 管壳模组上下相机对齐 ==========");

            // 上相机拍照
            Console.WriteLine("步骤1：上相机拍照");
            int cameraNo = 1;  // 上相机
            string currentPosition = "100.0,200.0,0.0";
            string nozzlePickPosition = "999,999,999";  // 上相机时发送999

            await protocol.SendT4Async(cameraNo, currentPosition, nozzlePickPosition);
            var upResponse = await protocol.ReceiveT4Async();

            if (upResponse.Success && upResponse.ResultCode == 0)
            {
                Console.WriteLine($"上相机拍照成功，Mark坐标: X={upResponse.X}, Y={upResponse.Y}, R={upResponse.R}");

                // 下相机拍照
                Console.WriteLine("步骤2：下相机拍照");
                cameraNo = 2;  // 下相机
                nozzlePickPosition = "110.0,210.0,5.0";  // 吸嘴取标定板的轴坐标

                await protocol.SendT4Async(cameraNo, currentPosition, nozzlePickPosition);
                var downResponse = await protocol.ReceiveT4Async();

                if (downResponse.Success && downResponse.ResultCode == 0)
                {
                    Console.WriteLine($"下相机拍照成功");
                    Console.WriteLine($"Mark对齐图心的坐标: X={downResponse.X}, Y={downResponse.Y}, R={downResponse.R}");
                }
                else if (downResponse.ResultCode == 2)
                {
                    Console.WriteLine("Mark没有对齐图心，需要重新拍照");
                }
            }
        }

        /// <summary>
        /// T5 示例：管壳检测模组自动引导
        /// 用于管壳检测的取料和放料自动引导
        /// </summary>
        private async Task T5_ShellModuleGuideExample(VisionMotionProtocol protocol)
        {
            Console.WriteLine("\n========== T5: 管壳检测模组自动引导 ==========");

            int cameraNo = 1;           // 相机号：1=管壳上相机
            int nozzleNo = 1;           // 吸嘴号
            int pickPlaceFlag = 1;      // 取放料标志：1-取料，2-放料
            string photoPosition1 = "100.0,200.0,0.0";      // 第一次拍照位
            string photoPosition2 = "105.0,205.0,0.0";      // 第二次拍照位（如果只拍一次，发送999,999,999）

            // 发送T5命令
            await protocol.SendT5Async(cameraNo, nozzleNo, pickPlaceFlag, photoPosition1, photoPosition2);

            // 接收T5响应
            var response = await protocol.ReceiveT5Async();

            if (response.Success)
            {
                if (response.ResultCode == 0)
                {
                    Console.WriteLine("T5自动引导成功");
                    if (cameraNo == 1)
                    {
                        Console.WriteLine($"上相机：Mark的绝对坐标: X={response.X}, Y={response.Y}, R={response.R}");
                    }
                    else
                    {
                        Console.WriteLine($"下相机：计算的绝对坐标: X={response.X}, Y={response.Y}, R={response.R}");
                        Console.WriteLine("轴需要运行到此坐标位置");
                    }
                }
            }
            else
            {
                Console.WriteLine($"T5自动引导失败，结果码: {response.ResultCode}");
            }
        }

        /// <summary>
        /// T6 示例：锗窗检测模组自动引导
        /// 用于锗窗检测的取料和放料自动引导，支持多种吸嘴类型
        /// </summary>
        private async Task T6_GeWindowModuleGuideExample(VisionMotionProtocol protocol)
        {
            Console.WriteLine("\n========== T6: 锗窗检测模组自动引导 ==========");

            int cameraNo = 3;           // 相机号：3=锗窗上相机
            int nozzleNo = 1;           // 吸嘴号：1=18mm，2=23mm，3=配重块，4=夹爪
            int pickPlaceFlag = 1;      // 取放料标志：1=取料，2=放料
            string photoPosition1 = "200.0,300.0,0.0";
            string photoPosition2 = "999,999,999";  // 如果只拍一次，发送999

            // 发送T6命令（取料）
            Console.WriteLine("执行取料引导");
            await protocol.SendT6Async(cameraNo, nozzleNo, pickPlaceFlag, photoPosition1, photoPosition2);

            // 接收T6响应
            var response = await protocol.ReceiveT6Async();

            if (response.Success && response.ResultCode == 0)
            {
                Console.WriteLine($"取料引导成功，目标坐标: X={response.X}, Y={response.Y}, R={response.R}");

                // 执行放料引导
                Console.WriteLine("\n执行放料引导");
                pickPlaceFlag = 2;  // 放料
                cameraNo = 4;       // 下相机
                photoPosition1 = "210.0,310.0,5.0";

                await protocol.SendT6Async(cameraNo, nozzleNo, pickPlaceFlag, photoPosition1, photoPosition2);
                var placeResponse = await protocol.ReceiveT6Async();

                if (placeResponse.Success && placeResponse.ResultCode == 0)
                {
                    Console.WriteLine($"放料引导成功，目标坐标: X={placeResponse.X}, Y={placeResponse.Y}, R={placeResponse.R}");
                }
            }
        }

        /// <summary>
        /// T7 示例：出料载具
        /// 包含定位功能和示教功能两种模式
        /// </summary>
        private async Task T7_UnloadCarrierExample(VisionMotionProtocol protocol)
        {
            Console.WriteLine("\n========== T7: 出料载具 ==========");

            int carrierNo = 1;          // 载具号：1=取料18mm载具，2=取料23mm载具，等
            string photoPosition1 = "300.0,400.0,0.0";
            string photoPosition2 = "305.0,405.0,0.0";

            // 模式1：示教功能
            Console.WriteLine("模式1：示教功能");
            int functionNo = 2;         // 功能号：2=示教
            string pickPosition1 = "310.0,410.0,5.0";  // 示教取料位

            await protocol.SendT7Async(carrierNo, functionNo, photoPosition1, photoPosition2, pickPosition1);
            var teachResponse = await protocol.ReceiveT7Async();

            if (teachResponse.Success && teachResponse.ResultCode == 0)
            {
                Console.WriteLine("示教功能执行成功");
                Console.WriteLine("示教功能返回999（不返回具体坐标）");
            }

            // 模式2：定位功能
            Console.WriteLine("\n模式2：定位功能");
            functionNo = 1;             // 功能号：1=定位
            pickPosition1 = "999,999,999";  // 定位功能时发送999

            await protocol.SendT7Async(carrierNo, functionNo, photoPosition1, photoPosition2, pickPosition1);
            var locateResponse = await protocol.ReceiveT7Async();

            if (locateResponse.Success && locateResponse.ResultCode == 0)
            {
                Console.WriteLine("定位功能执行成功");
                Console.WriteLine($"载具R轴坐标: {locateResponse.R}");

                if (locateResponse.XCoordinates != null && locateResponse.YCoordinates != null)
                {
                    Console.WriteLine($"载具共有 {locateResponse.XCoordinates.Length} 个穴位");
                    for (int i = 0; i < locateResponse.XCoordinates.Length; i++)
                    {
                        Console.WriteLine($"  穴位{i + 1}: X={locateResponse.XCoordinates[i]}, Y={locateResponse.YCoordinates[i]}");
                    }
                }
            }
        }

        /// <summary>
        /// T8 示例：出料载具穴位对齐图心
        /// 用于出料载具的三个Mark点对齐图心标定
        /// </summary>
        private async Task T8_CarrierCavityAlignmentExample(VisionMotionProtocol protocol)
        {
            Console.WriteLine("\n========== T8: 出料载具穴位对齐图心 ==========");

            int carrierNo = 4;          // 载具号：4=18mm出料载具
            // 三个Mark点的拍照位置
            string photoPosition1 = "400.0,500.0,0.0";
            string photoPosition2 = "450.0,500.0,0.0";
            string photoPosition3 = "425.0,550.0,0.0";

            // 发送T8命令
            await protocol.SendT8Async(carrierNo, photoPosition1, photoPosition2, photoPosition3);

            // 接收T8响应
            var response = await protocol.ReceiveT8Async();

            if (response.Success && response.ResultCode == 0)
            {
                Console.WriteLine("T8穴位对齐图心成功");
                Console.WriteLine($"Mark1对齐结果: X={response.X1}, Y={response.Y1}");
                Console.WriteLine($"Mark2对齐结果: X={response.X2}, Y={response.Y2}");
                Console.WriteLine($"Mark3对齐结果: X={response.X3}, Y={response.Y3}");
            }
            else
            {
                Console.WriteLine($"T8穴位对齐图心失败，结果码: {response.ResultCode}");
            }
        }

        /// <summary>
        /// 简化示例：单个流程的快速使用
        /// 演示如何快速使用单个协议进行通讯
        /// </summary>
        public async Task QuickExample_T1()
        {
            using (var protocol = new VisionMotionProtocol())
            {
                // 连接
                if (await protocol.ConnectAsync("127.0.0.1", 7950))
                {
                    // 发送并接收T1
                    await protocol.SendT1Async("100.5,200.3,45.0");
                    var response = await protocol.ReceiveT1Async();

                    if (response.Success)
                    {
                        Console.WriteLine($"成功: X={response.X}, Y={response.Y}, R={response.R}");
                    }
                }
            }
        }

        /// <summary>
        /// 错误处理示例
        /// 演示如何处理通讯过程中的各种错误情况
        /// </summary>
        public async Task ErrorHandlingExample()
        {
            using (var protocol = new VisionMotionProtocol())
            {
                // 订阅错误事件
                protocol.OnError += (message) =>
                {
                    // 记录错误到日志文件或数据库
                    System.IO.File.AppendAllText("error.log", $"{DateTime.Now}: {message}\n");
                };

                // 尝试连接，带重试机制
                int retryCount = 3;
                bool connected = false;

                for (int i = 0; i < retryCount; i++)
                {
                    connected = await protocol.ConnectAsync("127.0.0.1", 7950);
                    if (connected)
                        break;

                    Console.WriteLine($"连接失败，重试 {i + 1}/{retryCount}");
                    await Task.Delay(1000);  // 等待1秒后重试
                }

                if (!connected)
                {
                    Console.WriteLine("连接失败，已达到最大重试次数");
                    return;
                }

                // 发送命令并处理响应
                bool sendSuccess = await protocol.SendT1Async("100.0,200.0,0.0");
                if (!sendSuccess)
                {
                    Console.WriteLine("发送命令失败，检查网络连接");
                    return;
                }

                // 接收响应并检查结果
                var response = await protocol.ReceiveT1Async(10000);

                if (string.IsNullOrEmpty(response.RawData))
                {
                    Console.WriteLine("未收到响应数据，可能超时");
                    return;
                }

                if (!response.Success)
                {
                    Console.WriteLine($"视觉系统返回失败，结果码: {response.ResultCode}");

                    // 根据不同的结果码采取不同的处理措施
                    switch (response.ResultCode)
                    {
                        case -1:
                            Console.WriteLine("视觉算法执行失败，检查图像质量或参数设置");
                            break;
                        case 2:
                            Console.WriteLine("需要重新定位和拍照");
                            // 执行重新定位逻辑
                            break;
                        default:
                            Console.WriteLine($"未知的结果码: {response.ResultCode}");
                            break;
                    }
                }
            }
        }
    }
}
