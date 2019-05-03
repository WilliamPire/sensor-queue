namespace Sensor.Processameto
{
    using Microsoft.Azure.ServiceBus;
    using MongoDB.Driver;
    using Sensor.Processameto.Core;
    using Sensor.Processameto.Domain.Eventos;
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {   
        const string CSTSERVICEBUS = "Endpoint=sb://sensor-bus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=wgfBPd8TMBRvlv5evuJdF0o71CGVYjN76ASj/mRwDlw=";
        const string QUEUENAME = "sensor-queue";
        static IQueueClient queueClient;

        static IMongoDatabase Database;
        static IMongoCollection<Evento> Collection;
        const string CSTMONGODB = "mongodb://adm:12345678wi@ds040027.mlab.com:40027/sensormdb";
        static IMongoClient mongoClient;

        static void Main(string[] args)
        {
            mongoClient = new MongoClient(CSTMONGODB);

            Database = mongoClient.GetDatabase("sensormdb");
            Collection = Database.GetCollection<Evento>(typeof(Evento).Name);

            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            queueClient = new QueueClient(CSTSERVICEBUS, QUEUENAME);

            Console.WriteLine("======================================================");
            Console.WriteLine("Press ENTER key to exit after receiving all the messages.");
            Console.WriteLine("======================================================");

            IniciarServiceBus();

            Console.ReadKey();

            await queueClient.CloseAsync();
        }

        static void IniciarServiceBus()
        {   
            var messageHandlerOptions = new MessageHandlerOptions(RegistrarExcecao)
            {
                MaxConcurrentCalls = 100,
                AutoComplete = false
            };

            queueClient.RegisterMessageHandler(ProcessarMensagemAsync, messageHandlerOptions);
        }

        static async Task ProcessarMensagemAsync(Message message, CancellationToken token)
        {
            try
            {
                Console.WriteLine($"Mensagem recebida: Número sequencial: {message.SystemProperties.SequenceNumber} Body: {Encoding.UTF8.GetString(message.Body)}");

                await queueClient.CompleteAsync(message.SystemProperties.LockToken);

                var evento = Newtonsoft.Json.JsonConvert.DeserializeObject<Domain.Eventos.Evento>(Encoding.UTF8.GetString(message.Body));
                evento.Status = EnumMethods.GetDescription(EnumStatus.Processado);

                if (await Collection.Find(x => x.Id == evento.Id).FirstOrDefaultAsync() == null)
                    await Collection.InsertOneAsync(evento);
            }
            catch (Exception ex)
            {
                var evento = Newtonsoft.Json.JsonConvert.DeserializeObject<Domain.Eventos.Evento>(Encoding.UTF8.GetString(message.Body));
                evento.Status = EnumMethods.GetDescription(EnumStatus.Erro);

                if (await Collection.Find(x => x.Id == evento.Id).FirstOrDefaultAsync() == null)
                    await Collection.InsertOneAsync(evento);

                throw ex;
            }
        }

        static Task RegistrarExcecao(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Exceção encontrada {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Contexto de exceção para solução de problemas:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");

            return Task.CompletedTask;
        }
    }
}
