using System;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    /// <summary>
    /// VisionWrapper 使用示例
    /// 演示如何在运控程序中调用视觉系统
    /// </summary>
    public class VisionWrapperUsageExample
    {
        /// <summary>
        /// 示例1：基本使用流程
        /// </summary>
        public static async Task Example1_基本使用()
        {
            // 1. 创建视觉对象
            var vision = new VisionWrapper();

            // 2. 订阅日志事件（可选）
            vision.OnLog += (msg) => Console.WriteLine($"[日志] {msg}");
            vision.OnError += (msg) => Console.WriteLine($"[错误] {msg}");

            // 3. 连接到视觉服务器
            bool connected = await vision.ConnectAsync("127.0.0.1", 7950);
            if (!connected)
            {
                Console.WriteLine("连接失败");
                return;
            }

            // 4. 调用视觉功能
            var result = await vision.T1_单次定位拍照(
                currentPosition: "100.0,200.0,0.0"
            );

            // 5. 处理结果
            if (result.Success)
            {
                Console.WriteLine($"定位成功！X={result.X}, Y={result.Y}, R={result.R}");
                // 运控程序使用这些坐标进行移动
            }
            else
            {
                Console.WriteLine($"定位失败：{result.Message}");
            }

            // 6. 断开连接
            vision.Disconnect();
        }

        /// <summary>
        /// 示例2：取料流程（实际运控场景）
        /// </summary>
        public static async Task Example2_取料流程()
        {
            var vision = new VisionWrapper();
            await vision.ConnectAsync("127.0.0.1", 7920);

            // 步骤1：移动到取料位置上方
            Console.WriteLine("运控：移动到取料位置...");
            await Task.Delay(100); // 模拟运控移动

            // 步骤2：调用T3取料对位
            var result = await vision.T3_取料对位(
                cameraNo: 2,
                gripperNo: 1,
                position: "150.5,250.3,45.2"
            );

            if (result.Success)
            {
                // 步骤3：根据视觉返回的偏移进行补偿
                Console.WriteLine($"视觉偏移：X={result.X}, Y={result.Y}, R={result.R}");
                Console.WriteLine("运控：应用偏移补偿...");
                // 运控程序在这里调整位置
                // MoveToPosition(currentX + result.X, currentY + result.Y, currentR + result.R);

                // 步骤4：下降夹取
                Console.WriteLine("运控：下降夹取...");
            }
            else
            {
                Console.WriteLine($"取料对位失败：{result.Message}");
            }

            vision.Disconnect();
        }

        /// <summary>
        /// 示例3：九点标定流程
        /// </summary>
        public static async Task Example3_九点标定流程()
        {
            var vision = new VisionWrapper();
            await vision.ConnectAsync("127.0.0.1", 7930);

            int cameraNo = 2;
            int moduleNo = 4;
            int gripperNo = 1;
            double step = 5.0;            // 步距5mm
            string centerPoint = "0,0,0"; // 中心点

            // 步骤1：清除旧标定点
            Console.WriteLine("1. 清除旧标定点...");
            var result1 = await vision.T2_清除标定点(cameraNo, moduleNo, gripperNo, step, centerPoint);
            if (!result1.Success)
            {
                Console.WriteLine("清除失败！");
                return;
            }

            // 步骤2：执行九点标定
            Console.WriteLine("2. 开始九点标定（需要移动到9个位置拍照）...");
            var result2 = await vision.T2_九点标定(cameraNo, moduleNo, gripperNo, step, centerPoint);
            if (!result2.Success)
            {
                Console.WriteLine("九点标定失败！");
                return;
            }

            // 步骤3：结束标定
            Console.WriteLine("3. 结束标定...");
            var result3 = await vision.T2_结束标定(cameraNo, moduleNo, gripperNo);
            if (result3.Success)
            {
                Console.WriteLine("标定完成！");
            }

            vision.Disconnect();
        }

        /// <summary>
        /// 示例4：管壳检测引导流程
        /// </summary>
        public static async Task Example4_管壳检测引导()
        {
            var vision = new VisionWrapper();
            await vision.ConnectAsync("127.0.0.1", 7940);

            // 取料时的引导
            var result = await vision.T5_管壳检测引导(
                cameraNo: 1,
                nozzleNo: 1,
                pickPlaceFlag: 1,  // 1=取料
                photoPosition1: "100.0,200.0,0.0",
                photoPosition2: "999,999,999"  // 只拍一次
            );

            if (result.Success)
            {
                Console.WriteLine($"引导成功！目标位置：X={result.X}, Y={result.Y}, R={result.R}");
                // 运控程序移动到目标位置
            }

            vision.Disconnect();
        }

        /// <summary>
        /// 示例5：完整的生产流程
        /// </summary>
        public static async Task Example5_完整生产流程()
        {
            var vision = new VisionWrapper();

            // 订阅日志
            vision.OnLog += (msg) => Console.WriteLine($"[视觉日志] {msg}");
            vision.OnError += (msg) => Console.WriteLine($"[视觉错误] {msg}");

            // 连接
            if (!await vision.ConnectAsync("127.0.0.1", 7950))
            {
                Console.WriteLine("视觉服务器连接失败！");
                return;
            }

            try
            {
                // 1. 取料对位
                Console.WriteLine("\n=== 步骤1：取料对位 ===");
                var pickResult = await vision.T3_取料对位(2, 1, "100,200,0");
                if (!pickResult.Success)
                {
                    Console.WriteLine($"取料失败：{pickResult.Message}");
                    return;
                }
                Console.WriteLine($"取料位置：X={pickResult.X}, Y={pickResult.Y}, R={pickResult.R}");

                // 2. 管壳检测引导
                Console.WriteLine("\n=== 步骤2：管壳检测 ===");
                var shellResult = await vision.T5_管壳检测引导(1, 1, 1, "150,250,0");
                if (!shellResult.Success)
                {
                    Console.WriteLine($"管壳检测失败：{shellResult.Message}");
                    return;
                }
                Console.WriteLine($"管壳位置：X={shellResult.X}, Y={shellResult.Y}, R={shellResult.R}");

                // 3. 锗窗检测引导
                Console.WriteLine("\n=== 步骤3：锗窗检测 ===");
                var windowResult = await vision.T6_锗窗检测引导(3, 1, 2, "200,300,0");
                if (!windowResult.Success)
                {
                    Console.WriteLine($"锗窗检测失败：{windowResult.Message}");
                    return;
                }
                Console.WriteLine($"锗窗位置：X={windowResult.X}, Y={windowResult.Y}, R={windowResult.R}");

                // 4. 出料载具
                Console.WriteLine("\n=== 步骤4：放料到载具 ===");
                var carrierResult = await vision.T7_出料载具(1, 1, "250,350,0", "260,360,0", "270,370,0");
                if (!carrierResult.Success)
                {
                    Console.WriteLine($"出料失败：{carrierResult.Message}");
                    return;
                }
                Console.WriteLine($"载具位置：X={carrierResult.X}, Y={carrierResult.Y}, R={carrierResult.R}");

                Console.WriteLine("\n=== 生产流程完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"流程异常：{ex.Message}");
            }
            finally
            {
                vision.Disconnect();
                vision.Dispose();
            }
        }

        /// <summary>
        /// 示例6：错误处理
        /// </summary>
        public static async Task Example6_错误处理()
        {
            var vision = new VisionWrapper();

            // 1. 连接失败处理
            if (!await vision.ConnectAsync("127.0.0.1", 9999))
            {
                Console.WriteLine("连接失败，请检查：");
                Console.WriteLine("1. 视觉服务器是否启动");
                Console.WriteLine("2. IP和端口是否正确");
                Console.WriteLine("3. 网络是否通畅");
                return;
            }

            // 2. 操作失败处理
            var result = await vision.T1_单次定位拍照("100,200,0");
            if (!result.Success)
            {
                // 根据结果码判断失败原因
                switch (result.ResultCode)
                {
                    case 0:
                        Console.WriteLine("成功");
                        break;
                    case 1:
                        Console.WriteLine("视觉识别失败，请检查工件是否在视野内");
                        break;
                    case 2:
                        Console.WriteLine("通讯超时，请检查网络");
                        break;
                    default:
                        Console.WriteLine($"未知错误，结果码：{result.ResultCode}");
                        break;
                }
            }

            vision.Disconnect();
        }
    }
}
