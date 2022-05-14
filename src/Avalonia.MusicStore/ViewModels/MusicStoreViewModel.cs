using Avalonia.MusicStore.Models;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;

namespace Avalonia.MusicStore.ViewModels
{
    public class MusicStoreViewModel : ViewModelBase
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isBusy;
        private string? _searchText;
        private AlbumViewModel? _selectedAlbum;


        public MusicStoreViewModel()
        {
            this.WhenAnyValue(x => x.SearchText)
                    .Throttle(TimeSpan.FromMilliseconds(400))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(DoSearch!);

            BuyMusicCommand = ReactiveCommand.Create(() =>
            {
                return SelectedAlbum;
            });
        }

        public ReactiveCommand<Unit, AlbumViewModel?> BuyMusicCommand { get; }
        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }
        public AlbumViewModel? SelectedAlbum
        {
            get => _selectedAlbum;
            set => this.RaiseAndSetIfChanged(ref _selectedAlbum, value);
        }
        public ObservableCollection<AlbumViewModel> SearchResults { get; } = new();
        public string? SearchText
        {
            get => _searchText;
            set => this.RaiseAndSetIfChanged(ref _searchText, value);
        }

        private async void DoSearch(string s)
        {
            IsBusy = true;
            SearchResults.Clear();

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            if (!string.IsNullOrWhiteSpace(s))
            {
                var albums = await Album.SearchAsync(s);

                foreach (var album in albums)
                {
                    var vm = new AlbumViewModel(album);

                    SearchResults.Add(vm);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    LoadCovers(cancellationToken);
                }
            }

            IsBusy = false;
        }

        private async void LoadCovers(CancellationToken cancellationToken)
        {
            foreach (var album in SearchResults)
            {
                await album.LoadCover();

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }
}
