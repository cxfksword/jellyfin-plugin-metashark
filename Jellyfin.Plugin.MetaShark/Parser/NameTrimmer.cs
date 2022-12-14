
namespace Jellyfin.Plugin.MetaShark.Parser
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    class NameTrimmer
    {

        private static TreeNode node;
        private const string path = "/Parser/dict.txt";


        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }


        public string trimName(string name, ILogger _logger)
        {
            if (NameTrimmer.node == null)
            {
                _logger.LogInformation($"生成词库树");
                List<string> dics = new List<string>();
                List<string> lines = File.ReadAllLines(AssemblyDirectory + path).ToList();
                lines.ForEach(line =>
                {
                    string[] words = line.Split(" ");
                    if (words.Length == 2)
                    {
                        dics.Add(words[0]);
                    }
                });
                NameTrimmer.node = new TreeNode();
                node.insertMany(dics);
                _logger.LogInformation($"生成词库树完成");
            }
            _logger.LogInformation($"解析名称");
            foreach (char c in "!\"#$%&()*+,-./:;<=>?@[\\]^_‘{|}~")
            {
                name = name.Replace(c, ' ');
            }
            string[] nameWords = name.Split(' ');
            string empStr = "";
            _logger.LogInformation($"开始比对" + name);
            foreach (string word in nameWords)
            {
                if (string.IsNullOrEmpty(word))
                {
                    continue;
                }
                if (NameTrimmer.node.search(word.ToLower()))
                {
                    continue;
                }
                else
                {
                    empStr += word + " ";
                }
            }
            _logger.LogInformation($"比对结果" + empStr);
            return empStr;
        }

        private class TreeNode
        {
            private bool IsLeaf = false;
            private List<TreeNode> nodes = new List<TreeNode>();
            private char value;

            public void insert(string word)
            {
                var currentNode = this;
                foreach (char c in word)
                {
                    var findNode = currentNode.nodes.FirstOrDefault(a => a.value == c);
                    if (findNode == null)
                    {

                        TreeNode newNode = new TreeNode();
                        newNode.value = c;
                        currentNode.nodes.Add(newNode);
                        currentNode = newNode;
                    }
                    else
                    {
                        currentNode = findNode;
                    }

                }
                currentNode.IsLeaf = true;
            }
            public void insertMany(List<string> words)
            {
                foreach (var work in words)
                {
                    this.insert(work);
                }
            }
            public bool search(string word)
            {
                var current = this;
                foreach (char c in word)
                {
                    if (current != null)
                    {
                        current = current.nodes.FirstOrDefault(a => a.value == c);
                    }
                    else
                    {
                        break;
                    }
                }
                return current != null ? current.IsLeaf : false;

            }

        }
    }
}
