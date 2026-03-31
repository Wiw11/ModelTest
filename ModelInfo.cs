namespace ModelTest;

public class ModelInfo     // 模型测试基础信息类
{
    public string HomePath { get; set; } = string.Empty;    // 根目录

    public string Province { get; set; } = string.Empty;     // 省份
    public string Type { get; set; } = string.Empty;         // 模型类型
    public string MainFile { get; set; } = string.Empty;     // 模型可执行文件名称
    public string InputFile { get; set; } = string.Empty;    // 模型输入文件名称
    public string OutputDir { get; set; } = string.Empty;    // 模型输出目录名称
    public string SourcePath { get; set; } = string.Empty;   // 模型源文件路径
    
    public string InstantiationPath { get; set; } = string.Empty;       // 流域实例数据路径

    public string SourceRoot { get; set; } = @"D:\Projects\水科院模型\";        // 模型源文件根目录   
    public string TestPath { get; set; } = string.Empty;      // 测试空间路径
    public string MainPath { get; set; } = string.Empty;      // 模型可执行文件路径
    public string OutputPath { get; set; } = string.Empty;    // 模型输出目录路径    
    public string ResultPath { get; set; } = string.Empty;    // 测试分析结果保存路径 
    public string InputPath { get; set; } = string.Empty;     // 模型输入文件路径
    public string GisPath { get; set; } = string.Empty;     // 地理信息数据转存路径


    public ModelInfo()
    {
        // 获取当前目录
        HomePath = @"D:\Projects\ModelTest";
    }

    public void InitializePaths(string instantiationPath)     // 初始化各路径
    {
        InstantiationPath = instantiationPath;
        TestPath = Path.Combine(HomePath, Type, Province);
        MainPath = Path.Combine(TestPath, MainFile);
        OutputPath = Path.Combine(TestPath, OutputDir);
        InputPath = Path.Combine(TestPath, InputFile);
        ResultPath = Path.Combine(HomePath, "Result", Type, Province, Path.GetFileName(InstantiationPath));
        GisPath = Path.Combine(ResultPath, "GIS");
    }
}