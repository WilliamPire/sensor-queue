using System;

namespace Sensor.Processameto.Domain.Eventos
{
    public class Evento
    {
        public Guid Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public string Tag { get; set; }
        public string Valor { get; set; }
        public string Status { get; set; }
    }
}
