using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace FinMonAPI
{
    public class ClientManager : IDisposable
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://fedsfm.ru";// Проверь адрес для создания соединения!

        /// <summary>
        /// Предоставляет доступ к настроенному HttpClient для всего приложения
        /// </summary>
        public HttpClient HttpClient => _httpClient;

        /// <summary>
        /// Инициализирует менеджер сетевого соединения с сертификатом
        /// </summary>
        /// <param name="certThumbprint">Отпечаток сертификата</param>
        public ClientManager(string certThumbprint)
        {
            if (string.IsNullOrWhiteSpace(certThumbprint))
                throw new ArgumentException("Отпечаток сертификата не может быть пустым.", nameof(certThumbprint));

            // Достаем сертификат из системы через ваш CertificateProvider
            var gostCert = CertificateProvider.GetGostCertificate(certThumbprint);

            // Инициализируем handler и прикрепляем сертификат
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(gostCert);

            // ВАЖНО ДЛЯ ТЕСТОВОГО КОНТУРА: Если на тестовом сервере Росфинмониторинга 
            // используется самоподписанный SSL-сертификат, Schannel может выдать ошибку валидации.
            // Если соединение будет обрываться, раскомментируйте строку ниже (ТОЛЬКО ДЛЯ ТЕСТОВ!):
            // handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            // Создаем единый клиент для работы и задаем базовый URL
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromMinutes(10) // Увеличиваем таймаут для загрузки больших ZIP-файлов и чанков
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
