using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarregaReceitasSalaProva.Interfaces
{
    public static class StatusMessageService
    {
        public static Action<object>? OnStatusMessage { get; internal set; }

        public static event Action<string>? StatusMessageReceived;

        public static void SendStatusMessage(string message)
        {
            StatusMessageReceived?.Invoke(message);
        }
    }
}
