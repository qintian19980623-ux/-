namespace 通讯协议测试
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new VisionProtocolTestForm());
        }
    }
}
