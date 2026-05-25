using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;


namespace FinMonAPI
{
    internal class Program
    {
        public HttpClient CreateHttpClient(X509Certificate2 cert)
        {
            // Используем актуальный HttpClientHandler взамен WebRequestRequestHandler
            var handler = new HttpClientHandler();

            // Добавляем ваш ГОСТ-сертификат в коллекцию клиентских сертификатов
            handler.ClientCertificates.Add(cert);

            // Возвращаем клиент, который будет автоматически отправлять сертификат при каждом запросе
            return new HttpClient(handler);
        }

        static void Main(string[] args)
        {
        }
    }
}
