using Blazored.LocalStorage;
using Iceshrimp.Frontend.Core.Schemas;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Iceshrimp.Frontend.Core.Services;

internal class SessionService
{
	public SessionService(ApiService apiService, ISyncLocalStorageService localStorage, IJSRuntime js)
	{
		ApiService   = apiService;
		LocalStorage = localStorage;
		Js           = js;
		Users        = LocalStorage.GetItem<Dictionary<string, StoredUser>>("Users") ?? [];
		var lastUser = LocalStorage.GetItem<string?>("last_user");
		if (lastUser != null)
		{
			SetSession(lastUser);
		}
	}

	[Inject] public ISyncLocalStorageService       LocalStorage { get; }
	[Inject] public ApiService                     ApiService   { get; }
	[Inject] public IJSRuntime                     Js           { get; }
	public          Dictionary<string, StoredUser> Users        { get; }
	public          StoredUser?                    Current      { get; private set; }

	private void WriteUsers()
	{
		LocalStorage.SetItem("Users", Users);
	}

	public void AddUser(StoredUser user)
	{
		try
		{
			Users.Add(user.Id, user);
		}
		catch (ArgumentException) // Update user if it already exists.
		{
			Users[user.Id] = user;
		}

		WriteUsers();
	}

	public void DeleteUser(string id)
	{
		if (id == Current?.Id) throw new ArgumentException("Cannot remove current user.");
		Users.Remove(id);
		WriteUsers();
	}

	private StoredUser GetUserById(string id)
	{
		var user = Users[id];
		return user;
	}

	public void EndSession()
	{
		Current = null;
		LocalStorage.RemoveItem("last_user");
		((IJSInProcessRuntime)Js).InvokeVoid("eval",
		                                     "document.cookie = \"admin_session=; path=/ ; Fri, 31 Dec 1000 23:59:59 GMT SameSite=Lax\"");
	}

	public void SetSession(string id)
	{
		((IJSInProcessRuntime)Js).InvokeVoid("eval",
		                                     "document.cookie = \"admin_session=; path=/; expires=Fri, 31 Dec 1000 23:59:59 GMT SameSite=Lax\"");
		var user = GetUserById(id);
		if (user == null) throw new Exception("Did not find User in Local Storage");
		ApiService.SetBearerToken(user.Token);
		Current = user;
		LocalStorage.SetItem("last_user", user.Id);
		var sessionsString = Users.Aggregate("", (current, el) => current + $"{el.Value.Token},").TrimEnd(',');
		((IJSInProcessRuntime)Js).InvokeVoid("eval",
		                                     $"document.cookie = \"sessions={sessionsString}; path=/; expires=Fri, 31 Dec 9999 23:59:59 GMT; SameSite=Lax\"");
		if (user.IsAdmin)
		{
			((IJSInProcessRuntime)Js).InvokeVoid("eval",
			                                     $"document.cookie = \"admin_session={user.Token}; path=/; expires=Fri, 31 Dec 9999 23:59:59 GMT; SameSite=Lax\"");
		}
		// Security implications of this need a second pass? user.Id should never be user controllable, but still.
	}
}