using System;
using System.Collections.Generic;
using Toggl.Foundation.Autocomplete.Suggestions;

namespace Toggl.Foundation.Autocomplete
{
    public interface IAutocompleteProvider
    {
        QueryInfo ParseFieldInfo(TextFieldInfo info);

        IObservable<IEnumerable<AutocompleteSuggestion>> Query(TextFieldInfo info);

        IObservable<IEnumerable<AutocompleteSuggestion>> Query(QueryInfo queryInfo);
    }
}
