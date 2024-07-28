using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VirtualDesktopManager.Utils
{
    /// <summary>
    /// Helps writing in different circumstances.
    /// </summary>
    public static class TextManager
    {
        // Member variables:
        public enum StorageFormat
        {
            Storage,
            RAM,
        }

        // Methods:

        #region string manipulation

        /// <summary>
        /// Writes a multiline sized ASCII text to several strings
        /// </summary>
        /// <param name="data">Strings to add text to. Make sure they are equally long.</param>
        /// <param name="toWrite">Text to write.</param>
        public static void MultiLineWriting(ref List<string> data, string toWrite)
        {
            List<string> output = new List<string>();

            List<List<string>> letters = new List<List<string>>();
            for (int iii = 0; iii < toWrite.Count(); iii++)
            {
                letters.Add(TextLibrary(data.Count, toWrite.ElementAt(iii)));
            }

            for (int line = 0; line < data.Count(); line++)
            {
                string completeLine = data.ElementAt(line);

                for (int indexLetter = 0; indexLetter < letters.Count; indexLetter++)
                {
                    completeLine += letters.ElementAt(indexLetter).ElementAt(line);
                }

                output.Add(completeLine);
            }

            data = output;
        }

        /// <summary>
        /// Contains the text data for the text engine.
        /// </summary>
        /// <param name="textSize">Specifies the text size of the outputed text.</param>
        /// <param name="character">Specifies the characte that is to be outputed.</param>
        /// <returns>The requested character. In multiple lines if required.</returns>
        private static List<string> TextLibrary(int textSize, char character)
        {
            List<string> output = new List<string>();

            switch (textSize)
            {
                case 1:

                    break;


                case 5:
                    for (int iii = 0; iii < 5; iii++) output.Add("  ");

                    switch (character)
                    {
                        case ' ':
                            for (int iii = 0; iii < 5; iii++) output.Add("   ");
                            break;

                        case ':':
                            output.Add(" ");
                            output.Add("#");
                            output.Add(" ");
                            output.Add("#");
                            output.Add(" ");
                            break;

                        case 'A':
                            output.Add(" ### ");
                            output.Add("#   #");
                            output.Add("#####");
                            output.Add("#   #");
                            output.Add("#   #");
                            break;

                        case 'E':
                            output.Add("######");
                            output.Add("#     ");
                            output.Add("##### ");
                            output.Add("#     ");
                            output.Add("######");
                            break;

                        case 'K':
                            output.Add("#   #");
                            output.Add("#  # ");
                            output.Add("###  ");
                            output.Add("#  # ");
                            output.Add("#   #");
                            break;

                        case 'N':
                            output.Add("##    #");
                            output.Add("# #   #");
                            output.Add("#  #  #");
                            output.Add("#   # #");
                            output.Add("#    ##");
                            break;

                        case 'S':
                            output.Add(" ####");
                            output.Add("#    ");
                            output.Add(" ### ");
                            output.Add("    #");
                            output.Add("#### ");
                            break;

                        case 'T':
                            output.Add("#######");
                            output.Add("   #   ");
                            output.Add("   #   ");
                            output.Add("   #   ");
                            output.Add("   #   ");
                            break;

                        case 'W':
                            output.Add("#       #");
                            output.Add("#   #   #");
                            output.Add("#   #   #");
                            output.Add("#  # #  #");
                            output.Add(" ##   ## ");
                            break;

                        default:
                            output.Add("");
                            output.Add("");
                            output.Add("");
                            output.Add("");
                            output.Add("");
                            break;
                    }

                    break;
            }

            // if text was added in more than one instance they will be on different lines therefore any line after the text Size should be added to the first lines.

            if (output.Count > textSize)
            {
                List<string> newOutput = new List<string>();

                for (int iii = 0; iii < textSize; iii++)
                {
                    string line = "";

                    for (int jjj = iii; jjj < output.Count; jjj += textSize)
                    {
                        line += output.ElementAt(jjj);
                    }

                    newOutput.Add(line);
                }

                output = newOutput;
            }



            return output;
        }


        public static string SurroundWithQuotes(string text)
        {
            return "\"" + text + "\"";
        }

        public static string RemoveSurroundingQuotes(string text)
        {
            if (text.Length > 2)
            {
                if (text[0] == '\"') text = text.Remove(0, 1);
                if (text[text.Length - 1] == '\"') text = text.Remove(text.Length - 1, 1);
            }
            return text;
        }

        /// <summary>
        /// Removes spaces at the beginning and end of a string.
        /// </summary>
        /// <param name="text">Text to remove spaces from.</param>
        /// <returns>Text with no spaces in beginning and end.</returns>
        public static string RemoveSurroundingSpaces(string text)
        {
            while (text[0] == ' ' || text[0] == '\t')
            {
                text = text.Remove(0, 1);

                if (text.Length == 0) break;
            }
            if (text.Length > 1) while (text[text.Length - 1] == ' ' || text[text.Length - 1] == '\t') text = text.Remove(text.Length - 1); // length can't become zero since there is a character that stoped the removal of spaces at the beginnig of the text.

            return text;
        }

        /// <summary>
        /// Separates text enclosed within specified strings from a text. Cares about number of times enclosed.
        /// </summary>
        /// <param name="inputText">Text to remove other text from.</param>
        /// <param name="normalText">Not enclosed text.</param>
        /// <param name="encapsulatedText">Enclosed text, separated by grouping.</param>
        /// <param name="encapsulationStarter">String that starts encapsulation.</param>
        /// <param name="encapsulationEnder">String that end encapsulation.</param>
        public static void SeparateEncapsulatedText(List<string> inputText, out List<string> normalText, out List<List<string>> encapsulatedText, string encapsulationStarter, string encapsulationEnder)
        {
            normalText = new List<string>();
            encapsulatedText = new List<List<string>>();
            List<string> encapsulatedTextCurrent = new List<string>();
            int encapsulatedTimes = 0;

            foreach (string line in inputText)
            {
                string lineToParse = line;

                // encapsulated
                string encapsulatedLine = "";

                do
                {
                    int indexOfStarter = -1;
                    int indexOfEnder = -1;

                    if (lineToParse.Contains(encapsulationStarter))
                    {
                        indexOfStarter = lineToParse.IndexOf(encapsulationStarter);
                    }
                    if (lineToParse.Contains(encapsulationEnder))
                    {
                        indexOfEnder = lineToParse.IndexOf(encapsulationEnder);
                    }
                    int indexOfFirstMarker = indexOfStarter < indexOfEnder ? indexOfStarter : indexOfEnder;
                    if (indexOfFirstMarker == -1) indexOfFirstMarker = indexOfStarter != -1 ? indexOfStarter : indexOfEnder;


                    if (indexOfFirstMarker == -1)
                    {
                        // No markers:
                        if (encapsulatedTimes == 0) normalText.Add(lineToParse);
                        else encapsulatedLine += lineToParse;

                        lineToParse = "";
                    }
                    else
                    {
                        int starterLength = encapsulationStarter.Length;
                        if (lineToParse.Substring(indexOfFirstMarker).Length >= starterLength)
                        {
                            string marker = lineToParse.Substring(indexOfFirstMarker, starterLength);

                            if (marker == encapsulationStarter)
                            {
                                string text = lineToParse.Remove(indexOfFirstMarker);
                                lineToParse = lineToParse.Remove(0, indexOfFirstMarker + starterLength);

                                if (encapsulatedTimes == 0) normalText.Add(text);
                                else encapsulatedLine += text + encapsulationStarter;

                                encapsulatedTimes++;

                                continue;
                            }
                        }

                        int enderLength = encapsulationEnder.Length;
                        if (lineToParse.Substring(indexOfFirstMarker).Length >= enderLength)
                        {
                            string marker = lineToParse.Substring(indexOfFirstMarker, enderLength);

                            if (marker == encapsulationEnder)
                            {
                                string text = "";
                                if (lineToParse.Length > indexOfFirstMarker + enderLength) lineToParse.Remove(indexOfFirstMarker + enderLength);
                                lineToParse = lineToParse.Remove(0, indexOfFirstMarker + enderLength);

                                encapsulatedLine += text;
                                encapsulatedTimes--;

                                if (encapsulatedTimes < 0) throw new Exception("Too many closing parentheses.");
                                else if (encapsulatedTimes == 0)
                                {
                                    encapsulatedTextCurrent.Add(encapsulatedLine);
                                    encapsulatedLine = "";
                                    encapsulatedText.Add(encapsulatedTextCurrent);
                                    encapsulatedTextCurrent = new List<string>();
                                }

                                continue;
                            }
                        }
                    }

                } while (lineToParse.Length > 0);

                if (encapsulatedLine.Length > 0) encapsulatedTextCurrent.Add(encapsulatedLine);

            }   // New input line!

            if (encapsulatedTextCurrent.Count > 0) encapsulatedText.Add(encapsulatedTextCurrent);   // if input text ends in an encapsulation. (should never happen really)
        }

        /// <summary>
        /// Separates text enclosed within specified strings from a text. Cares about number of times enclosed.
        /// </summary>
        /// <param name="inputText">Text to remove other text from.</param>
        /// <param name="normalText">Not enclosed text.</param>
        /// <param name="encapsulatedText">Enclosed text, separated by grouping.</param>
        /// <param name="encapsulationStarter">String that starts encapsulation.</param>
        /// <param name="encapsulationEnder">String that end encapsulation.</param>
        /// <param name="insertLocationsOfEncapsulatedText">Points that indicate on what line and on what character on that line that encapsulated text at the same index as the point should be inserted at to restore the orignal text.</param>
        public static void SeparateEncapsulatedText(List<string> inputText, out List<string> normalText, out List<List<string>> encapsulatedText, string encapsulationStarter, string encapsulationEnder, out List<System.Drawing.Point> insertLocationsOfEncapsulatedText)
        {
            if (encapsulationStarter == encapsulationEnder) throw new Exception("Can't have same ender as starter for encapsulated text. That would be the same as only having starters.");

            normalText = new List<string>();
            encapsulatedText = new List<List<string>>();
            insertLocationsOfEncapsulatedText = new List<System.Drawing.Point>();
            int encapsulatedTimes = 0;

            for (int lineIndex = 0; lineIndex < inputText.Count; lineIndex++)
            {
                string line = inputText.ElementAt(lineIndex);

                string textSoFar = "";
                string normalLine = "";

                foreach (char character in line)
                {
                    textSoFar += character;

                    if (textSoFar.Length >= encapsulationStarter.Length && textSoFar.EndsWith(encapsulationStarter))
                    {
                        // starter found!
                        if (encapsulatedTimes == 0)
                        {
                            textSoFar = textSoFar.Remove(textSoFar.Length - encapsulationStarter.Length); // remove marker
                            normalLine += textSoFar;
                            textSoFar = "";
                            encapsulatedText.Add(new List<string>());
                            insertLocationsOfEncapsulatedText.Add(new System.Drawing.Point(normalLine.Length, lineIndex));
                        }
                        encapsulatedTimes++;
                    }
                    else if (textSoFar.Length >= encapsulationEnder.Length && textSoFar.EndsWith(encapsulationEnder))
                    {
                        // ender found!
                        encapsulatedTimes--;

                        if (encapsulatedTimes == 0)
                        {
                            textSoFar = textSoFar.Remove(textSoFar.Length - encapsulationEnder.Length);
                            encapsulatedText.Last().Add(textSoFar);
                            textSoFar = "";
                        }
                    }
                }

                if (encapsulatedTimes == 0)
                {
                    normalLine += textSoFar;
                }
                else
                {
                    encapsulatedText.Last().Add(textSoFar);
                }
                normalText.Add(normalLine);
            }
        }

        /// <summary>
        /// Split a string with new line markers into seperate lines at those markers.
        /// </summary>
        /// <param name="stringText">Text to split up into several strings.</param>
        /// <returns>Strings with no new line markers in them.</returns>
        public static string[] SplitMutiLineString(string stringText)
        {
            return stringText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None); // splits string at the given string combinations. Newline can be writen in 3 ways.

            /* Alternative execution:
            List<string> listText = new List<string>();

            using (StringReader sr = new StringReader(stringText))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    listText.Add(line);
                }
            }

            return listText;
            */
        }

        /// <summary>
        /// Combines multiple strings into one.
        /// </summary>
        /// <param name="collection">Collection of strings to combine.</param>
        /// <param name="seperatorBetweenLines">String to put between every line. If it is null then "Environment.NewLine" will be used.</param>
        /// <returns></returns>
        public static string CombineStringCollection(IEnumerable<string> collection, string seperatorBetweenLines = null)
        {
            return String.Join(seperatorBetweenLines ?? Environment.NewLine, collection);
        }


        /// <summary>
        /// Convert a string to a regular expresion.
        ///   ? - any character  (one and only one)
        ///   * - any characters (zero or more)
        /// </summary>
        /// <param name="text">String to convert.</param>
        /// <returns>Converted string. This string can be used with Regex.IsMatch.</returns>
        public static string WildCardToRegular(string text)
        {
            return "^" + Regex.Escape(text).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }
        /// <summary>
        /// Test a string against another string with wildcards:
        ///   ? - any character  (one and only one)
        ///   * - any characters (zero or more)
        /// </summary>
        /// <param name="text">A string to check against a pattern.</param>
        /// <param name="wildcards">A string with wildcards that the first string should be tested against.</param>
        /// <returns>True if the test string follows the pattern of the wildcards string; otherwise false.</returns>
        public static bool CheckWildCards(string text, string wildcards)
        {
            return Regex.IsMatch(text, WildCardToRegular(wildcards));
        }

        #endregion


        #region meta text operations

        /// <summary>
        /// Determines how many characters there are in a string. Some characters can be longer than 1 character. (for example new line marker "\r\n")
        /// </summary>
        /// <param name="text">Text to count characters for.</param>
        /// <returns>Number of characters in string.</returns>
        public static int CountNumberOfCharactersInText(string text)
        {
            return text.Replace("\r\n", "\n").Length;
        }

        public static int TextByteLength(string text, Encoding encoding)
        {
            return encoding.GetBytes(text).ToArray().Length;
        }


        /// <summary>
        /// Converts text to bytes according to a specified encoding format.
        /// </summary>
        /// <param name="textToConvert">The text to convert to bytes.</param>
        /// <param name="encoding">Encoding to use for conversion.</param>
        /// <returns>An array the bytes converted from the string.</returns>
        public static byte[] TextToBytes(string textToConvert, Encoding encoding)
        {
            return encoding.GetBytes(textToConvert);
        }

        /// <summary>
        /// Converts text to bytes according to a specified encoding format.
        /// </summary>
        /// <param name="textToConvert">The text to convert to bytes. (will be emptied).</param>
        /// <param name="encoding">Encoding to use for conversion.</param>
        /// <param name="numberOfBytes">The number of bytes in the returned queue.</param>
        /// <returns>A queue with the strings converted to bytes.</returns>
        public static Queue<byte[]> TextToBytes(ref Queue<string> textToConvert, Encoding encoding, out int numberOfBytes)
        {
            Queue<byte[]> textInByteForm = new Queue<byte[]>();
            numberOfBytes = 0;

            while (textToConvert.Count > 0)
            {
                byte[] line = encoding.GetBytes(textToConvert.Dequeue() + (textToConvert.Count > 0 ? Environment.NewLine : ""));
                textInByteForm.Enqueue(line);
                numberOfBytes += line.Length;
            }

            return textInByteForm;
        }


        public static string BytesToString(byte[] data, Encoding encoding)
        {
            return encoding.GetString(data);
        }


        /// <summary>
        /// Gets the default encoding for different storage formats.
        /// </summary>
        /// <param name="format">The storage format the text is stored in.</param>
        /// <returns>The default encoding for the specified storage format.</returns>
        public static Encoding GetDefaultEncodingFor(StorageFormat format)
        {
            switch (format)
            {
                case StorageFormat.Storage:
                    return Encoding.UTF8;
                case StorageFormat.RAM:
                    return Encoding.Unicode;
                default:
                    return Encoding.Default;
            }
        }


        public static string GetCurrentTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }


        public static Size GetTextScreenSize(string text, Font font)
        {
            var fontFamily = new System.Windows.Media.FontFamily(font.FontFamily.Name);

            System.Windows.FontStyle fontStyle = System.Windows.FontStyles.Normal;
            if (font.Italic)
                fontStyle = System.Windows.FontStyles.Italic;

            return GetTextScreenSize(text, fontFamily, font.Size, fontStyle, System.Windows.FontWeights.Normal, System.Windows.FontStretches.Normal);
        }
        public static Size GetTextScreenSize(string text, System.Windows.Media.FontFamily fontFamily = null, double fontSize = 0)
        {
            return GetTextScreenSize(text, fontFamily, fontSize, System.Windows.FontStyles.Normal, System.Windows.FontWeights.Normal, System.Windows.FontStretches.Normal);
        }
        /// <summary>
        /// Draw a text in memory and calculate its size.
        /// </summary>
        /// <param name="text">The text to determine the size for.</param>
        /// <param name="fontFamily">The font family to use when drawing the text.</param>
        /// <param name="fontSize">The size to draw the text.</param>
        /// <param name="fontStyle">Use System.Windows.FontStyles.</param>
        /// <param name="fontWeight">Use System.Windows.FontWeights.</param>
        /// <param name="fontStretch">Use System.Windows.FontStretches.</param>
        /// <returns></returns>
        public static Size GetTextScreenSize(string text, System.Windows.Media.FontFamily fontFamily, double fontSize,
            System.Windows.FontStyle fontStyle, System.Windows.FontWeight fontWeight, System.Windows.FontStretch fontStretch)
        {
            fontFamily = fontFamily ?? new System.Windows.Controls.TextBlock().FontFamily;
            fontSize = fontSize > 0 ? fontSize : new System.Windows.Controls.TextBlock().FontSize;
            var typeface = new System.Windows.Media.Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
            var ft = new System.Windows.Media.FormattedText(text ?? string.Empty, CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, typeface, fontSize, System.Windows.Media.Brushes.Black);
            return new Size((int)ft.Width, (int)ft.Height);
        }

        #endregion

    }
}



