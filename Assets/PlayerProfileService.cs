using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Photon.Pun;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;
using UnityEngine;

public class PlayerProfileService : MonoBehaviour
{
    const string CloudNicknameKey = "profile_nickname";
    const string CloudShipSkinKey = "profile_ship_skin";
    const string CloudGamesPlayedKey = "profile_games_played";

    static PlayerProfileService instance;
    Task initializationTask;
    bool initialized;

    public static PlayerProfileService Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    public static bool HasInstance => instance != null;

    public bool IsInitialized => initialized;
    public bool IsBusy { get; private set; }
    public string PlayerId => AuthenticationService.Instance?.PlayerId;
    public PlayerProfileData CurrentProfile { get; private set; } = PlayerProfileData.Default();

    public event Action<PlayerProfileData> ProfileChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        EnsureInstance();
    }

    static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("PlayerProfileService");
        instance = root.AddComponent<PlayerProfileService>();
        DontDestroyOnLoad(root);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    async void Start()
    {
        await EnsureInitializedAsync();
    }

    public Task EnsureInitializedAsync()
    {
        initializationTask ??= InitializeInternalAsync();
        return initializationTask;
    }

    async Task InitializeInternalAsync()
    {
        try
        {
            IsBusy = true;

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            await LoadProfileAsync();
            initialized = true;
        }
        catch (Exception ex)
        {
            Debug.LogError("PlayerProfileService init failed: " + ex);
            CurrentProfile = BuildFallbackProfile();
            ApplyProfileToPhoton();
            NotifyProfileChanged();
        }
        finally
        {
            IsBusy = false;
        }
    }

    async Task LoadProfileAsync()
    {
        var keys = new HashSet<string> { CloudNicknameKey, CloudShipSkinKey, CloudGamesPlayedKey };
        Dictionary<string, Item> data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        string nickname = null;
        int shipSkinIndex = 0;
        int gamesPlayed = 0;

        if (data != null)
        {
            if (data.TryGetValue(CloudNicknameKey, out Item nicknameItem) && nicknameItem?.Value != null)
            {
                nickname = nicknameItem.Value.GetAsString();
            }

            if (data.TryGetValue(CloudShipSkinKey, out Item skinItem) && skinItem?.Value != null)
            {
                shipSkinIndex = Mathf.Clamp(skinItem.Value.GetAs<int>(), 0, 2);
            }

            if (data.TryGetValue(CloudGamesPlayedKey, out Item gamesItem) && gamesItem?.Value != null)
            {
                gamesPlayed = Mathf.Max(0, gamesItem.Value.GetAs<int>());
            }
        }

        if (string.IsNullOrWhiteSpace(nickname))
        {
            nickname = BuildFallbackProfile().Nickname;
        }

        CurrentProfile = new PlayerProfileData
        {
            Nickname = SanitizeNickname(nickname),
            ShipSkinIndex = Mathf.Clamp(shipSkinIndex, 0, 2),
            GamesPlayed = gamesPlayed
        };

        ApplyProfileToPhoton();
        NotifyProfileChanged();
    }

    public async Task SaveProfileAsync(string nickname, int shipSkinIndex)
    {
        await EnsureInitializedAsync();

        try
        {
            IsBusy = true;

            CurrentProfile = new PlayerProfileData
            {
                Nickname = SanitizeNickname(nickname),
                ShipSkinIndex = Mathf.Clamp(shipSkinIndex, 0, 2),
                GamesPlayed = CurrentProfile != null ? CurrentProfile.GamesPlayed : 0
            };

            var data = new Dictionary<string, object>
            {
                [CloudNicknameKey] = CurrentProfile.Nickname,
                [CloudShipSkinKey] = CurrentProfile.ShipSkinIndex,
                [CloudGamesPlayedKey] = CurrentProfile.GamesPlayed
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            ApplyProfileToPhoton();
            NotifyProfileChanged();
        }
        catch (Exception ex)
        {
            Debug.LogError("PlayerProfileService save failed: " + ex);
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ApplyProfileToPhoton()
    {
        if (CurrentProfile == null)
            return;

        if (!string.IsNullOrWhiteSpace(CurrentProfile.Nickname))
        {
            PhotonNetwork.NickName = CurrentProfile.Nickname;
        }

        if (PhotonNetwork.LocalPlayer == null)
            return;

        var props = new ExitGames.Client.Photon.Hashtable
        {
            [RoomSettings.ShipSkinKey] = CurrentProfile.ShipSkinIndex
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public async Task RecordGameStartedAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            IsBusy = true;
            CurrentProfile.GamesPlayed = Mathf.Max(0, CurrentProfile.GamesPlayed + 1);

            var data = new Dictionary<string, object>
            {
                [CloudGamesPlayedKey] = CurrentProfile.GamesPlayed
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            NotifyProfileChanged();
        }
        catch (Exception ex)
        {
            Debug.LogError("PlayerProfileService games played update failed: " + ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    PlayerProfileData BuildFallbackProfile()
    {
        string suffix = "0000";
        string playerId = AuthenticationService.Instance != null ? AuthenticationService.Instance.PlayerId : string.Empty;

        if (!string.IsNullOrWhiteSpace(playerId))
        {
            suffix = playerId.Length <= 4 ? playerId : playerId.Substring(playerId.Length - 4);
        }

        return new PlayerProfileData
        {
            Nickname = "Pilot " + suffix.ToUpperInvariant(),
            ShipSkinIndex = 0,
            GamesPlayed = 0
        };
    }

    string SanitizeNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return BuildFallbackProfile().Nickname;

        string trimmed = nickname.Trim();
        if (trimmed.Length > 18)
            trimmed = trimmed.Substring(0, 18);

        return trimmed;
    }

    void NotifyProfileChanged()
    {
        ProfileChanged?.Invoke(CurrentProfile);
    }
}

[Serializable]
public class PlayerProfileData
{
    public string Nickname;
    public int ShipSkinIndex;
    public int GamesPlayed;

    public static PlayerProfileData Default()
    {
        return new PlayerProfileData
        {
            Nickname = "Pilot",
            ShipSkinIndex = 0,
            GamesPlayed = 0
        };
    }
}
