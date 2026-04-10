namespace ModelTest;

public class Tools
{
    public static string GetParentPath(string filePath)
    {
        var splitPath = filePath.Split('\\');
        return string.Join("\\", splitPath.Take(splitPath.Length - 1));
    }

    public static string? GetSpecialLevelFiles(string directoryPath, string targetFileName)
    {
        var files = Directory.GetFiles(directoryPath, targetFileName, SearchOption.AllDirectories);
        
        if (files.Length == 0)
        {
            Console.WriteLine($"在指定路径下没有找到目标文件: {targetFileName}");
            return null;
        }
        
        return GetParentPath(files[0]);
    }

    public static void CopyDirectory(string sourceDir, string destDir, bool recursive)    // 复制文件夹
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            return;

        DirectoryInfo[] dirs = dir.GetDirectories();

        Directory.CreateDirectory(destDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
                foreach (FileInfo file in subDir.GetFiles())
                {
                    string targetFilePath = Path.Combine(destDir, file.Name);
                    file.CopyTo(targetFilePath, true);
                }
            }
        }
    }

    public static void PostRequest(string url, string jsonPath)     // 发送POST请求
    {
        using (var client = new HttpClient())
        {
            var content = new StringContent(File.ReadAllText(jsonPath), System.Text.Encoding.UTF8, "application/json");
            var response = client.PostAsync(url, content).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"请求失败，状态码: {response.StatusCode}");
            }
        }
    }
}