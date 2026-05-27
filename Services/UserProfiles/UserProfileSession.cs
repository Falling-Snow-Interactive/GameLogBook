using Microsoft.EntityFrameworkCore;
using VGL.Data;
using VGL.Models.Users;

namespace VGL.Services.UserProfiles;

public class UserProfileSession(GameLogBookDbContext dbContext)
{
    private const string AutoSignInProfileIdKey = "AutoSignInProfileId";

    public event Action? Changed;

    public UserProfile? CurrentUser { get; private set; }

    public int? CurrentUserID => CurrentUser?.ID;

    public bool IsSignedIn => CurrentUser is not null;

    public bool IsAdmin => CurrentUser?.Role == UserRole.Admin;

    public int? AutoSignInProfileID
    {
        get
        {
            int profileId = Preferences.Default.Get(AutoSignInProfileIdKey, 0);
            return profileId > 0 ? profileId : null;
        }
    }

    public async Task<bool> TryAutoSignInAsync()
    {
        if (CurrentUser is not null)
        {
            return true;
        }

        int? autoSignInProfileId = AutoSignInProfileID;

        if (autoSignInProfileId is null)
        {
            return false;
        }

        UserProfile? profile = await dbContext.UserProfiles
                                              .AsNoTracking()
                                              .FirstOrDefaultAsync(user => user.ID == autoSignInProfileId.Value
                                                                           && !user.IsHidden);

        if (profile is null)
        {
            ClearAutoSignIn();
            return false;
        }

        CurrentUser = profile;
        NotifyChanged();
        return true;
    }

    public async Task SignInAsync(int profileId, bool signInAutomatically)
    {
        UserProfile? profile = await dbContext.UserProfiles
                                              .AsNoTracking()
                                              .FirstOrDefaultAsync(user => user.ID == profileId
                                                                           && !user.IsHidden);

        if (profile is null)
        {
            ClearAutoSignIn();
            return;
        }

        CurrentUser = profile;

        if (signInAutomatically)
        {
            Preferences.Default.Set(AutoSignInProfileIdKey, profile.ID);
        }
        else
        {
            ClearAutoSignIn();
        }

        NotifyChanged();
    }

    public void SignOut()
    {
        CurrentUser = null;
        NotifyChanged();
    }

    public void ClearAutoSignIn()
    {
        Preferences.Default.Remove(AutoSignInProfileIdKey);
    }

    public async Task RefreshCurrentUserAsync()
    {
        if (CurrentUser is null)
        {
            return;
        }

        UserProfile? profile = await dbContext.UserProfiles
                                              .AsNoTracking()
                                              .FirstOrDefaultAsync(user => user.ID == CurrentUser.ID);

        if (profile is null || profile.IsHidden)
        {
            SignOut();
            ClearAutoSignIn();
            return;
        }

        CurrentUser = profile;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
