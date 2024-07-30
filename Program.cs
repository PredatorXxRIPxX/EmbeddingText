using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private IMongoDatabase db;
    private IMongoCollection<BsonDocument> collection;
    public List<BsonDocument> documents;

    public static async Task<string> EmbeddText(string title)
    {
        string apiKey = "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";
        string url = "https://api.openai.com/v1/embeddings";

        var jsonPayload = new
        {
            input = title,
            model = "text-embedding-3-large"
        };

       
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        var json = JsonConvert.SerializeObject(jsonPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var rootObject = JsonConvert.DeserializeObject<JObject>(responseString);
                var embedding = rootObject?["data"]?[0]?["embedding"];
                if (embedding != null)
                {
                    return embedding.ToString();
                }
            }
            else
            {
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                Console.WriteLine("Error: " + response.StatusCode);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
        return null;
    }

    public async Task DatabaseConnection()
    {
        Console.WriteLine("Connecting to DB");
        string connectionString = "mongodb+srv://wassim:wassimoux30@cluster0.3pj8tye.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0";
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        var client = new MongoClient(settings);

        try
        {
            var result = client.GetDatabase("test").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            Console.WriteLine("Pinged your deployment, you are successfully connected to DB");
            Console.WriteLine(result);
            db = client.GetDatabase("test");
            collection = db.GetCollection<BsonDocument>("exceldatas");
            documents = await collection.Find(new BsonDocument()).ToListAsync();
            Console.WriteLine($"Retrieved {documents.Count} documents.");
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
    }

    public static async Task Main(string[] args)
    {
        Program program = new Program();
        await program.DatabaseConnection();

        foreach (var doc in program.documents)
        {
            var title = doc["title"].AsString;
            var embedding = await EmbeddText(title);
            if (embedding != null)
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]);
                var update = Builders<BsonDocument>.Update.Set("embedding", embedding);
                await program.collection.UpdateOneAsync(filter, update);
                Console.WriteLine($"Updated document with _id: {doc["_id"]}");
            }
        }
        Console.WriteLine("Finished updating all documents.");
    }
}
