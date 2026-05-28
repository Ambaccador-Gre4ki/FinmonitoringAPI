using Microsoft.Extensions.Configuration;
using Serilog;
using System.Net.Http.Headers;

namespace FinMonAPI
{
    internal class Program
    {
        private const string BaseUrl = "https://portal.fedsfm.ru:8081/Services/fedsfm-service/";

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Зарузка настроек из appsettings.json           
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Читаем переменные из секции "RosFinMon"
            string? thumbprint = configuration["RosFinMon:Thumbprint"];
            string? login = configuration["RosFinMon:Login"];
            string? password = configuration["RosFinMon:Password"];
            string downloadFolder = configuration["RosFinMon:DownloadFolder"] ?? AppContext.BaseDirectory;
            string logsFolder = configuration["RosFinMon:LogsFolder"] ?? Path.Combine(downloadFolder, "logs");

            // Создаёт сетевую папку, если её ещё нет
            try
            {
                Directory.CreateDirectory(logsFolder);
                Directory.CreateDirectory(downloadFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка: Нет доступа к сетевой папке. {ex.Message}");
                return;
            }

            // Создание логгера
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() // Записываем Debug, Information, Warning, Error
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: Path.Combine(logsFolder, "log-.txt"),
                    rollingInterval: RollingInterval.Day, // Автоматический новый файл каждый день
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Приложение FinMonAPI успешно запущено.");

            Log.Debug("/// Загрузка конфигурации из appsettings.json ///");
            // Валидация: проверяем, что в конфигурации заполнены все поля
            if (string.IsNullOrWhiteSpace(thumbprint) || string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                throw new Exception("Критическая ошибка конфигурации: В appsettings.json не заполнены Thumbprint, Login или Password.");
            }

            try
            {
                Log.Information("=== Поиск сертификата ===");
                var cert = CertificateProvider.GetGostCertificate(thumbprint);
                Console.WriteLine($"Сертификат найден: {cert.Subject}\n");
                Log.Information("Сертификат успешно подключен: {Subject}", cert.Subject);

                Log.Information("=== Инициализация соединения с логгированием конвертов ===");
                // 1. Настраиваем базовый шлюз КриптоПро
                var baseHandler = new HttpClientHandler();
                baseHandler.ClientCertificates.Add(cert);
                baseHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                baseHandler.CheckCertificateRevocationList = false;

                // 2. Оборачиваем его в наш новый LoggingHandler для автоматической выгрузки конвертов
                var loggingHandler = new LoggingHandler(baseHandler);

                // 3. Создаем HttpClient, передавая loggingHandler
                using var httpClient = new HttpClient(loggingHandler);
                httpClient.BaseAddress = new Uri(BaseUrl);
                httpClient.Timeout = TimeSpan.FromMinutes(10);// Защита от таймаутов на больших файлах

                Log.Information("=== Запрос JWT-токена ===");
                var authService = new AuthService(httpClient);
                string accessToken = await authService.AuthenticateAsync(login, password);
                Log.Information("Токен получен");

                // Ставим Bearer-токен в заголовки для всех последующих запросов
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                Log.Information("=== Скачивание актуальных справочников ===");
                var catalogService = new CatalogService(httpClient);

                // Скачивание Перечня Террористов
                Log.Debug("/// Запрос актуального списка экстремистов ///");
                var te2Catalog = await catalogService.GetTe2CatalogAsync();
                if (te2Catalog != null && te2Catalog.IsActive)
                {
                    string path = Path.Combine(downloadFolder, "current_te2.zip");
                    Log.Information("Найден активный перечень ТЭ от {Date}. Скачивание файла...", te2Catalog.Date);
                    await catalogService.DownloadTe2FileAsync(te2Catalog.IdXml, path);
                    Log.Information("Файл ТЭ успешно сохранен: {Path}", path);
                }
                else
                {
                    Log.Warning("!!! Актуальный перечень ТЭ не найден или неактивен !!!");
                }
                //// Скачивание Русской версии Перечня ООН
                //Log.Debug("/// Запрос актуального каталога ООН (RU) ///");
                //var unRusCatalog = await catalogService.GetUnCatalogRusAsync();
                //if (unRusCatalog != null && unRusCatalog.IsActive)
                //{
                //    string path = Path.Combine(downloadFolder, "current_un_rus.xml");
                //    Log.Information("Найден активный перечень ООН (RU) от {Date}. Скачивание файла...", unRusCatalog.Date);
                //    await catalogService.DownloadUnFileAsync(unRusCatalog.IdXml, path);
                //    Log.Information("Файл ООН (RU) успешно сохранен: {Path}", path);
                //}
                //else
                //{
                //    Log.Warning("!!! Актуальный перечень ООН (RU) не найден или неактивен !!!");
                //}
                Log.Information("Все запланированные операции успешно завершены.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nПроизошла критическая ошибка:");
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null) Console.WriteLine($"Детали: {ex.InnerException.Message}");
                Log.Fatal(ex, "### Произошел критический сбой при выполнении программы ###");
                Console.ResetColor();
            }
            finally
            {
                Log.Information("Приложение завершает свою работу.");
                Log.CloseAndFlush(); // Принудительно сбрасываем буфер логов в файл на диске
            }
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }
}