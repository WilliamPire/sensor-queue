using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Sensor.Processameto.Domain.Eventos
{   
    public enum EnumStatus
    {
        [Description("Processado")]
        Processado,
        [Description("Erro")]
        Erro
    }
}
