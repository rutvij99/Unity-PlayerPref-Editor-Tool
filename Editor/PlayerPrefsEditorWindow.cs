/*
*  Author: Rutvij
*  Created On: 25-01-2023 00:00:01
*/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using Tools.Mac;
using UnityEngine.Profiling;
using TMPro;

namespace PlayerPrefsEditor
{
    
    public class PlayerPrefsEditorWindow : EditorWindow
    {
        private enum ValueType { String, Float, Integer }

        private class PlayerPrefData
        {
            public string key;
            public object value = new object();

            public ValueType Type;
            public bool hasChanged = false;

            private string oldKey;

            public string Key { set { if (value != key) { key = value; hasChanged = true; } } }
            public object Value { set { if (!value.Equals(this.value)) { this.value = value; hasChanged = true; } } }

            public PlayerPrefData()
            {

            }

            public PlayerPrefData(string key)
            {
                this.key = key;
                oldKey = key;

                GetValue();
            }

            public PlayerPrefData(string key, string value)
            {
                this.key = key;
                this.value = value;
                this.Type = ValueType.String;
            }

            public PlayerPrefData(string key, float value)
            {
                this.key = key;
                this.value = value;
                this.Type = ValueType.Float;
            }

            public PlayerPrefData(string key, int value)
            {
                this.key = key;
                this.value = value;
                this.Type = ValueType.Integer;
            }

            public void SaveChanges()
            {
                //Debug.Log(Type.ToString() + " saving::"+key);
                //Debug.Log(" old key::"+key);
                switch (Type)
                {
                    case ValueType.String:
                        PlayerPrefs.SetString(key, (string)value);
                        break;
                    case ValueType.Float:
                        PlayerPrefs.SetFloat(key, (float)value);
                        break;
                    case ValueType.Integer:
                        PlayerPrefs.SetInt(key, (int)value);
                        break;
                    default:
                        break;
                }

                if (oldKey != key)
                {
                    PlayerPrefs.DeleteKey(oldKey);
                    oldKey = key;
                }

                hasChanged = false;
                PlayerPrefs.Save();
            }

            public void Revert()
            {
                GetValue();
            }

            public void GetValue()
            {
                if (PlayerPrefs.GetString(key, "i-source") != "i-source")
                {
                    Type = ValueType.String;
                    value = PlayerPrefs.GetString(key);
                }
                else if (PlayerPrefs.GetInt(key, int.MinValue) != int.MinValue)
                {
                    Type = ValueType.Integer;
                    value = PlayerPrefs.GetInt(key);
                }
                else if (PlayerPrefs.GetFloat(key, float.MinValue) != float.MinValue)
                {
                    Type = ValueType.Float;
                    value = PlayerPrefs.GetFloat(key);
                }
                hasChanged = false;
            }
        }

        private bool isFirstIterationDone = false;

        private List<PlayerPrefData> prefList;
        private static readonly string[] UNITY_SPECIFIC_PREFS = new string[]
        {
            "UnityGraphicsQuality",
            "unity.cloud_userid",
            "unity.player_session_background_time",
            "unity.player_session_elapsed_time",
            "unity.player_sessionid",
            "unity.player_session_count"
        };
        private Vector2 scrollPos;

        private bool addPrefToggle;
        private ValueType selectedType;
        private string newKey = "";
        private string newString = "";
        private int newInt = 0;
        private float newFloat = 0.0f;

        private static PlayerPrefsEditorWindow window;

        [MenuItem("Tools/PlayerPref Editor")]
        public static void ShowWindow() 
        {
            window = GetWindow<PlayerPrefsEditorWindow>("PlayerPref Editor");
            window.maxSize = new Vector2(700, 400);
            window.minSize = new Vector2(700, 400);

            window.Show();
        }

        private void OnGUI()
        {
            if (!isFirstIterationDone || prefList == null)
            {
                GetPrefList();
                isFirstIterationDone = true;
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            {
                Refresh();
            };
            if (GUILayout.Button("Force Save Prefs", GUILayout.Width(150)))
            {
                PlayerPrefs.Save();
            };
            if (GUILayout.Button("Delete All", GUILayout.Width(100)))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
            };

            if (GUILayout.Button("Add New", GUILayout.Width(100)))
            {
                addPrefToggle = !addPrefToggle;
            };
            GUILayout.EndHorizontal();

            if (addPrefToggle) AddNewPrefView();
            PrefListDisplay();


        }

        private void AddNewPrefView()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(35);
            //GUILayout.Label("Pref key", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label("New Pref key", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label("Type", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("Value", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(35);
            newKey = EditorGUILayout.TextField(newKey, GUILayout.Width(200));
            EditorGUI.BeginChangeCheck();
            selectedType = (ValueType)EditorGUILayout.EnumPopup(selectedType, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                newString = "";
                newFloat = 0f;
                newInt = 0;
            }
            switch (selectedType)
            {
                default:
                case ValueType.String:
                    newString = EditorGUILayout.TextField(newString, GUILayout.Width(200));
                    break;
                case ValueType.Float:
                    newFloat = EditorGUILayout.FloatField(newFloat, GUILayout.Width(200));
                    break;
                case ValueType.Integer:
                    newInt = EditorGUILayout.IntField(newInt, GUILayout.Width(200));
                    break;
            }
            if (GUILayout.Button("Add Pref", GUILayout.Width(100)))
            {
                if (string.IsNullOrEmpty(newKey))
                {
                    switch (selectedType)
                    {
                        default:
                        case ValueType.String:
                            PlayerPrefs.SetString(newKey, newString);
                            break;
                        case ValueType.Float:
                            PlayerPrefs.SetFloat(newKey, newFloat);
                            break;
                        case ValueType.Integer:
                            PlayerPrefs.SetInt(newKey, newInt);
                            break;
                    }
                    PlayerPrefs.Save();
                    newKey = "";
                    newFloat = 0;
                    newInt = 0;
                    newString = "";
                    Refresh();
                }
                else
                {
                    Debug.Log("Can't Save Pref with empty key");
                }
            };
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        private void PrefListDisplay()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(35);
            //GUILayout.Label("Pref key", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label("Pref key", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label("Pref Value", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label("Type", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("Options", EditorStyles.boldLabel, GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            for (int i = 0; i < prefList.Count; i++)
            {
                if (prefList[i] != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label((i + 1).ToString(), GUILayout.Width(20));
                    prefList[i].Key = GUILayout.TextField(prefList[i].key, GUILayout.Width(200));
                    switch (prefList[i].Type)
                    {
                        case ValueType.String:
                            prefList[i].Value = EditorGUILayout.TextField((string)prefList[i].value, GUILayout.Width(200));
                            break;
                        case ValueType.Float:
                            prefList[i].Value = EditorGUILayout.FloatField((float)prefList[i].value, GUILayout.Width(200));
                            break;
                        case ValueType.Integer:
                            prefList[i].Value = EditorGUILayout.IntField((int)prefList[i].value, GUILayout.Width(200));
                            break;
                    }
                    GUILayout.Label(prefList[i].Type.ToString(), GUILayout.Width(100));
                    if (GUILayout.Button("Update", GUILayout.Width(70)))
                    {
                        prefList[i].SaveChanges();
                        Refresh();
                    }
                    if (GUILayout.Button("Delete", GUILayout.Width(50)))
                    {
                        PlayerPrefs.DeleteKey(prefList[i].key);
                        PlayerPrefs.Save();
                        Refresh();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            //EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"Total {prefList.Count} Prefs found");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        public void Refresh()
        {
            GetPrefList();
            Repaint();
        }


        private void GetPrefList()
        {
            string[] keys = null;
            if (Application.platform == RuntimePlatform.WindowsEditor)
                keys = GetWindowsKeys();
            else if (Application.platform == RuntimePlatform.OSXEditor)
                keys = GetMacKeys();
            else if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                Debug.Log("Linux editor prefs not supported yet");
                return;
                keys = GetLinuxKeys();
            }

            List<string> keysList = keys.ToList();

            foreach (var key in keys)
            {
                for (int i = 0; i < UNITY_SPECIFIC_PREFS.Length; i++)
                {
                    if (key == UNITY_SPECIFIC_PREFS[i])
                        keysList.Remove(UNITY_SPECIFIC_PREFS[i]);
                }
            }

            prefList = new List<PlayerPrefData>();
            for (int i = 0; i < keysList.Count; i++)
            {
                prefList.Add(new PlayerPrefData(keysList[i]));
            }
        }

        private string[] GetWindowsKeys()
        {
            Microsoft.Win32.RegistryKey registryKey;

            // From Unity docs: On Windows, PlayerPrefs are stored in the registry under HKCU\Software\[company name]\[product name] key,
            // where company and product names are the names set up in Project Settings.
            registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Unity\\UnityEditor\\" + PlayerSettings.companyName + "\\" + PlayerSettings.productName);
            string[] keys = registryKey.GetValueNames();
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = keys[i].Substring(0, keys[i].LastIndexOf("_"));
            }

            return keys;
        }

        private string[] GetMacKeys()
        {
            string[] keys = new string[0];

            string plistPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Library/Preferences/unity." + PlayerSettings.companyName + "." + PlayerSettings.productName + ".plist";
            if (File.Exists(plistPath))
            {
                FileInfo file = new FileInfo(plistPath);
                Dictionary<string, object> plist = (Dictionary<string, object>)Plist.readPlist(file.FullName);

                keys = plist.Keys.ToArray();
            }

            return keys;
        }

        private string[] GetLinuxKeys()
        {
            return null;
        }
    }
}
