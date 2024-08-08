using System;
using System.Net;
using System.Net.Mail;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json.Nodes;

namespace EmailConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "emailQueue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var emailMessage = System.Text.Json.JsonSerializer.Deserialize<MailEntitiy>(message);

                    if (emailMessage != null)
                    {
                        string to = emailMessage.To;
                        string subject = emailMessage.Subject;
                        string bodyMessage = emailMessage.Body;
                        string identity = emailMessage.Identity;


                        try
                        {
                            // SendEmail(to, subject, bodyMessage);
                            //  Task.Delay(3000).Wait();
                            Thread.Sleep(3000);
                            
                            SendFeedback(channel, identity, "Success");
                            Console.WriteLine("Feedback Success :" + identity);

                        }
                        catch (Exception )
                        {
                            Console.WriteLine(" [ Failed to send email ");
                            SendFeedback(channel, identity, "Failure");
                        }


                    }

                };

                channel.BasicConsume(queue: "emailQueue",
                                  autoAck: true,
                                  consumer: consumer);



                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }

        private static void SendEmail(string to, string subject, string body)
        {
            var fromAddress = new MailAddress("your-email@example.com", "Your Name");
            var toAddress = new MailAddress(to);
            const string fromPassword = "your-email-password";

            var smtp = new SmtpClient
            {
                Host = "smtp.example.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }

        private static void SendFeedback(IModel channel, string identity, string status)
        {
            var feedbackMessage = new { Identity = identity, Status = status };
            var message = System.Text.Json.JsonSerializer.Serialize(feedbackMessage);
            var body = Encoding.UTF8.GetBytes(message);


            channel.QueueDeclare(queue: "feedbackQueue" + identity.Replace("-", ""),
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: true,
                                 arguments: null);


            channel.BasicPublish(exchange: "",
                                 routingKey: "feedbackQueue" + identity.Replace("-", ""),
                                 basicProperties: null,
                                 body: body);


            Console.WriteLine("BasicPublish :" + identity);
        }
    }

    public class MailEntitiy
    {
        public  string To { get; set; }
        public  string Subject { get; set; }
        public  string Body { get; set; }
        public  string Identity { get; set; }



    }
}
