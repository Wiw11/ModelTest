namespace ModelTest;

using System.Reflection;
using System.Linq;
using Daany;

public class ModelTestController
{
    public string Province { get; set; }
    public string Type {get; set;}
    public double Multiple { get; set; } = 3.0;
    public string ResultPathRoot { get; set; } = string.Empty;   // 模型测试结果保存路径
    public ModelConfigLoader modelConfigLoader;
    public ModelInfo model;

    public ModelTestController(string province, string type)
    {
        Province = province;
        Type = type;
        modelConfigLoader = new ModelConfigLoader();
        model = modelConfigLoader.LoadSingleModel(province, type);
        ResultPathRoot = Path.Combine(model.HomePath, "Result", Type, Province);
        if (!Directory.Exists(ResultPathRoot))
        {
            Directory.CreateDirectory(ResultPathRoot);
        }
        else
        {
            Directory.Delete(ResultPathRoot, true);
            Directory.CreateDirectory(ResultPathRoot);
        }
    }

    public SingleModelTest? GetSingleModelTest()
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
            var singleModelTest = Activator.CreateInstance(specifiedType,[Multiple,model]) as SingleModelTest;    // 创建类实例并传递参数
            return singleModelTest;
        }
        else
        {
            throw new Exception($"Class {classNameToInvoke} not found in namespace ModelTest.");
        }
    }

    public void RunIteratively()     // 多实例循环计算
    {
        var structDirs = Directory.GetDirectories(model.SourcePath, "*结构数据*", SearchOption.AllDirectories);

        File.WriteAllText(Path.Combine(ResultPathRoot, "conclusion.csv"), "Basin,IsSuccess,IsPositive\n");
            
        foreach (var instantiationDir in Directory.GetDirectories(structDirs[0]))      // 遍历所有实例
        {
            var count = instantiationDir.Split('\\', '/').Last().Split('_').Length;
            if (count < 4)
            {
                model.InitializePaths(instantiationDir);
                SingleModelTest? singleModelTest = GetSingleModelTest();    // 获取指定模型测试类
                if (singleModelTest != null)
                {
                    singleModelTest.Execute();
                    MakeConclusion(singleModelTest);
                }                
            }
        }
    }

    public void RunSingleModel(string instantiationPath)     // 单实例计算
    {   
        if (!File.Exists(Path.Combine(ResultPathRoot, "conclusion.csv")))
        {
            throw new FileNotFoundException("conclusion.csv file not found.");
        }
        model.InitializePaths(instantiationPath);
        SingleModelTest? singleModelTest = GetSingleModelTest();
        if (singleModelTest != null)
        {
            singleModelTest.Execute();
            MakeConclusion(singleModelTest);
        } 
    }

    public void MakeConclusion(SingleModelTest singleModelTest)     // 生成模型测试结论
    {
        if (singleModelTest.IsSuccess)
        {   
            var analysis = DataFrame.FromCsv(Path.Combine(singleModelTest.modelInfo.ResultPath,"analysis.csv"), sep: ',');
            if (Convert.ToDouble(analysis[0,1]) <= Convert.ToDouble(analysis[1,1]) && 
                Convert.ToDouble(analysis[1,1]) <= Convert.ToDouble(analysis[2,1]))
            {
                File.AppendAllText(Path.Combine(Tools.GetParentPath(model.ResultPath), "conclusion.csv"), 
                    $"{Path.GetFileName(singleModelTest.modelInfo.ResultPath)},Yes,Yes\n");
            }
            else
            {
                File.AppendAllText(Path.Combine(Tools.GetParentPath(model.ResultPath), "conclusion.csv"), 
                    $"{Path.GetFileName(singleModelTest.modelInfo.ResultPath)},Yes,No\n");
            }
        }
        else
        {
            File.AppendAllText(Path.Combine(Tools.GetParentPath(model.ResultPath), "conclusion.csv"), 
                $"{Path.GetFileName(singleModelTest.modelInfo.ResultPath)},No,No\n");
        }
    }
}