namespace DesignAutomation.AppCatalog.Model;

public enum APIResultType
{
	/// <summary>Successful results. Carry on.</summary>
	Success = 0,
	/// <summary>First time the user has ever logged in so they may need to set their UserID and Password.</summary>
	FirstLogin = 1,
	/// <summary>The user has to reset their password before they do anything.</summary>
	ResetPassword = 2,
	/// <summary>The user's account is disabled either by an admin or because of too many failed login attempts</summary>
	Disabled = 3,
	/// <summary>The user ID is already taken</summary>
	UserIDTaken = 4,
	/// <summary>The email address is already in use</summary>
	EmailTaken = 5,
	/// <summary>Invalid PW format</summary>
	InvalidPWFormat = 6,
	/// <summary>Invalid User ID format</summary>
	InvalidUserIDFormat = 7,
	/// <summary>Invalid email format</summary>
	InvalidEmailFormat = 8,
	/// <summary>This request cannont be preformed on this environment</summary>
	InvalidEnvironment = 9,
	/// <summary>The credentials are invalid or the account is terminated. Return an unauthorzied.</summary>
	InvalidCredentials = 10,
	/// <summary>Password does not match the one in the DB</summary>
	IncorrectPW = 11,
	/// <summary>There's no matching UserID in the DB</summary>
	UserIDNotFound = 12,
	/// <summary>The user has no role in this domain or the domain does not exist</summary>
	InvalidDomain = 13,
	/// <summary>There's no matching 2FA or Confirmation Code</summary>
	AuthCodeNotFound = 14,
	/// <summary>Failed to get an login access token with a valid response</summary>
	NoAccessToken = 15,
	/// <summary>Failed to get the users profile</summary>
	NoAccessProfile = 16,
	/// <summary>Failed creating a new datastore insert</summary>
	InsertFailed = 20,
	/// <summary>Failed to update the database with a new record</summary>
	UpdateFailed = 21,
	/// <summary>Failed to delete the database record</summary>
	DeleteFailed = 22,
	/// <summary>The record in the data store is newer than the one being pushed.</summary>
	UpdateStale = 23,
	/// <summary>None specific insert or update failure</summary>
	InsertOrUpdateFailed = 24,
	/// <summary>The input parameters do not validate</summary>
	InvalidInput = 25,
	/// <summary>No result data was obtained to return</summary>
	NoResults = 26,
	/// <summary>Failed a logic rule, see the logs or additional data</summary>
	FailedRule = 27,
	/// <summary>No data matching the entity ID or filter</summary>
	NoMatch = 28,
	/// <summary>Data or file has exceeded allowable size</summary>
	TooLarge = 29,
	/// <summary>A name such as a filename is not valid</summary>
	InvalidName = 30,
	/// <summary>The user or their role is not authorized to access this method.</summary>
	Unauthorized = 31,
	/// <summary>The user has maxed out their usage of this method or storage.</summary>
	ExceededQuota = 32,
	/// <summary>User must be logged in to use this method in this way.</summary>
	IllegalAnonymousRequest = 33,
	/// <summary>The app is purposely offline. No one is allowed to login.</summary>
	Offline = 90,
	/// <summary>A backend server is is not responding.</summary>
	Down = 98,
	/// <summary>An exception was thrown, see the logs</summary>
	Exception = 99,
	/// <summary>Error unknown, check the logs</summary>
	Unknown = 100
}
