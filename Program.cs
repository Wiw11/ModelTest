namespace ModelTest;

class Program
{
    static async Task Main (string[] args)
    {
        // ModelTestController controller = new ModelTestController(args[0], args[1]);
        ModelTestController controller = new ModelTestController("Guangxi","Hydrodynamic");

        if (args.Length == 3)
        {
            string param3 = args[2].ToLower();  // 解析第三个参数
            switch (param3)
            {
                case "true":
                    await controller.RunIteratively(true);       // 异步多实例循环计算
                    break;
                case "false":
                    await controller.RunIteratively(false);       // 多实例循环计算
                    break;
                default:
                    controller.RunSingleModel(args[2]);       // 指定单实例计算
                    break;
            }
        }
        else
        {
            await controller.RunIteratively();     // 默认进行多实例循环计算
        }
    }
}