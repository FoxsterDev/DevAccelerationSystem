using System.Collections.Generic;
using UnityEngine;

namespace TheBestLogger.Examples.LogTargets
{
    public class IMGUIRuntimeDrawer : MonoBehaviour
    {
        private IMGUIRuntimeLogTargetConfiguration _configuration;

        // List to store log messages
        private IReadOnlyCollection<string> _logEntries;
        private GUIStyle _logStyle; // Custom GUIStyle for the log text

        private Vector2 _scrollPosition;
        private bool _showLogWindow = false;
        private Rect _windowRect = new(20, 20, 500, 300);

        private void Update()
        {
            if (_configuration == null)
            {
                return;
            }

            if (Input.GetKeyDown(_configuration.KeyboardKeyDownToActivateConsole))
            {
                _showLogWindow = !_showLogWindow;
            }

            if (Input.touchCount == _configuration.CountOfScreenTouchesToActivateConsole)
            {
                _showLogWindow = !_showLogWindow;
            }
        }

        private void OnGUI()
        {
            if (_showLogWindow)
            {
                if (_logStyle == null)
                {
                    // Initialize the GUIStyle with a larger font size
                    _logStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = CalculateFontSize(), // Set the desired font size here
                        wordWrap = true,
                        richText = true // Optional: Wrap text within the label
                    };
                }

                GUI.Window(0, _windowRect, DrawLogWindow, "Runtime Log");
            }
        }

        public void Initialize(IMGUIRuntimeLogTargetConfiguration configuration, IReadOnlyCollection<string> logEntries)
        {
            _configuration = configuration;
            _logEntries = logEntries;
            _windowRect = new Rect(0, 20, Screen.width, Screen.height * 0.33f);
        }

        private int CalculateFontSize()
        {
            // Base font size for reference resolution (e.g., 1920x1080)
            var baseFontSize = 48;
            var referenceWidth = 1920.0f; // Reference resolution width
            var referenceHeight = 1080.0f; // Reference resolution height

            // Determine scaling factors for width and height
            var widthScale = Screen.width / referenceWidth;
            var heightScale = Screen.height / referenceHeight;

            // Choose the smaller scaling factor to maintain aspect ratio
            var scalingFactor = Mathf.Min(widthScale, heightScale);

            // Calculate and return the scaled font size
            return Mathf.RoundToInt(baseFontSize * scalingFactor);
        }

        private void DrawLogWindow(int windowID)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            foreach (var log in _logEntries)
            {
                GUILayout.Label(log, _logStyle);
            }

            GUILayout.EndScrollView();

            GUI.DragWindow(new Rect(0, 0, 10000, 40));
        }
    }
}
