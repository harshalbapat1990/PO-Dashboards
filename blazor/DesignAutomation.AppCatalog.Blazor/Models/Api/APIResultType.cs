using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum APIResultType
{
    Success = 0,
    FirstLogin = 1,
    ResetPassword = 2,
    Disabled = 3,
    UserIDTaken = 4,
    EmailTaken = 5,
    InvalidPWFormat = 6,
    InvalidUserIDFormat = 7,
    InvalidEmailFormat = 8,
    InvalidEnvironment = 9,
    InvalidCredentials = 10,
    IncorrectPW = 11,
    UserIDNotFound = 12,
    InvalidDomain = 13,
    AuthCodeNotFound = 14,
    NoAccessToken = 15,
    NoAccessProfile = 16,
    InsertFailed = 20,
    UpdateFailed = 21,
    DeleteFailed = 22,
    UpdateStale = 23,
    InsertOrUpdateFailed = 24,
    InvalidInput = 25,
    NoResults = 26,
    FailedRule = 27,
    NoMatch = 28,
    TooLarge = 29,
    InvalidName = 30,
    Unauthorized = 31,
    ExceededQuota = 32,
    IllegalAnonymousRequest = 33,
    Offline = 90,
    Down = 98,
    Exception = 99,
    Unknown = 100
}
