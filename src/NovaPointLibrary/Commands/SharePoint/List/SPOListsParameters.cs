using NovaPointLibrary.Solutions;
using System.Linq.Expressions;


namespace NovaPointLibrary.Commands.SharePoint.List
{
    public class SPOListsParameters : ISolutionParameters
    {
        internal Expression<Func<Microsoft.SharePoint.Client.List, object>>[] ListExpressions = [];

        public bool AllLists { get; set; } = true;
        public bool IncludeLists { get; set; } = true;
        public bool IncludeLibraries { get; set; } = true;
        public bool IncludeHiddenLists { get; set; } = false;
        public bool IncludeSystemLists { get; set; } = false;

        internal HashSet<string> CollectionLists { get; set; } = [];
        private string _collectionListsPath = string.Empty;
        public string CollectionListsPath
        {
            get { return _collectionListsPath; }
            set { _collectionListsPath = value.Trim(); }
        }

        private string _listTitle = string.Empty;
        public string ListTitle
        {
            get { return _listTitle; }
            set { _listTitle = value.Trim(); }
        }

        public void ParametersCheck()
        {
            if (!AllLists && string.IsNullOrWhiteSpace(CollectionListsPath) && string.IsNullOrWhiteSpace(ListTitle))
            {
                throw new Exception("No List or Library was selected. Select all lists, add a file with a collection of list or a list Title.");
            }
            if (AllLists && !IncludeLists && !IncludeLibraries)
            {
                throw new Exception("No List or Library was selected. Select to include at least list or libraries.");
            }
            if (!string.IsNullOrWhiteSpace(CollectionListsPath))
            {
                if (File.Exists(CollectionListsPath))
                {
                    IEnumerable<string> lines = File.ReadLines(@$"{CollectionListsPath}");
                    CollectionLists = lines
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(l => l.Trim())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    throw new Exception("File with the collection of lists doesn't exist.");
                }
            }
        }

    }
}
