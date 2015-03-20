using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Jarvis.ConfigurationService.Host.Support
{
    static internal class ParameterManager
    {
        internal class ReplaceResult
        {
            public Boolean HasReplaced { get; set; }

            public HashSet<String> MissingParams { get; set; }

            public ReplaceResult()
            {
                MissingParams = new HashSet<string>();
            }
            internal void Merge(ReplaceResult result)
            {
                HasReplaced = HasReplaced || result.HasReplaced;
                foreach (var missingParam in result.MissingParams)
                {
                    MissingParams.Add(missingParam);
                }
            }
        }

        /// <summary>
        /// it is used to retrieve parameters settings from config file.
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="parameterObject"></param>
        /// <returns></returns>
        internal static String GetParameterValue(string settingName, JObject parameterObject)
        {
            var path = settingName.Split('.');
            JObject current = parameterObject;
            for (int i = 0; i < path.Length - 1; i++)
            {
                if (current[path[i]] == null) return null;
                current = (JObject)current[path[i]];
            }
            if (current[path.Last()] == null)
                return null;
            return current[path.Last()].ToString();
        }

        internal static ReplaceResult ReplaceParameters(JObject source, JObject parameterObject)
        {
            ReplaceResult result = new ReplaceResult();
            foreach (var property in source.Properties())
            {
                if (property.Value is JObject)
                {
                    var replaceReturn = ReplaceParameters((JObject)property.Value, parameterObject);
                    result.Merge(replaceReturn);
                }
                else if (property.Value is JToken)
                {
                    String value = property.Value.ToString();
                    if (Regex.IsMatch(property.Value.ToString(), "(?<!%)%(?!%).+?(?<!%)%(?!%)"))
                    {
                        value = Regex.Replace(
                            value,
                            @"(?<!%)%(?!%)(?<match>.+?)(?<!%)%(?!%)",
                            new MatchEvaluator(m =>
                            {
                                var parameterName = m.Groups["match"].Value;
                                var paramValue = GetParameterValue(parameterName, parameterObject);
                                if (String.IsNullOrEmpty(paramValue))
                                {
                                    result.MissingParams.Add(parameterName);
                                    return "%" + parameterName + "%";
                                }
                                result.HasReplaced = true;
                                return paramValue;
                            }));
                    }
                    source[property.Name] = value;
                }
            }
            return result;
        }



        internal static void UnescapePercentage(JObject source)
        {
            foreach (var property in source.Properties())
            {
                if (property.Value is JObject)
                {
                    UnescapePercentage((JObject)property.Value);
                }
                else if (property.Value is JToken && property.Value.ToString().Contains("%%"))
                {
                    source[property.Name] = property.Value.ToString().Replace("%%", "%");
                }
            }
        }
    }
}