
class Program
{
    static async Task Main()
    {
        var cts = new CancellationTokenSource();
        var paused = false;
        var pauseLock = new object();

        // Task para ler tecla e pausar/despausar
        var keyTask = Task.Run(() =>
        {
            Console.WriteLine("Pressione [Espaço] para pausar/despausar ou [Esc] para sair.");

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    cts.Cancel();
                    break;
                }
                if (key.Key == ConsoleKey.Spacebar)
                {
                    lock (pauseLock)
                    {
                        paused = !paused;
                        Console.WriteLine(paused ? "\n[Pausado]" : "\n[Resumido]");
                    }
                }
            }
        });

        var rand = new Random();
        while (!cts.IsCancellationRequested)
        {
            lock (pauseLock)
            {
                if (!paused)
                {
                    Console.Write(rand.Next(0, 100) + " ");
                }
            }
            await Task.Delay(200, cts.Token).ContinueWith(_ => { });
        }

        Console.WriteLine("\nEncerrado.");
    }
}
// console fica escrevendo numeros aleatorios na tela enquanto aguarda o usuario precionar uma tecla para pausar a geração de numeros
