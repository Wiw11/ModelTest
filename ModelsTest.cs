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
        lines[2] = $"set FILENAME={modelInfo.TestPath}\\MAIN.cas";
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

public class GuangxiHydrodynamic: SingleModelTest
{
    public GuangxiHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
        modelInfo.SourcePath = @"\\192.168.9.107\model_test\2024年广西水动力学模型成果\2024年度水动力模型建设成果";
    }

    public override void Preprocess()
    {
        RunWithConsole = true;
        // File.Copy(Path.Combine(modelInfo.TestPath,"lj007（网格面）.json"),Path.Combine(modelInfo.InputPath,"lj007（网格面）.json"),true);
    }

    public override void ModifyFlowFiles()
    {
        // string jsonFile = Path.Combine(modelInfo.InputPath, "lj007.json");
        string jsonContent = File.ReadAllText(modelInfo.InputPath);
        JsonNode? node = JsonNode.Parse(jsonContent);

        if (node != null && node["inflow"] != null)
        {
            foreach (var q in node["inflow"]!.AsArray())
            {
                if (q != null && q["data"] != null)
                {
                    var dataArray = q["data"]!.AsArray();
                    var newData = new JsonArray();
                    foreach (var item in dataArray)
                    {
                        if (item != null)
                        {
                            double value = item.GetValue<double>() * Multiple;
                            newData.Add(value);
                        }
                    }
                    q["data"] = newData;
                }
            }
        }

        // 保存
        File.WriteAllText(modelInfo.InputPath, node!.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public override void ModifyConfigFiles()
    {
        string[] lines = File.ReadAllLines(Path.Combine(modelInfo.TestPath,"setting","config.ini"));
        lines[3] = $"TELEMAC_SOFT = {modelInfo.TestPath.Replace("\\", "/")}/soft/V8P4/configs/pysource.bat";
        lines[5] = $"MODEL_FOLDER = {modelInfo.TestPath.Replace("\\", "/")}/data/config/{{configCode}}";
        lines[8] = $"SIMPLE_PATH = param/lj007.json";
        File.WriteAllLines(Path.Combine(modelInfo.TestPath,"setting","config.ini"), lines);
    }
}

public class JiangxiHydrodynamic: SingleModelTest
{
    public JiangxiHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void ExecutableCore()
    {
        var pythonScript = Path.Combine(modelInfo.HomePath, "post_request.py");
        string arguments = $"{pythonScript} \"{modelInfo.InputPath}\""; 
        AppConfig appConfig = new AppConfig();
        // 发送请求
        ExternalProcessRunner.Run(appConfig.PythonCommand, arguments);

        // 复制结果
        string jsonContent = File.ReadAllText(modelInfo.InputPath);
        JsonNode? node = JsonNode.Parse(jsonContent);
        string result_name = node["taskid"]!.GetValue<string>();    // 获取文件名
        string result_file = Path.Combine(Tools.GetParentPath(modelInfo.TestPath),".running",$"{result_name}.txt");
        File.Copy(result_file, Path.Combine(modelInfo.OutputPath, Path.GetFileName(result_file)), true);
    }

    public override void ModifyFlowFiles()
    {
        string jsonContent = File.ReadAllText(modelInfo.InputPath);
        JsonNode? node = JsonNode.Parse(jsonContent);


        if (node != null && node["hmqList"] != null)
        {
            foreach (var q in node["hmqList"]!.AsArray())
            {
                if (q != null && q["q"] != null)
                {
                    double[] q_list = q["q"]!.GetValue<string>().Split(',').Select(double.Parse).ToArray();
                    string new_q = "";
                    foreach (var item in q_list)
                    {
                        double mul_q = item * Multiple;
                        new_q += mul_q + ",";
                    }
                    q["q"] = new_q.TrimEnd(',');
                }
            }
        }

        // 保存
        File.WriteAllText(
            modelInfo.InputPath, 
            node!.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })
        );
    }
}

public class JilinHydrodynamic: JiangxiHydrodynamic
{
    public JilinHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
        modelInfo.SourcePath = @"u:\inspur\model_test\0520\附件8 水动力学模型-吉林-0520\附件8 水动力学模型-吉林-0520\2023年度水动力模型建设成果";
    }
}

public class HenanHydrodynamic: JiangxiHydrodynamic
{
    public HenanHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {       
    }
}

public class HubeiHydrodynamic: SingleModelTest
{
    public HubeiHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
        modelInfo.TestPath = Path.Combine(modelInfo.TestPath, "src");
    }

    public override void Preprocess()
    {
        Directory.CreateDirectory(Path.Combine(Tools.GetParentPath(modelInfo.TestPath), "ProcessData"));
        foreach (var file in Directory.GetFiles(modelInfo.TestPath,"*"))
        {
            File.Copy(file, Path.Combine(Tools.GetParentPath(modelInfo.TestPath), "ProcessData", Path.GetFileName(file)), true);
        }

        modelInfo.TestPath = Tools.GetParentPath(modelInfo.TestPath);

        string content = "cd .\\src\nwsl ./main";
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "run.bat"), content);
        modelInfo.MainPath = Path.Combine(modelInfo.TestPath, "run.bat");
    }

    public override void ModifyFlowFiles()
    {
        foreach (var file in Directory.GetFiles(modelInfo.InputPath, "Inflow*"))
        {
            string[] lines = File.ReadAllLines(file);
            string[] newLines = new string[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 1)
                {
                    newLines[i] = lines[i].Split(',')[0] + "," + double.Parse(lines[i].Split(',')[1]) * Multiple;
                }
                else
                {
                    newLines[i] = lines[i];
                }
            }
            File.WriteAllLines(file, newLines);
        }
    }
}

public class HunanHydrodynamic: HubeiHydrodynamic
{
    public HunanHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
    }
}

public class YunnanHydrodynamic: HubeiHydrodynamic
{
    public YunnanHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
    }
}

public class BingtuanHydrodynamic: SingleModelTest
{
    public BingtuanHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void ModifyConfigFiles()
    {
        string jsonFile = Path.Combine(modelInfo.TestPath, "appsettings.json");
        string jsonArray = File.ReadAllText(jsonFile);
        JsonNode? node = JsonNode.Parse(jsonArray);

        if (node != null && node["prjFile"] != null)
        {
            node["prjFile"] = Path.Combine(modelInfo.TestPath, @"qibenhe2D\data\qibenhe2D","model.dat").Replace("\\", "/");
        }
        if (node != null && node["outputout"] != null)
        {
            node["outputout"] = Path.Combine(modelInfo.TestPath, @"data\qibenhe2D","output.txt").Replace("\\", "/");
        }

        File.WriteAllText(jsonFile, node!.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public override void ModifyFlowFiles()
    {
        // 读取文件内容
        var lines = File.ReadAllText(modelInfo.InputPath, System.Text.Encoding.UTF8);
        
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
        File.WriteAllText(modelInfo.InputPath, newContent, System.Text.Encoding.Default);
    }

    public override void Postprocess()
    {
        modelInfo.OutputPath = Path.Combine(modelInfo.TestPath, @"data\qibenhe2D\output\txt");
    }
}

public class GansuHydrodynamic: AnhuiHydrodynamic
{
    public GansuHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {}

    public override void Preprocess()
    {
        Directory.CreateDirectory(Path.Combine(modelInfo.TestPath, "Template"));
        Tools.CopyDirectory(Tools.GetParentPath(modelInfo.InputPath), Path.Combine(modelInfo.TestPath, "Template"), true);
    }
}

public class BeijingHydrodynamic: JiangxiHydrodynamic
{
    public BeijingHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
        modelInfo.SourcePath = @"\\192.168.9.107\model_test\0608\2024年度北京市水动力模型建设成果\2024年度水动力模型建设成果\白马关水动力模型";
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
        string zqValue = "sections = \"./HIWHF65003J0000000_469025101000000.geojson\"\nsigmac = \"./sigmac.dat\"\nm = 1.0\n[license]\nlicense = \"./License.lic\"\nkey =\"MIVpbhegaV+SZVEK+YWOnui4tkfObRUs06eDmxKcytc=\"";
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "zq.conf"), zqValue);
        string inundateValue = "sections = \"./HIWHF65003J0000000_469025101000000.geojson\"\nzq = \"./results/HIWHF65003J0000000_469025101000000_ZQ.CSV\"\nsection_flow = \"./输入数据样例_Q.csv\"\n" + 
            "[license]\nlicense = \"./License.lic\"\nkey =\"MIVpbhegaV+SZVEK+YWOnui4tkfObRUs06eDmxKcytc=\"";
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "inundate.conf"), inundateValue);
    }
}

public class QinghaiSubmerged: GansuSubmerged
{
    public QinghaiSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
        modelInfo.SourcePath = @"\\192.168.9.109\model_test\附件8-简化淹没范围与水深分析样例-青海省（第二次提交）\附件8-简化淹没范围与水深分析样例-青海省\2024年度简化淹没范围与水深分析模型建设成果\简化淹没范围与水深分析模型建设成果";
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

public class ChongqingSubmerged: SingleModelTest
{
    public ChongqingSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void Preprocess()
    {
        Tools.CopyDirectory(Path.Combine(modelInfo.TestPath, "release_test_workdir","input"), Path.Combine(modelInfo.TestPath, "input"), true);

        modelInfo.MainPath = Path.Combine(modelInfo.TestPath, "run.bat");
    }

    public override void ModifyFlowFiles()
    {
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "run.bat"), 
            $"chcp 65001\n\n\n\n.\\simplifiedinundate.exe --name \"大溪河\" --design-flow {CurrentMultiple * 100}");
    }
}

public class JiangxiSubmerged: SingleModelTest
{
    public JiangxiSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
        modelInfo.SourcePath = @"\\192.168.9.106\model_test\江西简化淹没\2024年度简化淹没范围与水深分析模型建设成果";
    }

    public override void ExecutableCore()
    {
        // 重新定位输出目录
        modelInfo.OutputPath = modelInfo.OutputDir.Replace("..", Tools.GetParentPath(modelInfo.TestPath));

        // 运行前需手动启动.running文件夹中的服务
        // 发送请求
        string jsonFile = Directory.GetFiles(modelInfo.TestPath, "*.json", SearchOption.TopDirectoryOnly)[0];
        Tools.PostRequest("http://127.0.0.1:8087/jlSimpleModel/simple/model/getParseData", jsonFile);
    }

    public override void ModifyFlowFiles()
    {
        string jsonFile = Directory.GetFiles(modelInfo.TestPath, "*.json", SearchOption.TopDirectoryOnly)[0];
        string jsonContent = File.ReadAllText(jsonFile);
        JsonNode? node = JsonNode.Parse(jsonContent);


        if (node != null && node["list"] != null)
        {
            foreach (var q in node["list"]!.AsArray())
            {
                if (q != null && q["maxq"] != null)
                {
                    q["maxq"] = q["maxq"]!.GetValue<double>() * Multiple; 
                }
            }
        }

        // 保存
        File.WriteAllText(jsonFile, node!.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}

public class JilinSubmerged: SingleModelTest
{
    public JilinSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
        modelInfo.SourcePath = @"\\192.168.9.109\model_test\附件8 简化淹没与水深分析模型-吉林20260520提交";
    }

    public override void CopyExecutables()
    {
    }

    public override void CopyShapeFiles()
    {
        if (! Directory.Exists(modelInfo.GisPath))
        {
            Directory.CreateDirectory(modelInfo.GisPath);
        }
    }

    public override void ExecutableCore()
    {
        // 重新定位输出目录
        modelInfo.OutputPath = modelInfo.OutputDir.Replace("..", Tools.GetParentPath(modelInfo.TestPath));

        // 运行前需手动启动.running文件夹中的服务
        // 发送请求
        string jsonFile = Directory.GetFiles(modelInfo.TestPath, "*.json", SearchOption.TopDirectoryOnly)[0];

        try
        {
            Tools.PostRequest("http://127.0.0.1:9000/jlSimpleModel/simple/model/getParseData", jsonFile);
        }
        catch (Exception)
        {
            IsSuccess = false;
        }
    }

    public override void Preprocess()
    {
        foreach (var file in Directory.GetFiles(modelInfo.TestPath, "*.geojson", SearchOption.TopDirectoryOnly))
        {
            File.Copy(file, Path.Combine(Tools.GetParentPath(modelInfo.TestPath), ".running", "input", Path.GetFileName(file)), true);
        }
    }

    public override void ModifyFlowFiles()
    {
        string jsonFile = Directory.GetFiles(modelInfo.TestPath, "*.json", SearchOption.TopDirectoryOnly)[0];
        string jsonContent = File.ReadAllText(jsonFile);
        JsonNode? node = JsonNode.Parse(jsonContent);

        if (node is JsonArray array && array.Count > 0 && array[0] is JsonObject firstNode && firstNode["list"] is JsonArray list)
        {
            foreach (var q in list)
            {
                if (q is JsonObject item && item["maxq"] is JsonNode maxqNode)
                {
                    item["maxq"] = maxqNode.GetValue<double>() * Multiple;
                }
            }
        }

        // 保存
        if (node != null)
        {
            File.WriteAllText(jsonFile, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public override void Postprocess()
    {   
        modelInfo.OutputPath = Tools.GetBottomDirectory(modelInfo.OutputPath);
    }
}

public class HainanSubmerged: GansuSubmerged
{
    public HainanSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {}
}

public class XinjiangSubmerged: SingleModelTest
{
    public XinjiangSubmerged(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

}