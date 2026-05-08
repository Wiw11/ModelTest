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

public class GuangdongHydrodynamic: SingleModelTest
{
    public GuangdongHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void Preprocess()
    {
        RunWithConsole = true;
        Directory.CreateDirectory(Path.Combine(modelInfo.TestPath, "Template", Path.GetFileName(modelInfo.TestPath)));
        Directory.GetFiles(Tools.GetParentPath(modelInfo.InputPath), "*.*", SearchOption.TopDirectoryOnly).ToList().ForEach(file =>
        {
            var destFile = Path.Combine(modelInfo.TestPath, "Template", Path.GetFileName(modelInfo.TestPath), Path.GetFileName(file));
            File.Copy(file, destFile, true);
        });
    }

    public override void ModifyFlowFiles()
    {
        // 遍历目标路径下的所有.dat文件
        foreach (var file in Directory.GetFiles(Path.Combine(modelInfo.TestPath, "Template", Path.GetFileName(modelInfo.TestPath)), "*.dat"))
        {
            // 读取文件内容
            var lines = File.ReadAllText(file, System.Text.Encoding.UTF8);
            
            // 定义正则表达式模式
            var pattern = @"(TV\s+\d+\s+)(\d+\.\d+)";
            var regex = new Regex(pattern);

            // 替换匹配到的内容
            var newContent = regex.Replace(lines, match =>
            {
                // 获取捕获组的值
                var group1 = match.Groups[1].Value;
                var group2 = match.Groups[2].Value;

                // 将捕获的数字转换为浮点数进行计算
                var result = float.Parse(group2) * Multiple;

                // 返回替换后的字符串
                return $"{group1} {result}";
            });

            // 将处理后的内容写回原文件
            File.WriteAllText(file, newContent, System.Text.Encoding.Default);
        }
    }

    public override void Postprocess()
    {
        var destFile = Path.Combine(Path.Combine(modelInfo.TestPath, "Template"), "CellResult.txt");
        File.Copy(Path.Combine(modelInfo.TestPath, "Template", Path.GetFileName(modelInfo.TestPath), "CellResult.txt"), destFile, true);
    }
}

public class AnhuiHydrodynamic: SingleModelTest
{
    public AnhuiHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void Preprocess()
    {
        RunWithConsole = true;
    }

    public override void ModifyFlowFiles()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(modelInfo.TestPath, "Template"), "*.dat"))
        {
            // 读取文件内容
            var lines = File.ReadAllText(file, System.Text.Encoding.UTF8);
            
            // 定义正则表达式模式
            var pattern = @"(TV\s+\d+\s+)(\d+\.?\d+)";
            var regex = new Regex(pattern);

            // 替换匹配到的内容
            var newContent = regex.Replace(lines, match =>
            {
                // 获取捕获组的值
                var group1 = match.Groups[1].Value;
                var group2 = match.Groups[2].Value;

                // 将捕获的数字转换为浮点数进行计算
                var result = float.Parse(group2) * Multiple;

                // 返回替换后的字符串
                return $"{group1} {result}";
            });

            // 将处理后的内容写回原文件
            File.WriteAllText(file, newContent, System.Text.Encoding.Default);
        }
    }
}

public class HeilongjiangHydrodynamic: SingleModelTest
{
    public HeilongjiangHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void ModifyFlowFiles()
    {
        string[] lines = File.ReadAllLines(modelInfo.InputPath);
        string[] newLines = new string[lines.Length];
        Array.Copy(lines, 0, newLines, 0, 2);   // 保留前两行不变
        for (int i = 2; i < lines.Length; i++)
        {
            string line = lines[i];
            var space = new Regex(@"\s+");
            string lineWithoutSpace = space.Replace(line, ",");
            string[] values = lineWithoutSpace.Split(",", StringSplitOptions.RemoveEmptyEntries);
            string newLine = string.Empty;
            for (int j = 0; j < values.Length; j++)
            {
                if (j == 0)
                {
                    newLine += values[j] + "   ";
                }
                else
                {
                    double value = double.Parse(values[j]) * Multiple;
                    newLine += value + "   ";
                }
            }
            newLines[i] = newLine.TrimEnd();
        }
        File.WriteAllLines(modelInfo.InputPath, newLines);
    }

    public override void ModifyConfigFiles()
    {
        string[] lines = File.ReadAllLines(modelInfo.MainPath);
        lines[3] = @"set HOMETEL=D:\telemac\V8P4";
        File.WriteAllLines(modelInfo.MainPath, lines);
    }
}

public class ShanxiHydrodynamic: SingleModelTest
{
    public ShanxiHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
    }
}

public class XizangHydrodynamic: SingleModelTest
{
    public XizangHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void Preprocess()
    {
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "run.bat"), @".\.venv\Scripts\python.exe run.py");
        modelInfo.MainPath = Path.Combine(modelInfo.TestPath, "run.bat");
    }

    public override void ModifyFlowFiles()
    {
        foreach (var file in Directory.GetFiles(modelInfo.InputPath, "h_BC"))
        {
            string[] lines = File.ReadAllLines(file);
            string[] newLines = new string[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string pattern = @"^(\d+)\s+(\d+)";
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
        string subDir1 = Directory.GetDirectories(modelInfo.OutputPath)[0];
        string subDir2 = Directory.GetDirectories(subDir1)[0];
        modelInfo.OutputPath = subDir2;
    }
}

public class GuangxiSubmerged: AnhuiSubmerged
{
    public GuangxiSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void Preprocess()
    {
        RunWithConsole = true;
    }

    public override void ModifyConfigFiles()
    {}
}

public class GuangdongSubmerged: SingleModelTest
{
    public GuangdongSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void Preprocess()
    {
        RunWithConsole = true;
        string inputDir = Path.Combine(modelInfo.TestPath, "Templates", Path.GetFileName(modelInfo.TestPath),"results");
        Directory.CreateDirectory(inputDir);
        Tools.CopyDirectory(Path.Combine(modelInfo.TestPath, "results"), inputDir, true);
        modelInfo.InputPath = Path.Combine(inputDir, "xaj_para", modelInfo.InputFile);
        modelInfo.OutputPath = Path.Combine(modelInfo.OutputPath,Path.GetFileName(modelInfo.TestPath),"results",Path.GetFileName(modelInfo.TestPath)+"_depth");
    }

    public override void ModifyConfigFiles()
    {
        string[] lines = File.ReadAllLines(Path.Combine(modelInfo.TestPath,"config.yml"));
        lines[5] = "  base_folder: ./Templates/";
        File.WriteAllLines(Path.Combine(modelInfo.TestPath,"config.yml"), lines);
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
                if (j == 0 || j == 1)
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