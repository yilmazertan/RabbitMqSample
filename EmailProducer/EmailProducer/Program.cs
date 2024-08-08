using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace EmailProducer
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {

                string identity = Guid.NewGuid().ToString();

                channel.QueueDeclare(queue: "emailQueue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                channel.QueueDeclare(queue: "feedbackQueue" + identity.Replace("-", ""),
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: true,
                                     arguments: null);




                string to = "ylmz.ertan@gmail.com";
                string subject = "Subject " + identity;
                string body = "Body " + identity;


                var emailMessage = new { To = to, Subject = subject, Body = body, Identity = identity };
                var message = System.Text.Json.JsonSerializer.Serialize(emailMessage);
                var bodyBytes = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: "emailQueue",
                                     basicProperties: null,
                                     body: bodyBytes);

                Console.WriteLine(" [x] Sent {0}", message);

                var consumer = new EventingBasicConsumer(channel);

                channel.BasicConsume(queue: "feedbackQueue" + identity.Replace("-", ""),
                       autoAck: true,
                       consumer: consumer);


                consumer.Received += (model, ea) =>
                {
                    var feedbackBody = ea.Body.ToArray();
                    var feedbackMessage = Encoding.UTF8.GetString(feedbackBody);
                    var feedback = System.Text.Json.JsonSerializer.Deserialize<MailResult>(feedbackMessage);

                    if (feedback != null) 
                    if (feedback.Identity == identity)
                    {
                        Console.WriteLine("Email send status: {0} {1} ", feedback.Status, feedback.Identity);
                         channel.QueueDelete(queue: feedback.Identity.Replace("-", ""));
                        channel.Close();
                        connection.Close();
                    }

                };


                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }

        public class MailResult
        {
            public  string  Identity { get; set; }
            public  string Status { get; set; }




        }
    }
}
