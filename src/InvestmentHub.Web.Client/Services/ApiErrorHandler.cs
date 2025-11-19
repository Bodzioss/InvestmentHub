using Refit;
using System.Net;

namespace InvestmentHub.Web.Client.Services;

/// <summary>
/// Handler for API errors - converts Refit exceptions to InvestmentHubApiException
/// </summary>
public static class ApiErrorHandler
{
    /// <summary>
    /// Handle API exception and throw user-friendly InvestmentHubApiException
    /// </summary>
    public static async Task<T> HandleApiCall<T>(Func<Task<T>> apiCall)
    {
        try
        {
            return await apiCall();
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvestmentHubApiException(
                HttpStatusCode.Unauthorized,
                "Your session has expired. Please login again.",
                ex);
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            var content = await GetResponseContent(ex);
            throw new InvestmentHubApiException(
                HttpStatusCode.NotFound,
                "The requested resource was not found.",
                content);
        }
        catch (Refit.ApiException ex)
        {
            var message = await TryGetErrorMessage(ex);
            var content = await GetResponseContent(ex);
            throw new InvestmentHubApiException(
                ex.StatusCode,
                message ?? "An error occurred while communicating with the server.",
                content);
        }
        catch (HttpRequestException ex)
        {
            throw new InvestmentHubApiException(
                HttpStatusCode.ServiceUnavailable,
                "Unable to connect to the server. Please check your connection.",
                ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new InvestmentHubApiException(
                HttpStatusCode.RequestTimeout,
                "The request timed out. Please try again.",
                ex);
        }
    }

    private static Task<string?> GetResponseContent(Refit.ApiException ex)
    {
        try
        {
            return Task.FromResult<string?>(ex.Content);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    private static async Task<string?> TryGetErrorMessage(Refit.ApiException ex)
    {
        try
        {
            // Try to extract error message from response
            var content = await GetResponseContent(ex);
            if (!string.IsNullOrEmpty(content))
            {
                // Here you could parse JSON error response if your API returns structured errors
                return content;
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }
}

