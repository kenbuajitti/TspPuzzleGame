using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

// Opt-in analytics: never submit gameplay events unless consent has been granted.
// Editor/development builds log each step to make integration testing observable.
public sealed class RouteIQAnalytics : MonoBehaviour
{
    const string Preference = "RouteIQ.AnalyticsChoice"; // -1 = undecided, 0 = no, 1 = yes
    static RouteIQAnalytics instance;
    readonly Queue<CustomEvent> pending = new Queue<CustomEvent>();
    bool initialized;
    bool initializing;
    bool menuVisited;
    bool loadRecorded;

    public static int Choice => PlayerPrefs.GetInt(Preference, -1);
    public static bool Allowed => Choice == 1;

    static void Log(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[RouteIQ Analytics] " + message);
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap() { GetInstance(); }

    static RouteIQAnalytics GetInstance()
    {
        if (instance != null) return instance;
        var obj = new GameObject("RouteIQ Analytics");
        DontDestroyOnLoad(obj);
        instance = obj.AddComponent<RouteIQAnalytics>();
        return instance;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        Log("Saved choice = " + (Choice == 1 ? "ON" : Choice == 0 ? "OFF" : "not yet answered") + ".");
        ApplyConsent(Allowed);
        if (Allowed) Initialize();
    }

    // Called each time the main menu appears; report one launch per WebGL load.
    public static void MenuShown()
    {
        var service = GetInstance();
        service.menuVisited = true;
        Log("Main menu displayed.");
        service.RecordLaunchIfNeeded();
    }

    public static void SetPlayerChoice(bool allowed)
    {
        var service = GetInstance();
        PlayerPrefs.SetInt(Preference, allowed ? 1 : 0);
        PlayerPrefs.Save();
        Log("Privacy choice changed to " + (allowed ? "ON" : "OFF") + " and saved.");
        if (allowed)
        {
            if (service.initialized) service.ApplyConsent(true);
            else service.Initialize();
            service.RecordLaunchIfNeeded();
        }
        else
        {
            service.pending.Clear();
            service.ApplyConsent(false);
        }
    }

    async void Initialize()
    {
        if (initializing || initialized || !Allowed) return;
        initializing = true;
        Log("Initializing Unity Gaming Services...");
        try
        {
            await UnityServices.InitializeAsync();
            initialized = true;
            Log("Unity Gaming Services initialized.");
            ApplyConsent(Allowed);
            if (Allowed)
            {
                RecordLaunchIfNeeded();
                int buffered = pending.Count;
                while (pending.Count > 0)
                    AnalyticsService.Instance.RecordEvent(pending.Dequeue());
                if (buffered > 0) Log("Recorded " + buffered + " buffered event(s) in the Analytics SDK.");
                AnalyticsService.Instance.Flush();
                Log("Requested analytics upload. Check Analytics Debug Panel / Event Browser for delivery.");
            }
            else pending.Clear();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[RouteIQ Analytics] Initialization/recording failed (gameplay unaffected): " + ex);
            pending.Clear();
        }
        finally { initializing = false; }
    }

    void ApplyConsent(bool allowed)
    {
        // Unity 6.2+ / Analytics SDK 6.1+: Developer Data consent API.
        try { EndUserConsent.SetConsentState(new ConsentState
        {
            AnalyticsIntent = allowed ? ConsentStatus.Granted : ConsentStatus.Denied,
            AdsIntent = ConsentStatus.Denied
        }); } catch (Exception ex) { Debug.LogWarning("[RouteIQ Analytics] Consent update failed: " + ex.Message); }
        Log("Analytics consent applied: " + (allowed ? "Granted" : "Denied") + ".");
    }

    void RecordLaunchIfNeeded()
    {
        if (!menuVisited || loadRecorded || !Allowed) return;
        loadRecorded = true;
        Send("routeiq_loaded", new CustomEvent("routeiq_loaded"), true);
    }

    static void Send(string name, CustomEvent evt, bool flush = false)
    {
        var service = GetInstance();
        if (!Allowed) return;
        if (!service.initialized)
        {
            service.pending.Enqueue(evt);
            Log("Buffered event " + name + " while services initialize.");
            service.Initialize();
            return;
        }
        try
        {
            AnalyticsService.Instance.RecordEvent(evt);
            Log("Recorded event " + name + " in SDK (not yet proof of server receipt).");
            if (flush)
            {
                AnalyticsService.Instance.Flush();
                Log("Requested upload of " + name + ".");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[RouteIQ Analytics] Event " + name + " not recorded: " + ex);
        }
    }

    // All events for an attempt share these fields. Do not call before opt-in.
    public static void Track(string name, string puzzleId, string attemptId,
        int nodes, int attempt, Dictionary<string, object> values = null)
    {
        if (!Allowed) return;
        var evt = new CustomEvent(name)
        {
            { "puzzle_id", puzzleId }, { "attempt_id", attemptId },
            { "node_count", nodes }, { "attempt", attempt }
        };
        try
        {
            if (values != null)
                foreach (var pair in values) evt.Add(pair.Key, pair.Value);
            Send(name, evt, true);
        }
        catch (Exception ex) { Debug.LogWarning("[RouteIQ Analytics] Event construction failed: " + ex.Message); }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && initialized && Allowed) AnalyticsService.Instance.Flush();
    }
}
