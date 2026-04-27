using System;
using System.Collections.Generic;
using System.Text;
using TheBestLoggerSample.CrashReporting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using UnityEngine.UI;

public sealed class GameLoggerSampleSceneUi : MonoBehaviour
{
    [SerializeField]
    private GameLoggerSample _sample;

    [SerializeField]
    private Text _summaryText;

    [SerializeField]
    private ScrollButton _buttonTemplate;

    [SerializeField]
    private Transform _loggerActionsRoot;

    [SerializeField]
    private Transform _managedActionsRoot;

    [SerializeField]
    private Transform _nativeActionsRoot;

    [SerializeField]
    private ScrollRect _actionScrollRect;

    [SerializeField]
    private int _defaultPoolCapacity = 24;

    [SerializeField]
    private int _maxPoolSize = 48;

    private readonly List<ScrollButton> _activeButtons = new();
    private readonly StringBuilder _summaryBuilder = new(512);
    private ObjectPool<ScrollButton> _buttonPool;

    private readonly struct ActionDefinition
    {
        public ActionDefinition(string title, string subtitle, UnityAction callback, ButtonTone tone)
        {
            Title = title;
            Subtitle = subtitle;
            Callback = callback;
            Tone = tone;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public UnityAction Callback { get; }
        public ButtonTone Tone { get; }
    }

    private enum ButtonTone
    {
        Primary,
        Managed,
        Native
    }

    private void Awake()
    {
        if (_sample == null)
        {
            _sample = FindObjectOfType<GameLoggerSample>();
        }

        if (_actionScrollRect == null)
        {
            _actionScrollRect = GetComponentInChildren<ScrollRect>(includeInactive: true);
        }

        if (_sample == null || _summaryText == null || _buttonTemplate == null)
        {
            enabled = false;
            return;
        }

        _buttonTemplate.gameObject.SetActive(false);
        _buttonPool = new ObjectPool<ScrollButton>(CreateButton,
                                                   OnGetButton,
                                                   OnReleaseButton,
                                                   OnDestroyButton,
                                                   collectionCheck: true,
                                                   defaultCapacity: _defaultPoolCapacity,
                                                   maxSize: _maxPoolSize);
        RebuildActionLists();
        RefreshSummary();
    }

    private void Start()
    {
        RefreshLayout();
    }

    private void OnEnable()
    {
        if (_sample != null)
        {
            _sample.StateChanged += RefreshSummary;
        }
    }

    private void OnDisable()
    {
        if (_sample != null)
        {
            _sample.StateChanged -= RefreshSummary;
        }
    }

    private void OnDestroy()
    {
        ReleaseActiveButtons();
        _buttonPool?.Clear();
    }

    private void RebuildActionLists()
    {
        ReleaseActiveButtons();
        PopulateSection(_loggerActionsRoot, GetLoggerActions());
        PopulateSection(_managedActionsRoot, GetManagedActions());
        PopulateSection(_nativeActionsRoot, GetNativeActions());
        RefreshLayout();
    }

    private void PopulateSection(Transform root, IReadOnlyList<ActionDefinition> actions)
    {
        if (root == null)
        {
            return;
        }

        foreach (ActionDefinition action in actions)
        {
            ScrollButton button = _buttonPool.Get();
            button.transform.SetParent(root, false);
            button.Bind(BuildButtonMarkup(action.Title, action.Subtitle),
                        action.Callback,
                        CreateColorBlock(action.Tone),
                        GetTextColor(action.Tone));
            _activeButtons.Add(button);
        }
    }

    private IReadOnlyList<ActionDefinition> GetLoggerActions()
    {
        return new[]
        {
            new ActionDefinition("Scripted QA bootstrap", "Switch to the scripted QA preset and reinitialize targets.", _sample.UseScriptedQaBootstrap, ButtonTone.Primary),
            new ActionDefinition("Toggle debug mode", "Flip the sample debug gate for the default debug id.", _sample.ToggleSampleDebugMode, ButtonTone.Primary),
            new ActionDefinition("Apply QA runtime patch", "Lower all active targets to Debug level.", _sample.ApplyQaRuntimePatch, ButtonTone.Primary),
            new ActionDefinition("Apply production patch", "Raise all active targets to Warning level.", _sample.ApplyProductionRuntimePatch, ButtonTone.Primary),
            new ActionDefinition("Log target summary", "Dump current target configuration to the Unity Console.", _sample.LogCurrentConfigurationSummary, ButtonTone.Primary),
            new ActionDefinition("Retrieve previous issues", "Request StabilityHub replay for the last session.", _sample.RetrievePreviousSessionIssues, ButtonTone.Primary),
            new ActionDefinition("Toggle runtime console", "Enable or disable the runtime console target and reinitialize.", _sample.ToggleRuntimeConsoleTarget, ButtonTone.Primary),
            new ActionDefinition("Toggle previous issue scan", "Enable or disable automatic session issue retrieval on startup.", _sample.TogglePreviousSessionIssueRetrievalOnStart, ButtonTone.Primary),
            new ActionDefinition("Toggle OpenSearch mock", "Enable or disable the mock transport target and reinitialize.", _sample.ToggleOpenSearchMockTarget, ButtonTone.Primary),
            new ActionDefinition("Emit OpenSearch mock log", "Send one structured payload through the mock OpenSearch target.", _sample.EmitOpenSearchMockLog, ButtonTone.Primary),
            new ActionDefinition("Apply OpenSearch QA patch", "Use a QA endpoint and Debug level for the mock target.", _sample.ApplyOpenSearchMockQaPatch, ButtonTone.Primary),
            new ActionDefinition("Apply OpenSearch production patch", "Use a production endpoint and Warning level for the mock target.", _sample.ApplyOpenSearchMockProductionPatch, ButtonTone.Primary),
            new ActionDefinition("Clear OpenSearch capture", "Reset captured payload history for the mock transport.", _sample.ClearOpenSearchMockCapture, ButtonTone.Primary),
            new ActionDefinition("Emit burst logs", "Generate a quick burst of sample logs for throughput checks.", _sample.EmitBurstLogs, ButtonTone.Primary),
            new ActionDefinition("Emit background log", "Queue a background log plus a handled background exception.", _sample.EmitBackgroundLog, ButtonTone.Primary),
            new ActionDefinition("Emit Unity console error", "Write to Console.Error and UnityEngine.Debug.LogError.", _sample.EmitUnityConsoleError, ButtonTone.Primary)
        };
    }

    private IReadOnlyList<ActionDefinition> GetManagedActions()
    {
        return new[]
        {
            new ActionDefinition("Handled exception", "Throw and capture a managed exception through the logger.", _sample.EmitHandledExceptionLog, ButtonTone.Managed),
            new ActionDefinition("Unobserved task exception", "Create a faulted task that is never observed.", _sample.EmitUnobservedTaskException, ButtonTone.Managed),
            new ActionDefinition("Main-thread unobserved task", "Schedule an unobserved task on the Unity synchronization context.", _sample.EmitMainThreadUnobservedTaskException, ButtonTone.Managed),
            new ActionDefinition("Observed task exception", "Await a faulted task and surface it as an observed exception.", _sample.EmitObservedTaskException, ButtonTone.Managed),
            new ActionDefinition("Aggregate exception", "Trigger the aggregate exception sample flow.", _sample.EmitAggregateHandledException, ButtonTone.Managed),
            new ActionDefinition("Inner exception", "Trigger the nested inner exception sample flow.", _sample.EmitInnerHandledException, ButtonTone.Managed),
            new ActionDefinition("Handled async void", "Exercise the handled async-void exception sample.", _sample.EmitHandledAsyncVoidException, ButtonTone.Managed),
            new ActionDefinition("Handled void exception", "Exercise the handled sync void exception sample.", _sample.EmitHandledVoidException, ButtonTone.Managed),
            new ActionDefinition("Unhandled async void", "Trigger the unhandled async-void crash path.", _sample.EmitUnhandledAsyncVoidException, ButtonTone.Managed)
        };
    }

    private IReadOnlyList<ActionDefinition> GetNativeActions()
    {
        string[] nativeMethods = _sample.GetNativeCrashTestNames();
        var actions = new ActionDefinition[nativeMethods.Length];
        for (var i = 0; i < nativeMethods.Length; i++)
        {
            string methodName = nativeMethods[i];
            actions[i] = new ActionDefinition(NicifyNativeName(methodName),
                                              "Native iOS crash trigger. Expected to terminate the player process.",
                                              () => _sample.InvokeNativeCrashTest(methodName),
                                              ButtonTone.Native);
        }

        return actions;
    }

    private void RefreshSummary()
    {
        _summaryBuilder.Clear();
        _summaryBuilder.Append("<size=21><b>TheBestLogger Sample Studio</b></size>\n");
        _summaryBuilder.Append("<size=12><color=#6A7787>Scene-authored logger validation workspace</color></size>\n\n");
        _summaryBuilder.Append(_sample.GetLiveOverviewSummary());
        _summaryBuilder.Append("\n\n<size=12><color=#6A7787><b>Latest action</b></color></size>\n");
        _summaryBuilder.Append(_sample.LastActionStatus);
        _summaryText.supportRichText = true;
        _summaryText.text = _summaryBuilder.ToString();
    }

    private void RefreshLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (transform is RectTransform rootRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        if (_actionScrollRect == null)
        {
            return;
        }

        if (_actionScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_actionScrollRect.content);
        }

        if (_actionScrollRect.viewport != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_actionScrollRect.viewport);
        }

        _actionScrollRect.StopMovement();
        _actionScrollRect.verticalNormalizedPosition = 1f;
        Canvas.ForceUpdateCanvases();
    }

    private ScrollButton CreateButton()
    {
        ScrollButton clone = Instantiate(_buttonTemplate, _buttonTemplate.transform.parent);
        clone.gameObject.name = $"{_buttonTemplate.gameObject.name}Instance";
        clone.gameObject.SetActive(false);
        return clone;
    }

    private static void OnGetButton(ScrollButton button)
    {
        button.gameObject.SetActive(true);
    }

    private void OnReleaseButton(ScrollButton button)
    {
        button.ResetState(_buttonTemplate.transform.parent);
    }

    private static void OnDestroyButton(ScrollButton button)
    {
        if (button != null)
        {
            Destroy(button.gameObject);
        }
    }

    private void ReleaseActiveButtons()
    {
        if (_buttonPool == null)
        {
            return;
        }

        for (var i = 0; i < _activeButtons.Count; i++)
        {
            _buttonPool.Release(_activeButtons[i]);
        }

        _activeButtons.Clear();
    }

    private static string BuildButtonMarkup(string title, string subtitle)
    {
        return $"<size=14><b>{title}</b></size>\n<size=11>{subtitle}</size>";
    }

    private static string NicifyNativeName(string methodName)
    {
        string value = methodName.StartsWith("Trigger", StringComparison.Ordinal)
                           ? methodName.Substring("Trigger".Length)
                           : methodName;

        if (string.IsNullOrEmpty(value))
        {
            return "Unnamed Trigger";
        }

        var builder = new StringBuilder(value.Length + 8);
        builder.Append(value[0]);
        for (var i = 1; i < value.Length; i++)
        {
            char current = value[i];
            char previous = value[i - 1];
            if (char.IsUpper(current) && !char.IsUpper(previous))
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static Color GetTextColor(ButtonTone tone)
    {
        return tone switch
        {
            ButtonTone.Managed => new Color(0.38f, 0.2f, 0.24f),
            ButtonTone.Native => new Color(0.42f, 0.29f, 0.12f),
            _ => new Color(0.12f, 0.19f, 0.29f)
        };
    }

    private static ColorBlock CreateColorBlock(ButtonTone tone)
    {
        Color normal = tone switch
        {
            ButtonTone.Managed => new Color(0.99f, 0.94f, 0.95f),
            ButtonTone.Native => new Color(1f, 0.96f, 0.88f),
            _ => new Color(0.93f, 0.96f, 1f)
        };

        Color highlighted = tone switch
        {
            ButtonTone.Managed => new Color(0.97f, 0.89f, 0.91f),
            ButtonTone.Native => new Color(0.99f, 0.9f, 0.74f),
            _ => new Color(0.86f, 0.93f, 1f)
        };

        Color pressed = tone switch
        {
            ButtonTone.Managed => new Color(0.92f, 0.81f, 0.84f),
            ButtonTone.Native => new Color(0.95f, 0.84f, 0.62f),
            _ => new Color(0.78f, 0.88f, 0.99f)
        };

        return new ColorBlock
        {
            normalColor = normal,
            highlightedColor = highlighted,
            pressedColor = pressed,
            selectedColor = highlighted,
            disabledColor = new Color(0.9f, 0.9f, 0.9f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
    }
}
