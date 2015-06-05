using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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
                else if (property.Value is JArray)
                {
                    ReplaceParametersInArray(parameterObject, result, property.Value as JArray);
                }
                else if (property.Value is JToken)
                {
                    source[property.Name] = ManageParametersInJToken(parameterObject, result, property.Value);
                }
            }
            return result;
        }

        internal static String ReplaceParametersInString(String source, JObject parameterObject)
        {
            ReplaceResult result = new ReplaceResult();
            var newValue = Regex.Replace(
                    source,
                    @"(?<!%)%(?!%)(?<match>.+?)(?<!%)%(?!%)",
                    new MatchEvaluator(m =>
                    {
                        var parameterName = m.Groups["match"].Value.Trim('{', '}');
                        var paramValue = GetParameterValue(parameterName, parameterObject);
                        if (paramValue == null)
                        {
                            result.MissingParams.Add(parameterName);
                            return "%" + parameterName + "%";
                        }
                        result.HasReplaced = true;
                        return paramValue;
                    }));
            if (result.MissingParams.Count > 0)
            {
                throw new ConfigurationErrorsException("Missing parameters: " +
                   result.MissingParams.Aggregate((s1, s2) => s1 + ", " + s2));
            }
            return newValue;
        }

        private static void ReplaceParametersInArray(
            JObject parameterObject, 
            ReplaceResult result, 
            JArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                var element = array[i];

                if (element is JObject)
                {
                    var replaceReturn = ReplaceParameters((JObject)element, parameterObject);
                    result.Merge(replaceReturn);
                }
                else if (element is JArray)
                {
                    ReplaceParametersInArray(parameterObject, result, element as JArray);
                }
                else if (element is JToken)
                {
                    array[i] = ManageParametersInJToken(parameterObject, result, element);
                }
            }
        }

        private static JToken ManageParametersInJToken(
            JObject parameterObject, 
            ReplaceResult result, 
            JToken token)
        {
            String value = token.ToString();
            if (Regex.IsMatch(value, "(?<!%)%(?!%).+?(?<!%)%(?!%)"))
            {
                var newValue = Regex.Replace(
                    value,
                    @"(?<!%)%(?!%)(?<match>.+?)(?<!%)%(?!%)",
                    new MatchEvaluator(m =>
                    {
                        var parameterName = m.Groups["match"].Value.Trim('{', '}');
                        var paramValue = GetParameterValue(parameterName, parameterObject);
                        if (paramValue == null)
                        {
                            result.MissingParams.Add(parameterName);
                            return "%" + parameterName + "%";
                        }
                        result.HasReplaced = true;
                        return paramValue;
                    }));
                if (value.StartsWith("%{") && value.EndsWith("}%"))
                {
                    return (JToken)JsonConvert.DeserializeObject(newValue);
                }
                else
                {
                    return newValue;
                }
            }
            return token;
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