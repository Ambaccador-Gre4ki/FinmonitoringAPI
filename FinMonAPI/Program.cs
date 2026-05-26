using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FinMonAPI
{
    internal class Program
    {
        private const string BaseUrl = "https://fedsfm.ru";

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // НАСТРОЙКИ ДЛЯ ПОДКЛЮЧЕНИЯ
            string thumbprint = "ВАШ_ОТПЕЧАТОК_СЕРТИФИКАТА_КРИПТО_ПРО";
            string login = "ваш_логин_лк";
            string password = "ваш_пароль_лк";

            try
            {
                Console.WriteLine("Шаг 1: Поиск ГОСТ-сертификата...");
                var cert = CertificateProvider.GetGostCertificate(thumbprint);
                Console.WriteLine($"Сертификат найден: {cert.Subject}\n");

                Console.WriteLine("Шаг 2: Инициализация HTTPS-соединения...");
                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(cert);

                // Для тестового контура (при проблемах со шлюзовыми SSL-сертификатами):
                // handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                using var httpClient = new HttpClient(handler);
                httpClient.BaseAddress = new Uri(BaseUrl);
                httpClient.Timeout = TimeSpan.FromMinutes(10); // Защита от таймаутов на больших файлах

                // Авторизация и получение JWT-токена
                Console.WriteLine("Шаг 3: Запрос JWT-токена сессии...");
                var authService = new AuthService(httpClient);
                string accessToken = await authService.AuthenticateAsync(login, password);
                Console.WriteLine("Токен успешно получен!");

                // Ставим Bearer-токен в заголовки для всех последующих запросов
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Работа с каталогами справочников
                var catalogService = new CatalogService(httpClient);
                Console.WriteLine("\nШаг 4: Скачивание актуальных перечней...");

                // --- 4.1 Скачивание Перечня Террористов (TE2) ---
                var te2Catalog = await catalogService.GetTe2CatalogAsync();
                if (te2Catalog != null && te2Catalog.IsActive)
                {
                    string path = Path.Combine(AppContext.BaseDirectory, "current_te2.zip");
                    Console.WriteLine($"-> Найден активный перечень ТЭ от {te2Catalog.Date}. Скачивание...");
                    await catalogService.DownloadTe2FileAsync(te2Catalog.IdXml, path);
                    Console.WriteLine($"   Файл ТЭ сохранен: {path}");
                }

                //// --- 4.2 Скачивание Русской версии Перечня ООН ---
                //var unRusCatalog = await catalogService.GetUnCatalogRusAsync();
                //if (unRusCatalog != null && unRusCatalog.IsActive)
                //{
                //    string path = Path.Combine(AppContext.BaseDirectory, "current_un_rus.xml");
                //    Console.WriteLine($"-> Найден активный перечень ООН (RU) от {unRusCatalog.Date}. Скачивание...");
                //    await catalogService.DownloadUnFileAsync(unRusCatalog.IdXml, path);
                //    Console.WriteLine($"   Файл ООН (RU) сохранен: {path}");
                //}

                Console.WriteLine("\nВсе операции успешно завершены!");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nПроизошла критическая ошибка:");
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null) Console.WriteLine($"Детали: {ex.InnerException.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }
}