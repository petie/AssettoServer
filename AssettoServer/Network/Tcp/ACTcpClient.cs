﻿using AssettoServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AssettoServer.Network.Packets.Incoming;
using AssettoServer.Network.Packets.Outgoing;
using AssettoServer.Network.Packets.Outgoing.Handshake;
using AssettoServer.Network.Packets.Shared;
using AssettoServer.Server;
using AssettoServer.Server.Configuration;
using AssettoServer.Server.Weather;
using NanoSockets;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AssettoServer.Network.Tcp
{
    public class ACTcpClient
    {
        public ACServer Server { get; }
        public byte SessionId { get; set; }
        public string? Name { get; private set; }
        public string? Team { get; private set; }
        public string? NationCode { get; private set; }
        public bool IsAdministrator { get; internal set; }
        public string? Guid { get; internal set; }
        [NotNull] public EntryCar? EntryCar { get; set; }
        public bool IsDisconnectRequested => _disconnectRequested == 1;

        public TcpClient TcpClient { get; }
        internal NetworkStream TcpStream { get; }
        internal bool HasSentFirstUpdate { get; private set; }
        [MemberNotNullWhen(true, nameof(Name), nameof(Team), nameof(NationCode), nameof(Guid))] internal bool HasStartedHandshake { get; private set; }
        internal bool HasPassedChecksum { get; private set; }
        internal bool IsConnected { get; set; }

        internal Address? UdpEndpoint { get; private set; }
        internal bool HasAssociatedUdp { get; private set; }

        private ThreadLocal<byte[]> UdpSendBuffer { get; }
        private Memory<byte> TcpSendBuffer { get; }
        private Channel<IOutgoingNetworkPacket> OutgoingPacketChannel { get; }
        private CancellationTokenSource DisconnectTokenSource { get; }
        [NotNull] private Task? SendLoopTask { get; set; }
        private long LastChatTime { get; set; }
        private int _disconnectRequested = 0;

        /// <summary>
        /// Fires when a client has started a handshake. At this point it is still possible to reject the connection by setting ClientHandshakeEventArgs.Cancel = true.
        /// </summary>
        public event EventHandler<ACTcpClient, ClientHandshakeEventArgs>? HandshakeStarted;
        
        /// <summary>
        /// Fires when a client passed the checksum checks. This does not mean that the player has finished loading, use ClientFirstUpdateSent for that.
        /// </summary>
        public event EventHandler<ACTcpClient, EventArgs>? ChecksumPassed;
        
        /// <summary>
        /// Fires when a client failed the checksum check.
        /// </summary>
        public event EventHandler<ACTcpClient, EventArgs>? ChecksumFailed;
        
        /// <summary>
        /// Fires when a client has sent a chat message. Set ChatEventArgs.Cancel = true to stop it from being broadcast to other players.
        /// </summary>
        public event EventHandler<ACTcpClient, ChatMessageEventArgs>? ChatMessageReceived;
        
        /// <summary>
        /// Fires when a player has started disconnecting.
        /// </summary>
        public event EventHandler<ACTcpClient, EventArgs>? Disconnecting;
        
        /// <summary>
        /// Fires when a client has sent the first position update and is visible to other players.
        /// </summary>
        public event EventHandler<ACTcpClient, EventArgs>? FirstUpdateSent;

        /// <summary>
        /// Fires when a client collided with something. TargetCar will be null for environment collisions.
        /// There are up to 5 seconds delay before a collision is reported to the server.
        /// </summary>
        public event EventHandler<ACTcpClient, CollisionEventArgs>? Collision;

        public ILogger Logger { get; }
        
        public class ACTcpClientLogEventEnricher : ILogEventEnricher
        {
            private readonly ACTcpClient _client;

            public ACTcpClientLogEventEnricher(ACTcpClient client)
            {
                _client = client;
            }
            
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                var endpoint = (IPEndPoint)_client.TcpClient.Client.RemoteEndPoint!;
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ClientName", _client.Name));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ClientSteamId", _client.Guid));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ClientIpAddress", endpoint.Address.ToString()));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ClientPort", endpoint.Port));
            }
        }

        internal ACTcpClient(ACServer server, TcpClient tcpClient)
        {
            Server = server;
            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.With(new ACTcpClientLogEventEnricher(this))
                .WriteTo.Logger(Log.Logger)
                .CreateLogger();

            UdpSendBuffer = new ThreadLocal<byte[]>(() => new byte[1500]);

            TcpClient = tcpClient;
            tcpClient.ReceiveTimeout = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;
            tcpClient.SendTimeout = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
            tcpClient.LingerState = new LingerOption(true, 2);

            TcpStream = tcpClient.GetStream();

            TcpSendBuffer = new byte[8192 + (server.CSPServerExtraOptions.EncodedWelcomeMessage.Length * 4) + 2];
            OutgoingPacketChannel = Channel.CreateBounded<IOutgoingNetworkPacket>(256);
            DisconnectTokenSource = new CancellationTokenSource();
        }

        internal Task StartAsync()
        {
            SendLoopTask = Task.Run(SendLoopAsync);
            _ = Task.Run(ReceiveLoopAsync);

            return Task.CompletedTask;
        }

        public void SendPacket<TPacket>(TPacket packet) where TPacket : IOutgoingNetworkPacket
        {
            try
            {
                if (!OutgoingPacketChannel.Writer.TryWrite(packet) && !(packet is SunAngleUpdate) && !IsDisconnectRequested)
                {
                    Logger.Warning("Cannot write packet to TCP packet queue for {ClientName}, disconnecting", Name);
                    _ = DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error sending {PacketName} to {ClientName}", typeof(TPacket).Name, Name);
            }
        }

        internal void SendPacketUdp<TPacket>(in TPacket packet) where TPacket : IOutgoingNetworkPacket
        {
            try
            {
                if (!UdpEndpoint.HasValue)
                {
                    throw new InvalidOperationException($"UDP endpoint not associated for {Name}");
                }
                
                byte[] buffer = UdpSendBuffer.Value!;
                PacketWriter writer = new PacketWriter(buffer);
                int bytesWritten = writer.WritePacket(in packet);

                Server.UdpServer.Send(UdpEndpoint.Value, buffer, 0, bytesWritten);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error sending {PacketName} to {ClientName}", typeof(TPacket).Name, Name);
            }
        }

        private async Task SendLoopAsync()
        {
            try
            {
                await foreach (var packet in OutgoingPacketChannel.Reader.ReadAllAsync(DisconnectTokenSource.Token))
                {
                    if (packet is not SunAngleUpdate)
                    {
                        if (packet is AuthFailedResponse authResponse)
                            Logger.Debug("Sending {PacketName} ({AuthResponseReason})", packet.GetType().Name, authResponse.Reason);
                        else if (packet is ChatMessage chatMessage && chatMessage.SessionId == 255)
                            Logger.Verbose("Sending {PacketName} ({ChatMessage}) to {ClientName}", packet.GetType().Name, chatMessage.Message, Name);
                        else
                            Logger.Verbose("Sending {PacketName} to {ClientName}", packet.GetType().Name, Name);
                    }

                    PacketWriter writer = new PacketWriter(TcpStream, TcpSendBuffer);
                    writer.WritePacket(packet);

                    await writer.SendAsync(DisconnectTokenSource.Token);
                }
            }
            catch (ChannelClosedException) { }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error sending TCP packet to {ClientName}", Name);
                _ = DisconnectAsync();
            }
        }

        private async Task ReceiveLoopAsync()
        {
            byte[] buffer = new byte[2046];
            NetworkStream stream = TcpStream;

            try
            {
                while (!DisconnectTokenSource.IsCancellationRequested)
                {
                    PacketReader reader = new PacketReader(stream, buffer);
                    reader.SliceBuffer(await reader.ReadPacketAsync());

                    if (reader.Buffer.Length == 0)
                        return;

                    byte id = reader.Read<byte>();
                    if (id != 0x82)
                        Logger.Verbose("Received TCP packet with ID {PacketId:X}", id);

                    if (!HasStartedHandshake && id != 0x3D)
                        return;

                    if (!HasStartedHandshake)
                    {
                        HandshakeRequest handshakeRequest = reader.ReadPacket<HandshakeRequest>();
                        if (handshakeRequest.Name.Length > 25)
                            handshakeRequest.Name = handshakeRequest.Name.Substring(0, 25);

                        Name = handshakeRequest.Name.Trim();

                        Logger.Information("{ClientName} ({ClientSteamId} - {ClientIpEndpoint}) is attempting to connect ({CarModel})", handshakeRequest.Name, handshakeRequest.Guid, TcpClient.Client.RemoteEndPoint?.ToString(), handshakeRequest.RequestedCar);

                        List<string> cspFeatures;
                        if (!string.IsNullOrEmpty(handshakeRequest.Features))
                        {
                            cspFeatures = handshakeRequest.Features.Split(',').ToList();
                            Logger.Debug("{ClientName} supports extra CSP features: {ClientFeatures}", handshakeRequest.Name, cspFeatures);
                        }
                        else
                        {
                            cspFeatures = new List<string>();
                        }

                        if (id != 0x3D || handshakeRequest.ClientVersion != 202)
                            SendPacket(new UnsupportedProtocolResponse());
                        else if (Server.IsGuidBlacklisted(handshakeRequest.Guid))
                            SendPacket(new BlacklistedResponse());
                        else if (Server.Configuration.Server.Password?.Length > 0 && handshakeRequest.Password != Server.Configuration.Server.Password && handshakeRequest.Password != Server.Configuration.Server.AdminPassword)
                            SendPacket(new WrongPasswordResponse());
                        else if (!Server.CurrentSession.Configuration.IsOpen)
                            SendPacket(new SessionClosedResponse());
                        else if ((Server.Configuration.Extra.EnableWeatherFx && !cspFeatures.Contains("WEATHERFX_V1"))
                                 || (Server.Configuration.Extra.UseSteamAuth && !cspFeatures.Contains("STEAM_TICKET")))
                            SendPacket(new AuthFailedResponse("Content Manager version not supported. Please update Content Manager to v0.8.2329.38887 or above."));
                        else if (Server.Configuration.Extra.UseSteamAuth && !await Server.Steam.ValidateSessionTicketAsync(handshakeRequest.SessionTicket, handshakeRequest.Guid, this))
                            SendPacket(new AuthFailedResponse("Steam authentication failed."));
                        else if (string.IsNullOrEmpty(handshakeRequest.Guid) || !(handshakeRequest.Guid.Length >= 6))
                            SendPacket(new AuthFailedResponse("Invalid Guid."));
                        else if (!await Server.TrySecureSlotAsync(this, handshakeRequest))
                            SendPacket(new NoSlotsAvailableResponse());
                        else
                        {
                            if (EntryCar == null)
                                throw new InvalidOperationException("No EntryCar set even though handshake started");
                            
                            var args = new ClientHandshakeEventArgs
                            {
                                HandshakeRequest = handshakeRequest
                            };
                            HandshakeStarted?.Invoke(this, args);

                            if (args.Cancel)
                            {
                                if (args.CancelType == ClientHandshakeEventArgs.CancelTypeEnum.Blacklisted)
                                    SendPacket(new BlacklistedResponse());
                                else if (args.CancelType == ClientHandshakeEventArgs.CancelTypeEnum.AuthFailed)
                                    SendPacket(new AuthFailedResponse(args.AuthFailedReason ?? "No reason specified"));

                                return;
                            }

                            EntryCar.SetActive();
                            Team = handshakeRequest.Team;
                            NationCode = handshakeRequest.Nation;
                            Guid = handshakeRequest.Guid;

                            // Gracefully despawn AI cars
                            EntryCar.SetAiOverbooking(0);

                            if (handshakeRequest.Password == Server.Configuration.Server.AdminPassword)
                                IsAdministrator = true;

                            Logger.Information("{ClientName} ({ClientSteamId}, {SessionId} ({Car})) has connected", Name, Guid, SessionId, EntryCar.Model + "-" + EntryCar.Skin);

                            var cfg = Server.Configuration.Server;
                            HandshakeResponse handshakeResponse = new HandshakeResponse
                            {
                                ABSAllowed = cfg.ABSAllowed,
                                TractionControlAllowed = cfg.TractionControlAllowed,
                                AllowedTyresOutCount = cfg.AllowedTyresOutCount,
                                AllowTyreBlankets = cfg.AllowTyreBlankets,
                                AutoClutchAllowed = cfg.AutoClutchAllowed,
                                CarModel = "ks_mazda_miata",
                                CarSkin = "00_classic_red",
                                FuelConsumptionRate = cfg.FuelConsumptionRate,
                                HasExtraLap = cfg.HasExtraLap,
                                InvertedGridPositions = cfg.InvertedGridPositions,
                                IsGasPenaltyDisabled = cfg.IsGasPenaltyDisabled,
                                IsVirtualMirrorForced = cfg.IsVirtualMirrorForced,
                                JumpStartPenaltyMode = cfg.JumpStartPenaltyMode,
                                MechanicalDamageRate = cfg.MechanicalDamageRate,
                                PitWindowEnd = cfg.PitWindowEnd,
                                PitWindowStart = cfg.PitWindowStart,
                                StabilityAllowed = cfg.StabilityAllowed,
                                RaceOverTime = cfg.RaceOverTime,
                                RefreshRateHz = cfg.RefreshRateHz,
                                ResultScreenTime = cfg.ResultScreenTime,
                                ServerName = cfg.Name,
                                SessionId = SessionId,
                                SunAngle = (float)WeatherUtils.SunAngleFromTicks(Server.CurrentDateTime.TimeOfDay.TickOfDay),
                                TrackConfig = cfg.TrackConfig,
                                TrackName = cfg.Track,
                                TyreConsumptionRate = cfg.TyreConsumptionRate,
                                UdpPort = cfg.UdpPort,
                                CurrentSession = Server.CurrentSession,
                                ChecksumCount = (byte)Server.TrackChecksums.Count,
                                ChecksumPaths = Server.TrackChecksums.Keys,
                                CurrentTime = Server.CurrentTime,
                                LegalTyres = cfg.LegalTyres,
                                RandomSeed = 123,
                                SessionCount = (byte)Server.Configuration.Sessions.Count,
                                Sessions = Server.Configuration.Sessions,
                                SpawnPosition = SessionId,
                                TrackGrip = Math.Clamp(cfg.DynamicTrack != null ? cfg.DynamicTrack.BaseGrip + (cfg.DynamicTrack.GripPerLap * cfg.DynamicTrack.TotalLapCount) : 1, 0, 1),
                                MaxContactsPerKm = cfg.MaxContactsPerKm
                            };

                            HasStartedHandshake = true;
                            SendPacket(handshakeResponse);

                            _ = Task.Delay(TimeSpan.FromMinutes(10)).ContinueWith(async _ =>
                            {
                                if (EntryCar.Client == this && IsConnected && !HasSentFirstUpdate)
                                {
                                    Logger.Information("{ClientName} has taken over 10 minutes to spawn in and will be disconnected", Name);
                                    await DisconnectAsync();
                                }
                            });
                        }

                        if (!HasStartedHandshake)
                            return;
                    }
                    else if (HasStartedHandshake)
                    {
                        if (id == 0x3F)
                            OnCarListRequest(reader);
                        else if (id == 0xD)
                            OnP2PUpdate(reader);
                        else if (id == 0x50)
                            OnTyreCompoundChange(reader);
                        else if (id == 0x43)
                            return;
                        else if (id == 0x47)
                            OnChat(reader);
                        else if (id == 0x44)
                            await OnChecksumAsync(reader);
                        else if (id == 0x49)
                            OnLapCompletedMessageReceived(reader);
                        else if (id == 0xAB)
                        {
                            id = reader.Read<byte>();
                            Logger.Verbose("Received extended TCP packet with ID {PacketId:X}", id);

                            if (id == 0x00)
                                OnSpectateCar(reader);
                            else if (id == 0x03)
                                OnCSPClientMessage(reader);
                        }
                        else if (id == 0x82)
                            OnClientEvent(reader);
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (IOException) { }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error receiving TCP packet from {ClientName}", Name);
            }
            finally
            {
                await DisconnectAsync();
            }
        }

        private void OnClientEvent(PacketReader reader)
        {
            ClientEvent clientEvent = reader.ReadPacket<ClientEvent>();

            foreach (var evt in clientEvent.ClientEvents)
            {
                EntryCar? targetCar = null;
                
                if (evt.Type == ClientEvent.ClientEventType.PlayerCollision)
                {
                    targetCar = Server.EntryCars[evt.TargetSessionId];
                    Logger.Information("Collision between {SourceCarName} ({SourceCarSessionId}) and {TargetCarName} ({TargetCarSessionId}), rel. speed {Speed:F0}km/h", 
                        Name, EntryCar.SessionId, targetCar.Client?.Name ?? targetCar.AiName, targetCar.SessionId, evt.Speed);
                }
                else
                {
                    Logger.Information("Collision between {SourceCarName} ({SourceCarSessionId}) and environment, rel. speed {Speed:F0}km/h", 
                        Name, EntryCar.SessionId, evt.Speed);
                }
                
                Collision?.Invoke(this, new CollisionEventArgs(targetCar, evt.Speed, evt.Position, evt.RelPosition));
            }
        }

        private void OnSpectateCar(PacketReader reader)
        {
            SpectateCar spectatePacket = reader.ReadPacket<SpectateCar>();
            EntryCar.TargetCar = spectatePacket.SessionId != SessionId ? Server.EntryCars[spectatePacket.SessionId] : null;
        }

        private void OnCSPClientMessage(PacketReader reader)
        {
            CSPClientMessageType packetType = (CSPClientMessageType)reader.Read<ushort>();
            if (packetType == CSPClientMessageType.LuaMessage)
            {
                int luaPacketType = reader.Read<int>();

                if (Server.CSPClientMessageTypes.TryGetValue(luaPacketType, out var handler))
                {
                    handler(this, reader);
                }
                else
                {
                    CSPClientMessage clientMessage = reader.ReadPacket<CSPClientMessage>();
                    clientMessage.Type = packetType;
                    clientMessage.LuaType = luaPacketType;
                    clientMessage.SessionId = SessionId;

                    Logger.Debug("Unknown CSP lua client message with type 0x{LuaType:X} received, data {Data}", clientMessage.LuaType, Convert.ToHexString(clientMessage.Data));
                    Server.BroadcastPacket(clientMessage);
                }
            }
            else
            {
                CSPClientMessage clientMessage = reader.ReadPacket<CSPClientMessage>();
                clientMessage.Type = packetType;
                clientMessage.SessionId = SessionId;
                
                Server.BroadcastPacket(clientMessage);
            }
        }

        private async ValueTask OnChecksumAsync(PacketReader reader)
        {
            bool passedChecksum = false;
            byte[] fullChecksum = new byte[16 * (Server.TrackChecksums.Count + 1)];
            if (reader.Buffer.Length == fullChecksum.Length + 1)
            {
                reader.ReadBytes(fullChecksum);
                passedChecksum = !Server.CarChecksums.TryGetValue(EntryCar.Model, out byte[]? modelChecksum) || fullChecksum.AsSpan().Slice(fullChecksum.Length - 16).SequenceEqual(modelChecksum);

                KeyValuePair<string, byte[]>[] allChecksums = Server.TrackChecksums.ToArray();
                for (int i = 0; i < allChecksums.Length; i++)
                    if (!allChecksums[i].Value.AsSpan().SequenceEqual(fullChecksum.AsSpan().Slice(i * 16, 16)))
                    {
                        Logger.Information("{ClientName} failed checksum for file {ChecksumFile}", Name, allChecksums[i].Key);
                        passedChecksum = false;
                        break;
                    }
            }

            HasPassedChecksum = passedChecksum;
            if (!passedChecksum)
            {
                ChecksumFailed?.Invoke(this, EventArgs.Empty);
                await Server.KickAsync(this, KickReason.ChecksumFailed, $"{Name} failed the checksum check and has been kicked.", false);
            }
            else
            {
                ChecksumPassed?.Invoke(this, EventArgs.Empty);

                Server.BroadcastPacket(new CarConnected
                {
                    SessionId = SessionId,
                    Name = Name,
                    Nation = NationCode
                }, this);
            }
        }

        private void OnChat(PacketReader reader)
        {
            if (Environment.TickCount64 - LastChatTime < 1000)
                return;
            LastChatTime = Environment.TickCount64;

            if (Server.Configuration.Extra.AfkKickBehavior == AfkKickBehavior.PlayerInput)
            {
                EntryCar.SetActive();
            }

            ChatMessage chatMessage = reader.ReadPacket<ChatMessage>();
            chatMessage.SessionId = SessionId;
            
            Logger.Information("CHAT: {ClientName} ({SessionId}): {ChatMessage}", Name, SessionId, chatMessage.Message);

            var args = new ChatMessageEventArgs
            {
                ChatMessage = chatMessage
            };
            ChatMessageReceived?.Invoke(this, args);
        }

        private void OnTyreCompoundChange(PacketReader reader)
        {
            TyreCompoundChangeRequest compoundChangeRequest = reader.ReadPacket<TyreCompoundChangeRequest>();
            EntryCar.Status.CurrentTyreCompound = compoundChangeRequest.CompoundName;

            Server.BroadcastPacket(new TyreCompoundUpdate
            {
                CompoundName = compoundChangeRequest.CompoundName,
                SessionId = SessionId
            });
        }

        private void OnP2PUpdate(PacketReader reader)
        {
            // ReSharper disable once InconsistentNaming
            P2PUpdateRequest p2pUpdateRequest = reader.ReadPacket<P2PUpdateRequest>();
            if (p2pUpdateRequest.P2PCount == -1)
                SendPacket(new P2PUpdate
                {
                    Active = false,
                    P2PCount = EntryCar.Status.P2PCount,
                    SessionId = SessionId
                });
            else
                Server.BroadcastPacket(new P2PUpdate
                {
                    Active = EntryCar.Status.P2PActive,
                    P2PCount = EntryCar.Status.P2PCount,
                    SessionId = SessionId
                });
        }

        private void OnCarListRequest(PacketReader reader)
        {
            CarListRequest carListRequest = reader.ReadPacket<CarListRequest>();
            Logger.Debug($"Received CarListRequest: {carListRequest.PageIndex} page");

            List<EntryCar> carsInPage = Server.SelectionCars.Skip(carListRequest.PageIndex).Take(10).ToList();
            foreach (EntryCar car in carsInPage) {
                Logger.Debug($"{car.SessionId} {car.Model}");
            }
            CarListResponse carListResponse = new CarListResponse()
            {
                PageIndex = carListRequest.PageIndex,
                EntryCarsCount = carsInPage.Count,
                EntryCars = carsInPage
            };

            SendPacket(carListResponse);
        }

        private void OnLapCompletedMessageReceived(PacketReader reader)
        {
            LapCompletedIncoming lapPacket = reader.ReadPacket<LapCompletedIncoming>();

            //Server.Configuration.DynamicTrack.TotalLapCount++; // TODO reset at some point
            if (OnLapCompleted(lapPacket))
            {
                Server.SendLapCompletedMessage(SessionId, lapPacket.LapTime, lapPacket.Cuts);
            }
        }

        private bool OnLapCompleted(LapCompletedIncoming lap)
        {
            int timestamp = Server.CurrentTime;

            var entryCarResult = Server.CurrentSession.Results?[SessionId] ?? throw new InvalidOperationException("Current session does not have results set");

            if (entryCarResult.HasCompletedLastLap)
            {
                Logger.Debug("Lap rejected by {ClientName}, already finished", Name);
                return false;
            }

            if (Server.CurrentSession.Configuration.Type == SessionType.Race && entryCarResult.NumLaps >= Server.CurrentSession.Configuration.Laps && !Server.CurrentSession.Configuration.IsTimedRace)
            {
                Logger.Debug("Lap rejected by {ClientName}, race over", Name);
                return false;
            }

            Logger.Information("Lap completed by {ClientName}, {NumCuts} cuts, laptime {LapTime}", Name, lap.Cuts, lap.LapTime);

            // TODO unfuck all of this

            if (Server.CurrentSession.Configuration.Type == SessionType.Race || lap.Cuts == 0)
            {
                entryCarResult.LastLap = lap.LapTime;
                if (lap.LapTime < entryCarResult.BestLap)
                {
                    entryCarResult.BestLap = lap.LapTime;
                }

                entryCarResult.NumLaps++;
                if (entryCarResult.NumLaps > Server.CurrentSession.LeaderLapCount)
                {
                    Server.CurrentSession.LeaderLapCount = entryCarResult.NumLaps;
                }

                entryCarResult.TotalTime = Server.CurrentSession.SessionTimeTicks - (EntryCar.Ping / 2);

                if (Server.CurrentSession.SessionOverFlag)
                {
                    if (Server.CurrentSession.Configuration.Type == SessionType.Race && Server.CurrentSession.Configuration.IsTimedRace)
                    {
                        if (Server.Configuration.Server.HasExtraLap)
                        {
                            if (entryCarResult.NumLaps <= Server.CurrentSession.LeaderLapCount)
                            {
                                entryCarResult.HasCompletedLastLap = Server.CurrentSession.LeaderHasCompletedLastLap;
                            }
                            else if (Server.CurrentSession.TargetLap > 0)
                            {
                                if (entryCarResult.NumLaps >= Server.CurrentSession.TargetLap)
                                {
                                    Server.CurrentSession.LeaderHasCompletedLastLap = true;
                                    entryCarResult.HasCompletedLastLap = true;
                                }
                            }
                            else
                            {
                                Server.CurrentSession.TargetLap = entryCarResult.NumLaps + 1;
                            }
                        }
                        else if (entryCarResult.NumLaps <= Server.CurrentSession.LeaderLapCount)
                        {
                            entryCarResult.HasCompletedLastLap = Server.CurrentSession.LeaderHasCompletedLastLap;
                        }
                        else
                        {
                            Server.CurrentSession.LeaderHasCompletedLastLap = true;
                            entryCarResult.HasCompletedLastLap = true;
                        }
                    }
                    else
                    {
                        entryCarResult.HasCompletedLastLap = true;
                    }
                }

                if (Server.CurrentSession.Configuration.Type != SessionType.Race)
                {
                    if (Server.CurrentSession.EndTime != 0)
                    {
                        entryCarResult.HasCompletedLastLap = true;
                    }
                }
                else if (Server.CurrentSession.Configuration.IsTimedRace)
                {
                    if (Server.CurrentSession.LeaderHasCompletedLastLap && Server.CurrentSession.EndTime == 0)
                    {
                        Server.CurrentSession.EndTime = timestamp;
                    }
                }
                else if (entryCarResult.NumLaps != Server.CurrentSession.Configuration.Laps)
                {
                    if (Server.CurrentSession.EndTime != 0)
                    {
                        entryCarResult.HasCompletedLastLap = true;
                    }
                }
                else if (!entryCarResult.HasCompletedLastLap)
                {
                    entryCarResult.HasCompletedLastLap = true;
                    if (Server.CurrentSession.EndTime == 0)
                    {
                        Server.CurrentSession.EndTime = timestamp;
                    }
                }
                else if (Server.CurrentSession.EndTime != 0)
                {
                    entryCarResult.HasCompletedLastLap = true;
                }

                return true;
            }

            if (Server.CurrentSession.EndTime == 0)
                return true;

            entryCarResult.HasCompletedLastLap = true;
            return false;
        }

        internal void SendFirstUpdate()
        {
            if (HasSentFirstUpdate)
                return;

            TcpClient.ReceiveTimeout = 0;
            EntryCar.LastPongTime = Server.CurrentTime;
            HasSentFirstUpdate = true;

            List<EntryCar> connectedCars = Server.EntryCars.Where(c => c.Client != null || c.AiControlled).ToList();

            if (!string.IsNullOrEmpty(Server.CSPServerExtraOptions.EncodedWelcomeMessage))
                SendPacket(new WelcomeMessage { Message = Server.CSPServerExtraOptions.EncodedWelcomeMessage });

            SendPacket(new DriverInfoUpdate { ConnectedCars = connectedCars });
            Server.WeatherImplementation.SendWeather(this);

            foreach (EntryCar car in connectedCars)
            {
                SendPacket(new MandatoryPitUpdate { MandatoryPit = car.Status.MandatoryPit, SessionId = car.SessionId });
                if (car != EntryCar)
                    SendPacket(new TyreCompoundUpdate { SessionId = car.SessionId, CompoundName = car.Status.CurrentTyreCompound });

                if (Server.Configuration.Extra.AiParams.HideAiCars)
                {
                    SendPacket(new CSPCarVisibilityUpdate
                    {
                        SessionId = car.SessionId,
                        Visible = car.AiControlled ? CSPCarVisibility.Invisible : CSPCarVisibility.Visible
                    });
                }
            }

            Server.SendLapCompletedMessage(255, 0, 0, this);

            _ = Task.Delay(40000).ContinueWith(async _ =>
            {
                if (!HasPassedChecksum && IsConnected)
                {
                    await Server.KickAsync(this, KickReason.ChecksumFailed, $"{Name} did not send the requested checksums.", false);
                }
            });
            
            FirstUpdateSent?.Invoke(this, EventArgs.Empty);
        }

        internal bool TryAssociateUdp(Address endpoint)
        {
            if (HasAssociatedUdp)
                return false;

            UdpEndpoint = endpoint;
            HasAssociatedUdp = true;

            return true;
        }

        internal async Task DisconnectAsync()
        {
            try
            {
                if (Interlocked.CompareExchange(ref _disconnectRequested, 1, 0) == 1)
                    return;

                if (!string.IsNullOrEmpty(Name))
                {
                    Logger.Debug("Disconnecting {ClientName} ({$ClientIpEndpoint})", Name, TcpClient.Client.RemoteEndPoint);
                    Disconnecting?.Invoke(this, EventArgs.Empty);
                }

                OutgoingPacketChannel.Writer.TryComplete();
                _ = await Task.WhenAny(Task.Delay(2000), SendLoopTask);

                try
                {
                    DisconnectTokenSource.Cancel();
                    DisconnectTokenSource.Dispose();
                }
                catch (ObjectDisposedException) { }

                if (IsConnected)
                    await Server.DisconnectClientAsync(this);

                TcpClient.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error disconnecting {ClientName}", Name);
            }
        }
    }
}
