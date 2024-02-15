using System.Collections.Generic;
using Codice.Client.Common;

namespace Codice.Client.IssueTracker.ClickUp
{
    public class ClickUpExtensionFactory : IPlasticIssueTrackerExtensionFactory
    {
        private List<IssueTrackerConfigurationParameter> _defaultParameters;

        public IssueTrackerConfiguration GetConfiguration(IssueTrackerConfiguration storedConfiguration) =>
            IssueTrackerConfigurationValidator.UpgradeStoredConfigurationWithDefaultParams(storedConfiguration, GetDefaultParameters());

        public IPlasticIssueTrackerExtension GetIssueTrackerExtension(IssueTrackerConfiguration configuration)
        {
            return new ClickUpExtension(configuration);
        }

        public string GetIssueTrackerName() => "ClickUp";

        private List<IssueTrackerConfigurationParameter> GetDefaultParameters()
        {
            if (_defaultParameters != null)
                return _defaultParameters;
            _defaultParameters = new List<IssueTrackerConfigurationParameter>
            {
                new(ClickUpExtension.PersonalTokenKey, string.Empty,
                    IssueTrackerConfigurationParameterType.Password, true),
                new(ClickUpExtension.BranchPrefixKey, "scm",
                    IssueTrackerConfigurationParameterType.BranchPrefix, false),
                new(ClickUpExtension.TeamId, "",
                    IssueTrackerConfigurationParameterType.Text, false),
                new(ClickUpExtension.SpaceId, "",
                    IssueTrackerConfigurationParameterType.Text, false)
            };
            return _defaultParameters;
        }
    }
}