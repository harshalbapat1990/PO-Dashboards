using DesignAutomation.AppCatalog.Model;

namespace DesignAutomation.AppCatalog.Data;

/// <summary></summary>
public interface IAppHeaderRepository
{
	/// <summary></summary>
	AppHeader? Get(Guid id);

	/// <summary></summary>
	Task<UpdatedDTO> GetLastUpdate(Guid appID);

	/// <summary></summary>
	Task<List<AppHeaderDTO>> ProjectList();

	/// <summary></summary>
	Task<List<AppHeader>> List();

	/// <summary></summary>
	Task<List<PoCDTO>> ProjectPoCs();

	/// <summary></summary>
	Task<List<RepoDTO>> ProjectRepos();

	/// <summary></summary>
	Task<bool> Insert(AppHeader app);

	/// <summary></summary>
	Task<DateTimeOffset?> Update(AppHeader item);

	/// <summary></summary>
	Task<bool> UpdateReview(Guid id, string review);

	/// <summary></summary>
	Task<DateTimeOffset> UpdateModified(Guid id);

	/// <summary></summary>
	Task<bool> Delete(Guid id);
}