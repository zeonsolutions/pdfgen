using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ExemploPDFGenerator
{
    public class Exemplo
    {
        private object GetData() {
            var clientes = new List<object>();
            clientes.Add(new { id = 1, name = "Joao", profissao = "Pedreiro", dataNasc = "31/05/1960"});
            clientes.Add(new { id = 2, name = "Pedro", profissao = "Arquiteto", dataNasc = "04/04/1998"});
            clientes.Add(new { id = 3, name = "Jonathas", profissao = "Zelador Condomínio", dataNasc = "01/11/1987"});
            clientes.Add(new { id = 4, name = "João Carlos", profissao = "Comerciante", dataNasc = "01/12/1974"});
            clientes.Add(new { id = 5, name = "Rafael", profissao = "Professor", dataNasc = "05/07/1990"});
            clientes.Add(new { id = 6, name = "Matheus", profissao = "Advogado", dataNasc = "08/06/1988"});
            clientes.Add(new { id = 7, name = "Bruno", profissao = "Programador", dataNasc = "22/08/1992"});
            clientes.Add(new { id = 8, name = "Valter", profissao = "Marceneiro", dataNasc = "08/03/1975"});
            clientes.Add(new { id = 9, name = "William", profissao = "Instrutor Auto-escola", dataNasc = "03/10/1979"});
            clientes.Add(new { id = 10, name = "Gabriel", profissao = "Youtuber", dataNasc = "26/06/2000"});

            return clientes;
        }

        public async Task<string> GerarPDF() {

            string templatesRootPath = @"./templates";
            string outputPath = @"./out/";

            bool force = true;

            PDFGenerator.PDFGenerator pdfGenerator = new PDFGenerator.PDFGenerator();
            bool retorno = pdfGenerator.Configure(Path.GetFullPath(templatesRootPath), Path.GetFullPath(outputPath), force);

            string ret = await pdfGenerator.Build("template1", "template1", GetData());

            Console.WriteLine("Path: {0}", ret);

            return ret;
        }
    }
}
