using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Itm.Shared.Events; // Traemos el evento de la clase 6 
using QRCoder; // Para generar el codigo QR

namespace Itm.Tickets.Functions
{
    public class GenerateQrFuction
    {
        private readonly ILogger<GenerateQrFuction> _logger;
        public GenerateQrFuction(ILogger<GenerateQrFuction> logger)
        {
            _logger = logger;
        }

        // El trigger:  Se despierta SOLA cuando llega una orden a RabbitMQ (o Azure Service Bus, o Kafka)
        [Function("GenerateQrCodeOnOrderCreated")]
        public void Run(
            [RabbitMQTrigger("order-created-queue", ConnectionStringSetting = "RabbitMQConnection")] OrderCreatedEvent orderEvent)
        {
            _logger.LogInformation($" !Despertando! Generando QR para la orden {orderEvent.OrderId}");
            _logger.LogInformation($" Usuario: {orderEvent.CustomerEmail} -Total: {orderEvent.TotalAmount}");

            // Generar codigo QR
            string qrText = $"OrdenId: {orderEvent.OrderId}, Correo: {orderEvent.CustomerEmail}, Total: {orderEvent.TotalAmount}";
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                string base64Image = Convert.ToBase64String(qrCodeImage);
                _logger.LogInformation($" [x] Código QR generado en base64: {base64Image.Substring(0, 50)}...");
            }

            _logger.LogInformation($" QR generado . Volviendo a dormir... Zzzzz");

        }
    }
}


