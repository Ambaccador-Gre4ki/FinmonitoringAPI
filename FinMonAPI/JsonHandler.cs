using Serilog;

namespace FinMonAPI
{
    public class LoggingHandler : DelegatingHandler
    {
        private readonly string _envelopesDir;
        private static int _stepCounter = 1;

        public LoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
            // Создаем папку для конвертов рядом с исполняемым файлом
            _envelopesDir = Path.Combine(AppContext.BaseDirectory, "test-envelopes");
            Directory.CreateDirectory(_envelopesDir);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int currentStep = Interlocked.Increment(ref _stepCounter) / 2; // Счетчик шагов API
            string urlPath = request.RequestUri?.AbsolutePath ?? "unknown";

            // Определяем имя для файлов на основе метода
            string methodName = "unknown";
            if (urlPath.Contains("authenticate")) methodName = "1_auth";
            else if (urlPath.Contains("current-te2-catalog")) methodName = "2_te2_catalog";
            else if (urlPath.Contains("current-te2-file")) methodName = "3_te2_file";
            else if (urlPath.Contains("current-mvk-catalog")) methodName = "4_mvk_catalog";
            else if (urlPath.Contains("current-mvk-file")) methodName = "5_mvk_file";

            // 1 Сохранение запроса
            string requestFileName = $"{methodName}_request.json";
            string requestFilePath = Path.Combine(_envelopesDir, requestFileName);

            if (request.Content != null)
            {
                // Читаем тело JSON запроса
                string requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
                await File.WriteAllTextAsync(requestFilePath, requestBody, cancellationToken);
                Log.Debug("Сохранен конверт запроса: {File}", requestFileName);
            }
            else
            {
                // Если содержимого нет (пустой POST), то создаем пустой файл (как требует регламент)
                await File.WriteAllTextAsync(requestFilePath, "{}", cancellationToken);
                Log.Debug("Сохранен пустой конверт запроса: {File}", requestFileName);
            }

            // Передаем запрос дальше
            var response = await base.SendAsync(request, cancellationToken);

            // 2 Сохранение конверта ответа
            string responseFileName = $"{methodName}_response.json";
            string responseFilePath = Path.Combine(_envelopesDir, responseFileName);

            if (response.Content != null)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;

                // Если сервер вернул JSON, сохраняем его текст
                if (contentType != null && contentType.Contains("json"))
                {
                    string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    await File.WriteAllTextAsync(responseFilePath, responseBody, cancellationToken);
                    Log.Debug("Сохранен конверт ответа: {File}", responseFileName);
                }
                // Если сервер отдал бинарный ZIP или XML файл перечня, записываем заглушку типа данных
                else
                {
                    string stubText = $"{{\"status\": \"{(int)response.StatusCode}\", \"contentType\": \"{contentType}\", \"description\": \"Бинарный поток перечня (файл скачан на диск)\"}}";
                    await File.WriteAllTextAsync(responseFilePath, stubText, cancellationToken);
                    Log.Debug("Сохранен конверт ответа (бинарный поток): {File}", responseFileName);
                }
            }
            return response;
        }
    }
}