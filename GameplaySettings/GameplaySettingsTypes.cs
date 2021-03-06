﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using CustomUI.Utilities;
using CustomUI.Settings;
using CustomUI.BeatSaber;
using UnityEngine.UI;

namespace CustomUI.GameplaySettings
{
    public enum GameplaySettingsPanels
    {
        ModifiersRight,
        ModifiersLeft,
        PlayerSettingsRight,
        PlayerSettingsLeft
    };

    public abstract class GameOption
    {
        public GameObject gameObject;
        public string optionName;
        public Sprite optionIcon;
        public string hintText;
        public bool initialized;
        public GameObject separator;
        protected string panelName;
        protected string pageName;
        protected List<string> conflicts = new List<string>();
        public abstract void Instantiate();
        public void AddConflict(string modifierName)
        {
            if (modifierName == optionName) return;
            conflicts.Add(modifierName);
        }

        public static void GetPanelNames(GameplaySettingsPanels panel, ref string pageName, ref string panelName)
        {
            switch (panel)
            {
                case GameplaySettingsPanels.ModifiersRight:
                    pageName = "GameplayModifiers";
                    panelName = "RightColumn";
                    break;
                case GameplaySettingsPanels.ModifiersLeft:
                    pageName = "GameplayModifiers";
                    panelName = "LeftColumn";
                    break;
                case GameplaySettingsPanels.PlayerSettingsRight:
                    pageName = "PlayerSettings";
                    panelName = "RightPanel";
                    break;
                case GameplaySettingsPanels.PlayerSettingsLeft:
                    pageName = "PlayerSettings";
                    panelName = "LeftPanel";
                    break;
            }
        }

        public static void GetOptionTransforms(GameplaySettingsPanels panel, RectTransform container, ref Transform option1, ref Transform option2, ref Transform option3, ref Transform option4)
        {
            switch (panel)
            {
                case GameplaySettingsPanels.ModifiersRight:
                    option1 = container.Find("NoFail");
                    option2 = container.Find("NoObstacles");
                    option3 = container.Find("NoBombs");
                    option4 = container.Find("SlowerSong");
                    break;
                case GameplaySettingsPanels.ModifiersLeft:
                    option1 = container.Find("InstaFail");
                    option2 = container.Find("BatteryEnergy");
                    option3 = container.Find("DisappearingArrows");
                    option4 = container.Find("FasterSong");
                    break;
                case GameplaySettingsPanels.PlayerSettingsRight:
                    option1 = container.Find("NoTextsAndHUDs");
                    option2 = container.Find("AdvancedHUD");
                    option3 = container.Find("SoundFX");
                    option4 = container.Find("ReduceDebris");
                    break;
                case GameplaySettingsPanels.PlayerSettingsLeft:
                    option1 = container.Find("LeftHanded");
                    option2 = container.Find("SwapColors");
                    option3 = container.Find("StaticLights");
                    option4 = container.Find("PlayerHeight");
                    break;
            }
        }
    }

    public class SubmenuOption : ToggleOption
    {
        public SubmenuOption(GameplaySettingsPanels panel, string optionName, string hintText, Sprite optionIcon) : base(panel, optionName, hintText, optionIcon, 0f)
        {
            this.optionName = optionName;
            this.hintText = hintText;
            this.optionIcon = optionIcon;
            GetPanelNames(panel, ref pageName, ref panelName);
        }

        public override void Instantiate()
        {
            base.Instantiate();

            var currentToggle = gameObject.GetComponent<GameplayModifierToggle>();
            if (currentToggle != null)
            {
                GameObject.Destroy(currentToggle.transform.Find("BG").gameObject);
            }
        }
    }

    public class ToggleOption : GameOption
    {
        public event Action<bool> OnToggle;
        public bool GetValue = false;

        public float multiplier;

        public ToggleOption(GameplaySettingsPanels panel, string optionName, string hintText, Sprite optionIcon, float multiplier)
        {
            this.optionName = optionName;
            this.hintText = hintText;
            this.optionIcon = optionIcon;
            this.multiplier = multiplier;
            GetPanelNames(panel, ref pageName, ref panelName);
        }

        public override void Instantiate()
        {
            //We have to find our own target
            //TODO: Clean up time complexity issue. This is called for each new option
            SoloFreePlayFlowCoordinator sfpfc = Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().First();
            GameplaySetupViewController gsvc = sfpfc.GetField<GameplaySetupViewController>("_gameplaySetupViewController");
            RectTransform container = (RectTransform)gsvc.transform.Find(pageName).Find(panelName);

            gameObject = UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<GameplayModifierToggle>().Where(g => g.transform.Find("BG"))?.Last().gameObject, container);
            gameObject.name = optionName;
            gameObject.layer = container.gameObject.layer;
            gameObject.transform.SetParent(container);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localScale = Vector3.one;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.SetActive(false);

            foreach (Transform t in container)
            {
                if (t.name.StartsWith("Separator"))
                {
                    separator = UnityEngine.Object.Instantiate(t.gameObject, container);
                    separator.name = "ExtraSeparator";
                    separator.SetActive(false);
                    break;
                }
            }

            string ConflictText = "\r\n\r\n<size=60%><color=#ff0000ff><b>Conflicts </b></color>";
            var currentToggle = gameObject.GetComponent<GameplayModifierToggle>();
            if (currentToggle != null)
            {
                currentToggle.toggle.isOn = GetValue;
                currentToggle.toggle.onValueChanged.RemoveAllListeners();
                currentToggle.toggle.onValueChanged.AddListener((bool e) => OnToggle?.Invoke(e));
                currentToggle.name = optionName.Replace(" ", "");

                GameplayModifierToggle[] gameplayModifierToggles = Resources.FindObjectsOfTypeAll<GameplayModifierToggle>();

                if (conflicts.Count > 0)
                {
                    hintText += ConflictText;
                    foreach (string conflict in conflicts)
                    {
                        var conflictingModifier = gameplayModifierToggles.Where(t => t?.gameplayModifier?.modifierName == conflict).FirstOrDefault();
                        if (conflictingModifier)
                        {
                            if (!hintText.Contains(ConflictText))
                                hintText += ConflictText;

                            hintText += Char.ConvertFromUtf32((char)0xE069) + conflict + Char.ConvertFromUtf32((char)0xE069);
                        }
                    }
                }

                GameplayModifierParamsSO _gameplayModifier = new GameplayModifierParamsSO();
                _gameplayModifier.SetPrivateField("_modifierName", optionName);
                _gameplayModifier.SetPrivateField("_hintText", hintText);
                _gameplayModifier.SetPrivateField("_multiplier", multiplier);
                _gameplayModifier.SetPrivateField("_icon", optionIcon == null ? UIUtilities.BlankSprite : optionIcon);
                currentToggle.SetPrivateField("_gameplayModifier", _gameplayModifier);

                string currentDisplayName = Char.ConvertFromUtf32((char)0xE069) + optionName + Char.ConvertFromUtf32((char)0xE069);
                foreach (string conflictingModifierName in conflicts)
                {
                    GameplayModifierToggle conflictToggle = gameplayModifierToggles.Where(t => t?.gameplayModifier?.modifierName == conflictingModifierName).FirstOrDefault();
                    if (conflictToggle)
                    {
                        if (!conflictToggle.gameplayModifier.hintText.Contains(ConflictText))
                            conflictToggle.gameplayModifier.SetPrivateField("_hintText", conflictToggle.gameplayModifier.hintText + ConflictText);

                        if (!conflictToggle.gameplayModifier.hintText.Contains(currentDisplayName))
                            conflictToggle.gameplayModifier.SetPrivateField("_hintText", conflictToggle.gameplayModifier.hintText + currentDisplayName);

                        conflictToggle.toggle.onValueChanged.AddListener((e) => { if (e) currentToggle.toggle.isOn = false; });
                        currentToggle.toggle.onValueChanged.AddListener((e) => { if (e) conflictToggle.toggle.isOn = false; });
                    }
                }

                if (hintText != String.Empty)
                {
                    HoverHint hoverHint = currentToggle.GetPrivateField<HoverHint>("_hoverHint");
                    hoverHint.text = hintText;
                    hoverHint.name = optionName;
                    HoverHintController hoverHintController = Resources.FindObjectsOfTypeAll<HoverHintController>().First();
                    hoverHint.SetPrivateField("_hoverHintController", hoverHintController);
                }
            }
            initialized = true;
        }
    }


    public class MultiSelectOption : GameOption
    {
        private Dictionary<float, string> _options = new Dictionary<float, string>();
        public Func<float> GetValue;
        public event Action<float> OnChange;

        public MultiSelectOption(GameplaySettingsPanels panel, string optionName, string hintText)
        {
            this.optionName = optionName;
            this.hintText = hintText;
            GetPanelNames(panel, ref pageName, ref panelName);
        }

        public override void Instantiate()
        {
            try
            {
                //We have to find our own target
                //TODO: Clean up time complexity issue. This is called for each new option
                SoloFreePlayFlowCoordinator sfpfc = Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().First();
                GameplaySetupViewController gsvc = sfpfc.GetField<GameplaySetupViewController>("_gameplaySetupViewController");
                RectTransform container = (RectTransform)gsvc.transform.Find(pageName).Find(panelName);

                var volumeSettings = Resources.FindObjectsOfTypeAll<VolumeSettingsController>().FirstOrDefault();
                gameObject = UnityEngine.Object.Instantiate(volumeSettings.gameObject, container);
                gameObject.name = optionName;
                gameObject.GetComponentInChildren<TMP_Text>().text = optionName;

                foreach (Transform t in container)
                {
                    if (t.name.StartsWith("Separator"))
                    {
                        separator = UnityEngine.Object.Instantiate(t.gameObject, container);
                        separator.name = "ExtraSeparator";
                        separator.SetActive(false);
                        break;
                    }
                }

            //Slim down the toggle option so it fits in the space we have before the divider
            (gameObject.transform as RectTransform).sizeDelta = new Vector2(50, (gameObject.transform as RectTransform).sizeDelta.y);

                //This magical nonsense is courtesy of Taz and his SettingsUI class
                VolumeSettingsController volume = gameObject.GetComponent<VolumeSettingsController>();
                ListViewController newListSettingsController = (ListViewController)ReflectionUtil.CopyComponent(volume, typeof(ListSettingsController), typeof(ListViewController), gameObject);
                UnityEngine.Object.DestroyImmediate(volume);

                newListSettingsController.values = _options.Keys.ToList();
                newListSettingsController.SetValue = OnChange;
                newListSettingsController.GetValue = () =>
                {
                    if (GetValue != null) return GetValue.Invoke();
                    return _options.Keys.ElementAt(0);
                };
                newListSettingsController.GetTextForValue = (v) =>
                {
                    if (_options.ContainsKey(v)) return _options[v] != null ? _options[v] : v.ToString();
                    return "UNKNOWN";
                };

                //Initialize the controller, as if we had just opened the settings menu
                newListSettingsController.Init();
                var value = newListSettingsController.gameObject.transform.Find("Value");
                var valueText = value.Find("ValueText");
                TMP_Text valueTextObject = valueText.GetComponent<TMP_Text>();
                valueTextObject.lineSpacing = -50;
                valueTextObject.alignment = TextAlignmentOptions.CenterGeoAligned;

                var nameText = newListSettingsController.gameObject.transform.Find("NameText");
                nameText.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                value.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                if (hintText != String.Empty)
                    BeatSaberUI.AddHintText(nameText as RectTransform, hintText);

                var dec = value.Find("DecButton");
                dec.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                var inc = value.Find("IncButton");
                inc.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                value.localPosition -= new Vector3(8, 0.3f);

                gameObject.SetActive(false);
                initialized = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception when trying to instantiate list option {e.ToString()}");
            }

        }

        public void AddOption(float value)
        {
            _options.Add(value, Convert.ToString(value));
        }

        public void AddOption(float value, string option)
        {
            _options.Add(value, option);
        }
    }

}
