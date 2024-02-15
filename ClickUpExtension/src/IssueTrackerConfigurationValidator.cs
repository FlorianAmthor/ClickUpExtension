using Codice.Client.IssueTracker;
using System;
using System.Collections.Generic;

namespace Codice.Client.Common
{
    public static class IssueTrackerConfigurationValidator
    {
        public static IssueTrackerConfiguration UpgradeStoredConfigurationWithDefaultParams(IssueTrackerConfiguration storedConfiguration,
            List<IssueTrackerConfigurationParameter> defaultParameters)
        {
            if (storedConfiguration == null)
                return new IssueTrackerConfiguration(ExtensionWorkingMode.TaskOnBranch, defaultParameters);
            var parameters = new List<IssueTrackerConfigurationParameter>(defaultParameters);
            foreach (var allParameter in storedConfiguration.GetAllParameters())
            {
                IssueTrackerConfigurationParameter parameterByName = GetParameterByName(parameters, allParameter.Name);
                if (parameterByName != null)
                    parameterByName.Value = allParameter.Value;
            }

            return new IssueTrackerConfiguration(storedConfiguration.WorkingMode, parameters);
        }

        private static IssueTrackerConfigurationParameter GetParameterByName(List<IssueTrackerConfigurationParameter> parameters, string name)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return parameter;
            }

            return null;
        }
    }
}