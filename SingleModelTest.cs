namespace ModelTest;

public class SingleModelTest      //单实例模型测试类
{
    public double Multiple { get; set; }       // 流量放大倍数  
    public double CurrentMultiple { get; set; }       // 当前流量放大倍数  
    public bool IsSuccess { get; set; }        // 模型运行是否成功
    public ModelInfo modelInfo;

    public SingleModelTest(double multiple, ModelInfo model)
    {
        Multiple = multiple;
        CurrentMultiple = 1.0;
        IsSuccess = false;
        modelInfo = model;
    }

    public void CopyExecutables()   //搜索并复制可执行文件
    {
        var dir = Directory.GetDirectories(modelInfo.SourcePath, "*可执行*", SearchOption.AllDirectories);
        var mainDir = Tools.GetSpecialLevelFiles(dir[0], modelInfo.MainFile);
        if (mainDir != null && Directory.Exists(mainDir))
        {
            Tools.CopyDirectory(mainDir, modelInfo.TestPath, true);
        }
    }

    public void CopyShapeFiles()   //搜索并复制地理空间数据
    {
        if (! Directory.Exists(modelInfo.GisPath))
        {
            Directory.CreateDirectory(modelInfo.GisPath);
        }

        foreach (var file in Directory.GetFiles(modelInfo.InstantiationPath, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file).ToLower();
            if (ext is ".shp" or ".dbf" or ".shx" or ".prj" or ".cpg")
            {
                var destFile = Path.Combine(modelInfo.GisPath, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
        }
    }

    public void CopyInputFiles()   //搜索并复制模型输入数据
    {
        if (Directory.Exists(modelInfo.InstantiationPath))
        {
            Tools.CopyDirectory(modelInfo.InstantiationPath, modelInfo.TestPath, true);
        }
    }

    public virtual void ModifyConfigFiles()   //修改模型配置文件
    {}

    public virtual void ModifyFlowFiles()   //缩放流量
    {}

    public virtual void Preprocess()   //执行前处理
    {}

    public void RunModel()   //运行模型
    {
        // 创建输出目录
        if (! Directory.Exists(modelInfo.OutputPath))
        {
            Directory.CreateDirectory(modelInfo.OutputPath);
        }
        else
        {
            Directory.Delete(modelInfo.OutputPath, true);    // 清空模型输出目录
            Directory.CreateDirectory(modelInfo.OutputPath);
        }
        
        // 创建init.txt使输出目录非空
        var initFile = Path.Combine(modelInfo.OutputPath, "init.txt");
        File.WriteAllText(initFile, DateTime.Now.ToString());

        // 记录初始状态
        var initialFiles = Directory.GetFiles(modelInfo.OutputPath, "*", SearchOption.AllDirectories).Select(f => new FileInfo(f)).ToList();
        
        // 运行可执行文件
        if (File.Exists(modelInfo.MainPath))
        {
            ExternalProcessRunner.Run(modelInfo.MainPath);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"可执行文件不存在: {modelInfo.MainPath}");
            Console.ResetColor();
            IsSuccess = false;
            return;
        }

        // 记录最终状态
        var finalFiles = Directory.GetFiles(modelInfo.OutputPath, "*", SearchOption.AllDirectories).Select(f => new FileInfo(f)).ToList();

        // 比较初始状态和最终状态
        var changed = initialFiles.Count != finalFiles.Count;

        if (changed)
        {
            IsSuccess = true;

            // 复制模型输出到结果目录
            string outputWithMultiple = Path.Combine(modelInfo.ResultPath, Path.GetFileName(modelInfo.OutputPath) + $"_{CurrentMultiple}");
            Directory.CreateDirectory(outputWithMultiple);
            Tools.CopyDirectory(modelInfo.OutputPath, outputWithMultiple, true);
            string destDir = Path.Combine(modelInfo.ResultPath, Path.GetFileName(modelInfo.OutputPath));
        }
        else
        {
            IsSuccess = false;
        }
    }

    public virtual void Postprocess()   //执行后处理
    {
    }

    public void CalculateSubmergedArea()   //计算淹没面积
    {
        var pythonScript = Path.Combine(modelInfo.HomePath, "postprocessing.py");
        string functionName = $"{modelInfo.Province}{modelInfo.Type}";
        string arguments = $"\"{pythonScript}\" \"{functionName}\" \"{modelInfo.OutputPath}\" \"{modelInfo.ResultPath}\" {CurrentMultiple}";
        ExternalProcessRunner.Run(@".\.venv\Scripts\python.exe", arguments);
    }

    public void Execute()   //执行单实例模型测试
    {
        if (string.IsNullOrEmpty(modelInfo.ResultPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{modelInfo.Province} {modelInfo.Type}模型不存在");
            Console.ResetColor();
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine($"Start running {modelInfo.Province} {modelInfo.Type} flow{CurrentMultiple} {Path.GetFileName(modelInfo.InstantiationPath)}...");

                if (i == 0)
                {
                    if (Directory.Exists(modelInfo.TestPath)) {Directory.Delete(modelInfo.TestPath, true);}    // 清空测试空间
                    
                    CopyExecutables();
                    CopyShapeFiles();
                    CopyInputFiles();
                    ModifyConfigFiles();
                    Preprocess();

                    File.WriteAllText(Path.Combine(modelInfo.ResultPath, "analysis.csv"), "Multiple,MaxSubmergedArea,Time\n");   // 新建分析结果文件
                }
                ModifyFlowFiles();
                RunModel();
                    
                if (IsSuccess)
                {
                    Postprocess();
                    CalculateSubmergedArea();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{modelInfo.Province} {modelInfo.Type} runs successfully!");
                    Console.ResetColor();

                    CurrentMultiple *= Multiple;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{modelInfo.Province} {modelInfo.Type} failed to run!");
                    Console.ResetColor();
                    break;
                }
            }
        }
    }
}