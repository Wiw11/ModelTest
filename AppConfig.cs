namespace ModelTest;

using Microsoft.Extensions.Configuration;

public class AppConfig
{
    public string HomePath { get; set; } = string.Empty;
    public string SourceRoot { get; set; } = string.Empty;
    public string PythonCommand { get; set; } = string.Empty;

    public AppConfig()
    {
        ReadFromConfig();
    }

    public void ReadFromConfig()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("application.json", optional: false, reloadOnChange: true);

        IConfigurationRoot configuration = builder.Build();

        HomePath = configuration["HomePath"] ?? string.Empty;
        SourceRoot = configuration["SourceRoot"] ?? string.Empty;
        PythonCommand = configuration["PythonCommand"] ?? string.Empty;
    }
}
