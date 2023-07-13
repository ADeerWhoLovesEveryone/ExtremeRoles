﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using HarmonyLib;

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using AmongUs.Data;

using ExtremeRoles.Module.CustomOption;
using ExtremeRoles.Performance;
using ExtremeRoles.Extension.UnityEvents;

using static UnityEngine.UI.Button;
using Object = UnityEngine.Object;


namespace ExtremeRoles.Patches.Option;


[HarmonyPatch]
[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class OptionsMenuBehaviourStartPatch
{

    private static ClientOption clientOpt => ClientOption.Instance;

    private static SelectionBehaviour[] modOption = {
        new SelectionBehaviour(
            "ghostsSeeTasksButton",
            () =>
            {
                bool newValue = !clientOpt.GhostsSeeTask.Value;
                clientOpt.GhostsSeeTask.Value = newValue;
                return newValue;
            }, clientOpt.GhostsSeeTask.Value),
        new SelectionBehaviour(
            "ghostsSeeVotesButton",
            () =>
            {
                bool newValue = !clientOpt.GhostsSeeVote.Value;
                clientOpt.GhostsSeeVote.Value = newValue;
                return newValue;
            }, clientOpt.GhostsSeeVote.Value),
        new SelectionBehaviour(
            "ghostsSeeRolesButton",
            () =>
            {
                bool newValue = !clientOpt.GhostsSeeRole.Value;
                clientOpt.GhostsSeeRole.Value = newValue;
                return newValue;
            }, clientOpt.GhostsSeeRole.Value),
        new SelectionBehaviour(
            "showRoleSummaryButton",
            () =>
            {
                bool newValue = !clientOpt.ShowRoleSummary.Value;
                clientOpt.ShowRoleSummary.Value = newValue;
                return newValue;
            }, clientOpt.ShowRoleSummary.Value),
        new SelectionBehaviour(
            "hideNamePlateButton",
            () =>
            {
                bool newValue = !clientOpt.HideNamePlate.Value;
                clientOpt.HideNamePlate.Value = newValue;
                Meeting.NamePlateHelper.NameplateChange = true;
                return newValue;
            }, clientOpt.HideNamePlate.Value)
    };

    private static GameObject popUp;
    private static TextMeshPro moreOptionText;
    private static TextMeshPro creditText;

    private static ToggleButtonBehaviour moreOptionButton;
    private static List<ToggleButtonBehaviour> modOptionButton;

    private static ToggleButtonBehaviour importButton;
    private static ToggleButtonBehaviour exportButton;

    private static ToggleButtonBehaviour buttonPrefab;
    public static void Postfix(OptionsMenuBehaviour __instance)
    {
        if (!__instance.CensorChatButton) { return; }

        if (!moreOptionText && Module.Prefab.Text != null)
        {
            moreOptionText = Object.Instantiate(
                Module.Prefab.Text);
            Object.DontDestroyOnLoad(moreOptionText);
            moreOptionText.gameObject.SetActive(false);
        }

        if (!popUp)
        {
            createCustomMenu(__instance);
        }

        if (!buttonPrefab)
        {
            buttonPrefab = Object.Instantiate(__instance.CensorChatButton);
            Object.DontDestroyOnLoad(buttonPrefab);
            buttonPrefab.name = "censorChatPrefab";
            buttonPrefab.gameObject.SetActive(false);
        }

        setUpOptions();
        initializeMoreButton(__instance);
        setLeaveGameButtonPostion();
    }

    public static void UpdateMenuTranslation()
    {
        if (moreOptionText != null)
        {
            moreOptionText.text = Helper.Translation.GetString("moreOptionText");
        }
        if (moreOptionButton != null)
        {
            moreOptionButton.Text.text = Helper.Translation.GetString("modOptionText");
        }
        if (modOptionButton != null)
        {
            for (int i = 0; i < modOption.Length; i++)
            {
                if (i >= modOptionButton.Count || modOptionButton[i] == null) { break; }
                modOptionButton[i].Text.text = Helper.Translation.GetString(modOption[i].Title);
            }
        }
        if (importButton != null)
        {
            importButton.Text.text = Helper.Translation.GetString("csvImport");
        }
        if (exportButton != null)
        {
            exportButton.Text.text = Helper.Translation.GetString("csvExport");
        }
    }

    private static void createCustomMenu(OptionsMenuBehaviour prefab)
    {
        popUp = Object.Instantiate(prefab.gameObject);
        popUp.name = "modMenu";
        Object.DontDestroyOnLoad(popUp);
        var transform = popUp.transform;
        var pos = transform.localPosition;
        pos.z = -810f;
        transform.localPosition = pos;

        Object.Destroy(popUp.GetComponent<OptionsMenuBehaviour>());
        foreach (var gObj in popUp.getAllChilds())
        {
            if (gObj.name != "Background" && gObj.name != "CloseButton")
            {
                Object.Destroy(gObj);
            }
        }

        popUp.SetActive(false);
    }

    private static void initializeMoreButton(OptionsMenuBehaviour __instance)
    {
        moreOptionButton = Object.Instantiate(
            buttonPrefab, __instance.CensorChatButton.transform.parent);
        moreOptionButton.transform.localPosition =
            __instance.CensorChatButton.transform.localPosition +
            Vector3.down * 1.0f;
        moreOptionButton.name = "modMenuButton";

        moreOptionButton.gameObject.SetActive(true);
        moreOptionButton.Text.text = Helper.Translation.GetString("modOptionText");
        var moreOptionsButton = moreOptionButton.GetComponent<PassiveButton>();
        moreOptionsButton.OnClick.RemoveAllPersistentAndListeners();
        moreOptionsButton.OnClick.AddListener(() =>
        {
            if (!popUp) { return; }

            if (DestroyableSingleton<HudManager>.InstanceExists &&
                __instance.transform.parent &&
                __instance.transform.parent == FastDestroyableSingleton<HudManager>.Instance.transform)
            {
                popUp.transform.SetParent(FastDestroyableSingleton<HudManager>.Instance.transform);
                popUp.transform.localPosition = new Vector3(0, 0, -800f);
            }
            else
            {
                popUp.transform.SetParent(null);
                Object.DontDestroyOnLoad(popUp);
            }

            checkSetTitle();
            refreshOpen();
        });
    }

    private static void refreshOpen()
    {
        popUp.SetActive(false);
        popUp.SetActive(true);
        setUpOptions();
    }

    private static void checkSetTitle()
    {
        if (!popUp || !moreOptionText) { return; }

        var title = moreOptionText = Object.Instantiate(moreOptionText, popUp.transform);
        title.GetComponent<RectTransform>().localPosition = Vector3.up * 2.3f;
        title.gameObject.SetActive(true);
        title.fontSize = title.fontSizeMin = title.fontSizeMax = 3.25f;
        title.text = Helper.Translation.GetString("moreOptionText");
        title.transform.localPosition += new Vector3(0.0f, 0.25f, 0f);
        title.name = "titleText";
    }

    private static void setUpOptions()
    {
        if (popUp.transform.GetComponentInChildren<ToggleButtonBehaviour>()) { return; }

        createModOption();
        createOptionInExButton();

        creditText = Object.Instantiate(
            Module.Prefab.Text, popUp.transform);
        creditText.name = "credit";

        StringBuilder showTextBuilder = new StringBuilder();

        showTextBuilder
            .Append("<size=175%>Extreme Roles<space=0.9em>")
            .Append(Helper.Translation.GetString("version"))
            .Append(Assembly.GetExecutingAssembly().GetName().Version)
            .AppendLine("</size>");

        showTextBuilder.AppendLine(
            $"<align=left>{Helper.Translation.GetString("developer")}yukieiji");
        showTextBuilder.AppendLine(
            $"<align=left>{Helper.Translation.GetString("debugThunk")}stou59，Tyoubi，mamePi,");
        showTextBuilder.AppendLine(
            $"<align=left>　アンハッピーセット");

        creditText.fontSize = creditText.fontSizeMin = creditText.fontSizeMax = 2.0f;
        creditText.font = Object.Instantiate(moreOptionText.font);
        creditText.GetComponent<RectTransform>().sizeDelta = new Vector2(5.0f, 5.5f);
        creditText.gameObject.SetActive(true);

        if (DataManager.Settings.Language.CurrentLanguage != SupportedLangs.Japanese)
        {
            creditText.transform.localPosition = new Vector3(0.0f, -1.895f, -.5f);
            showTextBuilder
                .Append($"<align=left>{Helper.Translation.GetString("langTranslate")}")
                .Append(Helper.Translation.GetString("translatorMember"));
        }
        else
        {
            creditText.transform.localPosition = new Vector3(0.0f, -2.0f, -.5f);
        }

        creditText.text = showTextBuilder.ToString();
    }

    private static void createModOption()
    {
        modOptionButton = new List<ToggleButtonBehaviour>();

        for (int i = 0; i < modOption.Length; i++)
        {
            var info = modOption[i];

            var button = Object.Instantiate(buttonPrefab, popUp.transform);
            button.transform.localPosition = new Vector3(
                i % 2 == 0 ? -1.17f : 1.17f,
                1.75f - i / 2 * 0.8f,
                -.5f);

            button.onState = info.DefaultValue;
            button.Background.color = button.onState ? Color.green : Palette.ImpostorRed;

            button.Text.text = Helper.Translation.GetString(info.Title);
            button.Text.fontSizeMin = button.Text.fontSizeMax = 2.2f;
            button.Text.font = Object.Instantiate(moreOptionText.font);
            button.Text.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 2);

            button.name = info.Title.Replace(" ", "") + "toggle";
            button.gameObject.SetActive(true);
            button.gameObject.transform.SetAsFirstSibling();

            var passiveButton = button.GetComponent<PassiveButton>();
            var colliderButton = button.GetComponent<BoxCollider2D>();

            colliderButton.size = new Vector2(2.2f, .7f);

            passiveButton.OnClick.RemoveAllPersistentAndListeners();
            passiveButton.OnMouseOut.RemoveAllPersistentAndListeners();
            passiveButton.OnMouseOver.RemoveAllPersistentAndListeners();

            passiveButton.OnClick.AddListener(() =>
            {
                button.onState = info.OnClick();
                button.Background.color = button.onState ? Color.green : Palette.ImpostorRed;
            });

            passiveButton.OnMouseOver.AddListener(
				() =>
				{
					button.Background.color = new Color32(34, 139, 34, byte.MaxValue);
				});
            passiveButton.OnMouseOut.AddListener(
				() =>
				{
					button.Background.color = button.onState ? Color.green : Palette.ImpostorRed;
				});

            foreach (var spr in button.gameObject.GetComponentsInChildren<SpriteRenderer>())
            {
                spr.size = new Vector2(2.2f, .7f);
            }
            modOptionButton.Add(button);
        }
    }

    private static void createOptionInExButton()
    {
        importButton = Object.Instantiate(
            buttonPrefab, popUp.transform);
        exportButton = Object.Instantiate(
            buttonPrefab, popUp.transform);

        importButton.transform.localPosition = new Vector3(
            -1.35f, -0.9f, -.5f);
        exportButton.transform.localPosition = new Vector3(
            1.35f, -0.9f, -.5f);

        importButton.Text.text = Helper.Translation.GetString("csvImport");
        importButton.Text.enableWordWrapping = false;
        importButton.Background.color = Color.green;
        importButton.Text.fontSizeMin = importButton.Text.fontSizeMax = 2.2f;

        exportButton.Text.text = Helper.Translation.GetString("csvExport");
        exportButton.Text.enableWordWrapping = false;
        exportButton.Background.color = Palette.ImpostorRed;
        exportButton.Text.fontSizeMin = exportButton.Text.fontSizeMax = 2.2f;

        var passiveImportButton = importButton.GetComponent<PassiveButton>();
        passiveImportButton.OnClick.RemoveAllPersistentAndListeners();
        passiveImportButton.OnClick.AddListener(CsvImport.Excute);

        var passiveExportButton = exportButton.GetComponent<PassiveButton>();
        passiveExportButton.OnClick.RemoveAllPersistentAndListeners();
        passiveExportButton.OnClick.AddListener(CsvExport.Excute);

        passiveImportButton.gameObject.SetActive(true);
        passiveExportButton.gameObject.SetActive(true);

        CsvImport.InfoPopup = Object.Instantiate(
            Module.Prefab.Prop, passiveImportButton.transform);
        CsvExport.InfoPopup = Object.Instantiate(
            Module.Prefab.Prop, passiveExportButton.transform);

        var pos = Module.Prefab.Prop.transform.position;
        pos.z = -2048f;
        CsvImport.InfoPopup.transform.position = pos;
        CsvExport.InfoPopup.transform.position = pos;

        CsvImport.InfoPopup.TextAreaTMP.fontSize *= 0.75f;
        CsvImport.InfoPopup.TextAreaTMP.enableAutoSizing = false;

        CsvExport.InfoPopup.TextAreaTMP.fontSize *= 0.6f;
        CsvExport.InfoPopup.TextAreaTMP.enableAutoSizing = false;
    }

    private static IEnumerable<GameObject> getAllChilds(this GameObject Go)
    {
        for (int i = 0; i < Go.transform.childCount; ++i)
        {
            yield return Go.transform.GetChild(i).gameObject;
        }
    }

    private static void setLeaveGameButtonPostion()
    {
        var leaveGameButton = GameObject.Find("LeaveGameButton");
        if (leaveGameButton == null) { return; }
        leaveGameButton.transform.localPosition += (Vector3.right * 1.3f);
    }

    private sealed class SelectionBehaviour
    {
        public string Title;
        public Func<bool> OnClick;
        public bool DefaultValue;

        public SelectionBehaviour(string title, Func<bool> onClick, bool defaultValue)
        {
            Title = title;
            OnClick = onClick;
            DefaultValue = defaultValue;
        }
    }

    private sealed class ClickBehavior
    {
        public string Title;
        public Action OnClick;

        public ClickBehavior(string title, Action onClick)
        {
            Title = title;
            OnClick = onClick;
        }
    }

    private sealed class CsvImport
    {
        public static GenericPopup InfoPopup;

        public static void Excute()
        {
            foreach (var sr in InfoPopup.gameObject.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.sortingOrder = 8;
            }
            foreach (var mr in InfoPopup.gameObject.GetComponentsInChildren<MeshRenderer>())
            {
                mr.sortingOrder = 9;
            }

            string info = Helper.Translation.GetString("importPleaseWait");
            InfoPopup.Show(info); // Show originally
            bool result = Module.CustomOptionCsvProcessor.Import();

            if (result)
            {
                info = Helper.Translation.GetString("importSuccess");
            }
            else
            {
                info = Helper.Translation.GetString("importError");
            }
            InfoPopup.StartCoroutine(
                Effects.Lerp(0.01f, new System.Action<float>((p) => { setPopupText(info); })));
        }
        private static void setPopupText(string message)
        {
            if (InfoPopup == null)
            {
                return;
            }
            if (InfoPopup.TextAreaTMP != null)
            {
                InfoPopup.TextAreaTMP.text = message;
            }
        }
    }
    private static class CsvExport
    {
        public static GenericPopup InfoPopup;

        public static void Excute()
        {
            foreach (var sr in InfoPopup.gameObject.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.sortingOrder = 8;
            }
            foreach (var mr in InfoPopup.gameObject.GetComponentsInChildren<MeshRenderer>())
            {
                mr.sortingOrder = 9;
            }


            InfoPopup.gameObject.transform.SetAsLastSibling();
            string info = Helper.Translation.GetString("exportPleaseWait");
            InfoPopup.Show(info); // Show originally
            bool result = Module.CustomOptionCsvProcessor.Export();

            if (result)
            {
                info = Helper.Translation.GetString("exportSuccess");
            }
            else
            {
                info = Helper.Translation.GetString("exportError");
            }
            InfoPopup.StartCoroutine(
                Effects.Lerp(0.01f, new System.Action<float>((p) => { setPopupText(info); })));
        }
        private static void setPopupText(string message)
        {
            if (InfoPopup == null)
            {
                return;
            }
            if (InfoPopup.TextAreaTMP != null)
            {
                InfoPopup.TextAreaTMP.text = message;
            }
        }
    }

}
