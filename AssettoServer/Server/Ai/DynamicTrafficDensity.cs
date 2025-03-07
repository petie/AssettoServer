﻿using System;
using System.Threading;
using System.Threading.Tasks;
using AssettoServer.Server.Configuration;
using AssettoServer.Server.Weather;
using AssettoServer.Utils;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AssettoServer.Server.Ai;

public class DynamicTrafficDensity : BackgroundService
{
    private readonly ACServerConfiguration _configuration;
    private readonly WeatherManager _weatherManager;

    public DynamicTrafficDensity(ACServerConfiguration configuration, WeatherManager weatherManager)
    {
        _configuration = configuration;
        _weatherManager = weatherManager;
    }

    private float GetDensity(double hourOfDay)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (Math.Truncate(hourOfDay) == hourOfDay)
        {
            return _configuration.Extra.AiParams.HourlyTrafficDensity![(int)hourOfDay];
        }

        int lowerBound = (int)Math.Floor(hourOfDay);
        int higherBound = (int)Math.Ceiling(hourOfDay) % 24;

        return (float)MathUtils.Lerp(_configuration.Extra.AiParams.HourlyTrafficDensity![lowerBound], _configuration.Extra.AiParams.HourlyTrafficDensity![higherBound], hourOfDay - lowerBound);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                double hours = _weatherManager.CurrentDateTime.TimeOfDay.TickOfDay / 10_000_000.0 / 3600.0;
                _configuration.Extra.AiParams.TrafficDensity = GetDensity(hours);
                _configuration.TriggerReload();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in dynamic traffic density update");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMinutes(10.0 / _configuration.Server.TimeOfDayMultiplier), stoppingToken);
            }
        }
    }
}
