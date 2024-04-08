using Blazored.LocalStorage;
using Iceshrimp.Frontend.Core.Schemas;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Core.Services;

internal class SessionService
{
    [Inject] public ISyncLocalStorageService       LocalStorage { get; }
    [Inject] public ApiService                     ApiService   { get; }
    public          Dictionary<string, StoredUser> Users        { get; }
    public          UserResponse?                  Current      { get; private set; }

    public SessionService(ApiService apiService, ISyncLocalStorageService localStorage)
    {
        ApiService   = apiService;
        LocalStorage = localStorage;
        Users        = LocalStorage.GetItem<Dictionary<string, StoredUser>>("Users") ?? [];
        var lastUser = LocalStorage.GetItem<string?>("last_user");
        if (lastUser != null)
        {
            SetSession(lastUser);
        }
    }

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
        catch( ArgumentException ) // Update user if it already exists.
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

    private StoredUser? GetUserById(string id)
    {
        var user = Users[id];
        return user;
    }

    public void EndSession()
    {
        Current = null;
        LocalStorage.RemoveItem("last_user");
    }
    public void SetSession(string id)
    {
        var user = GetUserById(id);
        if (user == null) throw new Exception("Did not find User in Local Storage");
        ApiService.SetBearerToken(user.Token);
        Current = user;
        LocalStorage.SetItem("last_user", user.Id);
    }
}