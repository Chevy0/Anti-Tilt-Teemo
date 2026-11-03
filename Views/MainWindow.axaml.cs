using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using NAudio.Wave;
using AntiTiltTeemo.ViewModels;

namespace AntiTiltTeemo.Views;

public partial class MainWindow : Window
{
    private readonly object _audioLock = new();
    private WaveOut? _hitAudioOutput;
    private Mp3FileReaderBase? _hitAudioReader;
    private int _audioRequest;
    private bool _isClosed;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnHitTeemo(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel ||
            !viewModel.HitTeemoCommand.CanExecute(null))
        {
            return;
        }

        viewModel.HitTeemoCommand.Execute(null);
        if (viewModel.LastTeemoDamageDealt > 0)
        {
            var soundAsset = viewModel.SelectedWeapon?.HitSoundAsset;
            if (soundAsset is not null)
            {
                PlayHitSound(soundAsset);
            }
        }
    }

    private void PlayHitSound(string soundAsset)
    {
        var request = Interlocked.Increment(ref _audioRequest);
        _ = Task.Run(() =>
        {
            try
            {
                var soundUri = new Uri($"avares://AntiTiltTeemo/Assets/{soundAsset}");
                if (!AssetLoader.Exists(soundUri))
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (!_isClosed && request == _audioRequest && DataContext is MainViewModel viewModel)
                        {
                            viewModel.AudioStatus = "OPTIONAL HIT SOUND NOT INSTALLED";
                        }
                    });
                    return;
                }

                lock (_audioLock)
                {
                    if (_isClosed || request != _audioRequest)
                    {
                        return;
                    }

                    _hitAudioOutput?.Stop();
                    if (_hitAudioOutput is not null)
                    {
                        _hitAudioOutput.PlaybackStopped -= OnHitAudioPlaybackStopped;
                    }
                    _hitAudioOutput?.Dispose();
                    _hitAudioReader?.Dispose();

                    var soundStream = AssetLoader.Open(soundUri);
                    _hitAudioReader = new HitSoundReader(soundStream);
                    _hitAudioOutput = new WaveOut();
                    _hitAudioOutput.Init(_hitAudioReader);
                    _hitAudioOutput.PlaybackStopped += OnHitAudioPlaybackStopped;
                    _hitAudioOutput.Play();
                }

                Dispatcher.UIThread.Post(() =>
                {
                    if (!_isClosed && request == _audioRequest && DataContext is MainViewModel viewModel)
                    {
                        viewModel.AudioStatus = $"PLAYING {soundAsset.ToUpperInvariant()}";
                    }
                });
            }
            catch (Exception exception) when (
                exception is IOException or InvalidOperationException or ArgumentException or
                    NotSupportedException or NAudio.MmException)
            {
                System.Diagnostics.Trace.TraceError($"Unable to play hit sound '{soundAsset}': {exception}");
                Dispatcher.UIThread.Post(() =>
                {
                    if (!_isClosed && request == _audioRequest && DataContext is MainViewModel viewModel)
                    {
                        viewModel.AudioStatus = "HIT SOUND UNAVAILABLE";
                    }
                });
            }
        });
    }

    private void OnHitAudioPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
        {
            System.Diagnostics.Trace.TraceError($"Hit sound playback failed: {e.Exception}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        lock (_audioLock)
        {
            _isClosed = true;
            _hitAudioOutput?.Stop();
            if (_hitAudioOutput is not null)
            {
                _hitAudioOutput.PlaybackStopped -= OnHitAudioPlaybackStopped;
            }
            _hitAudioOutput?.Dispose();
            _hitAudioReader?.Dispose();
        }

        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }

    private sealed class HitSoundReader(Stream stream)
        : Mp3FileReaderBase(stream, format => new AcmMp3FrameDecompressor(format), true);
}