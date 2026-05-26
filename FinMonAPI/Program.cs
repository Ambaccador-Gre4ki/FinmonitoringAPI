using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Serilog;

namespace FinMonAPI
{
    internal class Program
    {
        private const string BaseUrl = "https://portal.fedsfm.ru:8081/Services/fedsfm-service/";

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() // Записываем Debug, Information, Warning, Error
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: Path.Combine(AppContext.BaseDirectory, "logs", "log-.txt"),
                    rollingInterval: RollingInterval.Day, // Автоматический новый файл каждый день
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Приложение FinMonAPI успешно запущено.");

            // НАСТРОЙКИ ДЛЯ ПОДКЛЮЧЕНИЯ
            string thumbprint = "********";
            string login = "login";
            string password = "password";

            try
            {
                Log.Information("=== Поиск сертификата ===");
                Console.WriteLine("Шаг 1: Поиск ГОСТ-сертификата...");
                var cert = CertificateProvider.GetGostCertificate(thumbprint);
                Console.WriteLine($"Сертификат найден: {cert.Subject}\n");
                Log.Information("Сертификат успешно подключен: {Subject}", cert.Subject);

                Log.Information("=== Инициализация соединения ===");
                Console.WriteLine("Шаг 2: Инициализация HTTPS-соединения...");
                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(cert);

                // Для тестового контура (при проблемах со шлюзовыми SSL-сертификатами):
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                //ОТКЛЮЧАЕМ РЕЖИМЫ, КОТОРЫЕ МОГУТ БЛОКИРОВАТЬ ГОСТ (в некоторых сборках Windows это необходимо)
                handler.CheckCertificateRevocationList = false; // Отключаем онлайн-проверку отзыва (CRL)
                //
                using var httpClient = new HttpClient(handler);
                httpClient.BaseAddress = new Uri(BaseUrl);
                httpClient.Timeout = TimeSpan.FromMinutes(10); // Защита от таймаутов на больших файлах

                // Авторизация и получение JWT-токена
                Log.Information("=== Запрос токена ===");
                Console.WriteLine("Шаг 3: Запрос JWT-токена сессии...");
                var authService = new AuthService(httpClient);
                string accessToken = await authService.AuthenticateAsync(login, password);
                Console.WriteLine("Токен успешно получен!");
                Log.Information("Токен получен");

                // Ставим Bearer-токен в заголовки для всех последующих запросов
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Работа с каталогами справочников
                Log.Information("=== Скачивание справочников ===");
                var catalogService = new CatalogService(httpClient);
                Console.WriteLine("\nШаг 4: Скачивание актуальных перечней...");

                // --- 4.1 Скачивание Перечня Террористов (TE2) ---
                Log.Debug("/// Запрос актуального списка экстремистов ///");
                var te2Catalog = await catalogService.GetTe2CatalogAsync();
                if (te2Catalog != null && te2Catalog.IsActive)
                {
                    string path = Path.Combine(AppContext.BaseDirectory, "current_te2.zip");
                    Log.Information("Найден активный перечень ТЭ от {Date}. Скачивание файла...", te2Catalog.Date);
                    Console.WriteLine($"-> Найден активный перечень ТЭ от {te2Catalog.Date}. Скачивание...");
                    await catalogService.DownloadTe2FileAsync(te2Catalog.IdXml, path);
                    Console.WriteLine($"   Файл ТЭ сохранен: {path}");
                    Log.Information("Файл ТЭ успешно сохранен: {Path}", path);
                }
                else
                {
                    Log.Warning("!!! Актуальный перечень ТЭ не найден или неактивен !!!");
                }
                //// --- 4.2 Скачивание Русской версии Перечня ООН ---
                //Log.Debug("/// Запрос актуального каталога ООН (RU) ///");
                //var unRusCatalog = await catalogService.GetUnCatalogRusAsync();
                //if (unRusCatalog != null && unRusCatalog.IsActive)
                //{
                //    string path = Path.Combine(AppContext.BaseDirectory, "current_un_rus.xml");
                //    Log.Information("Найден активный перечень ООН (RU) от {Date}. Скачивание файла...", unRusCatalog.Date);
                //    Console.WriteLine($"-> Найден активный перечень ООН (RU) от {unRusCatalog.Date}. Скачивание...");
                //    await catalogService.DownloadUnFileAsync(unRusCatalog.IdXml, path);
                //    Console.WriteLine($"   Файл ООН (RU) сохранен: {path}");
                //    Log.Information("Файл ООН (RU) успешно сохранен: {Path}", path);
                //}
                //else
                //{
                //    Log.Warning("!!! Актуальный перечень ООН (RU) не найден или неактивен !!!");
                //}

                Console.WriteLine("\nВсе операции успешно завершены!");
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