using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using AdGemSimpleJSON;

namespace AdGemUnity
{

    public class AdGemManager : MonoBehaviour
    {

        public static AdGemManager staticRef;
        private bool refreshingVideos = false;
        public bool refreshingOfferWall = false;
        //private bool osVersionIsHighEnoughForUniwebview = false;
        private bool triedToFetchAdvertisingId = false;
        private bool sentSessionRequest = false;

        private bool shouldPoll = true;

        //public bool pollingForOfferCompletion = false;
        private bool pollProcessRunning = false;
        //private int pollCount = 0;
        //public string prefsKey_pollingForOfferCompletion = "dfisefuh978f4iu0090";


        public static string rewardedWebviewVideoAdUrl = "";
        public static string interstitialWebviewVideoAdUrl = "";
        public static bool rewardedVideoIsWebview = false;
        public static bool interstitialVideoIsWebview = false;
        public static string rewardWebviewVideoHtmlString = "";
        public static string interstitialWebviewVideoHtmlString = "";
        public static string rewardedVideoCacheId = "";
        public static string interstitialVideoCacheId = "";

        // Use this for initialization
        void Start()
        {
            //TODO - move to plugin
            //if(!PlayerPrefs.HasKey(prefsKey_pollingForOfferCompletion)) { PlayerPrefs.SetInt(prefsKey_pollingForOfferCompletion,0); }
            //pollingForOfferCompletion = PlayerPrefs.GetInt(prefsKey_pollingForOfferCompletion) > 0;

            staticRef = this;

            StartCoroutine(fetchAdvertisingId());
            StartCoroutine(logSessionOnServer());
            //StartCoroutine(loadImagesFromCache());

            AdGemPlugin.loadImagesFromCache();
            AdGemPlugin.appSessionId = getRandomUUID();
        }

        private string getRandomUUID()
        {
            string uuid =
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                "-" +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                "-" +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                "-" +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                "-" +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit() +
                getRandomHexDigit();
            return uuid;
        }
        private string hexDigits = "0123456789abcdef";
        private System.Random rng = new System.Random();
        private string getRandomHexDigit()
        {
            int index = rng.Next(hexDigits.Length);
            return hexDigits[index].ToString();
        }


        private IEnumerator logSessionOnServer()
        {
            yield return new WaitForSeconds(1f); //Wait a few seconds to allow other params to get generated.
            while (!triedToFetchAdvertisingId) { yield return new WaitForSeconds(0.1f); }

            if (AdGem.verboseLogging) { Debug.Log("session url: " + AdGemPlugin.baseApiUrl + "session?" + AdGemPlugin.getStandardQueryParameters()); }

            UnityEngine.Networking.UnityWebRequest webRequest = UnityEngine.Networking.UnityWebRequest.Get(AdGemPlugin.baseApiUrl + "session?" + AdGemPlugin.getStandardQueryParameters());
            yield return webRequest.SendWebRequest();

            sentSessionRequest = true;

            //get should poll
            try
            {
                JSONNode baseJson = JSON.Parse(webRequest.downloadHandler.text);
                JSONNode dataNode = baseJson["data"].AsObject;
                shouldPoll = dataNode["should_poll"].AsInt > 0;

                //Debug.Log(dataNode["offerwall_color"].Value);
                //AdGemOfferWall.staticRef.setBgColor(dataNode["offerwall_color"].Value);

            }
            catch (System.Exception ex) { if (AdGem.verboseLogging) Debug.Log(ex.ToString()); }

            //if (AdGem.verboseLogging) { Debug.Log("adgem--- session response: " + webRequest.downloadHandler.text); }
        }


        // Update is called once per frame
        void Update()
        {
            if (!triedToFetchAdvertisingId || !sentSessionRequest) { return; }

            if (!refreshingVideos &&
                (
                    (AdGemPlugin.usingInterstitialVideos && !interstitialVideoIsWebview && AdGemPlugin.CurrentTimeMillis() > AdGemPlugin.standardVideoExpirationDate) ||
                    (AdGemPlugin.usingRewardVideos && !rewardedVideoIsWebview && AdGemPlugin.CurrentTimeMillis() > AdGemPlugin.rewardVideoExpirationDate) ||
                    (AdGemPlugin.usingInterstitialVideos && !AdGemPlugin.interstitialVideoReady) ||
                    (AdGemPlugin.usingRewardVideos && !AdGemPlugin.rewardVideoReady)
                )
            )
            {
                StartCoroutine(refreshAdGemVideos());
            }

            //No offer wall caching for now. We will just load it when needed.
            //if (osVersionIsHighEnoughForUniwebview && !refreshingOfferWall && AdGemPlugin.usingOfferWall && (!AdGemPlugin.offerWallReady || AdGemPlugin.CurrentTimeMillis() > AdGemPlugin.offerWallExpirationDate))
            //if (!refreshingOfferWall && AdGemPlugin.usingOfferWall && (!AdGemPlugin.offerWallReady || AdGemPlugin.CurrentTimeMillis() > AdGemPlugin.offerWallExpirationDate))
            //{
            //    refreshOfferWall();
            //}

            if (AdGemPlugin.pollingForOfferCompletion && !pollProcessRunning)
            {
                StartCoroutine(pollForOfferCompletion());
            }

        }

        public void startPollingProcess()
        {
            AdGemPlugin.pollingForOfferCompletion = true;
            AdGemPlugin.pollCount = 0;
            PlayerPrefs.SetInt(AdGemPlugin.pollCountPrefsKey, AdGemPlugin.pollCount);
            PlayerPrefs.SetInt(AdGemPlugin.pollingForOfferCompletionPrefsKey, 1);
        }

        public void fireEventEndpoint(string eventName, string adTypeParam, string videoCacheId)
        {
            string eventUrl = AdGemPlugin.baseApiUrl + eventName + "?ad_type=" + adTypeParam + "&video_cache_id=" + videoCacheId + AdGemPlugin.getStandardQueryParameters();
            fireEventEndpoint(eventUrl);
        }
        public void fireEventEndpoint(string url)
        {
            if (AdGem.verboseLogging) { Debug.Log("Adgem - fireEventEndpoint: " + url); }
            StartCoroutine(fireEventEndpointRoutine(url));
        }
        private IEnumerator fireEventEndpointRoutine(string url)
        {
            UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(url);
            yield return request.SendWebRequest();
        }

        private IEnumerator pollForOfferCompletion()
        {
            if (shouldPoll)
            {

                pollProcessRunning = true;

                AdGemPlugin.pollCount++;
                PlayerPrefs.SetInt(AdGemPlugin.pollCountPrefsKey, AdGemPlugin.pollCount);

                string pollRequest = AdGemPlugin.baseApiUrl + "checkforoffercompletion?userid=" + AdGemPlugin.advertisingId + "&appid=" + AdGemPlugin.adGemAppId + "&salt=" + AdGemPlugin.offerWallSalt;

                UnityEngine.Networking.UnityWebRequest testRequest = UnityEngine.Networking.UnityWebRequest.Get(pollRequest);

                yield return testRequest.SendWebRequest(); //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

                if (testRequest.isNetworkError || testRequest.isHttpError)
                {
                    if (AdGem.verboseLogging) Debug.Log(testRequest.error);
                }
                else
                {

                    try
                    {
                        JSONNode baseJson = JSON.Parse(testRequest.downloadHandler.text);

                        JSONNode dataNode = baseJson["data"].AsObject;

                        int amount = dataNode["amount"].AsInt;

                        if (amount > 0)
                        {
                            AdGemPlugin.processOfferWallReward(dataNode, amount, AdGem.offerWallRewardReceived);
                        }
                    }
                    catch (System.Exception ex) { if (AdGem.verboseLogging) Debug.Log(ex.ToString()); }

                }

                if (AdGemPlugin.pollCount < 50)
                    yield return new WaitForSeconds(3f);
                else if (AdGemPlugin.pollCount < 100)
                    yield return new WaitForSeconds(10f);
                else if (AdGemPlugin.pollCount < 1000)
                    yield return new WaitForSeconds(30f);
                else
                    yield return new WaitForSeconds(3600f);

                pollProcessRunning = false;
            }
        }




        private IEnumerator fetchAdvertisingId()
        {
            bool advertisingIdFetched = false;
            float timeWeHaveWaitedToGetAdvertisingId = 0f;

            bool ableToGetAdvertisingId = Application.RequestAdvertisingIdentifierAsync(
                (string advertisingId, bool trackingEnabled, string error) =>
                {
                    if ((advertisingId.Replace("-", "")).Contains("00000000000000000000"))
                    {
                        //if (AdGem.verboseLogging) Debug.Log("Found advertising id was all zeros");
                        //PlayerPrefs.SetString(AdGemPlugin.advertisingIdPrefsKey, advertisingId);
                        AdGemPlugin.adTrackingEnabled = false;
                    }

                    Debug.Log("advertisingId " + advertisingId + " " + trackingEnabled + " " + error);

                    AdGemPlugin.advertisingId = advertisingId;
                    PlayerPrefs.SetString(AdGemPlugin.advertisingIdPrefsKey, advertisingId);

                    //if we previously generated an adgem uid, don't replace it when replacing the advertising id.
                    if (!PlayerPrefs.HasKey(AdGemPlugin.adgemUidPrefsKey)) { PlayerPrefs.SetString(AdGemPlugin.adgemUidPrefsKey, advertisingId); }
                    
                    advertisingIdFetched = true;

                });

            if (!ableToGetAdvertisingId) { AdGemPlugin.adTrackingEnabled = false; }

            while (ableToGetAdvertisingId && !advertisingIdFetched && timeWeHaveWaitedToGetAdvertisingId < 3.0f)
            {
                timeWeHaveWaitedToGetAdvertisingId += 0.1f; //We will only wait a second to get this value before giving up.
                yield return new WaitForSeconds(0.1f);

                if (timeWeHaveWaitedToGetAdvertisingId > 2.8f) { AdGemPlugin.adTrackingEnabled = false; }
            }

            triedToFetchAdvertisingId = true;

            if (!PlayerPrefs.HasKey(AdGemPlugin.adgemUidPrefsKey)) { PlayerPrefs.SetString(AdGemPlugin.adgemUidPrefsKey, AdGemPlugin.randomString(15)); }
            if (!PlayerPrefs.HasKey(AdGemPlugin.advertisingIdPrefsKey)) { PlayerPrefs.SetString(AdGemPlugin.advertisingIdPrefsKey, "00000000000"); }
            AdGemPlugin.adgemUID = PlayerPrefs.GetString(AdGemPlugin.adgemUidPrefsKey);
            AdGemPlugin.advertisingId = PlayerPrefs.GetString(AdGemPlugin.advertisingIdPrefsKey);

        }

        public void refreshOfferWall()
        {
            if (!refreshingOfferWall && AdGemWebview.staticRef != null)
            {
                StartCoroutine(refreshOfferWallRoutine());
            }
        }
        private IEnumerator refreshOfferWallRoutine()
        {
            refreshingOfferWall = true;
            yield return new WaitForSeconds((float)AdGemWebview.failCount);

            AdGemPlugin.checkOfferWallExpiration();

        }


        public IEnumerator fireImpressionUrl(string url, float delay)
        {
            yield return new WaitForSeconds(delay);
            UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(url);
            yield return request.SendWebRequest();
        }

        public IEnumerator refreshAdGemVideos()
        {
            if (AdGem.verboseLogging) Debug.Log("AdGem - refreshAdGemVideos starting");


            if (!refreshingVideos)
            {
                refreshingVideos = true;

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //STATUS CHECK
                //Extend the expiration date if a video campaign passes the status check.
                if (AdGemPlugin.statusCheckNeeded())
                {
                    if (AdGem.verboseLogging) Debug.Log("AdGem - Video expired : checking status");

                    UnityEngine.Networking.UnityWebRequest statusCheckWebRequest = UnityEngine.Networking.UnityWebRequest.Get(AdGemPlugin.getStatusCheckUrl());
                    yield return statusCheckWebRequest.SendWebRequest(); //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

                    if (statusCheckWebRequest.isNetworkError || statusCheckWebRequest.isHttpError)
                    {
                        if (AdGem.verboseLogging) Debug.Log(statusCheckWebRequest.error);
                        yield return new WaitForSeconds(10f); //make sure to wait a little while before trying the network again. //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                    }
                    else
                    {
                        AdGemPlugin.processStatusCheckResponse(statusCheckWebRequest.downloadHandler.text);
                    }
                }
                //END STATUS CHECK
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////



                //TODO REMOVE THIS TEST HARD CODED URL
                //Api call to get video info
                //string testCacheRequestUrl = "https://apitesting.adgem.com/v1/hardcoded_webview?appid=1&platform=android&sdk_version=2";
                //"native"
                //string testCacheRequestUrl = "https://apitesting.adgem.com/v1/cache?appid=217&adgem_uid=test&standard=true&rewarded=true";
                //webview
                //string testCacheRequestUrl = "https://apitesting.adgem.com/v1/cache?appid=" + ((int)UnityEngine.Random.Range(1,999)).ToString() + "&adgem_uid=test&standard=true&rewarded=true&sdk_version=2";
                //UnityEngine.Networking.UnityWebRequest cacheWebRequest = UnityEngine.Networking.UnityWebRequest.Get(testCacheRequestUrl);
                //yield return cacheWebRequest.SendWebRequest();
                //Debug.Log("Adgem -CACHE url: " + testCacheRequestUrl);


                ////Api call to get video info
                UnityEngine.Networking.UnityWebRequest cacheWebRequest = UnityEngine.Networking.UnityWebRequest.Get(AdGemPlugin.getCacheVideoUrl());
                if (AdGem.verboseLogging) { Debug.Log("Adgem -CACHE url: " + AdGemPlugin.getCacheVideoUrl()); }
                yield return cacheWebRequest.SendWebRequest();



                if (cacheWebRequest.isNetworkError || cacheWebRequest.isHttpError)
                {
                    if (AdGem.verboseLogging) Debug.Log("Adgem - NETWORK ERROR: " + cacheWebRequest.error);
                    yield return new WaitForSeconds(10f); //make sure to wait a little while before trying the network again. //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                }
                else //Initial api call returned successfully
                {
                    bool needToDownloadinterstitialVideo = AdGemPlugin.usingInterstitialVideos && !AdGemPlugin.interstitialVideoReady;
                    bool needToDownloadRewardVideo = AdGemPlugin.usingRewardVideos && !AdGemPlugin.rewardVideoReady;

                    if (AdGem.verboseLogging) { Debug.Log("Adgem -CACHE response: " + cacheWebRequest.downloadHandler.text); }

                    JSONNode baseJson = JSON.Parse(cacheWebRequest.downloadHandler.text);

                    JSONArray dataJsonArray = baseJson["data"].AsArray;
                    if (dataJsonArray != null && dataJsonArray.Count > 0)
                    {
                        for (int i = 0; i < dataJsonArray.Count; i++)
                        {
                            JSONNode videoNode = dataJsonArray[i];

                            string impressionUrl = videoNode["impression_url"].Value;
                            float impressionDelay = videoNode["impression_delay"].AsFloat;

                            if (impressionUrl != null && !impressionUrl.Equals("null", StringComparison.InvariantCultureIgnoreCase) && impressionUrl.Length > 4)
                            {
                                StartCoroutine(fireImpressionUrl(impressionUrl, impressionDelay));
                            }

                            bool rewarded = videoNode["rewarded"].AsBool;
                            bool isWebview = string.Equals(videoNode["type"].Value, "webview", StringComparison.OrdinalIgnoreCase);

                            string video_cache_id = videoNode["video_cache_id"].Value;
                            if (rewarded) { rewardedVideoCacheId = video_cache_id; }
                            else { interstitialVideoCacheId = video_cache_id; }

                            Debug.Log("Adgem _-_-_-_-_-_- videoNode type: " + videoNode["type"].Value);

                            if (isWebview)
                            {
                                if (rewarded)
                                {
                                    rewardedVideoIsWebview = true;
                                    rewardedWebviewVideoAdUrl = videoNode["html_url"].Value;

                                    UnityEngine.Networking.UnityWebRequest unityWebRequest = UnityEngine.Networking.UnityWebRequest.Get(rewardedWebviewVideoAdUrl);
                                    yield return unityWebRequest.SendWebRequest();
                                    if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError) { Debug.Log("Adgem --- Error: " + unityWebRequest.error); }
                                    else
                                    {
                                        while (AdGemPlugin.videoIsPlaying) { yield return new WaitForSeconds(0.5f); } //don't overwrite the html until the current video finishes playing

                                        //Debug.Log("text: " + unityWebRequest.downloadHandler.text);
                                        rewardWebviewVideoHtmlString = unityWebRequest.downloadHandler.text;
                                        AdGemPlugin.rewardVideoReady = true;
                                    }
                                }
                                else
                                {
                                    interstitialVideoIsWebview = true;
                                    interstitialWebviewVideoAdUrl = videoNode["html_url"].Value;

                                    UnityEngine.Networking.UnityWebRequest unityWebRequest = UnityEngine.Networking.UnityWebRequest.Get(interstitialWebviewVideoAdUrl);
                                    yield return unityWebRequest.SendWebRequest();
                                    if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError) { Debug.Log("Adgem --- Error: " + unityWebRequest.error); }
                                    else
                                    {
                                        while (AdGemPlugin.videoIsPlaying) { yield return new WaitForSeconds(0.5f); } //don't overwrite the html until the current video finishes playing

                                        //Debug.Log("text: " + unityWebRequest.downloadHandler.text);
                                        interstitialWebviewVideoHtmlString = unityWebRequest.downloadHandler.text;
                                        AdGemPlugin.interstitialVideoReady = true;
                                    }
                                }

                                AdGemWebview.initParams = videoNode["webview_init_params"].Value;
                                if(AdGem.verboseLogging) { Debug.Log("Adgem - weview init params: " + AdGemWebview.initParams); }
                            }
                            else //"native" video type
                            {
                                if(rewarded) { rewardedVideoIsWebview = false; }
                                else { interstitialVideoIsWebview = false; }

                                if ((!rewarded && needToDownloadinterstitialVideo) || (rewarded && needToDownloadRewardVideo))
                                {
                                    //for end card type 4, we will not get video data, so we will do things differently and skip straight to the end card
                                    //This situation is non-rewarded only, and there will not be an icon to download since it works similarly to full screen end type
                                    if (videoNode["end_type"].AsInt == 4 && videoNode["image"].Value != null && !videoNode["image"].Value.Equals("null"))
                                    {
                                        if (AdGem.verboseLogging) { Debug.Log("Adgem - Downloading Image"); }

                                        //Download end card image
                                        UnityEngine.Networking.UnityWebRequest endCardImageWebRequest = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(videoNode["image"].Value);
                                        endCardImageWebRequest.timeout = 15;
                                        yield return endCardImageWebRequest.SendWebRequest(); //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                                        if (endCardImageWebRequest.isNetworkError || endCardImageWebRequest.isHttpError) { if (AdGem.verboseLogging) Debug.Log("Adgem--- Error retreiving end card image data: " + endCardImageWebRequest.error); }
                                        else //End card image data received
                                        {
                                            AdGemPlugin.processDownloadedEndCardTexture(endCardImageWebRequest, rewarded);
                                        }

                                        AdGemPlugin.persistVideoInfo(rewarded, videoNode);
                                        yield return new WaitForSeconds(0.5f);
                                        AdGemPlugin.interstitialVideoReady = true;
                                    }
                                    else if (videoNode["video_url"].Value != null && !videoNode["video_url"].Value.Equals("null"))
                                    {
                                        //Download video data
                                        UnityEngine.Networking.UnityWebRequest videoWebRequest = UnityEngine.Networking.UnityWebRequest.Get(videoNode["video_url"].Value);
                                        videoWebRequest.timeout = 60;
                                        yield return videoWebRequest.SendWebRequest(); //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                                        if (videoWebRequest.isNetworkError || videoWebRequest.isHttpError) { if (AdGem.verboseLogging) Debug.Log("Adgem--- Error retreiving video data: " + videoWebRequest.error); }
                                        else
                                        {

                                            while ((AdGemPlugin.videoIsPlaying || AdGemPlugin.customEndCardShowing || AdGemPlugin.fullScreenEndCardShowing) && ((rewarded && AdGemPlugin.rewardMode) || (!rewarded && !AdGemPlugin.rewardMode))) //Only wait in the case that we just downloaded the type of video that is now playing.
                                            {
                                                yield return new WaitForSeconds(0.3f); //If a video is currently playing, wait until it finishes before saving the new video data to the persistent storage.
                                            }
                                            yield return new WaitForSeconds(0.2f); //Wait just a second to make sure the video player is properly shut down.

                                            AdGemPlugin.processDownloadedVideoData(videoWebRequest, rewarded, videoNode["video_url"].Value, AdGem.rewardVideoDownloadCompleted, AdGem.interstitialVideoDownloadCompleted);
                                        }

                                        if (videoNode["icon"].Value != null && !videoNode["icon"].Value.Equals("null"))
                                        {
                                            //Download video icon
                                            UnityEngine.Networking.UnityWebRequest iconWebRequest = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(videoNode["icon"].Value);
                                            iconWebRequest.timeout = 15;
                                            yield return iconWebRequest.SendWebRequest(); //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                                            if (iconWebRequest.isNetworkError || iconWebRequest.isHttpError) { if (AdGem.verboseLogging) Debug.Log("Adgem--- Error retreiving video icon image data: " + iconWebRequest.error); }
                                            else //Video icon image data received
                                            {
                                                AdGemPlugin.processDownloadedIconTexture(iconWebRequest, rewarded);
                                            }
                                        }
                                        if (videoNode["image"].Value != null && !videoNode["image"].Value.Equals("null"))
                                        {
                                            if (AdGem.verboseLogging) { Debug.Log("Adgem - Downloading Image"); }

                                            //Download end card image
                                            UnityEngine.Networking.UnityWebRequest endCardImageWebRequest = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(videoNode["image"].Value);
                                            endCardImageWebRequest.timeout = 15;
                                            yield return endCardImageWebRequest.SendWebRequest(); //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                                            if (endCardImageWebRequest.isNetworkError || endCardImageWebRequest.isHttpError) { if (AdGem.verboseLogging) Debug.Log("Adgem--- Error retreiving end card image data: " + endCardImageWebRequest.error); }
                                            else //End card image data received
                                            {
                                                AdGemPlugin.processDownloadedEndCardTexture(endCardImageWebRequest, rewarded);
                                            }

                                        }

                                        AdGemPlugin.persistVideoInfo(rewarded, videoNode);

                                        //I was having issues when immediately starting a video after the ready flag gets set to true, and don't understand why. This is an attempt to hack a solution by waiting half a second.
                                        if (AdGemPlugin.rewardVideoWeProcessedSuccessfully)
                                        {
                                            yield return new WaitForSeconds(0.5f);
                                            AdGemPlugin.rewardVideoWeProcessedSuccessfully = false;
                                            AdGemPlugin.rewardVideoReady = true;
                                        }
                                        if (AdGemPlugin.interstitialVideoWeProcessedSuccessfully)
                                        {
                                            yield return new WaitForSeconds(0.5f);
                                            AdGemPlugin.interstitialVideoWeProcessedSuccessfully = false;
                                            AdGemPlugin.interstitialVideoReady = true;
                                        }

                                    }
                                    else
                                    {
                                        yield return new WaitForSeconds(1f); //make sure to wait a little while before trying the network again. This wait should happen if there should have been video data but there was none. This wait should never happen if the end type is 4, which means we will be skipping the video.
                                    }

                                }

                            }

                        }
                    }
                    else //In this case, the Data array was missing or empty. It's not likely that the data will be any better on the next pull.
                    {
                        if (AdGem.verboseLogging) Debug.Log("adgem--- Error: Data array was null");
                        yield return new WaitForSeconds(10);
                    }
                    //End get data

                }



                if ((AdGemPlugin.usingInterstitialVideos && !AdGemPlugin.interstitialVideoReady) || (AdGemPlugin.usingRewardVideos && !AdGemPlugin.rewardVideoReady))
                {
                    //In this case, something went wrong because we didn't get the data we expected. Wait a while as to not spam the server
                    if (AdGem.verboseLogging) Debug.Log("adgem--- Error: Expected video data not found");
                    yield return new WaitForSeconds(5);
                }



                yield return new WaitForSeconds(0.1f);
                refreshingVideos = false;
            }
        }



    }


}
