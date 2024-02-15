using System;
using System.Collections.Generic;
using System.Diagnostics;
using Codice.Client.IssueTracker.ClickUpExtension.Model;
using log4net;

namespace Codice.Client.IssueTracker.ClickUp;

public class ClickUpExtension : IPlasticIssueTrackerExtension
{
    private const string ExtensionName = "ClickUp";
    internal const string PersonalTokenKey = "Personal API Key";
    internal const string BranchPrefixKey = "Branch prefix";
    internal const string TeamId = "Team Id";
    internal const string SpaceId = "Space Id";

    internal static readonly ILog Logger = LogManager.GetLogger(ExtensionName);

    private ClickUpUserInfo _authorizedUser;
    private readonly IssueTrackerConfiguration _config;
    private readonly ClickUpClient _clickUpClient;

    internal ClickUpExtension(IssueTrackerConfiguration config)
    {
        _config = new IssueTrackerConfiguration
        {
            Parameters = config.GetAllParameters().ToArray(),
            WorkingMode = config.WorkingMode
        };
        _clickUpClient = new ClickUpClient(_config.GetValue(PersonalTokenKey), _config.GetValue(TeamId),
            _config.GetValue(SpaceId));
    }

    public string GetExtensionName() => ExtensionName;

    #region not supported

    public void LogCheckinResult(PlasticChangeset changeset, List<PlasticTask> tasks)
    {
        //not supported
    }

    public void UpdateLinkedTasksToChangeset(PlasticChangeset changeset, List<string> tasks)
    {
        //not supported
    }

    #endregion

    #region Connection

    public void Connect()
    {
        Logger.Info(ExtensionName + "extension is connecting...");
        _clickUpClient.GetAuthorizedUser().ContinueWith(task => _authorizedUser = task.GetAwaiter().GetResult());
    }

    public void Disconnect()
    {
        Logger.Info(ExtensionName + "extension is disconnecting...");
        _authorizedUser = null;
    }

    public bool TestConnection(IssueTrackerConfiguration configuration)
    {
        return _clickUpClient.TestConnection().GetAwaiter().GetResult();
    }

    #endregion

    #region Task Utility

    public List<PlasticTask> LoadTasks(List<string> taskIds) => _clickUpClient.GetTasks(taskIds).GetAwaiter().GetResult();

    public List<PlasticTask> GetPendingTasks() => _clickUpClient.GetPendingTasks().GetAwaiter().GetResult();

    public List<PlasticTask> GetPendingTasks(string assignee)
    {
        if (_authorizedUser != null && _authorizedUser.Email.Equals(assignee))
        {
            Logger.Info($"Start get pending tasks with assignee {assignee}");
            return _clickUpClient.GetPendingTasks(_authorizedUser.Id).GetAwaiter().GetResult();
        }

        Logger.Error($"No authorized user found for {assignee}.");
        return new List<PlasticTask>();
    }

    public void MarkTaskAsOpen(string taskId, string assignee)
    {
        if (_authorizedUser != null)
        {
            Logger.Info($"Changing status of task {taskId} to 'in progress' and assigning {assignee}");
            _clickUpClient.MarkTaskAsOpen(taskId, _authorizedUser.Id);
        }
        else
        {
            Logger.Error($"No authorized user found for {assignee}.");
        }
    }

    public PlasticTask GetTaskForBranch(string fullBranchName)
    {
        var taskId = GetTaskIdFromBranchName(GetBranchName(fullBranchName), _config);

        return taskId.Equals(string.Empty) ? default : _clickUpClient.GetTask(taskId).GetAwaiter().GetResult();
    }

    public Dictionary<string, PlasticTask> GetTasksForBranches(List<string> fullBranchNames)
    {
        var resultDictionary = new Dictionary<string, PlasticTask>();

        foreach (var fullBranchName in fullBranchNames)
        {
            var taskId = GetTaskIdFromBranchName(GetBranchName(fullBranchName), _config);
            resultDictionary.Add(fullBranchName, taskId.Equals(string.Empty) ? default : _clickUpClient.GetTask(taskId).GetAwaiter().GetResult());
        }

        return resultDictionary;
    }

    public void OpenTaskExternally(string taskId)
    {
        try
        {
            var target = $"clickup://t/{taskId}";
            Logger.Info($"Attempting to open task {taskId} ClickUp desktop app or default browser");
            if (_config == null)
                return;
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch (System.ComponentModel.Win32Exception noBrowser)
        {
            if (noBrowser.ErrorCode == -2147467259)
                Logger.Error(noBrowser.Message);
        }
        catch (Exception e)
        {
            Logger.ErrorFormat("Could not open task with id {0}: {1}", taskId, e.Message);
            Logger.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
            throw;
        }
    }

    #endregion


    private static string GetBranchName(string fullBranchName)
    {
        if (fullBranchName == "main")
            return string.Empty;

        var lastSeparatorIndex = fullBranchName.LastIndexOf('/');


        if (lastSeparatorIndex < 0)
            return fullBranchName;

        return lastSeparatorIndex == fullBranchName.Length - 1 ? string.Empty : fullBranchName[(lastSeparatorIndex + 1)..];
    }

    private static string GetTaskIdFromBranchName(string branchName, IssueTrackerConfiguration config)
    {
        if (config == null)
            return string.Empty;

        var prefix = config.GetValue(BranchPrefixKey);
        if (string.IsNullOrEmpty(prefix))
            return branchName;

        if (!branchName.StartsWith(prefix) || branchName == prefix)
        {
            return string.Empty;
        }

        return branchName[prefix.Length..];
    }
}