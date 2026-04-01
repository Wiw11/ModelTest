namespace ModelTest;

using CsvHelper;
using CsvHelper.Configuration;
using Daany;
using System.Globalization;

public class ModelConfigLoader        // 载入模型元数据
{
    string csvPath;
    public List<ModelInfo> models = new List<ModelInfo>();

    public ModelConfigLoader(string path = "model_main.csv")
    {
        csvPath = "D:\\Projects\\ModelTest" + "\\" + path;
        if (!File.Exists(csvPath))
        {
            Console.WriteLine($"配置文件不存在: {csvPath}");
            throw new FileNotFoundException();
        }
        else
        {
            ReadModelInfo();
        }
    }

    public void ReadModelInfo()
    {
        var df = DataFrame.FromCsv(csvPath, sep: ',');    // 读取csv文件成DataFrame
        for (var i = 0; i < df.RowCount(); i++)    // 初始化所有模型信息
        {
            var model = new ModelInfo    
            {
                Province = df[i, 0]?.ToString() ?? string.Empty,
                Type = df[i, 1]?.ToString() ?? string.Empty,
                MainFile = df[i, 2]?.ToString() ?? string.Empty,
                InputFile = df[i, 3]?.ToString() ?? string.Empty,
                OutputDir = df[i, 4]?.ToString() ?? string.Empty,
                SourcePath = df[i, 5]?.ToString() ?? string.Empty,
            };
            model.SourcePath = model.SourceRoot + model.SourcePath;
            models.Add(model);
        }
    }

    public ModelInfo LoadSingleModel(string province, string type)    // 载入单个模型
    {
        foreach (var model in models)
        {
            if (model.Province == province && model.Type == type)
            {
                var modelCopy = new ModelInfo      // 建立模型副本
                {
                    Province = model.Province,
                    Type = model.Type,
                    MainFile = model.MainFile,
                    InputFile = model.InputFile,
                    OutputDir = model.OutputDir,
                    SourcePath = model.SourcePath,
                    SourceRoot = model.SourceRoot
                };
                return modelCopy;
            }
        }
        return new ModelInfo();
    }
}
