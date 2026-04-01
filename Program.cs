namespace ModelTest;

class Program
{
    static async Task Main (string[] args)
    {
        ModelTestController controller = new ModelTestController(args[0], args[1]);
        // ModelTestController controller = new ModelTestController("Guizhou","Hydrodynamic");
        bool async = args.Length == 3 && args[2].ToLower() == "true";
        await controller.RunIteratively(async);
    }
}