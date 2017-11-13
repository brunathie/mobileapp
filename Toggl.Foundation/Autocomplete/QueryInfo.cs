using Toggl.Foundation.Autocomplete.Suggestions;

namespace Toggl.Foundation.Autocomplete
{
    public struct QueryInfo
    {
        public string Text { get; }

        public AutocompleteSuggestionType SuggestionType { get; }

        public QueryInfo(string text, AutocompleteSuggestionType suggestionType)
        {
            Text = text;
            SuggestionType = suggestionType;
        }
    }
}
