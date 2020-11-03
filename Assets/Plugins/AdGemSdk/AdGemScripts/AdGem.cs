using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AdGemUnity
{

    public class AdGem
    {

        public static bool loadOfferWallBeforeShowing =
#if UNITY_ANDROID 
            true;
#else
            false;
#endif


        public static Action rewardVideoDownloadCompleted;
        public static Action rewardVideoFinishedPlaying;
        public static Action rewardVideoCanceled;
        public static Action interstitialVideoDownloadCompleted;
        public static Action interstitialVideoFinishedPlaying;
        public static Action interstitialVideoCanceled;
        public static Action videoFailedToLoad;
        public static Action adClicked;
        public static Action<int> offerWallRewardReceived;
        public static Action offerWallClosed;
        public static Action videoAdStarted;

        public static bool interstitialVideoReady { get { return AdGemPlugin.interstitialVideoReady; } set { AdGemPlugin.interstitialVideoReady = value; } }
        public static bool rewardVideoReady { get { return AdGemPlugin.rewardVideoReady; } set { AdGemPlugin.rewardVideoReady = value; } }
        public static bool offerWallReady { get { return AdGemPlugin.offerWallReady; } set { AdGemPlugin.offerWallReady = value; } }
        public static bool videoIsPlaying { get { return AdGemPlugin.videoIsPlaying; } set { AdGemPlugin.videoIsPlaying = value; } }
        public static bool alwaysOpenAppStoreInBrowser { get { return AdGemPlugin.alwaysOpenAppStoreInBrowser; } set { AdGemPlugin.alwaysOpenAppStoreInBrowser = value; } }
        public static bool openOfferWallInBrowser { get { return AdGemPlugin.openOfferWallInBrowser; } set { AdGemPlugin.openOfferWallInBrowser = value; } }
        public static string player_id { get { return AdGemPlugin.player_id; } set { AdGemPlugin.player_id = value; } }
        public static int player_age { get { return AdGemPlugin.player_age; } set { AdGemPlugin.player_age = value; } }
        public static bool player_payer { get { return AdGemPlugin.player_payer; } set { AdGemPlugin.player_payer = value; } }
        public static int player_iap_total_usd { get { return AdGemPlugin.player_iap_total_usd; } set { AdGemPlugin.player_iap_total_usd = value; } }
        public static string player_created_at { get { return AdGemPlugin.player_created_at; } set { AdGemPlugin.player_created_at = value; } }
        public static int player_level { get { return AdGemPlugin.player_level; } set { AdGemPlugin.player_level = value; } }
        public static int placement { get { return AdGemPlugin.placement; } set { AdGemPlugin.placement = value; } }
        public static string c1 { get { return AdGemPlugin.c1; } set { AdGemPlugin.c1 = value; } }
        public static string c2 { get { return AdGemPlugin.c2; } set { AdGemPlugin.c2 = value; } }
        public static string c3 { get { return AdGemPlugin.c3; } set { AdGemPlugin.c3 = value; } }
        public static string c4 { get { return AdGemPlugin.c4; } set { AdGemPlugin.c4 = value; } }
        public static string c5 { get { return AdGemPlugin.c5; } set { AdGemPlugin.c5 = value; } }
        public static AdGemPlugin.Gender player_gender { get { return AdGemPlugin.player_gender; } set { AdGemPlugin.player_gender = value; } }

        public static bool verboseLogging = false;


        public static void startSession(int appId, bool useinterstitialVideos, bool useRewardedVideos, bool useOfferWall)
        {
            if (AdGemPlugin.adGemObject == null) //In this case, the session has not been started.
            {
                AdGemPlugin.adGemObject = new GameObject("AdGemObject");
                AdGemPlugin.adGemObject.AddComponent<AdGemManager>();

                AdGemPlugin.startSession(appId, useinterstitialVideos, useRewardedVideos, useOfferWall);
            }

#if UNITY_IOS
        AdGemPlugin.iosSystemVersionString = UnityEngine.iOS.Device.systemVersion;
#endif

        }

        public static void playRewardedVideoAd()
        {
            playVideoAd(true);
        }
        public static void playinterstitialVideoAd()
        {
            playVideoAd(false);
        }

        public static void playVideoAd(bool rewarded)
        {
            if (AdGem.verboseLogging) { Debug.Log("Adgem - playVideoAd called."); }

            if (AdGemPlugin.videoIsPlaying) { return; }

            AdGemPlugin.playVideoAd(rewarded);

            string adTypeParam = rewarded ? "rewarded-video" : "nonrewarded-video";
            AdGemManager.staticRef.fireEventEndpoint("videoplay", adTypeParam, rewarded ? AdGemManager.rewardedVideoCacheId : AdGemManager.interstitialVideoCacheId);

            if (AdGemPrefabController.staticRef != null)
            {
                try
                {
                    if (rewarded && AdGemManager.rewardedVideoIsWebview)
                    {
                        if (AdGemWebview.staticRef.webviewAdLoadRequested) { return; } //safeguard against multiple taps

                        if (AdGem.verboseLogging) { Debug.Log("Adgem - attempting to show rewarded webview video ad."); }
                        AdGemWebview.currentWebviewVideoIsRewarded = true;
                        //AdGemWebview.staticRef.showWebviewVideoAd(AdGemManager.rewardedWebviewVideoAdUrl);
                        AdGemWebview.staticRef.showWebviewVideoAd(true);
                        AdGemPlugin.videoIsPlaying = true;
                        AdGemPlugin.rewardVideoReady = false;
                        
                    }
                    else if (!rewarded && AdGemManager.interstitialVideoIsWebview)
                    {
                        if (AdGemWebview.staticRef.webviewAdLoadRequested) { return; } //safeguard against multiple taps

                        if (AdGem.verboseLogging) { Debug.Log("Adgem - attempting to show interstitial webview video ad."); }
                        AdGemWebview.currentWebviewVideoIsRewarded = false;
                        //AdGemWebview.staticRef.showWebviewVideoAd(AdGemManager.standardWebviewVideoAdUrl);
                        AdGemWebview.staticRef.showWebviewVideoAd(false);
                        AdGemPlugin.videoIsPlaying = true;
                        AdGemPlugin.interstitialVideoReady = false;
                        
                    }
                    else //"Native" video
                    {
                        if(AdGem.verboseLogging) { Debug.Log("Adgem - attempting to show native video ad."); }
                        AdGemPrefabController.staticRef.playVideo(rewarded);
                    }
                }
                catch (Exception ex)
                {
                    if(verboseLogging)
                        Debug.Log("Error playing video: " + ex.ToString());
                }
            }
        }

        public static void showOfferWall()
        {
            if (AdGemPlugin.videoIsPlaying) { return; }

            //Only for logging
            AdGemPlugin.showOfferWall();

            if (AdGemPrefabController.staticRef != null)
            {
                try
                {
                    AdGemPrefabController.staticRef.showOfferWall();
                }
                catch (Exception ex)
                {
                    if (verboseLogging)
                        Debug.Log("Error showing offer wall: " + ex.ToString());
                }
            }

        }

        public static int GetMajorOsVersion(string osVersion)
        {
            string[] pieces = osVersion.Trim().Split('.');
            int majorVersion = -1;
            if (pieces.Length >= 1)
            {
                int.TryParse(pieces[0], out majorVersion);
            }
            return majorVersion;
        }

    }

}
