using System.Net.Http.Headers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Core.Federation;

public class Fetch {
	private const string Accept = "application/activity+json";

	public static void Test2() {
		var thing      = FetchActivity("https://staging.e2net.social/users/9esresfwle/outbox?page=true");
		var collection = thing.ToObject<List<ASCollection>>();

		/*ASObject test;
		test = new ASActor {Id = "asd", Type = ["asd"], Username = "asd"};
		if (test is ASNote note) {
			Console.WriteLine(note.PublishedAt);
		}
		else if (test is ASActor actor) {
			Console.WriteLine(actor.Username);
		}*/

		//FetchActivity("https://mastodon.social/@eugen");
		//FetchActivity("https://0w0.is/users/yassie_j");
		//var activity = FetchActivity("https://staging.e2net.social/notes/9koh2bdfcwzzfewv");
		//var notes    = activity.ToObject<List<ASNote>>();
		//notes?.ForEach(PerformActivity);
		//notes?.ForEach(p => Console.WriteLine(p.PublishedAt));
	}

	public static void PerformActivity(ASNote note) {
		var db       = new DatabaseContext();
		var actorUri = note.AttributedTo?.FirstOrDefault()?.Id;
		if (actorUri == null) return;
		var user = db.Users.FirstOrDefault(p => p.Uri == actorUri) ?? FetchUser(actorUri);
		Console.WriteLine($"PerformActivity: {user.Username}@{user.Host ?? "localhost"}");
	}

	public static User FetchUser(string uri) {
		Console.WriteLine($"Fetching user: {uri}");
		var activity = FetchActivity(uri);
		var actor    = activity.ToObject<List<ASActor>>();
		return new User {
			Username = actor![0].Username!,
			Host     = new Uri(uri).Host
		};
	}

	public static JArray FetchActivity(string url) {
		var client = new HttpClient {
			DefaultRequestHeaders = { Accept = { MediaTypeWithQualityHeaderValue.Parse(Accept) } }
		};

		var input = client.GetAsync(url).Result.Content.ReadAsStringAsync().Result;
		var json  = JsonConvert.DeserializeObject<JObject?>(input);

		var res = LDHelpers.Expand(json);
		if (res == null) throw new Exception();
		return res;
	}
}