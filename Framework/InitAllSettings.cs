using System.Collections.Generic;
using UiPath.CodedWorkflows;

namespace RoboticEnterpriseFrameworkcoded
{
    public class InitAllSettings : CodedWorkflow
    {
        [Workflow]
        public Dictionary<string, object> Execute(string in_ConfigFile, string[] in_ConfigSheets)
        {
            return LoadConfiguration(in_ConfigFile, in_ConfigSheets);
        }
    }
}
