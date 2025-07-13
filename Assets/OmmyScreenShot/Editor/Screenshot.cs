using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Ommy.Screenshot
{
    // Improved GIF Encoder for Unity
    public class SimpleGifEncoder
    {
        private List<Color32[]> frames = new List<Color32[]>();
        private int width, height;
        private int delay; // delay in 1/100th of a second
        private Color32[] globalPalette;
        private int quality;
        
        public SimpleGifEncoder(int width, int height, float frameRate, int quality = 80)
        {
            this.width = width;
            this.height = height;
            this.delay = Mathf.RoundToInt(100f / frameRate);
            this.quality = Mathf.Clamp(quality, 1, 100);
            GenerateGlobalPalette();
        }
        
        public void AddFrame(Texture2D texture)
        {
            // Ensure we have valid texture data
            if (texture == null)
            {
                Debug.LogError("Cannot add null texture to GIF encoder");
                return;
            }
            
            // Get pixels in Color32 format for better performance
            var pixels = texture.GetPixels32();
            
            // Verify we have valid pixel data
            if (pixels == null || pixels.Length == 0)
            {
                Debug.LogError("Texture has no pixel data");
                return;
            }
            
            // Check if texture dimensions match expected dimensions
            if (pixels.Length != width * height)
            {
                Debug.LogWarning($"Texture size mismatch. Expected {width}x{height} ({width * height} pixels), got {pixels.Length} pixels");
            }
            
            frames.Add(pixels);
        }
        
        public void SaveGif(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                WriteGifHeader(writer);
                WriteApplicationExtension(writer); // For looping
                
                for (int i = 0; i < frames.Count; i++)
                {
                    WriteGraphicsControlExtension(writer);
                    WriteImageDescriptor(writer);
                    WriteImageData(writer, frames[i]);
                }
                
                writer.Write((byte)0x3B); // GIF trailer
            }
        }
        
        private void GenerateGlobalPalette()
        {
            // Generate palette based on quality setting with better color distribution
            globalPalette = new Color32[256];
            
            // Calculate color steps based on quality (higher quality = more colors)
            int steps = Mathf.Max(3, Mathf.RoundToInt(Mathf.Sqrt(quality * 2.56f))); // 3-16 steps based on quality
            steps = Mathf.Min(steps, 8); // Cap at 8 for memory efficiency
            
            int index = 0;
            
            // Create RGB cube with uniform distribution
            for (int r = 0; r < steps && index < 256; r++)
            {
                for (int g = 0; g < steps && index < 256; g++)
                {
                    for (int b = 0; b < steps && index < 256; b++)
                    {
                        // Use better color distribution
                        byte red = steps == 1 ? (byte)0 : (byte)(r * 255 / (steps - 1));
                        byte green = steps == 1 ? (byte)0 : (byte)(g * 255 / (steps - 1));
                        byte blue = steps == 1 ? (byte)0 : (byte)(b * 255 / (steps - 1));
                        globalPalette[index++] = new Color32(red, green, blue, 255);
                    }
                }
            }
            
            // Fill remaining slots with grayscale values for better coverage
            int usedColors = index;
            int remainingSlots = 256 - usedColors;
            
            for (int i = 0; i < remainingSlots; i++)
            {
                byte gray = (byte)(i * 255 / Math.Max(1, remainingSlots - 1));
                globalPalette[usedColors + i] = new Color32(gray, gray, gray, 255);
            }
            
            Debug.Log($"Generated GIF palette with {steps}x{steps}x{steps} = {steps*steps*steps} colors + {remainingSlots} grayscale values");
        }
        
        private byte GetNearestPaletteIndex(Color32 color)
        {
            byte bestIndex = 0;
            int bestDistance = int.MaxValue;
            
            for (int i = 0; i < globalPalette.Length; i++)
            {
                var paletteColor = globalPalette[i];
                int distance = Math.Abs(color.r - paletteColor.r) + 
                              Math.Abs(color.g - paletteColor.g) + 
                              Math.Abs(color.b - paletteColor.b);
                              
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = (byte)i;
                    if (distance == 0) break; // Perfect match
                }
            }
            
            return bestIndex;
        }
        
        private void WriteGifHeader(BinaryWriter writer)
        {
            // GIF89a header
            writer.Write("GIF89a".ToCharArray());
            writer.Write((ushort)width);
            writer.Write((ushort)height);
            writer.Write((byte)0xF7); // Global color table: 256 colors
            writer.Write((byte)0);    // Background color index
            writer.Write((byte)0);    // Pixel aspect ratio
            
            // Write global color table
            foreach (var color in globalPalette)
            {
                writer.Write(color.r);
                writer.Write(color.g);
                writer.Write(color.b);
            }
        }
        
        private void WriteApplicationExtension(BinaryWriter writer)
        {
            // Application extension for looping
            writer.Write((byte)0x21); // Extension introducer
            writer.Write((byte)0xFF); // Application extension label
            writer.Write((byte)0x0B); // Block size
            writer.Write("NETSCAPE2.0".ToCharArray());
            writer.Write((byte)0x03); // Sub-block size
            writer.Write((byte)0x01); // Loop indicator
            writer.Write((ushort)0);  // Loop count (0 = infinite)
            writer.Write((byte)0x00); // Block terminator
        }
        
        private void WriteGraphicsControlExtension(BinaryWriter writer)
        {
            writer.Write((byte)0x21); // Extension introducer
            writer.Write((byte)0xF9); // Graphics control label
            writer.Write((byte)0x04); // Block size
            writer.Write((byte)0x08); // Disposal method: restore to background
            writer.Write((ushort)delay); // Delay time
            writer.Write((byte)0x00); // Transparent color index (none)
            writer.Write((byte)0x00); // Block terminator
        }
        
        private void WriteImageDescriptor(BinaryWriter writer)
        {
            writer.Write((byte)0x2C); // Image separator
            writer.Write((ushort)0);  // Left position
            writer.Write((ushort)0);  // Top position
            writer.Write((ushort)width);
            writer.Write((ushort)height);
            writer.Write((byte)0x00); // No local color table
        }
        
        private void WriteImageData(BinaryWriter writer, Color32[] frameData)
        {
            // Convert frame to indexed data
            var indexedData = new byte[frameData.Length];
            for (int i = 0; i < frameData.Length; i++)
            {
                indexedData[i] = GetNearestPaletteIndex(frameData[i]);
            }
            
            // Simple LZW-style compression
            writer.Write((byte)8); // LZW minimum code size
            
            var compressed = CompressLZW(indexedData);
            
            // Write compressed data in blocks
            int offset = 0;
            while (offset < compressed.Count)
            {
                int blockSize = Math.Min(255, compressed.Count - offset);
                writer.Write((byte)blockSize);
                
                for (int i = 0; i < blockSize; i++)
                {
                    writer.Write(compressed[offset + i]);
                }
                offset += blockSize;
            }
            
            writer.Write((byte)0x00); // Block terminator
        }
        
        private List<byte> CompressLZW(byte[] data)
        {
            var result = new List<byte>();
            var dictionary = new Dictionary<string, int>();
            
            // Initialize dictionary with single bytes
            for (int i = 0; i < 256; i++)
            {
                dictionary[((char)i).ToString()] = i;
            }
            
            int nextCode = 256;
            string current = "";
            
            foreach (byte b in data)
            {
                string next = current + (char)b;
                
                if (dictionary.ContainsKey(next))
                {
                    current = next;
                }
                else
                {
                    // Output current code
                    result.Add((byte)dictionary[current]);
                    
                    // Add new string to dictionary
                    if (nextCode < 4096) // Limit dictionary size
                    {
                        dictionary[next] = nextCode++;
                    }
                    
                    current = ((char)b).ToString();
                }
            }
            
            // Output final code
            if (!string.IsNullOrEmpty(current))
            {
                result.Add((byte)dictionary[current]);
            }
            
            return result;
        }
    }

    [ExecuteInEditMode]
    public class OmmyScreenshot : EditorWindow
    {
        #region Private Fields
        [SerializeField] private Camera targetCamera;
        [SerializeField] private int captureScale = 1;
        [SerializeField] private string savePath = "";
        [SerializeField] private bool useAlpha = false;
        [SerializeField] private bool isPortraitMode = false;
        [SerializeField] private bool shouldTakeHiResShot = false;
        [SerializeField] private string lastScreenshotPath = "";
        [SerializeField] private float timerDelay = 0f;
        [SerializeField] private bool isLoopCapture = false;
        [SerializeField] private bool captureAllCameras = false;
        
        // GIF Recording fields
        [SerializeField] private bool isRecordingGif = false;
        [SerializeField] private float gifFrameRate = 10f;
        [SerializeField] private float gifDuration = 3f;
        [SerializeField] private int gifWidth = 512;
        [SerializeField] private int gifHeight = 512;
        [SerializeField] private bool gifPerformanceMode = false;
        [SerializeField] private bool exportAsGif = true;
        [SerializeField] private int gifQuality = 80;
        
        private Texture2D logoTexture;
        private List<Vector2Int> customResolutions = new List<Vector2Int>
        {
            new Vector2Int(1920, 1080), // Full HD
            new Vector2Int(1280, 720),  // HD
            new Vector2Int(2560, 1440), // QHD
            new Vector2Int(3840, 2160)  // 4K
        };
        private Vector2 scrollPosition = Vector2.zero;
        private float currentTimer = 0f;
        private bool isTimerActive = false;
        private bool isUIScreenshot = false;
        private List<Camera> allCameras = new List<Camera>();
        private double timerStartTime = 0;
        
        // GIF Recording runtime fields
        private List<Texture2D> gifFrames = new List<Texture2D>();
        private double lastGifFrameTime = 0;
        private double gifRecordingStartTime = 0;
        private int skippedFrames = 0;
        private SimpleGifEncoder gifEncoder;
        
        // GUI Style Cache
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle buttonStyle;
        private GUIStyle primaryButtonStyle;
        private GUIStyle browseButtonStyle;
        private GUIStyle textFieldStyle;
        private GUIStyle intFieldStyle;
        private GUIStyle toggleStyle;
        private GUIStyle warningButtonStyle;
        private bool stylesInitialized = false;
        #endregion

        #region Unity Lifecycle
        [MenuItem("Ommy/Ommy Screenshot/Open ScreenShoot Window")]
        public static void ShowWindow()
        {
            EditorWindow editorWindow = GetWindow<OmmyScreenshot>();
            editorWindow.autoRepaintOnSceneChange = true;
            editorWindow.titleContent = new GUIContent("Screenshot");
            editorWindow.Show();
        }

        void OnEnable()
        {
            logoTexture = Resources.Load<Texture2D>("logo");
            RefreshCameraList();
        }

        void Update()
        {
            if (isTimerActive)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                double elapsedTime = currentTime - timerStartTime;
                currentTimer = timerDelay - (float)elapsedTime;
                
                if (currentTimer <= 0f)
                {
                    if (isUIScreenshot)
                    {
                        CaptureScreenshotsWithUI();
                        isUIScreenshot = false;
                    }
                    else
                    {
                        TakeHiResShot();
                    }
                    
                    // Handle loop capture
                    if (isLoopCapture && timerDelay > 0f)
                    {
                        timerStartTime = EditorApplication.timeSinceStartup; // Reset timer for next capture
                        currentTimer = timerDelay;
                        Debug.Log($"Loop capture: Next screenshot in {timerDelay} seconds");
                    }
                    else
                    {
                        isTimerActive = false;
                    }
                }
                Repaint(); // Refresh the window to show countdown
            }
            
            // Handle GIF recording
            if (isRecordingGif)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                double elapsedTime = currentTime - gifRecordingStartTime;
                
                // Check if we should capture a new frame using high precision timing
                double frameInterval = 1.0 / gifFrameRate;
                double timeSinceLastFrame = currentTime - lastGifFrameTime;
                
                if (timeSinceLastFrame >= frameInterval)
                {
                    // Check if we're falling behind and need to skip frames
                    if (timeSinceLastFrame > frameInterval * 2.0)
                    {
                        skippedFrames++;
                        // Skip to the next expected frame time to catch up
                        lastGifFrameTime = currentTime;
                    }
                    else
                    {
                        CaptureGifFrame();
                        lastGifFrameTime = currentTime;
                    }
                }
                
                // Check if recording duration is complete
                if (elapsedTime >= gifDuration)
                {
                    StopGifRecording();
                }
                
                Repaint(); // Refresh the window to show progress
            }
        }
        #endregion

        #region GUI Methods
        void OnGUI()
        {
            InitializeStyles();
            
            EditorGUILayout.BeginVertical("box");
            GUILayout.Space(10);
            
            DrawHeader();
            DrawScrollableContent();
            
            if (shouldTakeHiResShot)
            {
                CaptureScreenshots();
                shouldTakeHiResShot = false;
            }
            
            EditorGUILayout.EndVertical();
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) },
                margin = new RectOffset(0, 0, 10, 5)
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = CreateTexture(new Color(0.3f, 0.6f, 0.9f)) },
                hover = { background = CreateTexture(new Color(0.4f, 0.7f, 1.0f)) },
                fixedHeight = 35,
                margin = new RectOffset(2, 2, 2, 2)
            };

            primaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = CreateTexture(new Color(0.2f, 0.8f, 0.3f)) },
                hover = { background = CreateTexture(new Color(0.3f, 0.9f, 0.4f)) },
                fixedHeight = 40,
                margin = new RectOffset(2, 2, 2, 2)
            };

            browseButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = CreateTexture(new Color(0.5f, 0.5f, 0.5f)) },
                hover = { background = CreateTexture(new Color(0.6f, 0.6f, 0.6f)) },
                fixedHeight = 20
            };

            textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                normal = { textColor = Color.white, background = CreateTexture(new Color(0.2f, 0.2f, 0.2f)) },
                focused = { background = CreateTexture(new Color(0.3f, 0.3f, 0.3f)) }
            };

            intFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white, background = CreateTexture(new Color(0.2f, 0.2f, 0.2f)) },
                focused = { background = CreateTexture(new Color(0.3f, 0.3f, 0.3f)) }
            };

            toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontStyle = FontStyle.Normal,
                fontSize = 12
            };
            
            warningButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = CreateTexture(new Color(0.9f, 0.4f, 0.2f)) },
                hover = { background = CreateTexture(new Color(1.0f, 0.5f, 0.3f)) },
                fixedHeight = 35,
                margin = new RectOffset(2, 2, 2, 2)
            };

            stylesInitialized = true;
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(70));
            
            // Logo section
            if (logoTexture != null)
            {
                GUILayout.Label(logoTexture, GUILayout.Height(64), GUILayout.Width(64));
            }
            else
            {
                // Placeholder space if no logo
                GUILayout.Space(64);
            }
            
            GUILayout.Space(5); // Reduced space between logo and text
            
            // Title section - centered vertically
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace(); // Push content to center
            GUILayout.Label("Ommy Screenshot Tool", headerStyle);
            GUILayout.Label("Professional Screenshot Capture for Unity", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace(); // Push content to center
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(15);
        }

        private void DrawScrollableContent()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawResolutionSection();
            DrawPresetSection();
            DrawPathSection();
            DrawCameraSection();
            DrawTimerSection();
            DrawGifSection();
            DrawCaptureButtons();
            DrawUtilityButtons();
            
            EditorGUILayout.EndScrollView();
        }
        #endregion
        private void DrawResolutionSection()
        {
            GUILayout.Label("Custom Resolutions", subHeaderStyle);
            GUILayout.Space(5);

            for (int i = 0; i < customResolutions.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                customResolutions[i] = new Vector2Int(
                    EditorGUILayout.IntField(customResolutions[i].x, intFieldStyle, GUILayout.Width(80)),
                    EditorGUILayout.IntField(customResolutions[i].y, intFieldStyle, GUILayout.Width(80))
                );

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    customResolutions.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Resolution", GUILayout.Width(150)))
            {
                customResolutions.Add(new Vector2Int(1920, 1080));
            }

            GUILayout.Space(10);
        }

        private void DrawPresetSection()
        {
            GUILayout.Label("Presets", subHeaderStyle);
            GUILayout.Space(5);
            
            isPortraitMode = EditorGUILayout.Toggle("Is Portrait Mode", isPortraitMode, toggleStyle);
            EditorGUILayout.HelpBox("Check if your game is in portrait mode.", MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Icon Sizes", buttonStyle))
            {
                customResolutions.Clear();
                customResolutions.Add(new Vector2Int(1024, 1024));
                customResolutions.Add(new Vector2Int(512, 512));
            }
            
            if (GUILayout.Button("iOS Sizes", buttonStyle))
            {
                customResolutions.Clear();
                if (isPortraitMode)
                {
                    customResolutions.Add(new Vector2Int(1290, 2796));
                    customResolutions.Add(new Vector2Int(1284, 2778));
                    customResolutions.Add(new Vector2Int(1242, 2208));
                    customResolutions.Add(new Vector2Int(2048, 2732));
                }
                else
                {
                    customResolutions.Add(new Vector2Int(2796, 1290));
                    customResolutions.Add(new Vector2Int(2778, 1284));
                    customResolutions.Add(new Vector2Int(2208, 1242));
                    customResolutions.Add(new Vector2Int(2732, 2048));
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Android Sizes", buttonStyle))
            {
                customResolutions.Clear();
                customResolutions.Add(isPortraitMode ? new Vector2Int(1080, 1920) : new Vector2Int(1920, 1080));
            }
            
            if (GUILayout.Button("Feature Image", buttonStyle))
            {
                customResolutions.Clear();
                customResolutions.Add(new Vector2Int(1024, 500));
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Set To Game View Size", buttonStyle))
            {
                Vector2 screenSize = Handles.GetMainGameViewSize();
                customResolutions.Clear();
                customResolutions.Add(new Vector2Int((int)screenSize.x, (int)screenSize.y));
            }
            
            GUILayout.Space(10);
        }

        private void DrawPathSection()
        {
            GUILayout.Label("Save Path", subHeaderStyle);
            GUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            savePath = EditorGUILayout.TextField(savePath, textFieldStyle);
            if (GUILayout.Button("Browse", browseButtonStyle, GUILayout.Width(80)))
            {
                savePath = EditorUtility.SaveFolderPanel("Path to Save Images", savePath, Application.dataPath);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Choose the folder to save the screenshots.", MessageType.Info);
            GUILayout.Space(10);
        }

        private void DrawCameraSection()
        {
            GUILayout.Label("Camera Settings", subHeaderStyle);
            GUILayout.Space(5);
            
            captureAllCameras = EditorGUILayout.Toggle("Capture All Cameras", captureAllCameras, toggleStyle);
            
            if (captureAllCameras)
            {
                EditorGUILayout.HelpBox($"Will capture from {allCameras.Count} cameras in the scene.", MessageType.Info);
                
                if (GUILayout.Button("Refresh Camera List", GUILayout.Width(150)))
                {
                    RefreshCameraList();
                }
                
                if (allCameras.Count > 0)
                {
                    EditorGUILayout.BeginVertical("box");
                    GUILayout.Label("Cameras to capture:", EditorStyles.boldLabel);
                    for (int i = 0; i < allCameras.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{i + 1}. {allCameras[i].name}", GUILayout.ExpandWidth(true));
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            else
            {
                targetCamera = (Camera)EditorGUILayout.ObjectField("Target Camera", targetCamera, typeof(Camera), true);
                
                if (targetCamera == null)
                    targetCamera = Camera.main;
                    
                EditorGUILayout.HelpBox("Single camera mode. Use the selected camera for screenshots.", MessageType.Info);
            }

            useAlpha = EditorGUILayout.Toggle("Use Alpha Channel", useAlpha, toggleStyle);
            EditorGUILayout.HelpBox("Check to use ARGB32 format instead of RGB24.", MessageType.Info);

            GUILayout.Space(10);
        }

        private void DrawTimerSection()
        {
            GUILayout.Label("Timer Settings", subHeaderStyle);
            GUILayout.Space(5);
            
            timerDelay = EditorGUILayout.FloatField("Timer Delay (seconds)", timerDelay, textFieldStyle);
            isLoopCapture = EditorGUILayout.Toggle("Loop Capture", isLoopCapture, toggleStyle);
            
            if (isTimerActive)
            {
                EditorGUILayout.HelpBox($"Timer Active: {currentTimer:F1} seconds remaining", MessageType.Warning);
                if (isLoopCapture)
                {
                    EditorGUILayout.HelpBox("Loop mode enabled - will continue capturing automatically", MessageType.Info);
                    if (GUILayout.Button("Stop Loop Capture", GUILayout.Width(150)))
                    {
                        isTimerActive = false;
                        isLoopCapture = false;
                        Debug.Log("Loop capture stopped by user");
                    }
                }
            }
            else
            {
                if (isLoopCapture && timerDelay > 0f)
                {
                    EditorGUILayout.HelpBox($"Loop mode: Will capture screenshots every {timerDelay} seconds.", MessageType.Info);
                }
                else if (timerDelay > 0f)
                {
                    EditorGUILayout.HelpBox("Single timer mode: Will capture one screenshot after delay.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Instant capture mode: Screenshots taken immediately.", MessageType.Info);
                }
            }

            GUILayout.Space(10);
        }

        private void DrawGifSection()
        {
            GUILayout.Label("GIF Recording", subHeaderStyle);
            GUILayout.Space(5);
            
            // GIF Resolution
            GUILayout.Label("GIF Resolution", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Width:", GUILayout.Width(50));
            gifWidth = EditorGUILayout.IntField(gifWidth, intFieldStyle, GUILayout.Width(80));
            GUILayout.Space(10);
            GUILayout.Label("Height:", GUILayout.Width(50));
            gifHeight = EditorGUILayout.IntField(gifHeight, intFieldStyle, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            
            // Ensure reasonable values
            gifWidth = Mathf.Clamp(gifWidth, 64, 4096);
            gifHeight = Mathf.Clamp(gifHeight, 64, 4096);
            
            gifFrameRate = EditorGUILayout.FloatField("Frame Rate (FPS)", gifFrameRate, textFieldStyle);
            gifFrameRate = Mathf.Clamp(gifFrameRate, 1f, 60f); // Clamp to reasonable range
            gifDuration = EditorGUILayout.FloatField("Duration (seconds)", gifDuration, textFieldStyle);
            gifDuration = Mathf.Clamp(gifDuration, 0.1f, 30f); // Clamp to reasonable range
            
            // Show estimated frame count
            int estimatedFrames = Mathf.RoundToInt(gifFrameRate * gifDuration);
            if (captureAllCameras)
                estimatedFrames *= allCameras.Count;
            EditorGUILayout.LabelField($"Estimated frames: {estimatedFrames}", EditorStyles.miniLabel);
            
            gifPerformanceMode = EditorGUILayout.Toggle("Performance Mode", gifPerformanceMode, toggleStyle);
            if (gifPerformanceMode)
            {
                EditorGUILayout.HelpBox("Performance mode: Uses lower quality but faster capture method", MessageType.Info);
            }
            
            exportAsGif = EditorGUILayout.Toggle("Export as GIF", exportAsGif, toggleStyle);
            if (exportAsGif)
            {
                gifQuality = EditorGUILayout.IntField("GIF Quality (1-100)", gifQuality, intFieldStyle);
                gifQuality = Mathf.Clamp(gifQuality, 1, 100); // Ensure valid range
                EditorGUILayout.HelpBox("Higher quality = larger file size. Recommended: 70-90", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Will save as PNG sequence for manual conversion", MessageType.Info);
            }
            
            if (isRecordingGif)
            {
                double elapsed = EditorApplication.timeSinceStartup - gifRecordingStartTime;
                float progress = (float)(elapsed / gifDuration);
                string cameraMode = captureAllCameras ? $"All Cameras ({allCameras.Count})" : $"Single Camera";
                
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"Recording: {elapsed:F1}s / {gifDuration:F1}s");
                
                string performanceText = skippedFrames > 0 ? 
                    $"Recording GIF from {cameraMode}... {gifFrames.Count} frames captured, {skippedFrames} skipped" :
                    $"Recording GIF from {cameraMode}... {gifFrames.Count} frames captured";
                EditorGUILayout.HelpBox(performanceText, MessageType.Warning);
                
                if (GUILayout.Button("Stop Recording", warningButtonStyle))
                {
                    StopGifRecording();
                }
            }
            else
            {
                string cameraMode = captureAllCameras ? $"all {allCameras.Count} cameras" : "selected camera";
                string exportMode = exportAsGif ? "GIF file" : "PNG sequence";
                EditorGUILayout.HelpBox($"Will record {gifFrameRate} FPS for {gifDuration} seconds at {gifWidth}x{gifHeight} from {cameraMode} and export as {exportMode}", MessageType.Info);
                
                if (GUILayout.Button("Start GIF Recording", primaryButtonStyle))
                {
                    StartGifRecording();
                }
            }

            GUILayout.Space(10);
        }

        private void DrawCaptureButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Take Screenshot", primaryButtonStyle))
            {
                if (string.IsNullOrEmpty(savePath))
                {
                    savePath = EditorUtility.SaveFolderPanel("Path to Save Images", savePath, Application.dataPath);
                    Debug.Log("Path Set");
                }
                
                if (timerDelay > 0f)
                {
                    currentTimer = timerDelay;
                    timerStartTime = EditorApplication.timeSinceStartup;
                    isTimerActive = true;
                    isUIScreenshot = false;
                    Debug.Log($"Screenshot timer started: {timerDelay} seconds");
                }
                else
                {
                    TakeHiResShot();
                }
            }

            if (GUILayout.Button("Take Screenshot With UI", buttonStyle))
            {
                if (string.IsNullOrEmpty(savePath))
                {
                    savePath = EditorUtility.SaveFolderPanel("Path to Save Images", savePath, Application.dataPath);
                    Debug.Log("Path Set");
                }
                
                if (timerDelay > 0f)
                {
                    currentTimer = timerDelay;
                    timerStartTime = EditorApplication.timeSinceStartup;
                    isTimerActive = true;
                    isUIScreenshot = true;
                    Debug.Log($"Screenshot with UI timer started: {timerDelay} seconds");
                }
                else
                {
                    CaptureScreenshotsWithUI();
                }
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void DrawUtilityButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Open Last Screenshot", buttonStyle))
            {
                if (!string.IsNullOrEmpty(lastScreenshotPath))
                {
                    Application.OpenURL("file://" + lastScreenshotPath);
                    Debug.Log("Opening File " + lastScreenshotPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("No Screenshot", "No screenshot has been taken yet.", "OK");
                }
            }

            if (GUILayout.Button("Open Folder", buttonStyle))
            {
                if (!string.IsNullOrEmpty(savePath))
                {
                    Application.OpenURL("file://" + savePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("No Path Set", "Please set a save path first.", "OK");
                }
            }

            if (GUILayout.Button("More Assets", buttonStyle))
            {
                Application.OpenURL("https://assetstore.unity.com/publishers/71963");
            }

            EditorGUILayout.EndHorizontal();
        }
        #region Screenshot Methods
        [ExecuteAlways]
        void CaptureScreenshotsWithUI()
        {
            foreach (var resolution in customResolutions)
            {
                EditorApplication.ExecuteMenuItem("Window/General/Game");

                int resWidthN = resolution.x * captureScale;
                int resHeightN = resolution.y * captureScale;
                string filename = ScreenShotName(resWidthN, resHeightN, "GameView");

                ScreenCapture.CaptureScreenshot(filename, captureScale);

                Debug.Log($"Took screenshot with UI to: {filename}");
            }

            // Open the last screenshot taken
            if (customResolutions.Count > 0)
            {
                var lastResolution = customResolutions[customResolutions.Count - 1];
                Application.OpenURL("file://" + ScreenShotName(lastResolution.x * captureScale, lastResolution.y * captureScale, "GameView"));
            }
        }

        public string ScreenShotName(int width, int height, string cameraName = "")
        {
            string cameraPrefix = string.IsNullOrEmpty(cameraName) ? "" : $"{cameraName}_";
            string strPath = string.Format("{0}/{1}screen_{2}x{3}_{4}.png", savePath, cameraPrefix, width, height, System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            lastScreenshotPath = strPath;
            return strPath;
        }

        public void TakeHiResShot()
        {
            Debug.Log("Taking Screenshot");
            shouldTakeHiResShot = true;
        }

        void CaptureScreenshots()
        {
            List<Camera> camerasToCapture = captureAllCameras ? allCameras : new List<Camera> { targetCamera };
            
            if (camerasToCapture.Count == 0 || (camerasToCapture.Count == 1 && camerasToCapture[0] == null))
            {
                Debug.LogWarning("No valid cameras found for screenshot capture!");
                return;
            }

            foreach (var camera in camerasToCapture)
            {
                if (camera == null) continue;

                foreach (var resolution in customResolutions)
                {
                    int resWidthN = resolution.x * captureScale;
                    int resHeightN = resolution.y * captureScale;
                    string filename = ScreenShotName(resWidthN, resHeightN, camera.name);

                    RenderTexture rt = new RenderTexture(resWidthN, resHeightN, 24);
                    camera.targetTexture = rt;

                    TextureFormat tFormat = useAlpha ? TextureFormat.ARGB32 : TextureFormat.RGB24;
                    Texture2D screenShot = new Texture2D(resWidthN, resHeightN, tFormat, false);
                    camera.Render();
                    RenderTexture.active = rt;
                    screenShot.ReadPixels(new Rect(0, 0, resWidthN, resHeightN), 0, 0);
                    camera.targetTexture = null;
                    RenderTexture.active = null; // added line to fix an issue with RenderTexture
                    DestroyImmediate(rt); // added line to avoid memory leaks

                    // Save the screenshot as a PNG file
                    byte[] bytes = screenShot.EncodeToPNG();
                    File.WriteAllBytes(filename, bytes);

                    // Clean up memory
                    DestroyImmediate(screenShot);

                    Debug.Log($"Took screenshot from camera '{camera.name}' to: {filename}");
                }
            }

            // Open the last screenshot taken
            if (customResolutions.Count > 0 && camerasToCapture.Count > 0)
            {
                var lastResolution = customResolutions[customResolutions.Count - 1];
                var lastCamera = camerasToCapture[camerasToCapture.Count - 1];
                Application.OpenURL("file://" + ScreenShotName(lastResolution.x * captureScale, lastResolution.y * captureScale, lastCamera.name));
            }
        }
        #endregion

        #region GIF Recording Methods
        private void StartGifRecording()
        {
            List<Camera> camerasToRecord = captureAllCameras ? allCameras : new List<Camera> { targetCamera };
            
            if (camerasToRecord.Count == 0 || (camerasToRecord.Count == 1 && camerasToRecord[0] == null))
            {
                Debug.LogWarning("No valid cameras selected for GIF recording!");
                return;
            }
            
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = EditorUtility.SaveFolderPanel("Path to Save GIF Frames", savePath, Application.dataPath);
                if (string.IsNullOrEmpty(savePath))
                {
                    Debug.Log("GIF recording cancelled - no save path selected");
                    return;
                }
            }
            
            isRecordingGif = true;
            gifRecordingStartTime = EditorApplication.timeSinceStartup;
            lastGifFrameTime = EditorApplication.timeSinceStartup;
            gifFrames.Clear();
            skippedFrames = 0;
            
            // Initialize GIF encoder if we're exporting as GIF
            if (exportAsGif)
            {
                gifEncoder = new SimpleGifEncoder(gifWidth, gifHeight, gifFrameRate, gifQuality);
            }
            
            string cameraInfo = captureAllCameras ? $"all {camerasToRecord.Count} cameras" : $"camera '{camerasToRecord[0]?.name}'";
            Debug.Log($"Started GIF recording: {gifDuration}s at {gifFrameRate} FPS, resolution {gifWidth}x{gifHeight} from {cameraInfo}");
        }
        
        private void StopGifRecording()
        {
            if (!isRecordingGif)
                return;
                
            isRecordingGif = false;
            SaveGifFrames();
            
            string performanceInfo = skippedFrames > 0 ? 
                $"Captured {gifFrames.Count} frames (skipped {skippedFrames} frames due to performance)" : 
                $"Captured {gifFrames.Count} frames with perfect timing";
            Debug.Log($"GIF recording stopped. {performanceInfo}");
        }
        
        private void CaptureGifFrame()
        {
            List<Camera> camerasToCapture = captureAllCameras ? allCameras : new List<Camera> { targetCamera };
            
            if (camerasToCapture.Count == 0 || (camerasToCapture.Count == 1 && camerasToCapture[0] == null))
                return;
            
            // Use ARGB32 for reliable color capture - RGB565 can cause issues
            RenderTextureFormat rtFormat = RenderTextureFormat.ARGB32;
            TextureFormat texFormat = TextureFormat.RGB24; // Always use RGB24 for GIF compatibility
            
            foreach (var camera in camerasToCapture)
            {
                if (camera == null) continue;
                
                // Store previous render texture
                RenderTexture previousRT = camera.targetTexture;
                RenderTexture previousActive = RenderTexture.active;
                
                // Create render texture for this specific capture
                RenderTexture rt = RenderTexture.GetTemporary(gifWidth, gifHeight, 24, rtFormat);
                rt.Create(); // Ensure render texture is created
                
                try
                {
                    // Set up camera for rendering
                    camera.targetTexture = rt;
                    
                    // Force camera to render immediately
                    camera.Render();
                    
                    // Wait for GPU to finish rendering
                    RenderTexture.active = rt;
                    
                    // Create texture with proper format
                    Texture2D frame = new Texture2D(gifWidth, gifHeight, texFormat, false);
                    
                    // Read pixels from render texture
                    frame.ReadPixels(new Rect(0, 0, gifWidth, gifHeight), 0, 0);
                    
                    // Always apply to ensure pixel data is transferred properly
                    frame.Apply();
                    
                    // Verify frame has valid data (not all black)
                    var pixels = frame.GetPixels();
                    bool hasNonBlackPixels = false;
                    for (int i = 0; i < pixels.Length && !hasNonBlackPixels; i++)
                    {
                        if (pixels[i].r > 0.01f || pixels[i].g > 0.01f || pixels[i].b > 0.01f)
                        {
                            hasNonBlackPixels = true;
                        }
                    }
                    
                    if (!hasNonBlackPixels)
                    {
                        Debug.LogWarning($"Captured GIF frame appears to be black from camera: {camera.name}");
                    }
                    
                    // Store camera name in texture name for identification
                    frame.name = $"frame_{gifFrames.Count:D4}_{camera.name}";
                    
                    // Add frame to GIF encoder if exporting as GIF
                    if (exportAsGif && gifEncoder != null)
                    {
                        gifEncoder.AddFrame(frame);
                    }
                    
                    // Add frame to our collection (for PNG fallback or preview)
                    gifFrames.Add(frame);
                }
                finally
                {
                    // Always restore camera and cleanup
                    camera.targetTexture = previousRT;
                    RenderTexture.active = previousActive;
                    RenderTexture.ReleaseTemporary(rt);
                }
            }
        }
        
        private void SaveGifFrames()
        {
            if (gifFrames.Count == 0)
            {
                Debug.LogWarning("No GIF frames to save!");
                return;
            }
            
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string cameraMode = captureAllCameras ? "AllCameras" : "SingleCamera";
            
            if (exportAsGif && gifEncoder != null)
            {
                // Save as GIF file
                string gifFileName = $"GIF_{cameraMode}_{timestamp}.gif";
                string gifFilePath = Path.Combine(savePath, gifFileName);
                
                try
                {
                    gifEncoder.SaveGif(gifFilePath);
                    Debug.Log($"GIF saved successfully: {gifFilePath}");
                    Debug.Log($"Total frames: {gifFrames.Count}");
                    
                    // Open the GIF file
                    Application.OpenURL("file://" + gifFilePath);
                    
                    // Create a simple info file
                    string infoPath = Path.Combine(savePath, $"GIF_Info_{timestamp}.txt");
                    string cameraInfo = captureAllCameras ? 
                        $"Multi-camera recording from {allCameras.Count} cameras" : 
                        "Single camera recording";
                        
                    string info = $@"GIF Export Complete!
{cameraInfo}
File: {gifFileName}
Frames: {gifFrames.Count}
Resolution: {gifWidth}x{gifHeight}
Frame Rate: {gifFrameRate} FPS
Duration: {gifDuration} seconds
Quality: {gifQuality}%
Performance Mode: {(gifPerformanceMode ? "Enabled" : "Disabled")}

GIF file location: {gifFilePath}
";
                    File.WriteAllText(infoPath, info);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to save GIF: {e.Message}");
                    Debug.LogError("Falling back to PNG sequence export...");
                    SaveAsPngSequence(timestamp, cameraMode);
                }
            }
            else
            {
                // Save as PNG sequence
                SaveAsPngSequence(timestamp, cameraMode);
            }
            
            // Clean up memory
            foreach (var frame in gifFrames)
            {
                if (frame != null)
                    DestroyImmediate(frame);
            }
            gifFrames.Clear();
            gifEncoder = null;
        }
        
        private void SaveAsPngSequence(string timestamp, string cameraMode)
        {
            string gifFolderPath = Path.Combine(savePath, $"GIF_Sequence_{cameraMode}_{timestamp}");
            
            if (!Directory.Exists(gifFolderPath))
            {
                Directory.CreateDirectory(gifFolderPath);
            }
            
            // Save each frame as PNG
            int frameCount = gifFrames.Count;
            for (int i = 0; i < frameCount; i++)
            {
                // Use the texture name if it contains camera info, otherwise use sequential naming
                string frameName = string.IsNullOrEmpty(gifFrames[i].name) ? 
                    $"frame_{i:D4}.png" : 
                    $"{gifFrames[i].name}.png";
                    
                string framePath = Path.Combine(gifFolderPath, frameName);
                byte[] bytes = gifFrames[i].EncodeToPNG();
                File.WriteAllBytes(framePath, bytes);
            }
            
            // Create instructions file for creating GIF
            string instructionsPath = Path.Combine(gifFolderPath, "CREATE_GIF_INSTRUCTIONS.txt");
            string cameraInfo = captureAllCameras ? 
                $"Multi-camera recording from {allCameras.Count} cameras" : 
                "Single camera recording";
                
            string instructions = $@"GIF Recording Complete!
{cameraInfo}
Frames saved: {frameCount}
Resolution: {gifWidth}x{gifHeight}
Frame Rate: {gifFrameRate} FPS
Duration: {gifDuration} seconds

To create a GIF from these frames, you can use:

1. FFmpeg (recommended):
   For single camera or combined frames:
   ffmpeg -framerate {gifFrameRate} -i frame_%04d*.png -vf ""palettegen"" palette.png
   ffmpeg -framerate {gifFrameRate} -i frame_%04d*.png -i palette.png -lavfi ""paletteuse"" output.gif
   
   For specific camera frames (if multi-camera):
   ffmpeg -framerate {gifFrameRate} -i frame_%04d_CameraName.png -vf ""palettegen"" palette.png
   ffmpeg -framerate {gifFrameRate} -i frame_%04d_CameraName.png -i palette.png -lavfi ""paletteuse"" CameraName.gif

2. Online tools:
   - Upload PNG files to ezgif.com or similar online GIF makers
   - Set frame rate to {gifFrameRate} FPS
   - For multi-camera: create separate GIFs for each camera or combine as needed

3. Image editing software:
   - Import the PNG sequence into Photoshop, GIMP, or After Effects
   - Export as animated GIF

Frames are located in: {gifFolderPath}
";
            
            File.WriteAllText(instructionsPath, instructions);
            
            Debug.Log($"GIF frames saved to: {gifFolderPath}");
            Debug.Log($"Total frames: {frameCount}");
            
            // Open the folder
            Application.OpenURL("file://" + gifFolderPath);
        }
        #endregion

        #region Helper Methods
        private void RefreshCameraList()
        {
            allCameras.Clear();
            Camera[] sceneCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            allCameras.AddRange(sceneCameras);
            
            // Ensure main camera is first if it exists
            Camera mainCamera = Camera.main;
            if (mainCamera != null && allCameras.Contains(mainCamera))
            {
                allCameras.Remove(mainCamera);
                allCameras.Insert(0, mainCamera);
            }
            
            Debug.Log($"Found {allCameras.Count} cameras in the scene");
        }

        // Helper method to create textures
        private Texture2D CreateTexture(Color color)
        {
            Texture2D result = new Texture2D(1, 1);
            result.SetPixel(0, 0, color);
            result.Apply();
            return result;
        }
        #endregion
    }
}
