﻿using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

class VanityAddressGenerator
{
    static void Main(string[] args)
    {
        Console.WriteLine("Digite o prefixo que deseja encontrar:");
        string prefix = Console.ReadLine();

        Console.WriteLine("Escolha o tipo de endereço:");
        Console.WriteLine("1 - Legacy (começa com 1)");
        Console.WriteLine("2 - SegWit P2SH (começa com 3)");
        Console.WriteLine("3 - Native SegWit (bech32, começa com bc1)");
        int addressType = int.Parse(Console.ReadLine());

        if (addressType == 1 && !prefix.StartsWith("1"))
        {
            Console.WriteLine("Prefixo inválido! Endereços Legacy devem começar com '1'.");
            return;
        }
        if (addressType == 2 && !prefix.StartsWith("3"))
        {
            Console.WriteLine("Prefixo inválido! Endereços SegWit P2SH devem começar com '3'.");
            return;
        }
        if (addressType == 3 && !prefix.StartsWith("bc1q"))
        {
            Console.WriteLine("Prefixo inválido! Endereços Native SegWit devem começar com 'bc1q'.");
            return;
        }

        GenerateVanityAddressParallel(prefix, addressType);
    }

    static void GenerateVanityAddressParallel(string prefix, int addressType)
    {
        var startTime = DateTime.Now;
        var found = false; // Flag para indicar quando um endereço foi encontrado
        var lockObj = new object(); // Objeto para controle de sincronização
        long totalAttempts = 0; // Contador global de tentativas

        // Timer para atualizar a quantidade de verificações por segundo
        Timer timer = new Timer(state =>
        {
            lock (lockObj)
            {
                double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
                double attemptsPerSecond = totalAttempts / elapsedSeconds;
                Console.Clear(); // Limpa o console
                Console.WriteLine($"Procurando por prefixo: {prefix}");
                Console.WriteLine($"Total de chaves privadas verificadas: {totalAttempts}");
                Console.WriteLine($"Velocidade: {attemptsPerSecond:F2} chaves por segundo");
            }
        }, null, 0, 1000); // Atualiza a cada 1000 ms (1 segundo)

        // Usar Parallel.For para paralelismo
        Parallel.For(0, Environment.ProcessorCount / 2, (i, state) =>
        {
            var count = 0;

            while (!found)
            {
                // Gera uma nova chave privada
                Key privateKey = new Key();

                // Gera o endereço com base no tipo escolhido
                string address = addressType switch
                {
                    1 => privateKey.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main).ToString(),
                    2 => privateKey.PubKey.GetAddress(ScriptPubKeyType.SegwitP2SH, Network.Main).ToString(),
                    3 => privateKey.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main).ToString(),
                    _ => throw new ArgumentException("Tipo de endereço inválido.")
                };

                count++;
                Interlocked.Increment(ref totalAttempts); // Incrementa o contador global de forma segura

                // Verifica se o endereço começa com o prefixo desejado
                if (address.ToLower().StartsWith(prefix.ToLower()))
                {
                    // Bloqueia a execução de outras threads e marca o endereço como encontrado
                    lock (lockObj)
                    {
                        if (!found)
                        {
                            found = true; // Sinaliza que um endereço foi encontrado
                            var duration = DateTime.Now - startTime;
                            Console.Clear();
                            Console.WriteLine($"Endereço encontrado após {totalAttempts} tentativas em {duration.TotalSeconds} segundos.");
                            Console.WriteLine($"Endereço Bitcoin: {address}");
                            Console.WriteLine($"Chave Privada (WIF): {privateKey.GetWif(Network.Main)}");
                            // Para todas as outras threads
                            state.Stop();
                        }
                    }
                }
            }
        });

        // Finaliza o timer
        timer.Dispose();
    }
}
