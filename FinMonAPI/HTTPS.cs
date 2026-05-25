using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace FinMonAPI
{
    public class Client
    {
        public HttpClient InitClient()
        {
            string myCertThumbprint = "ЗАМЕНИТЬ НА ОТПЕЧАТОК СЕРТИФИКАТА";

            // Достаем сертификат из системы
            var gostCert = CertificateProvider.GetGostCertificate(myCertThumbprint);

            // Инициализируем handler и прикрепляем сертификат
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(gostCert);

            // Создаем клиент для работы
            return new HttpClient(handler);
        }
    }
}