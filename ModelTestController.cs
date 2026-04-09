namespace ModelTest;

using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using Daany;

public class ModelTestController
{
    public string Province { get; set; }
    public string Type {get; set;}
    public double Multiple { get; set; } = 3.0;
    public string ResultPathRoot { get; set; } = string.Empty;   // 模型测试结果保存路径
    public ModelConfigLoader modelConfigLoader;
    public ModelInfo model;
    private static readonly object _fileLock = new object();

    public ModelTestController(string province, string type)
    {
        Province = province;
        Type = type;
        modelConfigLoader = new ModelConfigLoader();
        model = modelConfigLoader.LoadSingleModel(province, type);
        ResultPathRoot = Path.Combine(model.HomePath, Type, Province, "Result");
        if (!Directory.Exists(ResultPathRoot))
        {
            Directory.CreateDirectory(ResultPathRoot);
        }
    }

    public SingleModelTest? GetSingleModelTest(ModelInfo modelInfo)
    {
        // 获取命名空间下的所有类
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type[] typesInNamespace = assembly.GetTypes()
            .Where(t => t.IsClass && t.Namespace == "ModelTest")
            .ToArray();
        string classNameToInvoke = Province + Type;         // 由省份和类型组成类名

        Type? specifiedType = typesInNamespace.FirstOrDefault(t => t.Name == classNameToInvoke);    // 获取指定类的Type对象
        if (specifiedType != null)
        {
            var singleModelTest = Activator.CreateInstance(specifiedType,[Multiple,modelInfo]) as SingleModelTest;    // 创建类实例并传递参数
            return singleModelTest;
        }
        else
        {
            throw new Exception($"Class {classNameToInvoke} not found in namespace ModelTest.");
        }
    }

    public async Task RunIteratively(bool async = false)     // 多实例循环计算
    {
        var structDirs = Directory.GetDirectories(model.SourcePath, "*结构数据*", SearchOption.AllDirectories);

        if (!File.Exists(Path.Combine(ResultPathRoot, "conclusion.csv")))     // 增量写入，不清空
        {
            File.WriteAllText(Path.Combine(ResultPathRoot, "conclusion.csv"), "Basin,IsSuccess,IsPositive\n");
        }

        var df = DataFrame.FromCsv(Path.Combine(ResultPathRoot, "conclusion.csv"), sep: ',');
            
        List<SingleModelTest> singleModelTests = new List<SingleModelTest>();
        foreach (var instantiationDir in Directory.GetDirectories(structDirs[0]))      // 循环遍历所有实例
        {
            var count = instantiationDir.Split('\\', '/').Last().Split('_').Length;    
            if (count < 4 && !df["Basin"].Contains(Path.GetFileName(instantiationDir)))    // 确保实例目录名称合法且未被执行过
            {
                ModelInfo modelInfo = modelConfigLoader.LoadSingleModel(Province, Type);
                modelInfo.InitializePaths(instantiationDir);
                SingleModelTest? singleModelTest = GetSingleModelTest(modelInfo);
                if (singleModelTest != null)
                {
                    singleModelTests.Add(singleModelTest);
                }
            }
        }

        if (async)    // 异步执行
        {
            int maxConcurrency = 5;     // 控制并发数
            using var semaphore = new SemaphoreSlim(maxConcurrency);
            var tasks = new List<Task>();
            
            foreach (var singleModelTest in singleModelTests)
            {
                await semaphore.WaitAsync();
                var test = singleModelTest;  // 捕获当前变量
                
                tasks.Add(Task.Run(() =>
                {
                    test.Execute();
                    MakeConclusion(test);
                    semaphore.Release();
                }));
            }
            
            await Task.WhenAll(tasks);
        }
        else     // 循环执行
        {
            foreach (var singleModelTest in singleModelTests)
            {
                singleModelTest.Execute();
                MakeConclusion(singleModelTest);
            }
        }
    }

    public void RunSingleModel(string instantiationDir)     // 单实例计算
    {   
        var structDirs = Directory.GetDirectories(model.SourcePath, "*结构数据*", SearchOption.AllDirectories);
        string instantiationPath = Path.Combine(structDirs[0], instantiationDir);

        if (!File.Exists(Path.Combine(ResultPathRoot, "conclusion.csv")))     // 增量写入，不清空
        {
            File.WriteAllText(Path.Combine(ResultPathRoot, "conclusion.csv"), "Basin,IsSuccess,IsPositive\n");
        }

        ModelInfo modelInfo = modelConfigLoader.LoadSingleModel(Province, Type);
        modelInfo.InitializePaths(instantiationPath);
        SingleModelTest? singleModelTest = GetSingleModelTest(modelInfo);
        if (singleModelTest != null)
        {
            singleModelTest.Execute();
            MakeConclusion(singleModelTest);
        } 
    }

    public void MakeConclusion(SingleModelTest singleModelTest)     // 生成模型测试结论
    {
        var analysis = DataFrame.FromCsv(Path.Combine(singleModelTest.modelInfo.ResultPath,"analysis.csv"), sep: ',');
        
        if (singleModelTest.IsSuccess && analysis.RowCount() >= 3)
        {   
            if (Convert.ToDouble(analysis[0,1]) <= Convert.ToDouble(analysis[1,1]) && 
                Convert.ToDouble(analysis[1,1]) <= Convert.ToDouble(analysis[2,1]))
            {
                lock (_fileLock)
                {
                   File.AppendAllText(Path.Combine(Tools.GetParentPath(singleModelTest.modelInfo.ResultPath), "conclusion.csv"), 
                    $"{Path.GetFileName(singleModelTest.modelInfo.ResultPath)},Yes,Yes\n"); 
                }
            }
            else
            {
                lock (_fileLock)
                {
                    File.AppendAllText(Path.Combine(Tools.GetParentPath(singleModelTest.modelInfo.ResultPath), "conclusion.csv"), 
                        $"{Path.GetFileName(singleModelTest.modelInfo.ResultPath)},Yes,No\n");
                }
            }
        }
        else
        {
            lock (_fileLock)
            {
                File.AppendAllText(Path.Combine(Tools.GetParentPath(singleModelTest.modelInfo.ResultPath), "conclusion.csv"), 
                $"{Path.GetFileName(singleModelTest.modelInfo.ResultPath)},No,--\n");
            }
        }
    }
}