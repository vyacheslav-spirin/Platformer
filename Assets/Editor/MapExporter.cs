using System.Collections.Generic;
using Assets.Scripts.Match.Map;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Editor
{
    [UsedImplicitly]
    public sealed class MapExporter : EditorWindow
    {
        [UsedImplicitly]
        [MenuItem("PlatformerArena/Map Exporter")]
        private static void CreateWindow()
        {
            var window = GetWindow<MapExporter>(true);

            window.Show();
        }

        private MapDescription targetMapDescription;

        [UsedImplicitly]
        private void OnGUI()
        {
            targetMapDescription = (MapDescription) EditorGUILayout.ObjectField("Target Map Description", targetMapDescription, typeof(MapDescription), false);

            EditorGUILayout.Separator();

            GUI.enabled = targetMapDescription != null;

            if (GUILayout.Button("Export"))
            {
                var scene = SceneManager.GetActiveScene();

                var gameObjects = scene.GetRootGameObjects();

                var blocks = new List<MapDescription.Block>();
                var spawns = new List<Vector2>();

                foreach (var gameObject in gameObjects)
                {
                    if (gameObject.name.StartsWith("SpawnPos"))
                    {
                        spawns.Add(new Vector2(gameObject.transform.position.x, gameObject.transform.position.y));

                        continue;
                    }

                    var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);

                    if (path.StartsWith("Assets/Resources/Blocks/"))
                    {
                        path = path.Substring("Assets/Resources/Blocks/".Length);

                        var values = path.Split('.');

                        if(values.Length > 0 && int.TryParse(values[0], out var typeId))
                        {
                            blocks.Add(new MapDescription.Block
                            {
                                typeId = typeId,
                                posX = (int) gameObject.transform.position.x,
                                posY = (int)gameObject.transform.position.y
                            });
                        }
                    }
                }

                targetMapDescription.blocks = blocks.ToArray();
                targetMapDescription.spawns = spawns.ToArray();

                EditorUtility.SetDirty(targetMapDescription);
            }
        }
    }
}