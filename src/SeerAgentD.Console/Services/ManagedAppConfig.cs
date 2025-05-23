using System.Collections.Generic;

namespace SeerD.Services
{
    /// <summary>
    /// Representa a configuração de um aplicativo monitorado.
    /// </summary>
    public class ManagedAppConfig
    {
        public string Name { get; set; }
        public string ExecutablePath { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
    }

    /// <summary>
    /// Representa o modelo de configuração de todos os aplicativos.
    /// </summary>
    public class AppsConfig
    {
        public List<ManagedAppConfig> Apps { get; set; }
    }
}