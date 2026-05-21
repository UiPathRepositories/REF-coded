using UiPath.CodedWorkflows;

namespace RoboticEnterpriseFrameworkcoded
{
    public class TakeScreenshot : CodedWorkflow
    {
        [Workflow]
        public string Execute(string in_Folder, string io_FilePath)
        {
            return TakeScreenshotCore(in_Folder, io_FilePath);
        }
    }
}
