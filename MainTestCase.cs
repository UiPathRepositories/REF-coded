using UiPath.CodedWorkflows;

namespace RoboticEnterpriseFrameworkcoded
{
    public class MainTestCase : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            var config = LoadConfiguration("Data\\Config.xlsx", ["Settings", "Constants"]);
            testing.VerifyExpression(config.Count > 0, "Main prerequisites should load configuration.");
        }
    }
}
