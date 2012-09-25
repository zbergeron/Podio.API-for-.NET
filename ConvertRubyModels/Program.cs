﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConvertRubyModels
{
    class Program
    {
        /// <summary>
        /// No comments means no responsibility - this was written by a cat.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            DirectoryInfo inputdir = new DirectoryInfo(@"C:\Users\Kasper Andersen\Documents\GitHub\podio-rb\lib\podio\models");

            DirectoryInfo outputdir = Directory.CreateDirectory(@"C:\Work\Git\Podio.API\Podio.API-for-.NET\Podio.API\Model"); 
          
            // convert all ruby files and output it to outputdir
            foreach (var fi in inputdir.GetFiles("*.rb"))
            {
                string filename = Path.GetFileNameWithoutExtension(fi.FullName);
                string filecontents = fi.OpenText().ReadToEnd();

                using (var writer = File.CreateText(Path.Combine(outputdir.FullName, ConvertCaseString(filename, Case.PascalCase) + ".cs")))
                {
                    writer.WriteLine(ConvertPodioModelFile(filecontents));
                }
            }


        }

        static string ConvertPodioModelFile(string rubyFileContent)
        {

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Runtime.Serialization;");

            sb.AppendLine("");
            sb.AppendLine("");
            sb.AppendLine("/// AUTOGENERATED FROM RUBYSOURCE");
            sb.AppendLine("namespace Podio.API.Model");
            sb.AppendLine("{");
            sb.AppendLine(Indent(1) + "[DataContract]");
            sb.AppendLine(Indent(1) + "public class " + Regex.Match(rubyFileContent, @"class.*::(.*)<").Groups[1].Value);
            sb.AppendLine(Indent(1) + "{");
            sb.AppendLine("");
            sb.AppendLine("");


            List<KeyValuePair<string, string>> properties = new List<KeyValuePair<string, string>>();

            // locate ruby valuetype properties
            var matches = Regex.Matches(rubyFileContent, @"property :([^ ,]*), :([^ \n,]*[a-zA-Z_])", RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {

                string rubyName = match.Groups[1].Value;
                string rubyType = match.Groups[2].Value;

                StringBuilder tmp = new StringBuilder();

                tmp.AppendLine(Indent(2) + "[DataMember(Name = \"" + rubyName + "\", IsRequired=false)]");
                tmp.AppendLine(Indent(2) + "public " + ConvertValueType(rubyType, rubyName) + " " + ConvertCaseString(rubyName, Case.PascalCase) + " { get; set; }");
                tmp.AppendLine("");

                properties.Add(new KeyValuePair<string, string>(rubyName, tmp.ToString()));
            }


            // locate the referencetype properties
            matches = Regex.Matches(rubyFileContent, @"has_one :(.*), :class => '(.*[a-zA-Z_])'", RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                StringBuilder tmp = new StringBuilder();

                string rubyName = match.Groups[1].Value;
                string rubyType = match.Groups[2].Value;
                tmp.AppendLine(Indent(2) + "[DataMember(Name = \"" + rubyName + "\", IsRequired=false)]");
                tmp.AppendLine(Indent(2) + "public " + rubyType + " " + ConvertCaseString(rubyName, Case.PascalCase) + " { get; set; }");
                tmp.AppendLine("");

                /// if a lower priority property has the same name - it will be removed
                properties.RemoveAll(x => x.Key == rubyName);

                properties.Add(new KeyValuePair<string, string>(rubyName, tmp.ToString()));
            }

            // locate the collections of referencetype properties
            matches = Regex.Matches(rubyFileContent, @"has_many :(.*), :class => '(.*[a-zA-Z_])'", RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                StringBuilder tmp = new StringBuilder();

                string rubyName = match.Groups[1].Value;
                string rubyType = match.Groups[2].Value;
                tmp.AppendLine(Indent(2) + "[DataMember(Name = \"" + rubyName + "\", IsRequired=false)]");
                tmp.AppendLine(Indent(2) + "public List<" + rubyType + "> " + ConvertCaseString(rubyName, Case.PascalCase) + " { get; set; }");
                tmp.AppendLine("");

                /// if a lower priority property has the same name - it will be removed
                properties.RemoveAll(x => x.Key == rubyName);

                properties.Add(new KeyValuePair<string, string>(rubyName, tmp.ToString()));
            }

            foreach (string s in properties.Select(x => x.Value))
            {
                sb.AppendLine(s);
            }

            sb.AppendLine(Indent(1) + "}"); // end class
            sb.AppendLine("}"); // end namespace
            return sb.ToString();
        }

        #region Helpers

        public static string Indent(int c)
        {
            return "".PadLeft(c, '\t');
        }

        static string ConvertCaseString(string phrase, Case cases)
        {
            string[] splittedPhrase = phrase.Split(' ', '-', '_');
            var sb = new StringBuilder();

            if (cases == Case.CamelCase)
            {
                sb.Append(splittedPhrase[0].ToLower());
                splittedPhrase[0] = string.Empty;
            }
            else if (cases == Case.PascalCase)
                sb = new StringBuilder();

            foreach (String s in splittedPhrase)
            {
                char[] splittedPhraseChars = s.ToCharArray();
                if (splittedPhraseChars.Length > 0)
                {
                    splittedPhraseChars[0] = ((new String(splittedPhraseChars[0], 1)).ToUpper().ToCharArray())[0];
                }
                sb.Append(new String(splittedPhraseChars));
            }
            return sb.ToString();
        }

        enum Case
        {
            PascalCase,
            CamelCase
        }

        static Dictionary<string, string> knownhashes = new Dictionary<string, string> { { "spaces", "IEnumerable<Space>" }, { "space", "Space" } };

        public static string ConvertValueType(string type, string rubyname)
        {
            if (type == "hash" && knownhashes.ContainsKey(rubyname.ToLower()))
            {
                return knownhashes[rubyname.ToLower()];
            }

            if (type == "integer") return "int?";
            if (type == "datetime" || type == "date" || type == "time") return "string"; // JSON does not do Dates
            if (type == "boolean") return "bool?";
            if (type == "hash" && rubyname.EndsWith("s")) return "Podio.API.Utils.JSONVariableData[]"; // A take on the Ruby Hash datatype
            if (type == "hash" && !rubyname.EndsWith("s")) return "Podio.API.Utils.JSONVariableData"; // A take on the Ruby Hash datatype

            if (type == "array") return "string[]"; // 

            return type;
        }

        #endregion
    }
}
