using UnityEngine;
using UnityEngine.UI;
using Byn.Awrtc;
using Byn.Awrtc.Unity;
using System.Collections.Generic;
using Byn.Unity.Examples;
using System.Linq;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class WebRTCViewer : MonoBehaviour
{
    #region Varibales
    [Header("Glitch Server URL To Fetch the Streaming Channels")]
    [SerializeField] string serverUrl = "https://metal-hushed-loganberry.glitch.me/active-streams";
    [Space(3)]
    [Header("Reference For Raw Image Where We Show the Stream")]
    [SerializeField] RawImage display; // UI to show stream
    [Space(3)]
    [Header("Reference UI")]
    public GameObject EmptyText; // Button to join selected stream
    [Space(3)]
    [Header("Reference Channel List Prefab/Container")]
    public Transform Content;  // 🎯 Parent for dynamic buttons
    public GameObject ChannelPrefab;   // 🎯 Prefab for each channel button
    [Space(3)]
    [Header("Reference Panels")]
    public GameObject StreamingCanvasUI;   // Channel List UI
    public GameObject ChannelList;   // Channel List UI
    public GameObject mediaPlayerUI;   // 🎥 Media player UI for playback
    public GameObject loading;   // Loading..

    public Renderer sphereRenderer;  // Assign the Sphere Renderer in Inspector
    private RenderTexture renderTexture;
    //[SerializeField] RenderTexture renderTexture2;
    [SerializeField] private Material sbsMaterial;


    [SerializeField] private GameObject VR;  // Assign in Inspector
    [SerializeField] private RawImage leftEyeImage;  // Assign in Inspector
    [SerializeField] private RawImage rightEyeImage; // Assign in Inspector

    private MediaConfig _mediaConfig;
    private ICall _call;
    private Texture2D _videoTexture;
    private NetworkConfig mNetConfig = new NetworkConfig();
    private List<string> availableStreams = new List<string>();
    private List<GameObject> _ChannelList = new List<GameObject>();
    #endregion

    #region Mono Behaviour Methods
    void Start()
    {
        UnityCallFactory.RequestLogLevelStatic(UnityCallFactory.LogLevel.Info);
        UnityCallFactory.EnsureInit(OnWebRTCInitialized, OnWebRTCError);
        StartCoroutine(FetchActiveStreams());
    }
    private void Update()
    {
        if (_call != null)
        {
            _call.Update();     //update the call
        }
    }
    void OnDestroy()
    {
        StopWatchingStream();
    }
    #endregion
    #region Custom Properties
    public NetworkConfig NetConfig { get { return mNetConfig; } set { mNetConfig = value; } }
    #endregion
    #region Custom Methods
    private void OnWebRTCInitialized()
    {
        Debug.Log("WebRTC Initialized!");
        _mediaConfig = new MediaConfig();
        _mediaConfig.Audio = false; // Disable audio
        _mediaConfig.Video = false;  // Enable video
        NetConfig.KeepSignalingAlive = true;
        NetConfig.MaxIceRestart = 5;
        NetConfig.IceServers.Add(ExampleGlobals.DefaultIceServer);
        NetConfig.SignalingUrl = ExampleGlobals.SignalingConference;
        NetConfig.IsConference = true;
        _call = UnityCallFactory.Instance.Create(NetConfig);
        _call.CallEvent += OnCallEvent;
        //_call.Configure(_mediaConfig);
    }
    private void OnWebRTCError(string error)
    {
        Debug.LogError($"WebRTC Error: {error}");
    }
    private void OnCallEvent(object sender, CallEventArgs args)
    {
        Debug.Log($"OnCallEvent: {args.Type}");

        if (args.Type == CallEventType.CallAccepted)
        {
            //AttachAudio();
            Debug.Log("Connected to stream!");
            // Log the audio status
            UnityEngine.Debug.Log($"🎤 Audio Enabled: {_mediaConfig.Audio}");
            UnityEngine.Debug.Log($"🎤 Audio Enabled: {_call.IsMute()}");
        }
        else if (args.Type == CallEventType.ConnectionFailed)
        {
            Debug.LogError("Failed to connect.");
        }
        else if (args.Type == CallEventType.FrameUpdate)
        {
            OnFrameUpdate(args as FrameUpdateEventArgs);
        }
        else if (args.Type == CallEventType.AudioFrames)
        {
            Debug.Log("Media stream updated (possibly audio added).");
            OnAudioFrameReceived(args as AudioFramesEventArgs);
        }
        else if (args.Type == CallEventType.CallEnded)
        {
            OnClickBack();
        }
    }
    private void OnAudioFrameReceived(AudioFramesEventArgs audioArgs)
    {
        if (audioArgs == null || audioArgs.Frames == null)
        {
            Debug.LogError("🚨 Invalid audio frame received.");
            return;
        }

        AudioFrames frame = audioArgs.Frames;
        short[] audioSamples = frame.Samples; // ✅ Get raw PCM samples
        int sampleRate = frame.SampleRate;
        int channels = frame.NumberOfChannels;

        Debug.Log($"🔊 Audio Frame Received - Sample Rate: {sampleRate}, Channels: {channels}, Sample Count: {audioSamples.Length}");

        // Play audio using Unity AudioSource
        PlayAudio(audioSamples, sampleRate, channels);
    }
    private void PlayAudio(short[] pcmData, int sampleRate, int channels)
    {
        if (pcmData == null || pcmData.Length == 0)
        {
            Debug.LogError("🚨 No audio data to play.");
            return;
        }

        Debug.Log("🎧 Converting PCM data to AudioClip...");

        // Convert short PCM data to float (Unity requires float PCM)
        float[] floatData = new float[pcmData.Length];
        for (int i = 0; i < pcmData.Length; i++)
        {
            floatData[i] = pcmData[i] / 32768.0f; // Convert from 16-bit PCM to float (-1 to 1 range)
        }

        // Create AudioClip
        AudioClip audioClip = AudioClip.Create("RemoteAudio", pcmData.Length, channels, sampleRate, false);
        audioClip.SetData(floatData, 0);

        // Play the AudioClip using Unity AudioSource
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("🚨 No AudioSource found on this GameObject!");
            return;
        }

        audioSource.clip = audioClip;
        audioSource.loop = false; // Play once
        audioSource.Play();

        Debug.Log("🎵 Playing remote audio...");
    }
    private void OnFrameUpdate(FrameUpdateEventArgs frameArgs)
    {
        if (frameArgs == null || frameArgs.Frame == null)
        {
            Debug.LogWarning("FrameUpdate received, but frame is NULL.");
            return;
        }

        IFrame frame = frameArgs.Frame;
        int width = frame.Width;
        int height = frame.Height;
        byte[] frameData = frame.Buffer;

        if (frameData == null || frameData.Length == 0)
        {
            Debug.LogWarning("Received frame has no data.");
            return;
        }

        // Ensure the texture exists and has correct size
        if (_videoTexture == null || _videoTexture.width != width || _videoTexture.height != height)
        {
            _videoTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Debug.Log($"Created new Texture2D: {width}x{height}");
        }

        _videoTexture.LoadRawTextureData(frameData);
        _videoTexture.Apply();

        // ✅ Create RenderTexture for SBS processing
        if (renderTexture == null || renderTexture.width != width || renderTexture.height != height)
        {
            renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);

        }

        // ✅ Blit the frame into the RenderTexture (for further processing)
        Graphics.Blit(_videoTexture, renderTexture);

        // ✅ Assign to sphere material for SBS rendering
        //sphereRenderer.material.mainTexture = renderTexture;
        //renderTexture2 = renderTexture;

        //// Apply the RenderTexture to the Sphere's Material
        //sphereRenderer.material.mainTexture = _videoTexture;

        // 🎯 Render left and right half
        //if (leftEyeImage != null && rightEyeImage != null)
        //{
        //    // Set main texture
        //    leftEyeImage.texture = _videoTexture;
        //    rightEyeImage.texture = _videoTexture;

        //    // Set UV Rect to crop
        //    leftEyeImage.uvRect = new Rect(0f, 0f, 0.5f, 1f);  // Left Half
        //    rightEyeImage.uvRect = new Rect(0.5f, 0f, 0.5f, 1f); // Right Half
        //}
        // Apply the RenderTexture to the SBS Shader Material
        if (sbsMaterial != null)
        {
            sbsMaterial.SetTexture("_MainTex", renderTexture);
        }

        if (display != null)
        {
            Debug.Log("Eye Index: " + Shader.GetGlobalFloat("_UnityStereoEyeIndex"));
            RectTransform rt = display.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(renderTexture.width, renderTexture.height);  // Adjust for stereo content


            display.texture = renderTexture;
            if (!display.enabled)
            {
                display.enabled = true;
                loading.SetActive(false);
            }
            Debug.Log("Updated RawImage with new video frame.");
        }
    }
    IEnumerator FetchActiveStreams()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(serverUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                availableStreams = JsonUtility.FromJson<StreamListWrapper>("{\"streams\":" + jsonResponse + "}")?.streams;
                ClearChannelList();
                if (availableStreams.Count > 0)
                {
                    EmptyText.SetActive(false);
                    foreach (string stream in availableStreams)
                    {
                        GameObject buttonGO = Instantiate(ChannelPrefab, Content);
                        Button button = buttonGO.GetComponent<Button>();
                        TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();

                        buttonText.text = stream; // Set button label

                        button.onClick.AddListener(() => JoinSelectedStream(stream));
                        _ChannelList.Add(buttonGO);
                    }
                }
                else
                {
                    EmptyText.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("Failed to fetch streams: " + request.error);
            }
            loading.SetActive(false);
            ChannelList.SetActive(true);
        }
    }
    private void JoinSelectedStream(string selectedStream)
    {
        if (_call == null) 
        {
            UnityCallFactory.EnsureInit(OnWebRTCInitialized, OnWebRTCError);
        }
        //return;

        if (selectedStream != "")
        {
            _call.Listen(selectedStream);
            ChannelList.SetActive(false);
            display.gameObject.SetActive(true);
            loading.SetActive(true);
            //sphereRenderer.gameObject.SetActive(true);
            //VR.SetActive(true);
            Debug.Log($"Listening to stream: {selectedStream}");

            // 🎤 Attach WebRTC audio stream to Unity AudioSource
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                //audioSource.clip = _call.GetRemoteAudioClip(); // ✅ Fetch WebRTC audio
                audioSource.loop = false;
                audioSource.Play();
                Debug.Log("🎵 Audio stream started!");
            }
            else
            {
                Debug.LogError("🚨 No AudioSource found on this GameObject!");
            }
        }
    }
    public void OnClickBack()
    {
        loading.SetActive(true);
        ChannelList.SetActive(false);
        StreamingCanvasUI.SetActive(true);
        display.gameObject.SetActive(false);
        //sphereRenderer.gameObject.SetActive(false);
        VR.SetActive(false);
        StopWatchingStream();
        StartCoroutine(FetchActiveStreams());
    }
    public void OnClickRefersh()
    {
        loading.SetActive(true);
        StartCoroutine(FetchActiveStreams());
    }
    public void OnClickQuit()
    {
        Application.Quit();
    }
    public void StopWatchingStream()
    {
        if (_call != null)
        {
            _call.Dispose();
            _call = null;
        }
    }
    void ClearChannelList()
    {
        for (int i = 0; i < _ChannelList.Count; i++)
        {
            Destroy(_ChannelList[i]);
        }
        _ChannelList = new List<GameObject>();
    }
    #endregion
    #region Serialize Class
    [System.Serializable]
    private class StreamListWrapper
    {
        public List<string> streams;
    }
    #endregion
}
