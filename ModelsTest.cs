namespace ModelTest;

using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using System.Text.Json;

public class GuizhouHydrodynamic: SingleModelTest
{
    public GuizhouHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void Preprocess()
    {
        RunWithConsole = true;
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "run.bat"), $"{modelInfo.MainPath} {modelInfo.TestPath}\\programmeinfo.json");
        modelInfo.MainPath = Path.Combine(modelInfo.TestPath, "run.bat");
    }
}

public class ShandongHydrodynamic: SingleModelTest
{
    public ShandongHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void Preprocess()
    {
        RunWithConsole = true;    // 设置为弹窗运行
        File.Copy(Path.Combine(modelInfo.HomePath, "qemu-aarch64-static"), Path.Combine(modelInfo.TestPath, "qemu-aarch64-static"), true);
        string mianPath = modelInfo.MainPath.Replace("\\", "/").Replace("D:", "/mnt/d");
        string testPath = modelInfo.TestPath.Replace("\\", "/").Replace("D:", "/mnt/d");
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "run.bat"), $"wsl {testPath}/qemu-aarch64-static -L /usr/aarch64-linux-gnu {mianPath}");
        modelInfo.MainPath = Path.Combine(modelInfo.TestPath, "run.bat");
    }

    public override void ModifyFlowFiles()
    {
        // 实现修改流量文件的逻辑
        string[] lines = File.ReadAllLines(modelInfo.InputPath);
        string[] newLines = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string pattern = @"^(\d+)\s+(\d+\.\d+)";
            Match match = Regex.Match(line, pattern);
            if (match.Success)
            {
                string id = match.Groups[1].Value;
                double value = double.Parse(match.Groups[2].Value) * Multiple;
                newLines[i] = $"{id} {value}";
            }
            else
            {
                newLines[i] = line;
            }
        }
        File.WriteAllLines(modelInfo.InputPath, newLines);
    }
}

public class AnhuiSubmerged: SingleModelTest
{
    public AnhuiSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void Preprocess()
    {
        string content = @".\simplifiedinundate.exe -c .\setup.conf -o .\results";
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "run.bat"), content);
        modelInfo.MainPath = Path.Combine(modelInfo.TestPath, "run.bat");
    }

    public override void ModifyConfigFiles()
    {
        string[] lines = File.ReadAllLines(Path.Combine(modelInfo.TestPath, "setup.conf"));
        string[] newLines = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string pattern = "\"";
            Match match = Regex.Match(line, pattern);
            if (match.Success)
            {
                string key = line.Split("=")[0].Trim();
                string value = line.Split("/")[line.Split("/").Length - 1].Trim().Replace("\"", "");
                newLines[i] = $"{key} = \"./{value}\"";
            }
            else
            {
                newLines[i] = line;
            }
        }
        File.WriteAllLines(Path.Combine(modelInfo.TestPath, "setup.conf"), newLines);
    }

    public override void ModifyFlowFiles()
    {
        string[] lines = File.ReadAllLines(modelInfo.InputPath);
        string[] newLines = new string[lines.Length];
        Array.Copy(lines, 0, newLines, 0, 1); 
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Split(",", StringSplitOptions.RemoveEmptyEntries);
            string newLine = string.Empty;
            for (int j = 0; j < values.Length; j++)
            {
                if (j == 0)
                {
                    newLine += values[j] + ",";
                }
                else if (j == values.Length - 1)
                {
                    double value = double.Parse(values[j]) * Multiple;
                    newLine += value;
                }
                else
                {
                    double value = double.Parse(values[j]) * Multiple;
                    newLine += value + ",";
                }
            }
            newLines[i] = newLine.TrimEnd();
        }
        File.WriteAllLines(modelInfo.InputPath, newLines);
    }
}

public class GansuSubmerged: AnhuiSubmerged
{
    public GansuSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void Preprocess()
    {
        // RunWithConsole = true;
        string content = ".\\simplifiedinundate.exe stagedischarge -c .\\zq.conf -o .\\results\n.\\simplifiedinundate.exe inundate -c .\\inundate.conf -o .\\results";
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "run.bat"), content);
        modelInfo.MainPath = Path.Combine(modelInfo.TestPath, "run.bat");
    }

    public override void ModifyConfigFiles()
    {
        string zqValue = "sections = \"./建模区域断面高程数据.geojson\"\nsigmac = \"./sigmac.dat\"\nm = 1.0\n[license]\nlicense = \"./License.lic\"\nkey =\"H/7VfJJaxwT3D7YnZ3YKeVyPI3DWuQ0CBLXewrsvzYU=\"";
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "zq.conf"), zqValue);
        string inundateValue = "sections = \"./建模区域断面高程数据.geojson\"\nzq = \"./results/建模区域断面高程数据_ZQ.CSV\"\nsection_flow = \"./输入数据样例_Q.csv\"\ndem = \"./建模区域数字高程信息.asc\"\n" + 
            "[license]\nlicense = \"./License.lic\"\nkey =\"H/7VfJJaxwT3D7YnZ3YKeVyPI3DWuQ0CBLXewrsvzYU=\"";
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "inundate.conf"), inundateValue);
    }
}

public class QinghaiSubmerged: GansuSubmerged
{
    public QinghaiSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
    }
}

public class HenanSubmerged: SingleModelTest
{
    public HenanSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void CopyExecutables()
    {
    }

    public override void ExecutableCore()
    {
        // 重新定位输出目录
        modelInfo.OutputPath = modelInfo.OutputDir.Replace("..", Tools.GetParentPath(modelInfo.TestPath));

        // 发送请求
        string jsonFile = Directory.GetFiles(modelInfo.TestPath, "*.json", SearchOption.TopDirectoryOnly)[0];
        Tools.PostRequest("http://127.0.0.1:8087/server/simple/model/getParseData", jsonFile);
    }

    public override void ModifyFlowFiles()
    {
        string jsonFile = Directory.GetFiles(modelInfo.TestPath, "*.json", SearchOption.TopDirectoryOnly)[0];
        string jsonArray = File.ReadAllText(jsonFile);
        JsonNode? node = JsonNode.Parse(jsonArray);

        foreach (var item in node!.AsArray())
        {
            if (item != null && item["list"] != null)
            {
                foreach (var q in item["list"]!.AsArray())
                {
                    if (q != null && q["maxq"] != null)
                    {
                       q["maxq"] = q["maxq"]!.GetValue<double>() * Multiple; 
                    }
                }
            }
        }

        // 保存
        File.WriteAllText(jsonFile, node!.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}

public class NingxiaSubmerged: SingleModelTest
{
    public NingxiaSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void Preprocess()
    {
        Directory.CreateDirectory(Path.Combine(modelInfo.TestPath, "input"));
        File.Copy(Path.Combine(modelInfo.TestPath,"input_Q.json"), Path.Combine(modelInfo.TestPath, "input", "input_Q.json"), true);
    }

    public override void ModifyConfigFiles()
    {
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "config.txt"), "INPUT_Q_JSON&&./input/input_Q.json\nMODEL_JSON&&./1.XJG.json");
        File.Copy(Path.Combine(modelInfo.TestPath,"config.txt"), Path.Combine(modelInfo.TestPath, "input", "config.txt"), true);
    }
}

public class HebeiSubmerged: HenanSubmerged
{
    public HebeiSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void ExecutableCore()
    {
        // 重新定位输出目录
        modelInfo.OutputPath = modelInfo.OutputDir.Replace("..", Tools.GetParentPath(modelInfo.TestPath));

        // 发送请求
        Tools.PostRequest("http://127.0.0.1:8087/SimplifyModel/simple/model/getParseData", modelInfo.InputPath);
    }

    public override void ModifyFlowFiles()
    {
        string json = File.ReadAllText(modelInfo.InputPath);
        JsonNode? node = JsonNode.Parse(json);

        if (node != null && node["list"] != null)
        {
            foreach (var item in node["list"]!.AsArray())
            {
                if (item != null && item["maxq"] != null)
                {
                    item["maxq"] = item["maxq"]!.GetValue<double>() * Multiple; 
                }
            }
        }

        // 保存
        File.WriteAllText(modelInfo.InputPath, node!.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public override void Postprocess()
    {
        // Console.WriteLine(modelInfo.OutputPath);
        string subDir1 = Directory.GetDirectories(modelInfo.OutputPath)[0];
        string subDir2 = Directory.GetDirectories(subDir1)[0];
        modelInfo.OutputPath = subDir2;
    }
}