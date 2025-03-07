﻿using System;
using System.Threading;
using System.Threading.Tasks;
using AssettoServer.Network.Tcp;
using AssettoServer.Server.Configuration;
using AssettoServer.Server.TrackParams;
using AssettoServer.Server.Weather.Implementation;
using Microsoft.Extensions.Hosting;
using NodaTime;
using NodaTime.Extensions;
using Serilog;
using SunCalcNet;
using SunCalcNet.Model;

namespace AssettoServer.Server.Weather;

public class WeatherManager : BackgroundService
{
    private readonly ACServerConfiguration _configuration;
    private readonly IWeatherImplementation _weatherImplementation;
    private readonly IWeatherTypeProvider _weatherTypeProvider;
    private readonly ITrackParamsProvider _trackParamsProvider;
    private readonly SessionManager _timeSource;
    private readonly RainHelper _rainHelper;

    public WeatherManager(IWeatherImplementation weatherImplementation, 
        IWeatherTypeProvider weatherTypeProvider, 
        ITrackParamsProvider trackParamsProvider, 
        ACServerConfiguration configuration, 
        SessionManager timeSource, 
        RainHelper rainHelper)
    {
        _weatherImplementation = weatherImplementation;
        _weatherTypeProvider = weatherTypeProvider;
        _trackParamsProvider = trackParamsProvider;
        _configuration = configuration;
        _timeSource = timeSource;
        _rainHelper = rainHelper;
    }

    public TrackParams.TrackParams? TrackParams { get; private set; }
    public WeatherData CurrentWeather { get; private set; } = null!;

    private ZonedDateTime _currentDateTime;
    public ZonedDateTime CurrentDateTime
    {
        get => _currentDateTime;
        set
        {
            _currentDateTime = value;
            UpdateSunPosition();
        }
    }

    public SunPosition? CurrentSunPosition { get; private set; }

    public void SetTime(float time)
    {
        CurrentDateTime = CurrentDateTime.Date.AtStartOfDayInZone(CurrentDateTime.Zone).PlusSeconds((long)time);
        UpdateSunPosition();
    }
    
    public void SetWeather(WeatherData weather)
    {
        CurrentWeather = weather;
        _weatherImplementation.SendWeather(CurrentWeather, CurrentDateTime);
    }
    
    public void SetCspWeather(WeatherFxType upcoming, int duration)
    {
        Log.Information("CSP weather transitioning to {UpcomingWeatherType}", upcoming);
            
        CurrentWeather.UpcomingType = _weatherTypeProvider.GetWeatherType(upcoming);
        CurrentWeather.TransitionValue = 0;
        CurrentWeather.TransitionValueInternal = 0;
        CurrentWeather.TransitionDuration = duration * 1000;

        _weatherImplementation.SendWeather(CurrentWeather, CurrentDateTime);
    }

    public void SendWeather(ACTcpClient? client = null) => _weatherImplementation.SendWeather(CurrentWeather, CurrentDateTime, client);

    private void UpdateSunPosition()
    {
        if (TrackParams != null)
        {
            CurrentSunPosition = SunCalc.GetSunPosition(CurrentDateTime.ToDateTimeUtc(), TrackParams.Latitude, TrackParams.Longitude);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _trackParamsProvider.Initialize();
        TrackParams = _trackParamsProvider.GetParamsForTrack(_configuration.Server.Track);

        DateTimeZone? timeZone;
        if (TrackParams == null)
        {
            if (_configuration.Extra.IgnoreConfigurationErrors.MissingTrackParams)
            {
                Log.Warning("Using UTC as default time zone");
                timeZone = DateTimeZone.Utc;
            }
            else
            {
                throw new ConfigurationException($"No track params found for {_configuration.Server.Track}. More info: https://github.com/compujuckel/AssettoServer/wiki/Common-configuration-errors#missing-track-params");
            }
        }
        else if (string.IsNullOrEmpty(TrackParams.Timezone))
        {
            if (_configuration.Extra.IgnoreConfigurationErrors.MissingTrackParams)
            {
                Log.Warning("Using UTC as default time zone");
                timeZone = DateTimeZone.Utc;
            }
            else
            {
                throw new ConfigurationException($"No time zone found for {_configuration.Server.Track}. More info: https://github.com/compujuckel/AssettoServer/wiki/Common-configuration-errors#missing-track-params");
            }
        }
        else
        {
            timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(TrackParams.Timezone);

            if (timeZone == null)
            {
                throw new ConfigurationException($"Invalid time zone {TrackParams.Timezone} for track {_configuration.Server.Track}. Please enter a valid time zone for your track in cfg/data_track_params.ini.");
            }
        }
        
        CurrentDateTime = SystemClock.Instance.InZone(timeZone).GetCurrentDate().AtStartOfDayInZone(timeZone).PlusSeconds((long)WeatherUtils.SecondsFromSunAngle(_configuration.Server.SunAngle));
        
        long lastTimeUpdate = _timeSource.ServerTimeMilliseconds;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_configuration.Extra.EnableRealTime)
                {
                    CurrentDateTime = SystemClock.Instance.InZone(CurrentDateTime.Zone).GetCurrentZonedDateTime();
                }
                else
                {
                    CurrentDateTime += Duration.FromMilliseconds((_timeSource.ServerTimeMilliseconds - lastTimeUpdate) * _configuration.Server.TimeOfDayMultiplier);
                }
                
                _rainHelper.Update(CurrentWeather, _configuration.Server.DynamicTrack?.BaseGrip ?? 1, _configuration.Extra.RainTrackGripReductionPercent, _timeSource.ServerTimeMilliseconds - lastTimeUpdate);
                _weatherImplementation.SendWeather(CurrentWeather, CurrentDateTime);
                lastTimeUpdate = _timeSource.ServerTimeMilliseconds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in weather service update");
            }
            finally
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
