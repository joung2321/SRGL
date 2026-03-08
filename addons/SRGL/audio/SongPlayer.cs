namespace SRGL;

using Godot;
using System;

// [WARNING] read this documentation first:
// https://docs.godotengine.org/en/stable/tutorials/audio/sync_with_audio.html

// class controlling audio stream and playback position
public class SongPlayer
{
    private AudioStreamPlayer _asp;

    private long _resumedTicks; // ticks when Resume() called [us]
    private double _pausedPosition; // playback position when Pause() called [s]
    private long _pausedPositionUsec; // conversion of _pausedPosition [us]
    private bool _isFinished; // set true when _asp.Finished is fired

    /// <summary>
    /// [CAUTION] Playing is a distinct variable from _asp.Playing.
    /// </summary>
    public bool Playing { get; private set; }

    // wrapping of AudioStreamPlayer.Finished
    public event Action Finished
    {
        add { _asp.Finished += value; }
        remove { _asp.Finished -= value; }
    }

    public SongPlayer(Node parent)
    {
        ArgumentNullException.ThrowIfNull(parent);
        
        _asp = new AudioStreamPlayer();
        _asp.Finished += OnFinished;
        parent.AddChild(_asp);

        Init();
    }

    private void Init()
    {
        _pausedPosition = _pausedPositionUsec = 0;
        _isFinished = false;
        Playing = false;
    }

    public void LoadSong(string path)
    {
        _asp.Stream = (AudioStream)GD.Load(path);
        Init();
    }

    public void Resume()
    {
        if(Playing) { return; }
        Playing = true;
        
        long ticksUsec = (long)Time.GetTicksUsec();
        double mixDelay = AudioServer.GetTimeToNextMix();

        // play AudioStreamPlayer
        if(!_isFinished) { _asp.Play((float)_pausedPosition); }

        _resumedTicks = ticksUsec + (long)Math.Round(mixDelay * 1_000_000);
    }

    public void Pause()
    {
        if(!Playing) { return; }
        Playing = false;

        if(_asp.Playing)
        {
            // store playback position
            _pausedPosition = _asp.GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix();
            _pausedPositionUsec = (long)Math.Round(_pausedPosition * 1_000_000);

            // pause AudioStreamPlayer
            _asp.Stop();
        }
        else
        {
            long ticksUsec = (long)Time.GetTicksUsec();

            // calculate virtual paused position
            _pausedPositionUsec += ticksUsec - _resumedTicks;
            _pausedPosition = _pausedPositionUsec / 1_000_000.0;
        }
    }

    public void Stop()
    {
        _asp.Stop();
        Init();
    }

    private void OnFinished() { _isFinished = true; }

    /// <summary>
    /// Returns song time [us]<br/>
    /// e.g.) Getting current song time:
    /// <code>long time_us = GetSongTimeUsec((long)Time.GetTicksUsec());</code>
    /// [CAUTION] This method does NOT consider audio latency and audio drift.
    /// </summary>
    public long GetSongTimeUsec(long ticksUsec)
    {
        if(!Playing) { return _pausedPositionUsec; }
        else { return _pausedPositionUsec + (ticksUsec - _resumedTicks); }
    }
}