using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace DesignAutomation.AppCatalog.Data;

/// <summary>Base class for a Mongo DB Manager</summary>
/// <typeparam name="T">The entity object type</typeparam>
/// <typeparam name="P">The data type of the _id field</typeparam>
public abstract class MongoDataManager<T, P>
{
	#region [Fields]

	private MongoClient _dbConn = new MongoClient();

	private string _db = "dev";

	#endregion [Fields]

	/// <summary></summary>
	static MongoDataManager()
	{
		// default to using a standard GUID Representation when getting by GUID
		BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
	}

	/// <summary></summary>
	public MongoDataManager(string connStr, string db)
	{
		if (!String.IsNullOrWhiteSpace(connStr))
			this._dbConn = new MongoClient(connStr);

		if (!String.IsNullOrWhiteSpace(db))
			this._db = db.Trim();

		// how to deal with Enums
		ConventionRegistry.Register("EnumStringConvention", new ConventionPack { new EnumRepresentationConvention(BsonType.String) }, t => true);
	}

	#region [Properties]

	/// <summary>The DB Client</summary>
	public MongoClient Client
	{
		get { return this._dbConn; }
		protected set { this._dbConn = value; }
	}

	/// <summary>The DB</summary>
	public IMongoDatabase Database
	{
		get { return this.Client.GetDatabase(this._db); }
	}

	#endregion [Properties]

	#region [Base Methods]

	/// <summary>Returns basic info about the database as a simple string</summary>
	public string DBInfo()
	{
		//BsonDocument version = this.Database.RunCommand<BsonDocument>("db.version()");
		return this.Database.DatabaseNamespace.DatabaseName;
	}

	/// <summary>Gets all the collections in this DB.</summary>
	/// <returns>A list of collection names.</returns>
	/// <remarks>Useful to test the DB is alive</remarks>
	public List<string> GetAllCollections()
	{
		List<string> colls = [];
		List<BsonDocument> all = this.Database.ListCollections().ToList();

		foreach (BsonDocument doc in all)
		{
			BsonValue name = doc.GetValue("name");
			colls.Add(name.AsString);
		}

		return colls;
	}

	/// <summary>Gets a document by it's ID as the native type.</summary>
	protected T? Get(P id, string tbl)
	{
		if (String.IsNullOrWhiteSpace(tbl))
			return default;

		IMongoCollection<T> col = this.Database.GetCollection<T>(tbl);
		FilterDefinition<T> filter = Builders<T>.Filter.Eq("_id", id);
		List<T> results = col.Find<T>(filter).ToList();

		return (results != null && results.Count > 0) ? results[0] : default;
	}

	/// <summary>Gets all documents in the collection</summary>
	protected List<T> GetAll(string tbl) => this.List(tbl);

	/// <summary>Gets all documents in the collection</summary>
	protected List<T> List(string tbl)
	{
		if (String.IsNullOrWhiteSpace(tbl))
			return new List<T>();

		// find the root account
		IMongoCollection<T> collection = this.Database.GetCollection<T>(tbl);
		List<T> results = collection.Find<T>(Builders<T>.Filter.Empty).ToList();

		return (results != null && results.Count > 0) ? results : new List<T>();
	}

	/// <summary>Gets all documents in the collection</summary>
	protected async Task<List<T>> ListAsync(string tbl)
	{
		if (String.IsNullOrWhiteSpace(tbl))
			return new List<T>();

		// find the root account
		IMongoCollection<T> collection = this.Database.GetCollection<T>(tbl);
		List<T> results = (await collection.FindAsync<T>(Builders<T>.Filter.Empty)).ToList();

		return (results != null && results.Count > 0) ? results : new List<T>();
	}

	/// <summary>Deletes a document in the collection</summary>
	protected bool Delete(P id, string tbl)
	{
		if (String.IsNullOrWhiteSpace(tbl))
			return false;

		IMongoCollection<T> col = this.Database.GetCollection<T>(tbl);
		FilterDefinition<T> filter = Builders<T>.Filter.Eq("_id", id);
		DeleteResult result = col.DeleteOne(filter);

		return (result.DeletedCount > 0);
	}

	/// <summary>Deletes a single document from the collection</summary>
	protected async Task<bool> DeleteAsync(P id, string tbl)
	{
		if (String.IsNullOrWhiteSpace(tbl))
			return false;

		IMongoCollection<T> col = this.Database.GetCollection<T>(tbl);
		FilterDefinition<T> filter = Builders<T>.Filter.Eq("_id", id);
		DeleteResult result = await col.DeleteOneAsync(filter);

		return (result.DeletedCount > 0);
	}

	/// <summary>Inserts a document into the collection</summary>
	protected bool Insert(T item, string tbl)
	{
		if (String.IsNullOrWhiteSpace(tbl))
			return false;

		this.Database.GetCollection<T>(tbl).InsertOne(item);

		return true;
	}

	/// <summary>Inserts one document into the collection async</summary>
	protected async Task<bool> InsertAsync(T item, string tbl)
	{
		if (String.IsNullOrWhiteSpace(tbl))
			return false;

		await this.Database.GetCollection<T>(tbl).InsertOneAsync(item);

		return true;
	}

	#endregion [Base Methods]
}
