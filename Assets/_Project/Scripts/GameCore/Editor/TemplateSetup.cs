using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameCore.Editor
{
    /// Outils de démarrage d'un nouveau jeu à partir du template :
    /// - « Nouveau jeu… » : nom, société, identifiant (bundle id), version ;
    /// - « Créer les scènes de base » : Boot (services) → MainMenu → Game, et la liste de build.
    public class TemplateSetup : EditorWindow
    {
        public const string ScenesFolder = "Assets/_Project/Scenes";
        private static readonly string[] BaseScenes = { "Boot", "MainMenu", "Game" };

        private string productName;
        private string companyName;
        private string bundleId;
        private string version = "0.1.0";

        [MenuItem("Tools/Game Template/Nouveau jeu…", priority = 0)]
        private static void Open()
        {
            var window = GetWindow<TemplateSetup>(true, "Nouveau jeu", true);
            window.minSize = new Vector2(420, 230);
        }

        private void OnEnable()
        {
            productName = PlayerSettings.productName;
            companyName = PlayerSettings.companyName;
            bundleId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            version = PlayerSettings.bundleVersion;
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Règle l'identité du jeu pour Android et iOS. À faire au tout début d'un nouveau jeu, avant de lier les services Unity (Project Settings → Services).", MessageType.Info);
            productName = EditorGUILayout.TextField("Nom du jeu", productName);
            companyName = EditorGUILayout.TextField("Société", companyName);
            if (GUILayout.Button("Proposer un identifiant", GUILayout.Width(180)))
                bundleId = $"com.{Slug(companyName)}.{Slug(productName)}";
            bundleId = EditorGUILayout.TextField("Bundle id", bundleId);
            version = EditorGUILayout.TextField("Version", version);

            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(bundleId)))
                if (GUILayout.Button("Appliquer", GUILayout.Height(30))) Apply();
        }

        private void Apply()
        {
            PlayerSettings.productName = productName.Trim();
            PlayerSettings.companyName = companyName.Trim();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, bundleId.Trim());
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, bundleId.Trim());
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, bundleId.Trim());
            PlayerSettings.bundleVersion = version.Trim();
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.iOS.buildNumber = "1";
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Nouveau jeu",
                $"« {productName} » ({bundleId}) configuré.\n\nÀ faire ensuite :\n" +
                "• Project Settings → Services : lier un NOUVEAU projet Unity Cloud ;\n" +
                "• AdsManager (scène Boot) : clés LevelPlay du nouveau jeu ;\n" +
                "• Store.Products : identifiants des achats ;\n" +
                "• icône, écran de démarrage, keystore Android.", "OK");
        }

        private static string Slug(string s)
            => new string((s ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        [MenuItem("Tools/Game Template/Créer les scènes de base", priority = 1)]
        private static void CreateBaseScenesMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int created = CreateBaseScenes();
            EditorUtility.DisplayDialog("Scènes de base",
                created > 0 ? $"{created} scène(s) créée(s) dans {ScenesFolder}. Liste de build : Boot, MainMenu, Game."
                            : "Les scènes existent déjà. Liste de build remise dans l'ordre Boot, MainMenu, Game.", "OK");
        }

        /// Pour la ligne de commande : Unity -batchmode -executeMethod GameCore.Editor.TemplateSetup.CreateBaseScenesBatch
        public static void CreateBaseScenesBatch() => CreateBaseScenes();

        /// Crée les scènes manquantes et met la liste de build à jour. Renvoie le nombre de scènes créées.
        public static int CreateBaseScenes()
        {
            Directory.CreateDirectory(ScenesFolder);
            int created = 0;
            foreach (var name in BaseScenes)
            {
                string path = $"{ScenesFolder}/{name}.unity";
                if (File.Exists(path)) continue;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                switch (name)
                {
                    case "Boot": BuildBoot(); break;
                    case "MainMenu": BuildUiScene("MainMenu", "PLAY", "Game"); break;
                    case "Game": BuildUiScene("Game", "HOME", "MainMenu"); break;
                }
                EditorSceneManager.SaveScene(scene, path);
                created++;
            }

            var others = EditorBuildSettings.scenes.Where(s => !BaseScenes.Any(b => s.path == $"{ScenesFolder}/{b}.unity") && File.Exists(s.path));
            EditorBuildSettings.scenes = BaseScenes
                .Select(b => new EditorBuildSettingsScene($"{ScenesFolder}/{b}.unity", true))
                .Concat(others)
                .ToArray();
            AssetDatabase.SaveAssets();
            return created;
        }

        private static void BuildBoot()
        {
            var services = new GameObject("GameServices");
            services.AddComponent<GameBoot>();
            services.AddComponent<AdsManager>();
            services.AddComponent<LeaderboardManager>();
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = camera.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        private static void BuildUiScene(string title, string buttonLabel, string targetScene)
        {
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            camera.transform.position = new Vector3(0f, 1f, -10f);
            new GameObject("Directional Light", typeof(Light)).GetComponent<Light>().type = LightType.Directional;

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<SafeAreaRoot>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var label = MakeText(canvasGo.transform, "Title", title.ToUpperInvariant(), font, 110);
            var labelRt = (RectTransform)label.transform;
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 0.75f);
            labelRt.sizeDelta = new Vector2(1000, 200);

            var buttonGo = new GameObject("Button_" + buttonLabel, typeof(Image), typeof(Button), typeof(SceneButton));
            buttonGo.transform.SetParent(canvasGo.transform, false);
            var buttonRt = (RectTransform)buttonGo.transform;
            buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(0.5f, 0.3f);
            buttonRt.sizeDelta = new Vector2(600, 180);
            buttonGo.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f);
            var sceneButton = new SerializedObject(buttonGo.GetComponent<SceneButton>());
            sceneButton.FindProperty("sceneName").stringValue = targetScene;
            sceneButton.ApplyModifiedPropertiesWithoutUndo();
            var text = MakeText(buttonGo.transform, "Text", buttonLabel, font, 72);
            var textRt = (RectTransform)text.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            // Nouveau Input System : module d'UI ajouté par son nom pour ne pas dépendre de l'assembly.
            var module = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (module != null) eventSystem.AddComponent(module);
            else eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static Text MakeText(Transform parent, string name, string value, Font font, int size)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }
    }
}
