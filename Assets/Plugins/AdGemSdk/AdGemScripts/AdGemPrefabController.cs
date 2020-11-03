using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using AdGemUnity;

#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

public class AdGemPrefabController : MonoBehaviour
{
    public static AdGemPrefabController staticRef;

    public GameObject goAdGemVideoCamera, goAdGemCanvas, videoHolder, goBlackBg, countdownRingBg, countdownRing, countdownTextObject, xButtonObject, xButtonCLickableAreaObject, endCardImageObjectHolder, endCardImageObject, iconImageObject, iconMaskObject, customEndCardPanel, customEndCardInfoBg, goTitleText, goDownloadButton, goDescriptionText, goDownloadButtonText, goStars, goAdgemLogo, goReviewCount, goWebview;
    public RectTransform carouselRect, carouselContainerRect;
    AdGemWebview adGemWebview;

    private RectTransform videoHolderRect, blackBgRect, countdownRingBgRect, countdownRingrect, countdownTextRect, xButtonRect, xButtonClickableAreaRect, endCardImageHolderRect, endCardImageRect, iconImageRect, iconMaskRect, customEndCardPanelRect, customEndCardInfoBgRect, titleTextRect, descriptionTextRect, starsRect, adgemLogoRect, reviewCountRect, downloadButtonRect, downloadButtonTextRect;
    private Image countdownRingImage, endCardImage, iconImage;
    private Text countdownText, titleText, descriptionText, downloadButtonText, reviewCountText;
    private VideoPlayer videoPlayer;
    private RawImage vidoePlayerRawImage;
    private AudioSource audioSource;

    private bool xButtonWasClicked = false;

    AudioSource[] audioSources; //All the audio sources currently in the scene
    float[] audioSourceVolumes; //Volume of each existing audio source

    GameObject[] gameObjectsInScene;
    bool[] sceneObjectActiveStatus;

    float countdownTimeLeft, currentVideoLength;

    private int currentVideoEndType = 0;

#if UNITY_IOS
    [DllImport ("__Internal")] private static extern void _OpenAppInStore(int appID);
#endif





    ////TODO - REMOVE TEST CODE
    //public void testWebviewAd()
    //{
    //    offerwWall.showOfferWall("https://d2cmftj7qxns8m.cloudfront.net/campaigns/3005/webviews/c3005_portrait_webview.html");
    //}





    void Awake()
    {
        if (staticRef == null)
        {
            staticRef = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Use this for initialization
    void Start()
    {
        //In case this object gets recreated, we need to make sure a few settings are cleared:
        AdGemPlugin.fullScreenEndCardShowing = AdGemPlugin.customEndCardShowing = AdGemPlugin.carouselEndCardShowing = false;



        videoPlayer = goAdGemVideoCamera.GetComponent<VideoPlayer>();
        videoPlayer.loopPointReached += VideoEndReached;
        videoPlayer.errorReceived += VideoPlayerErrorReceived;
        audioSource = gameObject.AddComponent<AudioSource>();
        vidoePlayerRawImage = videoHolder.GetComponent<RawImage>();
        endCardImageHolderRect = endCardImageObjectHolder.GetComponent<RectTransform>();
        endCardImageRect = endCardImageObject.GetComponent<RectTransform>();
        iconImageRect = iconImageObject.GetComponent<RectTransform>();
        iconMaskRect = iconMaskObject.GetComponent<RectTransform>();
        videoHolderRect = videoHolder.GetComponent<RectTransform>();
        customEndCardInfoBgRect = customEndCardInfoBg.GetComponent<RectTransform>();
        customEndCardPanelRect = customEndCardPanel.GetComponent<RectTransform>();
        countdownRingBgRect = countdownRingBg.GetComponent<RectTransform>();
        countdownRingrect = countdownRing.GetComponent<RectTransform>();
        blackBgRect = goBlackBg.GetComponent<RectTransform>();
        countdownTextRect = countdownTextObject.GetComponent<RectTransform>();
        countdownText = countdownTextObject.GetComponent<Text>();
        countdownRingImage = countdownRing.GetComponent<Image>();
        titleText = goTitleText.GetComponent<Text>();
        titleTextRect = goTitleText.GetComponent<RectTransform>();
        descriptionText = goDescriptionText.GetComponent<Text>();
        descriptionTextRect = goDescriptionText.GetComponent<RectTransform>();
        starsRect = goStars.GetComponent<RectTransform>();
        downloadButtonRect = goDownloadButton.GetComponent<RectTransform>();
        downloadButtonText = goDownloadButtonText.GetComponent<Text>();
        downloadButtonTextRect = goDownloadButtonText.GetComponent<RectTransform>();
        reviewCountRect = goReviewCount.GetComponent<RectTransform>();
        reviewCountText = goReviewCount.GetComponent<Text>();
        adgemLogoRect = goAdgemLogo.GetComponent<RectTransform>();
        iconImage = iconImageObject.GetComponent<Image>();
        endCardImage = endCardImageObject.GetComponent<Image>();
        xButtonClickableAreaRect = xButtonCLickableAreaObject.GetComponent<RectTransform>();
        xButtonRect = xButtonObject.GetComponent<RectTransform>();
        adGemWebview = goWebview.GetComponent<AdGemWebview>();




        AdGemPlugin.initPrefabElements(videoPlayer, audioSource, videoHolderRect, vidoePlayerRawImage, iconImageRect, blackBgRect, iconMaskRect, countdownRingBgRect,
         countdownRingrect, countdownText, countdownTextRect, countdownRingImage, titleText, titleTextRect, descriptionText, descriptionTextRect, starsRect, downloadButtonRect,
         downloadButtonText, downloadButtonTextRect, reviewCountText, adgemLogoRect, iconImage, xButtonRect, xButtonClickableAreaRect, goWebview, goAdGemCanvas, goAdGemVideoCamera, xButtonObject);

    }

    // Update is called once per frame
    void Update()
    {
        if (AdGemPlugin.videoIsPlaying)
        {
            countdownRingImage.fillAmount = (countdownTimeLeft / currentVideoLength);
            countdownText.text = ((int)countdownTimeLeft + 1).ToString();
            countdownTimeLeft -= Time.unscaledDeltaTime;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (xButtonObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(xButtonClickableAreaRect, new Vector2(Input.mousePosition.x, Input.mousePosition.y)))
            {
                xButtonClicked();
            }
            else if (AdGemPlugin.fullScreenEndCardShowing)
            {
                if (AdGem.adClicked != null) { AdGem.adClicked(); }

                if (AdGemPlugin.rewardMode)
                {
                    if (AdGem.verboseLogging) { Debug.Log("Adgem - click event being sent from adgem prefab controller - rewarded, full screen endcard"); }

                    userClicked(AdGemPlugin.rewardVideoTrackingUrl, AdGemPlugin.rewardVideoAppStoreAppId);
                    AdGemManager.staticRef.fireEventEndpoint("click", "rewarded-video", AdGemManager.rewardedVideoCacheId);
                }
                else
                {
                    if (AdGem.verboseLogging) { Debug.Log("Adgem - click event being sent from adgem prefab controller - non-rewarded, full screen endcard"); }

                    userClicked(AdGemPlugin.standardVideoTrackingUrl, AdGemPlugin.standardVideoAppStoreAppId);
                    AdGemManager.staticRef.fireEventEndpoint("click", "nonrewarded-video", AdGemManager.interstitialVideoCacheId);
                }
            }
            else if(AdGemPlugin.carouselEndCardShowing)
            {
                //add carousel rectTransformUtility check if click is on carousel, ignore.
                if(RectTransformUtility.RectangleContainsScreenPoint(customEndCardPanelRect, new Vector2(Input.mousePosition.x, Input.mousePosition.y))){
                    if (AdGem.adClicked != null) { AdGem.adClicked(); }
                    if (AdGemPlugin.rewardMode)
                    {
                        userClicked(AdGemPlugin.rewardVideoTrackingUrl, AdGemPlugin.rewardVideoAppStoreAppId);
                    }
                    else
                    {
                        userClicked(AdGemPlugin.standardVideoTrackingUrl, AdGemPlugin.standardVideoAppStoreAppId);
                    }
                }
            }
            else if (AdGemPlugin.customEndCardShowing)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(downloadButtonRect, new Vector2(Input.mousePosition.x, Input.mousePosition.y)))
                {
                    if (AdGem.adClicked != null) { AdGem.adClicked(); }

                    if (AdGemPlugin.rewardMode)
                    {
                        if (AdGem.verboseLogging) { Debug.Log("Adgem - click event being sent from adgem prefab controller - rewarded, custom endcard"); }

                        userClicked(AdGemPlugin.rewardVideoTrackingUrl, AdGemPlugin.rewardVideoAppStoreAppId);
                        AdGemManager.staticRef.fireEventEndpoint("click", "rewarded-video", AdGemManager.rewardedVideoCacheId);
                    }
                    else
                    {
                        if (AdGem.verboseLogging) { Debug.Log("Adgem - click event being sent from adgem prefab controller - non-rewarded, custom endcard"); }

                        userClicked(AdGemPlugin.standardVideoTrackingUrl, AdGemPlugin.standardVideoAppStoreAppId);
                        AdGemManager.staticRef.fireEventEndpoint("click", "nonrewarded-video", AdGemManager.interstitialVideoCacheId);
                    }
                }
            }
        }
    }



    public void showOfferWall()
    {
        
        bool showOfferWallInBrowser = true;

        //Debug.Log(">>>>>>>>>>>> os version: " + GetMajorOsVersion(AdGemPlugin.getOsVersion()));

        int osMajorVersion = AdGem.GetMajorOsVersion(AdGemPlugin.getOsVersion());
#if UNITY_ANDROID
            if (osMajorVersion >= 5) { showOfferWallInBrowser = false; }
#endif
#if UNITY_IOS
            if (osMajorVersion >= 9) { showOfferWallInBrowser = false; }
#endif

        if (AdGemPlugin.openOfferWallInBrowser) { showOfferWallInBrowser = true; }

        if(showOfferWallInBrowser)
        {
            if (AdGem.verboseLogging) { Debug.Log("Adgem - showing offer wall in browser"); }

            Application.OpenURL(AdGemPlugin.getOfferWallUrl(false));
            AdGemManager.staticRef.startPollingProcess();
            if(AdGem.offerWallClosed != null) { StartCoroutine(waitThenCallOfferWallClosed()); }
        }
        else
        {
            if (goWebview != null)
            {
                adGemWebview.showOfferWall();
            }
            else
            {
                Debug.Log("Adgem --- Error! Offer wall not initialized.");
            }
        }
    }
    private IEnumerator waitThenCallOfferWallClosed()
    {
        yield return new WaitForSeconds(0.3f);
        AdGem.offerWallClosed();
    }


    public void playVideo(bool rewarded)
    {
        if (AdGem.verboseLogging) { Debug.Log("Adgem ----()-----() playVideo ()-----()-----"); }

        if (AdGem.verboseLogging) Debug.Log("Adgem standard? " + rewarded);
        if (AdGem.verboseLogging) Debug.Log("Adgem end type? " + AdGemPlugin.standardVideoEndType);

        if (AdGemPlugin.videoIsPlaying)
        {
            if (AdGem.verboseLogging) Debug.Log("Error: You cannot start a video ad when another ad is already playing");
            return; //Make sure one video stops playing before another starts.
        }

        //Reposition the elements in case the resolution has changed
        AdGemPlugin.initPrefabElements(videoPlayer, audioSource, videoHolderRect, vidoePlayerRawImage, iconImageRect, blackBgRect, iconMaskRect, countdownRingBgRect,
         countdownRingrect, countdownText, countdownTextRect, countdownRingImage, titleText, titleTextRect, descriptionText, descriptionTextRect, starsRect, downloadButtonRect,
         downloadButtonText, downloadButtonTextRect, reviewCountText, adgemLogoRect, iconImage, xButtonRect, xButtonClickableAreaRect, goWebview, goAdGemCanvas, goAdGemVideoCamera, xButtonObject);

        AdGemPlugin.rewardMode = rewarded;

        //This is logic to skip the video if the end type is "no video"
        if (!rewarded && AdGemPlugin.standardVideoEndType == AdGemPlugin.END_TYPE_NO_VIDEO)
        {
            if (AdGem.verboseLogging) { Debug.Log("Image only ad type - playVideo fired. *****"); }
            goAdGemCanvas.SetActive(true);
            endCardImageObject.SetActive(true);
            currentVideoEndType = AdGemPlugin.END_TYPE_NO_VIDEO;
            AdGemPlugin.fullScreenEndCardShowing = true;
            StartCoroutine(MonitorScreenSize());
            VideoEndReached(videoPlayer);
            AdGemPlugin.interstitialVideoReady = false;
            return;
        }

        xButtonObject.SetActive(false);
        customEndCardPanel.SetActive(false);
        

        // Start playback. This means the VideoPlayer may have to prepare (reserve
        // resources, pre-load a few frames, etc.). To better control the delays
        // associated with this preparation one can use videoPlayer.Prepare() along with
        // its prepareCompleted event.
        try
        {
            bool isIos =
#if UNITY_IPHONE
            true;
#else 
            false;
#endif
            videoPlayer.source = VideoSource.Url;
            if (rewarded)
            {
                if (AdGemPlugin.rewardVideoReady)
                {
                    currentVideoEndType = AdGemPlugin.rewardVideoEndType;
                    videoPlayer.url = Application.persistentDataPath + "/" + AdGemPlugin.rewardVideoFilename;
                    titleText.text = AdGemPlugin.rewardVideoName;

                    if (titleText.text.Length > 45) { titleText.fontSize = AdGemPlugin.smallerTitleTextFontSize; }
                    else { titleText.fontSize = AdGemPlugin.standardTitleTextFontSize; }

                    reviewCountText.text = "(" + AdGemPlugin.rewardVideoReviewCount.ToString() + ")";
                    downloadButtonText.text = AdGemPlugin.rewardVideoButtonText;
                    if (AdGemPlugin.rewardVideoExitButtonSeconds >= 0) { StartCoroutine(waitThenTurnOnXButton((float)AdGemPlugin.rewardVideoExitButtonSeconds)); }
                    if(AdGemPlugin.rewardVideoEndType == AdGemPlugin.END_TYPE_CAROUSEL)
                    {
                        AdGemPlugin.loadCarouselData(rewarded, isIos, carouselContainerRect.gameObject, newEndType =>
                        {
                            if(!AdGemPlugin.customEndCardShowing && !AdGemPlugin.fullScreenEndCardShowing)
                            {
                                currentVideoEndType = newEndType;
                            }
                        });
                    }
                }
                else
                {
                    if (AdGem.verboseLogging) Debug.Log("Adgem--- Reward Video not ready");
                    return;
                }
            }
            else
            {
                if (AdGemPlugin.interstitialVideoReady)
                {
                    //Debug.Log("standard video was ready!");

                    currentVideoEndType = AdGemPlugin.standardVideoEndType;
                    videoPlayer.url = Application.persistentDataPath + "/" + AdGemPlugin.standardVideoFilename;
                    titleText.text = AdGemPlugin.standardVideoName;

                    if (titleText.text.Length > 45) { titleText.fontSize = AdGemPlugin.smallerTitleTextFontSize; }
                    else { titleText.fontSize = AdGemPlugin.standardTitleTextFontSize; }

                    reviewCountText.text = "(" + AdGemPlugin.standardVideoReviewCount.ToString() + ")";
                    downloadButtonText.text = AdGemPlugin.standardVideoButtonText;
                    if (AdGemPlugin.standardVideoExitButtonSeconds >= 0) { StartCoroutine(waitThenTurnOnXButton((float)AdGemPlugin.standardVideoExitButtonSeconds)); }
                    if (AdGemPlugin.standardVideoEndType == AdGemPlugin.END_TYPE_CAROUSEL)
                    {
                        AdGemPlugin.loadCarouselData(rewarded, isIos, carouselContainerRect.gameObject, newEndType =>
                        {
                            if (!AdGemPlugin.customEndCardShowing && !AdGemPlugin.fullScreenEndCardShowing)
                            {
                                currentVideoEndType = newEndType;
                            }
                        });
                    }
                }
                else
                {
                    if (AdGem.verboseLogging) Debug.Log("Adgem--- Standard Video not ready");
                    return;
                }
            }



            StartCoroutine(prepareThenPlayVideo());
        }
        catch (Exception ex)
        {
            if (AdGem.verboseLogging) Debug.Log("Adgem--- " + ex.ToString());
        }

    }
    private IEnumerator prepareThenPlayVideo()
    {
        if(AdGem.verboseLogging) Debug.Log("Adgem--- prepareThenPlayVideo starting");

        goAdGemCanvas.SetActive(true);
        goAdGemVideoCamera.SetActive(true);
        endCardImageObject.SetActive(false);
        countdownRing.SetActive(true);
        countdownRingBg.SetActive(true);
        countdownTextObject.SetActive(true);
        Vector3 correctVideoHolderPosition = videoHolderRect.localPosition;
        videoHolderRect.localPosition = new Vector3(Screen.width * 5, 0, 1); //move the video holder off screen

        videoPlayer.Prepare();
        float prepareWaitTime = 0f; //only wait 5 seconds to prepare.
        while (!videoPlayer.isPrepared && prepareWaitTime < 5f)
        {
            yield return new WaitForSeconds(0.1f);
            prepareWaitTime += 0.1f;

            if (AdGem.verboseLogging) Debug.Log("Adgem--- videoPlayer preparing.............");
            if(AdGem.verboseLogging) Debug.Log("Adgem--- videoPlayer.texture is null? " + (videoPlayer.texture == null) );
        }
        yield return new WaitForSeconds(0.05f);

        if (videoPlayer.texture != null)
        {

            vidoePlayerRawImage.color = Color.white;
            vidoePlayerRawImage.texture = new RenderTexture(videoPlayer.texture.width, videoPlayer.texture.height, 0);
            videoPlayer.targetTexture = (RenderTexture)vidoePlayerRawImage.texture;

            xButtonWasClicked = false;

            goAdGemCanvas.SetActive(true);
            goAdGemVideoCamera.SetActive(true);

            //supressCurrentScene();
            audioSource.volume = 0.9f;

            videoHolder.SetActive(true);
            videoPlayer.Play();
            audioSource.Play();
            videoHolderRect.localPosition = correctVideoHolderPosition; //move the video holder back on screen
            AdGemPlugin.videoIsPlaying = true;
            AdGemPlugin.customEndCardShowing = false;
            AdGemPlugin.fullScreenEndCardShowing = false;
            AdGemPlugin.carouselEndCardShowing = false;

            if (AdGem.videoAdStarted != null) { AdGem.videoAdStarted(); }

            currentVideoLength = ((float)videoPlayer.frameCount) / videoPlayer.frameRate; ;
            countdownRingImage.fillAmount = 0f;
            countdownTimeLeft = currentVideoLength;

            StartCoroutine(MonitorScreenSize());

            StartCoroutine(sendVideoStartedConfirmation());

            //Allow next video to begin downloading
            if (AdGemPlugin.rewardMode) 
            {
                AdGemPlugin.rewardVideoReady = false; 
                //Debug.Log("Reward video ready set to false"); 
            }
            else 
            { 
                AdGemPlugin.interstitialVideoReady = false; 
                //Debug.Log("Standard video ready set to false"); 
            }

        }

    }

    private IEnumerator waitThenTurnOnXButton(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        xButtonObject.SetActive(true);
    }

    private void supressCurrentScene()
    {
        audioSources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
        audioSourceVolumes = new float[audioSources.Length];
        for (int i = 0; i < audioSources.Length; i++)
        {
            audioSourceVolumes[i] = audioSources[i].volume;
            audioSources[i].volume = 0;
        }

        //Disable all objects that do not have an audio listener
        gameObjectsInScene = SceneManager.GetActiveScene().GetRootGameObjects();
        sceneObjectActiveStatus = new bool[gameObjectsInScene.Length];
        bool lookingForObjectWithAudioListener = true;
        for (int i = 0; i < gameObjectsInScene.Length; i++)
        {
            sceneObjectActiveStatus[i] = gameObjectsInScene[i].activeInHierarchy;

            bool thisObjectHasAnAudioListener = false;
            if (lookingForObjectWithAudioListener)
            {
                AudioListener al = gameObjectsInScene[i].GetComponent<AudioListener>();
                if (al != null)
                {
                    thisObjectHasAnAudioListener = true;
                    lookingForObjectWithAudioListener = false;
                }
            }
            else if (!gameObjectsInScene[i].name.Contains("AdGem") && !thisObjectHasAnAudioListener)
            {
                gameObjectsInScene[i].SetActive(false);
            }
        }

    }
    private void restoreCurrentScene()
    {
        //if (gameObjectsInScene != null)
        //{
        //    for (int i = 0; i < gameObjectsInScene.Length; i++)
        //    {
        //        if (gameObjectsInScene[i] != null && !gameObjectsInScene[i].name.Contains("AdGem"))
        //        {
        //            gameObjectsInScene[i].SetActive(sceneObjectActiveStatus[i]);
        //        }
        //    }
        //}

        //if (audioSources != null)
        //{
        //    for (int i = 0; i < audioSources.Length; i++)
        //    {
        //        if (audioSources[i] != null)
        //        {
        //            audioSources[i].volume = audioSourceVolumes[i];
        //        }
        //    }
        //}
        carouselRect.gameObject.SetActive(false);
        goAdGemCanvas.SetActive(false);
        goAdGemVideoCamera.SetActive(false);
    }

    private IEnumerator MonitorScreenSize()
    {
        while (AdGemPlugin.videoIsPlaying || AdGemPlugin.customEndCardShowing || AdGemPlugin.fullScreenEndCardShowing || AdGemPlugin.carouselEndCardShowing)
        {
            try
            {
                AdGemPlugin.positionXButton(blackBgRect, xButtonRect);

                if (AdGemPlugin.customEndCardShowing || AdGemPlugin.fullScreenEndCardShowing || AdGemPlugin.carouselEndCardShowing)
                {
                    monitorEndCardConfiguration();
                }
                else
                {
                    AdGemPlugin.positionVideo(countdownRingBgRect, countdownRingrect, countdownTextRect, videoPlayer, videoHolderRect);
                }
            }
            catch (Exception ex)
            {
                if (AdGem.verboseLogging) Debug.Log("Adgem--- Error monitoring screen size: " + ex.ToString());
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void monitorEndCardConfiguration()
    {
        //Debug.Log("currentVideoEndType----- " + currentVideoEndType);

        switch (currentVideoEndType)
        {
            case AdGemPlugin.END_TYPE_REDIRECT: break;
            case AdGemPlugin.END_TYPE_FULLSCREEN: AdGemPlugin.monitorFullScreenEndCardConfiguration(endCardImageRect, endCardImageHolderRect); break;
            case AdGemPlugin.END_TYPE_NO_VIDEO: AdGemPlugin.monitorFullScreenEndCardConfiguration(endCardImageRect, endCardImageHolderRect); break; //same as full screen
            case AdGemPlugin.END_TYPE_CUSTOM: AdGemPlugin.monitorCustomEndCardConfiguration(endCardImageHolderRect, endCardImageRect, customEndCardPanelRect, customEndCardInfoBgRect, iconMaskRect, titleTextRect, descriptionTextRect, starsRect, reviewCountRect, downloadButtonRect, adgemLogoRect); break;
            case AdGemPlugin.END_TYPE_CAROUSEL: AdGemPlugin.monitorCarouselEndCardConfiguration(carouselRect, carouselContainerRect, customEndCardPanelRect, customEndCardInfoBgRect, iconMaskRect, titleTextRect, descriptionTextRect, starsRect, reviewCountRect, downloadButtonRect, adgemLogoRect); break;
            default: break;
        }
    }


    public void xButtonClicked()
    {
        xButtonWasClicked = true;

        if (AdGemPlugin.videoIsPlaying) //If video is still playing, go to the endcard
        {
            VideoEndReached(videoPlayer);

            if (AdGemPlugin.rewardMode)
            {
                AdGemManager.staticRef.fireEventEndpoint("videoskip", "rewarded-video", AdGemManager.rewardedVideoCacheId);
                if (AdGem.rewardVideoCanceled != null) { AdGem.rewardVideoCanceled(); }
            }
            else
            {
                AdGemManager.staticRef.fireEventEndpoint("videoskip", "nonrewarded-video", AdGemManager.interstitialVideoCacheId);
                if (AdGem.interstitialVideoCanceled != null) { AdGem.interstitialVideoCanceled(); }
            }
        }
        else //Otherwise, if we are on the endcard, close the ad
        {
            if(AdGemPlugin.carouselEndCardShowing)
            {
                for(int i = 0; i < carouselContainerRect.childCount; i++)
                {
                    Destroy(carouselContainerRect.GetChild(i).gameObject);
                }
                carouselContainerRect.transform.DetachChildren();
            }
            if (AdGemPlugin.rewardMode)
            {
                AdGemManager.staticRef.fireEventEndpoint("videocomplete", "rewarded-video", AdGemManager.rewardedVideoCacheId); 
            }
            else
            {
                AdGemManager.staticRef.fireEventEndpoint("videocomplete", "nonrewarded-video", AdGemManager.interstitialVideoCacheId);
            }

            AdGemPlugin.videoIsPlaying = false;
            AdGemPlugin.customEndCardShowing = false;
            AdGemPlugin.fullScreenEndCardShowing = false;
            AdGemPlugin.carouselEndCardShowing = false;
            videoPlayer.Stop();

            restoreCurrentScene();

            videoAdProcessFullyCompleted();
        }

    }

    void VideoPlayerErrorReceived(VideoPlayer source, string message)
    {
        Debug.Log("Adgem - VIDEO PLAYER ERROR: " + message);
    }

    void VideoEndReached(VideoPlayer videoPlayerPar)
    {

        videoPlayerPar.Stop();
        vidoePlayerRawImage.color = Color.black;
        countdownRing.SetActive(false);
        countdownRingBg.SetActive(false);
        countdownTextObject.SetActive(false);

        if (!xButtonWasClicked) { sendVideoCompleteConfirmation(); }

        if (AdGemPlugin.rewardMode)
        {
            switch (currentVideoEndType)
            {
                case AdGemPlugin.END_TYPE_REDIRECT:
                    immediateRedirect(true);
                    break;
                case AdGemPlugin.END_TYPE_FULLSCREEN:
                    setupFullScreenEndCard(true);
                    break;
                case AdGemPlugin.END_TYPE_CUSTOM:
                    setupCustomEndCard(true);
                    break;
                case AdGemPlugin.END_TYPE_CAROUSEL:
                    setupCarouselEndCard(true);
                    break;
                default:
                    immediateRedirect(true);
                    break;
            }
        }
        else
        {
            switch (currentVideoEndType)
            {
                case AdGemPlugin.END_TYPE_REDIRECT:
                    immediateRedirect(false);
                    break;
                case AdGemPlugin.END_TYPE_FULLSCREEN:
                    setupFullScreenEndCard(false);
                    break;
                case AdGemPlugin.END_TYPE_NO_VIDEO:
                    setupFullScreenEndCard(false);
                    break;
                case AdGemPlugin.END_TYPE_CUSTOM:
                    setupCustomEndCard(false);
                    break;
                case AdGemPlugin.END_TYPE_CAROUSEL:
                    setupCarouselEndCard(false);
                    break;
                default:
                    immediateRedirect(false);
                    break;
            }
        }

        AdGemPlugin.videoIsPlaying = false;

    }

    //This should be called when one of the following things happens: User x's out of the end card, user x's out of exitable video with no end card, uers is auto-redirected after a video finishes. 
    private void videoAdProcessFullyCompleted()
    {
        if (AdGemPlugin.rewardMode)
        {
            //AdGemPlugin.rewardVideoReady = false; //We will try downloading the next one while the current one is playing.
            if (AdGem.rewardVideoFinishedPlaying != null) { AdGem.rewardVideoFinishedPlaying(); }
        }
        else
        {
            //AdGemPlugin.standardVideoReady = false; //We will try downloading the next one while the current one is playing.
            if (AdGem.interstitialVideoFinishedPlaying != null) { AdGem.interstitialVideoFinishedPlaying(); }
        }
    }



    private void immediateRedirect(bool rewarded)
    {
        if (rewarded)
        {
            userClicked(AdGemPlugin.rewardVideoTrackingUrl, AdGemPlugin.rewardVideoAppStoreAppId);
        }
        else
        {
            userClicked(AdGemPlugin.standardVideoTrackingUrl, AdGemPlugin.standardVideoAppStoreAppId);
        }

        AdGemPlugin.videoIsPlaying = false;
        xButtonClicked();
    }
    private void setupFullScreenEndCard(bool rewarded)
    {
        AdGemPlugin.setupFullScreenEndCard(rewarded, customEndCardPanel, xButtonObject, videoHolder, endCardImage, endCardImageObject);
    }
    private void setupCustomEndCard(bool rewarded)
    {
        AdGemPlugin.setupCustomEndCard(rewarded, customEndCardPanel, xButtonObject, videoHolder, endCardImage, iconImage, endCardImageObject);
    }
    private void setupCarouselEndCard(bool rewarded)
    {
        AdGemPlugin.setupCarouselEndCard(rewarded, customEndCardPanel, xButtonObject, videoHolder, iconImage, carouselRect.gameObject, carouselContainerRect.gameObject);
    }

    private IEnumerator sendVideoStartedConfirmation()
    {
        UnityEngine.Networking.UnityWebRequest webRequest = UnityEngine.Networking.UnityWebRequest.Get(AdGemPlugin.getVideoStartedConfirmationUrl());
        yield return webRequest.SendWebRequest();
    }
    private void sendVideoCompleteConfirmation()
    {
        if (AdGemPlugin.rewardMode) { AdGemManager.staticRef.fireEventEndpoint("videocomplete", "rewarded-video", AdGemManager.rewardedVideoCacheId); }
        else { AdGemManager.staticRef.fireEventEndpoint("videocomplete", "nonrewarded-video", AdGemManager.interstitialVideoCacheId); }
    }

    public void userClicked(string trackingUrl, string appStoreAppId)
    {

        if (appStoreAppId != null && appStoreAppId.Length > 4 && !AdGemPlugin.alwaysOpenAppStoreInBrowser) //We have an app store link
        {
            if (AdGem.verboseLogging) Debug.Log("Adgem >>>>>>>>> userClicked-- appStoreAppId: " + appStoreAppId + ", trackingUrl: " + trackingUrl);

#if UNITY_IOS
            int appIDIOS;

            //Debug.Log(appStoreAppId);

            if(int.TryParse(appStoreAppId, out appIDIOS))
            {   _OpenAppInStore(appIDIOS);
            }
#elif UNITY_ANDROID
            Application.OpenURL("http://play.google.com/store/apps/details?id=" + appStoreAppId);  //  "market://details?id=" + appStoreAppId);
#endif
            StartCoroutine(goToTrackingUrlSilently(trackingUrl));
        }
        else
        {
            if (AdGem.verboseLogging) Debug.Log("Adgem >>>>>>>>> userClicked-- trackingUrl only: " + trackingUrl);

            Application.OpenURL(trackingUrl);
        }

    }
    private IEnumerator goToTrackingUrlSilently(string trackingUrl)
    {
        UnityEngine.Networking.UnityWebRequest initialRequest = UnityEngine.Networking.UnityWebRequest.Get(trackingUrl);
        yield return initialRequest.SendWebRequest();
    }






}
