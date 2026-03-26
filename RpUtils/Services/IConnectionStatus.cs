
using RpUtils.Models;
using System;
using System.Threading.Tasks;

namespace RpUtils.Services;

public interface IConnectionStatus
{
    ConnectionState Status { get; }
    bool IsConnected => Status == ConnectionState.Connected;
    event Action<ConnectionState>? OnStatusChanged;
    Task ConnectAsync();
    Task DisconnectAsync();
}
