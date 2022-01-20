﻿using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;


namespace ExtremeRoles.Helper
{
    public class Translation
    {
        private static int defaultLanguage = (int)SupportedLangs.English;
        private static Dictionary<string, Dictionary<int, string>> stringData = new Dictionary<string, Dictionary<int, string>>();

        private const string dataPath = "ExtremeRoles.Resources.LangData.stringData.json";

        public static void Load()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(dataPath);
            var byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            string json = System.Text.Encoding.UTF8.GetString(byteArray);

            stringData.Clear();
            JObject parsed = JObject.Parse(json);

            for (int i = 0; i < parsed.Count; i++)
            {
                JProperty token = parsed.ChildrenTokens[i].TryCast<JProperty>();
                if (token == null) { continue; }

                string stringName = token.Name;
                var val = token.Value.TryCast<JObject>();

                if (token.HasValues)
                {
                    var strings = new Dictionary<int, string>();

                    for (int j = 0; j < (int)SupportedLangs.Irish + 1; j++)
                    {
                        string key = j.ToString();
                        var text = val[key]?.TryCast<JValue>().Value.ToString();

                        if (text != null && text.Length > 0)
                        {
                            strings.Add(j,text);
                        }
                    }

                    stringData.Add(stringName, strings);
                }
            }
        }

        public static string GetString(string key)
        {

            if (stringData.Count == 0)
            {
                return key;
            }

            string keyClean = Regex.Replace(key, "<.*?>", "");
            keyClean = Regex.Replace(keyClean, "^-\\s*", "");
            keyClean = keyClean.Trim();

            if (!stringData.ContainsKey(keyClean))
            {
                return key;
            }

            var data = stringData[keyClean];
            int lang = (int)SaveManager.LastLanguage;

            if (data.ContainsKey(lang))
            {
                return key.Replace(keyClean, data[lang]);
            }
            else if (data.ContainsKey(defaultLanguage))
            {
                return key.Replace(keyClean, data[defaultLanguage]);
            }

            return key;
        }

    }
}
