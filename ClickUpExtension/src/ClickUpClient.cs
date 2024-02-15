using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Codice.Client.IssueTracker.ClickUpExtension.Model;
using Codice.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codice.Client.IssueTracker.ClickUp;

public class ClickUpClient
{
    private const string BaseUri = "https://app.clickup.com/api/v2";
    private readonly string _encryptedPersonalToken;
    private readonly string _teamId;
    private readonly string _spaceId;
    private readonly HttpClient _httpClient;

    public ClickUpClient(string encryptedPersonalToken, string teamId, string spaceId)
    {
        _httpClient = new HttpClient();
        _encryptedPersonalToken = encryptedPersonalToken;
        _teamId = teamId;
        _spaceId = spaceId;
    }

    internal async Task<PlasticTask> GetTask(string taskId, bool useCustomTaskId = false, string teamId = "", bool includeSubTasks = false)
    {
        if (string.IsNullOrEmpty(taskId))
            throw new ArgumentException("taskId cannot be null or empty");

        var requestUrl = $"{BaseUri}/task/{taskId}?custom_task_ids={useCustomTaskId}&team_id={teamId}&include_subtasks={includeSubTasks}";
        var response = await MakeHttpCall(requestUrl, HttpMethod.Get);
        var responseString = await response.Content.ReadAsStringAsync();

        try
        {
            return JsonConvert.DeserializeObject<ClickUpTask>(responseString).ConvertToPlasticTask();
        }
        catch (Exception e)
        {
            ClickUpExtension.Logger.ErrorFormat("Could not deserialize tasks: {0}", e.Message);
            ClickUpExtension.Logger.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
            throw;
        }
    }

    internal async Task<List<PlasticTask>> GetTasks(List<string> taskIds)
    {
        ClickUpExtension.Logger.Info($"Getting tasks with ids : {string.Join(", ", taskIds)}");
        
        var page = 0;
        var isLastPage = false;
        List<ClickUpTask> clickUpTasks = new();
        do
        {
            var requestUrl = BaseUri + $"/team/{_teamId}/task?page={page}&space_ids[]={_spaceId}";
            var response = await MakeHttpCall(requestUrl, HttpMethod.Get);
            var responseString = await response.Content.ReadAsStringAsync();

            try
            {
                var jObject = JObject.Parse(responseString);
                if (jObject.TryGetValue("tasks", out var jToken))
                {
                    clickUpTasks.AddRange(jToken.ToObject<ClickUpTask[]>());
                }

                if (jObject.TryGetValue("last_page", out jToken))
                {
                    isLastPage = jToken.ToObject<bool>();
                }

                page++;
            }
            catch (Exception e)
            {
                ClickUpExtension.Logger.ErrorFormat("Could not deserialize tasks: {0}", e.Message);
                ClickUpExtension.Logger.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
                throw;
            }
        } while (!isLastPage);
        
        return clickUpTasks.Select(clickUpTask => clickUpTask.ConvertToPlasticTask()).Where(clickUpTask => taskIds.Contains(clickUpTask.Id)).ToList();
    }

    internal async Task<ClickUpUserInfo> GetAuthorizedUser()
    {
        ClickUpExtension.Logger.Info("Start getting authorized user info");
        var request = ConstructAuthorizedHttpRequest(BaseUri + "/user", HttpMethod.Get);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        try
        {
            var jObject = JObject.Parse(responseString);
            var hasUser = jObject.TryGetValue("user", out var jToken);
            var user = !hasUser ? null : jToken.ToObject<ClickUpUserInfo>();
            if (hasUser)
                ClickUpExtension.Logger.Info($"Authorized user: {user.UserName}, {user.Email} with ID: {user.Id}");
            return user;
        }
        catch (Exception e)
        {
            ClickUpExtension.Logger.ErrorFormat("Could not deserialize authorized user: {0}", e.Message);
            ClickUpExtension.Logger.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
            throw;
        }
    }

    internal async Task<bool> TestConnection()
    {
        if (string.IsNullOrEmpty(_spaceId))
        {
            var request = ConstructAuthorizedHttpRequest(BaseUri + "/user", HttpMethod.Get);
            try
            {
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                ClickUpExtension.Logger.ErrorFormat("Could not establish test connection: {0}", e.Message);
                ClickUpExtension.Logger.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
                throw;
            }
        }
        else
        {
            var request = ConstructAuthorizedHttpRequest(BaseUri + $"/space/{_spaceId}", HttpMethod.Get);
            try
            {
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception e)
            {
                ClickUpExtension.Logger.ErrorFormat("Could not establish test connection to space {0}: {1}", _spaceId, e.Message);
                ClickUpExtension.Logger.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
                throw;
            }
        }
    }

    internal async Task<List<PlasticTask>> GetPendingTasks(string assignee = "")
    {
        var logMessage = "Start get pending tasks";
        if (!string.IsNullOrEmpty(assignee))
            logMessage = string.Concat(logMessage, $" with user id {assignee}");
        ClickUpExtension.Logger.Info(logMessage);
        var page = 0;
        var isLastPage = false;
        List<ClickUpTask> clickUpTasks = new();
        do
        {
            var requestUrl = BaseUri + $"/team/{_teamId}/task?statuses[]=open&statuses[]=in%20progress&page={page}&space_ids[]={_spaceId}&include_closed=false";
            if (!string.IsNullOrEmpty(assignee))
                requestUrl = string.Concat(requestUrl, $"&assignees[]={assignee}");

            var response = await MakeHttpCall(requestUrl, HttpMethod.Get);
            var responseString = await response.Content.ReadAsStringAsync();

            try
            {
                var jObject = JObject.Parse(responseString);
                if (jObject.TryGetValue("tasks", out var jToken))
                {
                    clickUpTasks.AddRange(jToken.ToObject<ClickUpTask[]>());
                }

                if (jObject.TryGetValue("last_page", out jToken))
                {
                    isLastPage = jToken.ToObject<bool>();
                }

                page++;
            }
            catch (Exception e)
            {
                ClickUpExtension.Logger.ErrorFormat("Unable to deserialize pending taks: {0}", e.Message);
                ClickUpExtension.Logger.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
                throw;
            }
        } while (!isLastPage);


        return clickUpTasks.Select(clickUpTask => clickUpTask.ConvertToPlasticTask()).ToList();
    }
    
    internal void MarkTaskAsOpen(string taskId, string assignee)
    {
        try
        {
            var obj = new
            {
                status = "in progress",
                assignees = new
                {
                    add = new[] { int.Parse(assignee) }
                }
            };

            var jObj = JsonConvert.SerializeObject(obj);
            var postData = new StringContent(jObj, Encoding.UTF8, "application/json");
            var requestUrl = string.Concat(BaseUri, "/task/", taskId);
            var request = ConstructAuthorizedHttpRequest(requestUrl, HttpMethod.Put);
            request.Content = postData;
            MakeHttpCall(request).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            ClickUpExtension.Logger.ErrorFormat("Unable to open task '{0}' and assign it to user '{1}': {2}", taskId, assignee, ex.Message);
            ClickUpExtension.Logger.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, ex.StackTrace);
            throw;
        }
    }

    #region private

    private string GetDecryptedToken()
    {
        return !string.IsNullOrEmpty(_encryptedPersonalToken) ? CryptoServices.GetDecryptedPassword(_encryptedPersonalToken) : string.Empty;
    }

    private HttpRequestMessage ConstructAuthorizedHttpRequest(string requestUrl, HttpMethod httpMethod)
    {
        if (string.IsNullOrEmpty(requestUrl))
        {
            ClickUpExtension.Logger.Error($"Invalid requestUrl: {requestUrl ?? string.Empty}");
            throw new ArgumentException("requestUrl is NULL or empty");
        }

        var request = new HttpRequestMessage(httpMethod, requestUrl);
        request.Headers.Add("Authorization", GetDecryptedToken());
        return request;
    }

    private async Task<HttpResponseMessage> MakeHttpCall(HttpRequestMessage request)
    {
        try
        {
            var msg = $"Executing HTTP {request.Method} request: {request.RequestUri}";
            if (request.Content != null)
                msg = string.Concat("\n", $"with content: \n{request.Content.ReadAsStringAsync().Result}");
            ClickUpExtension.Logger.Debug(msg);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (Exception e)
        {
            ClickUpExtension.Logger.ErrorFormat("Unable to make {0} call to '{1}' : {2}", request.Method, request.RequestUri, e.Message);
            ClickUpExtension.Logger.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
            throw;
        }
    }

    private async Task<HttpResponseMessage> MakeHttpCall(string requestUrl, HttpMethod httpMethod)
    {
        try
        {
            var request = ConstructAuthorizedHttpRequest(requestUrl, httpMethod);
            ClickUpExtension.Logger.Debug($"Executing HTTP {httpMethod} request: {requestUrl}");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (Exception e)
        {
            ClickUpExtension.Logger.ErrorFormat("Unable to make {0} call to '{1}' : {2}", httpMethod, requestUrl, e.Message);
            ClickUpExtension.Logger.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
            throw;
        }
    }

    #endregion
}