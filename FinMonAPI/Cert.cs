using System;
using System.Security.Cryptography.X509Certificates;


namespace FinMonAPI
{
    internal static class CertificateProvider
    {
        /// <summary>
        /// Находит ГОСТ-сертификат в хранилище Личное (My) по его отпечатку.
        /// </summary>
        /// <param name="thumbprint">Сертификат</param>
        public static X509Certificate2 GetGostCertificate(string thumbprint)
        {
            // Удаляем пробелы и приводим к верхнему регистру (защита от ошибок копирования)
            string cleanThumbprint = thumbprint.Replace(" ", "").ToUpperInvariant();

            // Открываем хранилище "Личное" текущего пользователя
            // Если сертификат установлен "Для всех пользователей", заменить StoreLocation.CurrentUser на StoreLocation.LocalMachine
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);

                // Ищем сертификат по отпечатку. Третий параметр (false) позволяет найти в том числе просроченные сертификаты
                var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, cleanThumbprint, false);

                if (certCollection.Count == 0)
                {
                    throw new Exception($"Сертификат с отпечатком {cleanThumbprint} не найден в хранилище CurrentUser/My.");
                }

                var certificate = certCollection[0];

                return certificate;
            }
        }
    }
}