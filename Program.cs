namespace ModelTest;

class Program
{
    static void Main (string[] args)
    {
        ModelTestController controller = new ModelTestController(args[0], args[1]);
        controller.RunIteratively();
    }
}