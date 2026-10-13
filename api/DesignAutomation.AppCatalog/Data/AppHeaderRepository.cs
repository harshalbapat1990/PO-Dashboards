using DesignAutomation.AppCatalog.Model;
using MongoDB.Driver;

namespace DesignAutomation.AppCatalog.Data;

/// <summary></summary>
public class AppHeaderRepository : MongoDataManager<AppHeader, Guid>, IAppHeaderRepository
{
    #region [Fields]

    private string _table = "Apps";

    #endregion [Fields]

    #region [Constructors]

    /// <summary></summary>
    public AppHeaderRepository(string connStr, string db, string table) : base(connStr, db)
    {
        this.Table = table;
    }

    #endregion [Constructors]

    #region [Properties]

    /// <summary>The physical Collection name</summary>
    public string Table
    {
        get { return this._table; }
        protected set { if (!String.IsNullOrWhiteSpace(value)) this._table = value.Trim(); }
    }

    #endregion [Properties]

    /// <summary></summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public AppHeader? Get(Guid id)
    {
        if (id == Guid.Empty)
            return null;

        return base.Get(id, this.Table);
    }

        /// <summary>Returns just the date of when the specified site was last changed</summary>
    public async Task<UpdatedDTO> GetLastUpdate(Guid appID)
    {
        IMongoCollection<AppHeader> coll = this.Database.GetCollection<AppHeader>(this.Table);
        FilterDefinition<AppHeader> filter = Builders<AppHeader>.Filter.Eq(i => i.ID, appID);

        var results = await coll.Aggregate()
            .Match( filter )
            .Project<AppHeader, UpdatedDTO>(ev => new UpdatedDTO
            {
                ID = ev.ID,
                Modified = ev.Modified
            }).FirstOrDefaultAsync();

        return results ?? new UpdatedDTO();
    }

    /// <summary>Insert a new item</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<bool> Insert(AppHeader item)
    {
        // validate the input data - specicfic ID format?
        if (item == null)
            return false;

        if (item.ID == Guid.Empty)
            item.ID = Guid.NewGuid();

        item.Created = DateTimeOffset.UtcNow;

        return await base.InsertAsync(item, this.Table);
    }

    /// <summary>list all</summary>
    public async Task<List<AppHeader>> List() => (await base.ListAsync(this.Table)).OrderBy(x => x.FullName).ToList();

    /// <summary>list all the apps in a projection</summary>
    public async Task<List<AppHeaderDTO>> ProjectList()
    {
        IMongoCollection<AppHeader> coll = this.Database.GetCollection<AppHeader>(this.Table);
        var results = await coll.Aggregate().Project<AppHeader, AppHeaderDTO>(item => new AppHeaderDTO()
        {
            ID = item.ID,
            FullName = item.FullName,
            Production = item.Production,
            ProductionBranch = item.ProductionBranch,
            Repo = item.Repos.Count > 0 ? item.Repos.First() : String.Empty,
            WebSite = item.WebSite,
            AppType = item.AppType,
            Modified = item.Modified,
            Created = item.Created
        }).ToListAsync();

        // default order by app name
        return results.OrderBy(x => x.FullName).ToList();
    }

    /// <summary>Get a list of all the PoCs only</summary>
    public async Task<List<PoCDTO>> ProjectPoCs()
    {
        List<PoCDTO> pocs = new List<PoCDTO>();
        var apps = await this.List();

        apps.ForEach(app =>
        {
            app.Team.ForEach( c =>
            {
                var poc = new PoCDTO { AppID = app.ID, App = app.FullName, Contact = c };
                pocs.Add(poc);  
            });
        });

        // default order by app name
        return pocs;
    }

    /// <summary>Get a list of all the PoCs only</summary>
    public async Task<List<RepoDTO>> ProjectRepos()
    {
        List<RepoDTO> items = [];
        var apps = await this.List();

        apps.ForEach(app =>
        {
            // if it doesnt have repos
            if (app.Repos.Count < 1)
                items.Add( new() { AppID = app.ID, App = app.FullName, Link = String.Empty } );
            else
                app.Repos.ForEach( c => items.Add( new() { AppID = app.ID, App = app.FullName, Link = c } ) );
        });

        // default order by app name
        return items;
    }

    /// <summary>Marks the record changed by changing to the last modified date to right now</summary>
    /// <param name="id"></param>
    /// <returns>The new modified date or MAX DATETIME if nothing updated</returns>
    public async Task<DateTimeOffset> UpdateModified(Guid id)
    {
        if (id == Guid.Empty)
            return DateTimeOffset.MaxValue;

        var now = DateTimeOffset.UtcNow;

        IMongoCollection<AppHeader> coll = this.Database.GetCollection<AppHeader>(this.Table);
        FilterDefinition<AppHeader> filter = Builders<AppHeader>.Filter.Eq(i => i.ID, id);
        UpdateDefinition<AppHeader> update = Builders<AppHeader>.Update.Set(s => s.Modified, now);
        return (await coll.UpdateOneAsync(filter, update)).ModifiedCount > 0 ? now : DateTimeOffset.MaxValue;
    }

    /// <summary>Updates ONLY the review field and modified making the record stale.</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<bool> UpdateReview(Guid id, string review)
    {
        if (id == Guid.Empty || String.IsNullOrWhiteSpace(review))
            return false;

        IMongoCollection<AppHeader> coll = this.Database.GetCollection<AppHeader>(this.Table);
        FilterDefinition<AppHeader> filterID = Builders<AppHeader>.Filter.Eq(i => i.ID, id);
        var exists = await coll.Find<AppHeader>(filterID).FirstOrDefaultAsync();

        if (exists == null || exists.ID == Guid.Empty)
            return false;

        // use the original created date and update mod
        exists.Review = review.Trim();
        exists.Modified = DateTimeOffset.UtcNow;

        UpdateDefinition<AppHeader> update = Builders<AppHeader>.Update.Set(s => s.Review, review.Trim()).Set(s => s.Modified, DateTimeOffset.UtcNow);
        return (await coll.UpdateOneAsync(filterID, update)).ModifiedCount > 0;
    }

    /// <summary>Does a complete replacement of an existing app except for creation date and ID</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<DateTimeOffset?> Update(AppHeader item)
    {
        // validate the input data
        if (item == null || item.ID == Guid.Empty)
            return null;

        // if app has a modfied date > than item - then someone has changed it since
        IMongoCollection<AppHeader> col = this.Database.GetCollection<AppHeader>(this.Table);
        FilterDefinition<AppHeader> filterID = Builders<AppHeader>.Filter.Eq(i => i.ID, item.ID);
        //FilterDefinition<AppHeader> filterMod = Builders<AppHeader>.Filter.Lte(i => i.Modified, item.Modified);
        var exists = await col.Find<AppHeader>(filterID).FirstOrDefaultAsync();

        if (exists == null || exists.ID == Guid.Empty)
            return null;

        // use the original created date and update mod
        item.Created = exists.Created;
        item.Modified = DateTimeOffset.UtcNow;

        ReplaceOneResult results = await col.ReplaceOneAsync(filterID, item);
        return results.IsAcknowledged && results.ModifiedCount > 0 ? item.Modified : null;
    }

    /// <summary>Deletes the app</summary>
    public async Task<bool> Delete(Guid id)
    {
        if (id == Guid.Empty)
            return false;

        return await base.DeleteAsync(id, this.Table);
    }

}