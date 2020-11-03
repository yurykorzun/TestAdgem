using UnityEngine;

using AdGemUnity;
using Facebook.Unity;

//using PlayFab;
//using PlayFab.ClientModels;
//using PlayFab.Json;

public class startup : MonoBehaviour
{
    private const int adGemAppId = 2378;
    private string YOUR_APP_KEY = "c77b1325";
    private bool UsesInterstitialVideos = true;
    private bool UsesRewardVideos = false;
    private bool UsesOfferWall = true;
    private static string titleID = "AF02";

    // Start is called before the first frame update
    void Start()
    {
        //Playfab
        //PlayFabSettings.TitleId = titleID;

        //Adgem start
        AdGem.startSession(adGemAppId, UsesInterstitialVideos, UsesRewardVideos, UsesOfferWall);

        //Iron Source start
        string YOUR_USER_ID = SystemInfo.deviceUniqueIdentifier;
        IronSource.Agent.setUserId(YOUR_USER_ID);
        IronSource.Agent.shouldTrackNetworkState(true);
        IronSource.Agent.init(YOUR_APP_KEY, IronSourceAdUnits.REWARDED_VIDEO, IronSourceAdUnits.INTERSTITIAL, IronSourceAdUnits.OFFERWALL, IronSourceAdUnits.BANNER);
        IronSource.Agent.validateIntegration();

        //Facebook start
        FB.Init(InitCallback, OnHideUnity);

    }

    private void InitCallback()
    {
        if (FB.IsInitialized)
        {
            FB.ActivateApp();
        }
        else
        {
            Debug.Log("Failed to Initialize the Facebook SDK");
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        if (!isGameShown)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }
}
