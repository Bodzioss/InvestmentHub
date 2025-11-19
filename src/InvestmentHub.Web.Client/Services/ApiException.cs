using System.Net;

namespace InvestmentHub.Web.Client.Services;

/// <summary>
/// Exception wrapper for API errors with user-friendly messages
/// </summary>
public class InvestmentHubApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ResponseContent { get; }

    public InvestmentHubApiException(HttpStatusCode statusCode, string message, string? responseContent = null) 
        : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }

    public InvestmentHubApiException(HttpStatusCode statusCode, string message, Exception innerException) 
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Get user-friendly error message based on status code
    /// </summary>
    public string GetUserFriendlyMessage()
    {
        return StatusCode switch
        {
            HttpStatusCode.BadRequest => "Invalid request. Please check your input and try again.",
            HttpStatusCode.Unauthorized => "You are not authorized. Please login again.",
            HttpStatusCode.Forbidden => "You don't have permission to perform this action.",
            HttpStatusCode.NotFound => "The requested resource was not found.",
            HttpStatusCode.Conflict => "This operation conflicts with existing data.",
            HttpStatusCode.InternalServerError => "A server error occurred. Please try again later.",
            HttpStatusCode.ServiceUnavailable => "The service is temporarily unavailable. Please try again later.",
            _ => $"An error occurred ({(int)StatusCode}). Please try again."
        };
    }
}

