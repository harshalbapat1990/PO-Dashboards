using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using MongoDB.Driver;

namespace DesignAutomation.AppCatalog.Data;

/// <summary></summary>
public class UsersMongoRepository : MongoDataManager<AppUser, Guid>, IUsersRepository
{
    #region [Fields]

    private string _table = "Users";

    #endregion [Fields]

    #region [Constructors]

    /// <summary></summary>
    public UsersMongoRepository(string connStr, string db, string table) : base(connStr, db)
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

    #region [Methods]

    /// <summary></summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public AppUser? Get(Guid id)
    {
        if (id == Guid.Empty)
            return null;

        return base.Get(id, this.Table);
    }

    /// <summary>List all users for now</summary>
    /// <returns></returns>
    public async Task<List<AppUser>> List() => await base.ListAsync(this.Table);

    /// <summary>Gets by the common user's email case insensitive</summary>
    /// <returns></returns>
    public AppUser? GetByEmail(string email)
    {
        if (String.IsNullOrWhiteSpace(email))
            return null;

        email = email.Trim().ToLowerInvariant();

        IMongoCollection<AppUser> collection = this.Database.GetCollection<AppUser>(this.Table);
        return collection.Find<AppUser>(u => u.Email.ToLowerInvariant() == email).FirstOrDefault();
    }

    /// <summary>Add a new account</summary>
    /// <returns></returns>
    public async Task<Guid> Insert(AppUser acct)
    {
        if (acct == null || String.IsNullOrWhiteSpace(acct.Email))
            return Guid.Empty;

        // check the email DNE
        var exists = this.GetByEmail(acct.Email);

        if (exists != null)
            return Guid.Empty;

        // try to insert user
        return await base.InsertAsync(acct, this.Table) ? acct.ID : Guid.Empty;
    }

    /// <summary>Updates just the the ping ID for now</summary>
    /// <returns></returns>
    public async Task<bool> Update(Guid userID, Guid pingID)
    {
        // validate the input data
        if (userID == Guid.Empty || pingID == Guid.Empty)
            return false;

        IMongoCollection<AppUser> coll = this.Database.GetCollection<AppUser>(this.Table);
        FilterDefinition<AppUser> filter = Builders<AppUser>.Filter.Eq(i => i.ID, userID);
        UpdateDefinition<AppUser> update = Builders<AppUser>.Update.Set(s => s.PingID, pingID).Set(s => s.Modified, DateTimeOffset.UtcNow);
        return (await coll.UpdateOneAsync(filter, update)).ModifiedCount > 0;
    }

    /// <summary>Updates just the last login</summary>
    /// <returns></returns>
    public async Task<bool> UpdateLogin(Guid userID)
    {
        // validate the input data
        if (userID == Guid.Empty)
            return false;

        IMongoCollection<AppUser> coll = this.Database.GetCollection<AppUser>(this.Table);
        FilterDefinition<AppUser> filter = Builders<AppUser>.Filter.Eq(i => i.ID, userID);
        UpdateDefinition<AppUser> update = Builders<AppUser>.Update.Set(s => s.LastLogin, DateTimeOffset.UtcNow);
        return (await coll.UpdateOneAsync(filter, update)).ModifiedCount > 0;
    }

    /// <summary>Updates user role</summary>
    /// <returns></returns>
    public async Task<bool> UpdateRole(Guid userID, UserRoleLevel role)
    {
        // validate the input data
        if ( userID == Guid.Empty )
            return false;

        // role cannot be < user
        if (role < UserRoleLevel.User)
            role = UserRoleLevel.User;

        IMongoCollection<AppUser> coll = this.Database.GetCollection<AppUser>(this.Table);
        FilterDefinition<AppUser> filter = Builders<AppUser>.Filter.Eq(i => i.ID, userID);
        UpdateDefinition<AppUser> update = Builders<AppUser>.Update.Set(s => s.Role, role).Set(s => s.Modified, DateTimeOffset.UtcNow);
        return (await coll.UpdateOneAsync(filter, update)).ModifiedCount > 0;
    }

    /// <summary>Completely deletes a user</summary>
    /// <param name="id">The users DB ID</param>
    /// <returns>True if the user was successfully deleted otherwise false</returns>
    public async Task<bool> Delete(Guid id)
    {
        if (id == Guid.Empty)
            return false;

        return await base.DeleteAsync(id, this.Table);
    }

    #endregion [Methods]
}