using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Improbable.Gdk.Tools.MiniJSON;
using UnityEditor;
using UnityEngine;

namespace Improbable.Gdk.DeploymentManager
{
    internal class DeploymentLauncherUi : EditorWindow
    {
        internal const string BuiltInErrorIcon = "console.erroricon.sml";
        internal const string BuiltInRefreshIcon = "Refresh";
        internal const string BuiltInTrashIcon = "TreeEditor.Trash";

        [MenuItem("SpatialOS/Deployment Launcher", false, 51)]
        private static void LaunchDeploymentMenu()
        {
            var inspectorWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
            var deploymentWindow = GetWindow<DeploymentLauncherUi>(inspectorWindowType);
            deploymentWindow.titleContent.text = "Deployment Launcher";
            deploymentWindow.titleContent.tooltip = "A tab for managing your SpatialOS deployments.";
            deploymentWindow.Show();
        }

        private DeploymentLauncherConfig launcherConfig;

        private static readonly Vector2 SmallIconSize = new Vector2(12, 12);


        private readonly Dictionary<int, object> localState = new Dictionary<int, object>();
        private Vector2 scrollPos;
        private string projectName;
        private string[] snapshots;

        private void OnEnable()
        {
            launcherConfig = DeploymentLauncherConfig.GetInstance();
            projectName = GetProjectName();
            snapshots = GetSnapshots();
        }

        private void OnGUI()
        {
            if (launcherConfig == null)
            {
                GUILayout.Label($"Could not find a {nameof(DeploymentLauncherConfig)} instance.\nPlease create one via the Assets > Create > SpatialOS menu.");
            }

            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Project Name", projectName);

                    var buttonIcon = new GUIContent(EditorGUIUtility.IconContent(BuiltInRefreshIcon))
                    {
                        tooltip = "Refresh your project name.."
                    };

                    GUILayout.Space(EditorGUIUtility.currentViewWidth * 0.6f);

                    if (GUILayout.Button(buttonIcon, EditorStyles.miniButton))
                    {
                        projectName = GetProjectName();
                    }
                }

                DrawHorizontalLine(5);

                launcherConfig.AssemblyConfig = DrawAssemblyConfig(launcherConfig.AssemblyConfig);

                GUILayout.Label("Deployment Configurations", EditorStyles.boldLabel);

                for (var index = 0; index < launcherConfig.DeploymentConfigs.Count; index++)
                {
                    var deplConfig = launcherConfig.DeploymentConfigs[index];
                    var (shouldRemove, updated) = DrawDeploymentConfig(deplConfig);
                    if (shouldRemove)
                    {
                        launcherConfig.DeploymentConfigs.RemoveAt(index);
                        index--;
                    }
                    else
                    {
                        launcherConfig.DeploymentConfigs[index] = updated;
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Add new deployment configuration"))
                    {
                        var deploymentConfig = new DeploymentConfig
                        {
                            AssemblyName = launcherConfig.AssemblyConfig.AssemblyName
                        };

                        deploymentConfig.Deployment.Name = $"deployment_{launcherConfig.DeploymentConfigs.Count}";

                        launcherConfig.DeploymentConfigs.Add(deploymentConfig);
                    }
                }

                scrollPos = scrollView.scrollPosition;
            }

            AssetDatabase.SaveAssets();
        }

        private AssemblyConfig DrawAssemblyConfig(AssemblyConfig config)
        {
            GUILayout.Label("Assembly Upload", EditorStyles.boldLabel);

            var copy = config.DeepCopy();

            using (new EditorGUILayout.VerticalScope())
            {
                copy.AssemblyName = EditorGUILayout.TextField("Assembly Name", config.AssemblyName);
                copy.ShouldForceUpload = EditorGUILayout.Toggle("Force Upload", config.ShouldForceUpload);

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Generate assembly name"))
                    {
                        copy.AssemblyName = $"{projectName}_{DateTime.Now.ToString("MMdd_hhmm")}";
                    }

                    if (GUILayout.Button("Copy assembly to deployments"))
                    {
                        foreach (var deplConfig in launcherConfig.DeploymentConfigs)
                        {
                            deplConfig.AssemblyName = launcherConfig.AssemblyConfig.AssemblyName;
                        }
                    }

                    if (GUILayout.Button("Upload Assembly"))
                    {
                    }
                }
            }

            DrawHorizontalLine(5);

            return copy;
        }

        private (bool, DeploymentConfig) DrawDeploymentConfig(DeploymentConfig config)
        {
            var foldoutState = GetStateObject<bool>(config.Deployment.Name.GetHashCode());
            var copy = config.DeepCopy();

            var errors = copy.GetErrors().ToList();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    foldoutState = EditorGUILayout.Foldout(foldoutState, new GUIContent(config.Deployment.Name), true);

                    GUILayout.FlexibleSpace();

                    using (new EditorGUIUtility.IconSizeScope(SmallIconSize))
                    {
                        if (errors.Count != 0)
                        {
                            GUILayout.Label(new GUIContent(EditorGUIUtility.IconContent(BuiltInErrorIcon))
                            {
                                tooltip = "One or more errors in deployment configuration."
                            });
                        }

                        var buttonContent = new GUIContent(string.Empty, "Remove deployment configuration");
                        buttonContent.image = EditorGUIUtility.IconContent("Toolbar Minus").image;

                        if (GUILayout.Button(buttonContent, EditorStyles.miniButton))
                        {
                            return (true, null);
                        }
                    }
                }

                using (new EditorGUI.IndentLevelScope())
                using (new EditorGUILayout.VerticalScope())
                {
                    if (foldoutState)
                    {
                        copy.AssemblyName = EditorGUILayout.TextField("Assembly Name", config.AssemblyName);
                        RenderBaseDeploymentConfig(config.Deployment, copy.Deployment);

                        if (copy.Deployment.Name != config.Deployment.Name)
                        {
                            UpdateSimulatedDeploymentNames(copy);
                        }

                        GUILayout.Space(10);

                        EditorGUILayout.LabelField("Simulated Player Deployments");

                        for (int i = 0; i < copy.SimulatedPlayerDeploymentConfig.Count; i++)
                        {
                            var simConfig = copy.SimulatedPlayerDeploymentConfig[i];
                            var (shouldRemove, updated) = DrawSimulatedConfig(i, simConfig);

                            GUILayout.Space(5);

                            if (shouldRemove)
                            {
                                copy.SimulatedPlayerDeploymentConfig.RemoveAt(i);
                                i--;
                                UpdateSimulatedDeploymentNames(copy);
                            }
                            else
                            {
                                copy.SimulatedPlayerDeploymentConfig[i] = updated;
                            }
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (foldoutState)
                    {
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Add simulated player deployment"))
                        {
                            var newSimPlayerDepl = new SimulatedPlayerDeploymentConfig();
                            newSimPlayerDepl.TargetDeploymentName = config.Deployment.Name;
                            newSimPlayerDepl.Name = $"{config.Deployment.Name}_sim{config.SimulatedPlayerDeploymentConfig.Count + 1}";

                            copy.SimulatedPlayerDeploymentConfig.Add(newSimPlayerDepl);
                        }
                    }
                }

                if (errors.Count > 0)
                {
                    EditorGUILayout.HelpBox($"This deployment configuration has the following errors:\n  - {string.Join("\n  - ", errors)}", MessageType.Error);
                }

                if (check.changed)
                {
                    SetStateObject(copy.Deployment.Name.GetHashCode(), foldoutState);
                }
            }

            DrawHorizontalLine(5);

            return (false, copy);
        }

        private void RenderBaseDeploymentConfig(BaseDeploymentConfig source, BaseDeploymentConfig dest)
        {
            using (new EditorGUI.DisabledScope(source is SimulatedPlayerDeploymentConfig))
            {
                dest.Name = EditorGUILayout.TextField("Deployment Name", source.Name);
            }

            dest.SnapshotPath = EditorGUILayout.TextField("Snapshot Path", source.SnapshotPath);
            dest.LaunchJson = EditorGUILayout.TextField("Launch Config", source.LaunchJson);
            dest.Region = (DeploymentRegionCode) EditorGUILayout.EnumPopup("Region", source.Region);


            EditorGUILayout.LabelField("Tags");

            using (new EditorGUI.IndentLevelScope())
            {
                for (int i = 0; i < dest.Tags.Count; i++)
                {
                    dest.Tags[i] = EditorGUILayout.TextField($"Tag #{i + 1}", dest.Tags[i]);
                }

                dest.Tags.Add(EditorGUILayout.TextField($"Tag #{dest.Tags.Count + 1}", ""));

                dest.Tags = dest.Tags.Where(tag => !string.IsNullOrEmpty(tag)).ToList();
            }
        }

        private (bool, SimulatedPlayerDeploymentConfig) DrawSimulatedConfig(int index, SimulatedPlayerDeploymentConfig config)
        {
            var copy = config.DeepCopy();
            var foldoutState = GetStateObject<bool>(config.Name.GetHashCode());

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    foldoutState = EditorGUILayout.Foldout(foldoutState, new GUIContent($"Simulated Player Deployment {index + 1}"), true);

                    GUILayout.FlexibleSpace();

                    using (new EditorGUIUtility.IconSizeScope(SmallIconSize))
                    {
                        var buttonContent = new GUIContent(string.Empty, "Remove simulated player deployment");
                        buttonContent.image = EditorGUIUtility.IconContent("Toolbar Minus").image;

                        if (GUILayout.Button(buttonContent, EditorStyles.miniButton))
                        {
                            return (true, null);
                        }
                    }
                }

                using (new EditorGUI.IndentLevelScope())
                using (new EditorGUILayout.VerticalScope())
                {
                    if (foldoutState)
                    {
                        RenderBaseDeploymentConfig(config, copy);
                    }
                }

                if (check.changed)
                {
                    SetStateObject(copy.Name.GetHashCode(), foldoutState);
                }
            }

            return (false, copy);
        }

        private void UpdateSimulatedDeploymentNames(DeploymentConfig config)
        {
            for (var i = 0; i < config.SimulatedPlayerDeploymentConfig.Count; i++)
            {
                var previousFoldoutState =
                    GetStateObject<bool>(config.SimulatedPlayerDeploymentConfig[i].Name.GetHashCode());

                config.SimulatedPlayerDeploymentConfig[i].Name = $"{config.Deployment.Name}_sim{i + 1}";
                config.SimulatedPlayerDeploymentConfig[i].TargetDeploymentName = config.Deployment.Name;

                SetStateObject(config.SimulatedPlayerDeploymentConfig[i].Name.GetHashCode(), previousFoldoutState);
            }
        }

        private void DrawHorizontalLine(int height)
        {
            var rect = EditorGUILayout.GetControlRect(false, height, EditorStyles.foldout);
            using (new Handles.DrawingScope(new Color(0.3f, 0.3f, 0.3f, 1)))
            {
                Handles.DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax));
            }

            GUILayout.Space(rect.height);
        }

        private T GetStateObject<T>(int hash) where T : new()
        {
            if (!localState.TryGetValue(hash, out var value))
            {
                value = new T();
                localState.Add(hash, value);
            }

            return (T) value;
        }

        private void SetStateObject<T>(int hash, T obj)
        {
            localState[hash] = obj;
        }

        private string GetProjectName()
        {
            var spatialJsonFile = Path.Combine(Tools.Common.SpatialProjectRootDir, "spatialos.json");

            projectName = (string) Json.Deserialize(File.ReadAllText(spatialJsonFile))["name"];

            return projectName;
        }

        private string[] GetSnapshots()
        {
            return Directory.GetFiles(Tools.Common.SpatialProjectRootDir, "*.snapshot", SearchOption.AllDirectories);
        }
    }
}
