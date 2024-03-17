using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Networking.Transports;

namespace Exanite.Networking
{
    public class NetworkServer : Network
    {
        private List<ITransport> transports = new();

        public override bool IsServer => true;
        public IReadOnlyList<ITransport> Transports => transports;

        public NetworkServer() {}

        public NetworkServer(IEnumerable<ITransport> transports) : this()
        {
            this.transports = new List<ITransport>(transports);
        }

        public override void Dispose()
        {
            foreach (var transport in transports)
            {
                transport.Dispose();
            }

            base.Dispose();
        }

        public override async UniTask StartConnection()
        {
            ValidateIsStopped();

            Status = LocalConnectionStatus.Starting;

            try
            {
                await UniTask.WhenAll(transports.Select(transport => StartTransport(transport)));
            }
            catch (Exception e)
            {
                StopConnection();

                throw new NetworkException($"Exception thrown while starting {GetType().Name}", e);
            }
        }

        public override void StopConnection()
        {
            foreach (var transport in transports)
            {
                transport.StopConnection();
                transport.SetNetwork(null);

                UnregisterTransportEvents(transport);
            }

            Status = LocalConnectionStatus.Stopped;
        }

        public void SetTransports(IEnumerable<ITransport> transports)
        {
            if (Status != LocalConnectionStatus.Stopped)
            {
                throw new NetworkException($"Setting transports is only possible when the {GetType().Name} is stopped");
            }

            this.transports.Clear();
            this.transports.AddRange(transports);
        }

        protected override bool AreAnyTransportsStopped()
        {
            foreach (var transport in transports)
            {
                if (transport.Status == LocalConnectionStatus.Stopped)
                {
                    return true;
                }
            }

            return false;
        }

        protected override void OnTickTransports()
        {
            base.OnTickTransports();

            foreach (var transport in transports)
            {
                transport.Tick();
            }
        }

        private async UniTask StartTransport(ITransport transport)
        {
            RegisterTransportEvents(transport);

            transport.SetNetwork(this);
            await transport.StartConnection();

            if (Status == LocalConnectionStatus.Stopped)
            {
                throw new NetworkException($"{GetType().Name} was stopped while starting transports");
            }

            // The Network is considered Started if one transport has started.
            // This is because one transport starting slowly should not block the others from communicating.
            Status = LocalConnectionStatus.Started;
        }
    }
}
