using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdGemSimpleJSON;
using System.Security.Cryptography;

namespace AdGemUnity
{
    public class AdGemWebview : MonoBehaviour
    {
        UniWebView webView;
        public static AdGemWebview staticRef;
        public static int failCount = 0;
        public static bool currentWebviewVideoIsRewarded = false;
        private bool rewardVideoWasClosedEarly = false;
        public static string initParams = "";

        private bool offerWallLoading = false;
        private bool adLoading = false;
        private bool waitingForPlaySuccessConfirmation = false;
        public bool webviewAdLoadRequested = false;

        private enum WebviewMode { offerWall, videoAd };
        private WebviewMode webviewMode = WebviewMode.offerWall;

        private void Start()
        {
            staticRef = this;

            //Removing this because we are loading the offerwall on the fly now
            //AdGemPlugin.offerWallReady = false; //If this object gets refreshed, make sure to refresh the contents

            initWebviewIfNeeded();
            AdGemPlugin.offerWallReady = true; //not caching for now
        }

        public void initWebviewIfNeeded()
        {
            if (webView == null)
            {
                UniWebView.SetAllowAutoPlay(true);
                UniWebView.SetAllowInlinePlay(true);

                webView = gameObject.AddComponent<UniWebView>();
                webView.SetLoadWithOverviewMode(true);
                webView.SetBackButtonEnabled(false);

                //Prevent the back button from destroying and recycling the webview
                webView.OnShouldClose += (view) =>
                {
                    closeWebView();
                    return false; //returning false stops the webview from being closed.
                };

                webView.OnPageFinished += (view, statusCode, url) =>
                {
                    if (webviewMode == WebviewMode.offerWall)
                    {
                        offerWallLoading = false;
                        adLoading = false;

                        AdGemPlugin.offerWallExpirationDate = AdGemPlugin.CurrentTimeMillis() + (AdGemPlugin.milliesPerMinute * 300);
                        if (AdGemManager.staticRef != null) { AdGemManager.staticRef.refreshingOfferWall = false; }

                        failCount = 0;

                        StartCoroutine(refreshWindowDimensions());
                    }

                    if (webviewMode == WebviewMode.videoAd)
                    {
                        offerWallLoading = false;
                        adLoading = false;

                        webView.Show();
                        StartCoroutine(waitThenStartAdVideoWithJavascriptCall());
                        StartCoroutine(refreshWindowDimensions());
                    }
                };

                webView.OnPageErrorReceived += (view, statusCode, errorMessage) =>
                {
                    if (AdGemManager.staticRef != null) { AdGemManager.staticRef.refreshingOfferWall = false; }

                    if (failCount < 15) failCount++;

                    Debug.Log("ERROR Loading web page! " + errorMessage);
                };

                webView.OnOrientationChanged += (view, orientation) =>
                {
                    StartCoroutine(refreshWindowDimensions());
                };

                webView.AddUrlScheme("adgem");

                webView.OnMessageReceived += (view, message) =>
                {
                    processAdgemUrlSchemeMessage(message);
                };
            }
        }

        //TESTING - TODO - remove
        //public void showWebviewVideoAd(bool test)
        //{
        //    if (webView != null)
        //    {
        //        adLoading = true;
        //        webView.Load("http://adgem-dashboard-staging.s3.us-east-2.amazonaws.com/campaigns/2843/webviews/landscape/c2843_video_webview.html");
        //        webView.OnPageFinished += (view, statusCode, url) =>
        //        {
        //            offerWallLoading = false;
        //            adLoading = false;
        //            webView.Show();
        //            StartCoroutine(waitThenStartAdVideoWithJavascriptCall());
        //            StartCoroutine(refreshWindowDimensions());
        //        };
        //    }
        //    else
        //    {
        //        Debug.Log("Adgem--- Error! Webview not initialized.");
        //    }
        //}

        public void showWebviewVideoAd(bool rewarded)
        {
            if(AdGem.verboseLogging) { Debug.Log("Adgem - showWebviewVideoAd()"); }

            webviewMode = WebviewMode.videoAd;

            adLoading = true;

            webView.LoadHTMLString(rewarded ? AdGemManager.rewardWebviewVideoHtmlString : AdGemManager.interstitialWebviewVideoHtmlString, "http://google.com", false);
        }

        public IEnumerator waitThenStartAdVideoWithJavascriptCall()
        {
            //if (AdGem.verboseLogging) { Debug.Log("Adgem - waitThenStartAdVideoWithJavascriptCall()"); }

            yield return new WaitForSeconds(0.05f);

            waitingForPlaySuccessConfirmation = true;

            if (AdGem.verboseLogging) { Debug.Log("Adgem - JAVASCRIPT CALL: " + "init(" + initParams + "," + AdGemPlugin.appSessionId + ");"); }

            //webView.EvaluateJavaScript("init_debug();", (payload) =>
            webView.EvaluateJavaScript("init(" + initParams + ",'" + AdGemPlugin.appSessionId + "');", (payload) =>
            {
                if (payload.resultCode.Equals("0"))
                {
                    if (AdGem.verboseLogging) Debug.Log("Starting webview ad video!");
                }
                else
                {
                    Debug.Log("Something went wrong starting the ad video: " + payload.data);
                }
            });

            //Make sure the play success message is received
            float waitTime = 0f;
            while (waitingForPlaySuccessConfirmation && waitTime < 5.5f)
            {
                Debug.Log("Adgem - waiting for confirmation...");

                if (waitTime > 5f)
                {
                    waitingForPlaySuccessConfirmation = false;
                    if (webView != null)
                    {
                        Debug.Log("Adgem - closing webview because did not get confirmation **********------------*********");
                        webView.Stop();
                        webView.CleanCache();
                        webView.Hide(false);
                        AdGem.videoIsPlaying = false;
                        if (AdGem.videoFailedToLoad != null) { AdGem.videoFailedToLoad(); }
                    }
                }

                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }
        }




        public void loadUrl(string theUrl)
        {
            if (webView != null)
            {
                webView.Load(theUrl);
                StartCoroutine(waitForPageToLoadThenShowWebview());
            }
        }

        private IEnumerator refreshWindowDimensions()
        {
            for (int i = 0; i < 10; i++)
            {
                webView.Frame = new Rect(0, 0, Screen.width, Screen.height);
                yield return new WaitForSeconds(0.1f);
            }
        }

        public void showOfferWall()
        {
            waitingForPlaySuccessConfirmation = false;
            webviewMode = WebviewMode.offerWall;

            

            if (webView != null)
            {
                if (AdGem.verboseLogging) { Debug.Log("Adgem - Offerwall url : " + AdGemPlugin.getOfferWallUrl(true)); }

                offerWallLoading = true;

                webView.Load(AdGemPlugin.getOfferWallUrl(true));

                StartCoroutine(waitForPageToLoadThenShowWebview());
            }
            else
            {
                Debug.Log("Adgem--- Error! Webview not initialized.");
            }
        }

        private IEnumerator waitForPageToLoadThenShowWebview()
        {
            float elapsedTime = 0f;
            float maxTimeToWait = 10f;
            while (offerWallLoading && elapsedTime < maxTimeToWait)
            {
                yield return new WaitForSeconds(0.15f);
                elapsedTime += 0.15f;
            }
            if (elapsedTime < maxTimeToWait)
            {
                webView.Show(false, UniWebViewTransitionEdge.Bottom, 0.35f);

                StartCoroutine(refreshWindowDimensions());
                StartCoroutine(sendOfferWallOpenedConfirmation());
            }

        }

        //private IEnumerator waitForAdToLoadThenShowWebview()
        //{
        //    float elapsedTime = 0f;
        //    float maxTimeToWait = 10f;
        //    bool adPageLoaded = false;
        //    while (adLoading && elapsedTime < maxTimeToWait)
        //    {
        //        yield return new WaitForSeconds(0.15f);
        //        elapsedTime += 0.15f;

        //        if (AdGem.verboseLogging) { Debug.Log("Adgem - webview ad loading...."); }
        //    }
        //    if (elapsedTime < maxTimeToWait)
        //    {
        //        webView.Show(false, UniWebViewTransitionEdge.None, 0.35f);

        //        adPageLoaded = true;

        //        StartCoroutine(refreshWindowDimensions());
        //        StartCoroutine(waitThenStartAdVideoWithJavascriptCall());
        //    }

        //    if (!adPageLoaded)
        //    {
        //        AdGemPlugin.videoIsPlaying = false;
        //        Debug.Log("Adgem - Ad video webview failed to load!");
        //    }

        //    webviewAdLoadRequested = false;
        //}


        private IEnumerator sendOfferWallOpenedConfirmation()
        {
            UnityEngine.Networking.UnityWebRequest webRequest = UnityEngine.Networking.UnityWebRequest.Get(AdGemPlugin.getOfferWallOpenedUrl());
            yield return webRequest.SendWebRequest();
        }

        public void closeNotRewarded()
        {
            rewardVideoWasClosedEarly = true;
            closeWebView();
        }

        public void closeWebView()
        {
            if (AdGem.videoIsPlaying) //when closing the webview, if it was a video ad rather than the offer wall, do some specific stuff like call delegates.
            {
                if (currentWebviewVideoIsRewarded)
                {
                    if (rewardVideoWasClosedEarly)
                    {
                        if (AdGem.rewardVideoCanceled != null) { AdGem.rewardVideoCanceled(); }

                        //--webview handles this now
                        //AdGemManager.staticRef.fireEventEndpoint("videoskip", "rewarded-video", AdGemManager.rewardedVideoCacheId);
                    }
                    else
                    {
                        if (AdGem.rewardVideoFinishedPlaying != null) { AdGem.rewardVideoFinishedPlaying(); }

                        //--webview handles this now
                        //AdGemManager.staticRef.fireEventEndpoint("videocomplete", "rewarded-video", AdGemManager.rewardedVideoCacheId);
                    }
                }
                else
                {
                    if (AdGem.interstitialVideoFinishedPlaying != null) { AdGem.interstitialVideoFinishedPlaying(); }

                    //--webview handles this now
                    //AdGemManager.staticRef.fireEventEndpoint("videocomplete", "nonrewarded-video", AdGemManager.interstitialVideoCacheId);
                }
                rewardVideoWasClosedEarly = false;
            }
            AdGem.videoIsPlaying = false;
            AdGemPlugin.videoIsPlaying = false;



            //Right now we don't have a delegate for offerWallClosed
            //if (AdGemPlugin.offerWallClosed != null)
            //{
            //    AdGemPlugin.offerWallClosed();
            //}

            if (webView != null)
            {
                webView.Hide(false);
                //AdGemPlugin.offerWallReady = false;
            }
        }

        private void processAdgemUrlSchemeMessage(UniWebViewMessage message)
        {
            if (AdGem.verboseLogging)
            {
                Debug.Log("Adgem: adgem url scheme message received");
                Debug.Log("Adgem: " + message.RawMessage);
            }

            if (message.Path.Equals("close", System.StringComparison.InvariantCultureIgnoreCase))
            {
                closeWebView();
            }
            else if (message.Path.Equals("close-not-rewarded", System.StringComparison.InvariantCultureIgnoreCase))
            {
                closeNotRewarded();
            }
            else if (message.Path.Equals("play-success", System.StringComparison.InvariantCultureIgnoreCase))
            {
                waitingForPlaySuccessConfirmation = false;
                if (AdGem.videoAdStarted != null) { AdGem.videoAdStarted(); }
            }
            else //otherwise, the path should be a url ecoded base64 encoded json string
            {
                try
                {
                    string base64payload = message.Path; //Uniwebview automatically url-decodes
                    string jsonString = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64payload));

                    //Debug.Log("Adgem--- offer wall message json string ---***--- " + jsonString);

                    JSONNode baseJson = JSON.Parse(jsonString); if (baseJson == null) { return; }

                    string actionType = baseJson["action"].Value;
                    string url = baseJson["url"].Value; if (url == null) { url = ""; }

                    //Debug.Log("url: " + url);

                    string storeID = baseJson["store_id"].Value; if (storeID == null) { storeID = ""; }
                    string app_id = baseJson["app_id"].Value; if (app_id == null) { app_id = ""; }
                    int amount = baseJson["amount"].AsInt;

                    if (actionType.Equals("browser", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        //Debug.Log("browser");
                        Application.OpenURL(url);

                        //Start polling for an offer completion
                        AdGemManager.staticRef.startPollingProcess();
                    }
                    if (actionType.Equals("appstore", System.StringComparison.InvariantCultureIgnoreCase) && AdGemPrefabController.staticRef != null) //appstore //store_id
                    {
                        //Start polling for an offer completion
                        AdGemManager.staticRef.startPollingProcess();

                        //Debug.Log("appstore");
                        AdGemPrefabController.staticRef.userClicked(url, app_id);

                    }
                    //if (actionType.Equals("reward", System.StringComparison.InvariantCultureIgnoreCase) && AdGemPlugin.offerWallRewardReceived != null)
                    //{
                    //    Debug.Log("Adgem--- **** Reward action fired");
                    //    AdGemPlugin.processOfferWallReward(baseJson, amount);
                    //}

                }
                catch (System.Exception ex)
                {
                    Debug.Log("Adgem--- " + ex.ToString());
                }

            }

        }

    }

}