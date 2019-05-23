using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace ExemploPDFGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando geração!");

            Exemplo p = new Exemplo();
            p.GerarPDF().Wait();

            Console.WriteLine("PDF Gerado!");
        }
    }
}
