using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NotificacoesLibrary
{
    public class NotificacoesLibrary
    {
        public ClientWebSocket clientWebSocket { get; set; }
        public string _logFilePath { get; set; }
        public string _uriServer { get; set; }

        public NotificacoesLibrary()
        {
            clientWebSocket = new ClientWebSocket();
            _logFilePath = "C:\\Drogaleste\\Client\\Log\\Notificacoes";
        }

        public async Task Export(string[] args = null)
        {
            if (args.Length > 0)
                _uriServer = args[0];

            await ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            try
            {
                await clientWebSocket.ConnectAsync(new Uri(_uriServer), CancellationToken.None);
                Log("Conectado ao servidor WebSocket com sucesso.");
                SendToastNotification("Conectado", "Conexão estabelecida com o servidor WebSocket.");

                if (clientWebSocket.State == WebSocketState.Open)
                    await ReceiveAsync();



            }
            catch (Exception ex)
            {
                Log($"Erro ao conectar ao servidor WebSocket: {ex.Message}");
                clientWebSocket = new ClientWebSocket();

                await Task.Delay(TimeSpan.FromSeconds(5));

                if (clientWebSocket.State != WebSocketState.Open)
                    ConnectAsync();
            }
        }

        private async Task SendAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);
            await clientWebSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            Log($"Mensagem enviada: {message}");
        }

        private async Task ReceiveAsync()
        {
            try
            {
                while (clientWebSocket.State == WebSocketState.Open)
                {
                    var buffer = new byte[1024];
                    var result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var received = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Log($"MENSAGEM RECEBIDA: {received}");

                        if (IsJsonValid(received))
                        {
                            dynamic objetoDynamic = JsonConvert.DeserializeObject<dynamic>(received);
                            string title = objetoDynamic.title;
                            string message = objetoDynamic.message;

                            SendToastNotification(title, message);
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                Log($"Erro ao conectar ao servidor WebSocket: {ex.Message}");

                if (clientWebSocket.State != WebSocketState.Open)
                {
                    Log("Conexão perdida com o servidor. Tentando reconectar");

                    if (clientWebSocket.State != WebSocketState.Open)
                        await ConnectAsync();

                    if (clientWebSocket.State == WebSocketState.Open)
                        ReceiveAsync();

                }
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (clientWebSocket.State == WebSocketState.Open)
                {
                    await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Fechando a conexão.", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Log($"Erro ao desconectar do servidor WebSocket: {ex.Message}");
                // Você pode implementar lógica de tratamento de erro aqui
            }
        }

        public void SendToastNotification(string title, string message)
        {
            try
            {
                // Definir o caminho para o ícone do aplicativo
                string appLogoPath = "C:\\Drogaleste\\Client\\Modules\\icon.ico";
                string appLogoUrl = "https://raw.githubusercontent.com/juniioroliveira/DrogalesteSocketExtensions/main/icon.ico";


                // Criar a notificação do Toast
                var toastContentBuilder = new ToastContentBuilder()
                    .AddArgument("action", "viewConversation")
                    .AddArgument("conversationId", 9813)
                    .AddText(title)
                    .AddText(message);

                // Verificar se o arquivo de ícone existe antes de adicioná-lo
                if (File.Exists(appLogoPath))
                {
                    // Adicionar o ícone do aplicativo
                    toastContentBuilder.AddAppLogoOverride(new Uri($"file:///{appLogoPath}"));
                }

                // Adicionar o ícone do aplicativo a partir da URL
                //toastContentBuilder.AddAppLogoOverride(new Uri(appLogoUrl));


                // Exibir a notificação do Toast
                toastContentBuilder.Show();
            }
            catch (Exception ex)
            {
                Log($"Erro ao exibir notificação: {ex.Message}");
            }
        }

        public static bool IsJsonValid(string jsonString)
        {
            try
            {
                JToken.Parse(jsonString);

                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }

        private void Log(string message)
        {
            try
            {
                // Obtém a data atual no formato "dd-MM-yyyy" (dia-mês-ano).
                string currentDate = DateTime.Now.ToString("dd-MM-yyyy");

                // Concatena o diretório do log com a pasta da data e o nome do arquivo.
                string logDirectory = Path.Combine(_logFilePath, currentDate);
                string logFilePath = Path.Combine(logDirectory, "Notificacoes.log");

                // Verifica se o diretório do arquivo de log existe, caso contrário, cria-o.
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Grava o log no arquivo.
                File.AppendAllText(logFilePath, $"{DateTime.Now} - {message}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gravar o log: {ex.Message}");
            }
        }


    }
}
