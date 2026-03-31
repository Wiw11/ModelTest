namespace ModelTest;

public class GuizhouHydrodynamic: SingleModelTest
{
    public GuizhouHydrodynamic(double multiple, ModelInfo model) : base(multiple, model)
    {
    }

    public override void Preprocess()
    {
        File.WriteAllText(Path.Combine(modelInfo.TestPath, "run.bat"), $"{modelInfo.MainPath} {modelInfo.TestPath}\\programmeinfo.json");
        modelInfo.MainPath = Path.Combine(modelInfo.TestPath, "run.bat");
    }

}