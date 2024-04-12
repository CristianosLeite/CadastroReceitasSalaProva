using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using CarregaReceitasSalaProva.Database;
using CarregaReceitasSalaProva.Interfaces;
using Sharp7;
using Sharp7.Rx;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Interfaces;

namespace CarregaReceitasSalaProva
{
    public class Plc
    {
        public static System.Timers.Timer Set(System.Action action, int interval)
        {
            var timer = new System.Timers.Timer(interval);
            timer.Elapsed += (s, e) =>
            {
                timer.Enabled = false;
                action();
                timer.Enabled = true;
            };
            timer.Enabled = true;
            return timer;
        }

        private readonly Sharp7Plc _client;
        private readonly string _ip = "192.168.100.111";
        private readonly int _rack = 0;
        private readonly int _slot = 2;
        private readonly Db db = new(DatabaseConfig.ConnectionString);
        private readonly TagManager _tagManager = new();
        private readonly string LoadedRecipeTag = "DB600.DBX1662.0";

        public Plc()
        {
            _client = new Sharp7Plc(_ip, _rack, _slot);
            StatusMessageService.SendStatusMessage("Conectando ao Plc...");

            Connect();
        }

        public async void Connect()
        {
            try
            {
                // initialize the plc
                await _client.InitializeAsync();

                //wait for the plc to get connected
                await _client
                    .ConnectionState.FirstAsync(c => c == Sharp7.Rx.Enums.ConnectionState.Connected)
                    .Timeout(TimeSpan.FromSeconds(5))
                    .ToTask();

                CheckConnection();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                StatusMessageService.SendStatusMessage("Falha ao conectar ao Plc");

                //Try to connect again
                System.Timers.Timer timer = Set(
                    () =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessageService.SendStatusMessage(
                                "Tentando conectar novamente..."
                            );
                        });
                    },
                    2000
                );
                Connect();
            }
        }

        public async void CheckConnection()
        {
            ConnectionState connection = await _client
                .ConnectionState.Timeout(TimeSpan.FromSeconds(5))
                .FirstAsync();

            try
            {
                await _client
                    .ConnectionState.Where(predicate: state => state == ConnectionState.Connected)
                    .Do(ConnectionState =>
                        StatusMessageService.SendStatusMessage("Conectado ao Plc")
                    )
                    .Timeout(TimeSpan.FromSeconds(5))
                    .FirstAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return;
            }

            if (connection == ConnectionState.ConnectionLost)
                StatusMessageService.SendStatusMessage("Conexão perdida com o Plc");

            if (connection == ConnectionState.DisconnectedByUser)
                StatusMessageService.SendStatusMessage("Desconectado do Plc");
        }

        private int GetColumn()
        {
            ObservableCollection<string> cycle = db.Cycle();
            if (cycle[0] == "5Min")
                return 1;
            else if (cycle[0] == "12Min")
                return 2;

            return 0;
        }

        public async void HandleFloats(string tag, float value)
        {
            byte[] bytes = new byte[4];
            bytes.SetRealAt(0, value);
            await _client.SetValue(tag, bytes);
        }

        public async void HandleNumbers(string tag, int value)
        {
            try
            {
                byte[] bytes = BitConverter.GetBytes(value);
                bytes = [bytes[1], bytes[0]];

                //Check if the tag is timer
                if (tag.Contains("DINT"))
                {
                    await _client.SetValue(tag, value * 1000);
                    return;
                }
                //Check if the tag is a real
                else if (!tag.Contains("INT"))
                {
                    HandleFloats(tag, value);
                    return;
                }

                await _client.SetValue(tag, bytes);
            }
            catch (Exception)
            {
                return;
            }
        }

        private async void WriteToPlc(string tag, string value)
        {
            value = value.Replace(".", ",");
            if (int.TryParse(value, out int intValue))
            {
                HandleNumbers(tag, intValue);
            }
            else if (float.TryParse(value, out float decimalValue))
            {
                HandleFloats(tag, (float)decimalValue);
            }
            else
            {
                //Value is a string
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                await _client.SetValue(tag, bytes);
            }
        }

        public async void LoadRecipeToPlc()
        {
            StatusMessageService.SendStatusMessage("Carregando receita...");
            //Load recipe from database
            ObservableCollection<string> recipeName = db.RecipeToLoad();
            System.Data.DataTable recipe = db.LoadRecipeFromDatabase(recipeName[0]);

            //Get number of the column to read data
            int column = GetColumn();

            if (column == 0)
            {
                StatusMessageService.SendStatusMessage("Receita não encontrada");
                return;
            }

            //Write data to plc
            foreach (System.Data.DataRow row in recipe.Rows)
            {
                foreach (var tag in _tagManager.Tags)
                {
                    if (tag.Key == row[0].ToString())
                    {
                        if (!string.IsNullOrEmpty(row[column].ToString()))
                            WriteToPlc(tag.Value, value: row[column].ToString()!);
                    }
                }
            }

            await _client.SetValue(LoadedRecipeTag, true);
            await _client
                .CreateNotification<bool>(
                    LoadedRecipeTag,
                    TransmissionMode.OnChange,
                    TimeSpan.FromMilliseconds(100)
                )
                .Where(b => b)
                .Do(b => StatusMessageService.SendStatusMessage("Receita carregada com sucesso!"))
                .Timeout(TimeSpan.FromSeconds(5))
                .FirstAsync();
        }
    }
}
