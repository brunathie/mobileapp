using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Models;
using static Toggl.Foundation.Helper.Constants;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectClientViewModel : MvxViewModel<long, long?>
    {
        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;

        private long workspaceId;
        private IEnumerable<IDatabaseClient> allClients;

        public string Text { get; set; } = "";

        public bool SuggestCreation
        {
            get
            {
                var text = Text.Trim();
                return !string.IsNullOrEmpty(text) 
                    && !Suggestions.Any(s => s == text)
                    && Encoding.UTF8.GetByteCount(text) <= MaxClientNameLengthInBytes;
            }
        }

        public IMvxAsyncCommand CloseCommand { get; }

        public IMvxAsyncCommand CreateClientCommand { get; }

        public IMvxAsyncCommand<string> SelectClientCommand { get; }

        public MvxObservableCollection<string> Suggestions { get; } = new MvxObservableCollection<string>();

        public SelectClientViewModel(ITogglDataSource dataSource, IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.navigationService = navigationService;

            CloseCommand = new MvxAsyncCommand(close);
            CreateClientCommand = new MvxAsyncCommand(createClient);
            SelectClientCommand = new MvxAsyncCommand<string>(selectClient);
        }

        public override void Prepare(long parameter)
        {
            workspaceId = parameter;
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            allClients = await dataSource.Clients.GetAllInWorkspace(workspaceId);

            Suggestions.Add(Resources.NoClient);
            Suggestions.AddRange(allClients.Select(c => c.Name));
        }

        private void OnTextChanged()
        {
            Suggestions.Clear();
            var text = Text.Trim();
            Suggestions.AddRange(
                allClients
                    .Select(c => c.Name)
                    .Where(name => name.ContainsIgnoringCase(text))
            );

            if (!string.IsNullOrEmpty(Text)) return;
            Suggestions.Insert(0, Resources.NoClient);
        }

        private Task close()
            => navigationService.Close(this, null);

        private async Task selectClient(string clientName)
        {
            var clientId = allClients.FirstOrDefault(c => c.Name == clientName)?.Id ?? 0;
            await navigationService.Close(this, clientId);
        }

        private async Task createClient()
        {
            if (!SuggestCreation) return;
            
            var client = await dataSource.Clients.Create(Text.Trim(), workspaceId);
            await navigationService.Close(this, client.Id);
        }
    }
}