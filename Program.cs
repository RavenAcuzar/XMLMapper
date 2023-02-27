using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Xml.Linq;

namespace XMLMapperApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to XML Mapper (XML to Word Template)!");
            Console.Write("Please Input template file path: ");
            string tmplFile = Console.ReadLine();
            Console.Write("Please Input xml file path: ");
            string xmlValues = Console.ReadLine();
            if (string.IsNullOrEmpty(tmplFile) && string.IsNullOrEmpty(xmlValues))
            {
                Console.WriteLine("Both Feilds are required!!");
                Environment.Exit(0);
            }

            WordProcess(tmplFile, xmlValues);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(0);
        }
        public static void WordProcess(string tmpl, string xmlFile)
        {
            if (!File.Exists(tmpl))
            {
                throw new ArgumentException("Template file not found.");
            }

            File.Copy(tmpl, "Result.docx", true);

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open("Result.docx", true))
            {
                Console.WriteLine("Loading...");

                MainDocumentPart mainPart = wordDoc.MainDocumentPart;
                var templateTable = GetTemplateTable(mainPart, "{{title-issue}}");

                XDocument xml = XDocument.Load(xmlFile);
                XElement reportSectoinListOfIssues = xml.Descendants("ReportSection").LastOrDefault();
                var groupingIssues = reportSectoinListOfIssues.Descendants("GroupingSection");

                foreach (var groupingIssue in groupingIssues)
                {
                    Table table = (Table)templateTable.CloneNode(true);

                    string title = groupingIssue.Element("groupTitle").Value;
                    var count = groupingIssue.Attribute("count").Value;

                    var majorAttr = groupingIssue.Descendants("MetaInfo").ToArray();
                    string description = majorAttr[0].Element("Value").Value;
                    string explanation = majorAttr[1].Element("Value").Value;
                    string recommendation = majorAttr[2].Element("Value").Value;

                    ReplaceTextInTable(table,"{{title-issue}}",title);
                    ReplaceTextInTable(table,"{{count}}",count);
                    ReplaceTextInTable(table,"{{description}}",description);
                    ReplaceTextInTable(table,"{{explanation}}",explanation);
                    ReplaceTextInTable(table,"{{recommendation}}",recommendation);

                    var issues = groupingIssue.Descendants("Issue");
                    var issuesTable = GetTemplateTable(table, "{{file-path}}");
                    Table issuesTableClone = (Table)issuesTable.CloneNode(true);
                    var rows = new List<TableRow>();
                    foreach (var issue in issues)
                    {
                        string filePath = string.Format("{0}/{1}", issue.Descendants("FilePath").FirstOrDefault().Value, issue.Descendants("FileName").FirstOrDefault().Value);
                        string lineNum = issue.Descendants("LineStart").FirstOrDefault().Value;
                        string risk = issue.Element("Friority").Value;
                        string method = issue.Descendants("Snippet").FirstOrDefault().Value;
                        // string method = issue.Descendants("TargetFunction").FirstOrDefault().Value; //Use this line if need to change "Variable/Method Affected" Column

                        TableRow row = (TableRow)issuesTableClone.LastChild.CloneNode(true);

                        ReplaceTextInTable(row,"{{file-path}}",filePath);
                        ReplaceTextInTable(row,"{{line-num}}",lineNum);
                        ReplaceTextInTable(row,"{{risk}}",risk);
                        ReplaceTextInTable(row,"{{method}}",method);

                        rows.Add(row);
                    }

                    issuesTable.Descendants<TableRow>().ElementAtOrDefault(1)?.Remove();
                    issuesTable.Append(rows);
                    mainPart.Document.Body.Append(table);
                    mainPart.Document.Body.Append(new Paragraph(new Run(new Break() { Type = BreakValues.Page })));
                }
                templateTable.Remove();
                mainPart.Document.Save();
                wordDoc.Close();
            }


            Console.WriteLine("Success...");
        }
        private static Table GetTemplateTable(MainDocumentPart mainPart, string searchText)
        {
            var mainTable = mainPart.Document.Descendants<Table>().FirstOrDefault((t => t.Descendants<Text>().Any(tt => tt.Text.Contains(searchText))));
            return mainTable;
        }
        private static Table GetTemplateTable(Table table, string searchText)
        {
            var mainTable = table.Descendants<Table>().FirstOrDefault((t => t.Descendants<Text>().Any(tt => tt.Text.Contains(searchText))));
            return mainTable;
        }
        private static void ReplaceTextInTable(Table table, string searchText, string replacementText)
        {
            table.Descendants<Text>().Where(t => t.Text.Contains(searchText)).FirstOrDefault().Text = replacementText;
        }
        private static void ReplaceTextInTable(TableRow table, string searchText, string replacementText)
        {
            table.Descendants<Text>().Where(t => t.Text.Contains(searchText)).FirstOrDefault().Text = replacementText;
        }

    }
}