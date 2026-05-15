using System.Text.RegularExpressions;

namespace ImageHub.Extensions;

internal static partial class HashTagStringExtension
{
    extension(string text)
    {
        /// <summary>
        /// 解析格式为 (#名称 ) 的标签并返回排除标签后的文本
        /// </summary>
        public IEnumerable<string> ParseHashSignTag (out string result)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                result = string.Empty;
                return [];
            }

            // 匹配 #后跟非空白字符（直到空格或结束）
            var matches = HashTagRegex().Matches(text);

            var tagList = new List<string>();
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                    tagList.Add(match.Groups[1].Value);
            }

            // 移除所有标签
            result = HashTagRegex().Replace(text, "").Trim();
            return tagList;
        }
    }


    //标签匹配
    [GeneratedRegex(@"#(\S+)")]
    private static partial Regex HashTagRegex();
}