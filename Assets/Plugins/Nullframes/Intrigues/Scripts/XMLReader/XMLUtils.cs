using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Nullframes.Intrigues.Graph;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEngine;

namespace Nullframes.Intrigues.XML
{
    public static class XMLUtils
    {
        private static string xmlRawFile;
        private static readonly string path = $"{Application.dataPath}/ILocalisation.xml";

        private static XmlDocument xmlDocument;

        public static void Init(IEDatabase ieDatabase) {
            if (!File.Exists(path)) return;

            xmlRawFile = File.ReadAllText(path);
            
            xmlDocument = new XmlDocument();
            xmlDocument.Load(new StringReader(xmlRawFile));
#if UNITY_EDITOR
            foreach (var key in GetLanguageKeys())
            {
                if (ieDatabase.localisationTexts.ContainsKey(key)) continue;
                if (ieDatabase.localisationTexts.Any())
                {
                    var copyTo = new SerializableDictionary<string, string>(ieDatabase.localisationTexts.First().Value);
                    ieDatabase.localisationTexts.Add(key, copyTo);
                    continue;
                }

                ieDatabase.localisationTexts.Add(key, new SerializableDictionary<string, string>());
            }
#endif
        }

        public static void CreateXML(IEDatabase ieDatabase)
        {
            var settings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "\t"
            };
            using var writer = XmlWriter.Create(path, settings);
            writer.WriteStartElement("localisation");
            foreach (var text in ieDatabase.localisationTexts)
            {
                writer.WriteStartElement("Language");
                writer.WriteAttributeString("key", text.Key);
                foreach (var value in text.Value) writer.WriteElementString(value.Key, value.Value);

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.Flush();

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public static string GetKey(IEDatabase ieDatabase, string key, string languageKey) {
            if (string.IsNullOrEmpty(xmlRawFile)) return string.Empty;

            var nodeList = xmlDocument.SelectNodes($"//Language[@key='{languageKey}']/{key}");
            return (from XmlNode node in nodeList where node.Name == key select node.InnerXml).FirstOrDefault();
        }

        private static IEnumerable<string> GetLanguageKeys()
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(xmlRawFile)) return null;
            foreach (Match key in Regex.Matches(xmlRawFile, @"(?<=<\s*Language\s*key\s*=\s*"")(.*)(?=""\s*>)"))
                list.Add(key.Value);

            return list;
        }

        private static Dictionary<string, string> GetValues(string key)
        {
            if (string.IsNullOrEmpty(xmlRawFile)) return null;

            var nodeList = xmlDocument.SelectNodes($"//Language[@key='{key}']/*");
            return nodeList?.Cast<XmlNode>()
                .ToDictionary(k => k.LocalName, v => v.InnerText);
        }

        public static void LoadXML(IEDatabase database) {
            if (!File.Exists(path)) return;
            Init(database);
            database.localisationTexts = new SerializableDictionary<string, SerializableDictionary<string, string>>();
            foreach (var lkey in GetLanguageKeys())
            {
                var values = new SerializableDictionary<string, string>();
                foreach (var value in GetValues(lkey)) values.Add(value.Key, value.Value);
                database.localisationTexts.Add(lkey, values);
            }
        }
    }
}