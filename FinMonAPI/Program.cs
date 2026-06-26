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

                Log.Information("=== Инициализация соединения ===");
                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(cert);

                using var httpClient = new HttpClient(handler);
                httpClient.BaseAddress = new Uri(BaseUrl);
                httpClient.Timeout = TimeSpan.FromMinutes(10); // Защита от таймаутов на больших файлах

                Log.Information("=== Запрос JWT-токена ===");
                var authService = new AuthService(httpClient);
                string accessToken = await authService.AuthenticateAsync(login, password);

                // Ставим Bearer-токен в заголовки для всех последующих запросов
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                Log.Information("=== Скачивание актуальных справочников ===");
                var catalogService = new CatalogService(httpClient);

                // Скачивание Перечня Террористов
                Log.Debug("/// Запрос актуального списка экстремистов ///");
                var te21Catalog = await catalogService.GetTe21CatalogAsync();
                if (te21Catalog != null && te21Catalog.IsActive)
                {
                    string stamp_te21 = te21Catalog.Date.ToString("yyyy-MM-dd");
                    string fileNameTe21 = $"current_te21_{stamp_te21}.zip";
                    string path_te21 = Path.Combine(downloadFolder, fileNameTe21);
                    Log.Information("Найден активный перечень ТЭ от {Date}. Скачивание файла...", te21Catalog.Date);
                    await catalogService.DownloadTe21FileAsync(te21Catalog.IdXml, path_te21);
                    Log.Information("Файл ТЭ успешно сохранен: {Path}", path_te21);
                }
                else
                {
                    Log.Warning("!!! Актуальный перечень ТЭ не найден или неактивен !!!");
                }
                try
                {
                    // Скачивание русской версии Перечня ООН
                    Log.Debug("/// Запрос актуального каталога ООН (RU) ///");
                    var unRusCatalog = await catalogService.GetUnCatalogRusAsync();
                    if (unRusCatalog != null && unRusCatalog.IsActive)
                    {
                        string stamp_unRus = unRusCatalog.Date.ToString("yyyy-MM-dd");
                        string fileNameUnRus = $"current_UnRUS_{stamp_unRus}.xml";
                        string path_unRus = Path.Combine(downloadFolder, fileNameUnRus);
                        Log.Information("Найден активный перечень ООН (RU) от {Date}. Скачивание файла...", unRusCatalog.Date);
                        await catalogService.DownloadUnFileAsync(unRusCatalog.IdXml, path_unRus);
                        Log.Information("Файл ООН (RU) успешно сохранен: {Path}", path_unRus);
                    }
                    else
                    {
                        Log.Warning("!!! Актуальный перечень ООН (RU) не найден или неактивен !!!");
                    }

                    // Скачивание английской версии Перечня ООН
                    Log.Debug("/// Запрос актуального каталога ООН (EN) ///");
                    var unEnCatalog = await catalogService.GetUnCatalogAsync();
                    if (unEnCatalog != null && unEnCatalog.IsActive)
                    {
                        string stamp_unEn = unEnCatalog.Date.ToString("yyyy-MM-dd");
                        string fileNameUnEn = $"current_UnENG_{stamp_unEn}.xml";
                        string path_unEn = Path.Combine(downloadFolder, fileNameUnEn);
                        Log.Information("Найден активный перечень ООН (EN) от {Date}. Скачивание файла...", unEnCatalog.Date);
                        await catalogService.DownloadUnFileAsync(unEnCatalog.IdXml, path_unEn);
                        Log.Information("Файл ООН (EN) успешно сохранен: {Path}", path_unEn);
                    }
                    else
                    {
                        Log.Warning("!!! Актуальный перечень ООН (EN) не найден или неактивен !!!");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Не удалось скачать перечень ООН");
                }
                Log.Debug("/// Запрос актуального перечня МВК");
                var mvkCatalog = await catalogService.GetMVKCatalogAsync();
                if (mvkCatalog != null && mvkCatalog.IsActive == true)
                {
                    string stamp_mvk = mvkCatalog.Date.ToString("yyyy-MM-dd");
                    string fileNameMVK = $"current_mvk_{stamp_mvk}.zip";
                    string path_mvk = Path.Combine(downloadFolder, fileNameMVK);
                    Log.Information("Найден активный перечень МВК от {Date}. Скачивание файла...", mvkCatalog.Date);
                    await catalogService.DownloadMVKFileAsync(mvkCatalog.IdXml, path_mvk);
                    Log.Information("Файл МВК успешно сохранён: {Path}", path_mvk);
                }
                else
                {
                    Log.Warning("!!! Перечень МВК не найден !!!");
                }
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
        }
    }
}