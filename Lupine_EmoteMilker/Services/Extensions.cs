using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LupineEmoteMilker.Services
{
    public static class Extensions
    {

        public static string GetUrlWithQueryString(this string requestUri, Dictionary<string, string> queryStringParams)
        {
            bool startingQuestionMarkAdded = false;
            var sb = new StringBuilder();
            sb.Append(requestUri);
            foreach (var parameter in queryStringParams)
            {
                if (parameter.Value == null)
                {
                    continue;
                }

                sb.Append(startingQuestionMarkAdded ? '&' : '?');
                sb.Append(parameter.Key);
                sb.Append('=');
                string value = parameter.Value.Contains(' ') ? "\"" + parameter.Value.Replace("", "%20") + "\"": parameter.Value;
                sb.Append(value);
                startingQuestionMarkAdded = true;
            }
            return sb.ToString();
        }

        public static string ChangeUrlFileExtension(this string uri, string newExtension)
        {
            var fileName = Path.GetFileNameWithoutExtension(uri);
            var extension = Path.GetExtension(uri);

            return uri.Replace(string.Format("{0}{1}", fileName, extension), string.Format("{0}.{1}", fileName, newExtension));
        }
    }
}
