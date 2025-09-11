using System.Globalization;
using System.Text.RegularExpressions;

namespace Nullframes.Intrigues.Utils
{
    public static class DialoguePattern
    {
        private const string IfGlobalpattern = @"(?<=!if\[).*?(?=\])";
        private const string IfTablepattern = @"(?<=\#if\[).*?(?=\])";
        private const string IfConspiratorpattern = @"(?<=\$if\[).*?(?=\])";
        
        private const string IfConspiratorHasPolicypattern = @"(?<=\$if\[HasPolicy).*?(?=\])";
        private const string IfTargetHasPolicypattern = @"(?<=\&if\[HasPolicy).*?(?=\])";
        private const string IfConspiratorNotHasPolicypattern = @"(?<=\$if\[\!HasPolicy).*?(?=\])";
        private const string IfTargetNotHasPolicypattern = @"(?<=\&if\[\!HasPolicy).*?(?=\])";
        
        private const string IfTargetpattern = @"(?<=\&if\[).*?(?=\])";

        private const string IfGlobalpatternStr = @"\!if\[(.*?)\]\{\""(.*?)\""\}";
        private const string IfTablepatternStr = @"\#if\[(.*?)\]\{\""(.*?)\""\}";
        private const string IfConspiratorpatternStr = @"\$if\[(.*?)\]\{\""(.*?)\""\}";
        private const string IfTargetpatternStr = @"\&if\[(.*?)\]\{\""(.*?)\""\}";

        // private const string Quoter = @"""[^""\\]*(?:\\[\s\S][^""\\]*)*""";
        private static string Namepattern => @".*?(?=\W)";
        private static string Valuepattern(string key) => $@"(?<={key}).*";

        public static string ClearChoicePatterns(this string value)
        {
            value = Regex.Replace(value, @"\&if\[.*?\]", string.Empty);
            value = Regex.Replace(value, @"\$if\[.*?\]", string.Empty);
            value = Regex.Replace(value, @"\#if\[.*?\]", string.Empty);
            value = Regex.Replace(value, @"\!if\[.*?\]", string.Empty);
            return value;
        }

        public static string ManipulateString(this string value, Scheme scheme)
        {
            globalMatch:
            var matches = Regex.Matches(value, IfGlobalpatternStr);
            foreach (Match globalMatch in matches)
            {
                if (!globalMatch.Success) continue;

                string newValue = "NULL";

                var square = Regex.Match(globalMatch.Value, @"(?<=\[).*?(?=\])");
                var curly = Regex.Match(globalMatch.Value, @"(?<=\{\"").*?(?=\""\})");
                if (curly.Success && square.Success)
                {
                    var varName = Regex.Match(square.Value, Namepattern);

                    #region GREATER-OR-EQUAL

                    var greater = Regex.Match(square.Value, Valuepattern(">>"));
                    if (varName.Success && greater.Success)
                    {
                        var nvar = IM.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(greater.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value > parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(greater.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value > parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    var greaterOrEqual = Regex.Match(square.Value, Valuepattern(">="));
                    if (varName.Success && greaterOrEqual.Success)
                    {
                        var nvar = IM.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(greaterOrEqual.Value, NumberStyles.Float,
                                            CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value >= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(greaterOrEqual.Value, NumberStyles.Integer,
                                            CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value >= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    #endregion

                    #region EQUAL-NOTEQUAL

                    var equal = Regex.Match(square.Value, Valuepattern("=="));
                    if (varName.Success && equal.Success)
                    {
                        var nvar = IM.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(equal.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !nFloat.Value.Equals(parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(equal.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = nInt.Value != parseValue ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NBool nBool:
                                {
                                    if (bool.TryParse(equal.Value, out bool parseValue))
                                    {
                                        newValue = nBool.Value != parseValue ? string.Empty : curly.Value;
                                    }
                                    else
                                    {
                                        NDebug.Log(
                                            "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                            NLogType.Error);
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    var notEqual = Regex.Match(square.Value, Valuepattern("!="));
                    if (varName.Success && notEqual.Success)
                    {
                        var nvar = IM.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(notEqual.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = nFloat.Value.Equals(parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(notEqual.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = nInt.Value == parseValue ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NBool nBool:
                                {
                                    if (bool.TryParse(equal.Value, out bool parseValue))
                                    {
                                        newValue = nBool.Value == parseValue ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    #endregion

                    #region LESSER-OR-EQUAL

                    var lesser = Regex.Match(square.Value, Valuepattern("<<"));
                    if (varName.Success && lesser.Success)
                    {
                        var nvar = IM.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(lesser.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value < parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(lesser.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value < parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    var lesserOrEqual = Regex.Match(square.Value, Valuepattern("<="));
                    if (varName.Success && lesserOrEqual.Success)
                    {
                        var nvar = IM.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(lesserOrEqual.Value, NumberStyles.Float,
                                            CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value <= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(lesserOrEqual.Value, NumberStyles.Integer,
                                            CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value <= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    #endregion
                }

                go:
                value = value.Remove(globalMatch.Index, globalMatch.Length);
                value = value.Insert(globalMatch.Index, newValue);
                value = value.Replace(@"\n", "\n");
                goto globalMatch;
            }

            tableMatch:
            matches = Regex.Matches(value, IfTablepatternStr);
            foreach (Match tableMatch in matches)
            {
                if (!tableMatch.Success) continue;

                string newValue = "NULL";

                var square = Regex.Match(tableMatch.Value, @"(?<=\[).*?(?=\])");
                var curly = Regex.Match(tableMatch.Value, @"(?<=\{\"").*?(?=\""\})");
                if (curly.Success && square.Success)
                {
                    var varName = Regex.Match(square.Value, Namepattern);

                    #region GREATER-OR-EQUAL

                    var greater = Regex.Match(square.Value, Valuepattern(">>"));
                    if (varName.Success && greater.Success)
                    {
                        var nvar = scheme.Schemer.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(greater.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value > parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(greater.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value > parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    var greaterOrEqual = Regex.Match(square.Value, Valuepattern(">="));
                    if (varName.Success && greaterOrEqual.Success)
                    {
                        var nvar = scheme.Schemer.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(greaterOrEqual.Value, NumberStyles.Float,
                                            CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value >= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(greaterOrEqual.Value, NumberStyles.Integer,
                                            CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value >= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    #endregion

                    #region EQUAL-NOTEQUAL

                    var equal = Regex.Match(square.Value, Valuepattern("=="));
                    if (varName.Success && equal.Success)
                    {
                        var nvar = scheme.Schemer.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(equal.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !nFloat.Value.Equals(parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(equal.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = nInt.Value != parseValue ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NBool nBool:
                                {
                                    if (bool.TryParse(equal.Value, out bool parseValue))
                                    {
                                        newValue = nBool.Value != parseValue ? string.Empty : curly.Value;
                                    }
                                    else
                                    {
                                        NDebug.Log(
                                            "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                            NLogType.Error);
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    var notEqual = Regex.Match(square.Value, Valuepattern("!="));
                    if (varName.Success && notEqual.Success)
                    {
                        var nvar = scheme.Schemer.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(notEqual.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = nFloat.Value.Equals(parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(notEqual.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = nInt.Value == parseValue ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NBool nBool:
                                {
                                    if (bool.TryParse(equal.Value, out bool parseValue))
                                    {
                                        newValue = nBool.Value == parseValue ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    #endregion

                    #region LESSER-OR-EQUAL

                    var lesser = Regex.Match(square.Value, Valuepattern("<<"));
                    if (varName.Success && lesser.Success)
                    {
                        var nvar = scheme.Schemer.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(lesser.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value < parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(lesser.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value < parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    var lesserOrEqual = Regex.Match(square.Value, Valuepattern("<="));
                    if (varName.Success && lesserOrEqual.Success)
                    {
                        var nvar = scheme.Schemer.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(lesserOrEqual.Value, NumberStyles.Float,
                                            CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value <= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(lesserOrEqual.Value, NumberStyles.Integer,
                                            CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value <= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    #endregion
                }

                go:
                value = value.Remove(tableMatch.Index, tableMatch.Length);
                value = value.Insert(tableMatch.Index, newValue);
                value = value.Replace(@"\n", "\n");
                goto tableMatch;
            }

            consMatch:
            matches = Regex.Matches(value, IfConspiratorpatternStr);
            foreach (Match consMatch in matches)
            {
                if (!consMatch.Success) continue;

                string newValue = "NULL";

                var square = Regex.Match(consMatch.Value, @"(?<=\[).*?(?=\])");
                var curly = Regex.Match(consMatch.Value, @"(?<=\{\"").*?(?=\""\})");
                if (curly.Success && square.Success)
                {
                    var varName = Regex.Match(square.Value, Namepattern);

                    #region GREATER-OR-EQUAL

                    var greater = Regex.Match(square.Value, Valuepattern(">>"));
                    if (varName.Success && greater.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(greater.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value > parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(greater.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value > parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    var greaterOrEqual = Regex.Match(square.Value, Valuepattern(">="));
                    if (varName.Success && greaterOrEqual.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(greaterOrEqual.Value, NumberStyles.Float,
                                            CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value >= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(greaterOrEqual.Value, NumberStyles.Integer,
                                            CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value >= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    #endregion

                    #region EQUAL-NOTEQUAL

                    var equal = Regex.Match(square.Value, Valuepattern("=="));
                    if (varName.Success && equal.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(equal.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !nFloat.Value.Equals(parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(equal.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = nInt.Value != parseValue ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NBool nBool:
                                {
                                    if (bool.TryParse(equal.Value, out bool parseValue))
                                    {
                                        newValue = nBool.Value != parseValue ? string.Empty : curly.Value;
                                    }
                                    else
                                    {
                                        NDebug.Log(
                                            "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                            NLogType.Error);
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    var notEqual = Regex.Match(square.Value, Valuepattern("!="));
                    if (varName.Success && notEqual.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(notEqual.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = nFloat.Value.Equals(parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(notEqual.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = nInt.Value == parseValue ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NBool nBool:
                                {
                                    if (bool.TryParse(equal.Value, out bool parseValue))
                                    {
                                        newValue = nBool.Value == parseValue ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    #endregion

                    #region LESSER-OR-EQUAL

                    var lesser = Regex.Match(square.Value, Valuepattern("<<"));
                    if (varName.Success && lesser.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(lesser.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value < parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(lesser.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value < parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    var lesserOrEqual = Regex.Match(square.Value, Valuepattern("<="));
                    if (varName.Success && lesserOrEqual.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(lesserOrEqual.Value, NumberStyles.Float,
                                            CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value <= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(lesserOrEqual.Value, NumberStyles.Integer,
                                            CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value <= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    #endregion
                }

                go:
                value = value.Remove(consMatch.Index, consMatch.Length);
                value = value.Insert(consMatch.Index, newValue);
                value = value.Replace(@"\n", "\n");
                goto consMatch;
            }

            targetMatch:
            matches = Regex.Matches(value, IfTargetpatternStr);
            foreach (Match targetMatch in matches)
            {
                if (!targetMatch.Success) continue;

                string newValue = "NULL";

                var square = Regex.Match(targetMatch.Value, @"(?<=\[).*?(?=\])");
                var curly = Regex.Match(targetMatch.Value, @"(?<=\{\"").*?(?=\""\})");
                if (curly.Success && square.Success)
                {
                    var varName = Regex.Match(square.Value, Namepattern);

                    #region GREATER-OR-EQUAL

                    var greater = Regex.Match(square.Value, Valuepattern(">>"));
                    if (varName.Success && greater.Success)
                    {
                        var nvar = scheme.Schemer.Target.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(greater.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value > parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(greater.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value > parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    var greaterOrEqual = Regex.Match(square.Value, Valuepattern(">="));
                    if (varName.Success && greaterOrEqual.Success)
                    {
                        var nvar = scheme.Schemer.Target.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(greaterOrEqual.Value, NumberStyles.Float,
                                            CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value >= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(greaterOrEqual.Value, NumberStyles.Integer,
                                            CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value >= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    #endregion

                    #region EQUAL-NOTEQUAL

                    var equal = Regex.Match(square.Value, Valuepattern("=="));
                    if (varName.Success && equal.Success)
                    {
                        var nvar = scheme.Schemer.Target.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(equal.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !nFloat.Value.Equals(parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(equal.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = nInt.Value != parseValue ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NBool nBool:
                                {
                                    if (bool.TryParse(equal.Value, out bool parseValue))
                                    {
                                        newValue = nBool.Value != parseValue ? string.Empty : curly.Value;
                                    }
                                    else
                                    {
                                        NDebug.Log(
                                            "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                            NLogType.Error);
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    var notEqual = Regex.Match(square.Value, Valuepattern("!="));
                    if (varName.Success && notEqual.Success)
                    {
                        var nvar = scheme.Schemer.Target.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(notEqual.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = nFloat.Value.Equals(parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(notEqual.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = nInt.Value == parseValue ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NBool nBool:
                                {
                                    if (bool.TryParse(equal.Value, out bool parseValue))
                                    {
                                        newValue = nBool.Value == parseValue ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    #endregion

                    #region LESSER-OR-EQUAL

                    var lesser = Regex.Match(square.Value, Valuepattern("<<"));
                    if (varName.Success && lesser.Success)
                    {
                        var nvar = scheme.Schemer.Target.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(lesser.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value < parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(lesser.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value < parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    var lesserOrEqual = Regex.Match(square.Value, Valuepattern("<="));
                    if (varName.Success && lesserOrEqual.Success)
                    {
                        var nvar = scheme.Schemer.Target.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                        }
                        else
                        {
                            switch (nvar)
                            {
                                case NFloat nFloat:
                                {
                                    if (float.TryParse(lesserOrEqual.Value, NumberStyles.Float,
                                            CultureInfo.InvariantCulture,
                                            out float parseValue))
                                    {
                                        newValue = !(nFloat.Value <= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                                case NInt nInt:
                                {
                                    if (int.TryParse(lesserOrEqual.Value, NumberStyles.Integer,
                                            CultureInfo.InvariantCulture,
                                            out int parseValue))
                                    {
                                        newValue = !(nInt.Value <= parseValue) ? string.Empty : curly.Value;
                                        goto go;
                                    }

                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);

                                    break;
                                }
                            }
                        }
                    }

                    #endregion
                }

                go:
                value = value.Remove(targetMatch.Index, targetMatch.Length);
                value = value.Insert(targetMatch.Index, newValue);
                value = value.Replace(@"\n", "\n");
                goto targetMatch;
            }

            return Regex.Replace(value, @"(\n\s*){3,}", "\n\n").TrimStart('\n').TrimEnd('\n');
        }

        public static bool PatternExists(this string value)
        {
            return Regex.IsMatch(value, IfGlobalpattern) || Regex.IsMatch(value, IfTablepattern) ||
                   Regex.IsMatch(value, IfConspiratorpattern) || Regex.IsMatch(value, IfConspiratorHasPolicypattern) || Regex.IsMatch(value, IfTargetpattern);
        }

        public static bool If(this string value, Scheme scheme)
        {
            var matches = Regex.Matches(value, IfGlobalpattern);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var varName = Regex.Match(match.Value, Namepattern);

                    #region GREATER-OR-EQUAL

                    var greater = Regex.Match(match.Value, Valuepattern(">>"));
                    if (varName.Success && greater.Success)
                    {
                        var nvar = IM.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(greater.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value > parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(greater.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value > parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    var greaterOrEqual = Regex.Match(match.Value, Valuepattern(">="));
                    if (varName.Success && greaterOrEqual.Success)
                    {
                        var nvar = IM.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(greaterOrEqual.Value, NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value >= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(greaterOrEqual.Value, NumberStyles.Integer,
                                        CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value >= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    #endregion

                    #region EQUAL-NOTEQUAL

                    var equal = Regex.Match(match.Value, Valuepattern("=="));
                    if (varName.Success && equal.Success)
                    {
                        var nvar = IM.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(equal.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!nFloat.Value.Equals(parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(equal.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (nInt.Value != parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NBool nBool:
                            {
                                if (bool.TryParse(equal.Value, out bool parseValue))
                                {
                                    if (nBool.Value != parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    var notEqual = Regex.Match(match.Value, Valuepattern("!="));
                    if (varName.Success && notEqual.Success)
                    {
                        var nvar = IM.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(notEqual.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (nFloat.Value.Equals(parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(notEqual.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (nInt.Value == parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NBool nBool:
                            {
                                if (bool.TryParse(equal.Value, out bool parseValue))
                                {
                                    if (nBool.Value == parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    #endregion

                    #region LESSER-OR-EQUAL

                    var lesser = Regex.Match(match.Value, Valuepattern("<<"));
                    if (varName.Success && lesser.Success)
                    {
                        var nvar = IM.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(lesser.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value < parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(lesser.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value < parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    var lesserOrEqual = Regex.Match(match.Value, Valuepattern("<="));
                    if (varName.Success && lesserOrEqual.Success)
                    {
                        var nvar = IM.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(lesserOrEqual.Value, NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value <= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(lesserOrEqual.Value, NumberStyles.Integer,
                                        CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value <= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    #endregion
                }
            }

            matches = Regex.Matches(value, IfTablepattern);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var varName = Regex.Match(match.Value, Namepattern);

                    #region GREATER-OR-EQUAL

                    var greater = Regex.Match(match.Value, Valuepattern(">>"));
                    if (varName.Success && greater.Success)
                    {
                        var nvar = scheme.Schemer.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(greater.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value > parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(greater.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value > parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    var greaterOrEqual = Regex.Match(match.Value, Valuepattern(">="));
                    if (varName.Success && greaterOrEqual.Success)
                    {
                        var nvar = scheme.Schemer.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(greaterOrEqual.Value, NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value >= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(greaterOrEqual.Value, NumberStyles.Integer,
                                        CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value >= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    #endregion

                    #region EQUAL-NOTEQUAL

                    var equal = Regex.Match(match.Value, Valuepattern("=="));
                    if (varName.Success && equal.Success)
                    {
                        var nvar = scheme.Schemer.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(equal.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!nFloat.Value.Equals(parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(equal.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (nInt.Value != parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NBool nBool:
                            {
                                if (bool.TryParse(equal.Value, out bool parseValue))
                                {
                                    if (nBool.Value != parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    var notEqual = Regex.Match(match.Value, Valuepattern("!="));
                    if (varName.Success && notEqual.Success)
                    {
                        var nvar = scheme.Schemer.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(notEqual.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (nFloat.Value.Equals(parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(notEqual.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (nInt.Value == parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NBool nBool:
                            {
                                if (bool.TryParse(equal.Value, out bool parseValue))
                                {
                                    if (nBool.Value == parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    #endregion

                    #region LESSER-OR-EQUAL

                    var lesser = Regex.Match(match.Value, Valuepattern("<<"));
                    if (varName.Success && lesser.Success)
                    {
                        var nvar = scheme.Schemer.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(lesser.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value < parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(lesser.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value < parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    var lesserOrEqual = Regex.Match(match.Value, Valuepattern("<="));
                    if (varName.Success && lesserOrEqual.Success)
                    {
                        var nvar = scheme.Schemer.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(lesserOrEqual.Value, NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value <= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(lesserOrEqual.Value, NumberStyles.Integer,
                                        CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value <= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    #endregion
                }
            }

            matches = Regex.Matches(value, IfConspiratorpattern);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var varName = Regex.Match(match.Value, Namepattern);

                    #region GREATER-OR-EQUAL

                    var greater = Regex.Match(match.Value, Valuepattern(">>"));
                    if (varName.Success && greater.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(greater.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value > parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(greater.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value > parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    var greaterOrEqual = Regex.Match(match.Value, Valuepattern(">="));
                    if (varName.Success && greaterOrEqual.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(greaterOrEqual.Value, NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value >= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(greaterOrEqual.Value, NumberStyles.Integer,
                                        CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value >= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    #endregion

                    #region EQUAL-NOTEQUAL

                    var equal = Regex.Match(match.Value, Valuepattern("=="));
                    if (varName.Success && equal.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(equal.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!nFloat.Value.Equals(parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(equal.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (nInt.Value != parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NBool nBool:
                            {
                                if (bool.TryParse(equal.Value, out bool parseValue))
                                {
                                    if (nBool.Value != parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    var notEqual = Regex.Match(match.Value, Valuepattern("!="));
                    if (varName.Success && notEqual.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(notEqual.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (nFloat.Value.Equals(parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(notEqual.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (nInt.Value == parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NBool nBool:
                            {
                                if (bool.TryParse(equal.Value, out bool parseValue))
                                {
                                    if (nBool.Value == parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    #endregion

                    #region LESSER-OR-EQUAL

                    var lesser = Regex.Match(match.Value, Valuepattern("<<"));
                    if (varName.Success && lesser.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(lesser.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value < parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(lesser.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value < parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    var lesserOrEqual = Regex.Match(match.Value, Valuepattern("<="));
                    if (varName.Success && lesserOrEqual.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(lesserOrEqual.Value, NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value <= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(lesserOrEqual.Value, NumberStyles.Integer,
                                        CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value <= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    #endregion
                }
            }
            
            matches = Regex.Matches(value, IfConspiratorHasPolicypattern);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var varName = Regex.Match(match.Value, Namepattern);

                    #region EQUAL-NOTEQUAL

                    var equal = Regex.Match(match.Value, Valuepattern("=="));
                    if (varName.Success && equal.Success)
                    {
                        var hasPolicy = scheme.Schemer.Conspirator.HasPolicy(equal.Value);
                        if (!hasPolicy)
                            return false;
                    }

                    #endregion
                }
            }
            
            matches = Regex.Matches(value, IfTargetHasPolicypattern);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var varName = Regex.Match(match.Value, Namepattern);

                    #region EQUAL-NOTEQUAL

                    var equal = Regex.Match(match.Value, Valuepattern("=="));
                    if (varName.Success && equal.Success)
                    {
                        var hasPolicy = scheme.Schemer.Target.HasPolicy(equal.Value);
                        if (!hasPolicy)
                            return false;
                    }

                    #endregion
                }
            }
            
            matches = Regex.Matches(value, IfConspiratorNotHasPolicypattern);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var varName = Regex.Match(match.Value, Namepattern);

                    #region EQUAL-NOTEQUAL

                    var equal = Regex.Match(match.Value, Valuepattern("=="));
                    if (varName.Success && equal.Success)
                    {
                        var hasPolicy = scheme.Schemer.Conspirator.HasPolicy(equal.Value);
                        if (hasPolicy)
                            return false;
                    }

                    #endregion
                }
            }
            
            matches = Regex.Matches(value, IfTargetNotHasPolicypattern);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var varName = Regex.Match(match.Value, Namepattern);

                    #region EQUAL-NOTEQUAL

                    var equal = Regex.Match(match.Value, Valuepattern("=="));
                    if (varName.Success && equal.Success)
                    {
                        var hasPolicy = scheme.Schemer.Target.HasPolicy(equal.Value);
                        if (hasPolicy)
                            return false;
                    }

                    #endregion
                }
            }

            matches = Regex.Matches(value, IfTargetpattern);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var varName = Regex.Match(match.Value, Namepattern);

                    #region GREATER-OR-EQUAL

                    var greater = Regex.Match(match.Value, Valuepattern(">>"));
                    if (varName.Success && greater.Success)
                    {
                        var nvar = scheme.Schemer.Target.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(greater.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value > parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(greater.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value > parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    var greaterOrEqual = Regex.Match(match.Value, Valuepattern(">="));
                    if (varName.Success && greaterOrEqual.Success)
                    {
                        var nvar = scheme.Schemer.Target.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(greaterOrEqual.Value, NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value >= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(greaterOrEqual.Value, NumberStyles.Integer,
                                        CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value >= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    #endregion

                    #region EQUAL-NOTEQUAL

                    var equal = Regex.Match(match.Value, Valuepattern("=="));
                    if (varName.Success && equal.Success)
                    {
                        var nvar = scheme.Schemer.Target.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(equal.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!nFloat.Value.Equals(parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(equal.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (nInt.Value != parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NBool nBool:
                            {
                                if (bool.TryParse(equal.Value, out bool parseValue))
                                {
                                    if (nBool.Value != parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    var notEqual = Regex.Match(match.Value, Valuepattern("!="));
                    if (varName.Success && notEqual.Success)
                    {
                        var nvar = scheme.Schemer.Target.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(notEqual.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (nFloat.Value.Equals(parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(notEqual.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (nInt.Value == parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NBool nBool:
                            {
                                if (bool.TryParse(equal.Value, out bool parseValue))
                                {
                                    if (nBool.Value == parseValue)
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    #endregion

                    #region LESSER-OR-EQUAL

                    var lesser = Regex.Match(match.Value, Valuepattern("<<"));
                    if (varName.Success && lesser.Success)
                    {
                        var nvar = scheme.Schemer.Target.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(lesser.Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value < parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(lesser.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value < parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    var lesserOrEqual = Regex.Match(match.Value, Valuepattern("<="));
                    if (varName.Success && lesserOrEqual.Success)
                    {
                        var nvar = scheme.Schemer.Conspirator.GetVariable(varName.Value);
                        if (nvar == null)
                        {
                            NDebug.Log("The specified variable in the condition command could not be found.",
                                NLogType.Error);
                            return false;
                        }

                        switch (nvar)
                        {
                            case NFloat nFloat:
                            {
                                if (float.TryParse(lesserOrEqual.Value, NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out float parseValue))
                                {
                                    if (!(nFloat.Value <= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                            case NInt nInt:
                            {
                                if (int.TryParse(lesserOrEqual.Value, NumberStyles.Integer,
                                        CultureInfo.InvariantCulture,
                                        out int parseValue))
                                {
                                    if (!(nInt.Value <= parseValue))
                                        return false;
                                }
                                else
                                {
                                    NDebug.Log(
                                        "The condition command is invalid. Please check the command. You can refer to the documentation for help.",
                                        NLogType.Error);
                                    return false;
                                }

                                break;
                            }
                        }
                    }

                    #endregion
                }
            }

            return true;
        }
    }
}